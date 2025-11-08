using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Main Speech-to-Text Engine for offline voice recognition
    /// This is the primary API that users interact with
    /// </summary>
    [AddComponentMenu("Offline Invoice Recognition/STT Engine")]
    public class STTEngine : MonoBehaviour
    {
        [Header("Model Configuration")]
        [Tooltip("Select the Whisper model size to use")]
        [SerializeField] private ModelSize modelSize = ModelSize.Small;

        [Header("Audio Settings")]
        [Tooltip("Maximum recording length in seconds")]
        [SerializeField] private int maxRecordingLength = 30;

        [Tooltip("Microphone device name (leave empty for default)")]
        [SerializeField] private string microphoneDevice = "";

        [Header("Runtime Info")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isRecording = false;
        [SerializeField] private string currentModelName = "";

        // Events
        public event Action<STTResult> OnTranscriptionComplete;
        public event Action<float> OnProgress;
        public event Action<string> OnStatusChanged;
        public event Action<string> OnError;

        // Private members
        private WhisperInference whisperEngine;
        private ModelConfig currentConfig;
        private AudioClip recordingClip;
        private float lastTranscriptionTime;

        #region Initialization

        /// <summary>
        /// Initialize the STT Engine and load the selected model
        /// </summary>
        public async Task<bool> Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("STTEngine already initialized");
                return true;
            }

            try
            {
                OnStatusChanged?.Invoke("Initializing STT Engine...");

                // Get model configuration
                currentConfig = ModelConfig.GetConfig(modelSize);
                currentModelName = currentConfig.modelName;

                // Check if model is downloaded
                if (!currentConfig.IsDownloaded())
                {
                    string error = $"Model {currentConfig.modelName} not downloaded. Please download it first.";
                    Debug.LogError(error);
                    OnError?.Invoke(error);
                    OnStatusChanged?.Invoke("Error: Model not found");
                    return false;
                }

                OnStatusChanged?.Invoke($"Loading {currentConfig.modelName}...");

                // Initialize Whisper engine
                whisperEngine = new WhisperInference();
                bool success = await Task.Run(() => whisperEngine.Initialize(
                    currentConfig.GetLocalEncoderPath(),
                    currentConfig.GetLocalDecoderPath()
                ));

                if (!success)
                {
                    OnError?.Invoke("Failed to initialize Whisper engine");
                    OnStatusChanged?.Invoke("Initialization failed");
                    return false;
                }

                isInitialized = true;
                OnStatusChanged?.Invoke("Ready");
                Debug.Log($"STTEngine initialized with model: {currentConfig.modelName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Initialization error: {e.Message}");
                OnError?.Invoke(e.Message);
                OnStatusChanged?.Invoke("Error");
                return false;
            }
        }

        #endregion

        #region Transcription

        /// <summary>
        /// Transcribe an AudioClip to text
        /// </summary>
        public async Task<STTResult> Transcribe(AudioClip audioClip)
        {
            if (!isInitialized)
            {
                return STTResult.Failure("Engine not initialized. Call Initialize() first.");
            }

            if (audioClip == null)
            {
                return STTResult.Failure("AudioClip is null");
            }

            try
            {
                OnStatusChanged?.Invoke("Transcribing...");
                OnProgress?.Invoke(0f);

                float startTime = Time.realtimeSinceStartup;

                // Process audio
                OnProgress?.Invoke(0.2f);
                float[] processedAudio = await Task.Run(() => AudioProcessor.ProcessAudioClip(audioClip));

                if (processedAudio.Length == 0)
                {
                    return STTResult.Failure("Failed to process audio");
                }

                // Run inference
                OnProgress?.Invoke(0.4f);
                string transcription = await Task.Run(() => whisperEngine.Transcribe(processedAudio));

                OnProgress?.Invoke(1f);

                float processingTime = Time.realtimeSinceStartup - startTime;
                lastTranscriptionTime = processingTime;

                var result = STTResult.Success(transcription, "auto", 1f, processingTime);

                OnTranscriptionComplete?.Invoke(result);
                OnStatusChanged?.Invoke("Complete");

                Debug.Log($"Transcription: {transcription} (took {processingTime:F2}s)");

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Transcription error: {e.Message}");
                OnError?.Invoke(e.Message);
                OnStatusChanged?.Invoke("Error");
                return STTResult.Failure(e.Message);
            }
        }

        /// <summary>
        /// Transcribe audio from a file path
        /// </summary>
        public async Task<STTResult> TranscribeFromFile(string audioFilePath)
        {
            // Note: Unity's AudioClip loading from file requires special handling
            // This is a placeholder - implement based on your file format needs
            OnError?.Invoke("TranscribeFromFile not implemented yet. Use Transcribe(AudioClip) instead.");
            return STTResult.Failure("Not implemented");
        }

        #endregion

        #region Microphone Recording

        /// <summary>
        /// Start recording from microphone
        /// </summary>
        public void StartRecording()
        {
            if (!isInitialized)
            {
                OnError?.Invoke("Engine not initialized");
                return;
            }

            if (isRecording)
            {
                Debug.LogWarning("Already recording");
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                OnError?.Invoke("No microphone detected");
                return;
            }

            string device = string.IsNullOrEmpty(microphoneDevice) ? null : microphoneDevice;
            recordingClip = AudioProcessor.StartRecording(maxRecordingLength, device);

            if (recordingClip != null)
            {
                isRecording = true;
                OnStatusChanged?.Invoke("Recording...");
                Debug.Log("Recording started");
            }
            else
            {
                OnError?.Invoke("Failed to start recording");
            }
        }

        /// <summary>
        /// Stop recording and transcribe
        /// </summary>
        public async Task<STTResult> StopRecordingAndTranscribe()
        {
            if (!isRecording)
            {
                return STTResult.Failure("Not currently recording");
            }

            string device = string.IsNullOrEmpty(microphoneDevice) ? null : microphoneDevice;
            AudioClip trimmedClip = AudioProcessor.StopRecording(recordingClip, device);

            isRecording = false;
            OnStatusChanged?.Invoke("Processing recording...");

            if (trimmedClip == null)
            {
                return STTResult.Failure("Failed to process recording");
            }

            return await Transcribe(trimmedClip);
        }

        /// <summary>
        /// Cancel current recording without transcribing
        /// </summary>
        public void CancelRecording()
        {
            if (isRecording)
            {
                string device = string.IsNullOrEmpty(microphoneDevice) ? null : microphoneDevice;
                Microphone.End(device);
                isRecording = false;
                OnStatusChanged?.Invoke("Recording cancelled");
                Debug.Log("Recording cancelled");
            }
        }

        #endregion

        #region Model Management

        /// <summary>
        /// Check if current model is downloaded
        /// </summary>
        public bool IsModelDownloaded()
        {
            var config = ModelConfig.GetConfig(modelSize);
            return config.IsDownloaded();
        }

        /// <summary>
        /// Get current model info
        /// </summary>
        public string GetModelInfo()
        {
            var config = ModelConfig.GetConfig(modelSize);
            return $"{config.modelName} ({new ModelDownloader(config).GetEstimatedSize()})";
        }

        /// <summary>
        /// Download the selected model
        /// </summary>
        public async Task<bool> DownloadModel(Action<float> progressCallback = null)
        {
            var config = ModelConfig.GetConfig(modelSize);
            var downloader = new ModelDownloader(config);

            // Subscribe to events
            if (progressCallback != null)
                downloader.OnProgress += progressCallback;

            downloader.OnStatusChanged += (status) => OnStatusChanged?.Invoke(status);

            bool success = await downloader.DownloadModelAsync();

            if (success)
            {
                Debug.Log($"Model {config.modelName} downloaded successfully");
            }

            return success;
        }

        /// <summary>
        /// Get available microphone devices
        /// </summary>
        public static string[] GetMicrophoneDevices()
        {
            return Microphone.devices;
        }

        #endregion

        #region Properties

        public bool IsInitialized => isInitialized;
        public bool IsRecording => isRecording;
        public ModelSize CurrentModelSize => modelSize;
        public string CurrentModelName => currentModelName;
        public float LastTranscriptionTime => lastTranscriptionTime;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (isRecording)
            {
                CancelRecording();
            }

            whisperEngine?.Dispose();
        }

        private void OnApplicationQuit()
        {
            whisperEngine?.Dispose();
        }

        #endregion

        #region Debug Helpers

        [ContextMenu("Print Microphone Devices")]
        private void PrintMicrophoneDevices()
        {
            var devices = GetMicrophoneDevices();
            Debug.Log($"Available microphones ({devices.Length}):");
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"  [{i}] {devices[i]}");
            }
        }

        [ContextMenu("Print Model Info")]
        private void PrintModelInfo()
        {
            Debug.Log($"Model: {GetModelInfo()}");
            Debug.Log($"Downloaded: {IsModelDownloaded()}");
            Debug.Log($"Initialized: {isInitialized}");
        }

        #endregion
    }
}
