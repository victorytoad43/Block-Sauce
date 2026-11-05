using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Spawns a character at a spawn point for each player who joins, feeds it that player's input every tick,
    /// and destroys it when the player disconnects. A bot's character (<see cref="BotSystem"/>) writes its own input;
    /// a player given a bot's slot takes over that character.
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
        /// Copies the player's input for this tick onto their character (a bot has already written its own).
        /// When the input carries no camera angles, such as a slot the server zero-filled, the last ones are kept.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            Player* player = filter.Player;
            if (frame.Has<Bot>(filter.Entity) == false)
            {
                Input* input = frame.GetPlayerInput(player->Ref);
                InputPitchYaw previousCamera = player->Input.Camera;

                player->Input = *input;

                if (input->HasCamera == false)
                {
                    player->Input.Camera = previousCamera;
                }
            }

            if (MatchSystem.IsInputFrozen(frame))
            {
                InputPitchYaw camera = player->Input.Camera;
                player->Input = default;
                player->Input.HasCamera = true;
                player->Input.Camera = camera;
            }
        }

        /// <summary>
        /// Returns the spawn point at the player's index, if the map has one.
        /// </summary>
        public static bool TryGetSpawn(Frame frame, PlayerRef player, out Transform3D spawn)
        {
            Transform3D* transform = FindSpawnTransform(frame, (int)player);
            spawn = transform != null ? *transform : default;
            return transform != null;
        }

        /// <summary>
        /// Spawns the joining player's character, or, when a bot already holds the slot, hands that bot's character to
        /// the player.
        /// </summary>
        void ISignalOnPlayerAdded.OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
        {
            if (TryGetCharacter(frame, player, out EntityRef existing))
            {
                BotSystem.ReleaseBot(frame, existing, player);
                return;
            }

            SpawnCharacter(frame, player, frame.GetPlayerData(player).PlayerAvatar);
        }

        /// <summary>
        /// Finds the character in a player slot.
        /// </summary>
        public static bool TryGetCharacter(Frame frame, PlayerRef player, out EntityRef character)
        {
            var players = frame.Filter<Player>();
            while (players.NextUnsafe(out EntityRef entity, out Player* candidate))
            {
                if (candidate->Ref == player)
                {
                    character = entity;
                    return true;
                }
            }

            character = EntityRef.None;
            return false;
        }

        /// <summary>
        /// Spawns a character for a player slot above the spawn point at the slot's index, facing the way the spawn
        /// faces, and puts it on a team of its own (free-for-all). Game modes reassign teams once match flow exists.
        /// Without a matching spawn point, characters spawn 2 meters apart along X, starting at the origin.
        /// </summary>
        public static EntityRef SpawnCharacter(Frame frame, PlayerRef player, AssetRef<EntityPrototype> avatar)
        {
            EntityRef playerEntity = frame.Create(avatar);

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

            return playerEntity;
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
        /// Returns which spawn point, in map order, player <paramref name="player"/> uses (see FindSpawnTransform).
        /// </summary>
        public static int GetSpawnIndex(Frame frame, int player)
        {
            int spawnPoints = frame.ComponentCount<PlayerSpawnPoint>();
            if (spawnPoints > frame.MaxPlayerCount)
                return (player * spawnPoints + frame.MaxPlayerCount / 2) / frame.MaxPlayerCount;

            return player;
        }

        /// <summary>
        /// Returns the transform of the spawn point for player <paramref name="index"/>, or null when the map has fewer
        /// spawn points. A map with more spawn points than player slots (the Lobby's ring of 8, with 6 players) spreads
        /// the players across them, so they stand evenly round the ring rather than bunched on one side.
        /// </summary>
        private static Transform3D* FindSpawnTransform(Frame frame, int index)
        {
            index = GetSpawnIndex(frame, index);

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
