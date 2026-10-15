using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Player;

namespace BlockyBattleRoyale.Items
{
    public class ThrowableProjectile : MonoBehaviour
    {
        private ItemData data;
        private float explodeAt;
        private GameObject owner;
        private bool exploded;

        public static void Spawn(Transform thrower, ItemData item)
        {
            if (thrower == null || item == null)
                return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = item.throwableType == ThrowableType.Smoke ? "SmokeGrenade" : "Grenade";
            Object.Destroy(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.2f;
            go.transform.position = thrower.position + Vector3.up * 1.2f + thrower.forward * 0.6f;
            go.transform.localScale = Vector3.one * 0.35f;

            var rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Vector3 dir = thrower.forward;
            var aim = thrower.GetComponent<PlayerAim>();
            if (aim != null)
            {
                dir = (aim.AimPoint - go.transform.position).normalized;
            }
            rb.AddForce(dir * item.throwForce + Vector3.up * 3f, ForceMode.VelocityChange);

            var proj = go.AddComponent<ThrowableProjectile>();
            proj.data = item;
            proj.owner = thrower.gameObject;
            proj.explodeAt = Time.time + item.explodeDelay;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = item.throwableType == ThrowableType.Smoke
                    ? new Color(0.7f, 0.7f, 0.75f)
                    : new Color(0.25f, 0.45f, 0.25f);
            }
        }

        private void Update()
        {
            if (exploded || Time.time < explodeAt)
                return;
            Explode();
        }

        private void Explode()
        {
            exploded = true;
            if (data == null)
            {
                Destroy(gameObject);
                return;
            }

            if (data.throwableType == ThrowableType.Smoke)
            {
                SmokeCloud.Spawn(transform.position, data.smokeRadius, data.smokeDuration);
            }
            else
            {
                var hits = Physics.OverlapSphere(transform.position, data.explodeRadius);
                foreach (var h in hits)
                {
                    var health = h.GetComponentInParent<Health>();
                    if (health == null)
                        continue;

                    float dist = Vector3.Distance(transform.position, health.transform.position);
                    float falloff = 1f - Mathf.Clamp01(dist / data.explodeRadius);
                    health.ApplyDamage(new DamageInfo
                    {
                        Amount = data.explodeDamage * falloff,
                        BodyMultiplier = 1f,
                        HitPoint = health.transform.position,
                        HitNormal = Vector3.up,
                        Attacker = owner,
                        Hitbox = HitboxType.Body
                    });
                }

                var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(flash.GetComponent<Collider>());
                flash.transform.position = transform.position;
                flash.transform.localScale = Vector3.one * data.explodeRadius * 0.5f;
                var r = flash.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Sprites/Default"));
                    r.material.color = new Color(1f, 0.5f, 0.1f, 0.5f);
                }
                Destroy(flash, 0.25f);
            }

            Destroy(gameObject);
        }
    }

    public class SmokeCloud : MonoBehaviour
    {
        private float endTime;
        private float radius;

        public float Radius => radius;
        public static readonly System.Collections.Generic.List<SmokeCloud> Active = new System.Collections.Generic.List<SmokeCloud>();

        public static void Spawn(Vector3 pos, float radius, float duration)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SmokeCloud";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 1.2f;
            go.transform.localScale = Vector3.one * radius * 2f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.75f, 0.75f, 0.78f, 0.45f);
                r.material = mat;
            }

            var cloud = go.AddComponent<SmokeCloud>();
            cloud.radius = radius;
            cloud.endTime = Time.time + duration;
            Active.Add(cloud);
            Object.Destroy(go, duration);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        public static bool IsInsideAny(Vector3 worldPos)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var c = Active[i];
                if (c == null) continue;
                if (Vector3.Distance(c.transform.position, worldPos) <= c.radius)
                    return true;
            }
            return false;
        }

        private void Update()
        {
            if (Time.time >= endTime)
                Destroy(gameObject);
        }
    }
}
