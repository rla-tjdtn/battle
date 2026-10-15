using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Weapons;
using BlockyBattleRoyale.InventorySystem;
// KnockdownSystem / BoostSystem / WeaponLoadout added on build

namespace BlockyBattleRoyale.Player
{
    public static class BlockyCharacterBuilder
    {
        public static GameObject Build(string name, Vector3 position, Color clothingColor, bool isPlayer, WeaponData weaponData = null)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // Proportions ~2.7 head-body ratio, chunky blocky look
            var pelvis = CreatePart(visual.transform, "Pelvis", new Vector3(0f, 0.55f, 0f), new Vector3(0.55f, 0.35f, 0.35f), clothingColor);
            var torso = CreatePart(visual.transform, "Torso", new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.55f, 0.4f), clothingColor);
            CreatePart(visual.transform, "Pack", new Vector3(0f, 1.02f, -0.28f), new Vector3(0.42f, 0.36f, 0.16f), Darken(clothingColor, 0.55f));
            var head = CreatePart(visual.transform, "Head", new Vector3(0f, 1.55f, 0.02f), new Vector3(0.72f, 0.68f, 0.68f), new Color(1f, 0.85f, 0.7f));

            // Face
            CreatePart(head.transform, "EyeL", new Vector3(-0.16f, 0.08f, 0.56f), new Vector3(0.14f, 0.1f, 0.06f), Color.black);
            CreatePart(head.transform, "EyeR", new Vector3(0.16f, 0.08f, 0.56f), new Vector3(0.14f, 0.1f, 0.06f), Color.black);
            CreatePart(head.transform, "Mouth", new Vector3(0f, -0.14f, 0.55f), new Vector3(0.22f, 0.05f, 0.05f), new Color(0.35f, 0.15f, 0.15f));

            var armL = CreatePart(visual.transform, "ArmL", new Vector3(-0.48f, 0.95f, 0f), new Vector3(0.22f, 0.55f, 0.22f), clothingColor);
            var armR = CreatePart(visual.transform, "ArmR", new Vector3(0.48f, 0.95f, 0f), new Vector3(0.22f, 0.55f, 0.22f), clothingColor);
            CreatePart(armL.transform, "HandL", new Vector3(0f, -0.38f, 0f), new Vector3(0.18f, 0.18f, 0.18f), new Color(1f, 0.85f, 0.7f));
            var handR = CreatePart(armR.transform, "HandR", new Vector3(0f, -0.38f, 0f), new Vector3(0.18f, 0.18f, 0.18f), new Color(1f, 0.85f, 0.7f));

            var legL = CreatePart(visual.transform, "LegL", new Vector3(-0.18f, 0.22f, 0f), new Vector3(0.24f, 0.45f, 0.28f), Darken(clothingColor, 0.75f));
            var legR = CreatePart(visual.transform, "LegR", new Vector3(0.18f, 0.22f, 0f), new Vector3(0.24f, 0.45f, 0.28f), Darken(clothingColor, 0.75f));
            CreatePart(legL.transform, "FootL", new Vector3(0f, -0.32f, 0.06f), new Vector3(0.26f, 0.16f, 0.4f), new Color(0.45f, 0.34f, 0.2f));
            CreatePart(legR.transform, "FootR", new Vector3(0f, -0.32f, 0.06f), new Vector3(0.26f, 0.16f, 0.4f), new Color(0.45f, 0.34f, 0.2f));

            // Slight personality tilt
            head.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            torso.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);

            var weaponVisual = CreateWeaponVisual(handR.transform, weaponData);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(weaponVisual.transform, false);
            muzzle.localPosition = weaponData != null ? weaponData.muzzleLocalOffset : new Vector3(0f, 0.05f, 0.45f);

            var health = root.AddComponent<Health>();
            health.Configure(100f);
            root.AddComponent<ArmorSystem>();
            root.AddComponent<BlockyBattleRoyale.InventorySystem.PlayerInventory>();
            root.AddComponent<HealUser>();
            root.AddComponent<BoostSystem>();
            root.AddComponent<KnockdownSystem>();
            root.AddComponent<WeaponLoadout>();
            root.AddComponent<BlockyBattleRoyale.Visibility.GrassStealth>();
            root.AddComponent<DeathLoot>();

            // Capsule collider / CharacterController
            if (isPlayer)
            {
                root.tag = "Player";
                var cc = root.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.skinWidth = 0.05f;
                cc.minMoveDistance = 0f;

                root.AddComponent<PlayerMotor>();
                root.AddComponent<PlayerAim>();
                root.AddComponent<DuckovDodgeController>();
                root.AddComponent<BlockyBattleRoyale.Audio.FootstepSystem>();
                root.AddComponent<BlockyBattleRoyale.UI.TutorialHints>();
            }
            else
            {
                root.tag = "Enemy";
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.height = 1.8f;
                capsule.radius = 0.35f;
                capsule.center = new Vector3(0f, 0.9f, 0f);
                var rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.height = 1.8f;
                agent.radius = 0.4f;
                agent.speed = 4.2f;
                agent.stoppingDistance = 4f;
                agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            }

            AddHitbox(head, health, HitboxType.Head);
            AddHitbox(torso, health, HitboxType.Body);
            AddHitbox(pelvis, health, HitboxType.Body);
            AddHitbox(armL, health, HitboxType.Limb);
            AddHitbox(armR, health, HitboxType.Limb);
            AddHitbox(legL, health, HitboxType.Limb);
            AddHitbox(legR, health, HitboxType.Limb);

            var weapon = root.AddComponent<WeaponController>();
            weapon.SetOwner(root);
            weapon.SetMuzzle(muzzle);
            weapon.SetPlayerControlled(isPlayer);
            if (weaponData != null)
                weapon.SetWeaponData(weaponData);

            root.AddComponent<EquipmentVisual>();
            var animator = root.AddComponent<BlockyAnimator>();
            animator.Configure(armL.transform, armR.transform, legL.transform, legR.transform, visual.transform);

            return root;
        }

        private static GameObject CreateWeaponVisual(Transform parent, WeaponData data)
        {
            Color color = data != null ? data.weaponColor : new Color(0.18f, 0.18f, 0.2f);
            var gun = CreatePart(parent, "Weapon", new Vector3(0f, -0.05f, 0.25f), new Vector3(0.12f, 0.14f, 0.55f), color);
            gun.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            CreatePart(gun.transform, "Barrel", new Vector3(0f, 0.02f, 0.35f), new Vector3(0.07f, 0.07f, 0.28f), Darken(color, 0.8f));
            CreatePart(gun.transform, "Grip", new Vector3(0f, -0.12f, -0.05f), new Vector3(0.08f, 0.2f, 0.12f), Darken(color, 0.7f));
            return gun;
        }

        private static GameObject CreatePart(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = CreateLitMaterial(color);
                rend.sharedMaterial = mat;
            }

            return go;
        }

        private static void AddHitbox(GameObject part, Health health, HitboxType type)
        {
            var box = part.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            box.isTrigger = false;
            var hitbox = part.AddComponent<Hitbox>();
            hitbox.Configure(health, type);
        }

        public static Material CreateLitMaterial(Color color)
        {
            Shader shader = Shader.Find("BlockyRoyale/StylizedLit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.25f);
            return mat;
        }

        private static Color Darken(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }
    }
}
