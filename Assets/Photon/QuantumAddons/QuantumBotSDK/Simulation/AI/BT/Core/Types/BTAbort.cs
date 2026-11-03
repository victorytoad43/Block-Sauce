namespace Quantum
{
    // The Abort mechanism works in two ways:

    // Abort Self:
    // The current branch execution needs to be interrputed. For this, the Abort node Id is
    // cached into the Agent component
    // When that is activated, "Is Aborting" returns true. When it happens, the BT pipeline
    // reacts accordingly by not letting the execution continue until that node is reached
    // when bubbling up in the tree
    // Nodes start returning Abort and have their Status marked as Abort
    // Composite nodes then treats these Abort results the same as Failures, on the child completion callback

    // Abort Lower Priority:
    // The sibling nodes from the node that caused the Abort are marked with Status = Abort
    // This does NOT make "Is Aborting" return true. Meaning that the current execution is
    // not interrupted at all
    // Then, when a Composite tries to execute the lower priority nodes, those nodes
    // will result right away in Abort, not even entering those nodes
    // The Composites then use such info by considering it the same as a Failure
    // in the OnUpdate

    public enum BTAbort
    {
        None,
        Self,
        LowerPriority,
        Both
    }

    public static class BTAbortExtensions
    {
        // ========== PUBLIC METHODS ==================================================================================

        public static bool IsSelf(this BTAbort abort)
        {
            return abort == BTAbort.Self || abort == BTAbort.Both;
        }

        public static bool IsLowerPriority(this BTAbort abort)
        {
            return abort == BTAbort.LowerPriority || abort == BTAbort.Both;
        }
    }
}
