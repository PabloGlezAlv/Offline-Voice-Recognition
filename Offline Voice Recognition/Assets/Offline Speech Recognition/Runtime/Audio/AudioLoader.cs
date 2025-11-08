using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace OfflineSpeechRecognition.Audio
{
    /// <summary>
    /// Loads audio files from disk (WAV, MP3, OGG)
    /// </summary>
    public class AudioLoader : MonoBehaviour
    {
        /// <summary>
        /// Callback when audio is loaded
        /// </summary>
        public event Action<AudioClip> OnAudioLoaded;

        /// <summary>
        /// Callback for loading errors
        /// </summary>
        public event Action<string> OnLoadError;

        /// <summary>
        /// Load audio file from path
        /// </summary>
        public void LoadAudioFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                OnLoadError?.Invoke($"File not found: {filePath}");
                return;
            }

            StartCoroutine(LoadAudioCoroutine(filePath));
        }

        /// <summary>
        /// Load audio coroutine
        /// </summary>
        private IEnumerator LoadAudioCoroutine(string filePath)
        {
            string uri = new System.Uri(filePath).AbsoluteUri;
            AudioType audioType = GetAudioTypeFromPath(filePath);

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                    {
                        OnAudioLoaded?.Invoke(clip);
                        Debug.Log($"Audio loaded successfully: {filePath}");
                    }
                    else
                    {
                        OnLoadError?.Invoke("Failed to extract audio clip from file");
                    }
                }
                else
                {
                    OnLoadError?.Invoke($"Failed to load audio: {request.error}");
                }
            }
        }

        /// <summary>
        /// Load audio from raw PCM data
        /// </summary>
        public AudioClip CreateAudioClipFromSamples(float[] samples, int channels = 1, int sampleRate = 16000)
        {
            if (samples == null || samples.Length == 0)
            {
                return null;
            }

            try
            {
                AudioClip clip = AudioClip.Create(
                    "GeneratedClip",
                    samples.Length / channels,
                    channels,
                    sampleRate,
                    false
                );

                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating audio clip from samples: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert audio file to float array
        /// </summary>
        public float[] LoadAudioFileToFloatArray(string filePath)
        {
            if (!File.Exists(filePath))
            {
                OnLoadError?.Invoke($"File not found: {filePath}");
                return null;
            }

            try
            {
                // For synchronous loading, we use a simpler approach
                // In production, you might want to use a dedicated audio library

                // Check file type
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".wav")
                {
                    return LoadWAVFile(filePath);
                }
                else
                {
                    // For MP3, OGG, etc., recommend using async loading with UnityWebRequest
                    Debug.LogError($"For {extension} files, use LoadAudioFromFile (async) instead");
                    return null;
                }
            }
            catch (Exception ex)
            {
                OnLoadError?.Invoke($"Error loading audio file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load WAV file directly (synchronous)
        /// </summary>
        private float[] LoadWAVFile(string filePath)
        {
            try
            {
                byte[] wavFile = File.ReadAllBytes(filePath);
                return WAVToFloatArray(wavFile);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading WAV file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert WAV file bytes to float array
        /// </summary>
        private float[] WAVToFloatArray(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44)
            {
                throw new ArgumentException("Invalid WAV data");
            }

            // WAV file header parsing
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int channels = BitConverter.ToInt16(wavData, 22);
            int subChunk2Size = BitConverter.ToInt32(wavData, 40);

            int sampleCount = subChunk2Size / 2; // Assuming 16-bit audio
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                int pos = 44 + (i * 2);
                short sample = BitConverter.ToInt16(wavData, pos);
                samples[i] = sample / 32768f;
            }

            return samples;
        }

        /// <summary>
        /// Get audio type from file extension
        /// </summary>
        private AudioType GetAudioTypeFromPath(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".wav" => AudioType.WAV,
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.UNKNOWN
            };
        }

        /// <summary>
        /// Check if file is a supported audio format
        /// </summary>
        public static bool IsSupportedAudioFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".wav" || extension == ".mp3" || extension == ".ogg";
        }

        /// <summary>
        /// Get audio file info
        /// </summary>
        public static bool TryGetAudioFileInfo(string filePath, out int sampleRate, out int channels, out float duration)
        {
            sampleRate = 0;
            channels = 0;
            duration = 0f;

            if (!File.Exists(filePath))
                return false;

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".wav")
                {
                    byte[] wavData = File.ReadAllBytes(filePath);
                    if (wavData.Length < 44)
                        return false;

                    sampleRate = BitConverter.ToInt32(wavData, 24);
                    channels = BitConverter.ToInt16(wavData, 22);
                    int subChunk2Size = BitConverter.ToInt32(wavData, 40);

                    int sampleCount = subChunk2Size / 2;
                    duration = (float)sampleCount / sampleRate;

                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Clean up audio resources
        /// </summary>
        public static void UnloadAudioClip(AudioClip clip)
        {
            if (clip != null)
            {
                Resources.UnloadAsset(clip);
            }
        }
    }
}
