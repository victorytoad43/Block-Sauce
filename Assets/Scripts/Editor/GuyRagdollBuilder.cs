using Quantum;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Builds the knockdown ragdoll prefab from Guy's skeleton (Block Sauce › Build Guy Ragdoll). The ragdoll is a
    /// skeleton of empty helper transforms placed on Guy's bones, baked by QuantumRagdoll into 11 jointed limbs; it has
    /// no mesh of its own, because Guy's own model is driven from it in the view (RagdollPoseView). Rerun it whenever
    /// CH_Guy.fbx changes proportions. Each helper's Y axis runs along its bone toward the next joint, the convention
    /// QuantumRagdoll's joint setup expects, and the collision shapes are sized to Guy's mesh rather than to his bones,
    /// because his body is a bean with short legs.
    /// </summary>
    public static class GuyRagdollBuilder
    {
        private const string GuyPath = "Assets/Art/Characters/CH_Guy.fbx";
        private const string PrefabPath = "Assets/Art/Characters/CH_Guy_Ragdoll.prefab";

        /// <summary>
        /// The ragdoll's total mass, in kilograms: heavy enough that a tumbling body usually topples a tower of 2 kg
        /// blocks (5 kg mostly shoved towers along), light enough that a thrown block still shoves a body around.
        /// </summary>
        private const float TotalMass = 8f;

        [MenuItem("Block Sauce/Build Guy Ragdoll")]
        public static void Build()
        {
            var guyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuyPath);
            var guy = Object.Instantiate(guyAsset);
            // Keep the model root's import rotation (Blender's axes); only its position moves.
            guy.transform.position = Vector3.zero;

            try
            {
                // The FBX's default pose is whatever frame Blender was on at export, so bones are read in the rest
                // pose from the skin's bind poses instead.
                var restPose = GetRestPositions(guy);
                Vector3 Bone(string name) => restPose[name];

                var root = new GameObject("CH_Guy_Ragdoll");
                var rootPrototype = root.AddComponent<QuantumEntityPrototype>();
                rootPrototype.TransformMode = QuantumEntityPrototypeTransformMode.Transform3D;
                rootPrototype.PhysicsCollider.IsEnabled = false;
                rootPrototype.PhysicsBody.IsEnabled = false;
                root.AddComponent<QuantumEntityView>();

                // Torso: the hips helper sits on the hips joint, the chest helper on the chest joint, the head on the neck.
                Transform hips = Helper("hips", root.transform, Bone("hips"), Vector3.up);
                Transform chest = Helper("chest", hips, Bone("chest"), Vector3.up);
                Transform head = Helper("head", chest, Bone("head"), Vector3.up);

                // Arms reach from the shoulder joint to the fingertips, so the forearm capsule covers Guy's big hands.
                Transform upperArmL = Limb("upper_arm.L", chest, Bone("upper_arm.L"), Bone("forearm.L"));
                Transform forearmL = Limb("forearm.L", upperArmL, Bone("forearm.L"), HandTip(restPose, "L"));
                Transform handL = Helper("hand.L", forearmL, HandTip(restPose, "L"), Vector3.up);
                Transform upperArmR = Limb("upper_arm.R", chest, Bone("upper_arm.R"), Bone("forearm.R"));
                Transform forearmR = Limb("forearm.R", upperArmR, Bone("forearm.R"), HandTip(restPose, "R"));
                Transform handR = Helper("hand.R", forearmR, HandTip(restPose, "R"), Vector3.up);

                // Legs: thigh to knee, shin to the sole.
                Transform thighL = Limb("thigh.L", hips, Bone("thigh.L"), Bone("shin.L"));
                Transform shinL = Limb("shin.L", thighL, Bone("shin.L"), Sole(Bone("foot.L")));
                Transform footL = Helper("foot.L", shinL, Sole(Bone("foot.L")), Vector3.up);
                Transform thighR = Limb("thigh.R", hips, Bone("thigh.R"), Bone("shin.R"));
                Transform shinR = Limb("shin.R", thighR, Bone("shin.R"), Sole(Bone("foot.R")));
                Transform footR = Helper("foot.R", shinR, Sole(Bone("foot.R")), Vector3.up);

                var ragdoll = root.AddComponent<QuantumRagdoll>();
                QuantumRagdoll.RagdollParameters settings = ragdoll.Settings;
                settings._limbUpDirection = QuantumRagdoll.BoneDirection.Y;
                settings._root = root.transform;
                settings._hip = hips; settings._chest = chest; settings._head = head;
                settings._leftArm = upperArmL; settings._leftElbow = forearmL; settings._leftHand = handL;
                settings._rightArm = upperArmR; settings._rightElbow = forearmR; settings._rightHand = handR;
                settings._leftLeg = thighL; settings._leftKnee = shinL; settings._leftFoot = footL;
                settings._rightLeg = thighR; settings._rightKnee = shinR; settings._rightFoot = footR;
                settings._limbThickness = 0.09f; settings._jointDistance = 0.02f;
                settings._headSize = 0.3f; settings._totalMass = TotalMass;
                ragdoll.Settings = settings;
                ragdoll.BuildRagdoll();

                // QuantumRagdoll sizes the torso from the joint positions; Guy's torso is a bean, so the shapes are set
                // from his mesh instead (measured from CH_Guy.fbx at game scale). Gaps between neighbors keep jointed
                // limbs from fighting each other.
                SetBox(hips, new Vector3(0f, 0.675f, 0f), new Vector3(0.36f, 0.12f, 0.32f));
                SetBox(chest, new Vector3(0f, 0.92f, 0f), new Vector3(0.3f, 0.1f, 0.3f));
                SetSphere(head, new Vector3(0f, 1.35f, 0f), 0.3f);
                SetCapsule(upperArmL, Bone("upper_arm.L"), Bone("forearm.L"), 0.1f, 0.1f, 0.02f);
                SetCapsule(forearmL, Bone("forearm.L"), HandTip(restPose, "L"), 0.12f, 0.02f, 0f);
                SetCapsule(upperArmR, Bone("upper_arm.R"), Bone("forearm.R"), 0.1f, 0.1f, 0.02f);
                SetCapsule(forearmR, Bone("forearm.R"), HandTip(restPose, "R"), 0.12f, 0.02f, 0f);
                SetCapsule(thighL, Bone("thigh.L"), Bone("shin.L"), 0.09f, 0.03f, 0.09f);
                SetCapsule(shinL, Bone("shin.L"), Sole(Bone("foot.L")), 0.08f, 0.08f, 0.08f);
                SetCapsule(thighR, Bone("thigh.R"), Bone("shin.R"), 0.09f, 0.03f, 0.09f);
                SetCapsule(shinR, Bone("shin.R"), Sole(Bone("foot.R")), 0.08f, 0.08f, 0.08f);

                // Freeze the bake so the hand-set shapes survive later inspector edits.
                var so = new SerializedObject(ragdoll);
                so.FindProperty("_autoBakeMode").enumValueIndex = (int)QuantumRagdoll.BakeMode.Disabled;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
                Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB();
                Debug.Log($"Built {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(guy);
            }
        }

        /// <summary>
        /// Creates an empty helper at a world position whose Y axis points along <paramref name="up"/> and whose Z axis
        /// stays as close to the character's forward as it can.
        /// </summary>
        private static Transform Helper(string name, Transform parent, Vector3 position, Vector3 up)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(Vector3.right, up);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward.normalized, up.normalized));
            return go.transform;
        }

        /// <summary>
        /// Creates a limb helper at <paramref name="from"/> whose Y axis points at <paramref name="to"/>.
        /// </summary>
        private static Transform Limb(string name, Transform parent, Vector3 from, Vector3 to)
        {
            return Helper(name, parent, from, to - from);
        }

        /// <summary>
        /// Returns the tip of a hand: the hand bone's head plus its length along the forearm's direction.
        /// </summary>
        private static Vector3 HandTip(System.Collections.Generic.Dictionary<string, Vector3> restPose, string side)
        {
            Vector3 forearm = restPose["forearm." + side];
            Vector3 hand = restPose["hand." + side];
            return hand + (hand - forearm).normalized * 0.2f;
        }

        /// <summary>
        /// Returns the point on the ground under a foot bone.
        /// </summary>
        private static Vector3 Sole(Vector3 foot)
        {
            return new Vector3(foot.x, 0f, foot.z);
        }

        private static void SetBox(Transform limb, Vector3 worldCenter, Vector3 extents)
        {
            SetShape(limb, new Shape3DConfig
            {
                ShapeType = Shape3DType.Box,
                BoxExtents = extents.ToFPVector3(),
                PositionOffset = limb.InverseTransformPoint(worldCenter).ToFPVector3(),
            });
        }

        private static void SetSphere(Transform limb, Vector3 worldCenter, float radius)
        {
            SetShape(limb, new Shape3DConfig
            {
                ShapeType = Shape3DType.Sphere,
                SphereRadius = radius.ToFP(),
                PositionOffset = limb.InverseTransformPoint(worldCenter).ToFPVector3(),
            });
        }

        /// <summary>
        /// Sets a capsule along the limb from <paramref name="from"/> to <paramref name="to"/>, pulled in by the given
        /// gaps at each end so it clears its jointed neighbors.
        /// </summary>
        private static void SetCapsule(Transform limb, Vector3 from, Vector3 to, float radius, float startGap, float endGap)
        {
            float length = Vector3.Distance(from, to) - startGap - endGap;
            float height = Mathf.Max(length + radius * 2f, radius * 2f + 0.01f);
            SetShape(limb, new Shape3DConfig
            {
                ShapeType = Shape3DType.Capsule,
                CapsuleRadius = radius.ToFP(),
                CapsuleHeight = height.ToFP(),
                PositionOffset = (Vector3.up * (startGap + length / 2f)).ToFPVector3(),
            });
        }

        private static void SetShape(Transform limb, Shape3DConfig shape)
        {
            var prototype = limb.GetComponent<QuantumEntityPrototype>();
            prototype.PhysicsCollider.Shape3D = shape;
        }

        /// <summary>
        /// Returns each skinned bone's world position in the rest pose, from the mesh's bind poses.
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, Vector3> GetRestPositions(GameObject model)
        {
            var positions = new System.Collections.Generic.Dictionary<string, Vector3>();
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            Matrix4x4[] bindPoses = skin.sharedMesh.bindposes;
            for (int i = 0; i < skin.bones.Length; i++)
            {
                Matrix4x4 rest = skin.transform.localToWorldMatrix * bindPoses[i].inverse;
                positions[skin.bones[i].name] = rest.GetColumn(3);
            }

            return positions;
        }
    }
}
