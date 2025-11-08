using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OfflineInvoiceRecognition;
using System.Threading.Tasks;

/// <summary>
/// Demo script showing how to use the STTEngine
/// Demonstrates both microphone recording and AudioClip transcription
/// </summary>
public class STTDemoScript : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the STTEngine component")]
    public STTEngine sttEngine;

    [Header("UI References (Optional - for visual demo)")]
    public Button initializeButton;
    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Button transcribeClipButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI transcriptionText;
    public Slider progressBar;

    [Header("Test Audio (Optional)")]
    [Tooltip("Optional AudioClip to test transcription without microphone")]
    public AudioClip testAudioClip;

    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        // Setup UI callbacks if UI elements are assigned
        SetupUI();

        // Subscribe to STTEngine events
        if (sttEngine != null)
        {
            sttEngine.OnTranscriptionComplete += OnTranscriptionComplete;
            sttEngine.OnProgress += OnProgress;
            sttEngine.OnStatusChanged += OnStatusChanged;
            sttEngine.OnError += OnError;
        }
        else
        {
            Debug.LogError("STTEngine reference is not assigned!");
        }

        UpdateStatus("Ready - Click Initialize to start");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (sttEngine != null)
        {
            sttEngine.OnTranscriptionComplete -= OnTranscriptionComplete;
            sttEngine.OnProgress -= OnProgress;
            sttEngine.OnStatusChanged -= OnStatusChanged;
            sttEngine.OnError -= OnError;
        }
    }

    #endregion

    #region UI Setup

    private void SetupUI()
    {
        if (initializeButton != null)
        {
            initializeButton.onClick.AddListener(() => InitializeEngine());
        }

        if (startRecordingButton != null)
        {
            startRecordingButton.onClick.AddListener(() => StartRecording());
            startRecordingButton.interactable = false;
        }

        if (stopRecordingButton != null)
        {
            stopRecordingButton.onClick.AddListener(() => StopRecordingAndTranscribe());
            stopRecordingButton.interactable = false;
        }

        if (transcribeClipButton != null)
        {
            transcribeClipButton.onClick.AddListener(() => TranscribeTestClip());
            transcribeClipButton.interactable = false;
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
    }

    private void UpdateUIState()
    {
        bool initialized = sttEngine != null && sttEngine.IsInitialized;
        bool recording = sttEngine != null && sttEngine.IsRecording;

        if (initializeButton != null)
            initializeButton.interactable = !initialized;

        if (startRecordingButton != null)
            startRecordingButton.interactable = initialized && !recording;

        if (stopRecordingButton != null)
            stopRecordingButton.interactable = initialized && recording;

        if (transcribeClipButton != null)
            transcribeClipButton.interactable = initialized && !recording && testAudioClip != null;
    }

    #endregion

    #region STT Engine Methods

    /// <summary>
    /// Initialize the STT Engine
    /// </summary>
    public async void InitializeEngine()
    {
        if (sttEngine == null)
        {
            Debug.LogError("STTEngine is not assigned!");
            return;
        }

        UpdateStatus("Initializing...");

        bool success = await sttEngine.Initialize();

        if (success)
        {
            isInitialized = true;
            UpdateStatus("Initialized successfully! Ready to transcribe.");
            Debug.Log("STT Engine initialized successfully");
        }
        else
        {
            UpdateStatus("Initialization failed. Check console for details.");
            Debug.LogError("Failed to initialize STT Engine");
        }

        UpdateUIState();
    }

    /// <summary>
    /// Start recording from microphone
    /// </summary>
    public void StartRecording()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Engine not initialized");
            return;
        }

        sttEngine.StartRecording();
        UpdateStatus("Recording... Speak now!");
        UpdateUIState();

        Debug.Log("Recording started");
    }

    /// <summary>
    /// Stop recording and transcribe
    /// </summary>
    public async void StopRecordingAndTranscribe()
    {
        if (!sttEngine.IsRecording)
        {
            Debug.LogWarning("Not currently recording");
            return;
        }

        UpdateStatus("Processing recording...");
        UpdateUIState();

        STTResult result = await sttEngine.StopRecordingAndTranscribe();

        if (result.success)
        {
            UpdateTranscription(result.text);
            Debug.Log($"Transcription successful: {result.text}");
        }
        else
        {
            UpdateTranscription($"Error: {result.error}");
            Debug.LogError($"Transcription failed: {result.error}");
        }

        UpdateUIState();
    }

    /// <summary>
    /// Transcribe a pre-recorded AudioClip
    /// </summary>
    public async void TranscribeTestClip()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Engine not initialized");
            return;
        }

        if (testAudioClip == null)
        {
            Debug.LogWarning("No test audio clip assigned");
            return;
        }

        UpdateStatus("Transcribing test audio...");

        STTResult result = await sttEngine.Transcribe(testAudioClip);

        if (result.success)
        {
            UpdateTranscription(result.text);
            Debug.Log($"Transcription successful: {result.text}");
        }
        else
        {
            UpdateTranscription($"Error: {result.error}");
            Debug.LogError($"Transcription failed: {result.error}");
        }
    }

    #endregion

    #region Event Handlers

    private void OnTranscriptionComplete(STTResult result)
    {
        Debug.Log($"[Event] Transcription complete: {result}");

        if (result.success)
        {
            UpdateStatus($"Complete! (took {result.processingTime:F2}s)");
        }
    }

    private void OnProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    private void OnStatusChanged(string status)
    {
        UpdateStatus(status);
    }

    private void OnError(string error)
    {
        Debug.LogError($"[STT Error] {error}");
        UpdateStatus($"Error: {error}");
    }

    #endregion

    #region Helper Methods

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log($"[Status] {status}");
    }

    private void UpdateTranscription(string text)
    {
        if (transcriptionText != null)
        {
            transcriptionText.text = text;
        }
    }

    #endregion

    #region Example Usage (Code-only, no UI)

    /// <summary>
    /// Example: Initialize and transcribe an AudioClip programmatically
    /// </summary>
    private async Task ExampleTranscribeClip(AudioClip clip)
    {
        // 1. Initialize engine
        bool initialized = await sttEngine.Initialize();
        if (!initialized)
        {
            Debug.LogError("Failed to initialize");
            return;
        }

        // 2. Transcribe
        STTResult result = await sttEngine.Transcribe(clip);

        // 3. Use result
        if (result.success)
        {
            Debug.Log($"Transcription: {result.text}");
            Debug.Log($"Language: {result.language}");
            Debug.Log($"Processing time: {result.processingTime}s");
        }
    }

    /// <summary>
    /// Example: Record from microphone and transcribe
    /// </summary>
    private async Task ExampleRecordAndTranscribe()
    {
        // 1. Initialize
        await sttEngine.Initialize();

        // 2. Start recording
        sttEngine.StartRecording();

        // 3. Wait for 3 seconds
        await Task.Delay(3000);

        // 4. Stop and transcribe
        STTResult result = await sttEngine.StopRecordingAndTranscribe();

        if (result.success)
        {
            Debug.Log($"You said: {result.text}");
        }
    }

    /// <summary>
    /// Example: Using events
    /// </summary>
    private void ExampleUsingEvents()
    {
        // Subscribe to events
        sttEngine.OnTranscriptionComplete += (result) =>
        {
            Debug.Log($"Transcription: {result.text}");
        };

        sttEngine.OnProgress += (progress) =>
        {
            Debug.Log($"Progress: {progress * 100}%");
        };

        sttEngine.OnError += (error) =>
        {
            Debug.LogError($"Error: {error}");
        };
    }

    #endregion
}
