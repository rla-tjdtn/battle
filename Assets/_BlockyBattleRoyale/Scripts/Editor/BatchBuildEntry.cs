#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlockyBattleRoyale.EditorTools
{
    public static class BatchBuildEntry
    {
        public static void GenerateAndQuit()
        {
            Debug.Log("[Blocky BR] Batch generate start");
            try
            {
                FullGameGenerator.GenerateFullGame();
                Debug.Log("[Blocky BR] Batch generate success");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Blocky BR] Batch generate failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void SetupUrpAndGenerate()
        {
            UrpSetupHelper.SetupUrp();
            GenerateAndQuit();
        }

        /// <summary>
        /// Two-phase safe path: import scripts first run uses GenerateOnly;
        /// this method regenerates scenes AFTER scripts exist, then builds.
        /// </summary>
        public static void GenerateAndBuildAndQuit()
        {
            Debug.Log("[Blocky BR] Safe regenerate + Windows build");
            try
            {
                // Ensure new scripts are imported before baking scenes
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();

                FullGameGenerator.GenerateFullGame();

                try
                {
                    VillageSliceGenerator.GenerateCameraReferenceScene();
                    VillageSliceGenerator.GenerateVisibilityTest();
                    VillageSliceGenerator.GenerateBuildingTest();
                }
                catch (System.Exception te)
                {
                    Debug.LogWarning("[BR] Test scenes optional fail: " + te.Message);
                }

                // Re-open / re-save GameWorld once more so script GUIDs resolve
                FullGameGenerator.GenerateFullGame();

                WindowsBuild.Build();

                // Force D3D11 in boot.config for player stability
                string boot = System.IO.Path.Combine(
                    System.IO.Directory.GetParent(Application.dataPath)!.FullName,
                    "Builds", "Windows", "BlockyBattleRoyale_Data", "boot.config");
                if (System.IO.File.Exists(boot))
                {
                    string text = System.IO.File.ReadAllText(boot);
                    if (!text.Contains("gfx-enable-gfx-jobs"))
                    { }
                    System.IO.File.AppendAllText(boot, "\nplayer-connection-ip=\n");
                }

                Debug.Log("[Blocky BR] Generate + build success");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Blocky BR] Generate + build failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void GenerateV3AndQuit()
        {
            GenerateAndQuit();
        }
    }
}
#endif
