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
            if (isRecording)
            {
                UpdateStatus("Already recording");
                return;
            }

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

            // Get microphone audio position and samples
            if (Microphone.IsRecording(null))
            {
                int position = Microphone.GetPosition(null);

                // Create a temporary clip to sample from microphone
                AudioClip clip = Microphone.Start(null, true, 1, 16000);

                if (clip != null && position > 0)
                {
                    // Get samples from the recording
                    int sampleCount = Mathf.Min(position, 4410); // 0.25 seconds at 16kHz
                    float[] samples = new float[sampleCount];
                    clip.GetData(samples, Mathf.Max(0, position - sampleCount));

                    // Calculate RMS (Root Mean Square) for volume
                    float sum = 0f;
                    foreach (float sample in samples)
                    {
                        sum += sample * sample;
                    }
                    _currentVolume = Mathf.Sqrt(sum / samples.Length);

                    volumeIndicatorText.text = $"Volume: {(_currentVolume * 100):F1}";
                }

                Microphone.End(null);
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

#if UNITY_EDITOR
        [ContextMenu("Generate UI")]
        public void GenerateUI()
        {
            // 1. Find or Create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create Main Panel
            GameObject panelObj = new GameObject("STT_Panel");
            panelObj.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800, 600);
            
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Vertical Layout for Panel
            VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            // 3. Create Header
            CreateText(panelObj, "Offline Speech Recognition", 32, TextAlignmentOptions.Center, true);

            // 4. Create Status Text
            statusText = CreateText(panelObj, "Ready", 18, TextAlignmentOptions.Left);

            // 5. Create Volume Indicator Text
            volumeIndicatorText = CreateText(panelObj, "Volume: 0.0", 14, TextAlignmentOptions.Left);

            // 6. Create Model Info Text
            modelInfoText = CreateText(panelObj, "Model Info...", 14, TextAlignmentOptions.Left);

            // 7. Create Controls Container (Horizontal)
            GameObject controlsObj = new GameObject("Controls");
            controlsObj.transform.SetParent(panelObj.transform, false);
            RectTransform controlsRect = controlsObj.AddComponent<RectTransform>();
            controlsRect.sizeDelta = new Vector2(0, 40);
            HorizontalLayoutGroup controlsLayout = controlsObj.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 10;
            controlsLayout.childControlWidth = true;
            controlsLayout.childForceExpandWidth = true;

            // 8. Create Dropdowns
            microphoneDropdown = CreateDropdown(controlsObj, "Microphone");
            modelDropdown = CreateDropdown(controlsObj, "Model");
            languageDropdown = CreateDropdown(controlsObj, "Language");

            // 9. Create Buttons Container
            GameObject buttonsObj = new GameObject("Buttons");
            buttonsObj.transform.SetParent(panelObj.transform, false);
            RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(0, 50);
            HorizontalLayoutGroup buttonsLayout = buttonsObj.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;

            // 10. Create Record Button
            recordButton = CreateButton(buttonsObj, "Start Recording", out recordButtonText);

            // 11. Create Cancel Button
            cancelButton = CreateButton(buttonsObj, "Cancel", out TextMeshProUGUI cancelText);
            cancelButton.image.color = new Color(0.8f, 0.2f, 0.2f);
            cancelButton.gameObject.SetActive(false);

            // 12. Create Transcription Text Area
            GameObject scrollObj = new GameObject("Scroll View");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(0, 300); // Remaining height
            
            // Add Layout Element to fill remaining space if needed, but fixed height is fine for now
            LayoutElement scrollLayoutElement = scrollObj.AddComponent<LayoutElement>();
            scrollLayoutElement.minHeight = 200;
            scrollLayoutElement.flexibleHeight = 1;

            Image scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.color = new Color(0, 0, 0, 0.5f);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            transcriptionText = CreateText(content, "Transcription will appear here...", 18, TextAlignmentOptions.TopLeft);
            transcriptionText.rectTransform.anchorMin = Vector2.zero;
            transcriptionText.rectTransform.anchorMax = Vector2.one;
            transcriptionText.rectTransform.sizeDelta = Vector2.zero;
            transcriptionText.enableWordWrapping = true;

            ScrollRect scrollRectComp = scrollObj.AddComponent<ScrollRect>();
            scrollRectComp.content = contentRect;
            scrollRectComp.viewport = viewportRect;
            scrollRectComp.movementType = ScrollRect.MovementType.Elastic;

            // 13. Assign STTEngine if missing
            if (sttEngine == null)
            {
                sttEngine = FindObjectOfType<STTEngine>();
                if (sttEngine == null)
                {
                    // Create one if it doesn't exist
                    GameObject engineObj = new GameObject("STT_Engine");
                    sttEngine = engineObj.AddComponent<STTEngine>();
                    Debug.Log("Created new STTEngine GameObject");
                }
            }

            Debug.Log("UI Generated Successfully!");
            
            // Mark scene dirty to save changes
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private TextMeshProUGUI CreateText(GameObject parent, string content, int fontSize, TextAlignmentOptions alignment, bool bold = false)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            if (bold) text.fontStyle = FontStyles.Bold;
            return text;
        }

        private Button CreateButton(GameObject parent, string label, out TextMeshProUGUI buttonText)
        {
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(parent.transform, false);
            
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 1.0f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(160, 40);

            buttonText = CreateText(btnObj, label, 16, TextAlignmentOptions.Center);
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.sizeDelta = Vector2.zero;
            buttonText.color = Color.black;

            return btn;
        }

        private TMP_Dropdown CreateDropdown(GameObject parent, string name)
        {
            // Creating a functional TMP_Dropdown from scratch via code is complex because of the template structure.
            // We will create a simplified structure that mimics the default TMP Dropdown.
            
            GameObject root = new GameObject(name + " Dropdown");
            root.transform.SetParent(parent.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(200, 35);

            Image rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(1, 1, 1, 0.1f);

            TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rootImage;

            // Label
            TextMeshProUGUI label = CreateText(root, name, 14, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 0);
            label.rectTransform.offsetMax = new Vector2(-25, 0);
            dropdown.captionText = label;

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(root.transform, false);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            Image arrowImg = arrow.AddComponent<Image>();
            arrowImg.color = Color.white;

            // Template (The popup part)
            GameObject template = new GameObject("Template");
            template.transform.SetParent(root.transform, false);
            template.SetActive(false);
            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            Image templateImg = template.AddComponent<Image>();
            templateImg.color = new Color(0.1f, 0.1f, 0.1f);
            
            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.content = null; // Needs content
            scrollRect.viewport = null; // Needs viewport

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            Image viewportImg = viewport.AddComponent<Image>(); // Mask needs image
            viewport.AddComponent<Mask>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Item Template
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 25);
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            
            Toggle itemToggle = item.AddComponent<Toggle>();
            
            GameObject itemBackground = new GameObject("Item Background");
            itemBackground.transform.SetParent(item.transform, false);
            RectTransform itemBgRect = itemBackground.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            Image itemBgImg = itemBackground.AddComponent<Image>();
            itemBgImg.color = new Color(1,1,1,0); // Transparent
            itemToggle.targetGraphic = itemBgImg;

            GameObject itemCheck = new GameObject("Item Checkmark");
            itemCheck.transform.SetParent(item.transform, false);
            RectTransform itemCheckRect = itemCheck.AddComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckRect.sizeDelta = new Vector2(20, 20);
            itemCheckRect.anchoredPosition = new Vector2(10, 0);
            Image itemCheckImg = itemCheck.AddComponent<Image>();
            itemCheckImg.color = Color.green;
            itemToggle.graphic = itemCheckImg;

            TextMeshProUGUI itemLabel = CreateText(item, "Option A", 14, TextAlignmentOptions.Left);
            itemLabel.rectTransform.anchorMin = Vector2.zero;
            itemLabel.rectTransform.anchorMax = Vector2.one;
            itemLabel.rectTransform.offsetMin = new Vector2(25, 0);
            itemLabel.color = Color.white;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            return dropdown;
        }
#endif
    }
}
