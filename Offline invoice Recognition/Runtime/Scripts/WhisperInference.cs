using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Sentis;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Whisper model inference using Unity Sentis
    /// </summary>
    public class WhisperInference : IDisposable
    {
        private Model encoderModel;
        private Model decoderModel;
        private IWorker encoderWorker;
        private IWorker decoderWorker;
        private bool isInitialized = false;

        // Whisper constants
        private const int MEL_BINS = 80;
        private const int MAX_LENGTH = 3000; // Max mel spectrogram frames (~30s)
        private const int N_FFT = 400;
        private const int HOP_LENGTH = 160;
        private const int SAMPLE_RATE = 16000;

        // Special tokens
        private const int TOKEN_SOT = 50258;      // Start of transcript
        private const int TOKEN_EOT = 50257;      // End of transcript
        private const int TOKEN_NO_SPEECH = 50362;
        private const int TOKEN_NO_TIMESTAMPS = 50363;
        private const int MAX_TOKENS = 448;

        /// <summary>
        /// Initialize the Whisper model
        /// </summary>
        public bool Initialize(string encoderPath, string decoderPath)
        {
            try
            {
                if (!File.Exists(encoderPath))
                {
                    Debug.LogError($"Encoder model not found at: {encoderPath}");
                    return false;
                }

                if (!File.Exists(decoderPath))
                {
                    Debug.LogError($"Decoder model not found at: {decoderPath}");
                    return false;
                }

                Debug.Log("Loading encoder model...");
                encoderModel = ModelLoader.Load(encoderPath);

                Debug.Log("Loading decoder model...");
                decoderModel = ModelLoader.Load(decoderPath);

                // Create workers with GPU backend if available
                BackendType backend = GetBestBackend();
                Debug.Log($"Using backend: {backend}");

                encoderWorker = WorkerFactory.CreateWorker(backend, encoderModel);
                decoderWorker = WorkerFactory.CreateWorker(backend, decoderModel);

                isInitialized = true;
                Debug.Log("Whisper models initialized successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Whisper: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Transcribe audio to text
        /// </summary>
        public string Transcribe(float[] audioSamples)
        {
            if (!isInitialized)
            {
                Debug.LogError("Whisper not initialized. Call Initialize() first.");
                return string.Empty;
            }

            try
            {
                // 1. Convert audio to mel spectrogram
                var melSpectrogram = AudioToMelSpectrogram(audioSamples);

                // 2. Run encoder
                using var encoderInput = new TensorFloat(new TensorShape(1, MEL_BINS, melSpectrogram.GetLength(1)), melSpectrogram);
                encoderWorker.Execute(encoderInput);
                var encoderOutput = encoderWorker.PeekOutput() as TensorFloat;

                // 3. Run decoder with greedy search
                var tokens = DecodeGreedy(encoderOutput);

                // 4. Convert tokens to text
                string text = TokensToText(tokens);

                return text;
            }
            catch (Exception e)
            {
                Debug.LogError($"Transcription failed: {e.Message}\n{e.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert audio samples to mel spectrogram
        /// </summary>
        private float[,] AudioToMelSpectrogram(float[] audio)
        {
            // Calculate number of frames
            int numFrames = Math.Min((audio.Length - N_FFT) / HOP_LENGTH + 1, MAX_LENGTH);
            var mel = new float[MEL_BINS, numFrames];

            // Create mel filter banks (simplified version)
            var melFilters = CreateMelFilterBank();

            // Process each frame
            for (int i = 0; i < numFrames; i++)
            {
                int startSample = i * HOP_LENGTH;

                // Extract frame and apply Hanning window
                var frame = new float[N_FFT];
                for (int j = 0; j < N_FFT && startSample + j < audio.Length; j++)
                {
                    float window = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * j / (N_FFT - 1)));
                    frame[j] = audio[startSample + j] * window;
                }

                // Compute FFT (simplified - in production use a proper FFT library)
                var spectrum = SimplifiedFFT(frame);

                // Apply mel filters
                for (int m = 0; m < MEL_BINS; m++)
                {
                    float sum = 0f;
                    for (int k = 0; k < spectrum.Length; k++)
                    {
                        sum += spectrum[k] * melFilters[m, k];
                    }
                    // Convert to log scale
                    mel[m, i] = Mathf.Max(Mathf.Log10(sum + 1e-10f), -8f);
                }
            }

            return mel;
        }

        /// <summary>
        /// Simplified FFT (magnitude spectrum)
        /// Note: For production, use a proper FFT library like DSPLib
        /// </summary>
        private float[] SimplifiedFFT(float[] samples)
        {
            int n = samples.Length;
            float[] magnitude = new float[n / 2];

            for (int k = 0; k < n / 2; k++)
            {
                float real = 0f;
                float imag = 0f;

                for (int t = 0; t < n; t++)
                {
                    float angle = 2 * Mathf.PI * k * t / n;
                    real += samples[t] * Mathf.Cos(angle);
                    imag -= samples[t] * Mathf.Sin(angle);
                }

                magnitude[k] = Mathf.Sqrt(real * real + imag * imag);
            }

            return magnitude;
        }

        /// <summary>
        /// Create mel filter bank (simplified)
        /// </summary>
        private float[,] CreateMelFilterBank()
        {
            int nFreqs = N_FFT / 2;
            var filters = new float[MEL_BINS, nFreqs];

            // Simplified mel scale conversion
            float melMin = HzToMel(0);
            float melMax = HzToMel(SAMPLE_RATE / 2f);

            for (int i = 0; i < MEL_BINS; i++)
            {
                float melCenter = melMin + (melMax - melMin) * i / (MEL_BINS - 1);
                float hzCenter = MelToHz(melCenter);
                int binCenter = (int)(hzCenter * N_FFT / SAMPLE_RATE);

                // Triangular filter
                for (int j = 0; j < nFreqs; j++)
                {
                    float distance = Mathf.Abs(j - binCenter);
                    filters[i, j] = Mathf.Max(0, 1 - distance / 10f); // Simplified
                }
            }

            return filters;
        }

        private float HzToMel(float hz) => 2595f * Mathf.Log10(1 + hz / 700f);
        private float MelToHz(float mel) => 700f * (Mathf.Pow(10, mel / 2595f) - 1);

        /// <summary>
        /// Decode using greedy search
        /// </summary>
        private List<int> DecodeGreedy(TensorFloat encoderOutput)
        {
            var tokens = new List<int> { TOKEN_SOT, TOKEN_NO_TIMESTAMPS };

            for (int i = 0; i < MAX_TOKENS; i++)
            {
                // Prepare decoder input
                var tokensTensor = new TensorInt(new TensorShape(1, tokens.Count), tokens.ToArray());

                // Run decoder
                using var inputs = new Dictionary<string, Tensor>
                {
                    { "encoder_hidden_states", encoderOutput },
                    { "input_ids", tokensTensor }
                };

                // Note: This is simplified - actual Whisper decoder needs more inputs
                decoderWorker.Execute(tokensTensor);
                var logits = decoderWorker.PeekOutput() as TensorFloat;

                // Get most likely token
                int nextToken = GetArgMax(logits);

                if (nextToken == TOKEN_EOT || nextToken == TOKEN_NO_SPEECH)
                    break;

                tokens.Add(nextToken);

                tokensTensor.Dispose();
            }

            return tokens;
        }

        /// <summary>
        /// Get argmax from logits
        /// </summary>
        private int GetArgMax(TensorFloat logits)
        {
            logits.MakeReadable();
            int maxIndex = 0;
            float maxValue = float.MinValue;

            for (int i = 0; i < logits.shape[logits.shape.rank - 1]; i++)
            {
                float value = logits[i];
                if (value > maxValue)
                {
                    maxValue = value;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Convert tokens to text (simplified - needs proper tokenizer)
        /// </summary>
        private string TokensToText(List<int> tokens)
        {
            // Note: This is a placeholder. In production, you need a proper Whisper tokenizer
            // You can use tiktoken or download Whisper's vocabulary file

            var text = new System.Text.StringBuilder();

            foreach (int token in tokens)
            {
                // Skip special tokens
                if (token == TOKEN_SOT || token == TOKEN_EOT ||
                    token == TOKEN_NO_SPEECH || token == TOKEN_NO_TIMESTAMPS)
                    continue;

                // This is a simplified mapping - you need actual Whisper vocabulary
                // For now, return a placeholder
                text.Append($"[{token}]");
            }

            // TODO: Implement proper detokenization using Whisper's vocabulary
            // For now, return a message indicating this needs implementation
            string result = text.ToString();

            if (string.IsNullOrEmpty(result))
                return "[Transcription completed - proper tokenizer needed for text output]";

            return result;
        }

        /// <summary>
        /// Get best available backend
        /// </summary>
        private BackendType GetBestBackend()
        {
            // Try to use GPU if available, fallback to CPU
            if (SystemInfo.supportsComputeShaders)
                return BackendType.GPUCompute;

            return BackendType.CPU;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            encoderWorker?.Dispose();
            decoderWorker?.Dispose();
            isInitialized = false;
            Debug.Log("Whisper resources disposed");
        }
    }
}
