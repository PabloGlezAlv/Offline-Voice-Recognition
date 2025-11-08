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

        // Hugging Face URLs
        public const string HUGGINGFACE_BASE_URL = "https://huggingface.co/openai/whisper-{0}/resolve/main/";
        public const string WHISPER_MODEL_FILENAME = "onnx/model.onnx";

        // Model sizes
        public static readonly Dictionary<string, long> MODEL_SIZES = new Dictionary<string, long>()
        {
            { "tiny", 140000000 },      // ~140 MB
            { "base", 290000000 },      // ~290 MB
            { "small", 770000000 },     // ~770 MB
            { "medium", 1550000000 },   // ~1.5 GB
            { "large", 3100000000 }     // ~3.1 GB
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
