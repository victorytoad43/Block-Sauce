using Photon.Deterministic;
using Quantum.Collections;
using System;

namespace Quantum
{
    [Serializable, BotSDKHidden] public unsafe class AIFunctionAssetRefList : AIFunction<QList<AssetRef>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionBoolList : AIFunction<QList<bool>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionByteList : AIFunction<QList<byte>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionEntityRefList : AIFunction<QList<EntityRef>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionFPList : AIFunction<QList<FP>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionFPVector2List : AIFunction<QList<FPVector2>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionFPVector3List : AIFunction<QList<FPVector3>> { }
    [Serializable, BotSDKHidden] public unsafe class AIFunctionIntegerList : AIFunction<QList<int>> { }
}
