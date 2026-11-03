namespace Quantum.BotSDK
{
    public static class HashCodeUtilities
    {
        public static unsafe int GetHashCodeDeterministic(this string data, int initialHash = 0)
        {
            int hash = initialHash;
            for (var i = 0; i < data.Length; ++i)
            {
                hash = hash * 31 + data[i];
            }
            return hash;
        }
    }
}
