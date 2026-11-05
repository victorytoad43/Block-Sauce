using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
    /// <summary>
    /// Runs match flow on maps that have a Match: Lobby → Countdown → Playing → RoundEnd → Results → Lobby.
    /// The lobby is a sandbox (free blocks, no locking, no win rule). Starting a match fills the empty player slots with
    /// bots unless the room turned that off, clears the sandbox blocks, moves every player to a match spawn and gives
    /// each their own blocks (at the map's block markers, if it has any); returning to the lobby removes the match blocks
    /// and puts the sandbox back. Rules come from the match's mode asset (see <see cref="GameModeConfig"/>).
    /// In the menu's demo match (<see cref="RuntimeConfig.DemoMatch"/>) the lobby starts the next match by itself.
    /// </summary>
    public unsafe class MatchSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
            if (frame.Unsafe.TryGetPointerSingleton(out Match* match) == false)
                return;

            GameModeConfig mode = frame.FindAsset(match->Mode);
            if (mode == null)
                return;

            if (match->SandboxRecorded == false)
            {
                RecordSandbox(frame, match);
            }

            switch (match->State)
            {
                case MatchState.Lobby:
                    if (WasStartRequested(frame) || frame.RuntimeConfig.DemoMatch)
                    {
                        if (match->LeaveSlotsEmpty == false)
                        {
                            BotSystem.FillEmptySlots(frame, frame.FindAsset(mode.Bots));
                        }

                        if (CountPlayers(frame) >= mode.MinPlayers)
                        {
                            StartMatch(frame, match, mode);
                        }
                    }
                    break;

                case MatchState.Countdown:
                    if (match->Timer.IsRunning(frame) == false)
                    {
                        SetState(frame, match, MatchState.Playing, FP._0);
                        mode.OnMatchStarted(frame);
                    }
                    break;

                case MatchState.Playing:
                    PlayerRef winner = mode.CheckWinner(frame);
                    if (winner.IsValid)
                    {
                        match->Winner = winner;
                        SetState(frame, match, MatchState.RoundEnd, mode.RoundEndSeconds);
                    }
                    break;

                case MatchState.RoundEnd:
                    if (match->Timer.IsRunning(frame) == false)
                    {
                        SetState(frame, match, MatchState.Results, mode.ResultsSeconds);
                    }
                    break;

                case MatchState.Results:
                    if (match->Timer.IsRunning(frame) == false)
                    {
                        ReturnToLobby(frame, match, mode);
                    }
                    break;
            }
        }

        /// <summary>
        /// Returns whether input should be ignored right now: during the countdown players can look around but not move.
        /// </summary>
        public static bool IsInputFrozen(Frame frame)
        {
            return frame.TryGetSingleton(out Match match) && match.State == MatchState.Countdown;
        }

        private static void StartMatch(Frame frame, Match* match, GameModeConfig mode)
        {
            match->Winner = PlayerRef.None;
            DestroyBlocks(frame);

            var players = frame.Filter<Player, Transform3D>();
            while (players.NextUnsafe(out EntityRef entity, out Player* player, out Transform3D* transform))
            {
                MovePlayerToSpawn(frame, entity, player->Ref, transform, mode.SpawnRadiusScale);
                SpawnPlayerBlocks(frame, mode, entity, player->Ref, transform);
            }

            SetState(frame, match, MatchState.Countdown, mode.CountdownSeconds);
        }

        private static void ReturnToLobby(Frame frame, Match* match, GameModeConfig mode)
        {
            DestroyBlocks(frame);

            QList<SandboxBlockHome> homes = frame.ResolveList(match->SandboxBlocks);
            for (int i = 0; i < homes.Count; i++)
            {
                EntityRef block = frame.Create(mode.BlockPrototype);
                Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(block);
                transform->Position = homes[i].Position;
                transform->Rotation = homes[i].Rotation;
            }

            var players = frame.Filter<Player, Transform3D>();
            while (players.NextUnsafe(out EntityRef entity, out Player* player, out Transform3D* transform))
            {
                MovePlayerToSpawn(frame, entity, player->Ref, transform, FP._1);
            }

            match->Winner = PlayerRef.None;
            SetState(frame, match, MatchState.Lobby, FP._0);
        }

        /// <summary>
        /// Remembers where the map's sandbox blocks start, so the lobby can be restored after a match.
        /// </summary>
        private static void RecordSandbox(Frame frame, Match* match)
        {
            QList<SandboxBlockHome> homes = frame.AllocateList(out match->SandboxBlocks);
            var blocks = frame.Filter<Block, Transform3D>();
            while (blocks.NextUnsafe(out EntityRef _, out Block* block, out Transform3D* transform))
            {
                if (block->Owner.IsValid == false)
                {
                    homes.Add(new SandboxBlockHome { Position = transform->Position, Rotation = transform->Rotation });
                }
            }

            match->SandboxRecorded = true;
        }

        /// <summary>
        /// Destroys every block: the sandbox's when a match starts, the match's when it ends.
        /// </summary>
        private static void DestroyBlocks(Frame frame)
        {
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* _))
            {
                frame.Destroy(entity);
            }
        }

        /// <summary>
        /// Moves a player, standing (got up if down), still and empty-handed, to their spawn point pushed out from the center by
        /// <paramref name="radiusScale"/>, facing the way the spawn faces.
        /// </summary>
        private static void MovePlayerToSpawn(Frame frame, EntityRef entity, PlayerRef player, Transform3D* transform, FP radiusScale)
        {
            frame.Signals.SetRagdoll(entity, false);
            frame.Signals.DropHeld(entity);

            if (PlayerSystem.TryGetSpawn(frame, player, out Transform3D spawn) == false)
                return;

            FPVector3 position = spawn.Position;
            position.X *= radiusScale;
            position.Z *= radiusScale;
            transform->Position = position + FPVector3.Up * frame.RuntimeConfig.PlayerSpawnHeightOffset;
            transform->Rotation = spawn.Rotation;

            if (frame.Unsafe.TryGetPointer(entity, out PhysicsBody3D* body))
            {
                body->Velocity = FPVector3.Zero;
                body->AngularVelocity = FPVector3.Zero;
                body->WakeUp();
            }
        }

        /// <summary>
        /// Returns where a player's tower is meant to start: their home block marker on a map with block markers,
        /// otherwise the middle of the row of blocks they start a match with, on the ground. False when the map has no
        /// spawn point for the player.
        /// </summary>
        public static bool TryGetBlockRowCenter(Frame frame, GameModeConfig mode, PlayerRef player, out FPVector3 center)
        {
            center = default;
            int slot = PlayerSystem.GetSpawnIndex(frame, player);
            var markers = frame.Filter<BlockSpawnPoint, Transform3D>();
            while (markers.NextUnsafe(out EntityRef _, out BlockSpawnPoint* marker, out Transform3D* markerTransform))
            {
                if (marker->Slot == slot && marker->Home)
                {
                    center = markerTransform->Position;
                    return true;
                }
            }

            if (PlayerSystem.TryGetSpawn(frame, player, out Transform3D spawn) == false)
                return false;

            center = spawn.Position;
            center.X *= mode.SpawnRadiusScale;
            center.Z *= mode.SpawnRadiusScale;
            center += spawn.Rotation * FPVector3.Forward * mode.BlockSpawnDistance;
            center.Y = mode.BlockSize * FP._0_50;
            return true;
        }

        /// <summary>
        /// Gives a player the mode's blocks, owned by them and in their team: one at each of the map's block markers for
        /// their spawn point (a bot's home is the Home one), or, on a map without markers, in a row in front of them.
        /// </summary>
        private static void SpawnPlayerBlocks(Frame frame, GameModeConfig mode, EntityRef entity, PlayerRef player, Transform3D* transform)
        {
            byte team = frame.TryGet(entity, out Team playerTeam) ? playerTeam.Id : TeamIds.None;
            if (frame.ComponentCount<BlockSpawnPoint>() > 0)
            {
                int slot = PlayerSystem.GetSpawnIndex(frame, player);
                var markers = frame.Filter<BlockSpawnPoint, Transform3D>();
                while (markers.NextUnsafe(out EntityRef _, out BlockSpawnPoint* marker, out Transform3D* markerTransform))
                {
                    if (marker->Slot != slot)
                        continue;

                    EntityRef block = CreateOwnedBlock(frame, mode, player, team, markerTransform->Position, markerTransform->Rotation);
                    if (marker->Home && frame.Unsafe.TryGetPointer(entity, out Bot* bot))
                    {
                        bot->Home = block;
                    }
                }

                return;
            }

            FPVector3 forward = transform->Rotation * FPVector3.Forward;
            FPVector3 right = transform->Rotation * FPVector3.Right;
            FPVector3 rowCenter = transform->Position + forward * mode.BlockSpawnDistance;
            rowCenter.Y = mode.BlockSize * FP._0_50;

            for (int i = 0; i < mode.BlocksPerPlayer; i++)
            {
                FP sideways = (i - (mode.BlocksPerPlayer - 1) * FP._0_50) * mode.BlockSpawnSpacing;
                CreateOwnedBlock(frame, mode, player, team, rowCenter + right * sideways, transform->Rotation);
            }
        }

        private static EntityRef CreateOwnedBlock(Frame frame, GameModeConfig mode, PlayerRef player, byte team,
            FPVector3 position, FPQuaternion rotation)
        {
            EntityRef block = frame.Create(mode.BlockPrototype);
            Transform3D* blockTransform = frame.Unsafe.GetPointer<Transform3D>(block);
            blockTransform->Position = position;
            blockTransform->Rotation = rotation;
            frame.Unsafe.GetPointer<Block>(block)->Owner = player;

            if (frame.Unsafe.TryGetPointer(block, out Team* blockTeam))
            {
                blockTeam->Id = team;
            }

            return block;
        }

        private static void SetState(Frame frame, Match* match, MatchState state, FP seconds)
        {
            match->State = state;
            match->Timer = seconds > FP._0 ? FrameTimer.FromSeconds(frame, seconds) : default;
            frame.Events.MatchStateChanged(state, match->Winner);
        }

        private static bool WasStartRequested(Frame frame)
        {
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                foreach (CommandStartMatch _ in frame.GetPlayerCommands<CommandStartMatch>(player))
                    return true;
            }

            return false;
        }

        private static int CountPlayers(Frame frame)
        {
            int count = 0;
            var players = frame.Filter<Player>();
            while (players.NextUnsafe(out EntityRef _, out Player* _))
            {
                count++;
            }

            return count;
        }
    }
}
