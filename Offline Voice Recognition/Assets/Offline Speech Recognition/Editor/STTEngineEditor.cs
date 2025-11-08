#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Download;
using Debug = UnityEngine.Debug;

namespace OfflineSpeechRecognition.Editor
{
    /// <summary>
    /// Custom inspector for STTEngine with download buttons and progress bar
    /// </summary>
    [CustomEditor(typeof(STTEngine))]
    public class STTEngineEditor : UnityEditor.Editor
    {
        private STTEngine _engine;
        private ModelManager _modelManager;
        private Dictionary<WhisperModel.ModelSize, float> _downloadProgress = new Dictionary<WhisperModel.ModelSize, float>();
        private Dictionary<WhisperModel.ModelSize, bool> _isDownloading = new Dictionary<WhisperModel.ModelSize, bool>();
        private double _lastRefreshTime;

        private void OnEnable()
        {
            _engine = target as STTEngine;
            _modelManager = FindObjectOfType<ModelManager>();

            // Initialize download tracking
            foreach (WhisperModel.ModelSize size in System.Enum.GetValues(typeof(WhisperModel.ModelSize)))
            {
                _isDownloading[size] = false;
                _downloadProgress[size] = 0f;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawHeader();
            DrawDefaultInspector();
            DrawModelsSection();
            DrawStorageSection();
            DrawDebugSection();

            // Refresh editor periodically
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > 0.5)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        /// <summary>
        /// Draw header
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Offline Speech Recognition - STT Engine", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Version 1.0.0", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draw models section with download buttons
        /// </summary>
        private void DrawModelsSection()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Model Management", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Download Whisper models from Hugging Face. Models are stored in persistent data path.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (_engine == null)
            {
                EditorGUILayout.HelpBox("STTEngine reference is null", MessageType.Error);
                return;
            }

            // Get all models
            var models = _engine.GetAllModels();

            if (models == null || models.Count == 0)
            {
                EditorGUILayout.HelpBox("No models available. Make sure STTEngine is properly initialized.", MessageType.Warning);
                return;
            }

            // Draw each model
            foreach (var model in models)
            {
                if (model != null)
                {
                    DrawModelItem(model);
                }
            }

            EditorGUILayout.Space(10);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            {
                // Download All button
                if (GUILayout.Button("Download All Models", GUILayout.Height(30)))
                {
                    foreach (var model in models)
                    {
                        if (model != null && !model.IsDownloaded)
                        {
                            _engine.DownloadModel(model.Size);
                            if (!_isDownloading.ContainsKey(model.Size))
                                _isDownloading[model.Size] = true;
                            else
                                _isDownloading[model.Size] = true;
                        }
                    }
                }

                // Clear All button
                if (GUILayout.Button("Clear All Models", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Models", "Delete all downloaded models? This cannot be undone.", "Delete", "Cancel"))
                    {
                        if (_modelManager != null)
                        {
                            _modelManager.ClearAllModels();
                            Debug.Log("All models cleared successfully");
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw individual model item with better UI
        /// </summary>
        private void DrawModelItem(WhisperModel model)
        {
            EditorGUILayout.BeginVertical("box");
            {
                // Model header with status
                EditorGUILayout.BeginHorizontal();
                {
                    // Model name
                    string modelName = model.GetSizeString().ToUpper();
                    EditorGUILayout.LabelField(modelName, EditorStyles.boldLabel, GUILayout.Width(70));

                    // Status badge
                    if (model.IsDownloaded)
                    {
                        var guiColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // Green
                        EditorGUILayout.LabelField("✓ Downloaded", EditorStyles.miniLabel, GUILayout.Width(100));
                        GUI.backgroundColor = guiColor;
                    }
                    else
                    {
                        var guiColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f); // Red
                        EditorGUILayout.LabelField("✗ Not Downloaded", EditorStyles.miniLabel, GUILayout.Width(120));
                        GUI.backgroundColor = guiColor;
                    }

                    // Size info
                    EditorGUILayout.LabelField(model.GetReadableSize(), EditorStyles.miniLabel, GUILayout.Width(90));

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // Progress bar if downloading
                if (_isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size])
                {
                    float progress = _downloadProgress.ContainsKey(model.Size) ? _downloadProgress[model.Size] : 0f;
                    EditorGUILayout.LabelField($"Downloading... {(progress * 100):F0}%");

                    // Simple progress bar using a filled rectangle
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.ProgressBar(rect, progress, $"{(progress * 100):F0}%");
                    EditorGUILayout.Space(3);
                }

                // Action buttons
                EditorGUILayout.BeginHorizontal();
                {
                    if (model.IsDownloaded)
                    {
                        // Delete button
                        var guiColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f); // Red
                        if (GUILayout.Button("🗑 Delete", GUILayout.Height(25)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Model",
                                $"Delete {model.GetSizeString()} model ({model.GetReadableSize()})? This cannot be undone.",
                                "Delete", "Cancel"))
                            {
                                if (_modelManager != null)
                                {
                                    _modelManager.DeleteModel(model.Size);
                                    Debug.Log($"Model {model.GetSizeString()} deleted successfully");
                                }
                            }
                        }
                        GUI.backgroundColor = guiColor;

                        // View button
                        if (GUILayout.Button("📁 View Storage", GUILayout.Height(25)))
                        {
                            string path = model.GetModelDirectory();
                            if (!string.IsNullOrEmpty(path))
                            {
                                RevealInExplorer(path);
                            }
                        }
                    }
                    else
                    {
                        // Download button
                        var guiColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.8f); // Blue
                        if (GUILayout.Button($"⬇ Download {model.GetSizeString()}", GUILayout.Height(25)))
                        {
                            if (_engine != null)
                            {
                                _engine.DownloadModel(model.Size);
                                _isDownloading[model.Size] = true;
                                _downloadProgress[model.Size] = 0f;
                                Debug.Log($"Starting download of {model.GetSizeString()} model ({model.GetReadableSize()})");
                            }
                        }
                        GUI.backgroundColor = guiColor;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draw storage information section
        /// </summary>
        private void DrawStorageSection()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Storage Information", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            if (_modelManager != null)
            {
                string storageInfo = _modelManager.GetStorageInfo();
                EditorGUILayout.TextArea(storageInfo, GUILayout.Height(100));
            }
            else
            {
                EditorGUILayout.TextArea("ModelManager not found", GUILayout.Height(100));
            }
        }

        /// <summary>
        /// Draw debug section
        /// </summary>
        private void DrawDebugSection()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            {
                if (_engine != null)
                {
                    string debugInfo = _engine.GetDebugInfo();
                    EditorGUILayout.TextArea(debugInfo, GUILayout.Height(120));
                }
                else
                {
                    EditorGUILayout.TextArea("STTEngine not initialized", GUILayout.Height(120));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Refresh button
            if (GUILayout.Button("🔄 Refresh Models Status", GUILayout.Height(25)))
            {
                if (_engine != null)
                {
                    var models = _engine.GetAllModels();
                    if (models != null)
                    {
                        foreach (var model in models)
                        {
                            if (model != null)
                                model.RefreshDownloadStatus();
                        }
                        Debug.Log("Models status refreshed");
                    }
                }
            }
        }

        /// <summary>
        /// Reveal folder in file explorer
        /// </summary>
        private void RevealInExplorer(string path)
        {
            #if UNITY_WINDOWS
            string winPath = path.Replace("/", "\\");
            Process.Start("explorer.exe", "/select,\"" + winPath + "\"");
            #elif UNITY_MACOS
            Process.Start("open", "-R \"" + path + "\"");
            #elif UNITY_LINUX
            Process.Start("nautilus", "\"" + path + "\"");
            #endif
        }
    }
}
#endif
