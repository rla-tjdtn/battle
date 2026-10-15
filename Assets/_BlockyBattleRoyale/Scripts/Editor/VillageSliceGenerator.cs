#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using BlockyBattleRoyale.AI;
using BlockyBattleRoyale.Audio;
using BlockyBattleRoyale.BattleRoyale;
using BlockyBattleRoyale.Buildings;
using BlockyBattleRoyale.CameraSystem;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Environment;
using BlockyBattleRoyale.InventorySystem;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.Networking;
using BlockyBattleRoyale.Occlusion;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.TerrainSystem;
using BlockyBattleRoyale.UI;
using BlockyBattleRoyale.Vehicles;
using BlockyBattleRoyale.Visibility;
using BlockyBattleRoyale.Weapons;
using BlockyBattleRoyale.World;

namespace BlockyBattleRoyale.EditorTools
{
    /// <summary>
    /// V3 Duckov-density village slice + camera reference scene.
    /// </summary>
    public static class VillageSliceGenerator
    {
        private const string Root = "Assets/_BlockyBattleRoyale";
        private const string GameScene = Root + "/Scenes/GameWorld.unity";
        private const string CamScene = Root + "/Scenes/CameraReferenceScene.unity";
        private const string VisScene = Root + "/Scenes/VisibilityTestScene.unity";
        private const string BldScene = Root + "/Scenes/BuildingTestScene.unity";
        private const string MatFolder = Root + "/Materials";
        private const string ProfilePath = Root + "/Data/Camera/DuckovDefault.asset";

        [MenuItem("Tools/Battle Royale/Generate Duckov Village Slice", priority = 2)]
        public static void GenerateVillageSliceMenu()
        {
            // Reuse full game weapons/items via FullGameGenerator path
            FullGameGenerator.GenerateFullGame();
        }

        [MenuItem("Tools/Battle Royale/Generate Camera Reference Scene", priority = 21)]
        public static void GenerateCameraReferenceScene()
        {
            EnsureFolders();
            var profile = EnsureCameraProfile();
            var mats = CreatePalette();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CameraReferenceScene";
            WarmLighting();

            var env = new GameObject("Environment");
            MeshTerrainBuilder.Build(env.transform, 40f, 48, 2.5f, mats.grass, 42);
            CreateRoad(env.transform, mats.asphalt);

            ProceduralBuildingFactory.Build(env.transform, BuildingKind.HouseSmall, new Vector3(8, 0, 6), mats.wall, mats.roof, mats.floor, mats.wood);
            ProceduralBuildingFactory.Build(env.transform, BuildingKind.Warehouse, new Vector3(-12, 0, 8), mats.concrete, mats.metal, mats.floor, mats.wood);
            ProceduralBuildingFactory.Build(env.transform, BuildingKind.Garage, new Vector3(14, 0, -8), mats.wall, mats.roof, mats.floor, mats.wood);

            VegetationFactory.CreateTree(env.transform, new Vector3(-6, 0, -4), TreeKind.Pine, mats.trunk, mats.leafDark);
            VegetationFactory.CreateTree(env.transform, new Vector3(4, 0, -10), TreeKind.Medium, mats.trunk, mats.leaf);
            VegetationFactory.CreateTree(env.transform, new Vector3(-14, 0, -6), TreeKind.Small, mats.trunk, mats.leafLight);
            VegetationFactory.CreateRock(env.transform, new Vector3(2, 0, 2), mats.rock);
            VegetationFactory.CreateFenceSegment(env.transform, new Vector3(0, 0, -14), 0, mats.wood);
            VegetationFactory.CreateProp(env.transform, "Crate", new Vector3(6, 0.4f, 2), new Vector3(1, 0.8f, 1), mats.wood);
            VegetationFactory.CreateProp(env.transform, "Drum", new Vector3(7.2f, 0.55f, 2.2f), new Vector3(0.7f, 1.1f, 0.7f), mats.metal);

            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "/ScriptableObjects/Weapons/Pistol.asset");
            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0, 0.05f, 0), new Color(0.22f, 0.48f, 0.88f), true, pistol);
            player.GetComponent<PlayerInventory>()?.SetStartingWeapon(pistol);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var qv = camGo.AddComponent<QuarterViewCamera>();
            qv.ApplyProfile(profile);
            qv.SetTarget(player.transform);
            camGo.AddComponent<CameraDebugHud>();
            camGo.AddComponent<CameraDebugPanel>();
            var calib = camGo.AddComponent<CameraReferenceCalibration>();
            var so = new SerializedObject(calib);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.ApplyModifiedPropertiesWithoutUndo();
            calib.AutoFitDistance();

            camGo.AddComponent<CameraOcclusionController>().SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene, CamScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[BR] CameraReferenceScene saved.");
        }

        [MenuItem("Tools/Battle Royale/Camera Calibration", priority = 22)]
        public static void OpenCalibration()
        {
            GenerateCameraReferenceScene();
            EditorSceneManager.OpenScene(CamScene);
            EditorUtility.DisplayDialog("Camera Calibration",
                "CameraReferenceScene opened.\nPlay Mode → F8 tweaks, F3 HUD.\nTarget char screen height 8–10%.", "OK");
        }

        [MenuItem("Tools/Battle Royale/Generate Visibility Test Scene", priority = 23)]
        public static void GenerateVisibilityTest()
        {
            EnsureFolders();
            var mats = CreatePalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WarmLighting();
            var env = new GameObject("Environment");
            MeshTerrainBuilder.Build(env.transform, 30f, 32, 1.5f, mats.grass, 7);
            ProceduralBuildingFactory.Build(env.transform, BuildingKind.HouseSmall, new Vector3(0, 0, 8), mats.wall, mats.roof, mats.floor, mats.wood);
            VegetationFactory.CreateTree(env.transform, new Vector3(-5, 0, 0), TreeKind.Large, mats.trunk, mats.leaf);

            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "/ScriptableObjects/Weapons/Pistol.asset");
            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0, 0.05f, -4), new Color(0.2f, 0.5f, 0.9f), true, pistol);
            var enemy = BlockyCharacterBuilder.Build("Enemy", new Vector3(0, 0.05f, 12), new Color(0.8f, 0.2f, 0.2f), false, pistol);
            enemy.AddComponent<BotBrain>();

            var camGo = CreateCamera(player.transform, EnsureCameraProfile());
            camGo.AddComponent<CameraOcclusionController>().SetTarget(player.transform);
            var systems = new GameObject("Systems");
            systems.AddComponent<VisibilityController>();
            systems.AddComponent<GameManager>();

            EditorSceneManager.SaveScene(scene, VisScene);
            Debug.Log("[BR] VisibilityTestScene saved.");
        }

        [MenuItem("Tools/Battle Royale/Generate Building Test Scene", priority = 24)]
        public static void GenerateBuildingTest()
        {
            EnsureFolders();
            var mats = CreatePalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WarmLighting();
            var env = new GameObject("Environment");
            MeshTerrainBuilder.Build(env.transform, 25f, 32, 1f, mats.grass, 3);
            ProceduralBuildingFactory.Build(env.transform, BuildingKind.HouseSmall, new Vector3(0, 0, 0), mats.wall, mats.roof, mats.floor, mats.wood);
            ProceduralBuildingFactory.Build(env.transform, BuildingKind.Warehouse, new Vector3(16, 0, 0), mats.concrete, mats.metal, mats.floor, mats.wood);

            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "/ScriptableObjects/Weapons/Pistol.asset");
            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0, 0.05f, -8), new Color(0.2f, 0.5f, 0.9f), true, pistol);
            CreateCamera(player.transform, EnsureCameraProfile());
            EditorSceneManager.SaveScene(scene, BldScene);
            Debug.Log("[BR] BuildingTestScene saved.");
        }

        /// <summary>Called from FullGameGenerator to replace flat plane map.</summary>
        public static void BuildDenseVillage(Transform parent, int seed = 2026)
        {
            EnsureFolders();
            var mats = CreatePalette();
            Random.InitState(seed);

            MeshTerrainBuilder.Build(parent, 110f, 48, 3.0f, mats.grass, seed);
            CreateRoad(parent, mats.asphalt);
            CreateRoadVertical(parent, mats.asphalt);

            // Buildings — varied kinds
            var spots = new (BuildingKind kind, Vector3 pos)[]
            {
                (BuildingKind.HouseSmall, new Vector3(18, 0, 14)),
                (BuildingKind.HouseTwoFloor, new Vector3(28, 0, 12)),
                (BuildingKind.Warehouse, new Vector3(-24, 0, 16)),
                (BuildingKind.Warehouse, new Vector3(-32, 0, -18)),
                (BuildingKind.Garage, new Vector3(36, 0, -24)),
                (BuildingKind.HouseSmall, new Vector3(-38, 0, 32)),
                (BuildingKind.HouseTwoFloor, new Vector3(40, 0, 28)),
                (BuildingKind.Warehouse, new Vector3(6, 0, 42)),
                (BuildingKind.Garage, new Vector3(-12, 0, -32)),
                (BuildingKind.HouseSmall, new Vector3(22, 0, -36)),
                (BuildingKind.HouseSmall, new Vector3(-45, 0, -8)),
                (BuildingKind.Garage, new Vector3(48, 0, 8))
            };

            foreach (var s in spots)
            {
                var wall = s.kind == BuildingKind.Warehouse ? mats.concrete : mats.wall;
                var roof = s.kind == BuildingKind.Warehouse ? mats.metal : mats.roof;
                ProceduralBuildingFactory.Build(parent, s.kind, s.pos, wall, roof, mats.floor, mats.wood);
                // Props around building
                ScatterProps(parent, s.pos, mats, 5);
            }

            // Trees — avoid road / buildings (stable counts for player builds)
            for (int i = 0; i < 40; i++)
            {
                Vector2 p = Random.insideUnitCircle * 90f;
                if (Mathf.Abs(p.x) < 5f || Mathf.Abs(p.y) < 5f) continue;
                if (NearBuilding(p, spots, 9f)) continue;
                var kind = (TreeKind)(i % 5);
                var leaf = kind == TreeKind.Dry ? mats.leafDry :
                    kind == TreeKind.Pine ? mats.leafDark :
                    (i % 2 == 0 ? mats.leaf : mats.leafLight);
                VegetationFactory.CreateTree(parent, new Vector3(p.x, 0, p.y), kind, mats.trunk, leaf);
            }

            for (int i = 0; i < 22; i++)
            {
                Vector2 p = Random.insideUnitCircle * 85f;
                VegetationFactory.CreateRock(parent, new Vector3(p.x, 0, p.y), mats.rock);
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 p = Random.insideUnitCircle * 80f;
                VegetationFactory.CreateBush(parent, new Vector3(p.x, 0, p.y), mats.leaf);
            }

            for (int i = 0; i < 36; i++)
            {
                Vector2 p = Random.insideUnitCircle * 95f;
                VegetationFactory.CreateGrassPatch(parent, new Vector3(p.x, 0, p.y), mats.grassTall);
            }

            ScatterForest(parent, new Vector3(-62f, 0f, 48f), mats, 16);
            ScatterForest(parent, new Vector3(58f, 0f, -52f), mats, 16);

            var dirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirt.name = "RoadShoulder";
            dirt.transform.SetParent(parent, false);
            dirt.transform.position = new Vector3(5.2f, 0.03f, 0);
            dirt.transform.localScale = new Vector3(2.2f, 0.06f, 200f);
            dirt.GetComponent<Renderer>().sharedMaterial = mats.dirt;
            Object.DestroyImmediate(dirt.GetComponent<Collider>());

            // Fence lines near houses
            for (int i = 0; i < 8; i++)
            {
                float x = -20 + i * 2.4f;
                VegetationFactory.CreateFenceSegment(parent, new Vector3(x, 0, 8), 0, mats.wood);
            }
        }

        private static bool NearBuilding(Vector2 p, (BuildingKind kind, Vector3 pos)[] spots, float r)
        {
            foreach (var s in spots)
            {
                if (Vector2.Distance(p, new Vector2(s.pos.x, s.pos.z)) < r)
                    return true;
            }
            return false;
        }

        private static void ScatterProps(Transform parent, Vector3 center, MatPalette mats, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 o = Random.insideUnitCircle * 5f;
                Vector3 pos = center + new Vector3(o.x, 0.4f, o.y - 4f);
                if (i % 3 == 0)
                {
                    var crate = VegetationFactory.CreateProp(parent, "Crate", pos, new Vector3(0.9f, 0.8f, 0.9f), mats.wood);
                    crate.AddComponent<BlockyBattleRoyale.World.DestructibleProp>().Configure(35f);
                }
                else if (i % 3 == 1)
                    VegetationFactory.CreateProp(parent, "Drum", pos + Vector3.up * 0.2f, new Vector3(0.65f, 1.0f, 0.65f), mats.metal);
                else
                    VegetationFactory.CreateProp(parent, "TireStack", pos, new Vector3(1.1f, 0.7f, 1.1f), mats.asphalt);
            }
        }

        private static void CreateRoad(Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Road_H";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0, 0.04f, 0);
            go.transform.localScale = new Vector3(7f, 0.08f, 200f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void CreateRoadVertical(Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Road_V";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0, 0.045f, 0);
            go.transform.localScale = new Vector3(160f, 0.08f, 7f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static GameObject CreateCamera(Transform target, CameraProfile profile)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var qv = camGo.AddComponent<QuarterViewCamera>();
            qv.ApplyProfile(profile);
            qv.SetTarget(target);
            camGo.AddComponent<CameraDebugHud>();
            camGo.AddComponent<CameraDebugPanel>();
            return camGo;
        }

        private static void ScatterForest(Transform parent, Vector3 center, MatPalette mats, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 o = Random.insideUnitCircle * 18f;
                var kind = (TreeKind)(i % 5);
                var leaf = kind == TreeKind.Dry ? mats.leafDry : (i % 2 == 0 ? mats.leafDark : mats.leaf);
                VegetationFactory.CreateTree(parent, center + new Vector3(o.x, 0, o.y), kind, mats.trunk, leaf);
                if (i % 2 == 0)
                    VegetationFactory.CreateBush(parent, center + new Vector3(o.x * 0.7f, 0, o.y * 0.7f), mats.leaf);
            }
        }

        private static void WarmLighting()
        {
            BlockyBattleRoyale.Rendering.SceneLighting.ApplyDaylight();
        }

        public static CameraProfile EnsureCameraProfile()
        {
            EnsureFolders();
            var p = AssetDatabase.LoadAssetAtPath<CameraProfile>(ProfilePath);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<CameraProfile>();
                p.targetCharScreenHeight = 0.09f;
                AssetDatabase.CreateAsset(p, ProfilePath);
            }
            return p;
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "_BlockyBattleRoyale");
            CreateFolder(Root, "Scenes");
            CreateFolder(Root, "Materials");
            CreateFolder(Root, "Data");
            CreateFolder(Root + "/Data", "Camera");
            CreateFolder(Root, "Prefabs");
            CreateFolder(Root + "/Prefabs", "Buildings");
        }

        private static void CreateFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private class MatPalette
        {
            public Material grass, grassTall, dirt, asphalt, concrete, wall, roof, floor, wood, metal, trunk, leaf, leafDark, leafLight, leafDry, rock;
        }

        private static MatPalette CreatePalette()
        {
            return new MatPalette
            {
                grass = Mat("V3_Grass", new Color(0.34f, 0.52f, 0.28f)),
                grassTall = Mat("V3_GrassTall", new Color(0.28f, 0.48f, 0.24f, 0.85f), "BlockyRoyale/Foliage"),
                dirt = Mat("V3_Dirt", new Color(0.45f, 0.36f, 0.26f)),
                asphalt = Mat("V3_Asphalt", new Color(0.26f, 0.26f, 0.28f)),
                concrete = Mat("V3_Concrete", new Color(0.55f, 0.55f, 0.52f)),
                wall = Mat("V3_Wall", new Color(0.78f, 0.68f, 0.52f)),
                roof = Mat("V3_Roof", new Color(0.55f, 0.28f, 0.22f)),
                floor = Mat("V3_Floor", new Color(0.55f, 0.42f, 0.3f)),
                wood = Mat("V3_Wood", new Color(0.48f, 0.34f, 0.22f)),
                metal = Mat("V3_Metal", new Color(0.62f, 0.58f, 0.4f)),
                trunk = Mat("V3_Trunk", new Color(0.35f, 0.25f, 0.16f)),
                leaf = Mat("V3_Leaf", new Color(0.28f, 0.5f, 0.26f), "BlockyRoyale/Foliage"),
                leafDark = Mat("V3_LeafDark", new Color(0.18f, 0.38f, 0.2f), "BlockyRoyale/Foliage"),
                leafLight = Mat("V3_LeafLight", new Color(0.4f, 0.62f, 0.3f), "BlockyRoyale/Foliage"),
                leafDry = Mat("V3_LeafDry", new Color(0.5f, 0.48f, 0.22f), "BlockyRoyale/Foliage"),
                rock = Mat("V3_Rock", new Color(0.42f, 0.42f, 0.4f))
            };
        }

        private static Material Mat(string name, Color c, string shaderName = "BlockyRoyale/StylizedLit")
        {
            string path = $"{MatFolder}/{name}.mat";
            var shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { color = c };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else mat.color = c;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", name.Contains("Metal") ? 0.55f : 0.16f);
            if (mat.HasProperty("_Wind")) mat.SetFloat("_Wind", 0.07f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
