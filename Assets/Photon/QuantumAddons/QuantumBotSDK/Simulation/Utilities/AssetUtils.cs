namespace Quantum.BotSDK
{
    public static class AssetUtils
    {
        public static string AssetPathToName(string path)
        {
            if (string.IsNullOrEmpty(path) == true)
            {
                return null;
            }

            var splitPath = path.Split('/');
            return splitPath[splitPath.Length - 1];
        }
    }
}
