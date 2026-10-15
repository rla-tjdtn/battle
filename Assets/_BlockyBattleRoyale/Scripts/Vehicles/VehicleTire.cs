using UnityEngine;

namespace BlockyBattleRoyale.Vehicles
{
    public class VehicleTire : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 35f;
        public int Index;
        public SimpleVehicle Vehicle;
        public bool Flat { get; private set; }
        public float Health { get; private set; }

        private void Awake()
        {
            Health = maxHealth;
        }

        public void Configure(SimpleVehicle vehicle, int index, float hp)
        {
            Vehicle = vehicle;
            Index = index;
            maxHealth = hp;
            Health = hp;
        }

        public void Hit(float damage)
        {
            if (Flat) return;
            Health -= damage;
            if (Health > 0f) return;
            Health = 0f;
            Flat = true;
            transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
            var r = GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.12f, 0.12f, 0.12f);
            Vehicle?.NotifyTire(Index);
        }
    }
}
