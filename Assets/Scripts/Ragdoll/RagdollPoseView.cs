using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: while the character is knocked down, poses Guy's own bones from the ragdoll body's
    /// limbs instead of hiding him behind a second model; when it gets up, plays the get-up clip for the way it lay
    /// (face down or face up) and blends into it from the last ragdoll pose. Reads the predicted frame every update,
    /// so a knockdown undone by a rollback simply snaps back to the animated pose. View only.
    /// </summary>
    public class RagdollPoseView : QuantumEntityViewComponent
    {
        private static readonly int GetUpFaceDownId = Animator.StringToHash("GetUpFaceDown");
        private static readonly int GetUpFaceUpId = Animator.StringToHash("GetUpFaceUp");

        /// <summary>
        /// One of Guy's bones driven by a ragdoll limb, with the fixed rotation between the limb and the bone.
        /// </summary>
        private struct DrivenBone
        {
            public Transform Bone;
            public string Limb;
            public Quaternion LimbToBone;
        }

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The ragdoll prefab the simulation spawns (CH_Guy_Ragdoll); its helpers' rest pose maps limbs to bones.")]
        [SerializeField] private GameObject _ragdollPrefab;
        [Tooltip("Seconds the get-up clip takes to blend in from the last ragdoll pose.")]
        [SerializeField] private float _getUpBlendSeconds = 0.15f;

        private readonly List<DrivenBone> _driven = new List<DrivenBone>();
        private readonly Dictionary<string, Transform> _limbs = new Dictionary<string, Transform>();
        // Each limb's path of sibling indices from the ragdoll root: Quantum renames entity views after their entity,
        // so the body's limbs are found by where they sit in the hierarchy, which matches the prefab.
        private readonly Dictionary<string, int[]> _limbPaths = new Dictionary<string, int[]>();
        private Transform[] _allBones;
        private Vector3[] _blendFromPositions;
        private Quaternion[] _blendFromRotations;
        private Transform _hips;
        private Vector3 _hipsFromPosition;
        private Quaternion _hipsFromRotation;
        private EntityRef _boundBody;
        private bool _wasDown;
        private float _blendTimer;

        /// <summary>
        /// True while the predicted frame shows this character knocked down.
        /// </summary>
        public bool IsDown { get; private set; }

        /// <summary>
        /// The world transform of a limb of the current ragdoll body (by helper name, e.g. "head"), or null.
        /// </summary>
        public Transform GetLimb(string name)
        {
            return IsDown && _limbs.TryGetValue(name, out Transform limb) ? limb : null;
        }

        public override void OnInitialize()
        {
            var skin = _animator.GetComponentInChildren<SkinnedMeshRenderer>();
            _allBones = skin.bones;
            _hips = System.Array.Find(_allBones, bone => bone.name == "hips");
            _blendFromPositions = new Vector3[_allBones.Length];
            _blendFromRotations = new Quaternion[_allBones.Length];
            BuildBoneMap(skin);
        }

        /// <summary>
        /// Pairs each ragdoll limb with Guy's bone of the same name and stores the rotation between them in the rest
        /// pose: the limb helpers' rest rotations come from the ragdoll prefab, the bones' from the skin's bind poses,
        /// both relative to the character root.
        /// </summary>
        private void BuildBoneMap(SkinnedMeshRenderer skin)
        {
            Matrix4x4 modelFromSkin = _animator.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            Matrix4x4[] bindPoses = skin.sharedMesh.bindposes;

            foreach (QuantumEntityPrototype limb in _ragdollPrefab.GetComponentsInChildren<QuantumEntityPrototype>())
            {
                if (limb.transform == _ragdollPrefab.transform)
                    continue;

                _limbPaths[limb.name] = GetSiblingPath(_ragdollPrefab.transform, limb.transform);

                int index = System.Array.FindIndex(skin.bones, bone => bone.name == limb.name);
                if (index < 0)
                    continue;

                Matrix4x4 boneRest = modelFromSkin * bindPoses[index].inverse;
                Quaternion boneRotation = Quaternion.LookRotation(boneRest.GetColumn(2), boneRest.GetColumn(1));
                Quaternion limbRotation = Quaternion.Inverse(_ragdollPrefab.transform.rotation) * limb.transform.rotation;

                _driven.Add(new DrivenBone
                {
                    Bone = skin.bones[index],
                    Limb = limb.name,
                    LimbToBone = Quaternion.Inverse(limbRotation) * boneRotation,
                });
            }
        }

        public override void OnUpdateView()
        {
            EntityRef body = EntityRef.None;
            IsDown = PredictedFrame != null && PredictedFrame.TryGet(EntityRef, out RagdollOwner owner)
                && PredictedFrame.Exists(owner.Body) && (body = owner.Body) != EntityRef.None;

            if (IsDown && body != _boundBody)
            {
                BindBody(body);
            }

            if (IsDown == _wasDown)
                return;

            _wasDown = IsDown;

            if (IsDown == false)
            {
                StartGetUp();
                _boundBody = EntityRef.None;
                _limbs.Clear();
            }
        }

        /// <summary>
        /// Finds the helper transforms of the body's view, which the limb views move every frame.
        /// </summary>
        private void BindBody(EntityRef body)
        {
            _limbs.Clear();
            QuantumEntityView bodyView = EntityView.EntityViewUpdater.GetView(body);
            if (bodyView == null)
                return;

            _boundBody = body;
            foreach (KeyValuePair<string, int[]> limb in _limbPaths)
            {
                Transform found = bodyView.transform;
                foreach (int index in limb.Value)
                {
                    found = index < found.childCount ? found.GetChild(index) : null;
                    if (found == null)
                        break;
                }

                if (found != null)
                {
                    _limbs[limb.Key] = found;
                }
            }
        }

        private static int[] GetSiblingPath(Transform root, Transform target)
        {
            var path = new List<int>();
            for (Transform t = target; t != root; t = t.parent)
            {
                path.Insert(0, t.GetSiblingIndex());
            }

            return path.ToArray();
        }

        public override void OnLateUpdateView()
        {
            if (IsDown)
            {
                ApplyRagdollPose();
                return;
            }

            if (_blendTimer > 0f)
            {
                BlendFromSnapshot();
            }
        }

        /// <summary>
        /// Turns each driven bone to its limb, and puts the hips where the hips limb is. The Animator keeps running
        /// underneath, so bones between driven ones (spine, shoulders, hands, feet) keep the animated local pose.
        /// </summary>
        private void ApplyRagdollPose()
        {
            foreach (DrivenBone driven in _driven)
            {
                if (_limbs.TryGetValue(driven.Limb, out Transform limb) == false)
                    continue;

                driven.Bone.rotation = limb.rotation * driven.LimbToBone;
                if (driven.Limb == "hips")
                {
                    driven.Bone.position = limb.position;
                }
            }
        }

        /// <summary>
        /// Remembers the last ragdoll pose and starts the get-up clip for the way the body lies: chest toward the
        /// ground is face down.
        /// </summary>
        private void StartGetUp()
        {
            for (int i = 0; i < _allBones.Length; i++)
            {
                _blendFromPositions[i] = _allBones[i].localPosition;
                _blendFromRotations[i] = _allBones[i].localRotation;
            }

            _hipsFromPosition = _hips.position;
            _hipsFromRotation = _hips.rotation;

            bool faceDown = true;
            if (_limbs.TryGetValue("chest", out Transform chest))
            {
                faceDown = Vector3.Dot(chest.forward, Vector3.down) > 0f;
            }

            _animator.Play(faceDown ? GetUpFaceDownId : GetUpFaceUpId, 0, 0f);
            _animator.Update(0f);
            _blendTimer = _getUpBlendSeconds;
        }

        /// <summary>
        /// Eases every bone from the remembered ragdoll pose into the animated get-up.
        /// </summary>
        private void BlendFromSnapshot()
        {
            _blendTimer -= Time.deltaTime;
            float weight = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(_blendTimer / _getUpBlendSeconds));

            // In local space, so a bone the Animator never writes (its curves were constant and stripped) blends from
            // its own unchanged pose to itself and stays put. The hips blend in world space instead, because the
            // character moved to its standing spot under them.
            for (int i = 0; i < _allBones.Length; i++)
            {
                Transform bone = _allBones[i];
                if (bone == _hips)
                    continue;

                bone.SetLocalPositionAndRotation(
                    Vector3.Lerp(_blendFromPositions[i], bone.localPosition, weight),
                    Quaternion.Slerp(_blendFromRotations[i], bone.localRotation, weight));
            }

            _hips.SetPositionAndRotation(Vector3.Lerp(_hipsFromPosition, _hips.position, weight),
                Quaternion.Slerp(_hipsFromRotation, _hips.rotation, weight));
        }
    }
}
