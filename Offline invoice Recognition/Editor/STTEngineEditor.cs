using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace OfflineInvoiceRecognition.Editor
{
    [CustomEditor(typeof(STTEngine))]
    public class STTEngineEditor : UnityEditor.Editor
    {
        private bool isDownloading = false;
        private float downloadProgress = 0f;
        private string downloadStatus = "";

        private SerializedProperty modelSizeProp;
        private SerializedProperty maxRecordingLengthProp;
        private SerializedProperty microphoneDeviceProp;

        private void OnEnable()
        {
            modelSizeProp = serializedObject.FindProperty("modelSize");
            maxRecordingLengthProp = serializedObject.FindProperty("maxRecordingLength");
            microphoneDeviceProp = serializedObject.FindProperty("microphoneDevice");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            STTEngine engine = (STTEngine)target;

            // Header
            EditorGUILayout.Space();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Offline Invoice Recognition", titleStyle);
            EditorGUILayout.LabelField("Speech-to-Text Engine", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Model Configuration Section
            EditorGUILayout.LabelField("Model Configuration", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(modelSizeProp, new GUIContent("Model Size"));
            bool modelChanged = EditorGUI.EndChangeCheck();

            var config = ModelConfig.GetConfig((ModelSize)modelSizeProp.enumValueIndex);
            bool isDownloaded = config.IsDownloaded();

            // Model Info Box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Model Information", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Name:", config.modelName);
            EditorGUILayout.LabelField("Size:", new ModelDownloader(config).GetEstimatedSize());
            EditorGUILayout.LabelField("Status:", isDownloaded ? "✓ Downloaded" : "✗ Not Downloaded");

            if (isDownloaded)
            {
                EditorGUILayout.LabelField("Encoder:", config.GetLocalEncoderPath(), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Decoder:", config.GetLocalDecoderPath(), EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Download Section
            if (!isDownloaded)
            {
                EditorGUILayout.HelpBox(
                    $"Model '{config.modelName}' is not downloaded. Click the button below to download it from HuggingFace.",
                    MessageType.Warning
                );

                GUI.enabled = !isDownloading;
                if (GUILayout.Button(isDownloading ? "Downloading..." : "Download Model", GUILayout.Height(30)))
                {
                    DownloadModelAsync(engine);
                }
                GUI.enabled = true;

                if (isDownloading)
                {
                    EditorGUILayout.Space();
                    Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(progressRect, downloadProgress, $"{downloadProgress * 100:F0}%");
                    EditorGUILayout.LabelField(downloadStatus, EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Re-download Model", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Re-download Model",
                        $"Are you sure you want to re-download {config.modelName}? This will replace the existing model.",
                        "Yes", "Cancel"))
                    {
                        // Delete existing model
                        var downloader = new ModelDownloader(config);
                        downloader.DeleteModel();

                        // Download again
                        DownloadModelAsync(engine);
                    }
                }

                if (GUILayout.Button("Delete Model", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Model",
                        $"Are you sure you want to delete {config.modelName}?",
                        "Yes", "Cancel"))
                    {
                        var downloader = new ModelDownloader(config);
                        if (downloader.DeleteModel())
                        {
                            EditorUtility.DisplayDialog("Success", "Model deleted successfully.", "OK");
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Audio Settings Section
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxRecordingLengthProp, new GUIContent("Max Recording Length (sec)"));

            // Microphone device dropdown
            string[] devices = Microphone.devices;
            if (devices.Length > 0)
            {
                string currentDevice = microphoneDeviceProp.stringValue;
                int selectedIndex = System.Array.IndexOf(devices, currentDevice);
                if (selectedIndex < 0) selectedIndex = 0;

                string[] options = new string[devices.Length + 1];
                options[0] = "Default Microphone";
                System.Array.Copy(devices, 0, options, 1, devices.Length);

                int newIndex = EditorGUILayout.Popup("Microphone Device", selectedIndex + 1, options);
                if (newIndex == 0)
                    microphoneDeviceProp.stringValue = "";
                else
                    microphoneDeviceProp.stringValue = devices[newIndex - 1];
            }
            else
            {
                EditorGUILayout.HelpBox("No microphone devices detected.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Runtime Info Section
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.enabled = false;
            EditorGUILayout.Toggle("Is Initialized", engine.IsInitialized);
            EditorGUILayout.Toggle("Is Recording", engine.IsRecording);
            if (engine.IsInitialized)
            {
                EditorGUILayout.LabelField("Current Model:", engine.CurrentModelName);
                if (engine.LastTranscriptionTime > 0)
                {
                    EditorGUILayout.LabelField("Last Transcription:", $"{engine.LastTranscriptionTime:F2}s");
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Quick Actions (Runtime only)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (!engine.IsInitialized)
                {
                    if (GUILayout.Button("Initialize Engine", GUILayout.Height(30)))
                    {
                        _ = engine.Initialize();
                    }
                }
                else
                {
                    GUI.enabled = !engine.IsRecording;
                    if (GUILayout.Button("Start Recording", GUILayout.Height(30)))
                    {
                        engine.StartRecording();
                    }
                    GUI.enabled = true;

                    GUI.enabled = engine.IsRecording;
                    if (GUILayout.Button("Stop & Transcribe", GUILayout.Height(30)))
                    {
                        _ = engine.StopRecordingAndTranscribe();
                    }
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();

            // Auto-repaint while downloading
            if (isDownloading)
            {
                Repaint();
            }
        }

        private async void DownloadModelAsync(STTEngine engine)
        {
            isDownloading = true;
            downloadProgress = 0f;
            downloadStatus = "Starting download...";

            try
            {
                bool success = await engine.DownloadModel((progress) =>
                {
                    downloadProgress = progress;
                });

                if (success)
                {
                    downloadStatus = "Download complete!";
                    EditorUtility.DisplayDialog("Success",
                        "Model downloaded successfully! You can now initialize the STT Engine.",
                        "OK");
                }
                else
                {
                    downloadStatus = "Download failed";
                    EditorUtility.DisplayDialog("Error",
                        "Failed to download model. Check the console for details.",
                        "OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Download error: {e.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Download error: {e.Message}",
                    "OK");
            }
            finally
            {
                isDownloading = false;
                downloadProgress = 0f;
            }
        }
    }
}
