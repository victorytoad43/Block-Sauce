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
        /// Picks the block to fetch by scoring the options with the bot's personality (<see cref="BotPersonality"/>):
        /// Build, the nearest of its own blocks that is loose, on the ground and not its home block; Raid, the nearest
        /// rival's loose block, when a rival's tower is worth raiding (scored by how close that rival is to finishing);
        /// ClearTop, a rival's block resting on its own tower's top, when in reach. The highest score wins, with a little
        /// randomness to break ties. Never a block another bot is already fetching. None when there's nothing to fetch.
        /// </summary>
        public static EntityRef ChooseBlockToFetch(Frame frame, EntityRef entity, Bot* bot, BotConfig config)
        {
            PlayerRef self = frame.Get<Player>(entity).Ref;
            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef skip = bot->UnreachableTimer.IsRunning(frame) ? bot->Unreachable : EntityRef.None;
            BotPersonality personality = config.GetPersonality(frame, bot);

            EntityRef best = EntityRef.None;
            FP bestScore = FP._0;

            EntityRef own = FindNearestFreeBlock(frame, position, self, true, bot->Home, skip);
            FP score = personality.BuildWeight + Jitter(frame);
            if (own != EntityRef.None && score > bestScore)
            {
                best = own;
                bestScore = score;
            }

            if (FindRaidTarget(frame, self, bot, config, out int leadCount) != EntityRef.None)
            {
                EntityRef rival = FindNearestFreeBlock(frame, position, self, false, EntityRef.None, skip);
                score = personality.RaidWeight * leadCount / FPMath.Max(FP._1, GetTowerHeight(frame) - 1) + Jitter(frame);
                if (rival != EntityRef.None && score > bestScore)
                {
                    best = rival;
                    bestScore = score;
                }
            }

            EntityRef foreign = FindForeignTop(frame, self);
            score = personality.ClearTopWeight * 3 / 2 + Jitter(frame);
            if (foreign != EntityRef.None && foreign != skip && score > bestScore)
            {
                best = foreign;
            }

            return best;
        }

        /// <summary>
        /// Returns a rival's block resting on top of the bot's tower (it blocks building there), when it is low enough to
        /// grab from the ground and nobody holds it or is fetching it; otherwise none.
        /// </summary>
        public static EntityRef FindForeignTop(Frame frame, PlayerRef self)
        {
            EntityRef towerBase = BlockTower.GetBestBase(frame, self);
            if (towerBase == EntityRef.None)
                return EntityRef.None;

            EntityRef top = BlockTower.GetTop(frame, towerBase);
            FPVector3 topPosition = frame.Get<Transform3D>(top).Position;
            FP size = frame.TryGetSingleton(out Match match) ? frame.FindAsset(match.Mode).BlockSize : (FP)8 / 10;
            var blocks = frame.Filter<Block, Transform3D, Grabbable>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block, out Transform3D* transform, out Grabbable* grabbable))
            {
                if (block->Owner == self || grabbable->Holder != EntityRef.None || IsBeingFetched(frame, entity))
                    continue;

                FPVector3 offset = transform->Position - topPosition;
                if (offset.Y > size / 2 && offset.Y < size * 3 / 2 && offset.X * offset.X + offset.Z * offset.Z < size * size
                    && HeightAboveGround(frame, transform->Position) <= MaxFetchHeight)
                    return entity;
            }

            return EntityRef.None;
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

            BotPersonality personality = config.GetPersonality(frame, bot);
            if (heldBlock.Owner != self)
            {
                // At a rival carrying a block, now and then (Wreckers), or at the tower worth raiding; with neither, set
                // down a few steps away from the bot's own tower (a block cleared off its top).
                EntityRef target = EntityRef.None;
                if (personality.ThrowAtCarrierChance > FP._0 && frame.RNG->Next() < personality.ThrowAtCarrierChance)
                {
                    target = FindNearestCarrier(frame, entity, config.ThrowMaxRange + 2);
                }

                if (target == EntityRef.None)
                {
                    target = FindRaidTarget(frame, self, bot, config, out int _);
                }

                if (target != EntityRef.None)
                {
                    BotSystem.SetTask(frame, bot, config, BotTaskType.ThrowAt, target, default);
                }
                else
                {
                    BotSystem.SetTask(frame, bot, config, BotTaskType.Drop, EntityRef.None, GetPointAwayFromTower(frame, entity, self));
                }

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
            if (coated == false && frame.RNG->Next() < config.DunkChance * personality.DunkChanceScale)
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
        /// while the bot holds a block or is still waiting after its last tackle. A tackle the personality's TackleChance
        /// passes up starts the wait as if it had been made.
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
                if (distance < bestDistance && IsTackleTarget(frame, entity, bot, other, config))
                {
                    best = other;
                    bestDistance = distance;
                }
            }

            BotPersonality personality = config.GetPersonality(frame, bot);
            if (best != EntityRef.None && frame.RNG->Next() >= personality.TackleChance)
            {
                bot->TackleTimer = FrameTimer.FromSeconds(frame, config.TackleCooldownSeconds * personality.TackleCooldownScale);
                return EntityRef.None;
            }

            return best;
        }

        /// <summary>
        /// Returns whether <paramref name="target"/> is worth a slide tackle for the bot: a rival on its feet and in
        /// control, within TackleRange and about level with the bot, and, by the bot's personality, carrying one of the
        /// bot's blocks or any block, near the bot's tower (carrying anything, or at all for a guard), or standing at its
        /// own tower (Wreckers).
        /// </summary>
        public static bool IsTackleTarget(Frame frame, EntityRef entity, Bot* bot, EntityRef target, BotConfig config)
        {
            if (target == entity || frame.Has<Player>(target) == false || frame.Has<RagdollOwner>(target)
                || frame.TryGet(target, out Grabber grabber) == false
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

            BotPersonality personality = config.GetPersonality(frame, bot);
            PlayerRef self = frame.Get<Player>(entity).Ref;
            bool carrying = grabber.Held != EntityRef.None;
            if (carrying && (personality.TackleCarriers >= 2
                || (personality.TackleCarriers == 1 && frame.TryGet(grabber.Held, out Block held) && held.Owner == self)))
                return true;

            if ((carrying || personality.GuardEmptyHanded) && IsNearTower(frame, self, targetPosition, personality.GuardRadius))
                return true;

            return personality.TackleBuilders && IsNearTower(frame, frame.Get<Player>(target).Ref, targetPosition, FP._2 + FP._0_50);
        }

        /// <summary>
        /// Returns whether <paramref name="position"/> is within <paramref name="radius"/> (horizontally) of the base of
        /// <paramref name="owner"/>'s tower.
        /// </summary>
        private static bool IsNearTower(Frame frame, PlayerRef owner, FPVector3 position, FP radius)
        {
            EntityRef towerBase = BlockTower.GetBestBase(frame, owner);
            if (towerBase == EntityRef.None)
                return false;

            FPVector3 offset = position - frame.Get<Transform3D>(towerBase).Position;
            return offset.X * offset.X + offset.Z * offset.Z < radius * radius;
        }

        /// <summary>
        /// Returns the rival tower top worth raiding for this bot, and how many counted blocks that rival has: the leading
        /// rival, from the personality's raid count (or the skill's), or only one block from finishing for a personality
        /// that raids only near a win. None when no rival is worth it.
        /// </summary>
        private static EntityRef FindRaidTarget(Frame frame, PlayerRef self, Bot* bot, BotConfig config, out int count)
        {
            BotPersonality personality = config.GetPersonality(frame, bot);
            int raidAt = personality.RaidAtCount > 0 ? personality.RaidAtCount : config.GetSkill(frame, bot).RaidAtCount;
            if (personality.RaidOnlyNearWin)
            {
                raidAt = System.Math.Max(raidAt, GetTowerHeight(frame) - 1);
            }

            return FindLeadingRivalTop(frame, self, raidAt, out count);
        }

        /// <summary>
        /// Returns the nearest rival carrying a block, within <paramref name="range"/> (horizontally), or none.
        /// </summary>
        private static EntityRef FindNearestCarrier(Frame frame, EntityRef entity, FP range)
        {
            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef best = EntityRef.None;
            FP bestDistance = range * range;
            var players = frame.Filter<Player, Transform3D, Grabber>();
            while (players.NextUnsafe(out EntityRef other, out Player* _, out Transform3D* transform, out Grabber* grabber))
            {
                FPVector3 offset = transform->Position - position;
                FP distance = offset.X * offset.X + offset.Z * offset.Z;
                if (other != entity && grabber->Held != EntityRef.None && frame.Has<RagdollOwner>(other) == false
                    && distance < bestDistance)
                {
                    best = other;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns a point 3 m from the bot, on the side away from its own tower (where it is, with no tower).
        /// </summary>
        private static FPVector3 GetPointAwayFromTower(Frame frame, EntityRef entity, PlayerRef self)
        {
            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef towerBase = BlockTower.GetBestBase(frame, self);
            if (towerBase == EntityRef.None)
                return default;

            FPVector3 away = position - frame.Get<Transform3D>(towerBase).Position;
            away.Y = FP._0;
            if (away.SqrMagnitude < FP._0_01)
            {
                away = FPVector3.Forward;
            }

            return position + away.Normalized * 3;
        }

        /// <summary>
        /// A random 0 to 0.4 added to each option's score, so close options go either way.
        /// </summary>
        private static FP Jitter(Frame frame)
        {
            return frame.RNG->Next() * 2 / 5;
        }

        private static int GetTowerHeight(Frame frame)
        {
            return frame.TryGetSingleton(out Match match) ? frame.FindAsset(match.Mode).TowerHeight : 3;
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
        /// <paramref name="raidAtCount"/>, and that rival's count; otherwise none and 0.
        /// </summary>
        private static EntityRef FindLeadingRivalTop(Frame frame, PlayerRef self, int raidAtCount, out int count)
        {
            EntityRef best = EntityRef.None;
            int bestCount = raidAtCount - 1;
            count = 0;
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                EntityRef towerBase = player == (int)self ? EntityRef.None : BlockTower.GetBestBase(frame, player);
                if (towerBase == EntityRef.None)
                    continue;

                int towerCount = BlockTower.CountBest(frame, player);
                if (towerCount > bestCount)
                {
                    bestCount = towerCount;
                    best = BlockTower.GetTop(frame, towerBase);
                }
            }

            count = best != EntityRef.None ? bestCount : 0;
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
