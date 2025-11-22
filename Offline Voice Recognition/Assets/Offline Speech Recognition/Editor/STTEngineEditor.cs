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
            // Mark that progress was updated (callback from background thread)
            // The actual UI update happens in OnInspectorGUI which calculates progress each frame
        }

        /// <summary>
        /// Handle download completion
        /// </summary>
        private void OnDownloadComplete(bool success)
        {
            if (_engine != null)
            {
                var models = _engine.GetAllModels();
                foreach (var model in models)
                {
                    if (_isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size])
                    {
                        _isDownloading[model.Size] = false;
                        _downloadProgress[model.Size] = success ? 1f : 0f;
                        _downloadingStatus[model.Size] = success ? "Downloaded" : "Failed";

                        // Refresh model status
                        model.RefreshDownloadStatus();

                        if (success)
                        {
                            Debug.Log($"{model.GetSizeString()} model downloaded successfully");
                        }
                        else
                        {
                            Debug.LogError($"{model.GetSizeString()} model download failed");
                        }
                        break;
                    }
                }
            }

            EditorApplication.delayCall += Repaint;
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
                        _downloadProgress[model.Size] = 0f;
                        break;
                    }
                }
            }

            EditorApplication.delayCall += Repaint;
        }

        public override void OnInspectorGUI()
        {
            DrawEditorHeader();
            DrawDefaultInspector();
            DrawModelsSection();
            DrawStorageSection();
            DrawDebugSection();

            // Refresh editor every frame while downloading
            Repaint();
        }

        /// <summary>
        /// Draw editor header
        /// </summary>
        private void DrawEditorHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Offline Speech Recognition - STT Engine", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Version 1.0.0 - GGML Models (whisper.cpp)", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("Models sourced from ggerganov/whisper.cpp on Hugging Face. GGML format provides optimized inference.", MessageType.Info);
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
                    // Model name with filename
                    string modelName = model.GetSizeString().ToUpper();
                    string filename = $"({model.FileName})";
                    EditorGUILayout.LabelField($"{modelName} {filename}", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

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
                }
                EditorGUILayout.EndHorizontal();

                // Model info row
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"Size: {model.GetReadableSize()}", EditorStyles.miniLabel);

                    // Show checksum info if model is downloaded
                    if (model.IsDownloaded)
                    {
                        string checksum = model.GetExpectedChecksum();
                        if (!string.IsNullOrEmpty(checksum))
                        {
                            EditorGUILayout.LabelField($"Checksum: {checksum.Substring(0, 8)}...", EditorStyles.miniLabel);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // State machine for button/progress display
                bool isDownloading = _isDownloading.ContainsKey(model.Size) && _isDownloading[model.Size];

                if (isDownloading)
                {
                    // DOWNLOADING STATE: Show progress bar and cancel button
                    // Calculate progress by checking file size on disk
                    float progress = 0f;
                    string progressText = "0.0 MB / 0 MB (0.0%)";

                    if (System.IO.File.Exists(model.ModelPath))
                    {
                        long fileSize = new System.IO.FileInfo(model.ModelPath).Length;
                        long totalSize = model.SizeInBytes;
                        if (totalSize > 0)
                        {
                            progress = (float)fileSize / totalSize;
                            float fileSizeMB = fileSize / (1024f * 1024f);
                            float totalSizeMB = totalSize / (1024f * 1024f);
                            float percentDone = progress * 100f;
                            progressText = $"{fileSizeMB:F1} MB / {totalSizeMB:F0} MB ({percentDone:F1}%)";
                        }
                    }

                    // Progress bar
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
                    EditorGUI.ProgressBar(rect, Mathf.Clamp01(progress), $"Downloading: {progressText}");
                    EditorGUILayout.Space(3);

                    // Cancel button
                    EditorGUILayout.BeginHorizontal();
                    {
                        var guiColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f); // Red
                        if (GUILayout.Button("⊘ Cancel", GUILayout.Height(25)))
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
                        GUI.backgroundColor = guiColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (model.IsDownloaded)
                {
                    // DOWNLOADED STATE: Show delete button
                    EditorGUILayout.BeginHorizontal();
                    {
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
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // NOT DOWNLOADED STATE: Show download button
                    EditorGUILayout.BeginHorizontal();
                    {
                        var guiColor = GUI.backgroundColor;
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
                                    _downloadingStatus[model.Size] = "0.0 MB / 0 MB (0.0%)";
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
                        GUI.backgroundColor = guiColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
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
