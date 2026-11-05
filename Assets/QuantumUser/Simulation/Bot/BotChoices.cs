using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// The choices behind a bot's brain (docs/PLAN.md, Phase 6): which block to fetch, and what to do with a block in
    /// hand. The Bot SDK state machine (built by Block Sauce › Build Bot Brain) calls them through its actions
    /// (<see cref="BotFetchAction"/>, <see cref="BotUseBlockAction"/>); <see cref="BotActions"/> carries out the task each
    /// one sets. Bots should look like players, not play well.
    /// </summary>
    public static unsafe class BotChoices
    {
        /// <summary>
        /// The highest a block's center may be above the ground under it for a bot to fetch it: a block lying on one
        /// other block.
        /// </summary>
        private static readonly FP MaxFetchHeight = (FP)7 / 5;
        /// <summary>
        /// The highest a block's center may be above the ground under it to count as on the ground (half a block, plus
        /// slack for a tilt on uneven ground).
        /// </summary>
        private static readonly FP MaxGroundHeight = (FP)6 / 10;
        /// <summary>
        /// How far down, in meters, the ground under a block is looked for.
        /// </summary>
        private static readonly FP GroundProbeLength = 4;

        /// <summary>
        /// Test only (editor soaks comparing with and without): no bot starts a slide tackle. Never set in a match
        /// that's online, since every client must agree.
        /// </summary>
        public static bool TestDisableTackles;

        /// <summary>
        /// Picks the block to fetch: the nearest of the bot's own blocks that is loose, on the ground and not its home
        /// block; when all its blocks are stacked, the nearest rival's loose block to raid with, as long as a rival has a
        /// tower worth raiding (RaidAtCount). Never a block another bot is already fetching. None when there's nothing to
        /// fetch.
        /// </summary>
        public static EntityRef ChooseBlockToFetch(Frame frame, EntityRef entity, Bot* bot, BotConfig config)
        {
            PlayerRef self = frame.Get<Player>(entity).Ref;
            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef skip = bot->UnreachableTimer.IsRunning(frame) ? bot->Unreachable : EntityRef.None;
            EntityRef own = FindNearestFreeBlock(frame, position, self, true, bot->Home, skip);
            if (own != EntityRef.None || FindLeadingRivalTop(frame, self, config) == EntityRef.None)
                return own;

            return FindNearestFreeBlock(frame, position, self, false, EntityRef.None, skip);
        }

        /// <summary>
        /// Decides what to do with the block in hand and sets the task: a rival's block is thrown at the leading rival's
        /// tower; anything else (a knocked-down player, say) is set down; the bot's own block goes on its tower (after a
        /// Stack dunk, some of the time), or, with no home block on the ground, is carried to where its blocks started
        /// and set down to become its home.
        /// </summary>
        public static void UseHeldBlock(Frame frame, EntityRef entity, Bot* bot, BotConfig config)
        {
            EntityRef held = frame.Get<Grabber>(entity).Held;
            PlayerRef self = frame.Get<Player>(entity).Ref;
            if (held == EntityRef.None)
            {
                BotSystem.SetTask(frame, bot, config, BotTaskType.None, EntityRef.None, default);
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            if (frame.TryGet(held, out Block heldBlock) == false)
            {
                BotSystem.SetTask(frame, bot, config, BotTaskType.Drop, EntityRef.None, default);
                return;
            }

            if (heldBlock.Owner != self)
            {
                EntityRef rival = FindLeadingRivalTop(frame, self, config);
                BotSystem.SetTask(frame, bot, config, rival != EntityRef.None ? BotTaskType.ThrowAt : BotTaskType.Drop,
                    rival, default);
                return;
            }

            EntityRef top = FindHomeTop(frame, entity, bot, held);
            if (top == EntityRef.None)
            {
                // Out in the open where its blocks started, rather than wherever the scramble left it.
                FPVector3 homePoint = default;
                if (frame.TryGetSingleton(out Match match))
                {
                    MatchSystem.TryGetBlockRowCenter(frame, frame.FindAsset(match.Mode), self, out homePoint);
                }

                BotSystem.SetTask(frame, bot, config, BotTaskType.Drop, EntityRef.None, homePoint);
                bot->Home = held;
                return;
            }

            bool coated = frame.TryGet(held, out SauceCoat coat) && coat.DipCount > 0;
            if (coated == false && frame.RNG->Next() < config.DunkChance)
            {
                EntityRef vat = FindNearestVat(frame, frame.Get<Transform3D>(entity).Position, SauceType.Stack);
                if (vat != EntityRef.None)
                {
                    BotSystem.SetTask(frame, bot, config, BotTaskType.Dunk, vat, default);
                    return;
                }
            }

            BotSystem.SetTask(frame, bot, config, BotTaskType.PlaceOn, top, default);
        }

        /// <summary>
        /// Returns the rival worth a slide tackle nearest the bot (see <see cref="IsTackleTarget"/>), or none; none too
        /// while the bot holds a block or is still waiting after its last tackle.
        /// </summary>
        public static EntityRef ChooseTackleTarget(Frame frame, EntityRef entity, Bot* bot, BotConfig config)
        {
            if (TestDisableTackles || bot->TackleTimer.IsRunning(frame) || frame.Get<Grabber>(entity).Held != EntityRef.None)
                return EntityRef.None;

            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef best = EntityRef.None;
            FP bestDistance = FP.UseableMax;
            var players = frame.Filter<Player, Transform3D>();
            while (players.NextUnsafe(out EntityRef other, out Player* _, out Transform3D* transform))
            {
                FP distance = (transform->Position - position).SqrMagnitude;
                if (distance < bestDistance && IsTackleTarget(frame, entity, other, config))
                {
                    best = other;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns whether <paramref name="target"/> is worth a slide tackle for the bot: a rival on its feet and in
        /// control, within TackleRange and about level with the bot, carrying one of the bot's blocks, or carrying
        /// anything near the bot's tower.
        /// </summary>
        public static bool IsTackleTarget(Frame frame, EntityRef entity, EntityRef target, BotConfig config)
        {
            if (target == entity || frame.Has<Player>(target) == false || frame.Has<RagdollOwner>(target)
                || frame.TryGet(target, out Grabber grabber) == false || grabber.Held == EntityRef.None
                || (frame.TryGet(target, out MovementData movement) && (movement.Tier != ControlTier.InControl || movement.IsSliding)))
                return false;

            if (frame.TryGet(entity, out Team team) && team.Id != TeamIds.None && frame.TryGet(target, out Team targetTeam)
                && targetTeam.Id == team.Id)
                return false;

            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            FPVector3 targetPosition = frame.Get<Transform3D>(target).Position;
            FPVector3 offset = targetPosition - position;
            if (FPMath.Abs(offset.Y) > FP._1 || offset.X * offset.X + offset.Z * offset.Z > config.TackleRange * config.TackleRange)
                return false;

            PlayerRef self = frame.Get<Player>(entity).Ref;
            if (frame.TryGet(grabber.Held, out Block held) && held.Owner == self)
                return true;

            EntityRef towerBase = BlockTower.GetBestBase(frame, self);
            if (towerBase == EntityRef.None)
                return false;

            FPVector3 tower = frame.Get<Transform3D>(towerBase).Position;
            FPVector3 fromTower = targetPosition - tower;
            return fromTower.X * fromTower.X + fromTower.Z * fromTower.Z < config.TackleTowerRadius * config.TackleTowerRadius;
        }

        /// <summary>
        /// Returns the top block of the bot's tower: the tallest stack it owns, or its home block (chosen here, the
        /// nearest of its own loose ground blocks, if it has none yet). None when it has no block on the ground.
        /// </summary>
        private static EntityRef FindHomeTop(Frame frame, EntityRef entity, Bot* bot, EntityRef held)
        {
            PlayerRef self = frame.Get<Player>(entity).Ref;
            EntityRef towerBase = BlockTower.GetBestBase(frame, self);
            if (towerBase != EntityRef.None)
            {
                bot->Home = towerBase;
                return BlockTower.GetTop(frame, towerBase);
            }

            if (IsUsableHome(frame, bot->Home, self, held) == false)
            {
                bot->Home = FindNearestFreeBlock(frame, frame.Get<Transform3D>(entity).Position, self, true, held,
                    EntityRef.None);
                if (IsUsableHome(frame, bot->Home, self, held) == false)
                {
                    bot->Home = EntityRef.None;
                }
            }

            return bot->Home;
        }

        private static bool IsUsableHome(Frame frame, EntityRef home, PlayerRef self, EntityRef held)
        {
            return home != held && frame.Exists(home) && frame.Get<Block>(home).Owner == self
                && frame.Get<Grabbable>(home).Holder == EntityRef.None
                && HeightAboveGround(frame, frame.Get<Transform3D>(home).Position) < MaxGroundHeight;
        }

        /// <summary>
        /// Returns how far <paramref name="position"/> is above the static ground straight under it (the arena's ground
        /// is uneven), or a large number over a drop.
        /// </summary>
        public static FP HeightAboveGround(Frame frame, FPVector3 position)
        {
            Hit3D? hit = frame.Physics3D.Raycast(position, FPVector3.Down, GroundProbeLength, -1, QueryOptions.HitStatics);
            return hit.HasValue ? position.Y - hit.Value.Point.Y : FP.UseableMax;
        }

        /// <summary>
        /// Returns the top of the tallest tower of the rival with the most counted blocks, as long as that is at least
        /// RaidAtCount; otherwise none.
        /// </summary>
        private static EntityRef FindLeadingRivalTop(Frame frame, PlayerRef self, BotConfig config)
        {
            EntityRef best = EntityRef.None;
            int bestCount = config.RaidAtCount - 1;
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                EntityRef towerBase = player == (int)self ? EntityRef.None : BlockTower.GetBestBase(frame, player);
                if (towerBase == EntityRef.None)
                    continue;

                int count = BlockTower.CountBest(frame, player);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = BlockTower.GetTop(frame, towerBase);
                }
            }

            return best;
        }

        /// <summary>
        /// Returns the nearest loose block within reach (on the ground, or lying on one other block) with nothing on it,
        /// not stacked on another block of its stack, nobody holding it, owned by
        /// <paramref name="self"/> (or, when <paramref name="own"/> is false, by someone else), other than
        /// <paramref name="exclude"/> and <paramref name="skip"/>, and that no bot is on its way to fetch.
        /// </summary>
        private static EntityRef FindNearestFreeBlock(Frame frame, FPVector3 position, PlayerRef self, bool own,
            EntityRef exclude, EntityRef skip)
        {
            EntityRef best = EntityRef.None;
            FP bestDistance = FP.UseableMax;
            var blocks = frame.Filter<Block, Transform3D, Grabbable>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block, out Transform3D* transform, out Grabbable* grabbable))
            {
                if (entity == exclude || entity == skip || grabbable->Holder != EntityRef.None || block->Stack != StackState.Loose
                    || (block->StackLevel != BlockStackSystem.NotStacked && block->StackLevel > 0) || block->HasAbove
                    || (block->Owner == self) != own
                    || HeightAboveGround(frame, transform->Position) > MaxFetchHeight
                    || IsBeingFetched(frame, entity))
                    continue;

                FP distance = (transform->Position - position).SqrMagnitude;
                if (distance < bestDistance)
                {
                    best = entity;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsBeingFetched(Frame frame, EntityRef block)
        {
            var bots = frame.Filter<Bot>();
            while (bots.NextUnsafe(out EntityRef _, out Bot* bot))
            {
                if (bot->Task == BotTaskType.Fetch && bot->Status == BotTaskStatus.Running && bot->Target == block)
                    return true;
            }

            return false;
        }

        private static EntityRef FindNearestVat(Frame frame, FPVector3 position, SauceType type)
        {
            EntityRef best = EntityRef.None;
            FP bestDistance = FP.UseableMax;
            var vats = frame.Filter<SauceVat, Transform3D>();
            while (vats.NextUnsafe(out EntityRef entity, out SauceVat* vat, out Transform3D* transform))
            {
                FP distance = (transform->Position - position).SqrMagnitude;
                if (vat->Type == type && distance < bestDistance)
                {
                    best = entity;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
