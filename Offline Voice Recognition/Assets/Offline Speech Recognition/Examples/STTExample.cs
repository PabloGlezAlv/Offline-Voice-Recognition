using UnityEngine;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Examples
{
    /// <summary>
    /// Basic example showing how to use the STT Engine
    /// This example demonstrates:
    /// - Starting/stopping microphone recording
    /// - Transcribing microphone audio
    /// - Transcribing audio files
    /// - Handling callbacks
    /// </summary>
    public class STTExample : MonoBehaviour
    {
        [SerializeField] private STTEngine sttEngine;
        [SerializeField] private string audioFilePath = "";

        private bool isRecording = false;

        private void Start()
        {
            // Validate engine reference
            if (sttEngine == null)
            {
                Debug.LogError("STT Engine reference not assigned!");
                return;
            }

            // Subscribe to engine events
            sttEngine.OnTranscriptionComplete += OnTranscriptionComplete;
            sttEngine.OnTranscriptionStarted += OnTranscriptionStarted;
            sttEngine.OnError += OnError;
            sttEngine.OnDownloadProgress += OnDownloadProgress;

            Debug.Log("STT Example initialized");
            Debug.Log("Available Microphones: " + string.Join(", ", STTEngine.GetAvailableMicrophones()));
        }

        /// <summary>
        /// Start recording from microphone
        /// Call this from a UI button or keyboard input
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("Already recording");
                return;
            }

            sttEngine.StartMicrophoneCapture();
            isRecording = true;
            Debug.Log("Recording started... speak now");
        }

        /// <summary>
        /// Stop recording and transcribe
        /// </summary>
        public void StopAndTranscribe()
        {
            if (!isRecording)
            {
                Debug.LogWarning("Not currently recording");
                return;
            }

            sttEngine.TranscribeFromMicrophone();
            isRecording = false;
        }

        /// <summary>
        /// Cancel recording without transcribing
        /// </summary>
        public void CancelRecording()
        {
            if (!isRecording)
                return;

            sttEngine.StopMicrophoneCapture();
            isRecording = false;
            Debug.Log("Recording cancelled");
        }

        /// <summary>
        /// Transcribe an audio file
        /// </summary>
        public void TranscribeAudioFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("File path not provided");
                return;
            }

            sttEngine.TranscribeFromFile(filePath);
        }

        /// <summary>
        /// Download a specific model
        /// </summary>
        public void DownloadModel(WhisperModel.ModelSize size)
        {
            Debug.Log($"Starting download of {size} model...");
            sttEngine.DownloadModel(size);
        }

        /// <summary>
        /// Set the active model
        /// </summary>
        public void SetModel(WhisperModel.ModelSize size)
        {
            sttEngine.SetModel(size);
            Debug.Log($"Model changed to: {size}");
        }

        /// <summary>
        /// Set the language
        /// </summary>
        public void SetLanguage(WhisperLanguage language)
        {
            sttEngine.SetLanguage(language);
            Debug.Log($"Language changed to: {language}");
        }

        /// <summary>
        /// Get engine status
        /// </summary>
        public void PrintStatus()
        {
            Debug.Log(sttEngine.GetDebugInfo());
        }

        // ===== CALLBACKS =====

        private void OnTranscriptionStarted()
        {
            Debug.Log(">>> Transcription started (processing audio)");
        }

        private void OnTranscriptionComplete(string text)
        {
            Debug.Log($">>> Transcription Complete:\n{text}");

            // TODO: Use this result in your game
            // Examples:
            // - Display text on UI
            // - Process command if it's a voice command app
            // - Send to server
            // - Trigger game logic
        }

        private void OnError(string error)
        {
            Debug.LogError($"!!! Error: {error}");
        }

        private void OnDownloadProgress(float progress)
        {
            Debug.Log($"Download Progress: {progress * 100:F1}%");
        }

        // ===== KEYBOARD INPUT (for testing) =====

        private void Update()
        {
            // Press 'R' to start recording
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!isRecording)
                {
                    StartRecording();
                }
            }

            // Press 'S' to stop and transcribe
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (isRecording)
                {
                    StopAndTranscribe();
                }
            }

            // Press 'C' to cancel recording
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (isRecording)
                {
                    CancelRecording();
                }
            }

            // Press 'P' to print status
            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintStatus();
            }

            // Press 'D' to download base model (for testing)
            if (Input.GetKeyDown(KeyCode.D))
            {
                DownloadModel(WhisperModel.ModelSize.Base);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (sttEngine != null)
            {
                sttEngine.OnTranscriptionComplete -= OnTranscriptionComplete;
                sttEngine.OnTranscriptionStarted -= OnTranscriptionStarted;
                sttEngine.OnError -= OnError;
                sttEngine.OnDownloadProgress -= OnDownloadProgress;
            }
        }
    }
}
