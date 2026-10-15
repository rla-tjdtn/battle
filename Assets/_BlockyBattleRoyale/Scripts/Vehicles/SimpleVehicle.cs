using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.InventorySystem;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Vehicles
{
    public class SimpleVehicle : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 14f;
        [SerializeField] private float turnSpeed = 70f;
        [SerializeField] private float maxHealth = 300f;
        [SerializeField] private float maxFuel = 40f;
        [SerializeField] private float driveFuelPerSecond = 1.1f;
        [SerializeField] private float idleFuelPerSecond = 0.12f;
        [SerializeField] private float tireHealth = 35f;
        [SerializeField] private int seatCount = 4;
        [SerializeField] private Transform seat;

        private readonly Transform[] seats = new Transform[4];
        private readonly Transform[] riders = new Transform[4];
        private readonly bool[] tireFlat = new bool[4];
        private float health;
        private float fuel;
        private Vector3 velocity;
        private int flatCount;
        private float fuelProgress;
        private Vector3 fuelStart;
        private bool fueling;

        public float Fuel => fuel;
        public float MaxFuel => maxFuel;
        public float VehicleHealth => health;
        public float MaxVehicleHealth => maxHealth;
        public int FlatTires => flatCount;
        public float Speed => new Vector3(velocity.x, 0f, velocity.z).magnitude;

        private void Awake()
        {
            health = maxHealth;
            fuel = maxFuel;
            BuildSeats();
            BuildTires();
        }

        private void BuildSeats()
        {
            Vector3[] local =
            {
                new Vector3(-0.25f, 0.55f, 0.15f),
                new Vector3(0.25f, 0.55f, 0.15f),
                new Vector3(-0.25f, 0.55f, -0.25f),
                new Vector3(0.25f, 0.55f, -0.25f)
            };
            int count = Mathf.Clamp(seatCount, 1, 4);
            for (int i = 0; i < 4; i++)
            {
                if (i >= count) break;
                var s = new GameObject(i == 0 ? "Seat_Driver" : "Seat_" + i);
                s.transform.SetParent(transform, false);
                s.transform.localPosition = local[i];
                seats[i] = s.transform;
            }
            if (seat == null) seat = seats[0];
        }

        private void BuildTires()
        {
            Vector3[] local =
            {
                new Vector3(-0.62f, -0.35f, 0.38f),
                new Vector3(0.62f, -0.35f, 0.38f),
                new Vector3(-0.62f, -0.35f, -0.38f),
                new Vector3(0.62f, -0.35f, -0.38f)
            };
            for (int i = 0; i < 4; i++)
            {
                var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = "Tire" + i;
                tire.transform.SetParent(transform, false);
                tire.transform.localPosition = local[i];
                tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tire.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);
                var comp = tire.AddComponent<VehicleTire>();
                comp.Configure(this, i, tireHealth);
            }
        }

        private void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            int localSeat = SeatOf(player != null ? player.transform : null);

            bool atDoor = player != null && BlockyBattleRoyale.World.DoorInteractable.BlocksPickup(player.transform.position);
            if (localSeat < 0 && player != null && !atDoor && Vector3.Distance(player.transform.position, transform.position) < 3.5f)
            {
                HandleOutside(player);
            }
            else if (localSeat >= 0)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    ExitSeat(localSeat);
                    return;
                }
                TrySwitchSeat(player.transform, localSeat);
            }

            bool driverPresent = riders[0] != null && fuel > 0f;
            float drive = 0f;
            float steer = 0f;
            if (driverPresent && localSeat == 0)
            {
                drive = Input.GetAxisRaw("Vertical");
                steer = Input.GetAxisRaw("Horizontal");
            }

            float tireMul = flatCount switch
            {
                0 => 1f,
                1 => 0.82f,
                2 => 0.6f,
                _ => 0.35f
            };
            float turnMul = flatCount == 0 ? 1f : flatCount == 1 ? 0.7f : 0.45f;
            if (driverPresent && Mathf.Abs(drive) > 0.01f)
            {
                fuel = Mathf.Max(0f, fuel - driveFuelPerSecond * Time.deltaTime);
                velocity = transform.forward * (drive * moveSpeed * tireMul);
            }
            else
            {
                if (riders[0] != null && fuel > 0f)
                    fuel = Mathf.Max(0f, fuel - idleFuelPerSecond * Time.deltaTime);
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, 10f * Time.deltaTime);
            }

            if (driverPresent)
            {
                float bias = 0f;
                if (tireFlat[0] || tireFlat[2]) bias += 8f;
                if (tireFlat[1] || tireFlat[3]) bias -= 8f;
                transform.Rotate(0f, (steer * turnSpeed * turnMul + bias) * Time.deltaTime, 0f);
            }

            transform.position += velocity * Time.deltaTime;
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
            }

            for (int i = 0; i < riders.Length; i++)
            {
                if (riders[i] != null && seats[i] != null)
                    riders[i].position = seats[i].position;
            }
        }

        private void HandleOutside(GameObject player)
        {
            var inv = player.GetComponent<PlayerInventory>();
            bool hasCan = inv != null && inv.CountItem(ItemCategory.Fuel) > 0 && fuel < maxFuel;
            if (!hasCan)
            {
                if (Input.GetKeyDown(KeyCode.F))
                    Enter(player.transform);
                fueling = false;
                return;
            }

            if (Input.GetKey(KeyCode.F))
            {
                if (!fueling)
                {
                    fueling = true;
                    fuelProgress = 0f;
                    fuelStart = player.transform.position;
                }
                if (Vector3.Distance(player.transform.position, fuelStart) > 0.45f)
                {
                    fueling = false;
                    return;
                }
                fuelProgress += Time.deltaTime;
                if (fuelProgress >= 2.5f)
                {
                    if (inv.ConsumeCategory(ItemCategory.Fuel, 1))
                        fuel = Mathf.Min(maxFuel, fuel + 20f);
                    fueling = false;
                }
            }
            else if (Input.GetKeyUp(KeyCode.F))
            {
                if (fuelProgress < 0.3f)
                    Enter(player.transform);
                fueling = false;
            }
        }

        private void TrySwitchSeat(Transform player, int from)
        {
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                return;
            int target = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1)) target = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) target = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) target = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) target = 3;
            if (target < 0 || target == from || seats[target] == null) return;
            if (riders[target] != null) return;
            riders[from] = null;
            riders[target] = player;
            Sit(player, target);
        }

        public void Enter(Transform player)
        {
            if (player == null) return;
            if (SeatOf(player) >= 0) return;
            int seatIndex = FirstFree();
            if (seatIndex < 0) return;
            riders[seatIndex] = player;
            Sit(player, seatIndex);
        }

        private void Sit(Transform player, int seatIndex)
        {
            var cc = player.GetComponent<CharacterController>();
            var motor = player.GetComponent<PlayerMotor>();
            var aim = player.GetComponent<PlayerAim>();
            var weapon = player.GetComponent<WeaponController>();
            if (cc != null) cc.enabled = false;
            if (motor != null) motor.enabled = false;
            if (aim != null) aim.enabled = true;
            bool driver = seatIndex == 0;
            if (weapon != null) weapon.enabled = !driver;
            var rider = player.GetComponent<VehicleRider>() ?? player.gameObject.AddComponent<VehicleRider>();
            rider.IsSeated = true;
            rider.IsDriver = driver;
            rider.Vehicle = this;
            player.SetParent(seats[seatIndex]);
            player.localPosition = Vector3.zero;
        }

        public void Exit()
        {
            for (int i = 0; i < riders.Length; i++)
                if (riders[i] != null && riders[i].CompareTag("Player"))
                    ExitSeat(i);
        }

        private void ExitSeat(int index)
        {
            var passenger = riders[index];
            riders[index] = null;
            if (passenger == null) return;
            passenger.SetParent(null);
            passenger.position = transform.position + transform.right * (2.2f + index * 0.1f) + Vector3.up * 0.1f;
            var cc = passenger.GetComponent<CharacterController>();
            var motor = passenger.GetComponent<PlayerMotor>();
            var weapon = passenger.GetComponent<WeaponController>();
            if (cc != null) cc.enabled = true;
            if (motor != null) motor.enabled = true;
            if (weapon != null) weapon.enabled = true;
            var rider = passenger.GetComponent<VehicleRider>();
            if (rider != null)
            {
                rider.IsSeated = false;
                rider.IsDriver = false;
                rider.Vehicle = null;
            }
        }

        private int SeatOf(Transform t)
        {
            if (t == null) return -1;
            for (int i = 0; i < riders.Length; i++)
                if (riders[i] == t) return i;
            return -1;
        }

        private int FirstFree()
        {
            for (int i = 0; i < seats.Length; i++)
                if (seats[i] != null && riders[i] == null) return i;
            return -1;
        }

        public void NotifyTire(int index)
        {
            if (index < 0 || index > 3 || tireFlat[index]) return;
            tireFlat[index] = true;
            flatCount++;
        }

        public void ApplyDamage(float amount)
        {
            health -= amount;
            if (health > 0f) return;
            for (int i = 0; i < riders.Length; i++)
                if (riders[i] != null) ExitSeat(i);
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || SeatOf(player.transform) < 0) return;
            string tires = "";
            for (int i = 0; i < 4; i++) tires += tireFlat[i] ? "X" : "O";
            GUI.Label(new Rect(20, Screen.height - 70, 420, 48),
                $"속도 {Speed:0.0}  내구 {health:0}/{maxHealth:0}  연료 {fuel:0}/{maxFuel:0}  타이어 {tires}");
        }
    }
}
