#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlockyBattleRoyale.EditorTools
{
    public static class UrpSetupHelper
    {
        [MenuItem("Tools/Blocky Battle Royale/Setup URP Pipeline Asset", priority = 20)]
        public static void SetupUrp()
        {
            // Prefer existing pipeline
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                EditorUtility.DisplayDialog("URP", "Render Pipeline Asset is already assigned.", "OK");
                return;
            }

            // Try to create via URP types if package is imported
            var rpAssetType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            var rendererDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");

            if (rpAssetType == null || rendererDataType == null)
            {
                EditorUtility.DisplayDialog(
                    "URP",
                    "URP package types not found yet.\n\n1) Open Window > Package Manager\n2) Wait for Universal RP to finish importing\n3) Run this menu again\n\nOr: Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings",
                    "OK");
                return;
            }

            const string folder = "Assets/_BlockyBattleRoyale/Settings";
            if (!AssetDatabase.IsValidFolder("Assets/_BlockyBattleRoyale/Settings"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_BlockyBattleRoyale"))
                    AssetDatabase.CreateFolder("Assets", "_BlockyBattleRoyale");
                AssetDatabase.CreateFolder("Assets/_BlockyBattleRoyale", "Settings");
            }

            var rendererData = ScriptableObject.CreateInstance(rendererDataType);
            AssetDatabase.CreateAsset(rendererData, folder + "/URP_Renderer.asset");

            var pipeline = ScriptableObject.CreateInstance(rpAssetType);
            // Assign renderer list via SerializedObject for compatibility across URP versions
            var so = new SerializedObject(pipeline);
            var rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var defaultRenderer = so.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null)
            {
                defaultRenderer.intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(pipeline, folder + "/URP_Pipeline.asset");

            GraphicsSettings.defaultRenderPipeline = pipeline as RenderPipelineAsset;
            QualitySettings.renderPipeline = pipeline as RenderPipelineAsset;

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("URP", "URP Pipeline Asset created and assigned.", "OK");
        }
    }
}
#endif
