using UnityEngine;

namespace BlockyBattleRoyale.Networking
{
    /// <summary>
    /// Lightweight transform sync placeholder used when full Netcode player prefabs
    /// are not yet spawned. Online path upgrades to NetworkTransform later.
    /// </summary>
    public class NetworkPlayerSync : MonoBehaviour
    {
        [SerializeField] private bool isLocal = true;
        [SerializeField] private float interp = 12f;

        private Vector3 remotePos;
        private Quaternion remoteRot;
        private bool hasRemote;

        public void SetLocal(bool value) => isLocal = value;

        public void PushRemoteState(Vector3 pos, Quaternion rot)
        {
            remotePos = pos;
            remoteRot = rot;
            hasRemote = true;
        }

        private void Update()
        {
            if (isLocal || !hasRemote)
                return;

            transform.position = Vector3.Lerp(transform.position, remotePos, Time.deltaTime * interp);
            transform.rotation = Quaternion.Slerp(transform.rotation, remoteRot, Time.deltaTime * interp);
        }
    }
}
