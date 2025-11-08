# Setup UI con TextMeshPro - Guía Completa

## Parte 1: Editor Tools para Descargar Modelos

### Paso 1: Inspector STTEngine

Cuando selecciones un GameObject con STTEngine en el Inspector, verás:

```
Offline Speech Recognition - STT Engine
Version 1.0.0

Model Management
ℹ️ Download Whisper models from Hugging Face...

┌─ TINY ────────────────────────────────┐
│ ✗ Not Downloaded (140 MB)             │
│ [Download Tiny]                        │
└────────────────────────────────────────┘

┌─ BASE ────────────────────────────────┐
│ ✓ Downloaded (290 MB)                 │
│ [Delete]  [View Storage]              │
└────────────────────────────────────────┘

┌─ SMALL ───────────────────────────────┐
│ ✗ Not Downloaded (770 MB)             │
│ [Download Small]                       │
└────────────────────────────────────────┘

┌─ MEDIUM ──────────────────────────────┐
│ ✗ Not Downloaded (1.5 GB)             │
│ [Download Medium]                      │
└────────────────────────────────────────┘

┌─ LARGE ───────────────────────────────┐
│ ✗ Not Downloaded (3.1 GB)             │
│ [Download Large]                       │
└────────────────────────────────────────┘

[Clear All Downloaded Models]

Debug Information
Initialized: True
Current Model: Base
Current Language: English
Model Downloaded: True
Recording: False
Queue Size: 0

[Refresh Models Status]
```

### Paso 2: Descargar un Modelo

1. Selecciona STTManager (GameObject con STTEngine)
2. En el Inspector, encuentra el modelo que quieres
3. Haz clic en **[Download Base]** (por ejemplo)
4. Espera a que termine
5. El botón cambiará a **[Delete]** cuando esté descargado
6. Ver en Console: `"Model Base downloaded successfully"`

---

## Parte 2: UI con TextMeshPro

### Paso 1: Crear Canvas y UI

1. En Hierarchy, click derecho: `GameObject > UI > Canvas - TextMeshPro`
   - Si pide instalar TextMeshPro, acepta
2. Unity creará automáticamente:
   ```
   Canvas
   └── TextMeshProDefaultMaterial (resources)
   ```

### Paso 2: Crear TextMeshPro Text Elements

Dentro del Canvas, crea estos textos:

#### **A) Transcription Text** (Lo más importante)
1. Click derecho en Canvas: `2D Object > Text - TextMeshPro`
2. Llámalo: **"TranscriptionText"**
3. En el Inspector:
   - Rect Transform Width: 800, Height: 400
   - Pos Y: 200
   - TextMeshPro component:
     - Text: "Waiting for input..."
     - Font Size: 36
     - Alignment: Center
     - Color: White

#### **B) Status Text** (Muestra qué está pasando)
1. Click derecho en Canvas: `2D Object > Text - TextMeshPro`
2. Llámalo: **"StatusText"**
3. En el Inspector:
   - Rect Transform Width: 800, Height: 100
   - Pos Y: 0
   - TextMeshPro component:
     - Text: "Ready"
     - Font Size: 24
     - Alignment: Center
     - Color: Yellow

#### **C) Model Info Text** (Controles y info)
1. Click derecho en Canvas: `2D Object > Text - TextMeshPro`
2. Llámalo: **"ModelInfoText"**
3. En el Inspector:
   - Rect Transform Width: 200, Height: 300
   - Pos X: 320, Pos Y: -100
   - TextMeshPro component:
     - Text: "Model: Base..."
     - Font Size: 20
     - Alignment: TopLeft
     - Color: Cyan

### Paso 3: Estructura Final de Canvas

```
Canvas
├── TranscriptionText (centro, grande)
├── StatusText (arriba, amarillo)
├── ModelInfoText (abajo, info)
└── GraphicRaycaster
```

### Paso 4: Agregar Script STTUIExample

1. En Hierarchy, selecciona tu GameObject STTManager
2. Click en `Add Component`
3. Busca: **"STTUIExample"**
4. En el Inspector, completa los campos:
   - **Stt Engine**: Arrastra STTManager
   - **Transcription Text**: Arrastra TranscriptionText
   - **Status Text**: Arrastra StatusText
   - **Model Info Text**: Arrastra ModelInfoText

### Paso 5: Verificar Asignaciones

Debería verse así:

```
STTUIExample
├── Stt Engine: STTManager ✓
├── Transcription Text: TranscriptionText ✓
├── Status Text: StatusText ✓
└── Model Info Text: ModelInfoText ✓
```

---

## ¡Ahora a Probar!

### Paso 1: Play Mode
Presiona **Play** (▶️) en Unity

### Paso 2: Controles de Teclado

| Tecla | Acción |
|-------|--------|
| **R** | 🎤 Empezar a grabar |
| **S** | ⏹️ Parar y transcribir |
| **C** | ❌ Cancelar grabación |
| **L** | 🌍 Cambiar idioma |
| **M** | 📊 Cambiar modelo |

### Paso 3: Ver en Tiempo Real

**TranscriptionText** mostrará:
```
"Waiting for input..."
         ↓ (presiona R)
"Listening..."
         ↓ (hablas)
"⏳ Processing audio..."
         ↓ (resultados)
"hello how are you today"
```

**StatusText** mostrará:
```
"Ready"
  ↓
"🎤 Recording... 2.5s (Press S to stop)"
  ↓
"⏳ Processing audio..."
  ↓
"✅ Transcription complete!"
```

**ModelInfoText** mostrará:
```
Model: Base
Language: English
Downloaded: 1/5

R: Record | S: Stop | L: Language
M: Model | C: Cancel
```

---

## Ejemplo de Ejecución Completa

1. **Iniciar** → StatusText: "Ready"
2. **Presionar R** → StatusText: "🎤 Recording..." + TranscriptionText: "Listening..."
3. **Hablar 5 segundos** → StatusText: "🎤 Recording... 5.3s"
4. **Presionar S** → StatusText: "⏳ Processing audio..."
5. **Esperar resultado** → TranscriptionText: "hello how are you today"
6. **Presionar L** → StatusText: "Language changed to: Spanish"
7. **Presionar M** → StatusText: "Model changed to: Small" (si está descargado)

---

## Solucionar Problemas

### "Script not found"
```
✅ Asegúrate de haber guardado STTUIExample.cs
✅ En Assets/Offline Speech Recognition/Examples/
```

### "Text not updating"
```
✅ Verifica que hayas asignado los TextMeshPro en el Inspector
✅ Que no sean nulos en el código
```

### "Microphone not working"
```
✅ StatusText dirá: "❌ Error: No microphone devices found"
✅ Verifica que tu micrófono esté conectado
✅ Que el micrófono esté habilitado en Settings
```

### "Modelo no descargado"
```
✅ En el Inspector STTEngine, haz clic en [Download Base]
✅ Espera a que diga "Model Base downloaded successfully"
✅ ModelInfoText mostrará "Downloaded: 1/5"
```

---

## Personalizar UI

### Cambiar Colores

En `STTUIExample.cs`, modifica los UpdateStatus():

```csharp
// Cambiar color de TranscriptionText
private void UpdateTranscriptionText(string text)
{
    if (transcriptionText != null)
    {
        transcriptionText.text = text;
        transcriptionText.color = Color.cyan; // Cambiar aquí
    }
}
```

### Cambiar Tamaños de Texto

En el Inspector, en cada TextMeshProUGUI:
- Font Size (aumenta o disminuye)
- Rect Transform (ancho/alto)

### Agregar Más Textos

Copia el script y agrega:
```csharp
[SerializeField] private TextMeshProUGUI customText;

private void UpdateCustomText(string text)
{
    if (customText != null)
        customText.text = text;
}
```

---

## ✅ Checklist Final

- ✅ Canvas creado
- ✅ TranscriptionText asignado
- ✅ StatusText asignado
- ✅ ModelInfoText asignado
- ✅ STTUIExample en STTManager
- ✅ Todas las referencias en Inspector completas
- ✅ Modelo descargado en Editor
- ✅ Play Mode activado

---

**¡Listo para usar!** 🎉 Ahora tu plugin muestra todo en tiempo real en la UI.
