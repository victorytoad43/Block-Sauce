using System;

[Serializable]
public struct CurveDefinition
{
    public ECurveType CurveType;
    public CurveParam[] Parameters;
}

[Serializable]
public struct CurveParam
{
    public string Name;
    public float Value;
}

public enum ECurveType { None, Linear, Exponential, Logistic }
