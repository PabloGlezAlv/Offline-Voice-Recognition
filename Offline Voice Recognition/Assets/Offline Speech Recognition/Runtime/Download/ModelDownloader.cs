using System;
using System.Collections;
using System.IO;
using System.Net.Http;
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
            if (_httpClient == null)
            {
                _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
                _httpClient.Timeout = TimeSpan.FromSeconds(Utilities.Constants.DOWNLOAD_TIMEOUT_SECONDS);
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
        /// Start downloading a model
        /// </summary>
        public void StartDownload(WhisperModel model)
        {
            if (_isDownloading)
            {
                OnDownloadError?.Invoke("A download is already in progress");
                return;
            }

            StartCoroutine(DownloadModelCoroutine(model));
        }

        /// <summary>
        /// Download coroutine
        /// </summary>
        private IEnumerator DownloadModelCoroutine(WhisperModel model)
        {
            _isDownloading = true;
            string url = model.GetDownloadUrl();
            string modelDir = model.GetModelDirectory();
            bool success = false;
            string errorMessage = "";

            // Ensure directory exists
            if (!Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }

            // Download the model file
            yield return DownloadFile(url, model.ModelPath, model, (isSuccess, error) =>
            {
                success = isSuccess;
                errorMessage = error;
            });

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

            _isDownloading = false;
        }

        /// <summary>
        /// Download a file from URL
        /// </summary>
        private IEnumerator DownloadFile(string url, string filePath, WhisperModel model, System.Action<bool, string> onComplete)
        {
            Debug.Log($"Downloading from: {url}");

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
            var buffer = new byte[8192];

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
                onComplete?.Invoke(true, "");
            }
        }

        /// <summary>
        /// Process the download stream
        /// </summary>
        private IEnumerator ProcessDownloadStream(System.IO.Stream contentStream, string filePath, byte[] buffer, long totalBytes, System.Action<long> onBytesReceived)
        {
            long receivedBytes = 0L;
            bool error = false;

            if (contentStream == null)
            {
                yield break;
            }

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
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
                    bytesRead = contentStream.Read(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error reading stream: {ex.Message}");
                    error = true;
                    break;
                }

                if (bytesRead <= 0)
                    break;

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

                if (totalBytes > 0)
                {
                    float progress = (float)receivedBytes / totalBytes;
                    OnDownloadProgress?.Invoke(progress);
                }

                yield return null;
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
        /// Clean up resources
        /// </summary>
        public static void Cleanup()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}
