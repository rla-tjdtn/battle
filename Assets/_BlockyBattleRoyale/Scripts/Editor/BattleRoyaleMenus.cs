#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlockyBattleRoyale.CameraSystem;

namespace BlockyBattleRoyale.EditorTools
{
    /// <summary>
    /// V2 menu aliases under Tools/Battle Royale (maps to existing generators).
    /// </summary>
    public static class BattleRoyaleMenus
    {
        [MenuItem("Tools/Battle Royale/Generate Full Prototype", priority = 0)]
        public static void GenerateFull()
        {
            FullGameGenerator.GenerateFullGame();
        }

        [MenuItem("Tools/Battle Royale/Generate Project Setup", priority = 10)]
        public static void GenerateSetup()
        {
            FullGameGenerator.GenerateFullGame();
        }

        [MenuItem("Tools/Battle Royale/Setup URP", priority = 11)]
        public static void SetupUrp()
        {
            UrpSetupHelper.SetupUrp();
        }

        [MenuItem("Tools/Battle Royale/Apply Duckov Camera Defaults", priority = 20)]
        public static void ApplyCamera()
        {
            var cam = Object.FindFirstObjectByType<QuarterViewCamera>();
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Camera", "No QuarterViewCamera in open scene.", "OK");
                return;
            }

            cam.ApplyDuckovDefaults();
            EditorUtility.SetDirty(cam);
            Debug.Log("[BR] Duckov camera defaults applied.");
        }

        [MenuItem("Tools/Battle Royale/Open MainMenu", priority = 30)]
        public static void OpenMenu()
        {
            EditorSceneManager.OpenScene("Assets/_BlockyBattleRoyale/Scenes/MainMenu.unity");
        }

        [MenuItem("Tools/Battle Royale/Open GameWorld", priority = 31)]
        public static void OpenWorld()
        {
            EditorSceneManager.OpenScene("Assets/_BlockyBattleRoyale/Scenes/GameWorld.unity");
        }

        [MenuItem("Tools/Battle Royale/Validate Project", priority = 40)]
        public static void Validate()
        {
            int issues = 0;
            if (!System.IO.File.Exists("Assets/_BlockyBattleRoyale/Scenes/MainMenu.unity"))
            {
                Debug.LogError("Missing MainMenu scene");
                issues++;
            }
            if (!System.IO.File.Exists("Assets/_BlockyBattleRoyale/Scenes/GameWorld.unity"))
            {
                Debug.LogError("Missing GameWorld scene");
                issues++;
            }

            EditorUtility.DisplayDialog(
                "Validate",
                issues == 0 ? "Core scenes present. See DEVELOPMENT_STATUS.md for full status." : $"Issues: {issues}",
                "OK");
        }
    }
}
#endif
