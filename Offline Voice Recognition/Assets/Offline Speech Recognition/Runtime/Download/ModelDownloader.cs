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
        private Task _currentDownloadTask;

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
            if (_isDownloading)
            {
                // Check if the download seems to be stuck (started but no recent activity)
                double elapsedTime = Time.realtimeSinceStartup - _downloadStartTime;
                if (_downloadStartTime < 0 || elapsedTime > 30.0)
                {
                    Debug.LogWarning($"Download appears stuck. Resetting.");
                    _isDownloading = false;
                    _currentResponse?.Dispose();
                    _currentResponse = null;
                    _downloadStartTime = -1;
                }
                else
                {
                    string errorMsg = "A download is already in progress";
                    OnDownloadError?.Invoke(errorMsg);
                    return;
                }
            }

            // Ensure HttpClient is initialized
            EnsureHttpClient();

            Debug.Log($"Downloading {model.GetSizeString()} model...");
            _downloadStartTime = Time.realtimeSinceStartup;

            // Start async download without blocking and track the task
            _currentDownloadTask = DownloadModelAsync(model);
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
                    }
                    else
                    {
                        OnDownloadError?.Invoke(errorMessage);
                        OnDownloadComplete?.Invoke(false);
                    }
                }
                else
                {
                    // Download was cancelled - clean up the incomplete file
                    CleanupIncompleteFile(model.ModelPath);
                    Debug.Log($"Cleaned up incomplete file for cancelled download");
                }
            }
            catch (Exception ex)
            {
                OnDownloadError?.Invoke(ex.Message);
                OnDownloadComplete?.Invoke(false);
                CleanupIncompleteFile(model.ModelPath);
            }
            finally
            {
                _isDownloading = false;
                _downloadStartTime = -1;
            }
        }

        /// <summary>
        /// Download a file from URL asynchronously (no coroutines = MUCH FASTER)
        /// </summary>
        private async Task<bool> DownloadFileAsync(string url, string filePath, WhisperModel model)
        {
            try
            {
                // Double-check HttpClient is initialized (fail-safe)
                EnsureHttpClient();
                if (_httpClient == null)
                {
                    return false;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                _currentResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!_currentResponse.IsSuccessStatusCode)
                {
                    Debug.LogError($"HTTP {(int)_currentResponse.StatusCode}: {_currentResponse.ReasonPhrase}");
                    _currentResponse?.Dispose();
                    _currentResponse = null;
                    return false;
                }

                long totalBytes = _currentResponse.Content.Headers.ContentLength ?? -1L;

                // Download directly without coroutines
                var contentStream = await _currentResponse.Content.ReadAsStreamAsync();
                long receivedBytes = await ProcessDownloadStreamAsync(contentStream, filePath, totalBytes);

                _currentResponse?.Dispose();
                _currentResponse = null;

                if (receivedBytes == 0)
                {
                    Debug.LogError("No data received from server");
                    CleanupIncompleteFile(filePath);
                    return false;
                }

                // Validate downloaded file integrity
                if (ValidateModelIntegrity(model))
                {
                    return true;
                }
                else
                {
                    CleanupIncompleteFile(filePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Download error: {ex.Message}");
                CleanupIncompleteFile(filePath);
                return false;
            }
        }

        /// <summary>
        /// Process the download stream asynchronously WITHOUT coroutines (MUCH FASTER)
        /// </summary>
        private async Task<long> ProcessDownloadStreamAsync(System.IO.Stream contentStream, string filePath, long totalBytes)
        {
            long receivedBytes = 0L;
            int updateCount = 0;

            if (contentStream == null)
            {
                return 0;
            }

            FileStream fileStream = null;
            try
            {
                // Use larger buffer for FileStream (64KB) for faster disk writes
                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);

                byte[] buffer = new byte[OPTIMAL_BUFFER_SIZE];
                int bytesRead;

                while (_isDownloading && (bytesRead = await contentStream.ReadAsync(buffer, 0, OPTIMAL_BUFFER_SIZE)) > 0)
                {
                    // Write to file asynchronously
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    receivedBytes += bytesRead;

                    // Update progress (no logs, just callbacks for UI)
                    if (totalBytes > 0)
                    {
                        float progress = (float)receivedBytes / totalBytes;
                        OnDownloadProgress?.Invoke(progress);
                        updateCount++;
                    }
                }

                await fileStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in download stream: {ex.Message}");
                receivedBytes = 0;
            }
            finally
            {
                fileStream?.Dispose();
                contentStream?.Dispose();
            }

            return receivedBytes;
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
            Debug.Log("Download cancelled - waiting for task to complete...");

            // Wait for the async task to finish and release the file
            // This is critical to ensure the file is no longer in use before deletion
            if (_currentDownloadTask != null && !_currentDownloadTask.IsCompleted)
            {
                try
                {
                    // Wait up to 5 seconds for task to complete
                    _currentDownloadTask.Wait(TimeSpan.FromSeconds(5));
                    Debug.Log("Download task completed after cancellation");
                }
                catch (AggregateException ex)
                {
                    Debug.LogWarning($"Download task was interrupted: {ex.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error waiting for download task: {ex.Message}");
                }
            }

            _currentDownloadTask = null;
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
                return true; // Skip validation if no checksum is available
            }

            if (!File.Exists(model.ModelPath))
            {
                return false;
            }

            string actualChecksum = CalculateFileSHA1(model.ModelPath);

            if (actualChecksum == null)
            {
                return false;
            }

            bool isValid = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
            if (!isValid)
            {
                Debug.LogError($"Checksum mismatch for {model.GetSizeString()}");
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
