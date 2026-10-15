using UnityEngine;

namespace BlockyBattleRoyale.World
{
    public class DoorInteractable : MonoBehaviour
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float speed = 6f;
        [SerializeField] private bool isOpen;
        [SerializeField] private float interactRange = 2.5f;

        private Quaternion closedRot;
        private Quaternion openRot;
        private bool playerNear;
        private Collider leaf;

        public static DoorInteractable Nearest(Vector3 playerPos, float maxDistance)
        {
            DoorInteractable best = null;
            float bestDist = maxDistance;
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None))
            {
                float dist = Vector3.Distance(playerPos, door.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = door;
                }
            }
            return best;
        }

        public static bool BlocksPickup(Vector3 playerPos)
        {
            return Nearest(playerPos, 2.6f) != null;
        }

        private void Awake()
        {
            if (doorPivot == null)
                doorPivot = transform;
            EnsureHinge();
            closedRot = doorPivot.localRotation;
            openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
            leaf = GetComponent<Collider>();
            interactRange = Mathf.Max(interactRange, 3.2f);
        }

        private void EnsureHinge()
        {
            if (transform.parent != null && transform.parent.name == "DoorHinge")
            {
                doorPivot = transform.parent;
                return;
            }

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(transform.parent, false);
            Vector3 edge = transform.localPosition;
            edge.x -= transform.localScale.x * 0.5f;
            edge.y -= transform.localScale.y * 0.5f;
            hinge.transform.localPosition = edge;
            hinge.transform.localRotation = transform.localRotation;
            transform.SetParent(hinge.transform, true);
            doorPivot = hinge.transform;
            openAngle = -105f;
        }

        private void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector3.Distance(player.transform.position, transform.position);
                playerNear = dist <= interactRange;
                if (playerNear && Input.GetKeyDown(KeyCode.F))
                {
                    if (Nearest(player.transform.position, interactRange) != this)
                        return;
                    if (player.GetComponentInParent<BlockyBattleRoyale.Vehicles.SimpleVehicle>() != null)
                        return;
                    var kd = player.GetComponent<BlockyBattleRoyale.Combat.KnockdownSystem>();
                    if (kd != null)
                    {
                        bool nearDowned = false;
                        foreach (var other in Object.FindObjectsByType<BlockyBattleRoyale.Combat.KnockdownSystem>(FindObjectsSortMode.None))
                        {
                            if (other != kd && other.IsKnocked &&
                                Vector3.Distance(player.transform.position, other.transform.position) < 2.2f)
                            {
                                nearDowned = true;
                                break;
                            }
                        }
                        if (nearDowned) return;
                    }
                    Toggle();
                }
            }

            Quaternion target = isOpen ? openRot : closedRot;
            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * speed);
            if (leaf != null)
                leaf.enabled = !isOpen;
        }

        public void Toggle()
        {
            isOpen = !isOpen;
        }

        public void Configure(Transform pivot, float angle)
        {
            doorPivot = pivot != null ? pivot : transform;
            openAngle = angle;
            closedRot = doorPivot.localRotation;
            openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
        }
    }
}
