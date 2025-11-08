using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfflineSpeechRecognition.Audio
{
    /// <summary>
    /// Handles microphone audio capture
    /// </summary>
    public class AudioCapture : MonoBehaviour
    {
        private AudioClip _recordingClip;
        private int _recordingPosition;
        private string _currentMicrophoneName;
        private bool _isRecording;
        private int _maxRecordingSeconds;
        private float[] _audioBuffer;
        private List<float> _recordedSamples;

        public event Action<float[]> OnAudioCaptured;
        public event Action<string> OnCaptureError;

        private void Awake()
        {
            _recordedSamples = new List<float>();
        }

        /// <summary>
        /// Start recording from microphone
        /// </summary>
        public bool StartRecording(int maxDurationSeconds = Utilities.Constants.MICROPHONE_RECORD_LENGTH)
        {
            if (_isRecording)
            {
                Debug.LogWarning("Already recording");
                return false;
            }

            try
            {
                // Check if microphone is available
                if (Microphone.devices.Length <= 0)
                {
                    OnCaptureError?.Invoke("No microphone devices found");
                    return false;
                }

                _currentMicrophoneName = null;
                _maxRecordingSeconds = maxDurationSeconds;
                _recordedSamples.Clear();

                // Request microphone permission on Android
                #if UNITY_ANDROID
                if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
                {
                    Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
                }
                #endif

                // Create audio clip for recording
                _recordingClip = Microphone.Start(
                    _currentMicrophoneName,
                    false,
                    maxDurationSeconds,
                    Utilities.Constants.MICROPHONE_RECORD_FREQUENCY
                );

                if (_recordingClip == null)
                {
                    OnCaptureError?.Invoke("Failed to start microphone recording");
                    return false;
                }

                _isRecording = true;
                _recordingPosition = 0;
                Debug.Log("Microphone recording started");

                return true;
            }
            catch (Exception ex)
            {
                OnCaptureError?.Invoke($"Error starting recording: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop recording and get the audio data
        /// </summary>
        public float[] StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("Not currently recording");
                return null;
            }

            try
            {
                Microphone.End(_currentMicrophoneName);
                _isRecording = false;

                if (_recordingClip == null)
                {
                    return null;
                }

                // Get audio data from clip
                float[] audioData = new float[_recordingClip.samples * _recordingClip.channels];
                _recordingClip.GetData(audioData, 0);

                // Convert to mono if needed
                if (_recordingClip.channels > 1)
                {
                    audioData = AudioUtils.StereoToMono(audioData);
                }

                // Cleanup
                Destroy(_recordingClip);
                _recordingClip = null;

                Debug.Log($"Recording stopped. Captured {audioData.Length} samples");
                OnAudioCaptured?.Invoke(audioData);

                return audioData;
            }
            catch (Exception ex)
            {
                OnCaptureError?.Invoke($"Error stopping recording: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get current recording position
        /// </summary>
        public int GetRecordingPosition()
        {
            if (Microphone.IsRecording(_currentMicrophoneName))
            {
                return Microphone.GetPosition(_currentMicrophoneName);
            }

            return 0;
        }

        /// <summary>
        /// Get recording duration in seconds
        /// </summary>
        public float GetRecordingDuration()
        {
            int position = GetRecordingPosition();
            return position / (float)Utilities.Constants.MICROPHONE_RECORD_FREQUENCY;
        }

        /// <summary>
        /// Check if currently recording
        /// </summary>
        public bool IsRecording => _isRecording && Microphone.IsRecording(_currentMicrophoneName);

        /// <summary>
        /// Get list of available microphones
        /// </summary>
        public static string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }

        /// <summary>
        /// Set the microphone to use
        /// </summary>
        public void SetMicrophone(string microphoneName)
        {
            if (_isRecording)
            {
                Debug.LogWarning("Cannot change microphone while recording");
                return;
            }

            var devices = Microphone.devices;
            if (Array.Exists(devices, element => element == microphoneName))
            {
                _currentMicrophoneName = microphoneName;
                Debug.Log($"Microphone set to: {microphoneName}");
            }
            else
            {
                Debug.LogWarning($"Microphone '{microphoneName}' not found");
            }
        }

        /// <summary>
        /// Cancel recording without returning audio
        /// </summary>
        public void CancelRecording()
        {
            if (!_isRecording)
                return;

            try
            {
                Microphone.End(_currentMicrophoneName);
                _isRecording = false;

                if (_recordingClip != null)
                {
                    Destroy(_recordingClip);
                    _recordingClip = null;
                }

                Debug.Log("Recording cancelled");
            }
            catch (Exception ex)
            {
                OnCaptureError?.Invoke($"Error cancelling recording: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if microphone is available
        /// </summary>
        public static bool IsMicrophoneAvailable()
        {
            return Microphone.devices.Length > 0;
        }

        /// <summary>
        /// Get current microphone name
        /// </summary>
        public string GetCurrentMicrophone()
        {
            return _currentMicrophoneName ?? Microphone.devices[0];
        }
    }
}
