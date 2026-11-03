namespace Quantum
{
    // Interface to make the communication between the three solutions involved: quantum.botsdk.core, unity and and quantum.ai.editor
    // Allows for polling the EntityRef from a Unity MonoBehaviour
    public interface IBotDebug
    {
        EntityRef BotEntityRef { get; }
    }
}