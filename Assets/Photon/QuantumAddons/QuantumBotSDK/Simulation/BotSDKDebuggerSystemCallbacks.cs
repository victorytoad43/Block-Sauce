using Quantum.Core;
using System;

namespace Quantum
{
    /// <summary>
    /// Callbacks used to send simulation data to the Visual Editor for debugging purposes.
    /// </summary>
    public static class BotSDKDebuggerSystemCallbacks
    {
        // Used for DEBUGGING purposes only
        public static Action<FrameBase> OnVerifiedFrame;
        public static Action<FrameBase, EntityRef, string, int, Func<DelegateGetDebugInfo>> SetEntityDebugLabel;
        public static Action<FrameBase, EntityRef, int> RemoveEntityFromDebugger;
    }
}
