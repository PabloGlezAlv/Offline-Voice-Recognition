# Installation Guide - Offline Speech Recognition Plugin

## Quick Setup (2 minutes)

### Step 1: Import the Plugin
The plugin is already in your project at `Assets/Offline Speech Recognition/`

### Step 2: Create a Test Scene
1. Create a new Scene
2. Create an empty GameObject named "STTManager"
3. Add component `STTEngine` to the GameObject:
   - In Inspector: Click `Add Component`
   - Search for "STT Engine"
   - Select "STT Engine" from `OfflineSpeechRecognition.Core`

### Step 3: Configure in Inspector
With the GameObject selected:
- **Selected Model**: Choose "Base" (recommended for testing)
- **Selected Language**: Choose "English"

### Step 4: Download Your First Model
1. Keep the GameObject selected
2. Look at the Inspector (the STTEngine component)
3. You should see a "Download Model" button
4. Click it and wait for download to complete
5. Check Console for "Model Base downloaded successfully"

### Step 5: Test with Example Script
1. Create a new C# script called "QuickTest.cs"
2. Copy this code:

```csharp
using UnityEngine;
using OfflineSpeechRecognition.Core;

public class QuickTest : MonoBehaviour
{
    public STTEngine sttEngine;

    void Start()
    {
        sttEngine.OnTranscriptionComplete += (text) =>
            Debug.Log("Transcribed: " + text);

        sttEngine.OnError += (error) =>
            Debug.LogError("Error: " + error);
    }

    void Update()
    {
        // Press R to start recording
        if (Input.GetKeyDown(KeyCode.R))
        {
            sttEngine.StartMicrophoneCapture();
            Debug.Log("Recording... speak now");
        }

        // Press S to stop and transcribe
        if (Input.GetKeyDown(KeyCode.S))
        {
            sttEngine.TranscribeFromMicrophone();
        }
    }
}
```

3. Attach this script to your GameObject
4. Drag the GameObject to the `sttEngine` field in the script
5. Press Play in editor
6. Press 'R' to start recording
7. Speak clearly
8. Press 'S' to transcribe
9. Check Console for result!

## Requirements

### Windows
- Windows 10 or later
- No additional software needed
- Microphone connected

### Android
- Android 8.0+ (API 26+)
- ~500MB free storage
- Microphone permission (auto-requested)

### Editor
- Unity 6 LTS or later
- Works on any OS

## Troubleshooting

### "Model not downloaded"
- Solution: Click "Download Model" button in Inspector
- Check Console for progress
- Wait for "downloaded successfully" message

### "No microphone found"
- Windows: Check Settings > Privacy & Security > Microphone
- Android: App permissions > Microphone
- Editor: Make sure microphone is enabled

### "Empty transcription"
- Speak louder and clearer
- Check microphone works (test in system settings)
- Try a larger model (Small or Medium)

### Compilation Errors
1. Delete the `Library` folder
2. Delete any `nul` or `nul.meta` files in plugin folder
3. Restart Unity
4. Let it recompile

## Next Steps

1. Check `README.md` for complete documentation
2. Look at `STTExample.cs` for more features
3. Check `AdvancedSTTExample.cs` for voice commands

## Support

- Console shows detailed error messages
- Use `sttEngine.GetDebugInfo()` to check status
- Check model availability: `sttEngine.GetAllModels()`

---

**You're all set!** 🎉 The plugin is ready to use.
