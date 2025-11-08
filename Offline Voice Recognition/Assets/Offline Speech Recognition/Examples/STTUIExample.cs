using UnityEngine;
using TMPro;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Examples
{
    /// <summary>
    /// Example that displays STT results in TextMeshPro UI
    /// Shows real-time transcription, status, and error messages
    /// </summary>
    public class STTUIExample : MonoBehaviour
    {
        [SerializeField] private STTEngine sttEngine;
        [SerializeField] private TextMeshProUGUI transcriptionText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI modelInfoText;

        private bool isRecording = false;
        private float recordingStartTime = 0f;

        private void Start()
        {
            if (sttEngine == null)
            {
                Debug.LogError("STTEngine not assigned!");
                return;
            }

            if (transcriptionText == null)
            {
                Debug.LogError("Transcription Text not assigned!");
                return;
            }

            // Subscribe to STT Engine events
            sttEngine.OnTranscriptionComplete += HandleTranscriptionComplete;
            sttEngine.OnTranscriptionStarted += HandleTranscriptionStarted;
            sttEngine.OnError += HandleError;
            sttEngine.OnDownloadProgress += HandleDownloadProgress;

            // Initialize UI
            UpdateStatus("Ready");
            UpdateModelInfo();

            Debug.Log("STT UI Example initialized");
        }

        private void Update()
        {
            HandleKeyboardInput();
            UpdateRecordingTime();
        }

        /// <summary>
        /// Handle keyboard input
        /// </summary>
        private void HandleKeyboardInput()
        {
            // R = Start recording
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!isRecording)
                {
                    StartRecording();
                }
            }

            // S = Stop and transcribe
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (isRecording)
                {
                    StopAndTranscribe();
                }
            }

            // C = Cancel recording
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (isRecording)
                {
                    CancelRecording();
                }
            }

            // L = Change language (cycle through)
            if (Input.GetKeyDown(KeyCode.L))
            {
                CycleLanguage();
            }

            // M = Change model (cycle through)
            if (Input.GetKeyDown(KeyCode.M))
            {
                CycleModel();
            }
        }

        /// <summary>
        /// Start microphone recording
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                UpdateStatus("Already recording");
                return;
            }

            sttEngine.StartMicrophoneCapture();
            isRecording = true;
            recordingStartTime = Time.time;

            UpdateStatus("🎤 Recording... (Press S to stop)");
            UpdateTranscriptionText("Listening...");

            Debug.Log("Recording started");
        }

        /// <summary>
        /// Stop recording and transcribe
        /// </summary>
        public void StopAndTranscribe()
        {
            if (!isRecording)
            {
                UpdateStatus("Not recording");
                return;
            }

            sttEngine.TranscribeFromMicrophone();
            isRecording = false;

            UpdateStatus("⏳ Processing audio...");
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

            UpdateStatus("Recording cancelled");
            UpdateTranscriptionText("");

            Debug.Log("Recording cancelled");
        }

        /// <summary>
        /// Transcribe audio file
        /// </summary>
        public void TranscribeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                UpdateStatus("File path not provided");
                return;
            }

            sttEngine.TranscribeFromFile(filePath);
            UpdateStatus("⏳ Processing file...");
        }

        /// <summary>
        /// Cycle through available languages
        /// </summary>
        private void CycleLanguage()
        {
            var currentLanguage = sttEngine.GetCurrentLanguage();
            var nextLanguage = (WhisperLanguage)(((int)currentLanguage + 1) % System.Enum.GetNames(typeof(WhisperLanguage)).Length);

            sttEngine.SetLanguage(nextLanguage);
            UpdateStatus($"Language changed to: {nextLanguage}");
            UpdateModelInfo();

            Debug.Log($"Language changed to: {nextLanguage}");
        }

        /// <summary>
        /// Cycle through available models
        /// </summary>
        private void CycleModel()
        {
            var currentModel = sttEngine.GetCurrentModel();
            var nextModel = (WhisperModel.ModelSize)(((int)currentModel + 1) % 5);

            if (sttEngine.IsModelDownloaded(nextModel))
            {
                sttEngine.SetModel(nextModel);
                UpdateStatus($"Model changed to: {nextModel}");
                UpdateModelInfo();
                Debug.Log($"Model changed to: {nextModel}");
            }
            else
            {
                UpdateStatus($"Model {nextModel} not downloaded");
            }
        }

        // ===== CALLBACKS =====

        private void HandleTranscriptionComplete(string text)
        {
            UpdateTranscriptionText(text);
            UpdateStatus("✅ Transcription complete!");

            Debug.Log($"Transcription: {text}");
        }

        private void HandleTranscriptionStarted()
        {
            UpdateStatus("⏳ Processing audio...");
            UpdateTranscriptionText("Processing...");
        }

        private void HandleError(string error)
        {
            UpdateStatus($"❌ Error: {error}");
            UpdateTranscriptionText("");

            Debug.LogError($"STT Error: {error}");
        }

        private void HandleDownloadProgress(float progress)
        {
            string progressBar = CreateProgressBar(progress);
            UpdateStatus($"Downloading model... {progressBar} {progress * 100:F0}%");
        }

        // ===== UI UPDATE METHODS =====

        private void UpdateTranscriptionText(string text)
        {
            if (transcriptionText != null)
            {
                transcriptionText.text = text;
            }
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        private void UpdateModelInfo()
        {
            if (modelInfoText != null)
            {
                var currentModel = sttEngine.GetCurrentModel();
                var currentLanguage = sttEngine.GetCurrentLanguage();
                var downloadedCount = sttEngine.GetDownloadedModels().Count;

                string info = $"Model: {currentModel}\nLanguage: {currentLanguage}\nDownloaded: {downloadedCount}/5\n\n" +
                             $"<size=70%>R: Record | S: Stop | L: Language | M: Model | C: Cancel</size>";

                modelInfoText.text = info;
            }
        }

        private void UpdateRecordingTime()
        {
            if (isRecording && statusText != null)
            {
                float recordingTime = Time.time - recordingStartTime;
                string timeText = $"🎤 Recording... {recordingTime:F1}s (Press S to stop)";
                statusText.text = timeText;
            }
        }

        /// <summary>
        /// Create a visual progress bar
        /// </summary>
        private string CreateProgressBar(float progress)
        {
            int barLength = 20;
            int filledLength = Mathf.RoundToInt(progress * barLength);
            string bar = new string('█', filledLength) + new string('░', barLength - filledLength);
            return $"[{bar}]";
        }

        private void OnDestroy()
        {
            if (sttEngine != null)
            {
                sttEngine.OnTranscriptionComplete -= HandleTranscriptionComplete;
                sttEngine.OnTranscriptionStarted -= HandleTranscriptionStarted;
                sttEngine.OnError -= HandleError;
                sttEngine.OnDownloadProgress -= HandleDownloadProgress;
            }
        }
    }
}
