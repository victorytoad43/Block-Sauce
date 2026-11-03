using System;

namespace Quantum
{
    [Serializable]
    [BotSDKTooltip(Text = "Logs a message to the console. \nThe message always include a header with the frame number and if it is a Verified frame.")]
    public unsafe partial class DebugLeaf : BTLeaf
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKTooltip(Text = "The text to be logged into the console.")]
        public string Message;

        // ========== BTNode INTERFACE ================================================================================

        /// <summary>
        /// When Update is called, we just write a message on the console.
        /// This Leaf never fails, nor takes more than one frame to finish,
        /// so we always return Success.
        /// </summary>
        protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            Log.Info($"{Message}");

            return BTStatus.Success;
        }
    }
}
