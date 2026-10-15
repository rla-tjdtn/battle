using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Player;

namespace BlockyBattleRoyale.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private bool isPlayerControlled = true;

        private int currentAmmo;
        private float nextFireTime;
        private bool isReloading;
        private float reloadEndTime;
        private PlayerAim aim;
        private PlayerMotor motor;
        private GameObject owner;
        private float currentRecoil;
        private LineRenderer tracer;
        private float tracerHideTime;
        private float attachSpreadMul = 1f;
        private float attachRecoilMul = 1f;
        private float attachRangeBonus;
        private int attachMagBonus;
        private int fireModeIndex;
        private bool holstered;

        public bool IsHolstered => holstered;

        public void SetHolstered(bool value)
        {
            holstered = value;
            var visual = FindDeepChild(transform, "Weapon");
            if (visual != null)
                visual.gameObject.SetActive(!holstered && (weaponData == null || !weaponData.isMelee || weaponData.weaponName != "Fists"));
            OnAmmoChanged?.Invoke();
        }

        public void ToggleHolster() => SetHolstered(!holstered);

        public WeaponData Data => weaponData;
        public int CurrentAmmo => currentAmmo;
        public int MagazineSize => weaponData != null ? weaponData.magazineSize + attachMagBonus : 0;
        public bool IsReloading => isReloading;
        public Transform Muzzle => muzzlePoint;

        public void SetAttachmentModifiers(float spreadMul, float recoilMul, float rangeBonus, int magBonus)
        {
            attachSpreadMul = Mathf.Max(0.2f, spreadMul);
            attachRecoilMul = Mathf.Max(0.2f, recoilMul);
            attachRangeBonus = rangeBonus;
            attachMagBonus = magBonus;
            OnAmmoChanged?.Invoke();
        }

        public System.Action OnFired;
        public System.Action OnReloadStarted;
        public System.Action OnReloadFinished;
        public System.Action OnAmmoChanged;

        private void Awake()
        {
            owner = transform.root.gameObject;
            aim = owner.GetComponent<PlayerAim>();
            motor = owner.GetComponent<PlayerMotor>();
            EnsureTracer();
        }

        private void Start()
        {
            if (weaponData != null)
            {
                currentAmmo = MagazineSize;
                OnAmmoChanged?.Invoke();
            }
        }

        private void Update()
        {
            if (currentRecoil > 0f)
                currentRecoil = Mathf.MoveTowards(currentRecoil, 0f, Time.deltaTime * 8f);

            if (tracer != null && Time.time >= tracerHideTime)
                tracer.enabled = false;

            if (isReloading && Time.time >= reloadEndTime)
                FinishReload();

            if (!isPlayerControlled)
                return;

            var health = owner.GetComponent<Health>();
            if (health != null && !health.IsAlive)
                return;

            if (holstered || weaponData == null)
                return;

            if (Core.InputContext.BlockCombat)
                return;

            var rider = owner.GetComponent<BlockyBattleRoyale.Vehicles.VehicleRider>();
            if (rider != null && rider.IsDriver)
                return;

            bool fireHeld = Input.GetMouseButton(0);
            bool fireDown = Input.GetMouseButtonDown(0);

            if (!weaponData.isMelee && Input.GetKeyDown(KeyCode.R))
                TryReload();

            if (!weaponData.isMelee && Input.GetKeyDown(KeyCode.B))
                CycleFireMode();

            if (weaponData.isMelee)
            {
                if (fireDown)
                    TryMelee();
                return;
            }

            var mode = GetEffectiveFireMode();
            switch (mode)
            {
                case WeaponFireMode.FullAuto:
                    if (fireHeld)
                        TryFire();
                    break;
                default:
                    if (fireDown)
                        TryFire();
                    break;
            }
        }

        private void CycleFireMode()
        {
            if (weaponData == null) return;
            if (weaponData.fireMode == WeaponFireMode.Shotgun) return;
            fireModeIndex = (fireModeIndex + 1) % 2;
            // 0 = native, 1 = force semi if auto
        }

        private WeaponFireMode GetEffectiveFireMode()
        {
            if (weaponData == null) return WeaponFireMode.SemiAuto;
            if (weaponData.fireMode == WeaponFireMode.FullAuto && fireModeIndex == 1)
                return WeaponFireMode.SemiAuto;
            return weaponData.fireMode;
        }

        public void SetWeaponData(WeaponData data)
        {
            weaponData = data;
            fireModeIndex = 0;
            if (data != null)
            {
                currentAmmo = data.isMelee ? 1 : MagazineSize;
                OnAmmoChanged?.Invoke();
            }
            else
            {
                currentAmmo = 0;
                OnAmmoChanged?.Invoke();
            }

            // Toggle held weapon mesh
            var visual = transform.Find("Visual/ArmR/HandR/Weapon");
            if (visual == null)
                visual = FindDeepChild(transform, "Weapon");
            if (visual != null)
                visual.gameObject.SetActive(data == null || !data.isMelee || data.weaponName != "Fists");
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        public void SetMuzzle(Transform muzzle)
        {
            muzzlePoint = muzzle;
        }

        public void SetPlayerControlled(bool value)
        {
            isPlayerControlled = value;
        }

        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
            aim = owner.GetComponent<PlayerAim>();
            motor = owner.GetComponent<PlayerMotor>();
        }

        public bool TryMelee()
        {
            if (weaponData == null || !weaponData.isMelee || Time.time < nextFireTime)
                return false;

            nextFireTime = Time.time + (1f / Mathf.Max(0.5f, weaponData.fireRate));
            Vector3 origin = transform.position + Vector3.up * 1.1f + transform.forward * 0.4f;
            float range = weaponData.meleeRange;
            float radius = weaponData.meleeRadius;

            // Prefer aim direction
            Vector3 dir = transform.forward;
            if (aim != null)
            {
                Vector3 to = aim.AimPoint - origin;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    dir = to.normalized;
            }

            bool hitSomeone = false;
            var cols = Physics.OverlapSphere(origin + dir * (range * 0.55f), radius, hitMask, QueryTriggerInteraction.Ignore);
            foreach (var col in cols)
            {
                if (col == null) continue;
                var hitbox = col.GetComponent<Hitbox>();
                var health = hitbox != null ? hitbox.Health : col.GetComponentInParent<Health>();
                if (health == null || health.gameObject == owner) continue;

                health.ApplyDamage(new DamageInfo
                {
                    Amount = weaponData.damage,
                    BodyMultiplier = hitbox != null ? hitbox.GetMultiplier() : 1f,
                    HitPoint = col.ClosestPoint(origin),
                    HitNormal = -dir,
                    Attacker = owner,
                    Hitbox = hitbox != null ? hitbox.Type : HitboxType.Body
                });
                SpawnHitEffect(col.ClosestPoint(origin), -dir, true);
                hitSomeone = true;
            }

            // Punch visual
            ShowMeleeSwing(origin, dir);
            var anim = owner.GetComponent<BlockyAnimator>();
            anim?.PlayPunch();
            OnFired?.Invoke();
            OnAmmoChanged?.Invoke();
            return hitSomeone;
        }

        private void ShowMeleeSwing(Vector3 origin, Vector3 dir)
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "MeleeSwing";
            Object.Destroy(fx.GetComponent<Collider>());
            fx.transform.position = origin + dir * 0.7f;
            fx.transform.localScale = Vector3.one * 0.35f;
            var rend = fx.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = new Color(1f, 1f, 1f, 0.45f);
            }
            Object.Destroy(fx, 0.08f);
        }

        public bool TryFire()
        {
            if (weaponData == null || weaponData.isMelee)
                return TryMelee();

            if (isReloading || Time.time < nextFireTime)
                return false;

            if (currentAmmo <= 0)
            {
                TryReload();
                return false;
            }

            nextFireTime = Time.time + (1f / Mathf.Max(0.1f, weaponData.fireRate));
            currentAmmo--;
            OnAmmoChanged?.Invoke();

            int pellets = weaponData.fireMode == WeaponFireMode.Shotgun
                ? Mathf.Max(1, weaponData.pelletCount)
                : 1;

            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.2f;
            Vector3 aimPoint = aim != null ? aim.AimPoint : origin + transform.forward * weaponData.range;
            Vector3 baseDir = (aimPoint - origin).normalized;

            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplySpread(baseDir);
                FireHitscan(origin, dir);
            }

            currentRecoil += weaponData.recoilPitch * attachRecoilMul;
            if (aim != null)
                aim.AddRecoil(weaponData.recoilPitch * attachRecoilMul, weaponData.recoilYaw * attachRecoilMul);

            ShowMuzzleFlash(origin, baseDir);
            ReportGunshot(origin);
            owner.GetComponent<BlockyBattleRoyale.Player.BlockyAnimator>()?.PlayShoot();
            OnFired?.Invoke();

            if (currentAmmo <= 0)
                TryReload();

            return true;
        }

        public void TryFireAt(Vector3 worldTarget)
        {
            if (weaponData != null && weaponData.isMelee)
            {
                TryMelee();
                return;
            }

            if (weaponData == null || isReloading || Time.time < nextFireTime || currentAmmo <= 0)
                return;

            nextFireTime = Time.time + (1f / Mathf.Max(0.1f, weaponData.fireRate));
            currentAmmo--;
            OnAmmoChanged?.Invoke();

            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.2f;
            Vector3 dir = ApplySpread((worldTarget - origin).normalized);
            FireHitscan(origin, dir);
            ShowMuzzleFlash(origin, dir);
            ReportGunshot(origin);
            OnFired?.Invoke();
        }

        public void TryReload()
        {
            if (weaponData == null || weaponData.isMelee || isReloading)
                return;

            if (currentAmmo >= MagazineSize)
                return;

            isReloading = true;
            reloadEndTime = Time.time + weaponData.reloadTime;
            OnReloadStarted?.Invoke();
        }

        private void FinishReload()
        {
            isReloading = false;
            currentAmmo = MagazineSize;
            OnAmmoChanged?.Invoke();
            OnReloadFinished?.Invoke();
        }

        private Vector3 ApplySpread(Vector3 direction)
        {
            float spread = weaponData.hipSpread;
            if (aim != null && aim.IsAiming)
                spread = weaponData.aimSpread;

            if (motor != null && motor.IsMoving)
                spread *= weaponData.moveSpreadMultiplier;

            spread *= attachSpreadMul;
            spread += currentRecoil * 0.35f * attachRecoilMul;

            Quaternion spreadRot = Quaternion.Euler(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f);

            return (spreadRot * direction).normalized;
        }

        private void FireHitscan(Vector3 origin, Vector3 direction)
        {
            float range = weaponData.range + attachRangeBonus;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                ShowTracer(origin, hit.point);

                var hitbox = hit.collider.GetComponent<Hitbox>();
                var health = hitbox != null ? hitbox.Health : hit.collider.GetComponentInParent<Health>();

                if (health != null && health.gameObject != owner)
                {
                    var info = new DamageInfo
                    {
                        Amount = weaponData.damage,
                        BodyMultiplier = hitbox != null ? hitbox.GetMultiplier() : 1f,
                        HitPoint = hit.point,
                        HitNormal = hit.normal,
                        Attacker = owner,
                        Hitbox = hitbox != null ? hitbox.Type : HitboxType.Body
                    };
                    health.ApplyDamage(info);
                    SpawnHitEffect(hit.point, hit.normal, true);
                }
                else
                {
                    var tire = hit.collider.GetComponent<BlockyBattleRoyale.Vehicles.VehicleTire>();
                    if (tire != null)
                        tire.Hit(weaponData.damage);
                    else
                    {
                        var car = hit.collider.GetComponentInParent<BlockyBattleRoyale.Vehicles.SimpleVehicle>();
                        car?.ApplyDamage(weaponData.damage * 0.35f);
                    }
                    var dest = hit.collider.GetComponentInParent<BlockyBattleRoyale.World.DestructibleProp>();
                    dest?.OnBulletHit(weaponData.damage);
                    SpawnHitEffect(hit.point, hit.normal, false);
                }
            }
            else
            {
                ShowTracer(origin, origin + direction * range);
            }
        }

        private void ShowTracer(Vector3 start, Vector3 end)
        {
            EnsureTracer();
            if (tracer == null)
                return;

            tracer.enabled = true;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracerHideTime = Time.time + 0.05f;
        }

        private void EnsureTracer()
        {
            if (tracer != null)
                return;

            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            tracer = go.AddComponent<LineRenderer>();
            tracer.positionCount = 2;
            tracer.startWidth = 0.03f;
            tracer.endWidth = 0.01f;
            tracer.material = new Material(Shader.Find("Sprites/Default"));
            tracer.startColor = new Color(1f, 0.85f, 0.3f, 0.9f);
            tracer.endColor = new Color(1f, 0.6f, 0.1f, 0.1f);
            tracer.enabled = false;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;
        }

        private void ShowMuzzleFlash(Vector3 origin, Vector3 direction)
        {
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "MuzzleFlash";
            Object.Destroy(flash.GetComponent<Collider>());
            flash.transform.position = origin + direction * 0.05f;
            flash.transform.localScale = Vector3.one * 0.18f;
            var rend = flash.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = new Color(1f, 0.75f, 0.2f, 1f);
            }
            Object.Destroy(flash, 0.05f);
        }

        private void SpawnHitEffect(Vector3 point, Vector3 normal, bool isCharacter)
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = isCharacter ? "HitBlood" : "HitSpark";
            Object.Destroy(fx.GetComponent<Collider>());
            fx.transform.position = point + normal * 0.05f;
            fx.transform.localScale = Vector3.one * (isCharacter ? 0.18f : 0.12f);
            var rend = fx.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                Color c = isCharacter ? new Color(0.75f, 0.18f, 0.16f, 1f) : new Color(0.72f, 0.66f, 0.48f, 1f);
                rend.material.color = c;
                fx.transform.localScale = Vector3.one * (isCharacter ? 0.22f : 0.1f);
            }
            Object.Destroy(fx, 0.2f);
        }

        private void ReportGunshot(Vector3 origin)
        {
            float radius = 48f;
            bool suppressed = false;
            var loadout = owner != null ? owner.GetComponent<WeaponLoadout>() : null;
            if (loadout != null)
            {
                radius *= loadout.HearRangeMultiplier();
                suppressed = loadout.HasSuppressor();
            }
            GunshotBus.Report(origin, radius, suppressed);
            var listener = GameObject.FindGameObjectWithTag("Player");
            if (listener != null && Vector3.Distance(listener.transform.position, origin) <= radius)
                BlockyBattleRoyale.Audio.GameAudio.Instance?.PlayBeep(suppressed ? 220f : 95f, suppressed ? 0.03f : 0.08f, suppressed ? 0.04f : 0.1f);
            SyncSuppressorMesh(suppressed);
        }

        private void SyncSuppressorMesh(bool on)
        {
            if (muzzlePoint == null) return;
            var existing = muzzlePoint.Find("SuppressorMesh");
            if (!on)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }
            if (existing == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "SuppressorMesh";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(muzzlePoint, false);
                go.transform.localPosition = new Vector3(0f, 0f, 0.12f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
                existing = go.transform;
            }
            existing.gameObject.SetActive(true);
        }
    }
}
