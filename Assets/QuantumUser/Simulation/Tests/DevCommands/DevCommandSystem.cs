using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Test-only: puts the sender's next free block squarely on top of a tower, on whatever already sits there (or on
    /// the ground in front of the sender when that tower is empty), at rest and counted as placed. The tower is the
    /// sender's own unless TargetPlayer names a rival, which tests foreign blocks. Speeds up match tests that would
    /// otherwise need hand-scripted placing. Only debug builds act on it.
    /// </summary>
    public class CommandDevStackTower : DeterministicCommand
    {
        /// <summary>
        /// Whose tower to stack on, as a player index; -1 for the sender's own.
        /// </summary>
        public int TargetPlayer = -1;

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref TargetPlayer);
        }
    }

    /// <summary>
    /// Test-only: runs dev commands in debug builds. Does nothing in release builds.
    /// </summary>
    public unsafe class DevCommandSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
#if DEBUG
            if (frame.TryGetSingleton(out Match match) == false || match.State != MatchState.Playing)
                return;

            GameModeConfig mode = frame.FindAsset(match.Mode);
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                foreach (CommandDevStackTower command in frame.GetPlayerCommands<CommandDevStackTower>(player))
                {
                    PlayerRef tower = command.TargetPlayer >= 0 ? (PlayerRef)command.TargetPlayer : (PlayerRef)player;
                    StackNextBlock(frame, mode, player, tower);
                }
            }
#endif
        }

        private static void StackNextBlock(Frame frame, GameModeConfig mode, PlayerRef player, PlayerRef tower)
        {
            EntityRef next = EntityRef.None;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (block->Owner != player || block->Lock != LockState.Free)
                    continue;

                if (frame.TryGet(entity, out Grabbable candidateGrabbable) && candidateGrabbable.Holder != EntityRef.None)
                    continue;

                next = entity;
                break;
            }

            if (next == EntityRef.None)
                return;

            FPVector3 position;
            EntityRef top = BlockTower.GetTop(frame, tower);
            if (top != EntityRef.None)
            {
                // On the highest thing above the tower's top block, so a block already sitting there is stacked on.
                FPVector3 topPosition = frame.Get<Transform3D>(top).Position;
                FPVector3 above = topPosition + FPVector3.Up * 10;
                Physics3D.Hit3D? hit = frame.Physics3D.Raycast(above, FPVector3.Down, 10, -1,
                    QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics);
                FP surface = hit.HasValue ? hit.Value.Point.Y : topPosition.Y + mode.BlockSize * FP._0_50;
                position = topPosition;
                position.Y = surface + mode.BlockSize * FP._0_50;
            }
            else if (TryGetCharacterEntity(frame, player, out EntityRef characterEntity))
            {
                Transform3D characterTransform = frame.Get<Transform3D>(characterEntity);
                position = characterTransform.Position + characterTransform.Rotation * FPVector3.Forward * 2;
                position.Y = mode.BlockSize * FP._0_50;
            }
            else
            {
                return;
            }

            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(next);
            transform->Position = position;
            transform->Rotation = FPQuaternion.Identity;
            // Counts as placed by the player, as the lock system requires.
            frame.Unsafe.GetPointer<Block>(next)->Handled = true;

            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(next);
            body->Velocity = FPVector3.Zero;
            body->AngularVelocity = FPVector3.Zero;
            body->WakeUp();
        }

        private static bool TryGetCharacterEntity(Frame frame, PlayerRef player, out EntityRef entity)
        {
            var players = frame.Filter<Player>();
            while (players.Next(out entity, out Player candidate))
            {
                if (candidate.Ref == player)
                    return true;
            }

            entity = EntityRef.None;
            return false;
        }
    }
}
