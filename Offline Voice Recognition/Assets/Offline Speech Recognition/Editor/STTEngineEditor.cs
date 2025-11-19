#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
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
        private ModelDownloader _modelDownloader;
        private Dictionary<WhisperModel.ModelSize, float> _downloadProgress = new Dictionary<WhisperModel.ModelSize, float>();
        private Dictionary<WhisperModel.ModelSize, bool> _isDownloading = new Dictionary<WhisperModel.ModelSize, bool>();
        private Dictionary<WhisperModel.ModelSize, string> _downloadingStatus = new Dictionary<WhisperModel.ModelSize, string>();
        private double _lastRefreshTime;
        private WhisperModel.ModelSize? _lastClickedModel = null;
        private double _lastClickTime = -999;

        private void OnEnable()
        {
            _engine = target as STTEngine;
            _modelManager = UnityEngine.Object.FindFirstObjectByType<ModelManager>();

            Debug.Log($"[STTEngineEditor] OnEnable - Engine: {(_engine != null ? "Found" : "Null")}, Manager: {(_modelManager != null ? "Found" : "Null")}");

            // Initialize STTEngine for editor if needed
            if (_engine != null)
            {
                Debug.Log("[STTEngineEditor] Initializing STTEngine for editor mode...");
                _engine.InitializeForEditor();
            }

            // Find ModelDownloader in children of STTEngine
            if (_engine != null)
            {
                _modelDownloader = _engine.GetComponentInChildren<ModelDownloader>();

                // Subscribe to download progress events
                if (_modelDownloader != null)
                {
                    _modelDownloader.OnDownloadProgress += OnDownloadProgress;
                    _modelDownloader.OnDownloadComplete += OnDownloadComplete;
                    _modelDownloader.OnDownloadError += OnDownloadError;
                    Debug.Log("[STTEngineEditor] Successfully subscribed to ModelDownloader events");
                }
                else
                {
                    Debug.LogWarning("[STTEngineEditor] ModelDownloader not found in STTEngine children! Trying again...");
                    // Try one more time after a brief delay
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (_engine != null)
                        {
                            _modelDownloader = _engine.GetComponentInChildren<ModelDownloader>();
                            if (_modelDownloader != null)
                            {
                                _modelDownloader.OnDownloadProgress += OnDownloadProgress;
                                _modelDownloader.OnDownloadComplete += OnDownloadComplete;
                                _modelDownloader.OnDownloadError += OnDownloadError;
                                Debug.Log("[STTEngineEditor] Successfully subscribed to ModelDownloader events (delayed)");
                            }
                        }
                    };
                }
            }
            else
            {
                Debug.LogError("[STTEngineEditor] STTEngine is null!");
            }

            // Initialize download tracking
            foreach (WhisperModel.ModelSize size in System.Enum.GetValues(typeof(WhisperModel.ModelSize)))
            {
                _isDownloading[size] = false;
                _downloadProgress[size] = 0f;
                _downloadingStatus[size] = "";
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (_modelDownloader != null)
            {
                _modelDownloader.OnDownloadProgress -= OnDownloadProgress;
                _modelDownloader.OnDownloadComplete -= OnDownloadComplete;
                _modelDownloader.OnDownloadError -= OnDownloadError;
            }
        }

        /// <summary>
        /// Handle download progress updates
        /// </summary>
        private void OnDownloadProgress(float progress)
        {
            Debug.Log($"[STTEngineEditor.OnDownloadProgress] Progress: {(progress * 100):F1}%");

            // Find which model is downloading and update its progress
            if (_engine != null)
            {
                var models = _engine.GetAllModels();
                foreach (var model in models)
                {
                    if (!model.IsDownloaded && _isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size])
                    {
                        _downloadProgress[model.Size] = progress;
                        _downloadingStatus[model.Size] = $"{(progress * 100):F1}%";
                        Debug.Log($"[STTEngineEditor] Updated {model.GetSizeString()} progress to {(progress * 100):F1}%");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[STTEngineEditor.OnDownloadProgress] Engine is null!");
            }

            Repaint();
        }

        /// <summary>
        /// Handle download completion
        /// </summary>
        private void OnDownloadComplete(bool success)
        {
            Debug.Log($"[STTEngineEditor.OnDownloadComplete] Success: {success}");

            if (_engine != null)
            {
                var models = _engine.GetAllModels();
                foreach (var model in models)
                {
                    if (_isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size])
                    {
                        _isDownloading[model.Size] = false;
                        _downloadProgress[model.Size] = success ? 1f : 0f;
                        _downloadingStatus[model.Size] = success ? "Completed" : "Failed";

                        // Refresh model status
                        model.RefreshDownloadStatus();

                        Debug.Log($"[STTEngineEditor] Download {(success ? "completed" : "failed")} for {model.GetSizeString()}");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[STTEngineEditor.OnDownloadComplete] Engine is null!");
            }

            Repaint();
        }

        /// <summary>
        /// Handle download errors
        /// </summary>
        private void OnDownloadError(string error)
        {
            Debug.LogError($"Download error: {error}");

            if (_engine != null)
            {
                var models = _engine.GetAllModels();
                foreach (var model in models)
                {
                    if (_isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size])
                    {
                        _isDownloading[model.Size] = false;
                        _downloadingStatus[model.Size] = "Error";
                        break;
                    }
                }
            }

            Repaint();
        }

        public override void OnInspectorGUI()
        {
            DrawEditorHeader();
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
        /// Draw editor header
        /// </summary>
        private void DrawEditorHeader()
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
                Debug.LogError("[STTEngineEditor.DrawModelsSection] STTEngine is null!");
                return;
            }

            // Get all models
            var models = _engine.GetAllModels();

            if (models == null || models.Count == 0)
            {
                EditorGUILayout.HelpBox("No models available. Make sure STTEngine is properly initialized.", MessageType.Warning);
                Debug.LogWarning("[STTEngineEditor.DrawModelsSection] No models available!");
                return;
            }

            // Debug info about downloader
            EditorGUILayout.HelpBox($"Downloader Status: {(_modelDownloader != null ? "Connected" : "Not Connected")}, Downloading: {(_modelDownloader != null ? _modelDownloader.IsDownloading : false)}", MessageType.Info);

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
                    string status = _downloadingStatus.ContainsKey(model.Size) ? _downloadingStatus[model.Size] : "";

                    EditorGUILayout.LabelField($"Status: {status}");

                    // Simple progress bar using a filled rectangle
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.ProgressBar(rect, Mathf.Clamp01(progress), $"{Mathf.Clamp01(progress * 100):F1}%");
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
                        // Download or Cancel button based on download state
                        var guiColor = GUI.backgroundColor;

                        if (_isDownloading[model.Size])
                        {
                            // Show cancel button during download
                            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f); // Red
                            if (GUILayout.Button($"⊘ Cancel {model.GetSizeString()}", GUILayout.Height(25)))
                            {
                                Debug.Log($"[STTEngineEditor] Cancel button clicked for {model.GetSizeString()}");
                                if (_modelDownloader != null)
                                {
                                    _modelDownloader.CancelDownload();
                                    _isDownloading[model.Size] = false;
                                    _downloadProgress[model.Size] = 0f;
                                    Debug.Log($"[STTEngineEditor] Download cancelled for {model.GetSizeString()}");
                                }
                            }
                        }
                        else
                        {
                            // Show download button
                            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.8f); // Blue
                            if (GUILayout.Button($"⬇ Download {model.GetSizeString()}", GUILayout.Height(25)))
                            {
                                // Prevent double-clicks within 500ms
                                double timeSinceLastClick = EditorApplication.timeSinceStartup - _lastClickTime;
                                bool isSameModel = _lastClickedModel == model.Size;

                                if (isSameModel && timeSinceLastClick < 0.5)
                                {
                                    Debug.LogWarning($"[STTEngineEditor] Double-click prevented for {model.GetSizeString()} (clicked {timeSinceLastClick:F3}s ago)");
                                }
                                else
                                {
                                    Debug.Log($"[STTEngineEditor] Download button clicked for {model.GetSizeString()}");
                                    Debug.Log($"[STTEngineEditor] Engine: {(_engine != null ? "Found" : "Null")}, Downloader: {(_modelDownloader != null ? "Found" : "Null")}");

                                    if (_engine != null)
                                    {
                                        Debug.Log($"[STTEngineEditor] Calling DownloadModel({model.Size})");
                                        _engine.DownloadModel(model.Size);
                                        _isDownloading[model.Size] = true;
                                        _downloadProgress[model.Size] = 0f;
                                        Debug.Log($"[STTEngineEditor] Download started for {model.GetSizeString()} model ({model.GetReadableSize()})");
                                    }
                                    else
                                    {
                                        Debug.LogError("[STTEngineEditor] Engine is null, cannot start download!");
                                    }

                                    _lastClickedModel = model.Size;
                                    _lastClickTime = EditorApplication.timeSinceStartup;
                                }
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
