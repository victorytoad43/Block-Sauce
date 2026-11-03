using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
    public partial class AIConfig : AssetObject
    {
        public enum EValueType
        {
            None,
            Int,
            Bool,
            Byte,
            FP,
            FPVector2,
            FPVector3,
            String,
            EntityRef,
            AssetRef
        }

        [Serializable]
        public class KeyValuePair
        {
            public string Key;
            public EValueType Type;
            public Value Value;
        }

        [Serializable]
        public struct Value
        {
            public Int32 Integer;
            public Boolean Boolean;
            public Byte Byte;
            public FP FP;
            public FPVector2 FPVector2;
            public FPVector3 FPVector3;
            public string String;
            public EntityRef EntityRef;
            public AssetRef AssetRef;
        }

        public int Count { get { return KeyValuePairs.Count; } }

        public AssetRef<AIConfig> DefaultConfig;
        public List<KeyValuePair> KeyValuePairs = new List<KeyValuePair>(32);
        [NonSerialized] private Dictionary<string, KeyValuePair> _stringToKvp;

        public override void Loaded(IResourceManager resourceManager)
        {
            _stringToKvp = new Dictionary<string, KeyValuePair>(KeyValuePairs.Count);
            for (int i = 0; i < KeyValuePairs.Count; i++)
            {
                var kvp = KeyValuePairs[i];
                _stringToKvp.Add(kvp.Key, kvp);
            }
        }

        public KeyValuePair Get(string key)
        {
            if (_stringToKvp == null)
            {
                return FallbackGet(key);
            }

            if (_stringToKvp.TryGetValue(key, out var kvp) == true)
            {
                return kvp;
            }

            return null;
        }

        private KeyValuePair FallbackGet(string key)
        {
            for (int i = 0; i < KeyValuePairs.Count; i++)
            {
                if (KeyValuePairs[i].Key == key)
                    return KeyValuePairs[i];
            }

            return null;
        }

        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                Log.Info($"[Bot SDK Simulation] Tried to set new variable in AI config but its key is null.");
                return;
            }

            KeyValuePair pair = Get(key);

            if (pair == null)
            {
                pair = new KeyValuePair();
                pair.Key = key;
                KeyValuePairs.Add(pair);
            }

            Set(pair, value);
        }

        private void Set<T>(KeyValuePair pair, T value)
        {
            if (value is int intValue)
            {
                pair.Type = EValueType.Int;
                pair.Value.Integer = intValue;
            }
            else if (value is bool boolValue)
            {
                pair.Type = EValueType.Bool;
                pair.Value.Boolean = boolValue;
            }
            else if (value is Byte byteValue)
            {
                pair.Type = EValueType.Byte;
                pair.Value.Byte = byteValue;
            }
            else if (value is FP fpValue)
            {
                pair.Type = EValueType.FP;
                pair.Value.FP = fpValue;
            }
            else if (value is FPVector2 fpVector2Value)
            {
                pair.Type = EValueType.FPVector2;
                pair.Value.FPVector2 = fpVector2Value;
            }
            else if (value is FPVector3 fpVector3Value)
            {
                pair.Type = EValueType.FPVector3;
                pair.Value.FPVector3 = fpVector3Value;
            }
            else if (value is string stringValue)
            {
                pair.Type = EValueType.String;
                pair.Value.String = stringValue;
            }
            else if (value is EntityRef entityRefValue)
            {
                pair.Type = EValueType.EntityRef;
                pair.Value.EntityRef = entityRefValue;
            }
            else if (value is AssetRef assetRefValue)
            {
                pair.Type = EValueType.AssetRef;
                pair.Value.AssetRef = assetRefValue;
            }
            else if (value is AssetObject assetObjectValue)
            {
                pair.Type = EValueType.AssetRef;
                pair.Value.AssetRef = assetObjectValue;
            }
            else
            {
                throw new NotSupportedException(string.Format("AIConfig - Type not supported. Type: {0} Key: {1}", typeof(T), pair.Key));
            }
        }
    }
}
