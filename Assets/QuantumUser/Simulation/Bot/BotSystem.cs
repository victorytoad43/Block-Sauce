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
            FP heading = frame.TryGet(character, out PlayerLook look) ? look.Yaw : FP._0;
            frame.Add(character, new Bot { Heading = heading });
            frame.Events.BotAdded(slot);
            return true;
        }

        /// <summary>
        /// Adds bots until every player slot is taken.
        /// </summary>
        public static void FillEmptySlots(Frame frame, BotConfig config)
        {
            while (AddBot(frame, config))
            {
            }
        }

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
            bot->Task = task;
            bot->Status = BotTaskStatus.Running;
            bot->Target = target;
            bot->Point = point;
            bot->Carried = EntityRef.None;
            bot->Step = 0;
            bot->Attempts = 0;
            bot->StepTimer = default;
            bot->TaskTimer = FrameTimer.FromSeconds(frame, config.TaskSeconds);
        }

        /// <summary>
        /// Writes each bot's input for this tick: its task's intent, or, while knocked down, tapping Jump to get up.
        /// Buttons keep their state from the last tick, so presses and releases register like a player's.
        /// </summary>
        private static void DriveBots(Frame frame, BotConfig config)
        {
            var bots = frame.Filter<Bot, Player>();
            while (bots.NextUnsafe(out EntityRef entity, out Bot* bot, out Player* player))
            {
                BotIntent intent;
                if (frame.Has<RagdollOwner>(entity))
                {
                    intent = new BotIntent { MoveYaw = bot->Heading, Jump = frame.Number % 8 < 2 };
                }
                else
                {
                    intent = BotActions.Tick(frame, entity, bot, config);
                }

                Input* input = &player->Input;
                input->HasCamera = true;
                input->Camera.Yaw = intent.MoveYaw;
                input->Camera.Pitch = 15;
                input->Move = intent.MoveAmount > FP._0 ? new FPVector2(FP._0, intent.MoveAmount) : FPVector2.Zero;
                input->Swing = intent.Swing;
                input->Run.Update(frame.Number, intent.Run);
                input->Grab.Update(frame.Number, intent.Grab);
                input->Throw.Update(frame.Number, intent.Throw);
                input->Jump.Update(frame.Number, intent.Jump);
                input->Crouch.Update(frame.Number, false);
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
