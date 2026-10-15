#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlockyBattleRoyale.AI;
using BlockyBattleRoyale.Audio;
using BlockyBattleRoyale.BattleRoyale;
using BlockyBattleRoyale.CameraSystem;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.UI;
using BlockyBattleRoyale.Vehicles;
using BlockyBattleRoyale.Visibility;
using BlockyBattleRoyale.Weapons;
using BlockyBattleRoyale.World;
using BlockyBattleRoyale.InventorySystem;
using BlockyBattleRoyale.Networking;
using BlockyBattleRoyale.Occlusion;

namespace BlockyBattleRoyale.EditorTools
{
    public static class FullGameGenerator
    {
        private const string Root = "Assets/_BlockyBattleRoyale";
        private const string GameScene = Root + "/Scenes/GameWorld.unity";
        private const string MenuScene = Root + "/Scenes/MainMenu.unity";
        private const string WeaponFolder = Root + "/ScriptableObjects/Weapons";
        private const string ItemFolder = Root + "/ScriptableObjects/Items";
        private const string SettingsFolder = Root + "/ScriptableObjects/GameSettings";
        private const string AttachmentFolder = Root + "/ScriptableObjects/Attachments";
        private const string MaterialFolder = Root + "/Materials";

        [MenuItem("Tools/Blocky Battle Royale/Generate Full Game (All Phases)", priority = 1)]
        public static void GenerateFullGame()
        {
            EnsureFolders();
            EnsureTags();

            var weapons = CreateWeapons();
            var attachments = CreateAttachments();
            var items = CreateItems(weapons, attachments);
            var zoneSettings = CreateZoneSettings();

            BuildMainMenuScene();
            BuildGameWorldScene(weapons, items, zoneSettings, attachments);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(MenuScene);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Blocky Battle Royale",
                    "FULL GAME generated (Phases 1-9 systems).\n\n" +
                    "Start: MainMenu scene → Play\n" +
                    "Or open GameWorld for direct test.\n\n" +
                    "Solo / Host / Join available in menu.",
                    "OK");
            }
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "_BlockyBattleRoyale");
            foreach (var p in new[]
                     {
                         "Scenes", "ScriptableObjects", "Materials", "Prefabs", "Settings",
                         "ScriptableObjects/Weapons", "ScriptableObjects/Items", "ScriptableObjects/GameSettings",
                         "Prefabs/Characters", "Prefabs/Environment", "Prefabs/Vehicles", "Prefabs/UI"
                     })
            {
                string full = Root + "/" + p;
                string parent = Path.GetDirectoryName(full)?.Replace("\\", "/");
                string name = Path.GetFileName(full);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(full))
                {
                    if (!AssetDatabase.IsValidFolder(parent))
                    {
                        // nested create
                    }
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, name);
                }
            }

            // Explicit nested creates
            CreateFolder(Root, "Scenes");
            CreateFolder(Root, "ScriptableObjects");
            CreateFolder(Root + "/ScriptableObjects", "Weapons");
            CreateFolder(Root + "/ScriptableObjects", "Items");
            CreateFolder(Root + "/ScriptableObjects", "GameSettings");
            CreateFolder(Root + "/ScriptableObjects", "Attachments");
            CreateFolder(Root, "Materials");
            CreateFolder(Root, "Prefabs");
            CreateFolder(Root + "/Prefabs", "Characters");
            CreateFolder(Root + "/Prefabs", "Vehicles");
            CreateFolder(Root, "Settings");
        }

        private static void CreateFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureTags()
        {
            foreach (var tag in new[] { "Player", "Enemy", "Ground", "Cover", "Pickup" })
                AddTag(tag);
            // Never invent tags at runtime — only TagManager entries above.
        }

        private static void AddTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        private static Dictionary<string, WeaponData> CreateWeapons()
        {
            var map = new Dictionary<string, WeaponData>
            {
                ["Pistol"] = MakeWeapon("Pistol", WeaponFireMode.SemiAuto, AmmoType.Pistol, 28f, 3.5f, 12, 1.5f, 55f, 1.4f, 0.5f, 2.2f, 0.5f, 1),
                ["SMG"] = MakeWeapon("SMG", WeaponFireMode.FullAuto, AmmoType.SMG, 18f, 10f, 30, 1.8f, 40f, 0.9f, 0.7f, 3.2f, 1f, 1),
                ["AssaultRifle"] = MakeWeapon("AssaultRifle", WeaponFireMode.FullAuto, AmmoType.Rifle, 24f, 7f, 30, 2f, 70f, 1.1f, 0.45f, 2.4f, 0.7f, 1),
                ["DMR"] = MakeWeapon("DMR", WeaponFireMode.SemiAuto, AmmoType.Rifle, 45f, 2.2f, 10, 2.2f, 100f, 2f, 0.35f, 1.6f, 0.35f, 1),
                ["Shotgun"] = MakeWeapon("Shotgun", WeaponFireMode.Shotgun, AmmoType.Shotgun, 14f, 1.1f, 6, 2.4f, 25f, 2.5f, 1.2f, 6f, 2.5f, 8),
                ["Sniper"] = MakeWeapon("Sniper", WeaponFireMode.SemiAuto, AmmoType.Rifle, 75f, 0.85f, 5, 3.2f, 140f, 3.2f, 0.5f, 1.2f, 0.15f, 1),
                ["LMG"] = MakeWeapon("LMG", WeaponFireMode.FullAuto, AmmoType.Rifle, 22f, 8.5f, 60, 4.5f, 75f, 1.4f, 0.6f, 3.5f, 1.1f, 1),
                ["Fists"] = MakeMelee("Fists", 18f, 2.2f, 1.7f, 0.5f, new Color(1f, 0.85f, 0.7f)),
                ["Crowbar"] = MakeMelee("Crowbar", 35f, 1.5f, 2.0f, 0.55f, new Color(0.45f, 0.45f, 0.48f)),
                ["Pan"] = MakeMelee("Pan", 28f, 1.8f, 1.85f, 0.6f, new Color(0.55f, 0.55f, 0.6f))
            };
            return map;
        }

        private static WeaponData MakeMelee(string n, float dmg, float rate, float range, float radius, Color color)
        {
            string path = $"{WeaponFolder}/{n}.asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.weaponName = n;
            data.isMelee = true;
            data.damage = dmg;
            data.fireRate = rate;
            data.meleeRange = range;
            data.meleeRadius = radius;
            data.magazineSize = 1;
            data.reloadTime = 0.1f;
            data.range = range;
            data.weaponColor = color;
            data.hipSpread = 0f;
            data.aimSpread = 0f;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static WeaponData MakeWeapon(string n, WeaponFireMode mode, AmmoType ammo,
            float dmg, float rate, int mag, float reload, float range,
            float rp, float ry, float hip, float aim, int pellets)
        {
            string path = $"{WeaponFolder}/{n}.asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.weaponName = n;
            data.fireMode = mode;
            data.ammoType = ammo;
            data.damage = dmg;
            data.fireRate = rate;
            data.magazineSize = mag;
            data.reloadTime = reload;
            data.range = range;
            data.recoilPitch = rp;
            data.recoilYaw = ry;
            data.hipSpread = hip;
            data.aimSpread = aim;
            data.pelletCount = pellets;
            data.weaponColor = new Color(0.16f, 0.17f, 0.2f);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static Dictionary<string, WeaponAttachmentData> CreateAttachments()
        {
            var map = new Dictionary<string, WeaponAttachmentData>
            {
                ["RedDot"] = MakeAttachment("RedDot", AttachmentSlot.Sight, 0.8f, 0.95f, 5f, 0),
                ["Compensator"] = MakeAttachment("Compensator", AttachmentSlot.Muzzle, 0.9f, 0.75f, 0f, 0),
                ["VerticalGrip"] = MakeAttachment("VerticalGrip", AttachmentSlot.Grip, 0.85f, 0.8f, 0f, 0),
                ["ExtendedMag"] = MakeAttachment("ExtendedMag", AttachmentSlot.Magazine, 1f, 1f, 0f, 10),
                ["Stock"] = MakeAttachment("Stock", AttachmentSlot.Stock, 0.9f, 0.85f, 8f, 0),
                ["Suppressor"] = MakeAttachment("Suppressor", AttachmentSlot.Muzzle, 1f, 1f, 0f, 0)
            };
            map["Suppressor"].isSuppressor = true;
            map["Suppressor"].hearRangeMultiplier = 0.35f;
            EditorUtility.SetDirty(map["Suppressor"]);
            return map;
        }

        private static WeaponAttachmentData MakeAttachment(string n, AttachmentSlot slot, float spread, float recoil, float range, int mag)
        {
            string path = $"{AttachmentFolder}/{n}.asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponAttachmentData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponAttachmentData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.attachmentName = n;
            data.slot = slot;
            data.spreadMul = spread;
            data.recoilMul = recoil;
            data.rangeBonus = range;
            data.magazineBonus = mag;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<ItemData> CreateItems(Dictionary<string, WeaponData> weapons, Dictionary<string, WeaponAttachmentData> attachments)
        {
            var list = new List<ItemData>();
            list.Add(MakeItem("Ammo_Pistol", ItemCategory.Ammo, new Color(0.9f, 0.85f, 0.3f), 60, 0.2f, i => { i.ammoType = AmmoType.Pistol; i.ammoAmount = 24; }));
            list.Add(MakeItem("Ammo_SMG", ItemCategory.Ammo, new Color(0.9f, 0.7f, 0.2f), 60, 0.2f, i => { i.ammoType = AmmoType.SMG; i.ammoAmount = 30; }));
            list.Add(MakeItem("Ammo_Rifle", ItemCategory.Ammo, new Color(0.85f, 0.6f, 0.2f), 60, 0.25f, i => { i.ammoType = AmmoType.Rifle; i.ammoAmount = 30; }));
            list.Add(MakeItem("Ammo_Shotgun", ItemCategory.Ammo, new Color(0.8f, 0.5f, 0.2f), 40, 0.3f, i => { i.ammoType = AmmoType.Shotgun; i.ammoAmount = 8; }));

            list.Add(MakeItem("Bandage", ItemCategory.Heal, new Color(0.95f, 0.95f, 0.95f), 5, 0.3f, i => { i.healType = HealType.Bandage; i.healAmount = 15f; i.useDuration = 2.5f; }));
            list.Add(MakeItem("FirstAid", ItemCategory.Heal, new Color(0.9f, 0.3f, 0.3f), 3, 0.6f, i => { i.healType = HealType.FirstAid; i.healAmount = 40f; i.useDuration = 4f; }));
            list.Add(MakeItem("MedKit", ItemCategory.Heal, new Color(0.85f, 0.15f, 0.15f), 2, 1.2f, i => { i.healType = HealType.MedKit; i.healAmount = 100f; i.useDuration = 6f; }));
            list.Add(MakeItem("EnergyDrink", ItemCategory.Heal, new Color(0.3f, 0.8f, 0.95f), 5, 0.4f, i => { i.healType = HealType.EnergyDrink; i.healAmount = 10f; i.useDuration = 2f; }));
            list.Add(MakeItem("Painkiller", ItemCategory.Heal, new Color(0.95f, 0.85f, 0.3f), 5, 0.35f, i => { i.healType = HealType.Painkiller; i.healAmount = 5f; i.useDuration = 3f; }));

            list.Add(MakeItem("Helmet_L1", ItemCategory.Helmet, new Color(0.5f, 0.55f, 0.4f), 1, 2f, i => { i.armorLevel = 1; i.damageReduction = 0.2f; i.durability = 80f; }));
            list.Add(MakeItem("Helmet_L2", ItemCategory.Helmet, new Color(0.4f, 0.45f, 0.5f), 1, 2.5f, i => { i.armorLevel = 2; i.damageReduction = 0.35f; i.durability = 120f; }));
            list.Add(MakeItem("Vest_L1", ItemCategory.Vest, new Color(0.35f, 0.4f, 0.3f), 1, 3f, i => { i.armorLevel = 1; i.damageReduction = 0.25f; i.durability = 100f; }));
            list.Add(MakeItem("Vest_L2", ItemCategory.Vest, new Color(0.3f, 0.35f, 0.4f), 1, 4f, i => { i.armorLevel = 2; i.damageReduction = 0.4f; i.durability = 150f; }));

            list.Add(MakeItem("Backpack_Lv1", ItemCategory.Backpack, new Color(0.25f, 0.35f, 0.28f), 1, 2f, i => { i.armorLevel = 1; }));
            list.Add(MakeItem("Backpack_Lv2", ItemCategory.Backpack, new Color(0.3f, 0.42f, 0.32f), 1, 3f, i => { i.armorLevel = 2; }));
            list.Add(MakeItem("Backpack_Lv3", ItemCategory.Backpack, new Color(0.55f, 0.45f, 0.22f), 1, 4f, i => { i.armorLevel = 3; }));

            list.Add(MakeItem("FuelCan", ItemCategory.Fuel, new Color(0.75f, 0.45f, 0.15f), 3, 4f, i => { i.worldScale = new Vector3(0.35f, 0.45f, 0.35f); }));
            if (attachments != null && attachments.TryGetValue("Suppressor", out var suppressor))
                list.Add(MakeItem("Suppressor", ItemCategory.Attachment, new Color(0.2f, 0.2f, 0.22f), 1, 1f, i => { i.attachmentData = suppressor; }));
            list.Add(MakeItem("Grenade", ItemCategory.Throwable, new Color(0.25f, 0.45f, 0.25f), 3, 0.8f, i => { i.throwableType = ThrowableType.Grenade; i.explodeDamage = 80f; i.explodeRadius = 5f; }));
            list.Add(MakeItem("SmokeGrenade", ItemCategory.Throwable, new Color(0.7f, 0.7f, 0.75f), 3, 0.7f, i => { i.throwableType = ThrowableType.Smoke; i.smokeDuration = 10f; i.smokeRadius = 6f; }));

            foreach (var kv in weapons)
            {
                var w = kv.Value;
                list.Add(MakeItem("Wpn_" + kv.Key, ItemCategory.Weapon, new Color(0.2f, 0.2f, 0.22f), 1, 4f, i => { i.weaponData = w; i.worldScale = new Vector3(0.8f, 0.25f, 0.35f); }));
            }

            return list;
        }

        private static ItemData MakeItem(string name, ItemCategory cat, Color tint, int stack, float weight, System.Action<ItemData> cfg)
        {
            string path = $"{ItemFolder}/{name}.asset";
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.itemName = name.Replace('_', ' ');
            data.category = cat;
            data.tint = tint;
            data.maxStack = stack;
            data.weight = weight;
            cfg?.Invoke(data);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static ZoneSettings CreateZoneSettings()
        {
            string path = $"{SettingsFolder}/ZoneSettings.asset";
            var data = AssetDatabase.LoadAssetAtPath<ZoneSettings>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ZoneSettings>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.startRadius = 95f;
            data.phases = new[]
            {
                new ZonePhaseData { waitTime = 90f, shrinkTime = 60f, endRadius = 70f, damagePerSecond = 1.5f },
                new ZonePhaseData { waitTime = 60f, shrinkTime = 50f, endRadius = 45f, damagePerSecond = 3f },
                new ZonePhaseData { waitTime = 45f, shrinkTime = 40f, endRadius = 25f, damagePerSecond = 5f },
                new ZonePhaseData { waitTime = 30f, shrinkTime = 35f, endRadius = 12f, damagePerSecond = 8f },
                new ZonePhaseData { waitTime = 20f, shrinkTime = 30f, endRadius = 4f, damagePerSecond = 12f }
            };
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.16f, 0.2f, 0.18f);
            cam.AddComponent<AudioListener>();

            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Decorative blocky character (no AI / NavMesh)
            var hero = BlockyCharacterBuilder.Build("MenuHero", new Vector3(0f, 0f, 4f), new Color(0.2f, 0.5f, 0.9f), false, null);
            var agent = hero.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) Object.DestroyImmediate(agent);
            var brain = hero.GetComponent<BotBrain>();
            if (brain != null) Object.DestroyImmediate(brain);

            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var title = CreateText(canvasGo.transform, "Title", new Vector2(0, 220), new Vector2(900, 100), "BLOCKY BATTLE ROYALE", TextAnchor.MiddleCenter, font, 54);
            title.color = new Color(0.93f, 0.9f, 0.82f);
            Center(title.rectTransform);

            var menu = canvasGo.AddComponent<MainMenuController>();
            var solo = CreateButton(canvasGo.transform, "SoloBtn", new Vector2(0, 60), "혼자 플레이", font, () => { });
            var host = CreateButton(canvasGo.transform, "HostBtn", new Vector2(0, -20), "호스트 생성", font, () => { });
            var join = CreateButton(canvasGo.transform, "JoinBtn", new Vector2(0, -100), "게임 참가", font, () => { });
            var quit = CreateButton(canvasGo.transform, "QuitBtn", new Vector2(0, -180), "종료", font, () => { });

            var addressGo = new GameObject("Address");
            addressGo.transform.SetParent(canvasGo.transform, false);
            var address = addressGo.AddComponent<InputField>();
            var addressText = CreateText(addressGo.transform, "Text", Vector2.zero, new Vector2(360, 40), "127.0.0.1", TextAnchor.MiddleCenter, font, 22);
            var placeholder = CreateText(addressGo.transform, "Placeholder", Vector2.zero, new Vector2(360, 40), "IP Address", TextAnchor.MiddleCenter, font, 20);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            address.textComponent = addressText;
            address.placeholder = placeholder;
            address.text = "127.0.0.1";
            var addrImg = addressGo.AddComponent<Image>();
            addrImg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            var addrRt = addressGo.GetComponent<RectTransform>();
            Center(addrRt);
            addrRt.anchoredPosition = new Vector2(0, -260);
            addrRt.sizeDelta = new Vector2(360, 44);

            var status = CreateText(canvasGo.transform, "Status", new Vector2(0, -320), new Vector2(600, 40), "Ready", TextAnchor.MiddleCenter, font, 20);
            Center(status.rectTransform);

            menu.Bind(address, status);
            menu.BindButtons(solo, host, join, quit);

            // Runtime WireButtons also finds by name; keep persistent refs on controller
            solo.onClick.AddListener(menu.OnSolo);
            host.onClick.AddListener(menu.OnHost);
            join.onClick.AddListener(menu.OnJoin);
            quit.onClick.AddListener(menu.OnQuit);

            // Settings panel (Esc)
            var settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvasGo.transform, false);
            var spImg = settingsPanel.AddComponent<Image>();
            spImg.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
            var spRt = settingsPanel.GetComponent<RectTransform>();
            Center(spRt);
            spRt.sizeDelta = new Vector2(520, 320);
            spRt.anchoredPosition = Vector2.zero;

            var settingsTitle = CreateText(settingsPanel.transform, "SettingsTitle", new Vector2(0, 120), new Vector2(400, 40), "SETTINGS", TextAnchor.MiddleCenter, font, 28);
            Center(settingsTitle.rectTransform);
            settingsTitle.rectTransform.anchoredPosition = new Vector2(0, 120);

            Slider MakeSlider(string name, Vector2 pos, float value)
            {
                var go = new GameObject(name);
                go.transform.SetParent(settingsPanel.transform, false);
                var bg = go.AddComponent<Image>();
                bg.color = new Color(0.2f, 0.22f, 0.26f, 1f);
                var rt = go.GetComponent<RectTransform>();
                Center(rt);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(360, 24);
                var slider = go.AddComponent<Slider>();
                slider.minValue = 0.1f;
                slider.maxValue = 2f;
                slider.value = value;
                var fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(go.transform, false);
                var fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                var fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.35f, 0.65f, 0.9f, 1f);
                slider.fillRect = fill.GetComponent<RectTransform>();
                slider.fillRect.sizeDelta = Vector2.zero;
                var handle = new GameObject("Handle");
                handle.transform.SetParent(go.transform, false);
                var handleImg = handle.AddComponent<Image>();
                handleImg.color = Color.white;
                var hRt = handle.GetComponent<RectTransform>();
                hRt.sizeDelta = new Vector2(16, 28);
                slider.handleRect = hRt;
                slider.targetGraphic = handleImg;
                return slider;
            }

            var volLabel = CreateText(settingsPanel.transform, "VolLabel", new Vector2(0, 50), new Vector2(360, 24), "Master Volume", TextAnchor.MiddleLeft, font, 18);
            Center(volLabel.rectTransform);
            volLabel.rectTransform.anchoredPosition = new Vector2(0, 55);
            var masterSlider = MakeSlider("MasterSlider", new Vector2(0, 20), GameSettings.MasterVolume);

            var sensLabel = CreateText(settingsPanel.transform, "SensLabel", new Vector2(0, -20), new Vector2(360, 24), "Mouse Sensitivity", TextAnchor.MiddleLeft, font, 18);
            Center(sensLabel.rectTransform);
            sensLabel.rectTransform.anchoredPosition = new Vector2(0, -15);
            var sensSlider = MakeSlider("SensSlider", new Vector2(0, -50), GameSettings.MouseSensitivity);

            var info = CreateText(settingsPanel.transform, "Info", new Vector2(0, -110), new Vector2(440, 40), "Esc to close", TextAnchor.MiddleCenter, font, 16);
            Center(info.rectTransform);
            info.rectTransform.anchoredPosition = new Vector2(0, -110);

            var settingsCtrl = canvasGo.AddComponent<SettingsMenuController>();
            settingsCtrl.Bind(settingsPanel, masterSlider, sensSlider, info);
            settingsPanel.SetActive(false);

            if (Object.FindFirstObjectByType<NetworkBootstrap>() == null)
            {
                var nb = new GameObject("NetworkBootstrap");
                nb.AddComponent<NetworkBootstrap>();
            }

            EditorSceneManager.SaveScene(scene, MenuScene);
            SetBuildScenes(MenuScene, GameScene);
        }

        private static void BuildGameWorldScene(
            Dictionary<string, WeaponData> weapons,
            List<ItemData> items,
            ZoneSettings zoneSettings,
            Dictionary<string, WeaponAttachmentData> attachments)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameWorld";

            BuildLighting();
            BuildGroundAndMap();

            var pistol = weapons["Pistol"];
            var fists = weapons["Fists"];
            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0f, 0.05f, 0f), new Color(0.22f, 0.48f, 0.88f), true, pistol);
            player.name = "Player";
            var inv = player.GetComponent<PlayerInventory>();
            inv?.SetFists(fists);
            inv?.SetStartingWeapon(pistol); // gun in slot 1; 3 = fists/melee
            if (attachments != null && attachments.TryGetValue("RedDot", out var redDot))
                player.GetComponent<WeaponLoadout>()?.Equip(redDot);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.78f);
            camGo.AddComponent<AudioListener>();
            var profile = VillageSliceGenerator.EnsureCameraProfile();
            var qv = camGo.AddComponent<QuarterViewCamera>();
            qv.ApplyProfile(profile);
            qv.SetTarget(player.transform);
            camGo.AddComponent<CameraDebugHud>();
            camGo.AddComponent<CameraDebugPanel>();
            camGo.AddComponent<CameraReferenceCalibration>();
            camGo.AddComponent<CameraOcclusionController>().SetTarget(player.transform);

            var systems = new GameObject("Systems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<MatchManager>();
            var msm = systems.AddComponent<MatchStateMachine>();
            var msmSo = new SerializedObject(msm);
            // Ground spawn for visual QA; drop still available via DropSystem
            var autoDrop = msmSo.FindProperty("autoStartDrop");
            if (autoDrop != null) { autoDrop.boolValue = false; msmSo.ApplyModifiedPropertiesWithoutUndo(); }
            systems.AddComponent<ObjectPool>();
            systems.AddComponent<GameAudio>();
            systems.AddComponent<VisibilityController>();
            systems.AddComponent<PingSystem>();
            var filler = systems.AddComponent<OfflineMatchFiller>();
            filler.Configure(weapons["SMG"], 8);
            BlockyBattleRoyale.Core.GameSettings.ApplyAll();

            var zoneGo = new GameObject("ZoneManager");
            zoneGo.transform.SetParent(systems.transform, false);
            var zone = zoneGo.AddComponent<ZoneManager>();
            zone.Configure(zoneSettings, Vector3.zero);

            var lootGo = new GameObject("LootSpawner");
            lootGo.transform.SetParent(systems.transform, false);
            var loot = lootGo.AddComponent<LootSpawner>();
            loot.Configure(items.ToArray(), 55, 75f);
            loot.SpawnAll();

            var supplyGo = new GameObject("SupplyDropSystem");
            supplyGo.transform.SetParent(systems.transform, false);
            var supply = supplyGo.AddComponent<SupplyDropSystem>();
            // rare: AR, DMR, L2 armor, medkit
            var rare = new List<ItemData>();
            foreach (var it in items)
            {
                if (it.itemName.Contains("Assault") || it.itemName.Contains("DMR") || it.itemName.Contains("L2") || it.itemName.Contains("Med"))
                    rare.Add(it);
            }
            supply.Configure(rare.Count > 0 ? rare.ToArray() : items.ToArray(), 50f);

            var dropGo = new GameObject("DropSystem");
            dropGo.transform.SetParent(systems.transform, false);
            var drop = dropGo.AddComponent<DropSystem>();
            // Optional: start on ground for faster combat test. Comment next line to force sky drop.
            // drop.Begin(player.transform, Vector3.zero);

            SpawnBots(weapons["SMG"]);
            SpawnVehicles();
            // Grass/props already dense from VillageSliceGenerator
            BuildFullHUD();
            TryBakeNavMesh();

            PrefabUtility.SaveAsPrefabAsset(player, Root + "/Prefabs/Characters/Player.prefab");

            EditorSceneManager.SaveScene(scene, GameScene);
            SetBuildScenes(MenuScene, GameScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[BR] GameWorld saved (V3 village).");
        }

        private static void SpawnBots(WeaponData weapon)
        {
            Color[] colors =
            {
                new Color(0.85f, 0.25f, 0.22f), new Color(0.25f, 0.7f, 0.35f), new Color(0.9f, 0.75f, 0.2f),
                new Color(0.35f, 0.35f, 0.38f), new Color(0.75f, 0.45f, 0.2f), new Color(0.55f, 0.3f, 0.65f),
                new Color(0.2f, 0.55f, 0.55f)
            };
            Vector3[] pos =
            {
                new Vector3(18, 0.05f, 10), new Vector3(-16, 0.05f, 18), new Vector3(24, 0.05f, -14),
                new Vector3(-26, 0.05f, -10), new Vector3(10, 0.05f, -24), new Vector3(-10, 0.05f, 26),
                new Vector3(32, 0.05f, 22)
            };

            var root = new GameObject("Bots");
            for (int i = 0; i < 7; i++)
            {
                var bot = BlockyCharacterBuilder.Build($"Bot_{i + 1}", pos[i], colors[i], false, weapon);
                bot.transform.SetParent(root.transform, true);
                var brain = bot.AddComponent<BotBrain>();
                brain.SetDifficulty(i % 3 == 0 ? BotDifficulty.Easy : i % 3 == 1 ? BotDifficulty.Normal : BotDifficulty.Hard);
                bot.GetComponent<PlayerInventory>()?.SetStartingWeapon(weapon);
            }
        }

        private static void SpawnVehicles()
        {
            var root = new GameObject("Vehicles");
            CreateVehicle(root.transform, new Vector3(12f, 0.1f, -8f), new Color(0.7f, 0.2f, 0.2f));
            CreateVehicle(root.transform, new Vector3(-20f, 0.1f, 8f), new Color(0.2f, 0.35f, 0.7f));
            CreateVehicle(root.transform, new Vector3(30f, 0.1f, -30f), new Color(0.25f, 0.45f, 0.25f));
            var station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            station.name = "RecallStation";
            station.transform.SetParent(root.transform, false);
            station.transform.position = new Vector3(0f, 0.4f, 18f);
            station.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            station.GetComponent<Renderer>().sharedMaterial = SaveMat("RecallMat", new Color(0.35f, 0.55f, 0.75f));
            station.AddComponent<BlockyBattleRoyale.World.RecallStation>();
        }

        private static void CreateVehicle(Transform parent, Vector3 pos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Vehicle";
            go.transform.SetParent(parent, false);
            go.transform.position = pos + Vector3.up * 0.6f;
            go.transform.localScale = new Vector3(2.4f, 1.1f, 4.2f);
            go.GetComponent<Renderer>().sharedMaterial = SaveMat("VehicleMat", color);
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(go.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.55f, 0.3f);
            cabin.transform.localScale = new Vector3(0.85f, 0.7f, 0.55f);
            Object.DestroyImmediate(cabin.GetComponent<Collider>());
            cabin.GetComponent<Renderer>().sharedMaterial = SaveMat("CabinMat", Darken(color, 0.7f));
            go.AddComponent<SimpleVehicle>();
        }

        private static void SpawnGrass()
        {
            var root = new GameObject("GrassPatches");
            for (int i = 0; i < 18; i++)
            {
                Vector2 c = Random.insideUnitCircle * 60f;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Grass";
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(c.x, 0.4f, c.y);
                go.transform.localScale = new Vector3(4f, 0.8f, 4f);
                go.GetComponent<Renderer>().sharedMaterial = SaveMat("GrassMat", new Color(0.2f, 0.5f, 0.22f, 0.7f));
                var col = go.GetComponent<Collider>();
                col.isTrigger = true;
                go.AddComponent<GrassPatch>();
            }
        }

        private static void BuildLighting()
        {
            BlockyBattleRoyale.Rendering.SceneLighting.ApplyDaylight();
        }

        private static void BuildGroundAndMap()
        {
            var env = new GameObject("Environment");
            // V3 Duckov-density village slice (mesh terrain, interiors, props)
            VillageSliceGenerator.BuildDenseVillage(env.transform, 2026);
        }

        private static void BuildBuilding(Transform parent, Vector3 pos, Vector3 size, Color wallColor)
        {
            var building = new GameObject("Building");
            building.transform.SetParent(parent, false);
            building.transform.position = pos;
            float w = size.x, h = size.y, d = size.z, t = 0.35f;
            CreateBox(building.transform, "Floor", new Vector3(0, 0.05f, 0), new Vector3(w, 0.1f, d), new Color(0.45f, 0.4f, 0.35f), false);
            CreateBox(building.transform, "Wall_N", new Vector3(0, h * 0.5f, d * 0.5f), new Vector3(w, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_S_L", new Vector3(-w * 0.28f, h * 0.5f, -d * 0.5f), new Vector3(w * 0.35f, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_S_R", new Vector3(w * 0.28f, h * 0.5f, -d * 0.5f), new Vector3(w * 0.35f, h, t), wallColor, true);
            CreateBox(building.transform, "Wall_W", new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wallColor, true);
            CreateBox(building.transform, "Wall_E", new Vector3(w * 0.5f, h * 0.5f, 0), new Vector3(t, h, d), wallColor, true);
            var roof = CreateBox(building.transform, "Roof", new Vector3(0, h + 0.15f, 0), new Vector3(w + 0.4f, 0.3f, d + 0.4f), new Color(0.45f, 0.25f, 0.2f), true);
            CreateBox(building.transform, "Crate", new Vector3(0, 0.45f, 0.8f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.55f, 0.4f, 0.2f), true);

            // Door leaf in doorway gap (S side)
            var door = CreateBox(building.transform, "Door", new Vector3(0f, h * 0.45f, -d * 0.5f), new Vector3(1.1f, h * 0.85f, 0.12f), new Color(0.35f, 0.25f, 0.18f), true);
            door.transform.localPosition = new Vector3(0f, h * 0.45f, -d * 0.5f);
            var doorComp = door.AddComponent<DoorInteractable>();
            doorComp.Configure(door.transform, 95f);

            var triggerGo = new GameObject("InteriorTrigger");
            triggerGo.transform.SetParent(building.transform, false);
            triggerGo.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(w * 0.85f, h, d * 0.85f);
            var fader = triggerGo.AddComponent<RoofFader>();
            fader.Configure(new[] { roof.GetComponent<Renderer>() }, box);
        }

        private static void CreateTree(Transform parent, Vector3 pos)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            CreateBox(tree.transform, "Trunk", new Vector3(0, 1, 0), new Vector3(0.45f, 2f, 0.45f), new Color(0.4f, 0.28f, 0.16f), true);
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0, 2.6f, 0);
            canopy.transform.localScale = new Vector3(2.2f, 2f, 2.2f);
            canopy.GetComponent<Renderer>().sharedMaterial = SaveMat("CanopyMat", new Color(0.22f, 0.45f, 0.22f));
        }

        private static void CreateRock(Transform parent, Vector3 pos)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.tag = "Cover";
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos + Vector3.up * 0.4f;
            rock.transform.localScale = new Vector3(Random.Range(1.2f, 2.4f), Random.Range(0.8f, 1.6f), Random.Range(1.2f, 2.2f));
            rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            rock.GetComponent<Renderer>().sharedMaterial = SaveMat("RockMat", new Color(0.45f, 0.43f, 0.4f));
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = SaveMat(name + "Mat", color);
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            else go.tag = "Cover";
            return go;
        }

        private static Material SaveMat(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = BlockyCharacterBuilder.CreateLitMaterial(color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Color Darken(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

        private static void BuildFullHUD()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            Text health = CreateText(canvasGo.transform, "HealthText", new Vector2(40, -40), new Vector2(360, 40), "HP  100/100", TextAnchor.UpperLeft, font, 28);
            Text ammo = CreateText(canvasGo.transform, "AmmoText", new Vector2(-40, -40), new Vector2(420, 40), "Pistol  12/12", TextAnchor.UpperRight, font, 26);
            AnchorTR(ammo.rectTransform);
            Text alive = CreateText(canvasGo.transform, "AliveText", new Vector2(0, -40), new Vector2(240, 40), "ALIVE  8", TextAnchor.UpperCenter, font, 24);
            AnchorTC(alive.rectTransform);
            Text kills = CreateText(canvasGo.transform, "KillText", new Vector2(40, -90), new Vector2(240, 36), "KILLS  0", TextAnchor.UpperLeft, font, 22);
            Text zone = CreateText(canvasGo.transform, "ZoneText", new Vector2(0, -90), new Vector2(700, 36), "ZONE", TextAnchor.UpperCenter, font, 20);
            AnchorTC(zone.rectTransform);
            zone.rectTransform.anchoredPosition = new Vector2(0, -90);
            Text feed = CreateText(canvasGo.transform, "KillFeed", new Vector2(0, -140), new Vector2(700, 40), "", TextAnchor.UpperCenter, font, 22);
            AnchorTC(feed.rectTransform);
            feed.rectTransform.anchoredPosition = new Vector2(0, -140);
            Text prompt = CreateText(canvasGo.transform, "Prompt", new Vector2(0, 120), new Vector2(500, 40), "", TextAnchor.LowerCenter, font, 24);
            var prt = prompt.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0, 120);
            Text inv = CreateText(canvasGo.transform, "InventoryText", new Vector2(40, -180), new Vector2(420, 420), "", TextAnchor.UpperLeft, font, 18);

            var barBg = CreateImage(canvasGo.transform, "HealthBarBG", new Vector2(40, -130), new Vector2(280, 16), new Color(0, 0, 0, 0.45f));
            var bar = CreateImage(barBg.transform, "HealthBarFill", Vector2.zero, Vector2.zero, new Color(0.85f, 0.25f, 0.25f, 0.95f));
            Stretch(bar.rectTransform);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillAmount = 1f;

            var armorBg = CreateImage(canvasGo.transform, "ArmorBarBG", new Vector2(40, -152), new Vector2(280, 10), new Color(0, 0, 0, 0.45f));
            var armor = CreateImage(armorBg.transform, "ArmorFill", Vector2.zero, Vector2.zero, new Color(0.35f, 0.55f, 0.95f, 0.95f));
            Stretch(armor.rectTransform);
            armor.type = Image.Type.Filled;
            armor.fillMethod = Image.FillMethod.Horizontal;

            var cross = CreateImage(canvasGo.transform, "Crosshair", Vector2.zero, new Vector2(18, 18), new Color(1, 1, 1, 0.85f));
            var crt = cross.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);

            // Minimap
            var mini = CreateImage(canvasGo.transform, "Minimap", new Vector2(-40, -40), new Vector2(180, 180), new Color(0.05f, 0.08f, 0.1f, 0.7f));
            AnchorTR(mini.rectTransform);
            mini.rectTransform.anchoredPosition = new Vector2(-40, -200);
            var pIcon = CreateImage(mini.transform, "PlayerIcon", Vector2.zero, new Vector2(10, 10), new Color(0.3f, 0.7f, 1f));
            Center(pIcon.rectTransform);
            var zRing = CreateImage(mini.transform, "ZoneRing", Vector2.zero, new Vector2(120, 120), new Color(0.2f, 0.6f, 1f, 0.25f));
            Center(zRing.rectTransform);
            var nRing = CreateImage(mini.transform, "NextRing", Vector2.zero, new Vector2(80, 80), new Color(1f, 1f, 1f, 0.15f));
            Center(nRing.rectTransform);

            var fullMap = CreateImage(canvasGo.transform, "FullMap", Vector2.zero, new Vector2(700, 700), new Color(0.05f, 0.07f, 0.09f, 0.92f));
            Center(fullMap.rectTransform);
            fullMap.gameObject.SetActive(false);

            var minimap = canvasGo.AddComponent<MinimapUI>();
            minimap.Bind(pIcon.rectTransform, zRing.rectTransform, nRing.rectTransform, fullMap.gameObject);

            var panel = new GameObject("ResultPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);
            Stretch(panel.GetComponent<RectTransform>());
            panel.SetActive(false);
            Text result = CreateText(panel.transform, "ResultText", Vector2.zero, new Vector2(800, 260), "", TextAnchor.MiddleCenter, font, 42);
            Center(result.rectTransform);

            var hud = canvasGo.AddComponent<GameHUD>();
            hud.BindUIReferences(health, ammo, alive, kills, feed, result, bar, cross, panel, zone, prompt, inv, armor);
        }

        private static Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, string content, TextAnchor anchor, Font font, int sizeFont)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = sizeFont;
            text.color = new Color(0.95f, 0.95f, 0.92f, 0.95f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 pos, string label, Font font, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.2f, 0.16f, 0.94f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.14f, 0.2f, 0.16f, 0.94f);
            colors.highlightedColor = new Color(0.28f, 0.4f, 0.28f, 1f);
            colors.pressedColor = new Color(0.1f, 0.14f, 0.11f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var rt = go.GetComponent<RectTransform>();
            Center(rt);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320, 56);
            var text = CreateText(go.transform, "Label", Vector2.zero, new Vector2(320, 56), label, TextAnchor.MiddleCenter, font, 24);
            Center(text.rectTransform);
            text.raycastTarget = false;
            return btn;
        }

        private static void Center(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void AnchorTR(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
        }

        private static void AnchorTC(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetBuildScenes(params string[] paths)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths)
                list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void TryBakeNavMesh()
        {
            try
            {
                var surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                if (surfaceType == null) return;
                var go = new GameObject("NavMeshSurface");
                var surface = go.AddComponent(surfaceType);
                surfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Blocky BR] NavMesh bake skipped: " + e.Message);
            }
        }
    }
}
#endif
