namespace Quantum
{
    public static class BotSDKCompilerCallbacks
    {
        public static System.Action<string, AssetRef<HFSMRoot>, AssetRef<AIBlackboard>, AssetRef<AIBlackboardInitializer>, AssetRef<AIConfig>> HFSMCompiled;
        public static System.Action<string, AssetRef<BTRoot>, AssetRef<AIBlackboard>, AssetRef<AIBlackboardInitializer>, AssetRef<AIConfig>> BTCompiled;
        public static System.Action<string, AssetRef<UTRoot>, AssetRef<AIBlackboard>, AssetRef<AIBlackboardInitializer>, AssetRef<AIConfig>> UTCompiled;
    }
}
