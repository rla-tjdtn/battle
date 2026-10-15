using UnityEngine;

namespace BlockyBattleRoyale.Vehicles
{
    public class VehicleRider : MonoBehaviour
    {
        public bool IsSeated { get; set; }
        public bool IsDriver { get; set; }
        public SimpleVehicle Vehicle { get; set; }
    }
}
