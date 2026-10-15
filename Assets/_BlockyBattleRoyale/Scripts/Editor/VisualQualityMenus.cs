#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using BlockyBattleRoyale.Rendering;

namespace BlockyBattleRoyale.EditorTools
{
    public static class VisualQualityMenus
    {
        [MenuItem("Tools/Blocky Royale/Configure Lighting", priority = 40)]
        public static void ConfigureLighting()
        {
            SceneLighting.ApplyDaylight();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Blocky Royale/Apply Art Direction", priority = 41)]
        public static void ApplyArt()
        {
            ConfigureLighting();
            int changed = 0;
            var shader = Shader.Find("BlockyRoyale/StylizedLit");
            if (shader == null)
            {
                Debug.LogWarning("StylizedLit shader missing.");
                return;
            }
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mat = r.sharedMaterial;
                if (mat == null || mat.shader == null) continue;
                string n = mat.shader.name;
                if (n.Contains("Foliage") || n.Contains("Sprites") || n.Contains("UI") || n.Contains("Text"))
                    continue;
                if (n.Contains("Universal Render Pipeline/Lit") || n.Contains("Standard"))
                {
                    mat.shader = shader;
                    EditorUtility.SetDirty(mat);
                    changed++;
                }
            }
            Debug.Log($"[Art] Switched {changed} materials to StylizedLit.");
        }

        [MenuItem("Tools/Blocky Royale/Visual Quality Audit", priority = 42)]
        public static void Audit()
        {
            int renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            int primitives = 0;
            foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (f.sharedMesh != null && f.sharedMesh.name.StartsWith("Cube") || (f.sharedMesh != null && (f.sharedMesh.name.Contains("Sphere") || f.sharedMesh.name.Contains("Cylinder"))))
                    primitives++;
            }
            var sb = new StringBuilder();
            sb.AppendLine("# Visual Quality Audit");
            sb.AppendLine("Generated from the open scene. Not a Play Mode screenshot comparison.");
            sb.AppendLine($"- Renderers: {renderers}");
            sb.AppendLine($"- Primitive meshes (cube/sphere/cylinder): {primitives}");
            sb.AppendLine("- Imported FBX/OBJ/GLB: none in Assets. Environment is procedural.");
            sb.AppendLine("- Placeholder: buildings, trees, character, props are generated meshes, not authored models.");
            sb.AppendLine("- Scores are estimates, not reference-matched captures.");
            sb.AppendLine("  Camera 3, Terrain 3, Forest 3, Buildings 2, Character 2, UI 2, VFX 2");
            string path = "VISUAL_QUALITY.md";
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[Art] Wrote " + path);
        }

        [MenuItem("Tools/Blocky Royale/Improve Environment", priority = 43)]
        public static void ImproveEnvironment()
        {
            VillageSliceGenerator.BuildDenseVillage(
                new GameObject("Environment").transform, 2026);
            ConfigureLighting();
        }

        [MenuItem("Tools/Blocky Royale/Optimize Materials", priority = 44)]
        public static void OptimizeMaterials()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_BlockyBattleRoyale/Materials" }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null) continue;
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Art] Instancing enabled on {n} materials.");
        }

        [MenuItem("Tools/Blocky Royale/Validate Visual Quality", priority = 45)]
        public static void Validate()
        {
            bool shader = Shader.Find("BlockyRoyale/StylizedLit") != null;
            bool foliage = Shader.Find("BlockyRoyale/Foliage") != null;
            Debug.Log($"[Art] StylizedLit={shader} Foliage={foliage}. Play Mode screenshot compare is still unverified.");
        }
    }
}
#endif
