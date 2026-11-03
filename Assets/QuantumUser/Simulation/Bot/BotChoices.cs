using Photon.Deterministic;

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
        /// Picks the block to fetch: the nearest of the bot's own blocks that is loose, on the ground and not its home
        /// block; when all its blocks are stacked, the nearest rival's loose block to raid with, as long as a rival has a
        /// tower to throw it at. None when there's nothing to fetch.
        /// </summary>
        public static EntityRef ChooseBlockToFetch(Frame frame, EntityRef entity, Bot* bot)
        {
            PlayerRef self = frame.Get<Player>(entity).Ref;
            FPVector3 position = frame.Get<Transform3D>(entity).Position;
            EntityRef own = FindNearestFreeBlock(frame, position, self, true, bot->Home);
            if (own != EntityRef.None || FindLeadingRivalTop(frame, self) == EntityRef.None)
                return own;

            return FindNearestFreeBlock(frame, position, self, false, EntityRef.None);
        }

        /// <summary>
        /// Decides what to do with the block in hand and sets the task: a rival's block is thrown at the leading rival's
        /// tower; the bot's own block goes on its tower (after a Stack dunk, some of the time), or, with no home block
        /// on the ground, is set down to become its home.
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

            if (frame.Get<Block>(held).Owner != self)
            {
                EntityRef rival = FindLeadingRivalTop(frame, self);
                BotSystem.SetTask(frame, bot, config, rival != EntityRef.None ? BotTaskType.ThrowAt : BotTaskType.Drop,
                    rival, default);
                return;
            }

            EntityRef top = FindHomeTop(frame, entity, bot, held);
            if (top == EntityRef.None)
            {
                BotSystem.SetTask(frame, bot, config, BotTaskType.Drop, EntityRef.None, default);
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
                bot->Home = FindNearestFreeBlock(frame, frame.Get<Transform3D>(entity).Position, self, true, held);
            }

            return bot->Home;
        }

        private static bool IsUsableHome(Frame frame, EntityRef home, PlayerRef self, EntityRef held)
        {
            return home != held && frame.Exists(home) && frame.Get<Block>(home).Owner == self
                && frame.Get<Grabbable>(home).Holder == EntityRef.None
                && frame.Get<Transform3D>(home).Position.Y < FP._0_50 + FP._0_10;
        }

        /// <summary>
        /// Returns the top of the tallest tower of the rival with the most counted blocks, or none.
        /// </summary>
        private static EntityRef FindLeadingRivalTop(Frame frame, PlayerRef self)
        {
            EntityRef best = EntityRef.None;
            int bestCount = -1;
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
        /// Returns the nearest loose block on the ground with nothing on it, nobody holding it, owned by
        /// <paramref name="self"/> (or, when <paramref name="own"/> is false, by someone else), other than
        /// <paramref name="exclude"/>.
        /// </summary>
        private static EntityRef FindNearestFreeBlock(Frame frame, FPVector3 position, PlayerRef self, bool own,
            EntityRef exclude)
        {
            EntityRef best = EntityRef.None;
            FP bestDistance = FP.UseableMax;
            var blocks = frame.Filter<Block, Transform3D, Grabbable>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block, out Transform3D* transform, out Grabbable* grabbable))
            {
                if (entity == exclude || grabbable->Holder != EntityRef.None || block->Stack != StackState.Loose
                    || block->HasAbove || (block->Owner == self) != own || transform->Position.Y > FP._0_50 + FP._0_10)
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
