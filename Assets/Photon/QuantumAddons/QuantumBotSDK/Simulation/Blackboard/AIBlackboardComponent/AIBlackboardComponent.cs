using Quantum.BotSDK;
using Quantum.Collections;
using Quantum.Core;

namespace Quantum
{
    public unsafe partial struct AIBlackboardComponent
    {
        #region Init/Free
        public void Initialize(FrameBase frame, AIBlackboard blackboardAsset)
        {
            Board = blackboardAsset;

            var assetEntries = blackboardAsset.Entries;

            if (Entries.Ptr != Ptr.Null)
            {
                Free(frame);
            }

            QDictionary<int, BlackboardEntry> entries = frame.AllocateDictionary<int, BlackboardEntry>(blackboardAsset.Entries.Length);

            for (int i = 0; i < assetEntries.Length; i++)
            {
                BlackboardValue newValue = CreateValueFromEntry(assetEntries[i]);
                entries.Add(assetEntries[i].Key.Key.GetHashCodeDeterministic(), new BlackboardEntry { Value = newValue });
            }

            Entries = entries;
        }

        public void Free(FrameBase frame)
        {
            if (Entries.Ptr != Ptr.Null)
            {
                // Free all reactive decorators - behaviour tree specific
                var entries = frame.ResolveDictionary(Entries);
                foreach (var kvp in entries)
                {
                    if (kvp.Value.ReactiveDecorators.Ptr != Ptr.Null)
                    {
                        frame.FreeList(kvp.Value.ReactiveDecorators);
                    }
                }

                frame.FreeDictionary(Entries);

                Entries = default;
            }
        }

        private BlackboardValue CreateValueFromEntry(AIBlackboardEntry entry)
        {
            BlackboardValue newValue = new BlackboardValue();

            if (entry.Type == AIBlackboardValueType.Boolean)
            {
                *newValue.BooleanValue = default;
            }

            if (entry.Type == AIBlackboardValueType.Byte)
            {
                *newValue.ByteValue = default;
            }

            if (entry.Type == AIBlackboardValueType.Integer)
            {
                *newValue.IntegerValue = default;
            }

            if (entry.Type == AIBlackboardValueType.FP)
            {
                *newValue.FPValue = default;
            }

            if (entry.Type == AIBlackboardValueType.Vector2)
            {
                *newValue.FPVector2Value = default;
            }

            if (entry.Type == AIBlackboardValueType.Vector3)
            {
                *newValue.FPVector3Value = default;
            }

            if (entry.Type == AIBlackboardValueType.EntityRef)
            {
                *newValue.EntityRefValue = default;
            }

            if (entry.Type == AIBlackboardValueType.AssetRef)
            {
                *newValue.AssetRefValue = default;
            }

            return newValue;
        }
        #endregion

        #region BT Specific
        public void RegisterReactiveDecorator(FrameBase frame, string key, BTDecorator decorator)
        {
            var blackboardEntry = GetBlackboardEntry(frame, key);

            QList<AssetRef<BTDecorator>> reactiveDecorators;
            if (blackboardEntry->ReactiveDecorators.Ptr == Ptr.Null)
            {
                reactiveDecorators = frame.AllocateList<AssetRef<BTDecorator>>();
            }
            else
            {
                reactiveDecorators = frame.ResolveList<AssetRef<BTDecorator>>(blackboardEntry->ReactiveDecorators);
            }
            reactiveDecorators.Add(decorator);

            blackboardEntry->ReactiveDecorators = reactiveDecorators;
        }

        public void RegisterReactiveDecorator(FrameThreadSafe frame, string key, BTDecorator decorator)
        {
            var blackboardEntry = GetBlackboardEntry(frame, key);

            QList<AssetRef<BTDecorator>> reactiveDecorators;
            if (blackboardEntry->ReactiveDecorators.Ptr == Ptr.Null)
            {
                Log.Warn($"[Bot SDK] Trying to register Reactive Decorator with FrameThreadSafe, but the reactive decorators list is not allocated. Try calling \"AllocateReactiveDecorators\" before.");
            }
            else
            {
                reactiveDecorators = frame.ResolveList<AssetRef<BTDecorator>>(blackboardEntry->ReactiveDecorators);
            }
            reactiveDecorators.Add(decorator);

            blackboardEntry->ReactiveDecorators = reactiveDecorators;
        }

        public void AllocateReactiveDecorators(FrameBase frame)
        {
            var entries = frame.ResolveDictionary(Entries);
            foreach (var e in entries)
            {
                QList<AssetRef<BTDecorator>> reactiveDecorators;
                if (e.Value.ReactiveDecorators.Ptr == Ptr.Null)
                {
                    reactiveDecorators = frame.AllocateList<AssetRef<BTDecorator>>();

                    entries.TryGetValuePointer(e.Key, out var entry);
                    entry->ReactiveDecorators = reactiveDecorators;
                }
            }
        }

        public void UnregisterReactiveDecorator(FrameBase frame, string key, BTDecorator decorator)
        {
            var blackboardEntry = GetBlackboardEntry(frame, key);

            if (blackboardEntry->ReactiveDecorators.Ptr != Ptr.Null)
            {
                QList<AssetRef<BTDecorator>> reactiveDecorators = frame.ResolveList<AssetRef<BTDecorator>>(blackboardEntry->ReactiveDecorators);
                reactiveDecorators.Remove(decorator);
                blackboardEntry->ReactiveDecorators = reactiveDecorators;
            }
        }

        public void UnregisterReactiveDecorator(FrameThreadSafe frame, string key, BTDecorator decorator)
        {
            var blackboardEntry = GetBlackboardEntry(frame, key);

            if (blackboardEntry->ReactiveDecorators.Ptr != Ptr.Null)
            {
                QList<AssetRef<BTDecorator>> reactiveDecorators = frame.ResolveList<AssetRef<BTDecorator>>(blackboardEntry->ReactiveDecorators);
                reactiveDecorators.Remove(decorator);
                blackboardEntry->ReactiveDecorators = reactiveDecorators;
            }
        }
        #endregion

        #region Debug
        public void Dump(FrameBase frame)
        {
            string dumpText = "";
            var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
            dumpText += "Blackboard Path and ID: " + bbAsset.Path + "  |  " + Board.Id.Value;

            var valuesList = frame.ResolveDictionary(Entries);
            for (byte i = 0; i < valuesList.Count; i++)
            {
                string value = "NONE";
                if (valuesList[i].Value.Field == BlackboardValue.BOOLEANVALUE) value = valuesList[i].Value.BooleanValue->Value.ToString();
                if (valuesList[i].Value.Field == BlackboardValue.BYTEVALUE) value = valuesList[i].Value.ByteValue->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.INTEGERVALUE) value = valuesList[i].Value.IntegerValue->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.FPVALUE) value = valuesList[i].Value.FPValue->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.FPVECTOR2VALUE) value = valuesList[i].Value.FPVector2Value->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.FPVECTOR3VALUE) value = valuesList[i].Value.FPVector3Value->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.ENTITYREFVALUE) value = valuesList[i].Value.EntityRefValue->ToString();
                if (valuesList[i].Value.Field == BlackboardValue.ASSETREFVALUE) value = valuesList[i].Value.AssetRefValue->ToString();

                dumpText += "\nName: " + bbAsset.GetEntryName(i) + ", Value: " + value;
            }

            Log.Info(dumpText);
        }
        #endregion
    }
}
