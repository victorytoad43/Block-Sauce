using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Pathfinding for bots on the map's Quantum navmesh (baked from Unity's by Block Sauce › Build Arena or
    /// › Bake Arena NavMesh). A bot walking somewhere asks here for the next point to head for: the next corner of the
    /// path, or, at the start of a link (a ledge onto a pillar or the inner ring), the link's far end with a jump.
    /// Paths are looked up a few times a second, not every tick, and kept on the <see cref="Bot"/>. A map without a
    /// navmesh (the Lobby) answers with the target itself, so bots walk straight as before.
    /// </summary>
    public static unsafe class BotNavigation
    {
        /// <summary>Seconds between path lookups while walking.</summary>
        private static readonly FP RepathSeconds = FP._0_25;
        /// <summary>How far, in meters, the target may move before the path is looked up again at once.</summary>
        private static readonly FP RetargetDistance = FP._1;
        /// <summary>How close, in meters, a corner counts as reached (the next lookup then aims past it).</summary>
        private static readonly FP CornerReachDistance = FP._0_50;
        /// <summary>How close to a link's start, in meters, the bot heads for its far end and jumps.</summary>
        private static readonly FP LinkStartDistance = (FP)8 / 10;
        /// <summary>How long, in seconds, a bot keeps heading for a link's far end before looking again.</summary>
        private static readonly FP LinkSeconds = FP._1 + FP._0_50;

        /// <summary>
        /// Editor tests only: true makes every bot walk straight, as without a navmesh, to compare the two. Never set in
        /// a networked game (it isn't part of the simulation state).
        /// </summary>
        public static bool TestDisable;

        /// <summary>
        /// Returns the point to walk toward on the way to <paramref name="target"/> and whether to jump now.
        /// </summary>
        public static FPVector3 NextPoint(Frame frame, Bot* bot, FPVector3 position, FPVector3 target, out bool jump)
        {
            jump = false;
            NavMesh navMesh = TestDisable ? null : FindNavMesh(frame);
            if (navMesh == null)
                return target;

            bool reachedPoint = bot->HasPath && FlatDistance(position, bot->PathPoint) < CornerReachDistance;
            bool retarget = bot->HasPath == false || FlatDistance(bot->PathTarget, target) > RetargetDistance;
            if (bot->PathJump && bot->PathTimer.IsRunning(frame) && reachedPoint == false && retarget == false)
            {
                // On a link: keep heading for its far end.
                return bot->PathPoint;
            }

            if (retarget || reachedPoint || bot->PathTimer.IsRunning(frame) == false)
            {
                FindNext(frame, bot, navMesh, position, target);
                jump = bot->PathJump;
            }

            return bot->HasPath ? bot->PathPoint : target;
        }

        /// <summary>
        /// Forgets the bot's path, so the next step looks it up afresh (a new task).
        /// </summary>
        public static void Clear(Bot* bot)
        {
            bot->HasPath = false;
            bot->PathJump = false;
        }

        private static void FindNext(Frame frame, Bot* bot, NavMesh navMesh, FPVector3 position, FPVector3 target)
        {
            bot->PathTarget = target;
            bot->PathTimer = FrameTimer.FromSeconds(frame, RepathSeconds);
            bot->PathJump = false;

            NavMeshRegionMask regions = NavMeshRegionMask.Default;
            Navigation.FindPathResult result = frame.Navigation.FindPath((FrameThreadSafe)frame, position, target, navMesh,
                regions);
            if (result.Result != Navigation.FindPathResult.Code.Success || result.PathSize < 2)
            {
                // Off the navmesh (on a tower, in the air) or no way there: straight at the target.
                bot->HasPath = false;
                return;
            }

            Navigation.PathVertex[] path = result.Path;
            int next = 1;
            while (next < result.PathSize - 1 && FlatDistance(position, path[next].Point) < CornerReachDistance
                   && path[next].Link < 0)
            {
                next++;
            }

            Navigation.PathVertex corner = path[next];
            bot->HasPath = true;
            bot->PathPoint = corner.Point;
            if (corner.Link >= 0 && FlatDistance(position, corner.Point) < LinkStartDistance)
            {
                NavMeshLink link = navMesh.Links[corner.Link];
                // Links run both ways: head for whichever end is further from here.
                FPVector3 far = FlatDistance(position, link.Start) < FlatDistance(position, link.End) ? link.End : link.Start;
                bot->PathPoint = far;
                bot->PathJump = true;
                bot->PathTimer = FrameTimer.FromSeconds(frame, LinkSeconds);
            }
        }

        private static NavMesh FindNavMesh(Frame frame)
        {
            if (frame.Map == null || frame.Map.NavMeshes == null || frame.Map.NavMeshes.Count == 0)
                return null;

            foreach (NavMesh navMesh in frame.Map.NavMeshes.Values)
                return navMesh;

            return null;
        }

        private static FP FlatDistance(FPVector3 a, FPVector3 b)
        {
            FPVector3 delta = a - b;
            delta.Y = FP._0;
            return delta.Magnitude;
        }
    }
}
