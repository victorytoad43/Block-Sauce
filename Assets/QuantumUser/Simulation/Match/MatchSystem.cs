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
                // The map's first tick: the room's settings start at their defaults.
                match->NewBotSkill = BotSkillLevel.Normal;
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
                            if (frame.RuntimeConfig.DemoMatch)
                            {
                                BotSystem.RollBots(frame);
                            }

                            StartMatch(frame, match, mode);
                        }
                    }
                    break;

                case MatchState.Countdown:
                    if (match->Timer.IsRunning(frame) == false)
                    {
                        SetState(frame, match, MatchState.Playing, mode.MatchSeconds);
                        mode.OnMatchStarted(frame);
                    }
                    break;

                case MatchState.Playing:
                    PlayerRef winner = mode.CheckWinner(frame);
                    bool ended = winner.IsValid;
                    if (ended == false && mode.MatchSeconds > FP._0)
                    {
                        ended = UpdateClock(frame, match, mode, out winner);
                    }

                    if (ended)
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

        /// <summary>
        /// The match clock: when it runs out, sudden death starts, and the next block to count wins (a player whose
        /// counted blocks go above the fewest they had since sudden death began). If none counts before sudden death's
        /// own clock runs out, the player with the most counted blocks in one tower wins; a tie is a draw (no winner).
        /// Returns true when the round is over.
        /// </summary>
        private static bool UpdateClock(Frame frame, Match* match, GameModeConfig mode, out PlayerRef winner)
        {
            winner = PlayerRef.None;
            if (match->SuddenDeath == false)
            {
                if (match->Timer.IsRunning(frame))
                    return false;

                match->SuddenDeath = true;
                match->Timer = FrameTimer.FromSeconds(frame, mode.SuddenDeathSeconds);
                for (int player = 0; player < Input.MAX_COUNT; player++)
                {
                    match->SuddenDeathCounted[player] = (byte)CountCounted(frame, player);
                }

                frame.Events.SuddenDeathStarted();
                return false;
            }

            int best = 0;
            for (int player = 0; player < Input.MAX_COUNT; player++)
            {
                int counted = CountCounted(frame, player);
                if (counted > match->SuddenDeathCounted[player])
                {
                    if (winner.IsValid == false || counted > best)
                    {
                        winner = player;
                        best = counted;
                    }
                }
                else
                {
                    match->SuddenDeathCounted[player] = (byte)counted;
                }
            }

            if (winner.IsValid)
                return true;

            if (match->Timer.IsRunning(frame))
                return false;

            bool tie = false;
            best = 0;
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                int tower = BlockTower.CountBest(frame, player);
                if (tower > best)
                {
                    best = tower;
                    winner = player;
                    tie = false;
                }
                else if (tower == best && tower > 0)
                {
                    tie = true;
                }
            }

            if (tie)
            {
                winner = PlayerRef.None;
            }

            return true;
        }

        /// <summary>
        /// How many of the player's blocks count right now, in any tower.
        /// </summary>
        private static int CountCounted(Frame frame, PlayerRef player)
        {
            int count = 0;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef _, out Block* block))
            {
                if (block->Owner == player && block->Stack == StackState.Counted)
                {
                    count++;
                }
            }

            return count;
        }

        private static void StartMatch(Frame frame, Match* match, GameModeConfig mode)
        {
            match->Winner = PlayerRef.None;
            match->SuddenDeath = false;
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
            match->SuddenDeath = false;
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
