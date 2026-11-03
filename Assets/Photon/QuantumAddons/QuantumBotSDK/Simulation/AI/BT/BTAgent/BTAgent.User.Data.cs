using Photon.Deterministic;
using Quantum.Core;
using System;

namespace Quantum
{
    public unsafe partial struct BTAgent
    {
        private const string INVALID_INDEX_ERROR = "[Bot SDK] Trying to access nodes data list in an invalid index.";

        /// <summary>
        /// Ensures that <see cref="BTDataValues"/> has enough entries to the requested amount.
        /// </summary>
        // This is mostly a safety measure when users have not yet compiled their BT AI documents after adding new entries of BTDataIndex
        // which could cause the BTDataValues list to have less fields than necessary for some of the indexes used on the tree
        private void EnsureNecessaryDataSlots(FrameBase frame, int necessaryAmount)
        {
            // Make it 1-based as it is always received as 0-based
            necessaryAmount += 1;

            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            if (nodesDataList.Count >= necessaryAmount)
            {
                return;
            }

            var missingAmount = necessaryAmount - nodesDataList.Count;
            for (int i = 0; i < missingAmount; i++)
            {
                nodesDataList.Add(new BTDataValue());
            }

            BTDataValues = nodesDataList;
        }

        #region Int and FP Data

        // TODO: look into codegen to create these methods
        // TODO: consider having the actual implementation only on the FTS versions and have the non-FTS overload just calling that. Need to test performance
        // Getter / Setters of node FP and Int32 data
        public void SetFPData(FrameBase frame, FP value, BTDataIndex index)
        {
            SetFPData(frame, value, index.Index);
        }

        public void SetFPData(FrameBase frame, FP value, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            *nodesDataList.GetPointer(index)->FPValue = value;
        }

        public void SetIntData(FrameBase frame, Int32 value, BTDataIndex index)
        {
            SetIntData(frame, value, index.Index);
        }

        public void SetIntData(FrameBase frame, Int32 value, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            *nodesDataList.GetPointer(index)->IntValue = value;
        }

        public FP GetFPData(FrameBase frame, BTDataIndex index)
        {
            return GetFPData(frame, index.Index);
        }

        public FP GetFPData(FrameBase frame, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            return *nodesDataList.GetPointer(index)->FPValue;
        }

        public Int32 GetIntData(FrameBase frame, BTDataIndex index)
        {
            return GetIntData(frame, index.Index);
        }

        public Int32 GetIntData(FrameBase frame, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            return *nodesDataList.GetPointer(index)->IntValue;
        }
        #endregion

        // -- THREADSAFE

        #region THREADSAFE Int and FP Data
        // Getter / Setters of node FP and Int32 data
        public void SetFPData(FrameThreadSafe frame, FP value, BTDataIndex index)
        {
            SetFPData(frame, value, index.Index);
        }

        public void SetFPData(FrameThreadSafe frame, FP value, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            *nodesDataList.GetPointer(index)->FPValue = value;
        }

        public void SetIntData(FrameThreadSafe frame, Int32 value, BTDataIndex index)
        {
            SetIntData(frame, value, index.Index);
        }

        public void SetIntData(FrameThreadSafe frame, Int32 value, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            *nodesDataList.GetPointer(index)->IntValue = value;
        }

        public FP GetFPData(FrameThreadSafe frame, BTDataIndex index)
        {
            return GetFPData(frame, index.Index);
        }

        public FP GetFPData(FrameThreadSafe frame, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            return *nodesDataList.GetPointer(index)->FPValue;
        }

        public Int32 GetIntData(FrameThreadSafe frame, BTDataIndex index)
        {
            return GetIntData(frame, index.Index);
        }

        public Int32 GetIntData(FrameThreadSafe frame, Int32 index)
        {
            var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
            EnsureNecessaryDataSlots(frame, index);

            Assert.Check(nodesDataList.Count > index, INVALID_INDEX_ERROR);
            return *nodesDataList.GetPointer(index)->IntValue;
        }
        #endregion
    }
}