# Offline Speech Recognition Plugin for Unity

A professional, production-ready Speech-to-Text (STT) plugin for Unity 6 that runs Whisper models entirely offline on Windows and Android devices.

## Features

- **Offline Processing**: No internet required for transcription
- **Multiple Models**: Support for Tiny, Base, Small, Medium, and Large Whisper models
- **Multi-Language**: Support for 100+ languages with intelligent language selection
- **Real-Time Microphone Capture**: Record and transcribe audio in real-time
- **File Processing**: Transcribe audio files (WAV, MP3, OGG)
- **Async Processing**: Background thread processing keeps UI responsive
- **Professional UI Components**: Inspector customization and editor tools
- **Cross-Platform**: Optimized for Windows and Android (tested on both)
- **Event-Based API**: Easy integration with callbacks and events

## Quick Start

### 1. Installation

1. Import the plugin into your Unity project
2. Ensure you have Unity 6 LTS or later
3. Required packages (should auto-import):
   - com.unity.sentis (AI/ML inference)
   - com.unity.burst (Performance optimization)

### 2. Basic Setup

1. Create a new GameObject in your scene
2. Add the `STTEngine` component: `Add Component > Offline Speech Recognition > STT Engine`
3. In the Inspector:
   - Select your desired model (e.g., "Base")
   - Select your desired language (e.g., "English")
4. Attach the included `STTExample` script to test

### 3. First Model Download

1. With STTEngine selected in the Inspector
2. Click the "Download Model" button
3. Wait for the download to complete (check the Console)
4. The model will be stored in `Application.persistentDataPath`

### 4. Basic Usage

```csharp
using OfflineSpeechRecognition.Core;
using UnityEngine;

public class MyVoiceApp : MonoBehaviour
{
    [SerializeField] private STTEngine sttEngine;

    void Start()
    {
        // Subscribe to transcription result
        sttEngine.OnTranscriptionComplete += HandleTranscription;
        sttEngine.OnError += HandleError;
    }

    public void RecordAndTranscribe()
    {
        // Start recording from microphone
        sttEngine.StartMicrophoneCapture();

        // ... later, when user stops speaking ...

        // Transcribe the recorded audio
        sttEngine.TranscribeFromMicrophone();
    }

    void HandleTranscription(string text)
    {
        Debug.Log($"Transcribed: {text}");
        // Use the transcribed text in your game
    }

    void HandleError(string error)
    {
        Debug.LogError($"Error: {error}");
    }
}
```

## API Reference

### STTEngine Component

The main component that orchestrates all STT functionality.

#### Methods

```csharp
// Microphone Recording
void StartMicrophoneCapture()          // Start recording from microphone
void TranscribeFromMicrophone()        // Stop recording and transcribe
void StopMicrophoneCapture()           // Cancel recording without transcribing

// File Processing
void TranscribeFromFile(string path)   // Transcribe audio file

// Model Management
void DownloadModel(WhisperModel.ModelSize size)  // Download a model
void SetModel(WhisperModel.ModelSize size)       // Switch active model
bool IsModelDownloaded(WhisperModel.ModelSize size)
List<WhisperModel> GetAllModels()
List<WhisperModel> GetDownloadedModels()

// Configuration
void SetLanguage(WhisperLanguage language)
WhisperLanguage GetCurrentLanguage()
WhisperModel.ModelSize GetCurrentModel()

// Status
bool IsRecording { get; }
int GetQueueSize()
string GetDebugInfo()

// Microphones
static string[] GetAvailableMicrophones()
void SetMicrophone(string deviceName)
```

#### Events

```csharp
// Transcription complete with result
event Action<string> OnTranscriptionComplete

// Processing started
event Action OnTranscriptionStarted

// Download progress (0.0 to 1.0)
event Action<float> OnDownloadProgress

// Errors
event Action<string> OnError
```

### Model Sizes

| Model | Size | Speed | Accuracy | Recommended For |
|-------|------|-------|----------|-----------------|
| Tiny | 140 MB | Very Fast | Good | Mobile, Quick responses |
| Base | 290 MB | Fast | Very Good | Balanced, General use |
| Small | 770 MB | Medium | Excellent | High accuracy needs |
| Medium | 1.5 GB | Slow | Excellent | High-end devices |
| Large | 3.1 GB | Very Slow | Best | Servers, High-end PCs |

### Supported Languages

The plugin supports 100+ languages including:
- English, Spanish, French, German, Italian, Portuguese
- Chinese (Simplified), Japanese, Korean
- Arabic, Hebrew, Hindi, Thai, Vietnamese
- Russian, Ukrainian, Polish, Czech
- And many more...

## Platform Requirements

### Windows
- Windows 10 or later
- .NET Framework 4.7.1+
- No additional dependencies

### Android
- Android 8.0 (API 26) or later
- Microphone permission required
- Recommended: ARM64 devices
- Min 500MB free storage for largest models

### Editor
- Full support for development and testing
- Same model downloading as runtime

## Performance Considerations

### Model Selection
- **Tiny**: ~1-2 seconds processing for 10s audio
- **Base**: ~2-4 seconds processing
- **Small**: ~5-10 seconds processing
- **Medium**: ~15-30 seconds processing
- **Large**: ~30+ seconds processing

### Memory Usage
- Models are loaded once and cached
- Typical memory overhead: 200-500MB depending on model size
- Audio processing: Additional ~10MB per concurrent task

### Optimization Tips
1. Use smaller models (Tiny/Base) for real-time apps
2. Run long transcriptions in background
3. Clear unused models to free storage space
4. Use Burst compilation for better performance

## Audio Format Support

### Microphone Input
- Automatic sample rate detection
- Mono and stereo support (converted to mono)
- 16-bit PCM recommended

### File Input
- **WAV**: Full support (recommended)
- **MP3**: Supported (async loading)
- **OGG**: Supported (async loading)

## Examples Included

### 1. STTExample.cs (Basic)
Simple example showing:
- Starting/stopping microphone recording
- Transcribing microphone audio
- Transcribing audio files
- Handling callbacks

**Usage**: Attach to GameObject with STTEngine

### 2. AdvancedSTTExample.cs
Advanced features:
- Voice command detection
- Multi-language support
- Model management
- Smart recording with auto-stop

**Usage**: Attach to GameObject with STTEngine

## Troubleshooting

### Model Download Issues
**Problem**: Download fails or is slow
- **Solution**: Check internet connection
- **Solution**: Try a smaller model (Tiny/Base)
- **Solution**: Check persistent data path has enough space

### Microphone Permission Error
**Problem**: "No microphone devices found" or permission denied
- **Windows**: Ensure microphone is not disabled in settings
- **Android**: Check app has microphone permission granted

### Transcription is Empty
**Problem**: Audio is recorded but no text returned
- **Solution**: Ensure model is fully downloaded
- **Solution**: Check audio quality (speak clearly)
- **Solution**: Verify correct language is selected
- **Solution**: Try a larger model for better accuracy

### Poor Transcription Quality
**Problem**: Incorrect or incomplete transcription
- **Solution**: Reduce background noise
- **Solution**: Speak more clearly and slowly
- **Solution**: Use a larger model (Small/Medium)
- **Solution**: Ensure correct language is selected

### Performance Issues
**Problem**: Game freezes or lags during processing
- **Solution**: Ensure audio processor is running (background thread)
- **Solution**: Use smaller models
- **Solution**: Reduce quality settings in game
- **Solution**: Check device temperature/thermal throttling

## Storage Management

Models are stored in:
- **Android**: `Application.persistentDataPath/OfflineSpeechRecognition/Models`
- **Windows**: `%APPDATA%/LocalLow/CompanyName/ProjectName/OfflineSpeechRecognition/Models`
- **Editor**: Project's persistent data path

To clear models:
```csharp
ModelManager manager = ModelManager.Instance;
manager.DeleteModel(WhisperModel.ModelSize.Base);
manager.ClearAllModels(); // Remove all
```

## Android Specific

### Permissions Required
Add to AndroidManifest.xml:
```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.INTERNET" />
```

### Permission Handling
The plugin automatically requests microphone permission at runtime.

### Memory Limitations
- Keep models 1GB or smaller for reliable performance
- Consider using Tiny model on older devices
- Monitor memory usage during processing

## Windows Specific

### Audio System
- Uses Unity's AudioListener
- Supports multiple audio devices
- Hardware acceleration enabled automatically

## Best Practices

1. **Model Selection**: Choose based on accuracy needs vs. device capabilities
2. **Language**: Set correctly for best results
3. **Audio Quality**: Ensure clear, quiet environment
4. **Threading**: Always use callbacks, don't block main thread
5. **Memory**: Unload models when not needed on mobile

## API Stability

This plugin uses:
- ✅ **Stable**: Unity Core Audio API
- ✅ **Stable**: File I/O (System.IO)
- ✅ **Stable**: Threading (System.Threading)
- ✅ **Stable**: Unity Sentis (Version 1.0+)

## License and Attribution

Whisper models are provided by OpenAI under CC-BY-NC 4.0 License.
This plugin provides an integration layer for Unity.

## Support and Issues

For issues, please check:
1. Console output for detailed error messages
2. Debug info via `sttEngine.GetDebugInfo()`
3. Model status via `sttEngine.GetAllModels()`
4. Available microphones via `STTEngine.GetAvailableMicrophones()`

## Version History

### Version 1.0.0
- Initial release
- Support for Tiny to Large models
- 100+ language support
- Windows and Android support
- Real-time and file transcription
- Professional editor tools

## FAQ

**Q: Can I use this offline?**
A: Yes! Once models are downloaded, everything runs locally without internet.

**Q: What's the largest model I can use on mobile?**
A: Base model (290 MB) is recommended for most Android devices.

**Q: How accurate is Whisper?**
A: Very accurate for clear audio. Accuracy depends on model size and audio quality.

**Q: Can I use custom models?**
A: Currently only OpenAI Whisper ONNX models are supported.

**Q: Does it support real-time transcription?**
A: It supports transcribing audio blocks; true streaming would require model modifications.

**Q: How much storage do I need?**
A: Depends on models. Tiny=140MB, Base=290MB, Small=770MB, Medium=1.5GB, Large=3.1GB

**Q: Is there a size limit for audio files?**
A: Practical limit is ~30 minutes per file on 2GB+ RAM devices.

## Credits

- Built for Unity 6 LTS
- Uses OpenAI Whisper models
- Powered by ONNX Runtime and Unity Sentis
- Professional integration and optimization

---

**Plugin Version**: 1.0.0
**Last Updated**: 2024
**Maintained by**: Your Company Name
