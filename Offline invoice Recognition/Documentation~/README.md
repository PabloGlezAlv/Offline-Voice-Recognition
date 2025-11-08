# Offline Invoice Recognition - Speech-to-Text Plugin for Unity

A powerful offline speech-to-text plugin for Unity based on OpenAI's Whisper model. Perfect for voice-controlled applications, accessibility features, and interactive experiences that work without internet connection.

## 🎯 Features

- ✅ **100% Offline** - No internet required after model download
- ✅ **Multilingual Support** - Supports 99+ languages automatically
- ✅ **Cross-Platform** - Works on PC (Windows, Mac, Linux) and Mobile (iOS, Android)
- ✅ **Multiple Model Sizes** - Choose between speed and accuracy
- ✅ **Unity Sentis Powered** - Leverages Unity's official ML runtime
- ✅ **Easy Integration** - Simple API, just a few lines of code
- ✅ **Microphone Recording** - Built-in recording functionality
- ✅ **Custom Editor** - Download models directly from Unity Inspector

## 📋 Requirements

- **Unity Version**: 2022.3 LTS or newer
- **Unity Sentis**: 1.3.0 or newer (will be installed automatically)
- **Platform**: Windows, macOS, Linux, iOS, Android
- **Storage**: 75 MB to 3 GB (depending on model size)

## 📦 Installation

### Option 1: Unity Package Manager (Git URL)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/YOUR_REPO/Offline-Voice-Recognition.git?path=/Offline invoice Recognition`

### Option 2: Manual Installation

1. Download the latest release
2. Import the `.unitypackage` into your project
3. Unity Sentis will be installed automatically if not present

## 🚀 Quick Start

### 1. Setup

1. Create an empty GameObject in your scene
2. Add the `STTEngine` component
3. In the Inspector, select a **Model Size**:
   - **Tiny** (75 MB) - Fast, basic accuracy (mobile friendly)
   - **Small** (150 MB) - Good balance ⭐ **Recommended**
   - **Medium** (1.5 GB) - High accuracy
   - **Large** (3 GB) - Best accuracy

4. Click **"Download Model"** button
5. Wait for download to complete

### 2. Basic Usage (Code)

```csharp
using OfflineInvoiceRecognition;
using UnityEngine;

public class MyScript : MonoBehaviour
{
    public STTEngine sttEngine;

    async void Start()
    {
        // Initialize the engine
        await sttEngine.Initialize();

        // Transcribe an AudioClip
        AudioClip myAudio = ...; // Your audio clip
        STTResult result = await sttEngine.Transcribe(myAudio);

        if (result.success)
        {
            Debug.Log($"Transcription: {result.text}");
            Debug.Log($"Language: {result.language}");
        }
    }
}
```

### 3. Microphone Recording

```csharp
using OfflineInvoiceRecognition;

public class VoiceRecorder : MonoBehaviour
{
    public STTEngine sttEngine;

    async void Start()
    {
        await sttEngine.Initialize();
    }

    public void OnRecordButtonPressed()
    {
        sttEngine.StartRecording();
    }

    public async void OnStopButtonPressed()
    {
        STTResult result = await sttEngine.StopRecordingAndTranscribe();

        if (result.success)
        {
            Debug.Log($"You said: {result.text}");
        }
    }
}
```

### 4. Using Events

```csharp
void Start()
{
    // Subscribe to events
    sttEngine.OnTranscriptionComplete += OnTranscriptionComplete;
    sttEngine.OnProgress += OnProgress;
    sttEngine.OnStatusChanged += OnStatusChanged;
    sttEngine.OnError += OnError;

    sttEngine.Initialize();
}

void OnTranscriptionComplete(STTResult result)
{
    Debug.Log($"Complete: {result.text}");
}

void OnProgress(float progress)
{
    Debug.Log($"Progress: {progress * 100}%");
}

void OnStatusChanged(string status)
{
    Debug.Log($"Status: {status}");
}

void OnError(string error)
{
    Debug.LogError($"Error: {error}");
}
```

## 📚 API Reference

### STTEngine Component

#### Methods

| Method | Description |
|--------|-------------|
| `Initialize()` | Load the model and prepare for transcription |
| `Transcribe(AudioClip)` | Transcribe an AudioClip to text |
| `StartRecording()` | Start recording from microphone |
| `StopRecordingAndTranscribe()` | Stop recording and transcribe |
| `CancelRecording()` | Cancel recording without transcribing |
| `DownloadModel(Action<float>)` | Download the selected model |
| `IsModelDownloaded()` | Check if model is downloaded |

#### Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnTranscriptionComplete` | `STTResult` | Fired when transcription completes |
| `OnProgress` | `float` | Progress updates (0-1) |
| `OnStatusChanged` | `string` | Status message updates |
| `OnError` | `string` | Error messages |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsInitialized` | `bool` | Whether engine is initialized |
| `IsRecording` | `bool` | Whether currently recording |
| `CurrentModelSize` | `ModelSize` | Selected model size |
| `LastTranscriptionTime` | `float` | Processing time of last transcription |

### STTResult Class

```csharp
public class STTResult
{
    public string text;              // Transcribed text
    public string language;          // Detected language (ISO 639-1)
    public float confidence;         // Confidence score (0-1)
    public float processingTime;     // Time taken in seconds
    public bool success;             // Whether transcription succeeded
    public string error;             // Error message if failed
}
```

## 🌍 Supported Languages

Whisper supports 99+ languages including:

- English (en)
- Spanish (es)
- French (fr)
- German (de)
- Italian (it)
- Portuguese (pt)
- Russian (ru)
- Chinese (zh)
- Japanese (ja)
- Korean (ko)
- Arabic (ar)
- And many more...

Language is detected automatically!

## 📊 Model Comparison

| Model | Size | Speed | Accuracy | Use Case |
|-------|------|-------|----------|----------|
| Tiny | 75 MB | ⚡⚡⚡⚡ | ⭐⭐ | Mobile, prototypes |
| Small | 150 MB | ⚡⚡⚡ | ⭐⭐⭐ | Recommended for most |
| Medium | 1.5 GB | ⚡⚡ | ⭐⭐⭐⭐ | Desktop apps |
| Large | 3 GB | ⚡ | ⭐⭐⭐⭐⭐ | Maximum accuracy |

## 🔧 Advanced Configuration

### Custom Microphone Device

```csharp
// Get available microphones
string[] devices = STTEngine.GetMicrophoneDevices();

// Set in Inspector or via code
sttEngine.microphoneDevice = devices[0];
```

### Processing External Audio Files

```csharp
// Load audio file (requires additional plugin like NAudio)
AudioClip clip = LoadAudioFile("path/to/file.wav");
STTResult result = await sttEngine.Transcribe(clip);
```

### Model Storage Location

Models are stored in:
```
Application.persistentDataPath/WhisperModels/{model-name}/
```

## ⚙️ Performance Tips

1. **Choose the right model**:
   - Mobile: Use Tiny or Small
   - Desktop: Use Medium or Large

2. **Audio quality matters**:
   - 16kHz sample rate is optimal
   - Mono audio is sufficient
   - Clear audio = better results

3. **GPU Acceleration**:
   - Automatically enabled if available
   - Fallback to CPU if needed

4. **First transcription is slower**:
   - Model loading happens on first run
   - Subsequent transcriptions are faster

## 🐛 Troubleshooting

### Model download fails
- Check internet connection
- Verify sufficient storage space
- Try re-downloading from Inspector

### Transcription returns empty text
- Ensure model is downloaded
- Check audio clip is not null
- Verify audio has actual content (not silence)

### "Engine not initialized" error
- Call `await sttEngine.Initialize()` before transcribing
- Check model files exist in persistent data path

### Poor accuracy
- Try a larger model size
- Improve audio quality (reduce background noise)
- Ensure microphone is working properly

## 📝 Example Scenes

Check the `Samples~/BasicDemo` folder for:
- **STTDemoScript.cs** - Complete usage example
- **Demo scene** (coming soon) - Interactive UI demo

## 🔗 Resources

- [Unity Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@latest)
- [Whisper Paper](https://arxiv.org/abs/2212.04356)
- [HuggingFace Whisper Models](https://huggingface.co/onnx-community)

## 📄 License

This plugin uses:
- **Unity Sentis** - Unity Technologies
- **Whisper** - OpenAI (MIT License)
- **ONNX Models** - HuggingFace Community

See LICENSE file for details.

## 🤝 Support

For issues, questions, or feature requests:
- GitHub Issues: [YOUR_REPO/issues]
- Email: support@yourcompany.com

## 🎉 Credits

Created with ❤️ for the Unity community

Based on OpenAI's Whisper model
