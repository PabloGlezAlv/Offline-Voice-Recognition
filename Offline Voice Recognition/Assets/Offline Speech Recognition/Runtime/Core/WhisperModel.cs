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
        /// Available Whisper model sizes (GGML format from ggerganov/whisper.cpp)
        /// </summary>
        public enum ModelSize
        {
            Tiny,           // ~77.7 MB (ggml-tiny.bin)
            Small,          // ~488 MB (ggml-small.bin)
            Medium,         // ~1.53 GB (ggml-medium.bin)
            LargeV3,        // ~3.1 GB (ggml-large-v3.bin) - Recommended for high accuracy
            LargeV3Turbo    // ~1.62 GB (ggml-large-v3-turbo.bin) - Faster inference
        }

        /// <summary>
        /// The size/type of the model
        /// </summary>
        public ModelSize Size { get; private set; }

        /// <summary>
        /// The filename of the model (e.g., "ggml-tiny.bin")
        /// </summary>
        public string FileName { get; private set; }

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
            FileName = $"ggml-{GetSizeString()}.bin";
            UpdatePath();
            RefreshDownloadStatus();
        }

        /// <summary>
        /// Get the string representation of the model size (GGML format)
        /// </summary>
        public string GetSizeString()
        {
            return Size switch
            {
                ModelSize.Tiny => "tiny",
                ModelSize.Small => "small",
                ModelSize.Medium => "medium",
                ModelSize.LargeV3 => "large-v3",
                ModelSize.LargeV3Turbo => "large-v3-turbo",
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
            string sizeString = GetSizeString();
            string filename = string.Format(Utilities.Constants.WHISPER_MODEL_FILENAME, sizeString);
            return Utilities.Constants.HUGGINGFACE_BASE_URL + filename;
        }

        /// <summary>
        /// Get the expected SHA1 checksum for this model
        /// </summary>
        public string GetExpectedChecksum()
        {
            string sizeString = GetSizeString();
            if (Utilities.Constants.MODEL_CHECKSUMS.TryGetValue(sizeString, out var checksum))
            {
                return checksum;
            }
            return null;
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

            // Store model directly in Models folder with GGML filename
            ModelPath = Path.Combine(
                modelsFolder,
                FileName
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
