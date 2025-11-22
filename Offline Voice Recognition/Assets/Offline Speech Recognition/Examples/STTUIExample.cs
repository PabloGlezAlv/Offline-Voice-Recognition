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
    /// Can auto-generate UI structure from context menu
    /// </summary>
    public class STTUIExample : MonoBehaviour
    {
        [SerializeField] private STTEngine sttEngine;
        [SerializeField] private TextMeshProUGUI transcriptionText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI modelInfoText;

        private bool isRecording = false;
        private float recordingStartTime = 0f;

        // UI Components
        private Button recordButton;
        private Button cancelButton;
        private TMP_Dropdown microphoneDropdown;
        private TMP_Dropdown modelDropdown;
        private TMP_Dropdown languageDropdown;

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

        /// <summary>
        /// Generate UI structure automatically via context menu
        /// </summary>
        [ContextMenu("Generate UI")]
        private void GenerateUI()
        {
            Debug.Log("Generating UI structure...");

            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("STTCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create or find main panel
            Transform panelTransform = canvas.transform.Find("STTPanel");
            GameObject panelObj;
            if (panelTransform == null)
            {
                panelObj = new GameObject("STTPanel");
                panelObj.transform.SetParent(canvas.transform, false);
                RectTransform panelRect = panelObj.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
            else
            {
                panelObj = panelTransform.gameObject;
            }

            // Clear existing children to regenerate
            foreach (Transform child in panelObj.transform)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create vertical layout group
            VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            }
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            // 1. Recording Control Section
            CreateRecordingSection(panelObj);

            // 2. Microphone Selection
            CreateMicrophoneDropdown(panelObj);

            // 3. Model Selection
            CreateModelDropdown(panelObj);

            // 4. Language Selection
            CreateLanguageDropdown(panelObj);

            // 5. Transcription Text Area
            CreateTranscriptionArea(panelObj);

            // 6. Status Text Area
            CreateStatusArea(panelObj);

            // 7. Model Info Area
            CreateModelInfoArea(panelObj);

            // Auto-assign STTEngine if not assigned
            if (sttEngine == null)
            {
                sttEngine = FindObjectOfType<STTEngine>();
                if (sttEngine != null)
                {
                    Debug.Log("STTEngine auto-assigned");
                }
            }

            Debug.Log("UI generation complete!");
        }

        private void CreateRecordingSection(GameObject parent)
        {
            GameObject sectionObj = new GameObject("RecordingSection");
            sectionObj.transform.SetParent(parent.transform, false);
            RectTransform sectionRect = sectionObj.AddComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(0, 80);

            HorizontalLayoutGroup hlg = sectionObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 10f;

            // Record button
            GameObject recordBtnObj = new GameObject("RecordButton");
            recordBtnObj.transform.SetParent(sectionObj.transform, false);
            RectTransform recordBtnRect = recordBtnObj.AddComponent<RectTransform>();
            recordBtnRect.sizeDelta = new Vector2(150, 60);

            Image recordBtnImage = recordBtnObj.AddComponent<Image>();
            recordBtnImage.color = new Color(0.2f, 0.7f, 0.2f, 1f);

            recordButton = recordBtnObj.AddComponent<Button>();
            recordButton.targetGraphic = recordBtnImage;

            ColorBlock colors = recordButton.colors;
            colors.normalColor = new Color(0.2f, 0.7f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.6f, 0.15f, 1f);
            recordButton.colors = colors;

            TextMeshProUGUI recordBtnText = recordBtnObj.AddComponent<TextMeshProUGUI>();
            recordBtnText.text = "Start Recording";
            recordBtnText.alignment = TextAlignmentOptions.Center;
            recordBtnText.fontSize = 28;

            // Cancel button
            GameObject cancelBtnObj = new GameObject("CancelButton");
            cancelBtnObj.transform.SetParent(sectionObj.transform, false);
            RectTransform cancelBtnRect = cancelBtnObj.AddComponent<RectTransform>();
            cancelBtnRect.sizeDelta = new Vector2(150, 60);

            Image cancelBtnImage = cancelBtnObj.AddComponent<Image>();
            cancelBtnImage.color = new Color(0.7f, 0.2f, 0.2f, 1f);

            cancelButton = cancelBtnObj.AddComponent<Button>();
            cancelButton.targetGraphic = cancelBtnImage;

            ColorBlock cancelColors = cancelButton.colors;
            cancelColors.normalColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            cancelColors.highlightedColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            cancelColors.pressedColor = new Color(0.6f, 0.15f, 0.15f, 1f);
            cancelButton.colors = cancelColors;

            TextMeshProUGUI cancelBtnText = cancelBtnObj.AddComponent<TextMeshProUGUI>();
            cancelBtnText.text = "Cancel";
            cancelBtnText.alignment = TextAlignmentOptions.Center;
            cancelBtnText.fontSize = 28;
        }

        private void CreateMicrophoneDropdown(GameObject parent)
        {
            GameObject containerObj = new GameObject("MicrophoneContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 60);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(containerObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            labelRect.anchoredPosition = new Vector2(-200, 15);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Microphone:";
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Left;

            // Dropdown
            GameObject dropdownObj = new GameObject("MicrophoneDropdown");
            dropdownObj.transform.SetParent(containerObj.transform, false);
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0, 40);

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            microphoneDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Create template for dropdown
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.offsetMin = new Vector2(0, 0);
            templateRect.offsetMax = new Vector2(0, 150);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(templateObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 200);

            VerticalLayoutGroup contentVLG = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVLG.childForceExpandWidth = true;
            contentVLG.spacing = 0;

            scrollRect.content = contentRect;

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 30);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            ToggleGroup tg = contentObj.AddComponent<ToggleGroup>();
            itemToggle.group = tg;

            Image itemImage = itemObj.AddComponent<Image>();
            itemImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            GameObject labelItemObj = new GameObject("Label");
            labelItemObj.transform.SetParent(itemObj.transform, false);
            RectTransform labelItemRect = labelItemObj.AddComponent<RectTransform>();
            labelItemRect.anchorMin = Vector2.zero;
            labelItemRect.anchorMax = Vector2.one;
            labelItemRect.offsetMin = new Vector2(20, 0);
            labelItemRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelItemText = labelItemObj.AddComponent<TextMeshProUGUI>();
            labelItemText.text = "Option A";
            labelItemText.fontSize = 18;
            labelItemText.alignment = TextAlignmentOptions.Left;

            microphoneDropdown.template = templateRect;

            // Create caption text
            GameObject captionObj = new GameObject("Label");
            captionObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform captionRect = captionObj.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 0);
            captionRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
            captionText.text = "Select Microphone";
            captionText.fontSize = 20;
            captionText.alignment = TextAlignmentOptions.Left;

            microphoneDropdown.captionText = captionText;
        }

        private void CreateModelDropdown(GameObject parent)
        {
            GameObject containerObj = new GameObject("ModelContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 60);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(containerObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            labelRect.anchoredPosition = new Vector2(-200, 15);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Model:";
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Left;

            // Dropdown
            GameObject dropdownObj = new GameObject("ModelDropdown");
            dropdownObj.transform.SetParent(containerObj.transform, false);
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0, 40);

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            modelDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Create template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.offsetMin = new Vector2(0, 0);
            templateRect.offsetMax = new Vector2(0, 150);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(templateObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 200);

            VerticalLayoutGroup contentVLG = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVLG.childForceExpandWidth = true;
            contentVLG.spacing = 0;

            scrollRect.content = contentRect;

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 30);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            ToggleGroup tg = contentObj.AddComponent<ToggleGroup>();
            itemToggle.group = tg;

            Image itemImage = itemObj.AddComponent<Image>();
            itemImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            GameObject labelItemObj = new GameObject("Label");
            labelItemObj.transform.SetParent(itemObj.transform, false);
            RectTransform labelItemRect = labelItemObj.AddComponent<RectTransform>();
            labelItemRect.anchorMin = Vector2.zero;
            labelItemRect.anchorMax = Vector2.one;
            labelItemRect.offsetMin = new Vector2(20, 0);
            labelItemRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelItemText = labelItemObj.AddComponent<TextMeshProUGUI>();
            labelItemText.text = "Option A";
            labelItemText.fontSize = 18;
            labelItemText.alignment = TextAlignmentOptions.Left;

            modelDropdown.template = templateRect;

            // Caption
            GameObject captionObj = new GameObject("Label");
            captionObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform captionRect = captionObj.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 0);
            captionRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
            captionText.text = "Select Model";
            captionText.fontSize = 20;
            captionText.alignment = TextAlignmentOptions.Left;

            modelDropdown.captionText = captionText;
        }

        private void CreateLanguageDropdown(GameObject parent)
        {
            GameObject containerObj = new GameObject("LanguageContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 60);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(containerObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            labelRect.anchoredPosition = new Vector2(-200, 15);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Language:";
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Left;

            // Dropdown
            GameObject dropdownObj = new GameObject("LanguageDropdown");
            dropdownObj.transform.SetParent(containerObj.transform, false);
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0, 40);

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            languageDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Create template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.offsetMin = new Vector2(0, 0);
            templateRect.offsetMax = new Vector2(0, 150);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(templateObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 200);

            VerticalLayoutGroup contentVLG = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVLG.childForceExpandWidth = true;
            contentVLG.spacing = 0;

            scrollRect.content = contentRect;

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 30);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            ToggleGroup tg = contentObj.AddComponent<ToggleGroup>();
            itemToggle.group = tg;

            Image itemImage = itemObj.AddComponent<Image>();
            itemImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            GameObject labelItemObj = new GameObject("Label");
            labelItemObj.transform.SetParent(itemObj.transform, false);
            RectTransform labelItemRect = labelItemObj.AddComponent<RectTransform>();
            labelItemRect.anchorMin = Vector2.zero;
            labelItemRect.anchorMax = Vector2.one;
            labelItemRect.offsetMin = new Vector2(20, 0);
            labelItemRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelItemText = labelItemObj.AddComponent<TextMeshProUGUI>();
            labelItemText.text = "Option A";
            labelItemText.fontSize = 18;
            labelItemText.alignment = TextAlignmentOptions.Left;

            languageDropdown.template = templateRect;

            // Caption
            GameObject captionObj = new GameObject("Label");
            captionObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform captionRect = captionObj.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 0);
            captionRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
            captionText.text = "Select Language";
            captionText.fontSize = 20;
            captionText.alignment = TextAlignmentOptions.Left;

            languageDropdown.captionText = captionText;
        }

        private void CreateTranscriptionArea(GameObject parent)
        {
            GameObject containerObj = new GameObject("TranscriptionContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 150);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(containerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 25);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Transcription:";
            titleText.fontSize = 20;
            titleText.alignment = TextAlignmentOptions.Left;

            // Text area
            GameObject textObj = new GameObject("TranscriptionText");
            textObj.transform.SetParent(containerObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, 120);

            Image textImage = textObj.AddComponent<Image>();
            textImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            transcriptionText = textObj.AddComponent<TextMeshProUGUI>();
            transcriptionText.text = "Transcription will appear here...";
            transcriptionText.fontSize = 18;
            transcriptionText.alignment = TextAlignmentOptions.TopLeft;

            LayoutElement textLayoutElement = textObj.AddComponent<LayoutElement>();
            textLayoutElement.preferredHeight = 120;
        }

        private void CreateStatusArea(GameObject parent)
        {
            GameObject containerObj = new GameObject("StatusContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 60);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(containerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 25);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Status:";
            titleText.fontSize = 20;
            titleText.alignment = TextAlignmentOptions.Left;

            // Text area
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(containerObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, 35);

            Image textImage = textObj.AddComponent<Image>();
            textImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "Ready";
            statusText.fontSize = 18;
            statusText.alignment = TextAlignmentOptions.Left;

            LayoutElement textLayoutElement = textObj.AddComponent<LayoutElement>();
            textLayoutElement.preferredHeight = 35;
        }

        private void CreateModelInfoArea(GameObject parent)
        {
            GameObject containerObj = new GameObject("ModelInfoContainer");
            containerObj.transform.SetParent(parent.transform, false);
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 80);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(containerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 25);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Model Info:";
            titleText.fontSize = 20;
            titleText.alignment = TextAlignmentOptions.Left;

            // Text area
            GameObject textObj = new GameObject("ModelInfoText");
            textObj.transform.SetParent(containerObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, 55);

            Image textImage = textObj.AddComponent<Image>();
            textImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            modelInfoText = textObj.AddComponent<TextMeshProUGUI>();
            modelInfoText.text = "Model info will appear here...";
            modelInfoText.fontSize = 16;
            modelInfoText.alignment = TextAlignmentOptions.TopLeft;

            LayoutElement textLayoutElement = textObj.AddComponent<LayoutElement>();
            textLayoutElement.preferredHeight = 55;
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
            if (recordButton != null)
            {
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop Recording";
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }

            UpdateStatus("🎤 Recording...");
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
            if (recordButton != null)
            {
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Recording";
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }

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

            // Reset button text and hide cancel button
            if (recordButton != null)
            {
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Recording";
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

                string info = $"Model: {currentModel}\nLanguage: {currentLanguage}\nDownloaded Models: {downloadedCount}/5";

                modelInfoText.text = info;
            }
        }

        private void UpdateRecordingTime()
        {
            if (isRecording && statusText != null)
            {
                float recordingTime = Time.time - recordingStartTime;
                string timeText = $"🎤 Recording... {recordingTime:F1}s";
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
