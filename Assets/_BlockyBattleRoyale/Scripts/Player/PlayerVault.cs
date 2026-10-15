using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    /// <summary>Space: short vault over low obstacles (PUBG-style).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerVault : MonoBehaviour
    {
        [SerializeField] private float maxObstacleHeight = 1.15f;
        [SerializeField] private float minObstacleHeight = 0.35f;
        [SerializeField] private float vaultDistance = 1.4f;
        [SerializeField] private float vaultDuration = 0.35f;
        [SerializeField] private float checkDistance = 1.1f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController cc;
        private PlayerMotor motor;
        private bool vaulting;
        private float vaultT;
        private Vector3 vaultFrom;
        private Vector3 vaultTo;

        public bool IsVaulting => vaulting;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            if (!CompareTag("Player")) return;

            if (vaulting)
            {
                vaultT += Time.deltaTime / Mathf.Max(0.05f, vaultDuration);
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(vaultT));
                Vector3 p = Vector3.Lerp(vaultFrom, vaultTo, u);
                // Arc
                p.y += Mathf.Sin(u * Mathf.PI) * 0.45f;
                Vector3 delta = p - transform.position;
                cc.Move(delta);
                if (vaultT >= 1f)
                {
                    vaulting = false;
                    if (motor != null) motor.enabled = true;
                }
                return;
            }

            if (!Input.GetKeyDown(KeyCode.Space)) return;
            if (motor != null && (motor.IsProne || motor.IsCrouching)) return;
            TryStartVault();
        }

        private void TryStartVault()
        {
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            Vector3 dir = transform.forward;
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, checkDistance, obstacleMask, QueryTriggerInteraction.Ignore))
                return;

            // Estimate height
            float topY = hit.collider.bounds.max.y;
            float height = topY - transform.position.y;
            if (height < minObstacleHeight || height > maxObstacleHeight)
                return;

            // Clearance beyond obstacle
            Vector3 land = hit.point + dir * vaultDistance;
            land.y = transform.position.y;
            if (Physics.CheckSphere(land + Vector3.up * 0.9f, 0.3f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // try slightly further
                land = hit.point + dir * (vaultDistance + 0.5f);
                land.y = transform.position.y;
            }

            vaultFrom = transform.position;
            vaultTo = land;
            vaultT = 0f;
            vaulting = true;
            if (motor != null) motor.enabled = false;
        }
    }
}
