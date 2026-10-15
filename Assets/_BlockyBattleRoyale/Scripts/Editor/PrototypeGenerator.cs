#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlockyBattleRoyale.AI;
using BlockyBattleRoyale.CameraSystem;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.UI;
using BlockyBattleRoyale.Weapons;
using BlockyBattleRoyale.World;

namespace BlockyBattleRoyale.EditorTools
{
    public static class PrototypeGenerator
    {
        private const string RootPath = "Assets/_BlockyBattleRoyale";
        private const string ScenePath = RootPath + "/Scenes/GameWorld.unity";
        private const string WeaponFolder = RootPath + "/ScriptableObjects/Weapons";
        private const string MaterialFolder = RootPath + "/Materials";
        private const string PrefabFolder = RootPath + "/Prefabs/Characters";

        [MenuItem("Tools/Blocky Battle Royale/Generate Prototype", priority = 0)]
        public static void GeneratePrototype()
        {
            // Full multi-phase generator (Phases 1-9 systems)
            FullGameGenerator.GenerateFullGame();
        }

        [MenuItem("Tools/Blocky Battle Royale/Generate Prototype (Legacy Phase1 Only)", priority = 50)]
        public static void GeneratePrototypeLegacy()
        {
            EnsureFolders();
            EnsureTagsAndLayers();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameWorld";

            var pistol = CreateOrLoadWeapon("Pistol", WeaponFireMode.SemiAuto, AmmoType.Pistol, 28f, 3.5f, 12, 1.5f, 55f, 1.4f, 0.5f, 2.2f, 0.5f, 1);
            CreateOrLoadWeapon("SMG", WeaponFireMode.FullAuto, AmmoType.SMG, 18f, 10f, 30, 1.8f, 40f, 0.9f, 0.7f, 3.2f, 1.0f, 1);
            CreateOrLoadWeapon("AssaultRifle", WeaponFireMode.FullAuto, AmmoType.Rifle, 24f, 7f, 30, 2.0f, 70f, 1.1f, 0.45f, 2.4f, 0.7f, 1);
            CreateOrLoadWeapon("DMR", WeaponFireMode.SemiAuto, AmmoType.Rifle, 45f, 2.2f, 10, 2.2f, 100f, 2.0f, 0.35f, 1.6f, 0.35f, 1);
            CreateOrLoadWeapon("Shotgun", WeaponFireMode.Shotgun, AmmoType.Shotgun, 14f, 1.1f, 6, 2.4f, 25f, 2.5f, 1.2f, 6f, 2.5f, 8);

            BuildLighting();
            BuildGround();
            BuildEnvironment();

            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0f, 0.05f, 0f), new Color(0.22f, 0.48f, 0.88f), true, pistol);
            player.name = "Player";
            Selection.activeGameObject = player;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.78f);
            camGo.AddComponent<AudioListener>();
            var qv = camGo.AddComponent<QuarterViewCamera>();
            qv.SetTarget(player.transform);

            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();

            SpawnEnemies(pistol);
            BuildHUD();

            // Runtime safety net
            var bootstrap = new GameObject("PrototypeBootstrap");
            var pb = bootstrap.AddComponent<PrototypeBootstrap>();
            var so = new SerializedObject(pb);
            so.FindProperty("buildIfMissing").boolValue = false;
            so.FindProperty("defaultWeapon").objectReferenceValue = pistol;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Try bake a simple navmesh for future AI (optional, ignore failures)
            TryBakeNavMesh();

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_BlockyBattleRoyale/Scenes"));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            // Save player prefab
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_BlockyBattleRoyale/Prefabs/Characters"));
            PrefabUtility.SaveAsPrefabAsset(player, PrefabFolder + "/Player.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Blocky BR] Prototype generated. Open GameWorld scene and press Play.\nControls: WASD move, Mouse aim, LMB shoot, RMB aim, Shift run, C crouch, R reload.");
            EditorUtility.DisplayDialog(
                "Blocky Battle Royale",
                "PHASE 1 Prototype ready!\n\nScene: Assets/_BlockyBattleRoyale/Scenes/GameWorld.unity\n\nPress Play to test.\n\nWASD Move | Mouse Aim | LMB Shoot | RMB ADS | Shift Run | C Crouch | R Reload",
                "OK");
        }

        [MenuItem("Tools/Blocky Battle Royale/Open GameWorld Scene")]
        public static void OpenGameWorld()
        {
            if (File.Exists(Path.Combine(Application.dataPath, "_BlockyBattleRoyale/Scenes/GameWorld.unity")))
                EditorSceneManager.OpenScene(ScenePath);
            else
                GeneratePrototype();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "_BlockyBattleRoyale");
            CreateFolder(RootPath, "Scenes");
            CreateFolder(RootPath, "ScriptableObjects");
            CreateFolder(RootPath + "/ScriptableObjects", "Weapons");
            CreateFolder(RootPath, "Materials");
            CreateFolder(RootPath, "Prefabs");
            CreateFolder(RootPath + "/Prefabs", "Characters");
            CreateFolder(RootPath + "/Prefabs", "Environment");
        }

        private static void CreateFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureTagsAndLayers()
        {
            AddTag("Player");
            AddTag("Enemy");
            AddTag("Ground");
            AddTag("Cover");
            AddTag("Pickup");
        }

        private static void AddTag(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0)
                return;

            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        private static WeaponData CreateOrLoadWeapon(
            string weaponName, WeaponFireMode mode, AmmoType ammo,
            float damage, float fireRate, int mag, float reload, float range,
            float recoilPitch, float recoilYaw, float hip, float aim, int pellets)
        {
            string path = $"{WeaponFolder}/{weaponName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.weaponName = weaponName;
            data.fireMode = mode;
            data.ammoType = ammo;
            data.damage = damage;
            data.fireRate = fireRate;
            data.magazineSize = mag;
            data.reloadTime = reload;
            data.range = range;
            data.recoilPitch = recoilPitch;
            data.recoilYaw = recoilYaw;
            data.hipSpread = hip;
            data.aimSpread = aim;
            data.pelletCount = pellets;
            data.weaponColor = new Color(0.16f, 0.17f, 0.2f);

            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.965f, 0.9f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.42f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(0.55f, 0.65f, 0.72f);
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.tag = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // ~200m
            var rend = ground.GetComponent<Renderer>();
            rend.sharedMaterial = SaveMaterial("GroundMat", new Color(0.34f, 0.48f, 0.3f));

            // Soft hills
            for (int i = 0; i < 6; i++)
            {
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"Hill_{i}";
                float x = Random.Range(-60f, 60f);
                float z = Random.Range(-60f, 60f);
                hill.transform.position = new Vector3(x, -1.5f, z);
                hill.transform.localScale = new Vector3(Random.Range(12f, 22f), Random.Range(4f, 8f), Random.Range(12f, 22f));
                hill.GetComponent<Renderer>().sharedMaterial = SaveMaterial("HillMat", new Color(0.32f, 0.45f, 0.28f));
            }
        }

        private static void BuildEnvironment()
        {
            var envRoot = new GameObject("Environment");

            // Village cluster
            BuildBuilding(envRoot.transform, new Vector3(18f, 0f, 12f), new Vector3(8f, 3.2f, 6f), new Color(0.72f, 0.62f, 0.48f), true);
            BuildBuilding(envRoot.transform, new Vector3(28f, 0f, 10f), new Vector3(6f, 5.5f, 6f), new Color(0.65f, 0.55f, 0.45f), true);
            BuildBuilding(envRoot.transform, new Vector3(-22f, 0f, 18f), new Vector3(10f, 3f, 7f), new Color(0.6f, 0.58f, 0.52f), true);
            BuildWarehouse(envRoot.transform, new Vector3(-30f, 0f, -20f));
            BuildMilitaryShack(envRoot.transform, new Vector3(35f, 0f, -28f));

            // Road strips
            CreateBox(envRoot.transform, "Road_Main", new Vector3(0f, 0.02f, 0f), new Vector3(8f, 0.05f, 120f), new Color(0.28f, 0.28f, 0.3f), false);
            CreateBox(envRoot.transform, "Road_Cross", new Vector3(0f, 0.02f, 0f), new Vector3(100f, 0.05f, 8f), new Color(0.28f, 0.28f, 0.3f), false);

            // Trees / rocks / cover
            for (int i = 0; i < 40; i++)
            {
                Vector2 p = Random.insideUnitCircle * 70f;
                if (Mathf.Abs(p.x) < 6f || Mathf.Abs(p.y) < 6f)
                    continue;
                CreateTree(envRoot.transform, new Vector3(p.x, 0f, p.y));
            }

            for (int i = 0; i < 25; i++)
            {
                Vector2 p = Random.insideUnitCircle * 65f;
                CreateRock(envRoot.transform, new Vector3(p.x, 0f, p.y));
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 p = Random.insideUnitCircle * 50f;
                CreateBox(envRoot.transform, $"Container_{i}", new Vector3(p.x, 1.1f, p.y), new Vector3(2.4f, 2.2f, 1.2f),
                    new Color(0.2f, 0.45f, 0.55f), true);
            }

            // Fences
            for (int i = -5; i <= 5; i++)
            {
                CreateBox(envRoot.transform, $"Fence_{i}", new Vector3(i * 2.2f + 15f, 0.6f, 22f), new Vector3(2f, 1.2f, 0.15f),
                    new Color(0.4f, 0.32f, 0.22f), true);
            }
        }

        private static void BuildBuilding(Transform parent, Vector3 pos, Vector3 size, Color wallColor, bool withRoof)
        {
            var building = new GameObject("Building");
            building.transform.SetParent(parent, false);
            building.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;
            float t = 0.35f;

            // Floor
            CreateBox(building.transform, "Floor", new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), new Color(0.45f, 0.4f, 0.35f), false);

            // Walls with door gap on +Z
            CreateBox(building.transform, "Wall_N", new Vector3(0f, h * 0.5f, d * 0.5f), new Vector3(w, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_S_L", new Vector3(-w * 0.28f, h * 0.5f, -d * 0.5f), new Vector3(w * 0.35f, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_S_R", new Vector3(w * 0.28f, h * 0.5f, -d * 0.5f), new Vector3(w * 0.35f, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_W", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(t, h, d), wallColor, true);
            CreateBox(building.transform, "Wall_E", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(t, h, d), wallColor, true);

            // Window holes approximated by not covering full wall; add window frames
            CreateBox(building.transform, "WindowFrame", new Vector3(w * 0.5f - 0.02f, h * 0.55f, 0f), new Vector3(0.08f, 1.1f, 1.4f), new Color(0.25f, 0.25f, 0.28f), false);

            Renderer roofRend = null;
            if (withRoof)
            {
                var roof = CreateBox(building.transform, "Roof", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.4f, 0.3f, d + 0.4f), new Color(0.45f, 0.25f, 0.2f), true);
                roofRend = roof.GetComponent<Renderer>();
            }

            // Interior crate loot marker (visual only for Phase 1)
            CreateBox(building.transform, "Crate", new Vector3(0f, 0.45f, 0.8f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.55f, 0.4f, 0.2f), true);

            // Roof fade trigger
            var triggerGo = new GameObject("InteriorTrigger");
            triggerGo.transform.SetParent(building.transform, false);
            triggerGo.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(w * 0.85f, h, d * 0.85f);
            var fader = triggerGo.AddComponent<RoofFader>();
            if (roofRend != null)
                fader.Configure(new[] { roofRend }, box);
        }

        private static void BuildWarehouse(Transform parent, Vector3 pos)
        {
            BuildBuilding(parent, pos, new Vector3(14f, 4.5f, 10f), new Color(0.5f, 0.52f, 0.55f), true);
            CreateBox(parent, "Warehouse_Ramp", pos + new Vector3(0f, 0.2f, -7f), new Vector3(4f, 0.4f, 3f), new Color(0.4f, 0.4f, 0.42f), true);
        }

        private static void BuildMilitaryShack(Transform parent, Vector3 pos)
        {
            BuildBuilding(parent, pos, new Vector3(7f, 3f, 5f), new Color(0.35f, 0.42f, 0.32f), true);
            CreateBox(parent, "Sandbag_A", pos + new Vector3(4f, 0.5f, 0f), new Vector3(2.5f, 1f, 0.8f), new Color(0.55f, 0.5f, 0.35f), true);
            CreateBox(parent, "Sandbag_B", pos + new Vector3(-4f, 0.5f, 2f), new Vector3(2.5f, 1f, 0.8f), new Color(0.55f, 0.5f, 0.35f), true);
        }

        private static void CreateTree(Transform parent, Vector3 pos)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            CreateBox(tree.transform, "Trunk", new Vector3(0f, 1f, 0f), new Vector3(0.45f, 2f, 0.45f), new Color(0.4f, 0.28f, 0.16f), true);
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            canopy.transform.localScale = new Vector3(2.2f, 2.0f, 2.2f);
            canopy.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            canopy.GetComponent<Renderer>().sharedMaterial = SaveMaterial("CanopyMat", new Color(0.22f, 0.45f, 0.22f));
        }

        private static void CreateRock(Transform parent, Vector3 pos)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos + Vector3.up * 0.4f;
            rock.transform.localScale = new Vector3(Random.Range(1.2f, 2.4f), Random.Range(0.8f, 1.6f), Random.Range(1.2f, 2.2f));
            rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-8f, 8f));
            rock.GetComponent<Renderer>().sharedMaterial = SaveMaterial("RockMat", new Color(0.45f, 0.43f, 0.4f));
            rock.tag = "Cover";
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = SaveMaterial(name + "Mat", color);
            if (!solid)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Object.DestroyImmediate(col);
            }
            else
            {
                go.tag = "Cover";
            }
            return go;
        }

        private static Material SaveMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.HasProperty("_BaseColor"))
                    existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Color"))
                    existing.color = color;
                return existing;
            }

            var mat = BlockyCharacterBuilder.CreateLitMaterial(color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SpawnEnemies(WeaponData weapon)
        {
            Color[] colors =
            {
                new Color(0.85f, 0.25f, 0.22f),
                new Color(0.25f, 0.7f, 0.35f),
                new Color(0.9f, 0.75f, 0.2f),
                new Color(0.35f, 0.35f, 0.38f),
                new Color(0.75f, 0.45f, 0.2f),
                new Color(0.55f, 0.3f, 0.65f),
                new Color(0.2f, 0.55f, 0.55f)
            };

            Vector3[] positions =
            {
                new Vector3(16f, 0.05f, 8f),
                new Vector3(-14f, 0.05f, 16f),
                new Vector3(22f, 0.05f, -12f),
                new Vector3(-24f, 0.05f, -8f),
                new Vector3(8f, 0.05f, -22f),
                new Vector3(-8f, 0.05f, 24f),
                new Vector3(30f, 0.05f, 20f)
            };

            var root = new GameObject("Enemies");
            for (int i = 0; i < positions.Length; i++)
            {
                var enemy = BlockyCharacterBuilder.Build($"Enemy_{i + 1}", positions[i], colors[i], false, weapon);
                enemy.transform.SetParent(root.transform, true);
                enemy.AddComponent<DummyEnemyAI>();
            }
        }

        private static void BuildHUD()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Text health = CreateText(canvasGo.transform, "HealthText", new Vector2(40, -40), new Vector2(360, 40), "HP  100/100", TextAnchor.UpperLeft, font, 28);
            Text ammo = CreateText(canvasGo.transform, "AmmoText", new Vector2(-40, -40), new Vector2(420, 40), "Pistol  12/12", TextAnchor.UpperRight, font, 26);
            ammo.alignment = TextAnchor.UpperRight;
            var ammoRt = ammo.rectTransform;
            ammoRt.anchorMin = ammoRt.anchorMax = new Vector2(1, 1);
            ammoRt.pivot = new Vector2(1, 1);

            Text alive = CreateText(canvasGo.transform, "AliveText", new Vector2(0, -40), new Vector2(240, 40), "ALIVE  8", TextAnchor.UpperCenter, font, 24);
            var aliveRt = alive.rectTransform;
            aliveRt.anchorMin = aliveRt.anchorMax = new Vector2(0.5f, 1);
            aliveRt.pivot = new Vector2(0.5f, 1);

            Text kills = CreateText(canvasGo.transform, "KillText", new Vector2(40, -90), new Vector2(240, 36), "KILLS  0", TextAnchor.UpperLeft, font, 22);

            Text feed = CreateText(canvasGo.transform, "KillFeed", new Vector2(0, -140), new Vector2(700, 40), "", TextAnchor.UpperCenter, font, 22);
            var feedRt = feed.rectTransform;
            feedRt.anchorMin = feedRt.anchorMax = new Vector2(0.5f, 1);
            feedRt.pivot = new Vector2(0.5f, 1);

            // Health bar
            var barBg = CreateImage(canvasGo.transform, "HealthBarBG", new Vector2(40, -130), new Vector2(280, 16), new Color(0f, 0f, 0f, 0.45f));
            barBg.rectTransform.anchorMin = barBg.rectTransform.anchorMax = new Vector2(0, 1);
            barBg.rectTransform.pivot = new Vector2(0, 1);
            var bar = CreateImage(barBg.transform, "HealthBarFill", Vector2.zero, Vector2.zero, new Color(0.85f, 0.25f, 0.25f, 0.95f));
            var barRt = bar.rectTransform;
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = Vector2.one;
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillAmount = 1f;

            // Crosshair
            var cross = CreateImage(canvasGo.transform, "Crosshair", Vector2.zero, new Vector2(18, 18), new Color(1f, 1f, 1f, 0.85f));
            var crossRt = cross.rectTransform;
            crossRt.anchorMin = crossRt.anchorMax = new Vector2(0.5f, 0.5f);
            crossRt.pivot = new Vector2(0.5f, 0.5f);
            crossRt.anchoredPosition = Vector2.zero;

            // Result panel
            var panel = new GameObject("ResultPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.SetActive(false);

            Text result = CreateText(panel.transform, "ResultText", Vector2.zero, new Vector2(800, 260), "", TextAnchor.MiddleCenter, font, 42);
            var resultRt = result.rectTransform;
            resultRt.anchorMin = resultRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultRt.pivot = new Vector2(0.5f, 0.5f);

            var hud = canvasGo.AddComponent<GameHUD>();
            hud.BindUIReferences(health, ammo, alive, kills, feed, result, bar, cross, panel);
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string content, TextAnchor anchor, Font font, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.color = new Color(0.95f, 0.95f, 0.92f, 0.95f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return img;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                    return;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void TryBakeNavMesh()
        {
            try
            {
                // Unity 6 / AI Navigation package surface bake if available
                var surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                if (surfaceType == null)
                    return;

                var go = new GameObject("NavMeshSurface");
                var surface = go.AddComponent(surfaceType);
                var bake = surfaceType.GetMethod("BuildNavMesh");
                bake?.Invoke(surface, null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Blocky BR] NavMesh bake skipped: " + e.Message);
            }
        }
    }
}
#endif
