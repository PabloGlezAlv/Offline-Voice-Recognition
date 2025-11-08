# Offline Voice Recognition for Unity

A complete offline speech-to-text solution for Unity, powered by OpenAI's Whisper model and Unity Sentis.

## 🎯 Overview

This repository contains a Unity plugin that enables offline voice recognition in your Unity projects. The plugin uses OpenAI's Whisper model for accurate multilingual speech recognition without requiring an internet connection.

## 📁 Repository Structure

```
Offline-Voice-Recognition/
│
└── Offline invoice Recognition/          # Main plugin package
    ├── Runtime/
    │   ├── Scripts/
    │   │   ├── STTEngine.cs              # Main API class
    │   │   ├── WhisperInference.cs       # Whisper model inference
    │   │   ├── ModelDownloader.cs        # Model download management
    │   │   ├── AudioProcessor.cs         # Audio processing utilities
    │   │   ├── ModelConfig.cs            # Model configurations
    │   │   └── STTResult.cs              # Result data structure
    │   └── Offline_invoice_Recognition.Runtime.asmdef
    │
    ├── Editor/
    │   ├── STTEngineEditor.cs            # Custom inspector
    │   └── Offline_invoice_Recognition.Editor.asmdef
    │
    ├── Samples~/
    │   └── BasicDemo/
    │       └── STTDemoScript.cs          # Usage examples
    │
    ├── Documentation~/
    │   └── README.md                     # Full documentation
    │
    ├── package.json                      # Unity package manifest
    ├── LICENSE.md
    └── .gitignore
```

## 🚀 Quick Start

### For Unity Users

1. **Add to Unity Project:**
   ```
   Window > Package Manager > + > Add package from git URL
   https://github.com/YOUR_USERNAME/Offline-Voice-Recognition.git?path=/Offline invoice Recognition
   ```

2. **Download a Model:**
   - Add `STTEngine` component to a GameObject
   - Select model size in Inspector
   - Click "Download Model"

3. **Start Using:**
   ```csharp
   using OfflineInvoiceRecognition;

   public class MyScript : MonoBehaviour
   {
       public STTEngine sttEngine;

       async void Start()
       {
           await sttEngine.Initialize();

           sttEngine.StartRecording();
           await Task.Delay(3000);

           STTResult result = await sttEngine.StopRecordingAndTranscribe();
           Debug.Log($"You said: {result.text}");
       }
   }
   ```

For complete documentation, see [`Offline invoice Recognition/Documentation~/README.md`](Offline%20invoice%20Recognition/Documentation~/README.md)

## ✨ Features

- ✅ **100% Offline** - Works without internet after initial model download
- ✅ **Multilingual** - Supports 99+ languages automatically
- ✅ **Cross-Platform** - PC (Windows, Mac, Linux) and Mobile (iOS, Android)
- ✅ **Multiple Models** - Choose between speed (Tiny) and accuracy (Large)
- ✅ **Easy Integration** - Simple C# API with async/await support
- ✅ **Unity Sentis** - Uses Unity's official ML runtime for best performance
- ✅ **Custom Editor** - Download and manage models from Unity Inspector

## 📊 Available Models

| Model | Size | Speed | Accuracy | Best For |
|-------|------|-------|----------|----------|
| Tiny | 75 MB | ⚡⚡⚡⚡ | ⭐⭐ | Mobile, Prototypes |
| Small | 150 MB | ⚡⚡⚡ | ⭐⭐⭐ | General Use ⭐ |
| Medium | 1.5 GB | ⚡⚡ | ⭐⭐⭐⭐ | Desktop Apps |
| Large | 3 GB | ⚡ | ⭐⭐⭐⭐⭐ | Maximum Accuracy |

## 🌍 Supported Languages

English, Spanish, French, German, Italian, Portuguese, Russian, Chinese, Japanese, Korean, Arabic, Hindi, Dutch, Polish, Turkish, Swedish, and 80+ more languages!

Language detection is automatic - no configuration needed.

## 📋 Requirements

- Unity 2022.3 LTS or newer
- Unity Sentis 1.3.0+ (installed automatically)
- Storage: 75 MB - 3 GB depending on model
- Platforms: Windows, macOS, Linux, iOS, Android

## 🔧 Development

### Project Setup

```bash
git clone https://github.com/YOUR_USERNAME/Offline-Voice-Recognition.git
cd Offline-Voice-Recognition
```

### Building the Package

The plugin is already structured as a Unity package. Simply reference it in your Unity project via Package Manager.

## 📄 License

MIT License - see [LICENSE.md](Offline%20invoice%20Recognition/LICENSE.md)

This project uses:
- **Unity Sentis** by Unity Technologies
- **Whisper** by OpenAI (MIT License)
- **ONNX Models** from HuggingFace Community

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/YOUR_USERNAME/Offline-Voice-Recognition/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YOUR_USERNAME/Offline-Voice-Recognition/discussions)
- **Documentation**: See `Offline invoice Recognition/Documentation~/README.md`

## 🎉 Acknowledgments

- OpenAI for the amazing Whisper model
- Unity Technologies for Unity Sentis
- HuggingFace for hosting ONNX models
- The open-source community

---

Made with ❤️ for the Unity community
