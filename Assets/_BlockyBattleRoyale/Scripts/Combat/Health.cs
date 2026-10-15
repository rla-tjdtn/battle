using UnityEngine;
using BlockyBattleRoyale.Core;

namespace BlockyBattleRoyale.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private float destroyDelay = 2.5f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float Normalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public bool IsAlive => currentHealth > 0f;

        public System.Action<float, float> OnHealthChanged;
        public System.Action<DamageInfo> OnDamaged;
        public System.Action OnDeath;

        private bool isDead;
        private GameObject lastAttacker;

        public void Configure(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            currentHealth = maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyDamage(DamageInfo info)
        {
            if (isDead || info.Amount <= 0f)
                return;

            lastAttacker = info.Attacker;
            float raw = info.Amount * info.BodyMultiplier;
            var armor = GetComponent<ArmorSystem>();
            float finalDamage = armor != null ? armor.Mitigate(raw, info.Hitbox) : raw;
            currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

            OnDamaged?.Invoke(info);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                var kd = GetComponent<KnockdownSystem>();
                if (kd != null && kd.IsKnocked)
                {
                    kd.AbsorbDownedDamage(finalDamage);
                    currentHealth = 1f;
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    return;
                }
                if (kd != null && kd.TryEnterKnockdown())
                {
                    currentHealth = 1f;
                    isDead = false;
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    return;
                }
                Die();
            }
        }

        public void RestoreAfterRecall(float hp)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(hp, 1f, maxHealth);
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
            var aim = GetComponent<BlockyBattleRoyale.Player.PlayerAim>();
            if (aim != null) aim.enabled = true;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead)
                return;

            isDead = true;
            OnDeath?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterKill(lastAttacker, gameObject);
                GameManager.Instance.NotifyDeath(gameObject);
            }

            var motor = GetComponent<BlockyBattleRoyale.Player.PlayerMotor>();
            if (motor != null)
                motor.enabled = false;

            var aim = GetComponent<BlockyBattleRoyale.Player.PlayerAim>();
            if (aim != null)
                aim.enabled = false;

            var weapon = GetComponentInChildren<BlockyBattleRoyale.Weapons.WeaponController>();
            if (weapon != null)
                weapon.enabled = false;

            var animator = GetComponent<BlockyBattleRoyale.Player.BlockyAnimator>();
            if (animator != null)
                animator.PlayDeath();

            var cc = GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            var dummy = GetComponent<BlockyBattleRoyale.AI.DummyEnemyAI>();
            if (dummy != null)
                dummy.enabled = false;

            if (CompareTag("Player"))
            {
                var spectate = Object.FindFirstObjectByType<BlockyBattleRoyale.CameraSystem.SpectateCamera>();
                if (spectate == null)
                {
                    var cam = Camera.main;
                    if (cam != null)
                        spectate = cam.gameObject.AddComponent<BlockyBattleRoyale.CameraSystem.SpectateCamera>();
                }
                spectate?.BeginSpectate();
            }

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }

    public struct DamageInfo
    {
        public float Amount;
        public float BodyMultiplier;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public GameObject Attacker;
        public HitboxType Hitbox;
    }

    public enum HitboxType
    {
        Body,
        Head,
        Limb
    }
}
