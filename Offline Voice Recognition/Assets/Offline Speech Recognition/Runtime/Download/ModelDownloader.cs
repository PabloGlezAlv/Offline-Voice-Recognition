using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using OfflineSpeechRecognition.Core;

namespace OfflineSpeechRecognition.Download
{
    /// <summary>
    /// Handles downloading Whisper models from Hugging Face
    /// </summary>
    public class ModelDownloader : MonoBehaviour
    {
        private static HttpClient _httpClient;
        private bool _isDownloading;
        private HttpResponseMessage _currentResponse;
        private double _downloadStartTime = -1;

        // Download optimization settings
        private const int OPTIMAL_BUFFER_SIZE = 262144; // 256 KB buffer for faster I/O
        private const int YIELD_INTERVAL = 10; // Yield every N chunk reads to keep Unity responsive

        /// <summary>
        /// Callback for download progress
        /// </summary>
        public event Action<float> OnDownloadProgress;

        /// <summary>
        /// Callback for download completion
        /// </summary>
        public event Action<bool> OnDownloadComplete;

        /// <summary>
        /// Callback for errors
        /// </summary>
        public event Action<string> OnDownloadError;

        private void OnEnable()
        {
            EnsureHttpClient();
        }

        /// <summary>
        /// Ensure HttpClient is initialized (lazy initialization fallback)
        /// </summary>
        private static void EnsureHttpClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
                _httpClient.Timeout = TimeSpan.FromSeconds(Utilities.Constants.DOWNLOAD_TIMEOUT_SECONDS);
                Debug.Log("[ModelDownloader.EnsureHttpClient] HttpClient initialized");
            }
        }

        private void OnDisable()
        {
            if (_isDownloading)
            {
                CancelDownload();
            }
        }

        /// <summary>
        /// Start downloading a model (async, non-blocking)
        /// </summary>
        public void StartDownload(WhisperModel model)
        {
            Debug.Log($"[ModelDownloader.StartDownload] Called for {model.GetSizeString()}, _isDownloading={_isDownloading}");

            if (_isDownloading)
            {
                // Check if the download seems to be stuck (started but no recent activity)
                double elapsedTime = Time.realtimeSinceStartup - _downloadStartTime;
                if (_downloadStartTime < 0 || elapsedTime > 30.0)
                {
                    Debug.LogWarning($"[ModelDownloader.StartDownload] Download appears stuck (elapsed: {elapsedTime:F2}s). Resetting.");
                    _isDownloading = false;
                    _currentResponse?.Dispose();
                    _currentResponse = null;
                    _downloadStartTime = -1;
                }
                else
                {
                    string errorMsg = "A download is already in progress";
                    Debug.LogWarning($"[ModelDownloader.StartDownload] {errorMsg}");
                    OnDownloadError?.Invoke(errorMsg);
                    return;
                }
            }

            // Ensure HttpClient is initialized
            EnsureHttpClient();

            Debug.Log($"[ModelDownloader.StartDownload] Starting async download for {model.GetSizeString()}");
            _downloadStartTime = Time.realtimeSinceStartup;

            // Start async download without blocking
            _ = DownloadModelAsync(model);
        }

        /// <summary>
        /// Download model asynchronously without coroutines (MUCH FASTER)
        /// </summary>
        private async Task DownloadModelAsync(WhisperModel model)
        {
            _isDownloading = true;
            string url = model.GetDownloadUrl();
            string modelDir = model.GetModelDirectory();
            bool success = false;
            string errorMessage = "";

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(modelDir))
                {
                    Directory.CreateDirectory(modelDir);
                }

                // Download the model file
                success = await DownloadFileAsync(url, model.ModelPath, model);

                if (_isDownloading) // Check if download wasn't cancelled
                {
                    if (success)
                    {
                        model.RefreshDownloadStatus();
                        OnDownloadComplete?.Invoke(true);
                        Debug.Log($"Model {model.GetSizeString()} downloaded successfully");
                    }
                    else
                    {
                        Debug.LogError($"Download failed: {errorMessage}");
                        OnDownloadError?.Invoke(errorMessage);
                        OnDownloadComplete?.Invoke(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Download error: {ex.Message}");
                OnDownloadError?.Invoke(ex.Message);
                OnDownloadComplete?.Invoke(false);
            }
            finally
            {
                _isDownloading = false;
                _downloadStartTime = -1;
            }
        }

        /// <summary>
        /// Download a file from URL
        /// </summary>
        private IEnumerator DownloadFile(string url, string filePath, WhisperModel model, System.Action<bool, string> onComplete)
        {
            Debug.Log($"Downloading from: {url}");

            // Double-check HttpClient is initialized (fail-safe)
            EnsureHttpClient();
            if (_httpClient == null)
            {
                string errorMsg = "Failed to initialize HttpClient";
                onComplete?.Invoke(false, errorMsg);
                Debug.LogError($"[ModelDownloader.DownloadFile] {errorMsg}");
                yield break;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var task = _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                onComplete?.Invoke(false, $"Download failed: {task.Exception?.Message}");
                yield break;
            }

            _currentResponse = task.Result;

            if (!_currentResponse.IsSuccessStatusCode)
            {
                onComplete?.Invoke(false, $"HTTP {(int)_currentResponse.StatusCode}: {_currentResponse.ReasonPhrase}");
                _currentResponse?.Dispose();
                _currentResponse = null;
                yield break;
            }

            long totalBytes = _currentResponse.Content.Headers.ContentLength ?? -1L;
            long receivedBytes = 0L;
            // Use larger buffer (256KB) for faster downloading
            var buffer = new byte[OPTIMAL_BUFFER_SIZE];

            var contentStreamTask = _currentResponse.Content.ReadAsStreamAsync();
            while (!contentStreamTask.IsCompleted)
            {
                yield return null;
            }

            // Process download outside of try-catch
            yield return ProcessDownloadStream(contentStreamTask.Result, filePath, buffer, totalBytes, (received) =>
            {
                receivedBytes = received;
            });

            _currentResponse?.Dispose();
            _currentResponse = null;

            if (receivedBytes == 0)
            {
                onComplete?.Invoke(false, "No data received from server");
                CleanupIncompleteFile(filePath);
            }
            else
            {
                Debug.Log($"Download complete: {receivedBytes} bytes written");

                // Validate downloaded file integrity
                if (ValidateModelIntegrity(model))
                {
                    Debug.Log($"[ModelDownloader] Model {model.GetSizeString()} passed integrity check");
                    onComplete?.Invoke(true, "");
                }
                else
                {
                    Debug.LogError($"[ModelDownloader] Model {model.GetSizeString()} failed integrity check - cleaning up");
                    CleanupIncompleteFile(filePath);
                    onComplete?.Invoke(false, "Model file integrity check failed");
                }
            }
        }

        /// <summary>
        /// Process the download stream with optimized buffer for faster downloads
        /// </summary>
        private IEnumerator ProcessDownloadStream(System.IO.Stream contentStream, string filePath, byte[] buffer, long totalBytes, System.Action<long> onBytesReceived)
        {
            long receivedBytes = 0L;
            bool error = false;
            int updateCount = 0;
            int yieldCounter = 0;

            if (contentStream == null)
            {
                yield break;
            }

            Debug.Log($"[ModelDownloader.ProcessDownloadStream] Starting download, total bytes: {totalBytes}");

            FileStream fileStream = null;
            try
            {
                // Use larger buffer for FileStream (64KB) for faster disk writes
                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating file: {ex.Message}");
                onBytesReceived?.Invoke(0);
                yield break;
            }

            while (!error && _isDownloading)
            {
                int bytesRead = 0;
                try
                {
                    // Read larger chunks (256KB) for better throughput
                    bytesRead = contentStream.Read(buffer, 0, Math.Min(buffer.Length, OPTIMAL_BUFFER_SIZE));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error reading stream: {ex.Message}");
                    error = true;
                    break;
                }

                if (bytesRead <= 0)
                {
                    Debug.Log($"[ModelDownloader.ProcessDownloadStream] Stream ended, total received: {receivedBytes} bytes");
                    break;
                }

                try
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error writing file: {ex.Message}");
                    error = true;
                    break;
                }

                receivedBytes += bytesRead;

                // Only update progress and yield every N iterations to reduce overhead
                yieldCounter++;
                if (yieldCounter >= YIELD_INTERVAL)
                {
                    yieldCounter = 0;

                    if (totalBytes > 0)
                    {
                        float progress = (float)receivedBytes / totalBytes;
                        if (updateCount % 10 == 0) // Log every 10 progress updates
                        {
                            Debug.Log($"[ModelDownloader] Progress: {(progress * 100):F1}% ({receivedBytes}/{totalBytes} bytes)");
                        }
                        OnDownloadProgress?.Invoke(progress);
                        updateCount++;
                    }

                    yield return null; // Only yield every YIELD_INTERVAL reads
                }
            }

            try
            {
                fileStream?.Dispose();
            }
            catch { }

            if (error)
            {
                CleanupIncompleteFile(filePath);
                receivedBytes = 0;
            }

            onBytesReceived?.Invoke(receivedBytes);
        }

        /// <summary>
        /// Clean up incomplete file
        /// </summary>
        private void CleanupIncompleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Cancel the current download
        /// </summary>
        public void CancelDownload()
        {
            _isDownloading = false;
            _currentResponse?.Dispose();
            _currentResponse = null;
            Debug.Log("Download cancelled");
        }

        /// <summary>
        /// Check if download is in progress
        /// </summary>
        public bool IsDownloading => _isDownloading;

        /// <summary>
        /// Calculate SHA1 checksum of a file
        /// </summary>
        private string CalculateFileSHA1(string filePath)
        {
            try
            {
                using (var sha1 = SHA1.Create())
                {
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        byte[] hashBytes = sha1.ComputeHash(fileStream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModelDownloader.CalculateFileSHA1] Error calculating checksum: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validate model file integrity using SHA1 checksum (fast version - only if needed)
        /// </summary>
        private bool ValidateModelIntegrity(WhisperModel model)
        {
            string expectedChecksum = model.GetExpectedChecksum();
            if (string.IsNullOrEmpty(expectedChecksum))
            {
                Debug.LogWarning($"[ModelDownloader] No checksum available for {model.GetSizeString()}");
                return true; // Skip validation if no checksum is available
            }

            if (!File.Exists(model.ModelPath))
            {
                Debug.LogError($"[ModelDownloader] Model file not found: {model.ModelPath}");
                return false;
            }

            Debug.Log($"[ModelDownloader] Validating {model.GetSizeString()} checksum...");
            string actualChecksum = CalculateFileSHA1(model.ModelPath);

            if (actualChecksum == null)
            {
                Debug.LogError($"[ModelDownloader] Failed to calculate checksum for {model.GetSizeString()}");
                return false;
            }

            bool isValid = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
            if (isValid)
            {
                Debug.Log($"[ModelDownloader] ✓ Checksum valid for {model.GetSizeString()}");
            }
            else
            {
                Debug.LogError($"[ModelDownloader] ✗ Checksum mismatch for {model.GetSizeString()}");
                Debug.LogError($"  Expected: {expectedChecksum}");
                Debug.LogError($"  Actual:   {actualChecksum}");
            }

            return isValid;
        }

        /// <summary>
        /// Calculate SHA1 checksum while reading file (more efficient for large files)
        /// </summary>
        private string CalculateFileSHA1Optimized(string filePath)
        {
            try
            {
                using (var sha1 = SHA1.Create())
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, OPTIMAL_BUFFER_SIZE))
                    {
                        byte[] buffer = new byte[OPTIMAL_BUFFER_SIZE];
                        int bytesRead;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sha1.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        }
                        sha1.TransformFinalBlock(buffer, 0, 0);
                        byte[] hashBytes = sha1.Hash;
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModelDownloader.CalculateFileSHA1Optimized] Error calculating checksum: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public static void Cleanup()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}
