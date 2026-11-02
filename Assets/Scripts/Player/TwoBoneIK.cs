using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Analytic two-bone IK for Guy's arms and legs, applied on top of the animated pose in late update.
    /// </summary>
    public static class TwoBoneIK
    {
        /// <summary>
        /// Moves <paramref name="end"/> to <paramref name="target"/> by turning <paramref name="upper"/> and
        /// <paramref name="lower"/>, bending the middle joint in the plane the animation already bends it in.
        /// The end bone keeps its animated world rotation. A target out of reach is approached as far as it goes.
        /// </summary>
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target)
        {
            Quaternion endRotation = end.rotation;
            Vector3 root = upper.position;
            Vector3 joint = lower.position;

            float upperLength = Vector3.Distance(root, joint);
            float lowerLength = Vector3.Distance(joint, end.position);
            Vector3 toTarget = target - root;
            float reach = Mathf.Clamp(toTarget.magnitude, 0.01f, (upperLength + lowerLength) * 0.999f);
            Vector3 reachDirection = toTarget.normalized;

            Vector3 bendNormal = Vector3.Cross(reachDirection, joint - root);
            if (bendNormal.sqrMagnitude < 1e-6f)
            {
                bendNormal = Vector3.Cross(reachDirection, -Vector3.up);
            }

            float cosRoot = (upperLength * upperLength + reach * reach - lowerLength * lowerLength)
                / (2f * upperLength * reach);
            float rootAngle = Mathf.Acos(Mathf.Clamp(cosRoot, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 jointTarget = root + Quaternion.AngleAxis(rootAngle, bendNormal.normalized) * reachDirection * upperLength;

            upper.rotation = Quaternion.FromToRotation(joint - root, jointTarget - root) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, target - lower.position)
                * lower.rotation;
            end.rotation = endRotation;
        }
    }
}
