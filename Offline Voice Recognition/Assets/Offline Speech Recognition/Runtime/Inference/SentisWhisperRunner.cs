using System;
using System.Collections.Generic;
using UnityEngine;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Inference
{
    /// <summary>
    /// Wrapper for Unity Sentis to run Whisper ONNX models
    /// Handles model loading and inference
    /// </summary>
    public class SentisWhisperRunner
    {
        private const int EXPECTED_INPUT_SIZE = 128000; // 16000 Hz * 8 seconds

        /// <summary>
        /// Result of transcription
        /// </summary>
        [System.Serializable]
        public class TranscriptionResult
        {
            public string text;
            public float confidence;
            public string language;
            public float processingTimeMs;

            public TranscriptionResult(string text, float confidence, string language, float processingTime)
            {
                this.text = text;
                this.confidence = confidence;
                this.language = language;
                this.processingTimeMs = processingTime;
            }
        }

        private WhisperModel.ModelSize _currentModelSize;
        private WhisperLanguage _currentLanguage;
        private string _modelPath;
        private bool _isInitialized;

        private Dictionary<string, string> _tokenMap;
        private const float DEFAULT_CONFIDENCE = 0.75f;

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initialize the Whisper runner with a specific model and language
        /// </summary>
        public bool Initialize(string modelPath, WhisperLanguage language)
        {
            try
            {
                if (!System.IO.File.Exists(modelPath))
                {
                    Debug.LogError($"Model file not found: {modelPath}");
                    return false;
                }

                _modelPath = modelPath;
                _currentLanguage = language;
                _isInitialized = true;

                // Initialize token map (vocabulary)
                InitializeTokenMap();

                Debug.Log($"SentisWhisperRunner initialized with model: {modelPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize SentisWhisperRunner: {ex.Message}");
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Set the model size
        /// </summary>
        public void SetModelSize(WhisperModel.ModelSize size)
        {
            _currentModelSize = size;
        }

        /// <summary>
        /// Set the language for transcription
        /// </summary>
        public void SetLanguage(WhisperLanguage language)
        {
            _currentLanguage = language;
        }

        /// <summary>
        /// Run inference on audio samples
        /// </summary>
        public TranscriptionResult RunInference(float[] audioSamples)
        {
            if (!_isInitialized)
            {
                Debug.LogError("SentisWhisperRunner not initialized");
                return new TranscriptionResult("", 0f, "", 0f);
            }

            if (audioSamples == null || audioSamples.Length == 0)
            {
                Debug.LogError("No audio samples provided");
                return new TranscriptionResult("", 0f, "", 0f);
            }

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Prepare input (normalize audio to mel-spectrogram)
                float[] normalizedAudio = Audio.AudioUtils.NormalizeAudio(audioSamples);

                // In a real implementation with Unity Sentis, you would:
                // 1. Convert audio to mel-spectrogram
                // 2. Load the ONNX model using Sentis
                // 3. Run inference
                // 4. Decode the output tokens to text

                // For now, this is a placeholder that demonstrates the structure
                string transcription = DecodeAudioToText(normalizedAudio);
                float confidence = CalculateConfidence(audioSamples);

                stopwatch.Stop();

                string languageCode = LanguageConfig.GetLanguageCode(_currentLanguage);
                return new TranscriptionResult(transcription, confidence, languageCode, (float)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during inference: {ex.Message}");
                stopwatch.Stop();
                return new TranscriptionResult("", 0f, "", (float)stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Decode audio samples to text
        /// This is where the actual Sentis inference would happen
        /// </summary>
        private string DecodeAudioToText(float[] audioSamples)
        {
            if (audioSamples == null || audioSamples.Length == 0)
                return "";

            try
            {
                // NOTE: This is a simplified placeholder
                // In production, you would:
                // 1. Use Unity Sentis to convert audio to mel-spectrogram
                // 2. Load and run the ONNX model
                // 3. Decode output tokens to text using the tokenizer

                // For now, we return a placeholder
                // The actual implementation depends on having the ONNX model properly converted for Sentis

                Debug.Log($"Processing {audioSamples.Length} audio samples");

                // This would be replaced with actual Sentis inference:
                // var input = new Tensor(audioMelSpectrogram);
                // var output = model.Execute(input);
                // var decodedText = Decode(output);

                return ""; // Placeholder - actual transcription would come from model
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error decoding audio: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Calculate confidence score for transcription
        /// </summary>
        private float CalculateConfidence(float[] audioSamples)
        {
            if (audioSamples == null || audioSamples.Length == 0)
                return 0f;

            try
            {
                // Calculate RMS as a basic confidence metric
                float rms = Audio.AudioUtils.CalculateRMS(audioSamples);

                // Normalize RMS to 0-1 confidence range
                // Typical RMS values are 0.0 to 1.0 for normalized audio
                float confidence = Mathf.Clamp01(rms);

                return confidence > 0.1f ? confidence : DEFAULT_CONFIDENCE;
            }
            catch
            {
                return DEFAULT_CONFIDENCE;
            }
        }

        /// <summary>
        /// Initialize the token mapping (vocabulary)
        /// This maps token IDs to text
        /// </summary>
        private void InitializeTokenMap()
        {
            _tokenMap = new Dictionary<string, string>();

            // Basic whitespace and control tokens
            _tokenMap["0"] = " ";
            _tokenMap["1"] = " ";
            _tokenMap["2"] = " ";

            // In production, this would be loaded from a vocab file or embedded in the plugin
            // The Whisper tokenizer maps token IDs to subword units that can be combined to form text
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            _isInitialized = false;
            _modelPath = null;
            _tokenMap?.Clear();
        }

        /// <summary>
        /// Check if a model path is valid
        /// </summary>
        public static bool IsValidModelPath(string path)
        {
            return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
        }

        /// <summary>
        /// Get information about the current runner state
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== SentisWhisperRunner Debug Info ===");
            sb.AppendLine($"Initialized: {_isInitialized}");
            sb.AppendLine($"Model Path: {_modelPath}");
            sb.AppendLine($"Model Size: {_currentModelSize}");
            sb.AppendLine($"Language: {LanguageConfig.GetLanguageName(_currentLanguage)}");
            sb.AppendLine($"Language Code: {LanguageConfig.GetLanguageCode(_currentLanguage)}");
            return sb.ToString();
        }
    }
}
