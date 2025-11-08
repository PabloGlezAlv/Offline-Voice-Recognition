using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OfflineInvoiceRecognition
{
    /// <summary>
    /// Downloads Whisper models from HuggingFace
    /// </summary>
    public class ModelDownloader
    {
        public event Action<float> OnProgress;
        public event Action<string> OnStatusChanged;
        public event Action<bool, string> OnDownloadComplete;

        private ModelConfig config;
        private bool isDownloading = false;

        public ModelDownloader(ModelConfig modelConfig)
        {
            config = modelConfig;
        }

        /// <summary>
        /// Check if model is already downloaded
        /// </summary>
        public bool IsModelDownloaded()
        {
            return config.IsDownloaded();
        }

        /// <summary>
        /// Start downloading the model
        /// </summary>
        public async Task<bool> DownloadModelAsync()
        {
            if (isDownloading)
            {
                Debug.LogWarning("Download already in progress");
                return false;
            }

            if (IsModelDownloaded())
            {
                OnStatusChanged?.Invoke("Model already downloaded");
                OnDownloadComplete?.Invoke(true, "Model already exists");
                return true;
            }

            isDownloading = true;
            OnStatusChanged?.Invoke($"Starting download of {config.modelName}...");

            try
            {
                // Create directory if it doesn't exist
                string modelDir = Path.GetDirectoryName(config.GetLocalEncoderPath());
                if (!Directory.Exists(modelDir))
                {
                    Directory.CreateDirectory(modelDir);
                }

                // Download encoder
                OnStatusChanged?.Invoke("Downloading encoder model...");
                bool encoderSuccess = await DownloadFileAsync(config.encoderUrl, config.GetLocalEncoderPath(), 0f, 0.5f);

                if (!encoderSuccess)
                {
                    OnDownloadComplete?.Invoke(false, "Failed to download encoder");
                    return false;
                }

                // Download decoder
                OnStatusChanged?.Invoke("Downloading decoder model...");
                bool decoderSuccess = await DownloadFileAsync(config.decoderUrl, config.GetLocalDecoderPath(), 0.5f, 1f);

                if (!decoderSuccess)
                {
                    OnDownloadComplete?.Invoke(false, "Failed to download decoder");
                    return false;
                }

                OnStatusChanged?.Invoke("Download complete!");
                OnDownloadComplete?.Invoke(true, "Model downloaded successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error downloading model: {e.Message}");
                OnDownloadComplete?.Invoke(false, e.Message);
                return false;
            }
            finally
            {
                isDownloading = false;
            }
        }

        /// <summary>
        /// Download a single file with progress tracking
        /// </summary>
        private async Task<bool> DownloadFileAsync(string url, string savePath, float progressStart, float progressEnd)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Start the request
                var operation = request.SendWebRequest();

                // Wait for completion while reporting progress
                while (!operation.isDone)
                {
                    float progress = Mathf.Lerp(progressStart, progressEnd, operation.progress);
                    OnProgress?.Invoke(progress);
                    await Task.Delay(100);
                }

                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error downloading {url}: {request.error}");
                    return false;
                }

                // Save file
                try
                {
                    File.WriteAllBytes(savePath, request.downloadHandler.data);
                    OnProgress?.Invoke(progressEnd);
                    Debug.Log($"Downloaded: {savePath} ({FormatBytes(request.downloadHandler.data.Length)})");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving file {savePath}: {e.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete downloaded model files
        /// </summary>
        public bool DeleteModel()
        {
            try
            {
                if (File.Exists(config.GetLocalEncoderPath()))
                    File.Delete(config.GetLocalEncoderPath());

                if (File.Exists(config.GetLocalDecoderPath()))
                    File.Delete(config.GetLocalDecoderPath());

                // Try to delete directory if empty
                string modelDir = Path.GetDirectoryName(config.GetLocalEncoderPath());
                if (Directory.Exists(modelDir) && Directory.GetFiles(modelDir).Length == 0)
                    Directory.Delete(modelDir);

                OnStatusChanged?.Invoke("Model deleted");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting model: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get formatted file size
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Get estimated download size
        /// </summary>
        public string GetEstimatedSize()
        {
            return FormatBytes(config.estimatedSize);
        }
    }
}
