namespace Quantum
{
    /// <summary>
    /// Queries about a player's stacks, as worked out each tick by <see cref="BlockStackSystem"/>. A player may have
    /// more than one stack; their tower is the stack with the most blocks.
    /// </summary>
    public static unsafe class BlockTower
    {
        /// <summary>
        /// Returns the ground block of the player's tallest stack (counting every block in it, counted or not), or none.
        /// </summary>
        public static EntityRef GetBestBase(Frame frame, PlayerRef owner)
        {
            EntityRef best = EntityRef.None;
            int bestHeight = 0;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef _, out Block* block))
            {
                if (block->Owner != owner || block->StackLevel == BlockStackSystem.NotStacked
                    || block->StackLevel + 1 <= bestHeight)
                    continue;

                bestHeight = block->StackLevel + 1;
                best = block->Base;
            }

            return best;
        }

        /// <summary>
        /// Returns how many counted blocks the player's best stack holds: the most counted blocks sharing one base.
        /// </summary>
        public static int CountBest(Frame frame, PlayerRef owner)
        {
            int best = 0;
            var bases = frame.Filter<Block>();
            while (bases.NextUnsafe(out EntityRef baseEntity, out Block* baseBlock))
            {
                if (baseBlock->Owner != owner || baseBlock->StackLevel != 0)
                    continue;

                int count = 0;
                var blocks = frame.Filter<Block>();
                while (blocks.NextUnsafe(out EntityRef _, out Block* block))
                {
                    if (block->Base == baseEntity && block->Stack == StackState.Counted)
                        count++;
                }

                if (count > best)
                    best = count;
            }

            return best;
        }

        /// <summary>
        /// Returns the highest block of the stack on <paramref name="baseEntity"/>, or none.
        /// </summary>
        public static EntityRef GetTop(Frame frame, EntityRef baseEntity)
        {
            EntityRef top = EntityRef.None;
            int topLevel = -1;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (block->Base == baseEntity && baseEntity != EntityRef.None && block->StackLevel > topLevel)
                {
                    top = entity;
                    topLevel = block->StackLevel;
                }
            }

            return top;
        }

        /// <summary>
        /// Finds the player's block at <paramref name="level"/> of the stack on <paramref name="baseEntity"/>.
        /// </summary>
        public static bool TryGetAtLevel(Frame frame, EntityRef baseEntity, int level, out Block found)
        {
            var blocks = frame.Filter<Block>();
            while (blocks.Next(out EntityRef _, out Block block))
            {
                if (baseEntity != EntityRef.None && block.Base == baseEntity && block.StackLevel == level)
                {
                    found = block;
                    return true;
                }
            }

            found = default;
            return false;
        }
    }
}
