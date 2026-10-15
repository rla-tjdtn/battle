using UnityEngine;
using BlockyBattleRoyale.BattleRoyale;
using BlockyBattleRoyale.Player;
// MatchStateMachine in BattleRoyale namespace

namespace BlockyBattleRoyale.World
{
    public class DropSystem : MonoBehaviour
    {
        [SerializeField] private Transform dropship;
        [SerializeField] private float shipAltitude = 55f;
        [SerializeField] private float shipSpeed = 28f;
        [SerializeField] private float freefallSpeed = 18f;
        [SerializeField] private float parachuteSpeed = 6f;
        [SerializeField] private float parachuteDeployHeight = 28f;

        private Transform player;
        private bool onShip = true;
        private bool freefall;
        private bool parachute;
        private Vector3 shipDir = new Vector3(1f, 0f, 0.35f).normalized;
        private CharacterController playerCc;
        private PlayerMotor motor;
        private PlayerAim aim;

        public void Begin(Transform playerTransform, Vector3 mapCenter)
        {
            player = playerTransform;
            playerCc = player.GetComponent<CharacterController>();
            motor = player.GetComponent<PlayerMotor>();
            aim = player.GetComponent<PlayerAim>();

            if (dropship == null)
            {
                var ship = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ship.name = "Dropship";
                Object.Destroy(ship.GetComponent<Collider>());
                ship.transform.localScale = new Vector3(8f, 2f, 3f);
                var rend = ship.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = BlockyCharacterBuilder.CreateLitMaterial(new Color(0.35f, 0.38f, 0.4f));
                }
                dropship = ship.transform;
            }

            Vector3 start = mapCenter + new Vector3(-90f, shipAltitude, -20f);
            dropship.position = start;
            dropship.rotation = Quaternion.LookRotation(shipDir);

            player.SetParent(dropship);
            player.localPosition = new Vector3(0f, -1.2f, 0f);
            if (motor != null) motor.enabled = false;
            if (aim != null) aim.enabled = false;
            if (playerCc != null) playerCc.enabled = false;

            onShip = true;
            freefall = false;
            parachute = false;

            MatchManager.Instance?.BeginDrop();
        }

        private void Update()
        {
            if (player == null || dropship == null)
                return;

            if (onShip || freefall || parachute)
                dropship.position += shipDir * shipSpeed * Time.deltaTime;

            if (onShip)
            {
                // F / LMB leave the plane. Space stays dodge on the ground.
                if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0))
                    StartFreefall();
                return;
            }

            if (freefall || parachute)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 steer = new Vector3(h, 0f, v);
                if (steer.sqrMagnitude > 1f) steer.Normalize();

                float fall = parachute ? parachuteSpeed : freefallSpeed;
                player.position += (steer * 10f + Vector3.down * fall) * Time.deltaTime;

                if (!parachute && GetHeightAboveGround() <= parachuteDeployHeight)
                {
                    parachute = true;
                    freefall = false;
                    SpawnParachuteVisual();
                }

                if (GetHeightAboveGround() < 1.2f)
                    Land();
            }
        }

        private void StartFreefall()
        {
            onShip = false;
            freefall = true;
            player.SetParent(null);
        }

        private float GetHeightAboveGround()
        {
            if (Physics.Raycast(player.position + Vector3.up, Vector3.down, out RaycastHit hit, 200f))
                return hit.distance - 1f;
            return 100f;
        }

        private void SpawnParachuteVisual()
        {
            var chute = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chute.name = "Parachute";
            Object.Destroy(chute.GetComponent<Collider>());
            chute.transform.SetParent(player, false);
            chute.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            chute.transform.localScale = new Vector3(2.4f, 0.2f, 2.4f);
            chute.GetComponent<Renderer>().material = BlockyCharacterBuilder.CreateLitMaterial(new Color(0.9f, 0.85f, 0.3f));
            Object.Destroy(chute, 8f);
        }

        private void Land()
        {
            freefall = false;
            parachute = false;
            if (playerCc != null) playerCc.enabled = true;
            if (motor != null) motor.enabled = true;
            if (aim != null) aim.enabled = true;

            // Snap to ground
            if (Physics.Raycast(player.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
                player.position = hit.point + Vector3.up * 0.05f;

            MatchManager.Instance?.BeginPlay();
            MatchStateMachine.Instance?.NotifyLanded();
            enabled = false;
        }
    }
}
