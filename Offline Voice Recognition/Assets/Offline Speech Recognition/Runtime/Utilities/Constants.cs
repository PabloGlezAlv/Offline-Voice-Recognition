using System.Collections.Generic;

namespace OfflineSpeechRecognition.Utilities
{
    /// <summary>
    /// Global constants for the Offline Speech Recognition plugin
    /// </summary>
    public static class Constants
    {
        // Plugin Info
        public const string PLUGIN_NAME = "Offline Speech Recognition";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string PLUGIN_NAMESPACE = "OfflineSpeechRecognition";

        // Paths
        public const string MODELS_FOLDER_NAME = "OfflineSpeechRecognition/Models";
        public const string CACHE_FOLDER_NAME = "OfflineSpeechRecognition/Cache";

        // Hugging Face URLs - Using ggerganov's whisper.cpp GGML models
        // Official Whisper implementation with optimized GGML format
        // Reference: https://huggingface.co/ggerganov/whisper.cpp
        public const string HUGGINGFACE_BASE_URL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/";
        public const string WHISPER_MODEL_FILENAME = "ggml-{0}.bin";

        // Model sizes (in bytes) for download progress tracking
        public static readonly Dictionary<string, long> MODEL_SIZES = new Dictionary<string, long>()
        {
            { "tiny", 77700000 },       // ~77.7 MB (ggml-tiny.bin)
            { "small", 488000000 },     // ~488 MB (ggml-small.bin)
            { "medium", 1530000000 },   // ~1.53 GB (ggml-medium.bin)
            { "large-v3", 3100000000 }, // ~3.1 GB (ggml-large-v3.bin)
            { "large-v3-turbo", 1620000000 } // ~1.62 GB (ggml-large-v3-turbo.bin)
        };

        // Model checksums (SHA256) for integrity verification
        // Generate with: sha256sum ggml-*.bin
        public static readonly Dictionary<string, string> MODEL_CHECKSUMS = new Dictionary<string, string>()
        {
            { "tiny", "d3b3fea2f4cc0d4cba7b3732a7aad899c3e4478d" },         // ggml-tiny.bin
            { "small", "bc19f7fd388e432fa42ef6bda0f80981c7b36d45" },        // ggml-small.bin
            { "medium", "517c90c226313bec34158f0f6ca784068efc2b35" },       // ggml-medium.bin
            { "large-v3", "e4b97a5b64d1b1abf632723cb36d6693ee4c96cb" },     // ggml-large-v3.bin
            { "large-v3-turbo", "1bd4928b586e5cdc0f47cba5039d9c7ba4e8c9eb" } // ggml-large-v3-turbo.bin
        };

        // Whisper Configuration
        public const int SAMPLE_RATE = 16000;
        public const int CHANNELS = 1;
        public const int BUFFER_SIZE = 1024;

        // Microphone Configuration
        public const int MICROPHONE_RECORD_FREQUENCY = 16000;
        public const int MICROPHONE_RECORD_LENGTH = 30; // seconds max

        // Threading
        public const int THREAD_POOL_MAX_THREADS = 4;

        // Timeout values
        public const int DOWNLOAD_TIMEOUT_SECONDS = 3600; // 1 hour
        public const int INFERENCE_TIMEOUT_SECONDS = 300; // 5 minutes
    }
}
