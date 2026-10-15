using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    /// <summary>Duckov control: Space is a short dodge, not a jump or vault.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class DuckovDodgeController : MonoBehaviour
    {
        [SerializeField] private float dodgeDistance = 3f;
        [SerializeField] private float dodgeDuration = 0.32f;
        [SerializeField] private float cooldown = 1f;

        private CharacterController cc;
        private PlayerMotor motor;
        private bool dodging;
        private float dodgeT;
        private float cooldownLeft;
        private Vector3 dodgeDir;
        private Vector3 lastMoveDir = Vector3.forward;

        public bool IsDodging => dodging;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            if (!CompareTag("Player")) return;
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

            if (dodging)
            {
                float dt = Time.deltaTime;
                dodgeT += dt;
                float speed = dodgeDistance / Mathf.Max(0.05f, dodgeDuration);
                Vector3 motion = dodgeDir * speed * dt;
                motion.y = -2f * dt;
                cc.Move(motion);
                if (dodgeT >= dodgeDuration)
                {
                    dodging = false;
                    if (motor != null) motor.enabled = true;
                }
                return;
            }

            if (motor != null && motor.IsMoving)
            {
                Vector3 v = motor.PlanarVelocity;
                if (v.sqrMagnitude > 0.01f)
                    lastMoveDir = v.normalized;
            }

            if (!Input.GetKeyDown(KeyCode.Space)) return;
            if (cooldownLeft > 0f) return;
            if (motor != null && !motor.enabled) return;
            if (motor != null && motor.IsProne) return;

            dodgeDir = lastMoveDir;
            if (dodgeDir.sqrMagnitude < 0.01f)
                dodgeDir = transform.forward;
            dodgeDir.y = 0f;
            dodgeDir.Normalize();

            dodging = true;
            dodgeT = 0f;
            cooldownLeft = cooldown;
            if (motor != null) motor.enabled = false;
            GetComponent<BlockyAnimator>()?.PlayDodge();
        }
    }
}
