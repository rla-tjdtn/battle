#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlockyBattleRoyale.EditorTools
{
    /// <summary>
    /// After packages resolve, inject -nowarn:0619 into PackageCache rsp files
    /// so Unity 6000.6 obsolete-as-error does not block AI Nav / NGO experiments.
    /// </summary>
    [InitializeOnLoad]
    public static class PackageCompatPatcher
    {
        static PackageCompatPatcher()
        {
            EditorApplication.delayCall += TryPatch;
        }

        [MenuItem("Tools/Battle Royale/Patch Package Obsolete Warnings", priority = 90)]
        public static void TryPatch()
        {
            string cache = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Library", "PackageCache");
            if (!Directory.Exists(cache))
                return;

            int patched = 0;
            foreach (var dir in Directory.GetDirectories(cache))
            {
                string name = Path.GetFileName(dir);
                if (!(name.StartsWith("com.unity.ai.navigation") ||
                      name.StartsWith("com.unity.netcode") ||
                      name.StartsWith("com.unity.inputsystem") ||
                      name.StartsWith("com.unity.collab")))
                    continue;

                string rsp = Path.Combine(dir, "csc.rsp");
                const string line = "-nowarn:0619";
                if (File.Exists(rsp))
                {
                    string text = File.ReadAllText(rsp);
                    if (!text.Contains(line))
                    {
                        File.AppendAllText(rsp, "\n" + line + "\n");
                        patched++;
                    }
                }
                else
                {
                    File.WriteAllText(rsp, line + "\n");
                    patched++;
                }
            }

            if (patched > 0)
                Debug.Log($"[BR] Patched {patched} package csc.rsp files for CS0619.");
        }
    }
}
#endif
