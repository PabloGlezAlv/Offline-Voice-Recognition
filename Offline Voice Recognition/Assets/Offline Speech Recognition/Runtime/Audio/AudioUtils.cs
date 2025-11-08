using System;
using UnityEngine;

namespace OfflineSpeechRecognition.Audio
{
    /// <summary>
    /// Audio processing utilities for format conversion and normalization
    /// </summary>
    public static class AudioUtils
    {
        /// <summary>
        /// Convert AudioClip to float array
        /// </summary>
        public static float[] AudioClipToFloatArray(AudioClip clip)
        {
            if (clip == null)
                return null;

            int sampleCount = clip.samples;
            float[] samples = new float[sampleCount * clip.channels];

            try
            {
                clip.GetData(samples, 0);
                return samples;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting AudioClip to float array: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resample audio from source sample rate to target sample rate
        /// </summary>
        public static float[] ResampleAudio(float[] inputSamples, int inputSampleRate, int outputSampleRate)
        {
            if (inputSamples == null || inputSamples.Length == 0)
                return null;

            if (inputSampleRate == outputSampleRate)
                return inputSamples;

            float ratio = (float)outputSampleRate / inputSampleRate;
            int outputLength = (int)(inputSamples.Length * ratio);
            float[] outputSamples = new float[outputLength];

            try
            {
                for (int i = 0; i < outputLength; i++)
                {
                    float pos = i / ratio;
                    int index = (int)pos;
                    float frac = pos - index;

                    if (index + 1 < inputSamples.Length)
                    {
                        // Linear interpolation
                        outputSamples[i] = inputSamples[index] * (1 - frac) + inputSamples[index + 1] * frac;
                    }
                    else if (index < inputSamples.Length)
                    {
                        outputSamples[i] = inputSamples[index];
                    }
                }

                return outputSamples;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resampling audio: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert stereo to mono by averaging channels
        /// </summary>
        public static float[] StereoToMono(float[] stereoSamples)
        {
            if (stereoSamples == null || stereoSamples.Length == 0)
                return null;

            int monoLength = stereoSamples.Length / 2;
            float[] monoSamples = new float[monoLength];

            try
            {
                for (int i = 0; i < monoLength; i++)
                {
                    monoSamples[i] = (stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2.0f;
                }

                return monoSamples;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting stereo to mono: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Normalize audio samples to [-1, 1] range
        /// </summary>
        public static float[] NormalizeAudio(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return null;

            float maxAbs = 0f;

            // Find max absolute value
            foreach (float sample in samples)
            {
                float abs = Mathf.Abs(sample);
                if (abs > maxAbs)
                    maxAbs = abs;
            }

            // Avoid division by zero
            if (maxAbs == 0f || maxAbs == 1f)
                return samples;

            float[] normalized = new float[samples.Length];

            try
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    normalized[i] = samples[i] / maxAbs;
                }

                return normalized;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error normalizing audio: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Apply silence removal by trimming leading and trailing silence
        /// </summary>
        public static float[] TrimSilence(float[] samples, float threshold = 0.01f)
        {
            if (samples == null || samples.Length == 0)
                return samples;

            int start = 0;
            int end = samples.Length - 1;

            // Find start of non-silence
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > threshold)
                {
                    start = i;
                    break;
                }
            }

            // Find end of non-silence
            for (int i = samples.Length - 1; i >= 0; i--)
            {
                if (Mathf.Abs(samples[i]) > threshold)
                {
                    end = i;
                    break;
                }
            }

            if (start >= end)
                return new float[0];

            int length = end - start + 1;
            float[] trimmed = new float[length];

            try
            {
                System.Array.Copy(samples, start, trimmed, 0, length);
                return trimmed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error trimming silence: {ex.Message}");
                return samples;
            }
        }

        /// <summary>
        /// Apply gain (amplification) to audio samples
        /// </summary>
        public static float[] ApplyGain(float[] samples, float gain)
        {
            if (samples == null || samples.Length == 0)
                return null;

            if (gain == 1f)
                return samples;

            float[] result = new float[samples.Length];

            try
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    result[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying gain: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert float samples to PCM16 byte array
        /// </summary>
        public static byte[] FloatToPCM16(float[] floatSamples)
        {
            if (floatSamples == null || floatSamples.Length == 0)
                return null;

            byte[] result = new byte[floatSamples.Length * 2];

            try
            {
                for (int i = 0; i < floatSamples.Length; i++)
                {
                    short sample = (short)(floatSamples[i] * 32767);
                    result[i * 2] = (byte)(sample & 0xFF);
                    result[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting float to PCM16: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert PCM16 byte array to float samples
        /// </summary>
        public static float[] PCM16ToFloat(byte[] pcm16Data)
        {
            if (pcm16Data == null || pcm16Data.Length == 0)
                return null;

            float[] result = new float[pcm16Data.Length / 2];

            try
            {
                for (int i = 0; i < result.Length; i++)
                {
                    short sample = (short)((pcm16Data[i * 2 + 1] << 8) | pcm16Data[i * 2]);
                    result[i] = sample / 32768.0f;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting PCM16 to float: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate RMS (Root Mean Square) of audio samples
        /// </summary>
        public static float CalculateRMS(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;

            float sum = 0f;

            try
            {
                foreach (float sample in samples)
                {
                    sum += sample * sample;
                }

                return Mathf.Sqrt(sum / samples.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating RMS: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Calculate MFCC (Mel-Frequency Cepstral Coefficients) - simplified version
        /// This is a basic implementation for audio feature extraction
        /// </summary>
        public static float[] CalculateMFCC(float[] samples, int sampleRate, int numCoefficients = 13)
        {
            if (samples == null || samples.Length == 0)
                return null;

            try
            {
                // This is a placeholder for MFCC calculation
                // In a real implementation, you would need:
                // 1. Pre-emphasis filter
                // 2. Framing and windowing
                // 3. Power spectrum
                // 4. Mel-scale filtering
                // 5. Log compression
                // 6. DCT (Discrete Cosine Transform)

                float[] mfcc = new float[numCoefficients];
                for (int i = 0; i < numCoefficients; i++)
                {
                    mfcc[i] = 0f; // Placeholder
                }

                return mfcc;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating MFCC: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if audio is valid for processing
        /// </summary>
        public static bool IsValidAudio(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return false;

            // Check if all samples are NaN or infinite
            int validSamples = 0;
            foreach (float sample in samples)
            {
                if (!float.IsNaN(sample) && !float.IsInfinity(sample))
                    validSamples++;
            }

            return validSamples > 0;
        }

        /// <summary>
        /// Get audio duration in seconds
        /// </summary>
        public static float GetDuration(float[] samples, int sampleRate)
        {
            if (samples == null || samples.Length == 0 || sampleRate == 0)
                return 0f;

            return samples.Length / (float)sampleRate;
        }
    }
}
