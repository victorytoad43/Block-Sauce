using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Bots (docs/PLAN.md, Phase 6). A bot is a character in a player slot nobody is connected to, marked with
    /// <see cref="Bot"/>; it writes its own Player.Input each tick, before <see cref="PlayerSystem"/> would copy a
    /// client's, so every other system treats it as a player. Bots take the highest free slots, since clients joining
    /// are given the lowest; a client given a bot's slot takes over its character (see PlayerSystem). In the lobby the
    /// room's commands add and remove bots and set whether bots fill the empty slots when a match starts.
    /// </summary>
    public unsafe class BotSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
            if (frame.Unsafe.TryGetPointerSingleton(out Match* match) == false)
                return;

            BotConfig config = GetConfig(frame, *match);
            if (config == null)
                return;

            if (match->State == MatchState.Lobby)
            {
                ApplyRoomCommands(frame, match, config);
            }

            DriveBots(frame, config);
        }

        /// <summary>
        /// Adds a bot in the highest free player slot. Returns false when every slot is taken or bots aren't configured.
        /// </summary>
        public static bool AddBot(Frame frame, BotConfig config)
        {
            if (config == null || config.Avatar.IsValid == false || TryGetFreeSlot(frame, out PlayerRef slot) == false)
                return false;

            EntityRef character = PlayerSystem.SpawnCharacter(frame, slot, config.Avatar);
            MakeBot(frame, character, slot, config);
            return true;
        }

        /// <summary>
        /// Hands a character to a bot: from now on the bot writes its input and its brain decides what it does. Used for
        /// new bots, and by the dev bot match for a connected player's own character.
        /// </summary>
        public static void MakeBot(Frame frame, EntityRef character, PlayerRef slot, BotConfig config)
        {
            if (frame.Has<Bot>(character))
                return;

            FP heading = frame.TryGet(character, out PlayerLook look) ? look.Yaw : FP._0;
            BotSkillLevel skill = frame.TryGetSingleton(out Match match) ? match.NewBotSkill : BotSkillLevel.Normal;
            var personality = (BotPersonalityType)frame.RNG->Next(0, (int)BotPersonalityType.Klutz + 1);
            frame.Add(character, new Bot { Heading = heading, Skill = skill, Personality = personality });
            if (config.Brain.IsValid)
            {
                frame.Add<HFSMAgent>(character);
                HFSMManager.Init(frame, character, config.Brain);
            }

            frame.Events.BotAdded(slot);
        }

        /// <summary>
        /// Takes a character back from its bot, for the player in its slot; the player's input drives it again.
        /// </summary>
        public static void ReleaseBot(Frame frame, EntityRef character, PlayerRef slot)
        {
            if (frame.Has<Bot>(character) == false)
                return;

            frame.Remove<Bot>(character);
            frame.Remove<HFSMAgent>(character);
            frame.Events.BotRemoved(slot);
        }

        /// <summary>
        /// Adds bots until every player slot is taken.
        /// </summary>
        public static void FillEmptySlots(Frame frame, BotConfig config)
        {
            if (TestBotCount >= 0)
            {
                // Test only: exactly this many bots (head-to-head soaks).
                while (frame.ComponentCount<Bot>() > TestBotCount)
                {
                    RemoveBot(frame);
                }

                while (frame.ComponentCount<Bot>() < TestBotCount && AddBot(frame, config))
                {
                }

                return;
            }

            while (AddBot(frame, config))
            {
            }
        }

        /// <summary>
        /// Test only (editor soaks): when 0 or more, filling the slots at a match start leaves exactly this many bots
        /// (removing the highest ones). Never set in a match that's online.
        /// </summary>
        public static int TestBotCount = -1;

        /// <summary>
        /// Returns the bot config of the match's mode, or null.
        /// </summary>
        public static BotConfig GetConfig(Frame frame, Match match)
        {
            GameModeConfig mode = frame.FindAsset(match.Mode);
            return mode != null ? frame.FindAsset(mode.Bots) : null;
        }

        /// <summary>
        /// Adds and removes bots and sets the fill-at-start setting from any player's commands this tick.
        /// </summary>
        private static void ApplyRoomCommands(Frame frame, Match* match, BotConfig config)
        {
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                foreach (CommandAddBot _ in frame.GetPlayerCommands<CommandAddBot>(player))
                {
                    AddBot(frame, config);
                }

                foreach (CommandRemoveBot _ in frame.GetPlayerCommands<CommandRemoveBot>(player))
                {
                    RemoveBot(frame);
                }

                foreach (CommandSetBotFill command in frame.GetPlayerCommands<CommandSetBotFill>(player))
                {
                    match->LeaveSlotsEmpty = command.Fill == false;
                }

                foreach (CommandSetBotSkill command in frame.GetPlayerCommands<CommandSetBotSkill>(player))
                {
                    SetSkill(frame, match, command.Slot, command.Level);
                }
            }
        }

        /// <summary>
        /// Sets the level of the bot in <paramref name="slot"/> (if there is one) and of bots added from now on.
        /// </summary>
        private static void SetSkill(Frame frame, Match* match, int slot, BotSkillLevel level)
        {
            if (level < BotSkillLevel.Easy || level > BotSkillLevel.Expert)
                return;

            match->NewBotSkill = level;
            if (slot >= 0 && slot < frame.MaxPlayerCount && PlayerSystem.TryGetCharacter(frame, slot, out EntityRef character)
                && frame.Unsafe.TryGetPointer(character, out Bot* bot))
            {
                bot->Skill = level;
            }
        }

        /// <summary>
        /// Gives every bot a level (Easy to Expert) and a personality at random: the menu's demo match, at each match
        /// start, so the same bots play differently from match to match.
        /// </summary>
        public static void RollBots(Frame frame)
        {
            var bots = frame.Filter<Bot>();
            while (bots.NextUnsafe(out EntityRef _, out Bot* bot))
            {
                bot->Skill = (BotSkillLevel)frame.RNG->Next(0, (int)BotSkillLevel.Expert + 1);
                bot->Personality = (BotPersonalityType)frame.RNG->Next(0, (int)BotPersonalityType.Klutz + 1);
            }
        }

        /// <summary>
        /// Removes the bot in the highest slot, if there is one.
        /// </summary>
        private static void RemoveBot(Frame frame)
        {
            EntityRef highest = EntityRef.None;
            PlayerRef highestSlot = PlayerRef.None;
            var bots = frame.Filter<Bot, Player>();
            while (bots.NextUnsafe(out EntityRef entity, out Bot* _, out Player* player))
            {
                if (highest == EntityRef.None || (int)player->Ref > (int)highestSlot)
                {
                    highest = entity;
                    highestSlot = player->Ref;
                }
            }

            if (highest == EntityRef.None)
                return;

            frame.Signals.DropHeld(highest);
            frame.Destroy(highest);
            frame.Events.BotRemoved(highestSlot);
        }

        /// <summary>
        /// Gives a bot a task, replacing whatever it was doing; <see cref="BotActions"/> carries it out and sets its
        /// status to Done or Failed.
        /// </summary>
        public static void SetTask(Frame frame, Bot* bot, BotConfig config, BotTaskType task, EntityRef target,
            FPVector3 point)
        {
            BotSkill skill = config.GetSkill(frame, bot);
            bot->Task = task;
            bot->Status = BotTaskStatus.Running;
            bot->Target = target;
            bot->Point = point;
            bot->Carried = EntityRef.None;
            bot->Step = 0;
            bot->Attempts = 0;
            bot->StepTimer = default;
            BotNavigation.Clear(bot);
            bot->TaskTimer = FrameTimer.FromSeconds(frame, config.TaskSeconds);
            bot->ThinkTimer = task == BotTaskType.None ? default
                : FrameTimer.FromSeconds(frame, frame.RNG->Next(skill.ThinkSecondsMin, skill.ThinkSecondsMax));
            bot->AimError = new FPVector2(frame.RNG->Next(-skill.AimError, skill.AimError),
                frame.RNG->Next(-skill.AimError, skill.AimError));
            // A point in the disc of radius PlaceError.
            FP placeError = skill.PlaceError;
            BotPersonality personality = config.GetPersonality(frame, bot);
            EntityRef self = GetEntity(frame, bot);
            // A Klutz's planned mistakes for this task: a wild throw, a placing well off center.
            if (task == BotTaskType.ThrowAt && personality.WildThrowChance > FP._0
                && frame.RNG->Next() < personality.WildThrowChance)
            {
                FP wild = personality.WildThrowError;
                bot->AimError += new FPVector2(frame.RNG->Next(-wild, wild), frame.RNG->Next(-wild, wild));
                frame.Events.BotMistake(self, BotMistakeType.WildThrow);
            }

            if (task == BotTaskType.PlaceOn && personality.OffCenterChance > FP._0
                && frame.RNG->Next() < personality.OffCenterChance)
            {
                placeError = FPMath.Max(placeError, personality.OffCenterError);
                frame.Events.BotMistake(self, BotMistakeType.OffCenter);
            }

            FP angle = frame.RNG->Next(FP._0, FP.PiTimes2);
            FP radius = placeError * FPMath.Sqrt(frame.RNG->Next());
            bot->PlaceError = new FPVector2(FPMath.Cos(angle) * radius, FPMath.Sin(angle) * radius);
        }

        /// <summary>
        /// With empty hands and its target block close, a bot looks at it, since a grab takes the block the camera looks
        /// at (<see cref="GrabSystem"/>); otherwise it looks where it walks.
        /// </summary>
        private static void AimAtTarget(Frame frame, EntityRef entity, Bot* bot, ref FP yaw, ref FP pitch)
        {
            if (frame.Unsafe.TryGetPointer(entity, out Grabber* grabber) == false || grabber->Held != EntityRef.None
                || frame.Exists(bot->Target) == false || frame.Has<Block>(bot->Target) == false)
                return;

            GrabConfig grab = frame.FindAsset(grabber->Config);
            FPVector3 origin = frame.Get<Transform3D>(entity).Position + FPVector3.Up * grab.AimPivotHeight;
            FPVector3 toTarget = frame.Get<Transform3D>(bot->Target).Position - origin;
            FP flat = FPMath.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);
            if (flat > AimDistance || flat < FP.EN2)
                return;

            yaw = FPMath.Atan2(toTarget.X, toTarget.Z) * FP.Rad2Deg;
            pitch = FPMath.Clamp(FPMath.Atan2(-toTarget.Y, flat) * FP.Rad2Deg, -30, 70);
        }

        /// <summary>
        /// How close, in meters on the flat, a bot's target block must be before the bot looks at it.
        /// </summary>
        private static readonly FP AimDistance = (FP)35 / 10;

        /// <summary>
        /// Returns the entity whose Bot this is.
        /// </summary>
        private static EntityRef GetEntity(Frame frame, Bot* bot)
        {
            var bots = frame.Filter<Bot>();
            while (bots.NextUnsafe(out EntityRef entity, out Bot* other))
            {
                if (other == bot)
                    return entity;
            }

            return EntityRef.None;
        }

        /// <summary>
        /// Runs each bot's brain, which sets its task, then writes its input for this tick: the task's intent, or, while
        /// knocked down, tapping Jump to get up. Buttons keep their state from the last tick, so presses and releases
        /// register like a player's.
        /// </summary>
        private static void DriveBots(Frame frame, BotConfig config)
        {
            // Trips knock bots down, which adds components; that waits until the loop is done with its pointers.
            EntityRef* trips = stackalloc EntityRef[frame.MaxPlayerCount];
            int tripCount = 0;
            var bots = frame.Filter<Bot, Player>();
            while (bots.NextUnsafe(out EntityRef entity, out Bot* bot, out Player* player))
            {
                if (BotConfig.TestSkillBySlot != null && (int)player->Ref < BotConfig.TestSkillBySlot.Length)
                {
                    bot->Skill = BotConfig.TestSkillBySlot[(int)player->Ref];
                }

                if (BotConfig.TestPersonalityBySlot != null && (int)player->Ref < BotConfig.TestPersonalityBySlot.Length)
                {
                    bot->Personality = BotConfig.TestPersonalityBySlot[(int)player->Ref];
                }

                if (frame.Has<HFSMAgent>(entity))
                {
                    HFSMManager.Update(frame, frame.DeltaTime, entity);
                }

                BotTaskType task = bot->Task;
                bool running = bot->Status == BotTaskStatus.Running && task != BotTaskType.None;
                BotIntent intent;
                if (frame.Has<RagdollOwner>(entity))
                {
                    intent = new BotIntent { MoveYaw = bot->Heading, Jump = frame.Number % 8 < 2 };
                }
                else
                {
                    intent = BotActions.Tick(frame, entity, bot, config);
                }

                if (running && bot->Task == task && bot->Status != BotTaskStatus.Running)
                {
                    frame.Events.BotTaskEnded(entity, task, bot->Status, bot->Target);
                }

                Input* input = &player->Input;
                input->HasCamera = true;
                FP cameraYaw = intent.MoveYaw;
                FP cameraPitch = 15;
                AimAtTarget(frame, entity, bot, ref cameraYaw, ref cameraPitch);
                input->Camera.Yaw = cameraYaw;
                input->Camera.Pitch = cameraPitch;
                // Movement is relative to the camera, so a camera turned to aim keeps the bot walking its way.
                FP moveTurn = (intent.MoveYaw - cameraYaw) * FP.Deg2Rad;
                input->Move = intent.MoveAmount > FP._0
                    ? new FPVector2(FPMath.Sin(moveTurn), FPMath.Cos(moveTurn)) * intent.MoveAmount
                    : FPVector2.Zero;
                input->Swing = intent.Swing;
                input->Run.Update(frame.Number, intent.Run);
                input->Grab.Update(frame.Number, intent.Grab);
                input->Throw.Update(frame.Number, intent.Throw);
                input->Jump.Update(frame.Number, intent.Jump);
                input->Crouch.Update(frame.Number, intent.Crouch);
                if (intent.Trip && tripCount < frame.MaxPlayerCount)
                {
                    trips[tripCount++] = entity;
                }
            }

            for (int i = 0; i < tripCount; i++)
            {
                // Down at the run's speed, as a ragdoll; downed, it drops what it holds.
                frame.Signals.SetRagdoll(trips[i], true);
            }
        }

        /// <summary>
        /// Finds the highest player slot with no character in it.
        /// </summary>
        private static bool TryGetFreeSlot(Frame frame, out PlayerRef slot)
        {
            for (int index = frame.MaxPlayerCount - 1; index >= 0; index--)
            {
                if (PlayerSystem.TryGetCharacter(frame, index, out EntityRef _) == false)
                {
                    slot = index;
                    return true;
                }
            }

            slot = PlayerRef.None;
            return false;
        }
    }
}
