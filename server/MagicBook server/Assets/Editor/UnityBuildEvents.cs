using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;

namespace CustomBuildEvents
{
    public class SharedScriptsImporter : ScriptableWizard
    {
        [System.Serializable]
        public struct Script
        {
            public string ScriptName;
            public bool Import;
        }

        public List<Script> ScriptsToImport = new();

        [MenuItem("Custom/Reimport shared scripts")]
        static void CopyShared()
        {
            Debug.Log("Copying shared scripts...");

            DisplayWizard<SharedScriptsImporter>("Import shared scripts", "Import");
        }

        private void OnFocus()
        {
            var sourceDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "shared"));
            var destDir = Path.Combine(Application.dataPath, "shared");

            if (!Directory.Exists(sourceDir))
                return;

            Directory.CreateDirectory(destDir);

            ScriptsToImport.Clear();
            foreach (var f in Directory.EnumerateFiles(sourceDir))
            {
                FileInfo file = new FileInfo(f);
                FileInfo destFile = new FileInfo(Path.Combine(destDir, file.Name));
                if (destFile != null && file != null && !file.Name.StartsWith('.'))
                {
                    ScriptsToImport.Add(new Script { ScriptName = f, Import = destFile.Exists });
                }
            }
        }

        void OnWizardCreate()
        {
            var sourceDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "shared"));
            var destDir = Path.Combine(Application.dataPath, "shared");

            if (!Directory.Exists(sourceDir))
                return;

            Directory.CreateDirectory(destDir);

            var needRefresh = false;
            foreach (var f in Directory.EnumerateFiles(sourceDir))
            {
                FileInfo file = new FileInfo(f);
                FileInfo destFile = new FileInfo(Path.Combine(destDir, file.Name));
                if (destFile != null && file != null && !file.Name.StartsWith('.') && ScriptsToImport.FindIndex(s => s.ScriptName == f && s.Import) >= 0)
                {
                    if (!destFile.Exists || file.LastWriteTime > destFile.LastWriteTime)
                    {
                        Debug.Log($"Shared file {f} timestamp {file.LastWriteTime} and instance {destFile.LastWriteTime}");
                        needRefresh = true;

                        if (destFile.Exists)
                            destFile.IsReadOnly = false;

                        var resultingFile = file.CopyTo(destFile.FullName, overwrite: true);
                        resultingFile.IsReadOnly = true;
                    }
                }
            }

            if (needRefresh)
                AssetDatabase.Refresh();
            else
                Debug.Log("Nothing new to import");
        }
    }

}
