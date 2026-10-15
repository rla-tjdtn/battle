using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    public class PlayerAim : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 20f;
        [SerializeField] private LayerMask aimMask = ~0;
        [SerializeField] private float defaultAimDistance = 40f;
        [SerializeField] private float aimHeight = 1.1f;

        private Vector3 aimPoint;
        private float recoilYaw;
        private float recoilPitch;
        private Camera cachedCamera;

        public Vector3 AimPoint => aimPoint;
        public bool IsAiming { get; private set; }
        public Vector3 AimDirection => (aimPoint - GetAimOrigin()).normalized;

        private void Awake()
        {
            cachedCamera = Camera.main;
            aimMask = LayerMask.GetMask("Default", "Ground", "Cover", "Enemy", "Player");
            if (aimMask == 0)
                aimMask = ~0;
        }

        private void Update()
        {
            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (Core.InputContext.BlockCombat)
                return;
            IsAiming = Input.GetMouseButton(1);
            UpdateAimPoint();
            RotateTowardsAim();

            recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, Time.deltaTime * 10f);
            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, Time.deltaTime * 10f);
        }

        public void AddRecoil(float pitch, float yaw)
        {
            recoilPitch += pitch;
            recoilYaw += Random.Range(-yaw, yaw);
        }

        private Vector3 GetAimOrigin()
        {
            return transform.position + Vector3.up * aimHeight;
        }

        private void UpdateAimPoint()
        {
            if (cachedCamera == null)
            {
                aimPoint = GetAimOrigin() + transform.forward * defaultAimDistance;
                return;
            }

            Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, GetAimOrigin().y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask, QueryTriggerInteraction.Ignore))
            {
                aimPoint = hit.point;
            }
            else if (groundPlane.Raycast(ray, out float enter))
            {
                aimPoint = ray.GetPoint(enter);
            }
            else
            {
                aimPoint = ray.GetPoint(defaultAimDistance);
            }

            // Keep aim roughly at character torso height for horizontal facing.
            aimPoint.y = Mathf.Lerp(aimPoint.y, GetAimOrigin().y, 0.35f);
        }

        private void RotateTowardsAim()
        {
            Vector3 origin = GetAimOrigin();
            Vector3 flat = aimPoint - origin;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.001f)
                return;

            Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
            target *= Quaternion.Euler(0f, recoilYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(GetAimOrigin(), aimPoint);
            Gizmos.DrawWireSphere(aimPoint, 0.15f);
        }
    }
}
