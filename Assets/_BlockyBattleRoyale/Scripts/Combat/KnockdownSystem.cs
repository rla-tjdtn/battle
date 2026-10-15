using UnityEngine;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Combat
{
    /// <summary>
    /// Solo: lethal. Duo/Squad-ready knockdown with revive interact (F hold).
    /// </summary>
    public class KnockdownSystem : MonoBehaviour
    {
        [SerializeField] private bool enableKnockdown;
        [SerializeField] private float bleedOutTime = 30f;
        [SerializeField] private float reviveTime = 10f;
        [SerializeField] private float crawlSpeed = 1.2f;
        [SerializeField] private float reviveHealth = 25f;
        [SerializeField] private float downedHealth = 100f;

        private Health health;
        private PlayerMotor motor;
        private WeaponController weapon;
        private bool knocked;
        private float bleedTimer;
        private float reviveProgress;
        private Vector3 reviveStart;
        private int knockdownCount;
        private float downedLeft;
        private bool recalled;
        private bool finishing;

        public bool IsKnocked => knocked;
        public bool WasRecalled => recalled;
        public float DownedLeft => downedLeft;
        public float ReviveProgress => reviveProgress;
        public int TeamId { get; set; }

        private void Awake()
        {
            health = GetComponent<Health>();
            motor = GetComponent<PlayerMotor>();
            weapon = GetComponent<WeaponController>();
            enableKnockdown = BlockyBattleRoyale.Core.GameSettings.KnockdownEnabled;
            if (CompareTag("Player"))
            {
                TeamId = BlockyBattleRoyale.Core.GameSettings.SquadSize <= 1 ? gameObject.GetHashCode() : 1;
                BlockyBattleRoyale.AI.BotBrain.ResetTeams();
            }
            if (health != null)
                health.OnDamaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDamaged -= OnDamaged;
        }

        private void OnDamaged(DamageInfo info)
        {
            // Health handles death; we intercept via checking next frame in Update if needed.
        }

        private void Update()
        {
            if (!enableKnockdown)
                return;

            if (knocked)
            {
                float drain = (100f / Mathf.Max(5f, bleedOutTime)) * (1f + knockdownCount * 0.35f);
                downedLeft -= drain * Time.deltaTime;
                bleedTimer = downedLeft;
                if (motor != null)
                    motor.enabled = false;
                if (!HasLivingTeammate())
                    downedLeft = 0f;

                // Crawl toward aim/input lightly
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 dir = new Vector3(h, 0f, v);
                if (dir.sqrMagnitude > 0.01f && CompareTag("Player"))
                {
                    dir.Normalize();
                    transform.position += dir * crawlSpeed * Time.deltaTime;
                }

                if (downedLeft <= 0f)
                    FinishBleedOut();
            }

            TryReviveOthers();
        }

        public bool TryEnterKnockdown()
        {
            if (finishing || !enableKnockdown || knocked)
                return false;

            knocked = true;
            knockdownCount++;
            downedLeft = downedHealth;
            bleedTimer = downedLeft;
            if (weapon != null) weapon.enabled = false;
            if (motor != null) motor.enabled = false;
            return true;
        }

        public void Revive()
        {
            knocked = false;
            if (health != null)
                health.Heal(reviveHealth);
            if (weapon != null) weapon.enabled = true;
            if (motor != null) motor.enabled = true;
        }

        private void FinishBleedOut()
        {
            finishing = true;
            knocked = false;
            // Force lethal by applying remaining damage
            if (health != null && health.IsAlive)
            {
                health.ApplyDamage(new DamageInfo
                {
                    Amount = 9999f,
                    BodyMultiplier = 1f,
                    Attacker = null,
                    HitPoint = transform.position,
                    HitNormal = Vector3.up,
                    Hitbox = HitboxType.Body
                });
            }
        }

        private void TryReviveOthers()
        {
            if (!CompareTag("Player") || knocked)
                return;

            KnockdownSystem nearest = null;
            float best = 2.2f;
            var all = FindObjectsByType<KnockdownSystem>(FindObjectsSortMode.None);
            foreach (var k in all)
            {
                if (k == this || !k.IsKnocked) continue;
                if (k.TeamId != TeamId) continue;
                float d = Vector3.Distance(transform.position, k.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = k;
                }
            }

            if (nearest != null && Input.GetKey(KeyCode.F))
            {
                if (reviveProgress <= 0f)
                    reviveStart = transform.position;
                if (Vector3.Distance(transform.position, reviveStart) > 0.35f)
                {
                    reviveProgress = 0f;
                    return;
                }
                reviveProgress += Time.deltaTime;
                if (reviveProgress >= reviveTime)
                {
                    nearest.Revive();
                    reviveProgress = 0f;
                }
            }
            else
            {
                reviveProgress = 0f;
            }
        }

        public void AbsorbDownedDamage(float amount)
        {
            if (!knocked) return;
            downedLeft -= Mathf.Max(0f, amount);
        }

        public void MarkRecalled()
        {
            recalled = true;
            knocked = false;
        }

        private bool HasLivingTeammate()
        {
            foreach (var k in FindObjectsByType<KnockdownSystem>(FindObjectsSortMode.None))
            {
                if (k == this || k.TeamId != TeamId) continue;
                var hp = k.GetComponent<Health>();
                if (hp != null && hp.IsAlive && !k.IsKnocked)
                    return true;
            }
            return false;
        }
    }
}
