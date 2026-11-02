using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Test-only: puts the sender's next block that isn't already in the tower squarely on top of that tower, on
    /// whatever already sits there (or on the ground in front of the sender when there is no tower), at rest. The
    /// tower is the sender's best stack, or a rival's when TargetPlayer names one, which tests foreign blocks. Speeds
    /// up match tests that would otherwise need hand-scripted placing. Only debug builds act on it.
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
    /// Test-only: knocks a player down with a hit from behind just over the knockdown threshold, through the normal
    /// knockback path. Works in the lobby and in a match. Only debug builds act on it.
    /// </summary>
    public class CommandDevKnockdown : DeterministicCommand
    {
        /// <summary>
        /// Whom to knock down, as a player index; -1 for the sender, -2 for everyone.
        /// </summary>
        public int TargetPlayer = -1;
        /// <summary>
        /// Sends the body flying at the target's own best tower, if it has one, instead of straight ahead.
        /// </summary>
        public bool TowardTower;
        /// <summary>
        /// Instead of a hit, moves the target 1.2 m in front of the sender and drops it limp there, ready to be grabbed
        /// (tests carrying bodies).
        /// </summary>
        public bool DropInFront;

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref TargetPlayer);
            stream.Serialize(ref TowardTower);
            stream.Serialize(ref DropInFront);
        }
    }

    /// <summary>
    /// Test-only: stacks free blocks (any owner) into one straight column 2 m in front of the sender, each resting
    /// exactly on the one below, at rest. Tests how tall stacks hold up. Works in the lobby and in a match. Only debug
    /// builds act on it.
    /// </summary>
    public class CommandDevBuildColumn : DeterministicCommand
    {
        /// <summary>
        /// How many blocks to stack.
        /// </summary>
        public int Count = 8;
        /// <summary>
        /// How far in front of the sender, in meters, the column stands.
        /// </summary>
        public FP Distance = 2;
        /// <summary>
        /// How many free blocks to pass over first, so a second column takes different blocks.
        /// </summary>
        public int Skip;

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref Count);
            stream.Serialize(ref Distance);
            stream.Serialize(ref Skip);
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
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                foreach (CommandDevKnockdown command in frame.GetPlayerCommands<CommandDevKnockdown>(player))
                {
                    PlayerRef target = command.TargetPlayer >= 0 ? (PlayerRef)command.TargetPlayer : (PlayerRef)player;
                    if (command.DropInFront)
                    {
                        DropInFront(frame, target, player);
                    }
                    else
                    {
                        KnockDown(frame, target, command.TargetPlayer == -2, command.TowardTower);
                    }
                }

                foreach (CommandDevBuildColumn command in frame.GetPlayerCommands<CommandDevBuildColumn>(player))
                {
                    BuildColumn(frame, player, command.Count, command.Distance, command.Skip);
                }
            }

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

        private static void DropInFront(Frame frame, PlayerRef target, PlayerRef sender)
        {
            EntityRef targetEntity = EntityRef.None, senderEntity = EntityRef.None;
            var players = frame.Filter<Player>();
            while (players.NextUnsafe(out EntityRef entity, out Player* player))
            {
                if (player->Ref == target) targetEntity = entity;
                if (player->Ref == sender) senderEntity = entity;
            }

            if (targetEntity == EntityRef.None || senderEntity == EntityRef.None || targetEntity == senderEntity)
                return;

            Transform3D senderTransform = frame.Get<Transform3D>(senderEntity);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(targetEntity);
            transform->Position = senderTransform.Position + senderTransform.Rotation * FPVector3.Forward * (FP)6 / 5;
            transform->Rotation = senderTransform.Rotation;
            frame.Unsafe.GetPointer<PhysicsBody3D>(targetEntity)->Velocity = FPVector3.Zero;
            frame.Signals.SetRagdoll(targetEntity, true);
        }

        private static void KnockDown(Frame frame, PlayerRef target, bool everyone, bool towardTower)
        {
            var players = frame.Filter<Player, Transform3D, PhysicsBody3D, Knockbackable>();
            while (players.NextUnsafe(out EntityRef entity, out Player* player, out Transform3D* transform,
                       out PhysicsBody3D* body, out Knockbackable* knockbackable))
            {
                if (everyone == false && player->Ref != target)
                    continue;

                KnockbackConfig config = frame.FindAsset(knockbackable->Config);
                FPVector3 flat = transform->Rotation * FPVector3.Forward;
                EntityRef tower = towardTower ? BlockTower.GetBestBase(frame, player->Ref) : EntityRef.None;
                if (tower != EntityRef.None)
                {
                    flat = frame.Get<Transform3D>(tower).Position - transform->Position;
                    flat.Y = FP._0;
                    flat = flat.Normalized;
                }

                FPVector3 direction = flat + FPVector3.Up * config.UpBias;
                FP speedChange = config.KnockdownSpeedChange + FP._1;
                frame.Signals.ApplyKnockback(entity, direction.Normalized * speedChange * body->Mass, EntityRef.None);
            }
        }

        private static void BuildColumn(Frame frame, PlayerRef player, int count, FP distance, int skip)
        {
            if (TryGetCharacterEntity(frame, player, out EntityRef character) == false)
                return;

            Transform3D characterTransform = frame.Get<Transform3D>(character);
            FPVector3 foot = characterTransform.Position + characterTransform.Rotation * FPVector3.Forward * distance;
            FP size = (FP)4 / 5;
            int level = 0;
            var blocks = frame.Filter<Block, Transform3D, PhysicsBody3D>();
            while (level < count && blocks.NextUnsafe(out EntityRef entity, out Block* _, out Transform3D* transform,
                       out PhysicsBody3D* body))
            {
                if (frame.TryGet(entity, out Grabbable grabbable) && grabbable.Holder != EntityRef.None)
                    continue;

                if (skip-- > 0)
                    continue;

                transform->Position = new FPVector3(foot.X, size * FP._0_50 + size * level, foot.Z);
                transform->Rotation = FPQuaternion.Identity;
                body->Velocity = FPVector3.Zero;
                body->AngularVelocity = FPVector3.Zero;
                body->WakeUp();
                level++;
            }
        }

        private static void StackNextBlock(Frame frame, GameModeConfig mode, PlayerRef player, PlayerRef tower)
        {
            EntityRef towerBase = BlockTower.GetBestBase(frame, tower);
            EntityRef next = EntityRef.None;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (block->Owner != player || (towerBase != EntityRef.None && block->Base == towerBase))
                    continue;

                if (frame.TryGet(entity, out Grabbable candidateGrabbable) && candidateGrabbable.Holder != EntityRef.None)
                    continue;

                next = entity;
                break;
            }

            if (next == EntityRef.None)
                return;

            FPVector3 position;
            EntityRef top = BlockTower.GetTop(frame, towerBase);
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
