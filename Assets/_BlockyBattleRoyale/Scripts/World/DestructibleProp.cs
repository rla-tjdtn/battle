using UnityEngine;
using BlockyBattleRoyale.Combat;

namespace BlockyBattleRoyale.World
{
    /// <summary>Wood crates / light fences — bullets and explosions can destroy.</summary>
    public class DestructibleProp : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 40f;
        [SerializeField] private float current;
        [SerializeField] private bool leaveDebris = true;

        private void Awake()
        {
            current = maxHealth;
        }

        public void Configure(float hp)
        {
            maxHealth = hp;
            current = hp;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || current <= 0f) return;
            current -= amount;
            if (current <= 0f)
                Break();
        }

        /// <summary>Hook from hitscan when collider has this component.</summary>
        public void OnBulletHit(float damage) => ApplyDamage(damage);

        private void Break()
        {
            if (leaveDebris)
            {
                for (int i = 0; i < 4; i++)
                {
                    var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bit.name = "Debris";
                    Object.Destroy(bit.GetComponent<Collider>());
                    bit.transform.position = transform.position + Random.insideUnitSphere * 0.3f;
                    bit.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
                    var r = bit.GetComponent<Renderer>();
                    if (r != null)
                        r.material.color = new Color(0.45f, 0.32f, 0.18f);
                    var rb = bit.AddComponent<Rigidbody>();
                    rb.AddExplosionForce(120f, transform.position, 2f);
                    Object.Destroy(bit, 2.5f);
                }
            }
            Destroy(gameObject);
        }
    }
}
