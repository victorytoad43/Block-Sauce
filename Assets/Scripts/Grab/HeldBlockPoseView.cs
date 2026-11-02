using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: makes Guy hold what he carries. Grabbing plays the GrabBlock clip on the upper-body
    /// layer, which ends with both hands on a block at the hold point. While the block is held, his spine and chest turn
    /// and lean toward wherever the swing has taken it, and both arms reach for its sides (two-bone IK), so the hands stay
    /// on the block as it swings or lags. Aiming holds the wind-up frame of the Throw clip (block overhead), and the
    /// block's model rides in his hands there; a throw plays the rest of the clip from that frame while the block's model
    /// catches up with where the real block flies. Airborne it is the same, over the jump. View only: it reads the
    /// predicted frame and never writes to the simulation. Only the block's model moves; the block itself stays where the
    /// simulation holds it.
    /// </summary>
    public class HeldBlockPoseView : QuantumEntityViewComponent
    {
        private static readonly int GrabBlockId = Animator.StringToHash("GrabBlock");
        private static readonly int ThrowId = Animator.StringToHash("Throw");
        private static readonly int AimId = Animator.StringToHash("Aim");
        private static readonly int EmptyId = Animator.StringToHash("Empty");

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The Animator layer that plays GrabBlock, Aim and Throw over the upper body.")]
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

        [Header("Aim and throw")]
        [Tooltip("The Throw clip frame held as the aim pose (the wind-up); a throw plays on from it.")]
        [SerializeField] private int _aimFrame = 4;
        [Tooltip("Seconds the aim pose takes to blend in, carrying the block's model up into the hands.")]
        [SerializeField] private float _aimBlendSeconds = 0.15f;
        [Tooltip("Seconds after a throw the block's model takes to go from the hands to the real, flying block.")]
        [SerializeField] private float _releaseSeconds = 0.08f;

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
        private float _aimTime;
        private bool _aiming;
        private float _aimWeight;
        private Transform _thrownView;
        private float _releaseTimer;
        private float _releaseWeight;

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

            AnimationClip throwClip = GetClip("Throw");
            _aimTime = throwClip != null ? Mathf.Clamp01(_aimFrame / (throwClip.length * throwClip.frameRate)) : 0f;
        }

        public override void OnActivate(Frame frame)
        {
            QuantumEvent.Subscribe(this, (EventThrown e) =>
            {
                if (e.Entity != EntityRef || _layer < 0)
                    return;

                AnimationClip throwClip = GetClip("Throw");
                _animator.Play(ThrowId, _layer, _aimTime);
                _throwTimer = throwClip != null ? throwClip.length * (1f - _aimTime) : 0f;

                QuantumEntityView view = EntityView.EntityViewUpdater.GetView(e.Held);
                _thrownView = view != null ? view.transform : null;
                _releaseTimer = _releaseSeconds;
                if (_held == e.Held)
                {
                    // Not yet seen as released this frame; otherwise the release already recorded the aim weight.
                    _releaseWeight = _aimWeight;
                }

                _aiming = false;
                _aimWeight = 0f;
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

            EntityRef held = GetHeld(out GrabConfig config, out bool aiming);
            if (held != _held)
            {
                _held = held;
                _heldView = null;
                _releaseWeight = _aimWeight;
                _aiming = false;
                _aimWeight = 0f;
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

            if (_heldView != null && aiming != _aiming)
            {
                _aiming = aiming;
                if (aiming)
                {
                    _animator.CrossFade(AimId, _aimBlendSeconds, _layer, _aimTime);
                }
                else
                {
                    _animator.CrossFade(GrabBlockId, _aimBlendSeconds, _layer, 1f);
                }
            }

            _aimWeight = Mathf.MoveTowards(_aimWeight, _aiming ? 1f : 0f, Time.deltaTime / _aimBlendSeconds);
            _releaseTimer = Mathf.Max(0f, _releaseTimer - Time.deltaTime);
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
        /// Returns what the character holds, or none while it is down or holds nothing, and whether it is aiming a throw.
        /// </summary>
        private EntityRef GetHeld(out GrabConfig config, out bool aiming)
        {
            config = null;
            aiming = false;
            Frame frame = PredictedFrame;
            if (frame == null || frame.Has<RagdollOwner>(EntityRef) || frame.TryGet(EntityRef, out Grabber grabber) == false
                || grabber.Held == EntityRef.None || frame.Exists(grabber.Held) == false)
                return EntityRef.None;

            config = frame.FindAsset(grabber.Config);
            aiming = grabber.IsAiming;
            return grabber.Held;
        }

        public override void OnLateUpdateView()
        {
            if (_thrownView != null && _releaseTimer > 0f && _handL != null)
            {
                float share = _releaseTimer / _releaseSeconds * _releaseWeight;
                _thrownView.position = Vector3.Lerp(_thrownView.position, GetBlockInHands(), share);
            }

            if (_heldView == null || _reachWeight <= 0f || _spine == null)
                return;

            // The animated hands, before the torso and arms are bent toward the real block, are where the aim pose holds
            // the block's model.
            Vector3 inHands = GetBlockInHands();
            float follow = _reachWeight * (1f - _aimWeight);

            Vector3 pivot = transform.position + Vector3.up * _pivotHeight;
            Vector3 toHeld = _heldView.position - pivot;
            Vector3 local = Quaternion.Inverse(transform.rotation) * toHeld;
            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            float turn = Mathf.Clamp(yaw, -_maxTurnDegrees, _maxTurnDegrees) * follow;
            float lean = pitch * follow;

            TurnBone(_spine, turn * _spineTurnShare, lean * _spineLeanShare);
            TurnBone(_chest, turn * _chestTurnShare, lean * _chestLeanShare);

            Vector3 flat = new Vector3(toHeld.x, 0f, toHeld.z);
            Vector3 direction = flat.sqrMagnitude > 1e-4f ? flat.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            float halfWidth = _heldIsBlock ? _blockGripHalfWidth : _otherGripHalfWidth;
            Vector3 grip = _heldView.position - direction * _gripBack;

            ReachFor(_upperArmL, _forearmL, _handL, grip - side * halfWidth, follow);
            ReachFor(_upperArmR, _forearmR, _handR, grip + side * halfWidth, follow);

            if (_aimWeight > 0f)
            {
                _heldView.position = Vector3.Lerp(_heldView.position, inHands, _aimWeight);
            }
        }

        /// <summary>
        /// Returns where a block gripped by both hands sits: between them, pushed out from the chest by the grip depth.
        /// </summary>
        private Vector3 GetBlockInHands()
        {
            Vector3 between = (_handL.position + _handR.position) * 0.5f;
            Vector3 outward = between - _chest.position;
            return outward.sqrMagnitude > 1e-4f ? between + outward.normalized * _gripBack : between;
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
        /// Moves the hand toward <paramref name="target"/> by <paramref name="weight"/>, keeping its animated rotation.
        /// </summary>
        private static void ReachFor(Transform upperArm, Transform forearm, Transform hand, Vector3 target, float weight)
        {
            TwoBoneIK.Solve(upperArm, forearm, hand, Vector3.Lerp(hand.position, target, weight));
        }

        private AnimationClip GetClip(string clipName)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                    return clip;
            }

            return null;
        }
    }
}
