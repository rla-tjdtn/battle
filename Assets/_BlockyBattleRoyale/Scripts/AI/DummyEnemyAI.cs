using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.AI
{
    /// <summary>
    /// Phase 1 placeholder combat dummy / light combatant.
    /// Full NavMesh battle AI arrives in Phase 5.
    /// </summary>
    public class DummyEnemyAI : MonoBehaviour
    {
        [SerializeField] private float detectRange = 28f;
        [SerializeField] private float fireRange = 22f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float fireInterval = 0.7f;

        private Health health;
        private WeaponController weapon;
        private Transform player;
        private float nextFire;
        private Vector3 spawnPos;
        private float wanderTimer;
        private Vector3 wanderTarget;

        private void Awake()
        {
            health = GetComponent<Health>();
            weapon = GetComponent<WeaponController>();
            spawnPos = transform.position;
            wanderTarget = spawnPos;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
                return;

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    player = p.transform;
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;
            bool canSee = dist <= detectRange && HasLineOfSight(player);

            if (canSee)
            {
                Vector3 flat = toPlayer;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                }

                if (dist > fireRange * 0.65f)
                {
                    Vector3 move = flat.normalized * moveSpeed * Time.deltaTime;
                    transform.position += move;
                }

                if (dist <= fireRange && Time.time >= nextFire)
                {
                    nextFire = Time.time + fireInterval;
                    Vector3 aim = player.position + Vector3.up * 1.1f;
                    weapon?.TryFireAt(aim);
                }
            }
            else
            {
                Wander();
            }
        }

        private void Wander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(2f, 4f);
                Vector2 offset = Random.insideUnitCircle * 6f;
                wanderTarget = spawnPos + new Vector3(offset.x, 0f, offset.y);
            }

            Vector3 dir = wanderTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.25f)
            {
                transform.position += dir.normalized * (moveSpeed * 0.55f) * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), turnSpeed * Time.deltaTime);
            }
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 dest = target.position + Vector3.up * 1.1f;
            Vector3 dir = dest - origin;
            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == target || hit.transform.IsChildOf(target) || hit.transform.root == target.root;
            }
            return true;
        }
    }
}
