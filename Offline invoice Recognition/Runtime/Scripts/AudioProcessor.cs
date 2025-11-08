using UnityEngine;
using System;
using System.Linq;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Processes audio data for Whisper model input
    /// Whisper requires: 16kHz sample rate, mono, normalized float array
    /// </summary>
    public static class AudioProcessor
    {
        private const int WHISPER_SAMPLE_RATE = 16000;
        private const int MAX_AUDIO_LENGTH_SECONDS = 30; // Whisper's max length
        private const int MAX_SAMPLES = WHISPER_SAMPLE_RATE * MAX_AUDIO_LENGTH_SECONDS;

        /// <summary>
        /// Convert AudioClip to float array suitable for Whisper
        /// </summary>
        public static float[] ProcessAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("AudioClip is null");
                return new float[0];
            }

            // Get audio data
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert to mono if stereo
            if (clip.channels > 1)
            {
                samples = ConvertToMono(samples, clip.channels);
            }

            // Resample to 16kHz if needed
            if (clip.frequency != WHISPER_SAMPLE_RATE)
            {
                samples = Resample(samples, clip.frequency, WHISPER_SAMPLE_RATE);
            }

            // Trim or pad to max length
            if (samples.Length > MAX_SAMPLES)
            {
                Debug.LogWarning($"Audio is longer than {MAX_AUDIO_LENGTH_SECONDS}s, trimming...");
                Array.Resize(ref samples, MAX_SAMPLES);
            }
            else if (samples.Length < WHISPER_SAMPLE_RATE) // Pad if too short (less than 1 second)
            {
                int targetLength = WHISPER_SAMPLE_RATE;
                Array.Resize(ref samples, targetLength);
            }

            // Normalize audio
            samples = Normalize(samples);

            return samples;
        }

        /// <summary>
        /// Convert multi-channel audio to mono by averaging channels
        /// </summary>
        private static float[] ConvertToMono(float[] samples, int channels)
        {
            int monoLength = samples.Length / channels;
            float[] mono = new float[monoLength];

            for (int i = 0; i < monoLength; i++)
            {
                float sum = 0f;
                for (int ch = 0; ch < channels; ch++)
                {
                    sum += samples[i * channels + ch];
                }
                mono[i] = sum / channels;
            }

            return mono;
        }

        /// <summary>
        /// Simple linear interpolation resampling
        /// </summary>
        private static float[] Resample(float[] samples, int sourceRate, int targetRate)
        {
            if (sourceRate == targetRate)
                return samples;

            float ratio = (float)sourceRate / targetRate;
            int targetLength = (int)(samples.Length / ratio);
            float[] resampled = new float[targetLength];

            for (int i = 0; i < targetLength; i++)
            {
                float sourceIndex = i * ratio;
                int index1 = (int)sourceIndex;
                int index2 = Math.Min(index1 + 1, samples.Length - 1);
                float fraction = sourceIndex - index1;

                resampled[i] = samples[index1] * (1 - fraction) + samples[index2] * fraction;
            }

            return resampled;
        }

        /// <summary>
        /// Normalize audio to [-1, 1] range
        /// </summary>
        private static float[] Normalize(float[] samples)
        {
            if (samples.Length == 0)
                return samples;

            float max = samples.Max(Math.Abs);
            if (max > 0f)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] /= max;
                }
            }

            return samples;
        }

        /// <summary>
        /// Record audio from microphone
        /// </summary>
        public static AudioClip StartRecording(int lengthSec = 30, string deviceName = null)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected!");
                return null;
            }

            string device = deviceName ?? Microphone.devices[0];
            return Microphone.Start(device, false, lengthSec, WHISPER_SAMPLE_RATE);
        }

        /// <summary>
        /// Stop recording and trim silence
        /// </summary>
        public static AudioClip StopRecording(AudioClip recordingClip, string deviceName = null)
        {
            if (recordingClip == null)
                return null;

            string device = deviceName ?? Microphone.devices[0];
            int position = Microphone.GetPosition(device);
            Microphone.End(device);

            // Create trimmed clip
            float[] samples = new float[position * recordingClip.channels];
            recordingClip.GetData(samples, 0);

            AudioClip trimmedClip = AudioClip.Create("Recording", position, recordingClip.channels,
                recordingClip.frequency, false);
            trimmedClip.SetData(samples, 0);

            return trimmedClip;
        }

        /// <summary>
        /// Convert float array back to AudioClip (for debugging)
        /// </summary>
        public static AudioClip FloatArrayToAudioClip(float[] samples, int frequency = 16000)
        {
            AudioClip clip = AudioClip.Create("ProcessedAudio", samples.Length, 1, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
