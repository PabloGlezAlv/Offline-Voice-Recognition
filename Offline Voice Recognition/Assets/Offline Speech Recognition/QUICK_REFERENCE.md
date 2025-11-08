# ⚡ QUICK REFERENCE - Guía Ultra Rápida

## 📍 3 PASOS PARA EMPEZAR

### 1️⃣ DESCARGAR MODELO EN EDITOR
```
GameObject > Create Empty > "STTManager"
STTManager > Add Component > STTEngine
Inspector > [Download Base]
⏳ Esperar a que termine
```

### 2️⃣ CONFIGURAR UI (Opcional)
```
Canvas > TextMeshPro UI
└── TranscriptionText, StatusText, ModelInfoText
STTManager > Add Component > STTUIExample
Arrastra los 3 textos a los campos
```

### 3️⃣ PLAY Y GRABAR
```
Play (▶️)
R → Grabar
S → Transcribir
Ver resultado en TranscriptionText
```

---

## 🎮 CONTROLES EN PLAY MODE

```
R = Grabar
S = Detener y transcribir
C = Cancelar
L = Cambiar idioma
M = Cambiar modelo
P = Debug info
```

---

## 💻 CÓDIGO MÍNIMO

```csharp
using OfflineSpeechRecognition.Core;
using UnityEngine;

public class QuickTest : MonoBehaviour
{
    public STTEngine sttEngine;

    void Start()
    {
        sttEngine.OnTranscriptionComplete +=
            (text) => Debug.Log("Transcrito: " + text);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            sttEngine.StartMicrophoneCapture();

        if (Input.GetKeyDown(KeyCode.S))
            sttEngine.TranscribeFromMicrophone();
    }
}
```

---

## 📂 ARCHIVOS IMPORTANTES

| Archivo | Propósito |
|---------|-----------|
| **STTEngine.cs** | Script principal |
| **STTEngineEditor.cs** | Botones en Inspector |
| **STTUIExample.cs** | UI con TextMeshPro |
| **README.md** | Documentación completa |
| **INSTALLATION.md** | Setup paso a paso |

---

## 🔍 INSPECTOR STTEngine

```
Selected Model: Base ← Selecciona modelo
Selected Language: English ← Selecciona idioma
Auto Download Models: ☐

[Download Tiny]
[Download Base] ← Click para descargar
[Download Small]
[Download Medium]
[Download Large]
```

---

## 📊 MODELOS DISPONIBLES

| Modelo | Tamaño | Velocidad | Precisión |
|--------|--------|-----------|-----------|
| Tiny | 140 MB | ⚡⚡⚡ | ⭐⭐ |
| Base | 290 MB | ⚡⚡ | ⭐⭐⭐ |
| Small | 770 MB | ⚡ | ⭐⭐⭐⭐ |
| Medium | 1.5 GB | ⭐ | ⭐⭐⭐⭐⭐ |
| Large | 3.1 GB | ⭐ | ⭐⭐⭐⭐⭐ |

**Recomendado**: Base (balance)

---

## 🌍 IDIOMAS SOPORTADOS

85+ idiomas incluyendo:
- English, Spanish, French, German, Italian
- Portuguese, Chinese, Japanese, Korean
- Russian, Arabic, Hindi, Thai, Vietnamese
- Y muchos más...

---

## 🛠️ TROUBLESHOOTING RÁPIDO

| Problema | Solución |
|----------|----------|
| "No microphone" | Conecta micrófono |
| "Model not downloaded" | Click [Download] en Inspector |
| "Empty transcription" | Habla más claro |
| "Script not found" | Asegúrate de haber guardado |
| "Text not updating" | Verifica referencias en Inspector |

---

## 📚 DOCUMENTOS

- **INSTALLATION.md** ← Empieza aquí
- **UI_SETUP.md** ← Para configurar TextMeshPro
- **README.md** ← Referencia completa
- **FINAL_SUMMARY.md** ← Overview

---

## ⚙️ CONFIGURACIÓN MÍNIMA

```
STTManager (GameObject)
├── STTEngine
│   ├── Selected Model: Base
│   └── Selected Language: English
└── STTUIExample (opcional)
    ├── Stt Engine: STTManager
    ├── Transcription Text: TranscriptionText
    ├── Status Text: StatusText
    └── Model Info Text: ModelInfoText
```

---

## ✅ CHECKLIST

- [ ] Modelo descargado en Editor
- [ ] GameObject con STTEngine creado
- [ ] Canvas y TextMeshPro (si usas UI)
- [ ] STTUIExample asignado (si usas UI)
- [ ] Referencias conectadas
- [ ] Play mode
- [ ] Presiona R para grabar

---

## 🎯 CASOS DE USO

**Transcripción simple:**
```csharp
sttEngine.OnTranscriptionComplete += (text) =>
    Debug.Log(text);
sttEngine.StartMicrophoneCapture();
```

**Transcripción con archivo:**
```csharp
sttEngine.TranscribeFromFile("path/audio.wav");
```

**Cambiar modelo:**
```csharp
sttEngine.SetModel(WhisperModel.ModelSize.Small);
```

**Cambiar idioma:**
```csharp
sttEngine.SetLanguage(WhisperLanguage.Spanish);
```

---

**¿Más detalles?** Lee INSTALLATION.md o README.md

**¿Dudas?** Revisa la Console para errores específicos.

¡Happy transcribing! 🎤
