#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BlockyBattleRoyale.EditorTools
{
    public static class WindowsBuild
    {
        [MenuItem("Tools/Battle Royale/Build Windows x64", priority = 100)]
        public static void Build()
        {
            // IL2CPP module may be missing on Personal installs — force Mono
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1); // x64
            // Prefer D3D11 for stability on some NVIDIA setups
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);

            string dir = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Builds", "Windows");
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "BlockyBattleRoyale.exe");

            var scenes = new[]
            {
                "Assets/_BlockyBattleRoyale/Scenes/MainMenu.unity",
                "Assets/_BlockyBattleRoyale/Scenes/GameWorld.unity"
            };

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[BR] Windows build: {report.summary.result} → {exe}");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Build", $"{report.summary.result}\n{exe}", "OK");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
        }

        public static void BuildAndQuit()
        {
            Build();
            EditorApplication.Exit(0);
        }
    }
}
#endif
