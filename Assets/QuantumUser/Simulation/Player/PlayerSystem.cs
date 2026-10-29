using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Spawns a character at a spawn point for each player who joins, feeds it that player's input every tick,
    /// and destroys it when the player disconnects.
    /// </summary>
    public unsafe class PlayerSystem : SystemMainThreadFilter<PlayerSystem.Filter>, ISignalOnPlayerAdded, ISignalOnPlayerDisconnected
    {
        /// <summary>
        /// The components the player system updates for each player character.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Player* Player;
        }

        /// <summary>
        /// Copies the player's input for this tick onto their character.
        /// When the input carries no camera angles, such as a slot the server zero-filled, the last ones are kept.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            Player* player = filter.Player;
            Input* input = frame.GetPlayerInput(player->Ref);

            InputPitchYaw previousCamera = player->Input.Camera;

            player->Input = *input;

            if (input->HasCamera == false)
            {
                player->Input.Camera = previousCamera;
            }
        }

        /// <summary>
        /// Spawns the player's character above the spawn point at the player's index, facing the way the spawn faces,
        /// and puts it on a team of its own (free-for-all). Game modes reassign teams once match flow exists.
        /// Without a matching spawn point, characters spawn 2 meters apart along X, starting at the origin.
        /// </summary>
        void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
        {
            RuntimePlayer data = frame.GetPlayerData(player);
            EntityRef playerEntity = frame.Create(data.PlayerAvatar);

            frame.Unsafe.GetPointer<Player>(playerEntity)->Ref = player;

            if (frame.Unsafe.TryGetPointer(playerEntity, out Team* team))
            {
                team->Id = (byte)(int)player;
            }

            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(playerEntity);

            Transform3D* spawnTransform = FindSpawnTransform(frame, (int)player);
            FPVector3 yOffset = FPVector3.Up * frame.RuntimeConfig.PlayerSpawnHeightOffset;
            if (spawnTransform != null)
            {
                transform->Position = spawnTransform->Position + yOffset;
                transform->Rotation = spawnTransform->Rotation;

                if (frame.Unsafe.TryGetPointer(playerEntity, out PlayerLook* look))
                {
                    FPVector3 forward = spawnTransform->Rotation * FPVector3.Forward;
                    look->Yaw = FPMath.Atan2(forward.X, forward.Z) * FP.Rad2Deg;
                }
            }
            else
            {
                transform->Position = FPVector3.Right * 2 * (int)player + yOffset;
            }
        }

        /// <summary>
        /// Destroys the disconnected player's character.
        /// </summary>
        void ISignalOnPlayerDisconnected.OnPlayerDisconnected(Frame frame, PlayerRef player)
        {
            var players = frame.Filter<Player>();
            while (players.NextUnsafe(out EntityRef entity, out Player* candidate))
            {
                if (candidate->Ref != player)
                    continue;

                frame.Destroy(entity);
            }
        }

        /// <summary>
        /// Returns the transform of the spawn point at <paramref name="index"/>, or null when the map has fewer spawn points.
        /// </summary>
        private Transform3D* FindSpawnTransform(Frame frame, int index)
        {
            int count = 0;

            var filter = frame.Filter<PlayerSpawnPoint, Transform3D>();
            while (filter.NextUnsafe(out EntityRef _, out PlayerSpawnPoint* _, out Transform3D* transform))
            {
                if (count == index)
                    return transform;

                count++;
            }

            return null;
        }
    }
}
