using System.Text;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Test helper: lists joints the physics solver can't use safely (an owner without a body, or a connected entity that
    /// is gone or has no body). Read-only; for soak tests run from the editor.
    /// </summary>
    public static unsafe class JointValidator
    {
        /// <summary>
        /// Returns one line per bad joint in <paramref name="frame"/>, or an empty string.
        /// </summary>
        public static string Check(Frame frame)
        {
            var sb = new StringBuilder();
            var all = frame.Filter<PhysicsJoints3D>();
            while (all.NextUnsafe(out EntityRef entity, out PhysicsJoints3D* joints))
            {
                bool ownerBody = frame.Has<PhysicsBody3D>(entity);
                string kind = frame.Has<Block>(entity) ? "block" : frame.Has<Player>(entity) ? "player" : "other";
                if (joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                {
                    if (joints->Count > 0)
                        sb.Append($"{entity} {kind}: count {joints->Count} but no buffer\n");
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    EntityRef connected = buffer[i].ConnectedEntity;
                    bool connectedOk = connected == EntityRef.None
                        || (frame.Exists(connected) && frame.Has<PhysicsBody3D>(connected));
                    if (ownerBody && connectedOk)
                        continue;

                    sb.Append($"{entity} {kind} body {ownerBody}: joint {i} {buffer[i].Type} tag {buffer[i].UserTag} enabled {buffer[i].Enabled}"
                        + $" -> {connected} exists {frame.Exists(connected)} body {connectedOk}\n");
                }
            }

            return sb.ToString();
        }
    }
}
