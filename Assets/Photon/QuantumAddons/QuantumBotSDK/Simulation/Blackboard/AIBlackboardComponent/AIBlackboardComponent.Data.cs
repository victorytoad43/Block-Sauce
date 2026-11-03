using Photon.Deterministic;
using Quantum.BotSDK;
using Quantum.Core;
using System;

namespace Quantum
{
    public unsafe partial struct AIBlackboardComponent
    {
        // TODO: look into codegen to create these methods
        // TODO: consider having the actual implementation only on the FTS versions and have the non-FTS overload just calling that. Need to test performance

        #region Private Helpers

        private static void AssertValidKey(string key)
        {
            Assert.Check(string.IsNullOrEmpty(key) == false, "Blackboard key cannot be null.");
        }

        private static int GetKeyHash(string key)
        {
            return key.GetHashCodeDeterministic();
        }

        private bool TryGetEntry(FrameThreadSafe frame, int hashCode, out BlackboardEntry* entry)
        {
            if (hashCode == 0)
            {
                entry = null;
                return false;
            }

            var entries = frame.ResolveDictionary(Entries);
            return entries.TryGetValuePointer(hashCode, out entry);
        }

        private static void LogMissingEntry(string key)
        {
            Log.Error($"[Bot SDK] Blackboard entry with name {key} was not found");
        }

        private BlackboardEntry* SetBooleanInternal(FrameThreadSafe frame, int hashCode, QBoolean value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.BooleanValue = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetByteInternal(FrameThreadSafe frame, int hashCode, byte value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.ByteValue = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetIntegerInternal(FrameThreadSafe frame, int hashCode, int value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.IntegerValue = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetFPInternal(FrameThreadSafe frame, int hashCode, FP value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.FPValue = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetVector2Internal(FrameThreadSafe frame, int hashCode, FPVector2 value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.FPVector2Value = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetVector3Internal(FrameThreadSafe frame, int hashCode, FPVector3 value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.FPVector3Value = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetEntityRefInternal(FrameThreadSafe frame, int hashCode, EntityRef value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.EntityRefValue = value;
                return entry;
            }

            return null;
        }

        private BlackboardEntry* SetAssetRefInternal(FrameThreadSafe frame, int hashCode, AssetRef value)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                *entry->Value.AssetRefValue = value;
                return entry;
            }

            return null;
        }

        #endregion

        #region FrameBase Getters

        // QBoolean
        public QBoolean GetBoolean(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetBoolean((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public QBoolean GetBoolean(FrameBase frame, int hashCode)
        {
            return GetBoolean((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetBoolean(FrameBase frame, string key, out QBoolean boolean)
        {
            AssertValidKey(key);
            return TryGetBoolean((FrameThreadSafe)frame, GetKeyHash(key), out boolean);
        }

        public bool TryGetBoolean(FrameBase frame, int hashCode, out QBoolean boolean)
        {
            return TryGetBoolean((FrameThreadSafe)frame, hashCode, out boolean);
        }

        // Byte
        public byte GetByte(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetByte((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public byte GetByte(FrameBase frame, int hashCode)
        {
            return GetByte((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetByte(FrameBase frame, string key, out byte value)
        {
            AssertValidKey(key);
            return TryGetByte((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetByte(FrameBase frame, int hashCode, out byte value)
        {
            return TryGetByte((FrameThreadSafe)frame, hashCode, out value);
        }

        // Integer
        public Int32 GetInteger(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetInteger((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public Int32 GetInteger(FrameBase frame, int hashCode)
        {
            return GetInteger((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetInteger(FrameBase frame, string key, out int value)
        {
            AssertValidKey(key);
            return TryGetInteger((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetInteger(FrameBase frame, int hashCode, out int value)
        {
            return TryGetInteger((FrameThreadSafe)frame, hashCode, out value);
        }

        // FP
        public FP GetFP(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetFP((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public FP GetFP(FrameBase frame, int hashCode)
        {
            return GetFP((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetFP(FrameBase frame, string key, out FP value)
        {
            AssertValidKey(key);
            return TryGetFP((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetFP(FrameBase frame, int hashCode, out FP value)
        {
            return TryGetFP((FrameThreadSafe)frame, hashCode, out value);
        }

        // FPVector2
        public FPVector2 GetVector2(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetVector2((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public FPVector2 GetVector2(FrameBase frame, int hashCode)
        {
            return GetVector2((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetBoolean(FrameBase frame, string key, out FPVector2 value)
        {
            AssertValidKey(key);
            return TryGetBoolean((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetBoolean(FrameBase frame, int hashCode, out FPVector2 value)
        {
            return TryGetBoolean((FrameThreadSafe)frame, hashCode, out value);
        }

        // FPVector3
        public FPVector3 GetVector3(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetVector3((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public FPVector3 GetVector3(FrameBase frame, int hashCode)
        {
            return GetVector3((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetVector3(FrameBase frame, string key, out FPVector3 value)
        {
            AssertValidKey(key);
            return TryGetVector3((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetVector3(FrameBase frame, int hashCode, out FPVector3 value)
        {
            return TryGetVector3((FrameThreadSafe)frame, hashCode, out value);
        }

        // EntityRef
        public EntityRef GetEntityRef(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetEntityRef((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public EntityRef GetEntityRef(FrameBase frame, int hashCode)
        {
            return GetEntityRef((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetEntityRef(FrameBase frame, string key, out EntityRef value)
        {
            AssertValidKey(key);
            return TryGetEntityRef((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetEntityRef(FrameBase frame, int hashCode, out EntityRef value)
        {
            return TryGetEntityRef((FrameThreadSafe)frame, hashCode, out value);
        }

        // AssetRef
        public AssetRef GetAssetRef(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetAssetRef((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public AssetRef GetAssetRef(FrameBase frame, int hashCode)
        {
            return GetAssetRef((FrameThreadSafe)frame, hashCode);
        }

        public bool TryGetAssetRef(FrameBase frame, string key, out AssetRef value)
        {
            AssertValidKey(key);
            return TryGetAssetRef((FrameThreadSafe)frame, GetKeyHash(key), out value);
        }

        public bool TryGetAssetRef(FrameBase frame, int hashCode, out AssetRef value)
        {
            return TryGetAssetRef((FrameThreadSafe)frame, hashCode, out value);
        }

        public bool TryGetID(FrameBase frame, string key, out Int32 id)
        {
            AssertValidKey(key);
            return TryGetID((FrameThreadSafe)frame, key, out id);
        }

        public bool TryGetID(FrameBase frame, int hashCode, out Int32 id)
        {
            return TryGetID((FrameThreadSafe)frame, hashCode, out id);
        }

        #endregion

        #region FrameBase Setters

        public BlackboardEntry* Set(FrameBase frame, string key, QBoolean value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, QBoolean value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, byte value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, byte value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, Int32 value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, Int32 value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, FP value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, FP value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, FPVector2 value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, FPVector2 value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, FPVector3 value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, FPVector3 value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, EntityRef value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, EntityRef value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameBase frame, string key, AssetRef value)
        {
            AssertValidKey(key);

            var entry = Set((FrameThreadSafe)frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameBase frame, int hashCode, AssetRef value)
        {
            return Set((FrameThreadSafe)frame, hashCode, value);
        }

        #endregion

        #region FrameBase Helpers

        public BlackboardEntry* GetBlackboardEntry(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetBlackboardEntry((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public BlackboardEntry* GetBlackboardEntry(FrameBase frame, int hashCode)
        {
            return GetBlackboardEntry((FrameThreadSafe)frame, hashCode);
        }

        public BlackboardValue GetBlackboardValue(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return GetBlackboardValue((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public BlackboardValue GetBlackboardValue(FrameBase frame, int hashCode)
        {
            return GetBlackboardValue((FrameThreadSafe)frame, hashCode);
        }

        /// <summary>
        /// Tries to get the blackboard value for the specified key.
        /// Returns false if the key is null, empty, or not found.
        /// </summary>
        public bool TryGetBlackboardValue(FrameBase frame, string key, out BlackboardValue bbValue)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                bbValue = default;
                Log.Warn("[Bot SDK] Tried get Blackboard value but the provided key is empty.");
                return false;
            }

            return TryGetBlackboardValue((FrameThreadSafe)frame, GetKeyHash(key), out bbValue);
        }

        public bool TryGetBlackboardValue(FrameBase frame, int hashCode, out BlackboardValue bbValue)
        {
            return TryGetBlackboardValue((FrameThreadSafe)frame, hashCode, out bbValue);
        }

        public bool HasEntry(FrameBase frame, string key)
        {
            AssertValidKey(key);
            return HasEntry((FrameThreadSafe)frame, GetKeyHash(key));
        }

        public bool HasEntry(FrameBase frame, int hashCode)
        {
            return HasEntry((FrameThreadSafe)frame, hashCode);
        }

        #endregion

        // -- THREADSAFE

        #region FrameThreadSafe Getters

        // QBoolean
        public QBoolean GetBoolean(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetBoolean(frame, GetKeyHash(key));
        }

        public QBoolean GetBoolean(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.BooleanValue;
        }

        public bool TryGetBoolean(FrameThreadSafe frame, string key, out QBoolean boolean)
        {
            AssertValidKey(key);
            return TryGetBoolean(frame, GetKeyHash(key), out boolean);
        }

        public bool TryGetBoolean(FrameThreadSafe frame, int hashCode, out QBoolean boolean)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.BOOLEANVALUE)
            {
                boolean = *bbValue.BooleanValue;
                return true;
            }

            boolean = default;
            return false;
        }

        // Byte
        public byte GetByte(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetByte(frame, GetKeyHash(key));
        }

        public byte GetByte(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.ByteValue;
        }

        public bool TryGetByte(FrameThreadSafe frame, string key, out byte value)
        {
            AssertValidKey(key);
            return TryGetByte(frame, GetKeyHash(key), out value);
        }

        public bool TryGetByte(FrameThreadSafe frame, int hashCode, out byte value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.BYTEVALUE)
            {
                value = *bbValue.ByteValue;
                return true;
            }

            value = default;
            return false;
        }

        // Integer
        public Int32 GetInteger(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetInteger(frame, GetKeyHash(key));
        }

        public Int32 GetInteger(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.IntegerValue;
        }

        public bool TryGetInteger(FrameThreadSafe frame, string key, out int value)
        {
            AssertValidKey(key);
            return TryGetInteger(frame, GetKeyHash(key), out value);
        }

        public bool TryGetInteger(FrameThreadSafe frame, int hashCode, out int value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.INTEGERVALUE)
            {
                value = *bbValue.IntegerValue;
                return true;
            }

            value = default;
            return false;
        }

        // FP
        public FP GetFP(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetFP(frame, GetKeyHash(key));
        }

        public FP GetFP(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.FPValue;
        }

        public bool TryGetFP(FrameThreadSafe frame, string key, out FP value)
        {
            AssertValidKey(key);
            return TryGetFP(frame, GetKeyHash(key), out value);
        }

        public bool TryGetFP(FrameThreadSafe frame, int hashCode, out FP value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.FPVALUE)
            {
                value = *bbValue.FPValue;
                return true;
            }

            value = default;
            return false;
        }

        // FPVector2
        public FPVector2 GetVector2(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetVector2(frame, GetKeyHash(key));
        }

        public FPVector2 GetVector2(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.FPVector2Value;
        }

        public bool TryGetBoolean(FrameThreadSafe frame, string key, out FPVector2 value)
        {
            AssertValidKey(key);
            return TryGetBoolean(frame, GetKeyHash(key), out value);
        }

        public bool TryGetBoolean(FrameThreadSafe frame, int hashCode, out FPVector2 value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.FPVECTOR2VALUE)
            {
                value = *bbValue.FPVector2Value;
                return true;
            }

            value = default;
            return false;
        }

        // FPVector3
        public FPVector3 GetVector3(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetVector3(frame, GetKeyHash(key));
        }

        public FPVector3 GetVector3(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.FPVector3Value;
        }

        public bool TryGetVector3(FrameThreadSafe frame, string key, out FPVector3 value)
        {
            AssertValidKey(key);
            return TryGetVector3(frame, GetKeyHash(key), out value);
        }

        public bool TryGetVector3(FrameThreadSafe frame, int hashCode, out FPVector3 value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.FPVECTOR3VALUE)
            {
                value = *bbValue.FPVector3Value;
                return true;
            }

            value = default;
            return false;
        }

        // EntityRef
        public EntityRef GetEntityRef(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetEntityRef(frame, GetKeyHash(key));
        }

        public EntityRef GetEntityRef(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.EntityRefValue;
        }

        public bool TryGetEntityRef(FrameThreadSafe frame, string key, out EntityRef value)
        {
            AssertValidKey(key);
            return TryGetEntityRef(frame, GetKeyHash(key), out value);
        }

        public bool TryGetEntityRef(FrameThreadSafe frame, int hashCode, out EntityRef value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.ENTITYREFVALUE)
            {
                value = *bbValue.EntityRefValue;
                return true;
            }

            value = default;
            return false;
        }

        // AssetRef
        public AssetRef GetAssetRef(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetAssetRef(frame, GetKeyHash(key));
        }

        public AssetRef GetAssetRef(FrameThreadSafe frame, int hashCode)
        {
            var bbValue = GetBlackboardValue(frame, hashCode);
            return *bbValue.AssetRefValue;
        }

        public bool TryGetAssetRef(FrameThreadSafe frame, string key, out AssetRef value)
        {
            AssertValidKey(key);
            return TryGetAssetRef(frame, GetKeyHash(key), out value);
        }

        public bool TryGetAssetRef(FrameThreadSafe frame, int hashCode, out AssetRef value)
        {
            if (TryGetBlackboardValue(frame, hashCode, out var bbValue) == true && bbValue.Field == BlackboardValue.ASSETREFVALUE)
            {
                value = *bbValue.AssetRefValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetID(FrameThreadSafe frame, string key, out Int32 id)
        {
            AssertValidKey(key);

            var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
            return bbAsset.TryGetEntryID(key, out id);
        }

        public bool TryGetID(FrameThreadSafe frame, int hashCode, out Int32 id)
        {
            if (HasEntry(frame, hashCode) == true)
            {
                id = hashCode;
                return true;
            }

            id = default;
            return false;
        }

        #endregion

        #region FrameThreadSafe Setters

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, QBoolean value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, QBoolean value)
        {
            return SetBooleanInternal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, byte value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, byte value)
        {
            return SetByteInternal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, int value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, int value)
        {
            return SetIntegerInternal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, FP value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, FP value)
        {
            return SetFPInternal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, FPVector2 value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, FPVector2 value)
        {
            return SetVector2Internal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, FPVector3 value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, FPVector3 value)
        {
            return SetVector3Internal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, EntityRef value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, EntityRef value)
        {
            return SetEntityRefInternal(frame, hashCode, value);
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, string key, AssetRef value)
        {
            AssertValidKey(key);

            var entry = Set(frame, GetKeyHash(key), value);
            if (entry == null)
            {
                LogMissingEntry(key);
            }

            return entry;
        }

        public BlackboardEntry* Set(FrameThreadSafe frame, int hashCode, AssetRef value)
        {
            return SetAssetRefInternal(frame, hashCode, value);
        }

        #endregion

        #region FrameThreadSafe Helpers

        public BlackboardEntry* GetBlackboardEntry(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetBlackboardEntry(frame, GetKeyHash(key));
        }

        public BlackboardEntry* GetBlackboardEntry(FrameThreadSafe frame, int hashCode)
        {
            TryGetEntry(frame, hashCode, out var entry);
            return entry;
        }

        public BlackboardValue GetBlackboardValue(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return GetBlackboardValue(frame, GetKeyHash(key));
        }

        public BlackboardValue GetBlackboardValue(FrameThreadSafe frame, int hashCode)
        {
            var entries = frame.ResolveDictionary(Entries);
            var entry = entries[hashCode];
            return entry.Value;
        }

        public bool TryGetBlackboardValue(FrameThreadSafe frame, string key, out BlackboardValue bbValue)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                bbValue = default;
                Log.Warn("[Bot SDK] Tried get Blackboard value but the provided key is empty.");
                return false;
            }

            return TryGetBlackboardValue(frame, GetKeyHash(key), out bbValue);
        }

        public bool TryGetBlackboardValue(FrameThreadSafe frame, int hashCode, out BlackboardValue bbValue)
        {
            if (TryGetEntry(frame, hashCode, out var entry) == true)
            {
                bbValue = entry->Value;
                return true;
            }

            bbValue = default;
            return false;
        }

        public bool HasEntry(FrameThreadSafe frame, string key)
        {
            AssertValidKey(key);
            return HasEntry(frame, GetKeyHash(key));
        }

        public bool HasEntry(FrameThreadSafe frame, int hashCode)
        {
            if (hashCode == 0)
            {
                return false;
            }

            var entries = frame.ResolveDictionary(Entries);
            return entries.ContainsKey(hashCode);
        }

        #endregion

        [Obsolete("Use Free instead")]
        public void FreeBlackboardComponent(FrameBase frame)
        {
            Free(frame);
        }
    }
}