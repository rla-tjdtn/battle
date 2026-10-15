using UnityEngine;
using UnityEngine.AI;
using BlockyBattleRoyale.BattleRoyale;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.Visibility;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.AI
{
    public enum BotDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class BotBrain : MonoBehaviour
    {
        [SerializeField] private BotDifficulty difficulty = BotDifficulty.Normal;
        [SerializeField] private float viewRange = 35f;
        [SerializeField] private float attackRange = 22f;
        [SerializeField] private float thinkInterval = 0.35f;

        private NavMeshAgent agent;
        private Health health;
        private WeaponController weapon;
        private HealUser healUser;
        private float nextThink;
        private Transform currentTarget;
        private Vector3 lastKnownTargetPos;
        private bool hasLastKnown;
        private float accuracy = 0.7f;
        private float reaction = 0.4f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            weapon = GetComponent<WeaponController>();
            healUser = GetComponent<HealUser>();
            ApplyDifficulty();
        }

        private void OnEnable()
        {
            GunshotBus.OnShot += HearGunshot;
        }

        private void OnDisable()
        {
            GunshotBus.OnShot -= HearGunshot;
        }

        private void HearGunshot(Vector3 pos, float radius, bool suppressed)
        {
            if (health != null && !health.IsAlive) return;
            if (Vector3.Distance(transform.position, pos) > radius) return;
            lastKnownTargetPos = pos;
            hasLastKnown = true;
            if (agent != null && agent.isOnNavMesh && currentTarget == null)
            {
                agent.isStopped = false;
                agent.SetDestination(pos);
            }
        }

        private void Start()
        {
            AssignTeam();
            if (agent != null)
            {
                agent.speed = difficulty == BotDifficulty.Hard ? 5.5f : 4.2f;
                agent.angularSpeed = 240f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 4f;
            }
        }

        private static int alliesLeft = -1;

        public static void ResetTeams()
        {
            alliesLeft = -1;
        }

        private void AssignTeam()
        {
            var kd = GetComponent<KnockdownSystem>();
            if (kd == null) return;
            if (Core.GameSettings.SquadSize <= 1)
            {
                kd.TeamId = gameObject.GetHashCode();
                return;
            }
            if (alliesLeft < 0)
                alliesLeft = Mathf.Max(0, Core.GameSettings.SquadSize - 1);
            if (CompareTag("Player"))
            {
                kd.TeamId = 1;
                return;
            }
            if (alliesLeft > 0)
            {
                alliesLeft--;
                kd.TeamId = 1;
            }
            else kd.TeamId = 2;
        }

        private void ApplyDifficulty()
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    accuracy = 0.45f;
                    reaction = 0.7f;
                    thinkInterval = 0.55f;
                    viewRange = 28f;
                    break;
                case BotDifficulty.Hard:
                    accuracy = 0.9f;
                    reaction = 0.18f;
                    thinkInterval = 0.2f;
                    viewRange = 42f;
                    break;
                default:
                    accuracy = 0.7f;
                    reaction = 0.35f;
                    thinkInterval = 0.35f;
                    viewRange = 35f;
                    break;
            }
        }

        public void SetDifficulty(BotDifficulty d)
        {
            difficulty = d;
            ApplyDifficulty();
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                if (agent != null && agent.enabled)
                    agent.isStopped = true;
                return;
            }

            if (Time.time < nextThink)
                return;
            nextThink = Time.time + thinkInterval + Random.Range(0f, reaction * 0.5f);

            EnsureZoneSafety();
            if (health != null && health.Normalized < 0.35f)
                TryHealOrFlee();

            AcquireTarget();
            if (currentTarget != null)
                FightTarget();
            else
                LootOrRoam();
        }

        private void EnsureZoneSafety()
        {
            if (ZoneManager.Instance == null || agent == null || !agent.isOnNavMesh)
                return;

            if (!ZoneManager.Instance.IsInside(transform.position))
            {
                Vector3 safe = ZoneManager.Instance.GetSafePoint(8f);
                safe.y = transform.position.y;
                agent.isStopped = false;
                agent.SetDestination(safe);
            }
        }

        private void TryHealOrFlee()
        {
            if (healUser != null)
                healUser.TryUseBestHeal();

            if (agent != null && agent.isOnNavMesh && currentTarget != null)
            {
                Vector3 away = transform.position - currentTarget.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.1f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(transform.position + away.normalized * 12f);
                }
            }
        }

        private void AcquireTarget()
        {
            currentTarget = null;
            float best = viewRange;
            var healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            foreach (var h in healths)
            {
                if (h == null || h.gameObject == gameObject || !h.IsAlive)
                    continue;
                if (h.transform.root == transform.root)
                    continue;
                var mine = GetComponent<KnockdownSystem>();
                var theirs = h.GetComponent<KnockdownSystem>();
                if (mine != null && theirs != null && mine.TeamId > 0 && mine.TeamId == theirs.TeamId)
                    continue;
                var kd = h.GetComponent<KnockdownSystem>();
                if (kd != null && kd.IsKnocked)
                    continue;

                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist > best)
                    continue;

                if (!VisibilityUtility.CanSee(transform, h.transform, viewRange))
                    continue;

                best = dist;
                currentTarget = h.transform;
                lastKnownTargetPos = h.transform.position;
                hasLastKnown = true;
            }
        }

        private void FightTarget()
        {
            if (currentTarget == null)
                return;

            float dist = Vector3.Distance(transform.position, currentTarget.position);
            Vector3 flat = currentTarget.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 8f * Time.deltaTime);

            bool canNav = agent != null && agent.enabled && agent.isOnNavMesh;

            if (dist > attackRange * 0.7f)
            {
                if (canNav)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentTarget.position);
                }
                else
                {
                    transform.position += flat.normalized * (3.5f * Time.deltaTime);
                }
            }
            else
            {
                if (canNav)
                    agent.isStopped = true;

                if (Random.value <= accuracy && weapon != null)
                {
                    Vector3 aim = currentTarget.position + Vector3.up * 1.1f;
                    aim += Random.insideUnitSphere * (1.5f * (1f - accuracy));
                    weapon.TryFireAt(aim);
                }
            }
        }

        private void LootOrRoam()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            var pickups = FindObjectsByType<WorldPickup>(FindObjectsSortMode.None);
            WorldPickup nearest = null;
            float best = 25f;
            foreach (var p in pickups)
            {
                if (p == null) continue;
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = p;
                }
            }

            if (nearest != null)
            {
                agent.isStopped = false;
                agent.SetDestination(nearest.transform.position);
                if (best < 1.6f)
                    nearest.TryPickup(gameObject);
                return;
            }

            if (hasLastKnown)
            {
                agent.isStopped = false;
                agent.SetDestination(lastKnownTargetPos);
                if (Vector3.Distance(transform.position, lastKnownTargetPos) < 2f)
                    hasLastKnown = false;
                return;
            }

            if (!agent.hasPath || agent.remainingDistance < 1.5f)
            {
                Vector3 dest = ZoneManager.Instance != null
                    ? ZoneManager.Instance.GetSafePoint(10f)
                    : transform.position + Random.insideUnitSphere * 20f;
                dest.y = transform.position.y;
                agent.SetDestination(dest);
            }
        }
    }
}
