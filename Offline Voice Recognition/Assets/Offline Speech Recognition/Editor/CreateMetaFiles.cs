#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OfflineSpeechRecognition.Editor
{
    /// <summary>
    /// Utility to create missing .meta files for folders
    /// </summary>
    public class CreateMetaFiles
    {
        [MenuItem("Assets/Offline Speech Recognition/Generate Missing Meta Files")]
        public static void GenerateMissingMetaFiles()
        {
            string basePath = "Assets/Offline Speech Recognition";
            CreateMetaFilesForDirectory(basePath);
            AssetDatabase.Refresh();
            Debug.Log("Meta files generated successfully");
        }

        private static void CreateMetaFilesForDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return;

            // Create meta file for current directory if missing
            string metaPath = dirPath + ".meta";
            if (!File.Exists(metaPath))
            {
                CreateMetaFile(metaPath);
            }

            // Process subdirectories
            string[] subdirs = Directory.GetDirectories(dirPath);
            foreach (string subdir in subdirs)
            {
                // Skip hidden folders
                if (Path.GetFileName(subdir).StartsWith("."))
                    continue;

                CreateMetaFilesForDirectory(subdir);
            }
        }

        private static void CreateMetaFile(string metaPath)
        {
            string guid = GenerateGuid();
            string content = $@"fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
";

            File.WriteAllText(metaPath, content, Encoding.UTF8);
            Debug.Log($"Created meta file: {metaPath}");
        }

        private static string GenerateGuid()
        {
            // Generate a simple GUID-like string
            return System.Guid.NewGuid().ToString().Replace("-", "").Substring(0, 32);
        }
    }
}
#endif
