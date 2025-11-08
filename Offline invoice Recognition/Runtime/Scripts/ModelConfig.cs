using System;
using UnityEngine;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Available Whisper model sizes
    /// </summary>
    public enum ModelSize
    {
        Tiny,       // ~75 MB - Fast, basic accuracy (mobile friendly)
        Small,      // ~150 MB - Good balance (recommended)
        Medium,     // ~1.5 GB - High accuracy
        Large       // ~3 GB - Best accuracy
    }

    /// <summary>
    /// Model configuration and URLs for downloading from HuggingFace
    /// </summary>
    [Serializable]
    public class ModelConfig
    {
        public ModelSize size;
        public string modelName;
        public string encoderUrl;
        public string decoderUrl;
        public long estimatedSize;

        private static readonly ModelConfig[] configs = new ModelConfig[]
        {
            new ModelConfig
            {
                size = ModelSize.Tiny,
                modelName = "whisper-tiny",
                encoderUrl = "https://huggingface.co/onnx-community/whisper-tiny/resolve/main/encoder_model.onnx",
                decoderUrl = "https://huggingface.co/onnx-community/whisper-tiny/resolve/main/decoder_model_merged.onnx",
                estimatedSize = 75 * 1024 * 1024 // 75 MB
            },
            new ModelConfig
            {
                size = ModelSize.Small,
                modelName = "whisper-small",
                encoderUrl = "https://huggingface.co/onnx-community/whisper-small/resolve/main/encoder_model.onnx",
                decoderUrl = "https://huggingface.co/onnx-community/whisper-small/resolve/main/decoder_model_merged.onnx",
                estimatedSize = 150 * 1024 * 1024 // 150 MB
            },
            new ModelConfig
            {
                size = ModelSize.Medium,
                modelName = "whisper-medium",
                encoderUrl = "https://huggingface.co/onnx-community/whisper-medium/resolve/main/encoder_model.onnx",
                decoderUrl = "https://huggingface.co/onnx-community/whisper-medium/resolve/main/decoder_model_merged.onnx",
                estimatedSize = 1536 * 1024 * 1024 // 1.5 GB
            },
            new ModelConfig
            {
                size = ModelSize.Large,
                modelName = "whisper-large-v3",
                encoderUrl = "https://huggingface.co/onnx-community/whisper-large-v3/resolve/main/encoder_model.onnx",
                decoderUrl = "https://huggingface.co/onnx-community/whisper-large-v3/resolve/main/decoder_model_merged.onnx",
                estimatedSize = 3072 * 1024 * 1024 // 3 GB
            }
        };

        /// <summary>
        /// Get configuration for a specific model size
        /// </summary>
        public static ModelConfig GetConfig(ModelSize size)
        {
            foreach (var config in configs)
            {
                if (config.size == size)
                    return config;
            }
            return configs[0]; // Default to Tiny
        }

        /// <summary>
        /// Get local path where model should be stored
        /// </summary>
        public string GetLocalEncoderPath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "WhisperModels", modelName, "encoder_model.onnx");
        }

        public string GetLocalDecoderPath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "WhisperModels", modelName, "decoder_model_merged.onnx");
        }

        /// <summary>
        /// Check if model is already downloaded
        /// </summary>
        public bool IsDownloaded()
        {
            return System.IO.File.Exists(GetLocalEncoderPath()) &&
                   System.IO.File.Exists(GetLocalDecoderPath());
        }
    }
}
