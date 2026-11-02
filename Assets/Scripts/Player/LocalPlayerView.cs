using Quantum;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: when the character belongs to this client, points the orbit camera at it,
    /// and tells the camera each frame whether it holds a block, which way it faces, and where the block's swing (or the
    /// throw aim) stands.
    /// </summary>
    public class LocalPlayerView : QuantumEntityViewComponent
    {
        private bool _isLocal;

        public override void OnActivate(Frame frame)
        {
            _isLocal = Game.PlayerIsLocal(frame.Get<Player>(EntityRef).Ref);
            if (_isLocal == false || OrbitCamera.Instance == null)
                return;

            // Start behind the character's spawn facing. PlayerLook only echoes the camera's own input, which is not
            // meaningful until the camera has a target.
            Photon.Deterministic.FPVector3 forward = frame.Get<Transform3D>(EntityRef).Rotation * Photon.Deterministic.FPVector3.Forward;
            float yaw = UnityEngine.Mathf.Atan2(forward.X.AsFloat, forward.Z.AsFloat) * UnityEngine.Mathf.Rad2Deg;
            OrbitCamera.Instance.SetTarget(transform, yaw);
        }

        public override void OnUpdateView()
        {
            if (_isLocal == false || OrbitCamera.Instance == null)
                return;

            bool isHolding = TryGetPredictedQuantumComponent(out Grabber grabber) && grabber.Held != EntityRef.None;
            OrbitCamera.Instance.SetHolding(isHolding, transform.eulerAngles.y);
            if (isHolding)
            {
                // Aiming spends vertical input on the throw angle instead of the swing pitch.
                GrabConfig config = PredictedFrame.FindAsset(grabber.Config);
                if (grabber.IsAiming)
                {
                    OrbitCamera.Instance.SetSwing(true, 0f, config.BodyTurnThreshold.AsFloat, grabber.AimPitch.AsFloat,
                        config.AimPitchMin.AsFloat, config.AimPitchMax.AsFloat, config.AimPitchSensitivity.AsFloat);
                }
                else
                {
                    OrbitCamera.Instance.SetSwing(false, grabber.SwingYaw.AsFloat, config.BodyTurnThreshold.AsFloat,
                        grabber.SwingPitch.AsFloat, config.SwingPitchMin.AsFloat, config.SwingPitchMax.AsFloat, 1f);
                }
            }
        }

        public override void OnDeactivate()
        {
            if (_isLocal && OrbitCamera.Instance != null)
            {
                OrbitCamera.Instance.SetHolding(false, 0f);
                OrbitCamera.Instance.SetTarget(null, OrbitCamera.Instance.Yaw);
            }
        }
    }
}
