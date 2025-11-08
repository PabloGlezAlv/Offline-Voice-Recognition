# Configuración Paso a Paso - Escena de Prueba

## Paso 1: Crear Nueva Escena

1. En Unity, abre el Project
2. Haz clic en `File > New Scene`
3. Elige "Basic" (escena vacía)
4. Guarda como: `Assets/Scenes/STTTestScene.unity`

## Paso 2: Crear el Manager GameObject

1. En la escena, haz clic derecho en la Hierarchy
2. Selecciona `Create Empty`
3. Llámalo: **"STTManager"**
4. En el Inspector, verifica que la posición sea (0, 0, 0)

## Paso 3: Agregar el Componente STTEngine

1. Con **STTManager** seleccionado
2. En el Inspector, haz clic en `Add Component`
3. Busca: **"STTEngine"**
4. Debería encontrar: `OfflineSpeechRecognition.Core.STTEngine`
5. Haz clic para agregarlo

Resultado: Verás el componente STTEngine en el Inspector con estos campos:
- **Selected Model**: Base
- **Selected Language**: English
- **Auto Download Models**: desmarcado

## Paso 4: Descargar el Modelo

⚠️ **IMPORTANTE**: Necesitas descargar un modelo antes de usar

### Opción A: Desde el Inspector (Recomendado)
1. Con STTManager seleccionado
2. En el componente STTEngine del Inspector
3. Deberías ver un botón **"Download Model"**
4. **Haz clic en él**
5. Espera a que termine (ve a la Console para ver el progreso)
6. Verás: `"Model Base downloaded successfully"`

### Opción B: Desde Code (Si no ves el botón)
```csharp
// En STTExample.cs
void Start()
{
    sttEngine.DownloadModel(WhisperModel.ModelSize.Base);
}
```

## Paso 5: Agregar Script de Ejemplo

1. Crea un nuevo script C# en: `Assets/Offline Speech Recognition/Examples/`
2. Llámalo: **"MySTTTest.cs"**
3. Copia este código:

```csharp
using UnityEngine;
using OfflineSpeechRecognition.Core;

public class MySTTTest : MonoBehaviour
{
    [SerializeField] private STTEngine sttEngine;
    private bool isRecording = false;

    void Start()
    {
        // Verificar que STTEngine esté asignado
        if (sttEngine == null)
        {
            Debug.LogError("STTEngine no está asignado!");
            return;
        }

        // Suscribirse a eventos
        sttEngine.OnTranscriptionComplete += OnTranscribeDone;
        sttEngine.OnError += OnError;

        Debug.Log("STT Test listo. Presiona R para grabar, S para transcribir");
    }

    void Update()
    {
        // R = Empezar a grabar
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isRecording)
            {
                sttEngine.StartMicrophoneCapture();
                isRecording = true;
                Debug.Log("🎤 Grabando... Habla ahora");
            }
        }

        // S = Detener y transcribir
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (isRecording)
            {
                sttEngine.TranscribeFromMicrophone();
                isRecording = false;
                Debug.Log("⏹️ Procesando audio...");
            }
        }

        // Mostrar estado
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log(sttEngine.GetDebugInfo());
        }
    }

    private void OnTranscribeDone(string text)
    {
        Debug.Log($"✅ Transcripción completada:\n{text}");
    }

    private void OnError(string error)
    {
        Debug.LogError($"❌ Error: {error}");
        isRecording = false;
    }
}
```

## Paso 6: Asignar el Script al GameObject

1. Con **STTManager** seleccionado
2. En el Inspector, haz clic en `Add Component`
3. Busca: **"MySTTTest"**
4. Selecciona tu script para agregarlo

## Paso 7: Conectar References

1. En el Inspector, busca el componente **"MySTTTest"**
2. Campo: **"Stt Engine"** (vacío)
3. Haz clic en el círculo pequeño a la derecha
4. Selecciona **STTManager** (el GameObject padre)
5. Esto asigna la referencia

## Paso 8: Verificar en Console

En la Console debería haber mensajes:
```
STT Test listo. Presiona R para grabar, S para transcribir
Model Base downloaded successfully
```

Si ves errores, verifica:
- ¿Está el modelo descargado?
- ¿Hay micrófono conectado?
- ¿Está en Silent Mode el PC?

## Paso 9: Probar el Plugin

**¡En Play Mode!**

1. Presiona **Play** en Unity (▶️)
2. En la escena, presiona **R** en tu teclado
3. Habla claro por el micrófono (5-10 segundos)
4. Presiona **S**
5. Mira la Console para el resultado

### Ejemplo de ejecución:
```
🎤 Grabando... Habla ahora
(esperas 5 segundos hablando)
⏹️ Procesando audio...
✅ Transcripción completada:
hello how are you today
```

## Controles Disponibles

| Tecla | Acción |
|-------|--------|
| **R** | Empezar grabación 🎤 |
| **S** | Detener y transcribir |
| **P** | Mostrar debug info |

## Solucionar Problemas

### "No microphone devices found"
```
✅ Solución: Verifica que tu micrófono esté conectado
✅ Solución: Windows > Settings > Privacy > Microphone (habilitado)
```

### "Model Base not downloaded"
```
✅ Solución: Haz clic en "Download Model" button en el Inspector
✅ Solución: Espera a que diga "downloaded successfully"
```

### "Empty transcription"
```
✅ Solución: Habla más fuerte y claro
✅ Solución: Verifica que el micrófono funcione
✅ Solución: Intenta con otro modelo (Small o Medium)
```

### "Cannot connect STTEngine reference"
```
✅ Solución: Asegúrate de asignar STTManager (el GameObject)
✅ Solución: No el componente, sino el GameObject completo
```

## Estructura Final de tu Escena

```
STTTestScene
└── STTManager (GameObject)
    ├── STTEngine (Componente)
    ├── AudioCapture (creado automáticamente)
    ├── AudioLoader (creado automáticamente)
    ├── ModelDownloader (creado automáticamente)
    └── MySTTTest (tu script)
```

## Inspector - Vista Final

Cuando todo esté bien configurado, verás en el Inspector:

```
STTManager
├── Transform
├── STTEngine
│   ├── Selected Model: Base
│   ├── Selected Language: English
│   ├── Auto Download Models: ☐
│   └── [Download Model] (botón)
└── MySTTTest
    └── Stt Engine: STTManager (asignado)
```

## ¡Listo! 🎉

Ya tienes tu plugin funcionando. Ahora puedes:
- Grabar y transcribir con micrófono
- Cambiar modelos y idiomas
- Agregar más funcionalidades

## Próximos Pasos

1. Mira **STTExample.cs** para más funcionalidades
2. Revisa **AdvancedSTTExample.cs** para comandos de voz
3. Lee **README.md** para la documentación completa

---

**¿Necesitas ayuda?** Revisa la Console para mensajes de error específicos.
