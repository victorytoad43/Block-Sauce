using Quantum;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: when the character belongs to this client, points the orbit camera at it.
    /// </summary>
    public class LocalPlayerView : QuantumEntityViewComponent
    {
        private bool _isLocal;

        public override void OnActivate(Frame frame)
        {
            _isLocal = Game.PlayerIsLocal(frame.Get<Player>(EntityRef).Ref);
            if (_isLocal == false || OrbitCamera.Instance == null)
                return;

            float yaw = frame.TryGet(EntityRef, out PlayerLook look) ? look.Yaw.AsFloat : transform.eulerAngles.y;
            OrbitCamera.Instance.SetTarget(transform, yaw);
        }

        public override void OnDeactivate()
        {
            if (_isLocal && OrbitCamera.Instance != null)
            {
                OrbitCamera.Instance.SetTarget(null, OrbitCamera.Instance.Yaw);
            }
        }
    }
}
