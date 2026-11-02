using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: makes Guy hold what he carries. Grabbing plays the GrabBlock clip on the upper-body
    /// layer, which ends with both hands on a block at the hold point. While the block is held, his spine and chest turn
    /// and lean toward wherever the swing has taken it, and both arms reach for its sides (two-bone IK), so the hands stay
    /// on the block as it swings, lags or is raised to throw. A throw plays the follow-through of the Throw clip. View
    /// only: it reads the predicted frame and never writes to the simulation.
    /// </summary>
    public class HeldBlockPoseView : QuantumEntityViewComponent
    {
        private static readonly int GrabBlockId = Animator.StringToHash("GrabBlock");
        private static readonly int ThrowId = Animator.StringToHash("Throw");
        private static readonly int EmptyId = Animator.StringToHash("Empty");

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The Animator layer that plays GrabBlock and Throw over the upper body.")]
        [SerializeField] private string _upperBodyLayer = "UpperBody";
        [Tooltip("Seconds the upper-body layer takes to blend in on a grab and out on a release.")]
        [SerializeField] private float _blendSeconds = 0.12f;

        [Header("Torso")]
        [Tooltip("The share of the swing's turn taken by the spine and by the chest; the rest comes from the arms.")]
        [SerializeField] private float _spineTurnShare = 0.35f;
        [SerializeField] private float _chestTurnShare = 0.45f;
        [Tooltip("The share of the swing's raise or drop taken by the spine and by the chest, leaning back or bending over.")]
        [SerializeField] private float _spineLeanShare = 0.2f;
        [SerializeField] private float _chestLeanShare = 0.3f;
        [Tooltip("The furthest, in degrees, the torso turns either way; the arms reach the rest of the way.")]
        [SerializeField] private float _maxTurnDegrees = 70f;

        [Header("Hands")]
        [Tooltip("How far from a held block's center each hand grips, in meters: half the block's width plus the hand.")]
        [SerializeField] private float _blockGripHalfWidth = 0.45f;
        [Tooltip("The same for anything else held, such as a limb of a downed body.")]
        [SerializeField] private float _otherGripHalfWidth = 0.18f;
        [Tooltip("How far behind the held block's center, toward the character, the hands grip, in meters.")]
        [SerializeField] private float _gripBack = 0.12f;
        [Tooltip("Seconds the hands take to reach the block after a grab.")]
        [SerializeField] private float _reachSeconds = 0.1f;
        [Tooltip("How far into the Throw clip, as a share of its length, the follow-through starts.")]
        [SerializeField] private float _throwStart = 0.3f;

        private int _layer;
        private Transform _spine;
        private Transform _chest;
        private Transform _upperArmL, _forearmL, _handL;
        private Transform _upperArmR, _forearmR, _handR;
        private EntityRef _held;
        private Transform _heldView;
        private bool _heldIsBlock;
        private float _pivotHeight;
        private float _layerWeight;
        private float _reachWeight;
        private float _throwTimer;

        public override void OnInitialize()
        {
            _layer = _animator.GetLayerIndex(_upperBodyLayer);
            foreach (Transform bone in _animator.GetComponentsInChildren<Transform>())
            {
                switch (bone.name)
                {
                    case "spine": _spine = bone; break;
                    case "chest": _chest = bone; break;
                    case "upper_arm.L": _upperArmL = bone; break;
                    case "forearm.L": _forearmL = bone; break;
                    case "hand.L": _handL = bone; break;
                    case "upper_arm.R": _upperArmR = bone; break;
                    case "forearm.R": _forearmR = bone; break;
                    case "hand.R": _handR = bone; break;
                }
            }
        }

        public override void OnActivate(Frame frame)
        {
            QuantumEvent.Subscribe(this, (EventThrown e) =>
            {
                if (e.Entity != EntityRef || _layer < 0)
                    return;

                _animator.Play(ThrowId, _layer, _throwStart);
                _throwTimer = GetClipLength("Throw") * (1f - _throwStart);
            });
        }

        public override void OnDeactivate()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        public override void OnUpdateView()
        {
            if (_layer < 0)
                return;

            EntityRef held = GetHeld(out GrabConfig config);
            if (held != _held)
            {
                _held = held;
                _heldView = null;
                if (held != EntityRef.None)
                {
                    QuantumEntityView view = EntityView.EntityViewUpdater.GetView(held);
                    _heldView = view != null ? view.transform : null;
                    _heldIsBlock = PredictedFrame.Has<Block>(held);
                    _pivotHeight = config.PivotHeight.AsFloat;
                    _throwTimer = 0f;
                    _reachWeight = 0f;
                    _animator.CrossFade(GrabBlockId, 0.05f, _layer, 0f);
                }
            }

            _throwTimer = Mathf.Max(0f, _throwTimer - Time.deltaTime);
            bool active = _heldView != null || _throwTimer > 0f;
            if (active == false && _layerWeight <= 0f)
            {
                _animator.Play(EmptyId, _layer, 0f);
            }

            _layerWeight = Mathf.MoveTowards(_layerWeight, active ? 1f : 0f, Time.deltaTime / _blendSeconds);
            _animator.SetLayerWeight(_layer, _layerWeight);
            _reachWeight = Mathf.MoveTowards(_reachWeight, _heldView != null ? 1f : 0f, Time.deltaTime / _reachSeconds);
        }

        /// <summary>
        /// Returns what the character holds, or none while it is down or holds nothing.
        /// </summary>
        private EntityRef GetHeld(out GrabConfig config)
        {
            config = null;
            Frame frame = PredictedFrame;
            if (frame == null || frame.Has<RagdollOwner>(EntityRef) || frame.TryGet(EntityRef, out Grabber grabber) == false
                || grabber.Held == EntityRef.None || frame.Exists(grabber.Held) == false)
                return EntityRef.None;

            config = frame.FindAsset(grabber.Config);
            return grabber.Held;
        }

        public override void OnLateUpdateView()
        {
            if (_heldView == null || _reachWeight <= 0f || _spine == null)
                return;

            Vector3 pivot = transform.position + Vector3.up * _pivotHeight;
            Vector3 toHeld = _heldView.position - pivot;
            Vector3 local = Quaternion.Inverse(transform.rotation) * toHeld;
            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            float turn = Mathf.Clamp(yaw, -_maxTurnDegrees, _maxTurnDegrees) * _reachWeight;
            float lean = pitch * _reachWeight;

            TurnBone(_spine, turn * _spineTurnShare, lean * _spineLeanShare);
            TurnBone(_chest, turn * _chestTurnShare, lean * _chestLeanShare);

            Vector3 flat = new Vector3(toHeld.x, 0f, toHeld.z);
            Vector3 direction = flat.sqrMagnitude > 1e-4f ? flat.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            float halfWidth = _heldIsBlock ? _blockGripHalfWidth : _otherGripHalfWidth;
            Vector3 grip = _heldView.position - direction * _gripBack;

            ReachFor(_upperArmL, _forearmL, _handL, grip - side * halfWidth);
            ReachFor(_upperArmR, _forearmR, _handR, grip + side * halfWidth);
        }

        /// <summary>
        /// Turns a bone about world up and leans it back (positive) or forward about the character's right.
        /// </summary>
        private void TurnBone(Transform bone, float turnDegrees, float leanDegrees)
        {
            bone.rotation = Quaternion.AngleAxis(turnDegrees, Vector3.up)
                * Quaternion.AngleAxis(-leanDegrees, transform.right) * bone.rotation;
        }

        /// <summary>
        /// Moves the hand toward <paramref name="target"/> by the reach weight, keeping its animated rotation.
        /// </summary>
        private void ReachFor(Transform upperArm, Transform forearm, Transform hand, Vector3 target)
        {
            TwoBoneIK.Solve(upperArm, forearm, hand, Vector3.Lerp(hand.position, target, _reachWeight));
        }

        private float GetClipLength(string clipName)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                    return clip.length;
            }

            return 0f;
        }
    }
}
