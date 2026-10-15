using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    public class BlockyAnimator : MonoBehaviour
    {
        [SerializeField] private Transform armL;
        [SerializeField] private Transform armR;
        [SerializeField] private Transform legL;
        [SerializeField] private Transform legR;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float walkSwing = 28f;
        [SerializeField] private float runSwing = 42f;

        private PlayerMotor motor;
        private float phase;
        private bool isDead;
        private Quaternion deathTarget = Quaternion.Euler(90f, 0f, 0f);
        private float deathBlend;
        private Vector3 lastPosition;
        private float externalSpeed;
        private bool externalRunning;
        private float punchTimer;
        private float recoilTimer;
        private float dodgeTimer;

        public void Configure(Transform leftArm, Transform rightArm, Transform leftLeg, Transform rightLeg, Transform visual)
        {
            armL = leftArm;
            armR = rightArm;
            legL = leftLeg;
            legR = rightLeg;
            visualRoot = visual;
            motor = GetComponent<PlayerMotor>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (isDead)
            {
                deathBlend = Mathf.MoveTowards(deathBlend, 1f, Time.deltaTime * 2f);
                if (visualRoot != null)
                    visualRoot.localRotation = Quaternion.Slerp(Quaternion.identity, deathTarget, deathBlend);
                return;
            }

            float speed = 0f;
            bool running = false;
            bool crouch = false;
            bool prone = false;

            if (motor != null)
            {
                speed = motor.CurrentSpeed;
                running = motor.IsRunning;
                crouch = motor.IsCrouching;
                prone = motor.IsProne;
            }
            else
            {
                Vector3 delta = transform.position - lastPosition;
                delta.y = 0f;
                float measured = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                lastPosition = transform.position;
                speed = Mathf.Max(externalSpeed, measured);
                running = externalRunning || measured > 5f;
            }

            if (punchTimer > 0f)
            {
                punchTimer -= Time.deltaTime;
                float p = 1f - Mathf.Clamp01(punchTimer / 0.25f);
                float punch = Mathf.Sin(p * Mathf.PI) * 55f;
                if (armR != null)
                    armR.localRotation = Quaternion.Euler(-punch, 0f, -20f);
                if (armL != null)
                    armL.localRotation = Quaternion.Euler(10f, 0f, 12f);
                return;
            }

            float swing = running ? runSwing : walkSwing;
            float cadence = running ? 10f : 7f;

            if (speed > 0.1f)
                phase += Time.deltaTime * cadence;
            else
                phase += Time.deltaTime * 1.6f;

            float move = Mathf.Clamp01(speed / 4.5f);
            float t = Mathf.Sin(phase) * Mathf.Lerp(4f, swing, move);

            if (armL != null)
                armL.localRotation = Quaternion.Euler(t, 0f, 8f);
            float kick = recoilTimer > 0f ? 22f : 0f;
            if (armR != null)
                armR.localRotation = Quaternion.Euler(-t - kick, 0f, -8f);
            if (legL != null)
                legL.localRotation = Quaternion.Euler(-t * (prone ? 0.2f : 1f), 0f, 0f);
            if (legR != null)
                legR.localRotation = Quaternion.Euler(t * (prone ? 0.2f : 1f), 0f, 0f);

            if (visualRoot != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(0.012f, 0.035f, move);
                float yOff = prone ? -0.55f : crouch ? -0.28f : 0f;
                visualRoot.localPosition = new Vector3(0f, bob + yOff, 0f);
                float lean = 0f;
                if (dodgeTimer > 0f)
                {
                    dodgeTimer -= Time.deltaTime;
                    lean = 28f * Mathf.Clamp01(dodgeTimer / 0.32f);
                }
                if (recoilTimer > 0f)
                    recoilTimer -= Time.deltaTime;
                visualRoot.localRotation = prone
                    ? Quaternion.Euler(75f, 0f, 0f)
                    : crouch
                        ? Quaternion.Euler(12f + lean, 0f, 0f)
                        : Quaternion.Euler(lean, 0f, 0f);
            }
        }

        public void PlayPunch()
        {
            punchTimer = 0.25f;
        }

        public void PlayDodge()
        {
            dodgeTimer = 0.32f;
        }

        public void PlayShoot()
        {
            recoilTimer = 0.08f;
        }

        public void PlayDeath()
        {
            isDead = true;
        }

        public void SetLocomotionSpeed(float speed, bool running)
        {
            externalSpeed = speed;
            externalRunning = running;
        }
    }
}
