using System;
using System.IO;
using UnityEngine;

namespace OfflineSpeechRecognition.Core
{
    /// <summary>
    /// Represents a Whisper model with its metadata
    /// </summary>
    [System.Serializable]
    public class WhisperModel
    {
        /// <summary>
        /// Available Whisper model sizes
        /// </summary>
        public enum ModelSize
        {
            Tiny,    // ~140 MB
            Base,    // ~290 MB
            Small,   // ~770 MB
            Medium,  // ~1.5 GB
            Large    // ~3.1 GB
        }

        /// <summary>
        /// The size/type of the model
        /// </summary>
        public ModelSize Size { get; private set; }

        /// <summary>
        /// The folder name of the model (e.g., "whisper-tiny")
        /// </summary>
        public string FolderName { get; private set; }

        /// <summary>
        /// Full path to the model file
        /// </summary>
        public string ModelPath { get; private set; }

        /// <summary>
        /// Whether the model is currently downloaded
        /// </summary>
        public bool IsDownloaded { get; private set; }

        /// <summary>
        /// Size of the model in bytes
        /// </summary>
        public long SizeInBytes { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public WhisperModel(ModelSize size)
        {
            Size = size;
            FolderName = $"whisper-{GetSizeString()}";
            UpdatePath();
            RefreshDownloadStatus();
        }

        /// <summary>
        /// Get the string representation of the model size
        /// </summary>
        public string GetSizeString()
        {
            return Size switch
            {
                ModelSize.Tiny => "tiny",
                ModelSize.Base => "base",
                ModelSize.Small => "small",
                ModelSize.Medium => "medium",
                ModelSize.Large => "large",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Get the human-readable size of the model
        /// </summary>
        public string GetReadableSize()
        {
            if (!Utilities.Constants.MODEL_SIZES.TryGetValue(GetSizeString(), out var bytes))
                return "Unknown";

            return FormatBytes(bytes);
        }

        /// <summary>
        /// Get the download URL for this model from Hugging Face
        /// </summary>
        public string GetDownloadUrl()
        {
            string baseUrl = string.Format(
                Utilities.Constants.HUGGINGFACE_BASE_URL,
                GetSizeString()
            );

            return baseUrl + Utilities.Constants.WHISPER_MODEL_FILENAME;
        }

        /// <summary>
        /// Update the model path based on persistent data path
        /// </summary>
        private void UpdatePath()
        {
            string modelsFolder = Path.Combine(
                Application.persistentDataPath,
                Utilities.Constants.MODELS_FOLDER_NAME
            );

            ModelPath = Path.Combine(
                modelsFolder,
                FolderName,
                "model.onnx"
            );
        }

        /// <summary>
        /// Refresh the download status by checking if file exists
        /// </summary>
        public void RefreshDownloadStatus()
        {
            IsDownloaded = File.Exists(ModelPath);

            if (IsDownloaded && Utilities.Constants.MODEL_SIZES.TryGetValue(GetSizeString(), out var size))
            {
                SizeInBytes = size;
            }
        }

        /// <summary>
        /// Format bytes to human-readable format
        /// </summary>
        private static string FormatBytes(long bytes)
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

        /// <summary>
        /// Get the directory where this model should be stored
        /// </summary>
        public string GetModelDirectory()
        {
            return Path.GetDirectoryName(ModelPath);
        }

        public override string ToString()
        {
            return $"WhisperModel({GetSizeString()}) - {(IsDownloaded ? "Downloaded" : "Not Downloaded")} - {GetReadableSize()}";
        }
    }
}
