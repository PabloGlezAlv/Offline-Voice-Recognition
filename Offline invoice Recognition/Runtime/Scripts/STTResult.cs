using System;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Result of a speech-to-text transcription
    /// </summary>
    [Serializable]
    public class STTResult
    {
        /// <summary>
        /// Transcribed text
        /// </summary>
        public string text;

        /// <summary>
        /// Detected language (ISO 639-1 code, e.g., "en", "es", "fr")
        /// </summary>
        public string language;

        /// <summary>
        /// Confidence score (0-1)
        /// </summary>
        public float confidence;

        /// <summary>
        /// Processing time in seconds
        /// </summary>
        public float processingTime;

        /// <summary>
        /// Whether the transcription was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string error;

        public STTResult()
        {
            text = string.Empty;
            language = "unknown";
            confidence = 0f;
            processingTime = 0f;
            success = false;
            error = string.Empty;
        }

        public static STTResult Success(string transcription, string lang = "unknown", float conf = 1f, float time = 0f)
        {
            return new STTResult
            {
                text = transcription,
                language = lang,
                confidence = conf,
                processingTime = time,
                success = true
            };
        }

        public static STTResult Failure(string errorMessage)
        {
            return new STTResult
            {
                success = false,
                error = errorMessage
            };
        }

        public override string ToString()
        {
            if (success)
                return $"[{language}] {text} (confidence: {confidence:F2}, time: {processingTime:F2}s)";
            else
                return $"Error: {error}";
        }
    }
}
