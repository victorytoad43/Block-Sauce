namespace Quantum
{
    [System.Serializable]
    [BotSDKTooltip(Text = "Logs a message to the console. \nThe message always include a header with the frame number and if it is a Verified frame.")]
    public unsafe class DebugAction : AIAction
    {
        [BotSDKTooltip(Text = "The text to be logged into the console.")]
        public string Message;

        public override void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            Log.Info($"[Frame {frame.Number} - IsVerified? {frame.IsVerified}] {Message}");
        }
    }
}
