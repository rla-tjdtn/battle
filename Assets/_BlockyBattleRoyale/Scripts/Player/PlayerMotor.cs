using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Speeds")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float proneSpeed = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float rotationSpeed = 18f;
        [SerializeField] private float aimMoveMul = 0.7f;

        private CharacterController controller;
        private PlayerAim aim;
        private Vector3 velocity;
        private bool isCrouching;
        private bool isProne;
        private bool isRunning;
        private Vector3 moveInput;
        private float standingHeight = 1.8f;
        private float crouchHeight = 1.2f;
        private float proneHeight = 0.7f;
        private Vector3 standingCenter = new Vector3(0f, 0.9f, 0f);
        private Vector3 crouchCenter = new Vector3(0f, 0.6f, 0f);
        private Vector3 proneCenter = new Vector3(0f, 0.35f, 0f);

        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public bool IsRunning => isRunning && IsMoving && !isCrouching && !isProne;
        public bool IsCrouching => isCrouching;
        public bool IsProne => isProne;
        public float CurrentSpeed { get; private set; }
        public Vector3 PlanarVelocity { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            aim = GetComponent<PlayerAim>();
            standingHeight = controller.height;
            standingCenter = controller.center;
        }

        private void Update()
        {
            if (!Core.GameplayGate.InputEnabled)
                return;
            ReadInput();
            ApplyMovement();
        }

        private void ReadInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = new Vector3(h, 0f, v);
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isProne;

            // Duckov: C is holster (inventory). Crouch is Left Ctrl.
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (isProne) SetProne(false);
                SetCrouching(!isCrouching);
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (isCrouching) SetCrouching(false);
                SetProne(!isProne);
            }
        }

        private void SetCrouching(bool value)
        {
            isCrouching = value;
            if (isCrouching) isProne = false;
            ApplyCapsule();
        }

        private void SetProne(bool value)
        {
            isProne = value;
            if (isProne) isCrouching = false;
            ApplyCapsule();
        }

        private void ApplyCapsule()
        {
            if (isProne)
            {
                controller.height = proneHeight;
                controller.center = proneCenter;
            }
            else if (isCrouching)
            {
                controller.height = crouchHeight;
                controller.center = crouchCenter;
            }
            else
            {
                controller.height = standingHeight;
                controller.center = standingCenter;
            }
        }

        private void ApplyMovement()
        {
            Camera cam = Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cam != null)
            {
                forward = cam.transform.forward;
                right = cam.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 desired = (forward * moveInput.z + right * moveInput.x);
            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            float speed = walkSpeed;
            if (isProne)
                speed = proneSpeed;
            else if (isCrouching)
                speed = crouchSpeed;
            else if (isRunning)
                speed = runSpeed;

            if (aim != null && aim.IsAiming)
                speed *= aimMoveMul;

            CurrentSpeed = desired.magnitude * speed;
            PlanarVelocity = desired * speed;

            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            velocity.x = PlanarVelocity.x;
            velocity.z = PlanarVelocity.z;
            velocity.y += gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);

            // Facing is controlled by aim; motor only moves.
            if (aim == null && desired.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
