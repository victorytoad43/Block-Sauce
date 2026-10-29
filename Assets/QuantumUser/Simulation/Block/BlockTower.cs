namespace Quantum
{
    /// <summary>
    /// Queries about a player's tower: the blocks they own that are locked, stacked from level 0 upward.
    /// </summary>
    public static unsafe class BlockTower
    {
        /// <summary>
        /// Returns how many of the player's blocks are locked.
        /// </summary>
        public static int CountLocked(Frame frame, PlayerRef owner)
        {
            int count = 0;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef _, out Block* block))
            {
                if (block->Owner == owner && block->Lock == LockState.Locked)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Returns the player's locked block at the top of their tower, or none.
        /// </summary>
        public static EntityRef GetTop(Frame frame, PlayerRef owner)
        {
            EntityRef top = EntityRef.None;
            int topLevel = -1;
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (block->Owner == owner && block->Lock == LockState.Locked && block->TowerLevel > topLevel)
                {
                    top = entity;
                    topLevel = block->TowerLevel;
                }
            }

            return top;
        }

        /// <summary>
        /// Returns whether another of the player's blocks, other than <paramref name="except"/>, is locking.
        /// Only one block per player locks at a time, so a player builds one tower.
        /// </summary>
        public static bool IsOtherLocking(Frame frame, PlayerRef owner, EntityRef except)
        {
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (entity != except && block->Owner == owner && block->Lock == LockState.Locking)
                    return true;
            }

            return false;
        }
    }
}
