using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OfflineSpeechRecognition.Core;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Examples
{
    /// <summary>
    /// Example that displays STT results in TextMeshPro UI
    /// Shows real-time transcription, status, and error messages
    /// Requires manual UI setup with Button, Dropdown, and TextMeshProUGUI components
    /// </summary>
    public class STTUIExample : MonoBehaviour
    {
        [SerializeField] private STTEngine sttEngine;
        [SerializeField] private TextMeshProUGUI transcriptionText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI modelInfoText;
        [SerializeField] private TextMeshProUGUI volumeIndicatorText;

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private float _currentVolume = 0f;

        // UI Components
        [SerializeField] private Button recordButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI recordButtonText;
        [SerializeField] private TMP_Dropdown microphoneDropdown;
        [SerializeField] private TMP_Dropdown modelDropdown;
        [SerializeField] private TMP_Dropdown languageDropdown;

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

            // Setup UI components
            SetupUIComponents();

            // Initialize UI
            UpdateStatus("Ready");
            UpdateModelInfo();

            Debug.Log("STT UI Example initialized");
        }

        private void Update()
        {
            UpdateRecordingTime();
            UpdateVolumeIndicator();
        }

        /// <summary>
        /// Setup UI component listeners and populate dropdowns
        /// </summary>
        private void SetupUIComponents()
        {
            // Setup record button
            if (recordButton != null)
            {
                recordButton.onClick.AddListener(OnRecordButtonClicked);
            }

            // Setup cancel button
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(CancelRecording);
                cancelButton.gameObject.SetActive(false);
            }

            // Setup microphone dropdown
            if (microphoneDropdown != null)
            {
                PopulateMicrophoneDropdown();
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneSelected);
            }

            // Setup model dropdown
            if (modelDropdown != null)
            {
                PopulateModelDropdown();
                modelDropdown.onValueChanged.AddListener(OnModelSelected);
            }

            // Setup language dropdown
            if (languageDropdown != null)
            {
                PopulateLanguageDropdown();
                languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
            }
        }

        private void PopulateMicrophoneDropdown()
        {
            string[] devices = STTEngine.GetAvailableMicrophones();
            microphoneDropdown.ClearOptions();
            microphoneDropdown.AddOptions(new System.Collections.Generic.List<string>(devices));

            // Set current microphone as default
            microphoneDropdown.RefreshShownValue();
        }

        private void PopulateModelDropdown()
        {
            var models = sttEngine.GetAllModels();
            System.Collections.Generic.List<string> modelNames = new System.Collections.Generic.List<string>();

            foreach (var model in models)
            {
                modelNames.Add($"{model.Size} ({model.GetReadableSize()})");
            }

            modelDropdown.ClearOptions();
            modelDropdown.AddOptions(modelNames);

            // Set current model as default
            var currentModel = sttEngine.GetCurrentModel();
            modelDropdown.value = (int)currentModel;
            modelDropdown.RefreshShownValue();
        }

        private void PopulateLanguageDropdown()
        {
            var languages = System.Enum.GetNames(typeof(WhisperLanguage));
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>(languages));

            // Set current language as default
            var currentLanguage = sttEngine.GetCurrentLanguage();
            languageDropdown.value = (int)currentLanguage;
            languageDropdown.RefreshShownValue();
        }

        private void OnRecordButtonClicked()
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopAndTranscribe();
            }
        }

        private void OnMicrophoneSelected(int index)
        {
            if (microphoneDropdown != null && index >= 0 && index < microphoneDropdown.options.Count)
            {
                string selectedMicrophone = microphoneDropdown.options[index].text;
                sttEngine.SetMicrophone(selectedMicrophone);
                UpdateStatus($"Microphone changed to: {selectedMicrophone}");
                Debug.Log($"Microphone changed to: {selectedMicrophone}");
            }
        }

        private void OnModelSelected(int index)
        {
            if (index >= 0 && index < 5)
            {
                var newModel = (WhisperModel.ModelSize)index;
                sttEngine.SetModel(newModel);
                UpdateStatus($"Model changed to: {newModel}");
                UpdateModelInfo();
                Debug.Log($"Model changed to: {newModel}");
            }
        }

        private void OnLanguageSelected(int index)
        {
            if (index >= 0)
            {
                var newLanguage = (WhisperLanguage)index;
                sttEngine.SetLanguage(newLanguage);
                UpdateStatus($"Language changed to: {newLanguage}");
                UpdateModelInfo();
                Debug.Log($"Language changed to: {newLanguage}");
            }
        }

        /// <summary>
        /// Start microphone recording
        /// </summary>
        public void StartRecording()
        {
            Debug.Log($"[StartRecording] Called - isRecording={isRecording}");

            if (isRecording)
            {
                Debug.LogWarning("[StartRecording] Already recording");
                UpdateStatus("Already recording");
                return;
            }

            Debug.Log("[StartRecording] Calling sttEngine.StartMicrophoneCapture()");
            sttEngine.StartMicrophoneCapture();
            isRecording = true;
            recordingStartTime = Time.time;

            // Update button text and show cancel button
            if (recordButtonText != null)
            {
                recordButtonText.text = "Stop Recording";
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }

            UpdateStatus("[REC] Recording...");
            UpdateTranscriptionText("Listening...");

            Debug.Log($"[StartRecording] Complete - isRecording={isRecording}, sttEngine.IsRecording={sttEngine.IsRecording}");
        }

        /// <summary>
        /// Stop recording and transcribe
        /// </summary>
        public void StopAndTranscribe()
        {
            Debug.Log($"[StopAndTranscribe] Called - isRecording={isRecording}");

            if (!isRecording)
            {
                Debug.LogWarning("[StopAndTranscribe] Not recording, returning");
                UpdateStatus("Not recording");
                return;
            }

            Debug.Log("[StopAndTranscribe] Calling sttEngine.TranscribeFromMicrophone()");
            sttEngine.TranscribeFromMicrophone();
            isRecording = false;

            // Reset button text and hide cancel button
            if (recordButtonText != null)
            {
                recordButtonText.text = "Start Recording";
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }

            UpdateStatus("[...] Processing audio...");
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

            // Reset button text and hide cancel button
            if (recordButtonText != null)
            {
                recordButtonText.text = "Start Recording";
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }

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


        // ===== CALLBACKS =====

        private void HandleTranscriptionComplete(string text)
        {
            UpdateTranscriptionText(text);
            UpdateStatus("[OK] Transcription complete!");

            Debug.Log($"Transcription: {text}");
        }

        private void HandleTranscriptionStarted()
        {
            UpdateStatus("[...] Processing audio...");
            UpdateTranscriptionText("Processing...");
        }

        private void HandleError(string error)
        {
            UpdateStatus($"[ERR] Error: {error}");
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

                string info = $"Model: {currentModel}\nLanguage: {currentLanguage}\nDownloaded Models: {downloadedCount}/5";

                modelInfoText.text = info;
            }
        }

        private void UpdateRecordingTime()
        {
            if (isRecording && statusText != null)
            {
                float recordingTime = Time.time - recordingStartTime;
                string timeText = $"[REC] Recording... {recordingTime:F1}s";
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
            string bar = new string('#', filledLength) + new string('-', barLength - filledLength);
            return $"[{bar}]";
        }

        /// <summary>
        /// Setup TextMeshPro component with text, alignment, and font size
        /// </summary>
        private void SetupTextMeshPro(TextMeshProUGUI textComponent, string text, TextAlignmentOptions alignment, int fontSize)
        {
            try
            {
                textComponent.text = text;
                textComponent.alignment = alignment;
                textComponent.fontSize = fontSize;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error setting up TextMeshPro: {ex.Message}");
            }
        }

        /// <summary>
        /// Update volume indicator from microphone input
        /// </summary>
        private void UpdateVolumeIndicator()
        {
            if (!isRecording || volumeIndicatorText == null)
                return;

            try
            {
                // Get recording clip from STT Engine's audio capture
                if (sttEngine != null && sttEngine.IsRecording)
                {
                    Debug.Log("[UpdateVolumeIndicator] Engine is recording, attempting to read volume");

                    // Get the recording position from all microphones
                    int position = Microphone.GetPosition(null);
                    Debug.Log($"[UpdateVolumeIndicator] Microphone position: {position}");

                    if (position > 0)
                    {
                        // Get samples from the recording
                        int sampleCount = Mathf.Min(position, 4410); // 0.25 seconds at 16kHz
                        float[] samples = new float[sampleCount];

                        // Create a temporary clip just to read from it
                        AudioClip tempClip = Microphone.Start(null, true, 1, 16000);
                        if (tempClip != null)
                        {
                            tempClip.GetData(samples, Mathf.Max(0, position - sampleCount));

                            // Calculate RMS (Root Mean Square) for volume
                            float sum = 0f;
                            foreach (float sample in samples)
                            {
                                sum += sample * sample;
                            }
                            _currentVolume = Mathf.Sqrt(sum / samples.Length);
                            Debug.Log($"[UpdateVolumeIndicator] Calculated volume: {_currentVolume * 100:F1}%");

                            volumeIndicatorText.text = $"Volume: {(_currentVolume * 100):F1}";

                            Microphone.End(null);
                        }
                        else
                        {
                            Debug.LogWarning("[UpdateVolumeIndicator] Failed to create temporary AudioClip");
                        }
                    }
                    else
                    {
                        Debug.Log("[UpdateVolumeIndicator] Microphone position is 0");
                    }
                }
                else
                {
                    Debug.Log($"[UpdateVolumeIndicator] Not recording - isRecording={isRecording}, sttEngine={sttEngine}, IsRecording={sttEngine?.IsRecording}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UpdateVolumeIndicator] Error: {ex.Message}\n{ex.StackTrace}");
            }
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
