using System;

namespace Quantum
{
    [Serializable]
    [BotSDKTooltip(Text = "Logs a message to the console. \nThe message always include a header with the frame number and if it is a Verified frame.")]
    public unsafe partial class DebugService : BTService
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKTooltip(Text = "The text to be logged into the console.")]
        public string Message;

        // ========== BTService INTERFACE =============================================================================

        protected unsafe override void OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            Log.Info($"[Frame {btParams.Frame.Number} - IsVerified? {btParams.Frame.IsVerified}] {Message}");
        }
    }
}
