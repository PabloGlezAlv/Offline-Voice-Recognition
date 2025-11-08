using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using OfflineSpeechRecognition.Inference;
using OfflineSpeechRecognition.Language;

namespace OfflineSpeechRecognition.Core
{
    /// <summary>
    /// Processes audio in a background thread
    /// Thread-safe audio processing and STT inference
    /// </summary>
    public class AudioProcessor
    {
        /// <summary>
        /// Result of audio transcription
        /// </summary>
        public struct TranscriptionResult
        {
            public string text;
            public float confidence;
            public string language;
            public float processingTime;
            public bool success;
        }

        private class ProcessingTask
        {
            public float[] audioData;
            public Action<TranscriptionResult> callback;
            public int attemptCount = 0;
            public const int MAX_ATTEMPTS = 3;
        }

        private Thread _processingThread;
        private Queue<ProcessingTask> _taskQueue;
        private SentisWhisperRunner _whisperRunner;
        private bool _isRunning;
        private object _queueLock = new object();
        private object _runnerLock = new object();
        private WhisperLanguage _currentLanguage;
        private bool _threadInitialized;

        public AudioProcessor(SentisWhisperRunner whisperRunner, WhisperLanguage language)
        {
            _whisperRunner = whisperRunner;
            _currentLanguage = language;
            _taskQueue = new Queue<ProcessingTask>();
            _isRunning = false;
            _threadInitialized = false;
        }

        /// <summary>
        /// Start the audio processing thread
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _processingThread = new Thread(ProcessingLoop)
            {
                IsBackground = true,
                Name = "AudioProcessorThread",
                Priority = System.Threading.ThreadPriority.Normal
            };

            _processingThread.Start();
            Debug.Log("Audio processing thread started");
        }

        /// <summary>
        /// Stop the processing thread
        /// </summary>
        public void Stop()
        {
            _isRunning = false;

            if (_processingThread != null && _processingThread.IsAlive)
            {
                // Give thread time to finish gracefully
                if (!_processingThread.Join(TimeSpan.FromSeconds(5)))
                {
                    _processingThread.Abort();
                }
                _processingThread = null;
            }

            Debug.Log("Audio processing thread stopped");
        }

        /// <summary>
        /// Queue audio for processing
        /// </summary>
        public void ProcessAudio(float[] audioData, Action<TranscriptionResult> onComplete)
        {
            if (audioData == null || audioData.Length == 0)
            {
                onComplete?.Invoke(new TranscriptionResult
                {
                    text = "",
                    confidence = 0f,
                    language = LanguageConfig.GetLanguageCode(_currentLanguage),
                    processingTime = 0f,
                    success = false
                });
                return;
            }

            if (!_isRunning)
            {
                onComplete?.Invoke(new TranscriptionResult
                {
                    text = "",
                    confidence = 0f,
                    language = LanguageConfig.GetLanguageCode(_currentLanguage),
                    processingTime = 0f,
                    success = false
                });
                Debug.LogWarning("Audio processor is not running");
                return;
            }

            lock (_queueLock)
            {
                _taskQueue.Enqueue(new ProcessingTask
                {
                    audioData = audioData,
                    callback = onComplete
                });
            }
        }

        /// <summary>
        /// Set the language for transcription
        /// </summary>
        public void SetLanguage(WhisperLanguage language)
        {
            _currentLanguage = language;

            lock (_runnerLock)
            {
                if (_whisperRunner != null)
                {
                    _whisperRunner.SetLanguage(language);
                }
            }
        }

        /// <summary>
        /// Get queue size
        /// </summary>
        public int GetQueueSize()
        {
            lock (_queueLock)
            {
                return _taskQueue.Count;
            }
        }

        /// <summary>
        /// Main processing loop (runs on background thread)
        /// </summary>
        private void ProcessingLoop()
        {
            _threadInitialized = true;

            try
            {
                while (_isRunning)
                {
                    ProcessingTask task = null;

                    lock (_queueLock)
                    {
                        if (_taskQueue.Count > 0)
                        {
                            task = _taskQueue.Dequeue();
                        }
                    }

                    if (task != null)
                    {
                        ProcessAudioTask(task);
                    }
                    else
                    {
                        // Sleep briefly to avoid busy waiting
                        Thread.Sleep(10);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Thread was aborted, cleanup gracefully
                Debug.Log("Audio processor thread aborted");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in audio processor thread: {ex.Message}");
            }
        }

        /// <summary>
        /// Process a single audio task
        /// </summary>
        private void ProcessAudioTask(ProcessingTask task)
        {
            try
            {
                if (task.audioData == null || task.audioData.Length == 0)
                {
                    ExecuteCallback(task, new TranscriptionResult
                    {
                        text = "",
                        confidence = 0f,
                        language = LanguageConfig.GetLanguageCode(_currentLanguage),
                        processingTime = 0f,
                        success = false
                    });
                    return;
                }

                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                lock (_runnerLock)
                {
                    if (_whisperRunner != null && _whisperRunner.IsInitialized)
                    {
                        var result = _whisperRunner.RunInference(task.audioData);

                        stopwatch.Stop();

                        var finalResult = new TranscriptionResult
                        {
                            text = result.text ?? "",
                            confidence = result.confidence,
                            language = result.language,
                            processingTime = (float)stopwatch.ElapsedMilliseconds,
                            success = !string.IsNullOrEmpty(result.text)
                        };

                        ExecuteCallback(task, finalResult);
                    }
                    else
                    {
                        stopwatch.Stop();
                        ExecuteCallback(task, new TranscriptionResult
                        {
                            text = "",
                            confidence = 0f,
                            language = LanguageConfig.GetLanguageCode(_currentLanguage),
                            processingTime = (float)stopwatch.ElapsedMilliseconds,
                            success = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing audio task: {ex.Message}");

                var errorResult = new TranscriptionResult
                {
                    text = "",
                    confidence = 0f,
                    language = LanguageConfig.GetLanguageCode(_currentLanguage),
                    processingTime = 0f,
                    success = false
                };

                ExecuteCallback(task, errorResult);
            }
        }

        /// <summary>
        /// Execute callback on main thread
        /// </summary>
        private void ExecuteCallback(ProcessingTask task, TranscriptionResult result)
        {
            // Note: In a production environment, you would use a thread-safe
            // mechanism to invoke this on the main thread (e.g., Unity's main thread dispatcher)
            // For now, we'll execute it directly with the understanding that
            // the callback should handle thread safety

            try
            {
                task.callback?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing transcription callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if processor is running
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Get thread status
        /// </summary>
        public string GetStatus()
        {
            int queueSize = GetQueueSize();
            return $"Running: {_isRunning}, Queue Size: {queueSize}, Thread Alive: {_processingThread?.IsAlive ?? false}";
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            Stop();

            lock (_queueLock)
            {
                _taskQueue.Clear();
            }

            lock (_runnerLock)
            {
                if (_whisperRunner != null)
                {
                    _whisperRunner.Cleanup();
                }
            }
        }
    }
}
