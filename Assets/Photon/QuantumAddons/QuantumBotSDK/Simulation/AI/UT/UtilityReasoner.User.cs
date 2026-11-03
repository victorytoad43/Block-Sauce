using Photon.Deterministic;
using Quantum.Collections;
using Quantum.Core;

namespace Quantum
{
    public unsafe partial struct UtilityReasoner : IConsiderationProvider
    {
        // ========== PUBLIC METHODS ==================================================================================

        public AssetRef<Consideration> GetConsideration(QList<AssetRef<Consideration>> sourceList, int id)
        {
            return sourceList[id];
        }

        public int Count(QList<AssetRef<Consideration>> sourceList)
        {
            return sourceList.Count;
        }

        public void Initialize(FrameBase frame, AssetRef<UTRoot> utRootRef, EntityRef entity)
        {
            // If we don't receive the UTRoot as parameter, we try to find it on the component itself
            // Useful for pre-setting the UTRoot on a Prototype
            UTRoot utRootInstance;
            if (utRootRef == default)
            {
                utRootInstance = frame.FindAsset<UTRoot>(UTRoot.Id);
            }
            else
            {
                UTRoot = utRootRef;
                utRootInstance = frame.FindAsset<UTRoot>(utRootRef.Id);
            }

            // Initialize the Reasoner's considerations.
            // Can be useful further when creating dynamically added Considerations to the Agent (runtime)
            QList<AssetRef<Consideration>> considerationsList = frame.AllocateList<AssetRef<Consideration>>();
            for (int i = 0; i < utRootInstance.ConsiderationsRefs.Length; i++)
            {
                considerationsList.Add(utRootInstance.ConsiderationsRefs[i]);
            }
            Considerations = considerationsList;

            HighRankConsiderations = frame.AllocateList<AssetRef<Consideration>>();

            CooldownsDict = frame.AllocateDictionary<long, FP>();

            QList<AssetRef<Consideration>> previousExecution = frame.AllocateList<AssetRef<Consideration>>();
            for (int i = 0; i < 6; i++)
            {
                previousExecution.Add(default);
            }
            PreviousExecution = previousExecution;

            MomentumList = frame.AllocateList<UTMomentumPack>();

            TimeToTick = 1;

            if (entity != default)
            {
                BotSDKEditorEvents.UT.InvokeSetupDebugger(frame, entity, utRootInstance.Path, utRootInstance.Guid.Value);
            }
        }

        public void Free(FrameBase frame)
        {
            UTRoot = default;
            frame.FreeList<AssetRef<Consideration>>(Considerations);
            frame.FreeList<AssetRef<Consideration>>(HighRankConsiderations);
            frame.FreeDictionary<long, FP>(CooldownsDict);
            frame.FreeList<AssetRef<Consideration>>(PreviousExecution);
            frame.FreeList<UTMomentumPack>(MomentumList);
        }

        public void Update(FrameThreadSafe frame, FP deltaTime, UtilityReasoner* reasoner, EntityRef entity, ref AIContext aiContext)
        {
            Assert.Check(reasoner != null, "[Bot SDK] Utility Reasoner is null.");
            Assert.Check(reasoner->UTRoot.IsValid == true, "[Bot SDK] UT Root is not valid.");
            Assert.Check(aiContext.AgentEntity != default, "[Bot SDK] UT Agent entity from AIContext is default.");

            // Make sure the UT Root asset is loaded upon Update so it guarantees that late joiners will load and trigger all Loaded callbacks needed,
            // which then caches references to other assets. The root references all the first level Considerations, which loads the nested Considerations in sequence
            var root = frame.FindAsset<UTRoot>(reasoner->UTRoot.Id);

            TickMomentum(frame, deltaTime, reasoner, entity, ref aiContext);
            TickCooldowns(frame, deltaTime, reasoner, entity, ref aiContext);

            BotSDKEditorEvents.UT.InvokeClearChosenConsiderations(aiContext.AgentEntity, reasoner->UTRoot.Id.Value);

            Consideration chosenConsideration = SelectBestConsideration(frame, this, 1, reasoner, entity, ref aiContext);

            if (chosenConsideration != default)
            {
                chosenConsideration.OnUpdate(frame, reasoner, entity, ref aiContext);
                BotSDKEditorEvents.UT.InvokeConsiderationChosen(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, chosenConsideration.CircuitId);
            }
        }

        private void TickCooldowns(FrameThreadSafe frame, FP deltaTime, UtilityReasoner* reasoner, EntityRef entity, ref AIContext aiContext)
        {
            var cooldowns = frame.ResolveDictionary(CooldownsDict);
            var cooldownsEnumerator = cooldowns.GetEnumerator();

            while (cooldownsEnumerator.MoveNext())
            {
                var current = cooldownsEnumerator.Current;
                var reducedValue = current.Value - deltaTime;

                if (reducedValue <= 0)
                {
                    BotSDKEditorEvents.UT.InvokeCooldownStateChanged(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, frame.FindAsset<Consideration>(current.Key).CircuitId, false);
                    cooldowns.Remove(current.Key);
                }
                else
                {
                    cooldowns[current.Key] = reducedValue;
                }
            }
        }

        public Consideration SelectBestConsideration<T>(FrameThreadSafe frame, T considerationProvider,
            byte depth, UtilityReasoner* reasoner, EntityRef entity, ref AIContext aiContext) where T : IConsiderationProvider
        {
            // Make sure the UT Root asset is loaded
            var root = frame.FindAsset<UTRoot>(reasoner->UTRoot.Id);

            QList<UTMomentumPack> momentumList = frame.ResolveList(MomentumList);

            // We get the Rank of every Consideration Set
            // This "filters" the Considerations with higher absolute utility
            QList<AssetRef<Consideration>> highRankConsiderationsRefs = frame.ResolveList(HighRankConsiderations);
            if (highRankConsiderationsRefs.Count > 0)
            {
                highRankConsiderationsRefs.Clear();
            }

            int highestRank = -1;
            QDictionary<long, FP> cooldowns = frame.ResolveDictionary(CooldownsDict);

            QList<AssetRef<Consideration>> solvedConsiderationsList = frame.ResolveList(Considerations);
            QList<AssetRef<Consideration>> previousExecution = frame.ResolveList(reasoner->PreviousExecution);

            for (int i = 0; i < considerationProvider.Count(solvedConsiderationsList); i++)
            {
                if (considerationProvider.GetConsideration(solvedConsiderationsList, i) == null)
                {
                    break;
                }

                AssetRef<Consideration> considerationRef = considerationProvider.GetConsideration(solvedConsiderationsList, i);
                Consideration consideration = frame.FindAsset<Consideration>(considerationRef.Id);

                // When in cooldown, just skip the consideration ranking
                if (cooldowns.Count > 0 && cooldowns.ContainsKey(considerationRef.Id.Value) == true)
                {
                    BotSDKEditorEvents.UT.InvokeProcessedScore(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, consideration.CircuitId, FP._0);

                    continue;
                }

                // If the Consideration has Momentum, then its Rank is defined by it.
                // Otherwise, we calculate the Rank dynamically.
                // GetMomentum handles UseNestedMomentum transparently.
                int rank;
                var momentum = GetMomentum(frame, momentumList, consideration, previousExecution);
                if (momentum > 0)
                {
                    rank = FPMath.CeilToInt(momentum);
                    BotSDKEditorEvents.UT.InvokeProcessedRank(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, consideration.CircuitId, rank, isInMomentum: true);
                }
                else
                {
                    rank = consideration.GetRank(frame, entity, ref aiContext);
                    BotSDKEditorEvents.UT.InvokeProcessedRank(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, consideration.CircuitId, rank, isInMomentum: false);
                }

                if (rank > highestRank)
                {
                    highestRank = rank;
                    highRankConsiderationsRefs.Clear();
                    highRankConsiderationsRefs.Add(considerationRef);
                }
                else if (highestRank == rank)
                {
                    highRankConsiderationsRefs.Add(considerationRef);
                }
            }

            // Based on the higher rank, we check which Considerations sets have greater utility
            // Then we choose that set this frame
            Consideration chosenConsideration = default;
            FP highestScore = FP.UseableMin;
            for (int i = 0; i < highRankConsiderationsRefs.Count; i++)
            {
                if (highRankConsiderationsRefs[i] == default)
                    continue;

                Consideration consideration = frame.FindAsset<Consideration>(highRankConsiderationsRefs[i].Id);
                FP score = consideration.Score(frame, reasoner, entity, ref aiContext);
                BotSDKEditorEvents.UT.InvokeProcessedScore(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, consideration.CircuitId, score);

                if (highestScore < score)
                {
                    highestScore = score;
                    chosenConsideration = consideration;
                }
            }

            if (chosenConsideration != default)
            {
                // If the winner at this depth changed, the old branch's momentum entries are stale.
                // We use PreviousExecution (which records who ran at each depth last frame) to detect this.
                // When a change is detected we evict all momentum entries at this depth and deeper.
                // This works even when the displaced consideration had no own-momentum entry — its
                // children's entries (sitting at deeper depths) are also cleared.
                int winnerDepthZeroBased = chosenConsideration.Depth - 1;
                if (winnerDepthZeroBased < previousExecution.Count
                    && previousExecution[winnerDepthZeroBased] != default
                    && previousExecution[winnerDepthZeroBased] != chosenConsideration)
                {
                    for (int k = momentumList.Count - 1; k >= 0; k--)
                    {
                        Consideration entry = frame.FindAsset<Consideration>(momentumList[k].ConsiderationRef.Id);
                        if (entry.Depth >= chosenConsideration.Depth)
                            momentumList.RemoveAt(k);
                    }
                }

                // Insert own momentum only when the consideration is FRESHLY entering this frame.
                // "Freshly entering" means it was not the winner at this depth last frame.
                // This prevents a parent whose own momentum expired (removed by TickMomentum) from
                // being re-inserted with full fresh momentum every frame it stays winning via nested
                // child momentum. Own momentum is a one-shot grant on first entry; once it decays
                // to zero it is gone. Nested momentum keeps the parent's rank alive independently.
                bool isNewlyChosen = winnerDepthZeroBased >= previousExecution.Count
                    || previousExecution[winnerDepthZeroBased] != chosenConsideration;

                if (chosenConsideration.MomentumData.Value > 0 && isNewlyChosen)
                {
                    InsertMomentum(frame, momentumList, chosenConsideration);
                }

                // If the chosen Consideration has cooldown and it is not yet on the cooldowns dictionary,
                // we add it there
                if (chosenConsideration.Cooldown > 0 && cooldowns.ContainsKey(chosenConsideration.Identifier.Guid.Value) == false)
                {
                    // We don't add Cooldown right away if the Consideration has Momentum, otherwise it is not gonna be effective
                    // In such case, the cooldown is only added when the Consideration is exited
                    bool shouldCooldown;

                    // If there is no Momentum on the Consideration asset, cooldown right away
                    if (chosenConsideration.MomentumData.Value == 0)
                    {
                        shouldCooldown = true;
                    }
                    else
                    {
                        //If there is Momentum on the Consideration asset, then it depends on the cooldown cancelation setup
                        shouldCooldown = chosenConsideration.CooldownCancelsMomentum;
                    }

                    if (shouldCooldown == true)
                    {
                        var considerationGuid = chosenConsideration.Identifier.Guid.Value;
                        cooldowns.Add(considerationGuid, chosenConsideration.Cooldown);
                        BotSDKEditorEvents.UT.InvokeCooldownStateChanged(aiContext.AgentEntity, reasoner->UTRoot.Id.Value, chosenConsideration.CircuitId, true);
                    }
                }

                // Add the chosen consideration to the choices history
                OnConsiderationChosen(frame, reasoner, chosenConsideration, entity, ref aiContext);
            }
            else
            {
                OnNoConsiderationChosen(frame, reasoner, depth, entity, ref aiContext);
            }

            // We return the chosen set so it can be executed
            return chosenConsideration;
        }

        // ========== PRIVATE METHODS =================================================================================

        #region Momentum

        // Returns the effective momentum for a consideration:
        //   - Own momentum: the live decayed value from the consideration's own entry in the list (if any).
        //   - Nested momentum (UseNestedMomentum == true): the greatest live value among children entries
        //     in the list, but only when this consideration is the currently active winner at its depth
        //     (guarded via PreviousExecution so displaced considerations cannot steal children's momentum).
        //   - Effective value = max(own, greatestChild).
        //
        // Crucially, children entries decay at their OWN rate in TickMomentum — the parent never touches
        // them and they never touch the parent's own entry. No cross-decay, no list mutations here.
        // A parent with no own momentum (MomentumData.Value == 0) is never inserted into the list;
        // its effective momentum is computed entirely on the fly from its children's live values.
        private FP GetMomentum(FrameThreadSafe frame, QList<UTMomentumPack> momentumList,
            Consideration consideration, QList<AssetRef<Consideration>> previousExecution)
        {
            FP ownMomentum = 0;
            for (int i = 0; i < momentumList.Count; i++)
            {
                if (momentumList[i].ConsiderationRef == consideration)
                {
                    ownMomentum = momentumList[i].MomentumData.Value;
                    break;
                }
            }

            if (consideration.UseNestedMomentum == false)
            {
                return ownMomentum;
            }

            // Only look at children if this consideration is the active winner at its depth.
            // Without this guard, a consideration that lost last frame would scan the list and
            // potentially steal momentum from the new winner's children.
            int depthZeroBased = consideration.Depth - 1;
            if (depthZeroBased >= previousExecution.Count || previousExecution[depthZeroBased] != consideration)
                return ownMomentum;

            // Scan the list for entries that are children of this consideration (deeper depth).
            // The list is depth-ordered, so all entries deeper than this consideration's depth
            // that remain after the eviction step belong to the current active branch.
            FP greatestChildMomentum = 0;
            for (int i = 0; i < momentumList.Count; i++)
            {
                if (momentumList[i].ConsiderationRef == consideration)
                    continue;
                Consideration entry = frame.FindAsset<Consideration>(momentumList[i].ConsiderationRef.Id);
                if (entry.Depth > consideration.Depth && momentumList[i].MomentumData.Value > greatestChildMomentum)
                    greatestChildMomentum = momentumList[i].MomentumData.Value;
            }
            return FPMath.Max(ownMomentum, greatestChildMomentum);
        }

        private void InsertMomentum(FrameThreadSafe frame, QList<UTMomentumPack> momentumList, AssetRef<Consideration> considerationRef)
        {
            Consideration newConsideration = frame.FindAsset<Consideration>(considerationRef.Id);

            // Replace the entry at the same depth if one exists (same-depth sibling switch).
            // We do NOT match on deeper depths: deeper entries are children that must be left intact.
            // Stale branches are already evicted by the caller before InsertMomentum is called,
            // so the only same-depth entry we could find here is a legitimate replacement target.
            for (int i = 0; i < momentumList.Count; i++)
            {
                Consideration currentConsideration = frame.FindAsset<Consideration>(momentumList[i].ConsiderationRef.Id);
                if (currentConsideration.Depth == newConsideration.Depth)
                {
                    momentumList.GetPointer(i)->ConsiderationRef = considerationRef;
                    momentumList.GetPointer(i)->MomentumData = newConsideration.MomentumData;
                    return;
                }
            }

            // No same-depth entry found — append. The list is depth-ordered (parents before children)
            // and the caller guarantees stale entries have been evicted, so appending always produces
            // a correctly ordered list (a parent is always inserted before its children are added).
            momentumList.Add(new UTMomentumPack()
            {
                ConsiderationRef = considerationRef,
                MomentumData = newConsideration.MomentumData,
            });
        }

        private void TickMomentum(FrameThreadSafe frame, FP deltaTime, UtilityReasoner* reasoner, EntityRef entity, ref AIContext aiContext)
        {
            QList<UTMomentumPack> momentumList = frame.ResolveList(MomentumList);

            for (int i = momentumList.Count - 1; i >= 0; i--)
            {
                UTMomentumPack* momentum = momentumList.GetPointer(i);

                // If we currently have a commitment, we check if it is done already
                // If it is done, that Consideration's Rank shall be re-calculated
                // If it is not done, then the Consideration's Rank will be kept due to the commitment
                // unless some other Consideration has greater Rank and replaces the current commitment
                Consideration momentumConsideration = frame.FindAsset<Consideration>(momentum->ConsiderationRef.Id);

                if (momentum->MomentumData.Value > 0 && momentumConsideration.MomentumData.DecayAmount > 0)
                {
                    momentum->MomentumData.Value -= momentumConsideration.MomentumData.DecayAmount * deltaTime;
                }

                bool isDone = false;
                if (momentumConsideration.Commitment != default)
                {
                    isDone = momentumConsideration.Commitment.Execute(frame, entity, ref aiContext);
                }

                if (isDone == true || momentum->MomentumData.Value <= 0)
                {
                    momentum->MomentumData.Value = 0;
                    momentumList.RemoveAt(i);

                    if (momentumConsideration.MomentumData.Value > 0 && momentumConsideration.CooldownCancelsMomentum == false)
                    {
                        QDictionary<long, FP> cooldowns = frame.ResolveDictionary(CooldownsDict);
                        cooldowns.Add(momentumConsideration.Guid.Value, momentumConsideration.Cooldown);
                    }
                }
            }
        }
        #endregion

        #region ConsiderationsChoiceReactions
        private static void OnConsiderationChosen(FrameThreadSafe frame, UtilityReasoner* reasoner,
            AssetRef<Consideration> chosenConsiderationRef, EntityRef entity, ref AIContext aiContext)
        {
            Consideration chosenConsideration = frame.FindAsset<Consideration>(chosenConsiderationRef.Id);

            QList<AssetRef<Consideration>> previousExecution = frame.ResolveList(reasoner->PreviousExecution);

            if (previousExecution[chosenConsideration.Depth - 1] != chosenConsideration)
            {
                // Exit the one that we're replacing
                var replacedConsideration = frame.FindAsset<Consideration>(previousExecution[chosenConsideration.Depth - 1].Id);
                if (replacedConsideration != default)
                {
                    replacedConsideration.OnExit(frame, entity, ref aiContext);
                }

                // Exit the consecutive ones
                for (int i = chosenConsideration.Depth; i < previousExecution.Count; i++)
                {
                    var cs = frame.FindAsset<Consideration>(previousExecution[i].Id);
                    if (cs == default)
                        break;

                    cs.OnExit(frame, entity, ref aiContext);

                    previousExecution[i] = default;
                }

                // Insert and Enter on the new chosen consideration
                previousExecution[chosenConsideration.Depth - 1] = chosenConsideration;
                chosenConsideration.OnEnter(frame, entity, ref aiContext);
            }
        }

        private static void OnNoConsiderationChosen(FrameThreadSafe frame, UtilityReasoner* reasoner, byte depth,
            EntityRef entity, ref AIContext aiContext)
        {
            QList<AssetRef<Consideration>> previousExecution = frame.ResolveList(reasoner->PreviousExecution);

            for (int i = depth - 1; i < previousExecution.Count; i++)
            {
                var cs = frame.FindAsset<Consideration>(previousExecution[i].Id);
                if (cs == default)
                    break;

                cs.OnExit(frame, entity, ref aiContext);
                previousExecution[i] = default;
            }
        }
        #endregion
    }
}
