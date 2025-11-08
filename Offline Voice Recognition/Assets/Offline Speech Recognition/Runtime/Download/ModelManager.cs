using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using OfflineSpeechRecognition.Core;

namespace OfflineSpeechRecognition.Download
{
    /// <summary>
    /// Manages local Whisper models - handles storage, validation, and cleanup
    /// </summary>
    public class ModelManager : MonoBehaviour
    {
        private static ModelManager _instance;

        private Dictionary<WhisperModel.ModelSize, WhisperModel> _models;
        private string _modelsCachePath;

        public static ModelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ModelManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("ModelManager");
                        _instance = obj.AddComponent<ModelManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        /// <summary>
        /// Initialize the model manager
        /// </summary>
        private void Initialize()
        {
            _models = new Dictionary<WhisperModel.ModelSize, WhisperModel>();
            _modelsCachePath = Path.Combine(
                Application.persistentDataPath,
                Utilities.Constants.MODELS_FOLDER_NAME
            );

            // Ensure models directory exists
            if (!Directory.Exists(_modelsCachePath))
            {
                Directory.CreateDirectory(_modelsCachePath);
            }

            // Initialize all models
            foreach (WhisperModel.ModelSize size in System.Enum.GetValues(typeof(WhisperModel.ModelSize)))
            {
                var model = new WhisperModel(size);
                _models[size] = model;
            }
        }

        /// <summary>
        /// Get a specific model by size
        /// </summary>
        public WhisperModel GetModel(WhisperModel.ModelSize size)
        {
            if (_models.TryGetValue(size, out var model))
            {
                model.RefreshDownloadStatus();
                return model;
            }

            Debug.LogError($"Model {size} not found in manager");
            return null;
        }

        /// <summary>
        /// Get all available models
        /// </summary>
        public List<WhisperModel> GetAllModels()
        {
            // Ensure initialization
            if (_models == null)
            {
                Initialize();
            }

            foreach (var model in _models.Values)
            {
                model.RefreshDownloadStatus();
            }

            return _models.Values.ToList();
        }

        /// <summary>
        /// Get all downloaded models
        /// </summary>
        public List<WhisperModel> GetDownloadedModels()
        {
            // Ensure initialization
            if (_models == null)
            {
                Initialize();
            }

            var downloaded = new List<WhisperModel>();

            foreach (var model in _models.Values)
            {
                model.RefreshDownloadStatus();
                if (model.IsDownloaded)
                {
                    downloaded.Add(model);
                }
            }

            return downloaded;
        }

        /// <summary>
        /// Check if a model is downloaded
        /// </summary>
        public bool IsModelDownloaded(WhisperModel.ModelSize size)
        {
            var model = GetModel(size);
            return model != null && model.IsDownloaded;
        }

        /// <summary>
        /// Get the path to a model file
        /// </summary>
        public string GetModelPath(WhisperModel.ModelSize size)
        {
            var model = GetModel(size);
            if (model != null && model.IsDownloaded)
            {
                return model.ModelPath;
            }

            return null;
        }

        /// <summary>
        /// Get the directory where a model is stored
        /// </summary>
        public string GetModelDirectory(WhisperModel.ModelSize size)
        {
            var model = GetModel(size);
            return model?.GetModelDirectory();
        }

        /// <summary>
        /// Delete a downloaded model
        /// </summary>
        public bool DeleteModel(WhisperModel.ModelSize size)
        {
            try
            {
                var model = GetModel(size);
                if (model != null && model.IsDownloaded)
                {
                    string directory = model.GetModelDirectory();
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                        model.RefreshDownloadStatus();
                        Debug.Log($"Model {size} deleted successfully");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete model {size}: {ex.Message}");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Clear all downloaded models
        /// </summary>
        public bool ClearAllModels()
        {
            try
            {
                if (Directory.Exists(_modelsCachePath))
                {
                    Directory.Delete(_modelsCachePath, true);
                    Directory.CreateDirectory(_modelsCachePath);

                    // Refresh all models
                    foreach (var model in _models.Values)
                    {
                        model.RefreshDownloadStatus();
                    }

                    Debug.Log("All models cleared successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear all models: {ex.Message}");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Get total size of downloaded models in bytes
        /// </summary>
        public long GetTotalDownloadedSize()
        {
            long totalSize = 0;

            try
            {
                if (Directory.Exists(_modelsCachePath))
                {
                    var info = new DirectoryInfo(_modelsCachePath);
                    totalSize = GetDirectorySize(info);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to calculate total size: {ex.Message}");
            }

            return totalSize;
        }

        /// <summary>
        /// Get the size of a directory recursively
        /// </summary>
        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;

            try
            {
                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    size += file.Length;
                }

                var directories = dir.GetDirectories();
                foreach (var directory in directories)
                {
                    size += GetDirectorySize(directory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating directory size: {ex.Message}");
            }

            return size;
        }

        /// <summary>
        /// Validate a model file (check if it exists and is not corrupted)
        /// </summary>
        public bool ValidateModel(WhisperModel.ModelSize size)
        {
            var model = GetModel(size);
            if (model != null && model.IsDownloaded)
            {
                // Check if file exists
                if (File.Exists(model.ModelPath))
                {
                    // Try to get file size (basic validation)
                    try
                    {
                        var info = new FileInfo(model.ModelPath);
                        // Basic check: file should be larger than 10MB for any model
                        return info.Length > 10 * 1024 * 1024;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get human readable storage info
        /// </summary>
        public string GetStorageInfo()
        {
            var sb = new System.Text.StringBuilder();
            long totalSize = GetTotalDownloadedSize();

            sb.AppendLine($"Models Storage Path: {_modelsCachePath}");
            sb.AppendLine($"Total Downloaded Size: {FormatBytes(totalSize)}");
            sb.AppendLine("\nDownloaded Models:");

            foreach (var model in GetDownloadedModels())
            {
                sb.AppendLine($"  - {model.GetSizeString()}: {model.GetReadableSize()}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format bytes to human-readable format
        /// </summary>
        private string FormatBytes(long bytes)
        {
            const long gb = 1024 * 1024 * 1024;
            const long mb = 1024 * 1024;
            const long kb = 1024;

            if (bytes >= gb)
                return $"{(double)bytes / gb:F2} GB";
            else if (bytes >= mb)
                return $"{(double)bytes / mb:F2} MB";
            else if (bytes >= kb)
                return $"{(double)bytes / kb:F2} KB";
            else
                return $"{bytes} B";
        }
    }
}
