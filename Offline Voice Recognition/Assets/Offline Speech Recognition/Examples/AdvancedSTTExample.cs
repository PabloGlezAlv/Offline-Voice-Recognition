using System.Collections.Generic;
using UnityEngine;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Examples
{
    /// <summary>
    /// Advanced example with more features:
    /// - Voice command detection
    /// - Multiple language support
    /// - Model management
    /// - Advanced logging
    /// </summary>
    public class AdvancedSTTExample : MonoBehaviour
    {
        [SerializeField] private STTEngine sttEngine;
        [SerializeField] private float maxRecordingTime = 10f;

        private float recordingStartTime;
        private bool isRecording;
        private Dictionary<string, System.Action> voiceCommands;

        private void Start()
        {
            if (sttEngine == null)
            {
                Debug.LogError("STT Engine not assigned!");
                return;
            }

            // Subscribe to events
            sttEngine.OnTranscriptionComplete += ProcessVoiceCommand;
            sttEngine.OnError += HandleError;

            // Setup voice commands
            InitializeVoiceCommands();

            Debug.Log("Advanced STT Example initialized");
            LogModelStatus();
        }

        /// <summary>
        /// Initialize voice commands
        /// </summary>
        private void InitializeVoiceCommands()
        {
            voiceCommands = new Dictionary<string, System.Action>
            {
                { "hello", () => Debug.Log(">> Command: HELLO executed") },
                { "stop", () => { Debug.Log(">> Command: STOP executed"); isRecording = false; } },
                { "status", () => LogModelStatus() },
                { "download tiny", () => DownloadModel(WhisperModel.ModelSize.Tiny) },
                { "download base", () => DownloadModel(WhisperModel.ModelSize.Base) },
                { "download small", () => DownloadModel(WhisperModel.ModelSize.Small) },
            };
        }

        /// <summary>
        /// Start recording with auto-stop after max time
        /// </summary>
        public void StartSmartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("Already recording");
                return;
            }

            sttEngine.StartMicrophoneCapture();
            recordingStartTime = Time.time;
            isRecording = true;
            Debug.Log($"Smart recording started (max {maxRecordingTime}s)");
        }

        /// <summary>
        /// Process voice command from transcription
        /// </summary>
        private void ProcessVoiceCommand(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("Empty transcription");
                return;
            }

            Debug.Log($"Processing command: '{text}'");

            // Convert to lowercase for matching
            string lowerText = text.ToLower().Trim();

            // Check for exact matches
            if (voiceCommands.TryGetValue(lowerText, out var command))
            {
                command?.Invoke();
                return;
            }

            // Check for partial matches
            foreach (var kvp in voiceCommands)
            {
                if (lowerText.Contains(kvp.Key))
                {
                    Debug.Log($">> Partial match found: '{kvp.Key}' in '{lowerText}'");
                    kvp.Value?.Invoke();
                    return;
                }
            }

            Debug.LogWarning($"No command found for: '{text}'");
        }

        /// <summary>
        /// Log current model and language status
        /// </summary>
        private void LogModelStatus()
        {
            var allModels = sttEngine.GetAllModels();
            var downloadedModels = sttEngine.GetDownloadedModels();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n=== Model Status ===");
            sb.AppendLine($"Current Model: {sttEngine.GetCurrentModel()}");
            sb.AppendLine($"Current Language: {sttEngine.GetCurrentLanguage()}");
            sb.AppendLine($"\nAll Models ({allModels.Count}):");

            foreach (var model in allModels)
            {
                string status = model.IsDownloaded ? "✓ DOWNLOADED" : "✗ Not Downloaded";
                sb.AppendLine($"  {model.GetSizeString()}: {status} ({model.GetReadableSize()})");
            }

            sb.AppendLine($"\nDownloaded Models ({downloadedModels.Count}):");
            foreach (var model in downloadedModels)
            {
                sb.AppendLine($"  - {model.GetSizeString()} ({model.GetReadableSize()})");
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Download a model
        /// </summary>
        private void DownloadModel(WhisperModel.ModelSize size)
        {
            if (sttEngine.IsModelDownloaded(size))
            {
                Debug.Log($"Model {size} is already downloaded");
                return;
            }

            Debug.Log($"Starting download of {size} model...");
            sttEngine.DownloadModel(size);
        }

        /// <summary>
        /// Switch model
        /// </summary>
        public void SwitchModel(WhisperModel.ModelSize size)
        {
            if (!sttEngine.IsModelDownloaded(size))
            {
                Debug.LogError($"Model {size} is not downloaded. Download it first.");
                DownloadModel(size);
                return;
            }

            sttEngine.SetModel(size);
            Debug.Log($"Switched to {size} model");
        }

        /// <summary>
        /// Switch language
        /// </summary>
        public void SwitchLanguage(WhisperLanguage language)
        {
            sttEngine.SetLanguage(language);
            Debug.Log($"Language changed to: {language}");
        }

        /// <summary>
        /// Handle recording during update
        /// </summary>
        private void Update()
        {
            if (isRecording && Time.time - recordingStartTime > maxRecordingTime)
            {
                Debug.Log("Max recording time reached, stopping...");
                StopRecordingAndTranscribe();
            }

            // Keyboard shortcuts
            HandleKeyboardInput();
        }

        /// <summary>
        /// Handle keyboard input
        /// </summary>
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!isRecording)
                    StartSmartRecording();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (isRecording)
                    StopRecordingAndTranscribe();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                LogModelStatus();
            }
        }

        /// <summary>
        /// Stop recording and transcribe
        /// </summary>
        private void StopRecordingAndTranscribe()
        {
            if (!isRecording)
                return;

            sttEngine.TranscribeFromMicrophone();
            isRecording = false;
            float recordingDuration = Time.time - recordingStartTime;
            Debug.Log($"Recording stopped (duration: {recordingDuration:F2}s)");
        }

        /// <summary>
        /// Handle errors
        /// </summary>
        private void HandleError(string error)
        {
            Debug.LogError($"STT Error: {error}");
        }

        /// <summary>
        /// Get recording status
        /// </summary>
        public bool IsRecording => isRecording;

        /// <summary>
        /// Get list of available voice commands
        /// </summary>
        public string[] GetAvailableCommands()
        {
            var commands = new List<string>(voiceCommands.Keys);
            return commands.ToArray();
        }

        private void OnDestroy()
        {
            if (sttEngine != null)
            {
                sttEngine.OnTranscriptionComplete -= ProcessVoiceCommand;
                sttEngine.OnError -= HandleError;
            }
        }
    }
}
