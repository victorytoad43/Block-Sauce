namespace Quantum
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class BotSDKHiddenAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class BotSDKCategoryAttribute : System.Attribute
    {
        public string Name;
    }


    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class BotSDKRequiredAttribute : System.Attribute
    {
        public bool WarnUnlinkedOnly;
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class BotSDKTooltipAttribute : System.Attribute
    {
        public string Text;
    }
}
