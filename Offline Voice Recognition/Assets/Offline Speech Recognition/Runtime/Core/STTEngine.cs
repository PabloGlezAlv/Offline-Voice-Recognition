using System;
using System.Collections.Generic;
using UnityEngine;
using OfflineSpeechRecognition.Audio;
using OfflineSpeechRecognition.Download;
using OfflineSpeechRecognition.Inference;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Core
{
    /// <summary>
    /// Main STT Engine component - orchestrates all speech-to-text functionality
    /// Attach this to a GameObject to use the plugin
    /// </summary>
    [AddComponentMenu("Offline Speech Recognition/STT Engine")]
    public class STTEngine : MonoBehaviour
    {
        [SerializeField] private WhisperModel.ModelSize selectedModel = WhisperModel.ModelSize.Base;
        [SerializeField] private WhisperLanguage selectedLanguage = WhisperLanguage.English;
        [SerializeField] private bool autoDownloadModels = false;

        private WhisperModel _currentModel;
        private AudioCapture _audioCapture;
        private AudioLoader _audioLoader;
        private ModelDownloader _modelDownloader;
        private ModelManager _modelManager;
        private SentisWhisperRunner _whisperRunner;
        private AudioProcessor _audioProcessor;

        private bool _isInitialized;

        // Events
        public event Action<string> OnTranscriptionComplete;
        public event Action OnTranscriptionStarted;
        public event Action<float> OnDownloadProgress;
        public event Action<string> OnError;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the STT Engine
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Get or create model manager
                _modelManager = ModelManager.Instance;

                // Get the current model
                _currentModel = _modelManager.GetModel(selectedModel);

                // Create audio capture
                var captureObj = new GameObject("AudioCapture");
                captureObj.transform.SetParent(transform);
                _audioCapture = captureObj.AddComponent<AudioCapture>();
                _audioCapture.OnCaptureError += HandleCaptureError;

                // Create audio loader
                var loaderObj = new GameObject("AudioLoader");
                loaderObj.transform.SetParent(transform);
                _audioLoader = loaderObj.AddComponent<AudioLoader>();
                _audioLoader.OnLoadError += HandleLoadError;

                // Create model downloader
                var downloaderObj = new GameObject("ModelDownloader");
                downloaderObj.transform.SetParent(transform);
                _modelDownloader = downloaderObj.AddComponent<ModelDownloader>();
                _modelDownloader.OnDownloadProgress += OnDownloadProgress;
                _modelDownloader.OnDownloadError += OnError;

                // Create Sentis Whisper runner
                _whisperRunner = new SentisWhisperRunner();
                _whisperRunner.SetLanguage(selectedLanguage);

                // Create audio processor
                _audioProcessor = new AudioProcessor(_whisperRunner, selectedLanguage);
                _audioProcessor.Start();

                _isInitialized = true;
                Debug.Log($"STT Engine initialized - Model: {selectedModel}, Language: {selectedLanguage}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to initialize STT Engine: {ex.Message}");
                Debug.LogError($"STT Engine initialization failed: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Start microphone recording
        /// </summary>
        public void StartMicrophoneCapture()
        {
            if (!_isInitialized)
            {
                OnError?.Invoke("STT Engine not initialized");
                return;
            }

            if (_audioCapture.IsRecording)
            {
                OnError?.Invoke("Already recording");
                return;
            }

            if (!_audioCapture.StartRecording())
            {
                OnError?.Invoke("Failed to start microphone capture");
            }
        }

        /// <summary>
        /// Stop microphone recording and transcribe
        /// </summary>
        public void TranscribeFromMicrophone()
        {
            if (!_isInitialized)
            {
                OnError?.Invoke("STT Engine not initialized");
                return;
            }

            if (!_audioCapture.IsRecording)
            {
                OnError?.Invoke("Not currently recording");
                return;
            }

            OnTranscriptionStarted?.Invoke();

            float[] audioData = _audioCapture.StopRecording();
            if (audioData != null && audioData.Length > 0)
            {
                ProcessAudioData(audioData);
            }
            else
            {
                OnError?.Invoke("No audio data captured");
            }
        }

        /// <summary>
        /// Stop microphone recording without transcribing
        /// </summary>
        public void StopMicrophoneCapture()
        {
            if (_audioCapture != null && _audioCapture.IsRecording)
            {
                _audioCapture.CancelRecording();
            }
        }

        /// <summary>
        /// Transcribe audio from file
        /// </summary>
        public void TranscribeFromFile(string filePath)
        {
            if (!_isInitialized)
            {
                OnError?.Invoke("STT Engine not initialized");
                return;
            }

            if (!System.IO.File.Exists(filePath))
            {
                OnError?.Invoke($"File not found: {filePath}");
                return;
            }

            OnTranscriptionStarted?.Invoke();

            // Try to load as float array directly (for WAV files)
            float[] audioData = _audioLoader.LoadAudioFileToFloatArray(filePath);

            if (audioData != null && audioData.Length > 0)
            {
                ProcessAudioData(audioData);
            }
            else
            {
                // If direct loading fails, load with AudioLoader async
                _audioLoader.OnAudioLoaded += (clip) =>
                {
                    float[] samples = AudioUtils.AudioClipToFloatArray(clip);
                    if (samples != null)
                    {
                        ProcessAudioData(samples);
                    }
                    else
                    {
                        OnError?.Invoke("Failed to extract audio samples from clip");
                    }
                };

                _audioLoader.LoadAudioFromFile(filePath);
            }
        }

        /// <summary>
        /// Process audio data
        /// </summary>
        private void ProcessAudioData(float[] audioData)
        {
            if (!_isInitialized || _audioProcessor == null)
            {
                OnError?.Invoke("Audio processor not initialized");
                return;
            }

            // Check if model is downloaded
            if (!_currentModel.IsDownloaded)
            {
                OnError?.Invoke($"Model {selectedModel} not downloaded. Download it first.");
                return;
            }

            // Initialize whisper runner if not already done
            if (!_whisperRunner.IsInitialized)
            {
                bool initialized = _whisperRunner.Initialize(_currentModel.ModelPath, selectedLanguage);
                if (!initialized)
                {
                    OnError?.Invoke("Failed to initialize Whisper runner");
                    return;
                }
            }

            // Preprocess audio
            float[] processedAudio = PreprocessAudio(audioData);

            // Queue for processing
            _audioProcessor.ProcessAudio(processedAudio, HandleTranscriptionResult);
        }

        /// <summary>
        /// Preprocess audio before inference
        /// </summary>
        private float[] PreprocessAudio(float[] audioData)
        {
            if (audioData == null)
                return null;

            // Normalize
            audioData = AudioUtils.NormalizeAudio(audioData);

            // Trim silence
            audioData = AudioUtils.TrimSilence(audioData, 0.01f);

            // Apply gain if needed
            audioData = AudioUtils.ApplyGain(audioData, 1.0f);

            return audioData;
        }

        /// <summary>
        /// Handle transcription result from processor
        /// </summary>
        private void HandleTranscriptionResult(AudioProcessor.TranscriptionResult result)
        {
            if (result.success && !string.IsNullOrEmpty(result.text))
            {
                OnTranscriptionComplete?.Invoke(result.text);
                Debug.Log($"Transcription: {result.text} (Confidence: {result.confidence:F2}, Time: {result.processingTime:F0}ms)");
            }
            else
            {
                OnError?.Invoke("Transcription failed or returned empty result");
            }
        }

        /// <summary>
        /// Download a model
        /// </summary>
        public void DownloadModel(WhisperModel.ModelSize size)
        {
            if (!_isInitialized)
            {
                OnError?.Invoke("STT Engine not initialized");
                return;
            }

            var model = _modelManager.GetModel(size);
            if (model.IsDownloaded)
            {
                OnError?.Invoke($"Model {size} already downloaded");
                return;
            }

            _modelDownloader.StartDownload(model);
        }

        /// <summary>
        /// Set the model to use
        /// </summary>
        public void SetModel(WhisperModel.ModelSize size)
        {
            selectedModel = size;
            _currentModel = _modelManager.GetModel(size);

            // Reinitialize runner
            if (_whisperRunner.IsInitialized && _currentModel.IsDownloaded)
            {
                _whisperRunner.Initialize(_currentModel.ModelPath, selectedLanguage);
            }

            Debug.Log($"Model changed to: {size}");
        }

        /// <summary>
        /// Set the language
        /// </summary>
        public void SetLanguage(WhisperLanguage language)
        {
            selectedLanguage = language;

            if (_whisperRunner != null)
            {
                _whisperRunner.SetLanguage(language);
            }

            if (_audioProcessor != null)
            {
                _audioProcessor.SetLanguage(language);
            }

            Debug.Log($"Language changed to: {language}");
        }

        /// <summary>
        /// Check if a model is downloaded
        /// </summary>
        public bool IsModelDownloaded(WhisperModel.ModelSize size)
        {
            return _modelManager.IsModelDownloaded(size);
        }

        /// <summary>
        /// Get all models info
        /// </summary>
        public List<WhisperModel> GetAllModels()
        {
            if (_modelManager == null)
            {
                _modelManager = ModelManager.Instance;
            }
            return _modelManager?.GetAllModels() ?? new List<WhisperModel>();
        }

        /// <summary>
        /// Get downloaded models
        /// </summary>
        public List<WhisperModel> GetDownloadedModels()
        {
            if (_modelManager == null)
            {
                _modelManager = ModelManager.Instance;
            }
            return _modelManager?.GetDownloadedModels() ?? new List<WhisperModel>();
        }

        /// <summary>
        /// Get recording status
        /// </summary>
        public bool IsRecording => _audioCapture != null && _audioCapture.IsRecording;

        /// <summary>
        /// Get processing queue size
        /// </summary>
        public int GetQueueSize()
        {
            return _audioProcessor?.GetQueueSize() ?? 0;
        }

        /// <summary>
        /// Get current language
        /// </summary>
        public WhisperLanguage GetCurrentLanguage()
        {
            return selectedLanguage;
        }

        /// <summary>
        /// Get current model
        /// </summary>
        public WhisperModel.ModelSize GetCurrentModel()
        {
            return selectedModel;
        }

        /// <summary>
        /// Handle capture errors
        /// </summary>
        private void HandleCaptureError(string error)
        {
            OnError?.Invoke($"Capture Error: {error}");
        }

        /// <summary>
        /// Handle load errors
        /// </summary>
        private void HandleLoadError(string error)
        {
            OnError?.Invoke($"Load Error: {error}");
        }

        /// <summary>
        /// Get available microphones
        /// </summary>
        public static string[] GetAvailableMicrophones()
        {
            return AudioCapture.GetAvailableMicrophones();
        }

        /// <summary>
        /// Set microphone device
        /// </summary>
        public void SetMicrophone(string deviceName)
        {
            if (_audioCapture != null)
            {
                _audioCapture.SetMicrophone(deviceName);
            }
        }

        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== STT Engine Debug Info ===");
            sb.AppendLine($"Initialized: {_isInitialized}");
            sb.AppendLine($"Current Model: {selectedModel}");
            sb.AppendLine($"Current Language: {selectedLanguage}");
            sb.AppendLine($"Model Downloaded: {_currentModel?.IsDownloaded}");
            sb.AppendLine($"Recording: {IsRecording}");
            sb.AppendLine($"Queue Size: {GetQueueSize()}");
            if (_audioProcessor != null)
                sb.AppendLine($"Processor Status: {_audioProcessor.GetStatus()}");
            return sb.ToString();
        }

        private void OnDestroy()
        {
            if (_audioProcessor != null)
            {
                _audioProcessor.Cleanup();
            }

            if (_whisperRunner != null)
            {
                _whisperRunner.Cleanup();
            }

            if (_modelDownloader != null)
            {
                ModelDownloader.Cleanup();
            }
        }
    }
}
