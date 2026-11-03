namespace Quantum.Editor
{
    using Circuit.Compiler;
    using UnityEditor;

    /// <summary>
    /// Defines the function used to retrieve the Quantum deterministic Guid using <see cref="QuantumUnityDBUtilities"/> API. Needed for generating asset objects' guids when creating new assets on the Bot SDK Compiler.
    /// </summary>
    [InitializeOnLoad]
    public static class BotSDKDeterministicGuidGetter
    {
        static BotSDKDeterministicGuidGetter()
        {
            CompilerHelpers.GetDeterministicGuid = (AssetObject ao) => GetDeterministicGuid(ao);
        }

        private static AssetGuid GetDeterministicGuid(AssetObject assetObject)
        {
            return QuantumUnityDBUtilities.CreateDeterministicAssetGuid(assetObject);
        }
    }
}
