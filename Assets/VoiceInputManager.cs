using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts
{
    /// <summary>
    /// State-of-the-art voice input system using Google Speech-to-Text API
    /// Supports real-time recording with visual feedback and error handling
    /// </summary>
    public class VoiceInputManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GeminiChatConfig config;
        
        [Header("Voice Settings")]
        [Tooltip("Maximum recording duration in seconds")]
        [SerializeField] private float maxRecordingDuration = 30f;
        
        [Tooltip("Minimum recording duration in seconds (to avoid accidental clicks)")]
        [SerializeField] private float minRecordingDuration = 0.5f;
        
        [Tooltip("Language code for speech recognition (e.g., 'en-US', 'fr-FR', 'ar-SA')")]
        [SerializeField] private string languageCode = "en-US";
        
        [Tooltip("Audio sample rate (higher = better quality but larger files)")]
        [SerializeField] private int sampleRate = 16000;
        
        [Header("Audio Processing")]
        [Tooltip("Silence threshold (0-1) for detecting end of speech")]
        [Range(0f, 1f)]
        [SerializeField] private float silenceThreshold = 0.01f;
        
        [Tooltip("Duration of silence (seconds) before auto-stopping")]
        [SerializeField] private float silenceDetectionDuration = 2f;
        
        // Events
        public event Action<string> OnTranscriptionComplete;
        public event Action<string> OnTranscriptionError;
        public event Action<float> OnRecordingProgress; // Progress 0-1
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;
        
        // State
        private bool isRecording = false;
        private AudioClip recordedClip;
        private string microphoneDevice;
        private float recordingStartTime;
        private Coroutine recordingCoroutine;
        
        // Constants
        private const string SPEECH_API_URL = "https://speech.googleapis.com/v1/speech:recognize";
        
        void Start()
        {
            // Auto-discover config if not assigned
            if (config == null)
            {
                config = FindObjectOfType<GeminiChat>()?.GetComponent<GeminiChat>()?.GetConfig();
                if (config == null)
                {
                    Debug.LogWarning("[VoiceInputManager] GeminiChatConfig not found. Voice input may not work without API key.");
                }
            }
            
            // Get default microphone
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                Debug.Log($"[VoiceInputManager] Using microphone: {microphoneDevice}");
            }
            else
            {
                Debug.LogError("[VoiceInputManager] No microphone detected!");
            }
        }
        
        /// <summary>
        /// Start recording voice input
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                Debug.LogWarning("[VoiceInputManager] Already recording!");
                return;
            }
            
            if (string.IsNullOrEmpty(microphoneDevice))
            {
                OnTranscriptionError?.Invoke("No microphone detected. Please connect a microphone.");
                return;
            }
            
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                OnTranscriptionError?.Invoke("API key not configured. Please set up your Gemini API key.");
                return;
            }
            
            isRecording = true;
            recordingStartTime = Time.time;
            
            // Start recording with the microphone
            recordedClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, sampleRate);
            
            OnRecordingStarted?.Invoke();
            Debug.Log("[VoiceInputManager] Recording started");
            
            // Start monitoring coroutine
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
            }
            recordingCoroutine = StartCoroutine(MonitorRecording());
        }
        
        /// <summary>
        /// Stop recording and process speech-to-text
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
            {
                return;
            }
            
            float recordingDuration = Time.time - recordingStartTime;
            
            // Stop the microphone
            int micPosition = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            
            isRecording = false;
            OnRecordingStopped?.Invoke();
            
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
            }
            
            Debug.Log($"[VoiceInputManager] Recording stopped. Duration: {recordingDuration:F2}s");
            
            // Check minimum duration
            if (recordingDuration < minRecordingDuration)
            {
                Debug.LogWarning($"[VoiceInputManager] Recording too short ({recordingDuration:F2}s < {minRecordingDuration}s)");
                OnTranscriptionError?.Invoke("Recording too short. Please hold the button longer.");
                return;
            }
            
            // Trim the audio clip to actual recorded length
            if (recordedClip != null && micPosition > 0)
            {
                float[] samples = new float[micPosition * recordedClip.channels];
                recordedClip.GetData(samples, 0);
                
                AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", micPosition, recordedClip.channels, recordedClip.frequency, false);
                trimmedClip.SetData(samples, 0);
                recordedClip = trimmedClip;
                
                // Send to speech recognition
                StartCoroutine(TranscribeAudio(recordedClip));
            }
            else
            {
                OnTranscriptionError?.Invoke("Failed to record audio. Please try again.");
            }
        }
        
        /// <summary>
        /// Monitor recording progress and detect silence
        /// </summary>
        private IEnumerator MonitorRecording()
        {
            float[] samples = new float[128];
            float lastSoundTime = Time.time;
            
            while (isRecording)
            {
                float recordingTime = Time.time - recordingStartTime;
                
                // Update progress
                float progress = Mathf.Clamp01(recordingTime / maxRecordingDuration);
                OnRecordingProgress?.Invoke(progress);
                
                // Auto-stop at max duration
                if (recordingTime >= maxRecordingDuration)
                {
                    Debug.Log("[VoiceInputManager] Max recording duration reached");
                    StopRecording();
                    yield break;
                }
                
                // Detect silence
                if (recordedClip != null)
                {
                    int micPosition = Microphone.GetPosition(microphoneDevice);
                    if (micPosition > samples.Length)
                    {
                        recordedClip.GetData(samples, micPosition - samples.Length);
                        
                        // Calculate RMS (Root Mean Square) for volume
                        float sum = 0f;
                        foreach (float sample in samples)
                        {
                            sum += sample * sample;
                        }
                        float rms = Mathf.Sqrt(sum / samples.Length);
                        
                        // Check if sound detected
                        if (rms > silenceThreshold)
                        {
                            lastSoundTime = Time.time;
                        }
                        
                        // Auto-stop after silence duration (only after min recording time)
                        if (recordingTime > minRecordingDuration && 
                            Time.time - lastSoundTime > silenceDetectionDuration)
                        {
                            Debug.Log("[VoiceInputManager] Silence detected, auto-stopping");
                            StopRecording();
                            yield break;
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// Transcribe audio using Google Speech-to-Text API
        /// </summary>
        private IEnumerator TranscribeAudio(AudioClip clip)
        {
            Debug.Log("[VoiceInputManager] Converting audio to base64...");
            
            // Convert AudioClip to WAV bytes
            byte[] wavData = ConvertAudioClipToWav(clip);
            if (wavData == null)
            {
                OnTranscriptionError?.Invoke("Failed to process audio data.");
                yield break;
            }
            
            // Convert to base64
            string base64Audio = Convert.ToBase64String(wavData);
            
            // Build request JSON
            string requestJson = BuildSpeechRequestJson(base64Audio);
            
            // Create request
            string apiUrl = $"{SPEECH_API_URL}?key={config.ApiKey}";
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                Debug.Log("[VoiceInputManager] Sending transcription request...");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"[VoiceInputManager] API Response: {responseText}");
                    
                    // Parse response
                    string transcription = ParseSpeechResponse(responseText);
                    
                    if (!string.IsNullOrEmpty(transcription))
                    {
                        Debug.Log($"[VoiceInputManager] Transcription: {transcription}");
                        OnTranscriptionComplete?.Invoke(transcription);
                    }
                    else
                    {
                        OnTranscriptionError?.Invoke("No speech detected. Please try again.");
                    }
                }
                else
                {
                    string error = $"Speech recognition failed: {request.error}";
                    Debug.LogError($"[VoiceInputManager] {error}\nResponse: {request.downloadHandler.text}");
                    OnTranscriptionError?.Invoke("Speech recognition failed. Please check your internet connection.");
                }
            }
        }
        
        /// <summary>
        /// Convert AudioClip to WAV format bytes
        /// </summary>
        private byte[] ConvertAudioClipToWav(AudioClip clip)
        {
            try
            {
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                
                // Convert to 16-bit PCM
                short[] intData = new short[samples.Length];
                byte[] bytesData = new byte[samples.Length * 2];
                
                float rescaleFactor = 32767; // To convert float to Int16
                
                for (int i = 0; i < samples.Length; i++)
                {
                    intData[i] = (short)(samples[i] * rescaleFactor);
                    byte[] byteArr = BitConverter.GetBytes(intData[i]);
                    byteArr.CopyTo(bytesData, i * 2);
                }
                
                // Create WAV file
                int headerSize = 44;
                int fileSize = bytesData.Length + headerSize;
                
                byte[] wav = new byte[fileSize];
                
                // WAV header
                // "RIFF"
                wav[0] = (byte)'R';
                wav[1] = (byte)'I';
                wav[2] = (byte)'F';
                wav[3] = (byte)'F';
                
                // File size - 8
                byte[] fileSizeBytes = BitConverter.GetBytes(fileSize - 8);
                fileSizeBytes.CopyTo(wav, 4);
                
                // "WAVE"
                wav[8] = (byte)'W';
                wav[9] = (byte)'A';
                wav[10] = (byte)'V';
                wav[11] = (byte)'E';
                
                // "fmt "
                wav[12] = (byte)'f';
                wav[13] = (byte)'m';
                wav[14] = (byte)'t';
                wav[15] = (byte)' ';
                
                // Subchunk1Size (16 for PCM)
                BitConverter.GetBytes(16).CopyTo(wav, 16);
                
                // AudioFormat (1 for PCM)
                BitConverter.GetBytes((short)1).CopyTo(wav, 20);
                
                // NumChannels
                BitConverter.GetBytes((short)clip.channels).CopyTo(wav, 22);
                
                // SampleRate
                BitConverter.GetBytes(clip.frequency).CopyTo(wav, 24);
                
                // ByteRate
                BitConverter.GetBytes(clip.frequency * clip.channels * 2).CopyTo(wav, 28);
                
                // BlockAlign
                BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wav, 32);
                
                // BitsPerSample
                BitConverter.GetBytes((short)16).CopyTo(wav, 34);
                
                // "data"
                wav[36] = (byte)'d';
                wav[37] = (byte)'a';
                wav[38] = (byte)'t';
                wav[39] = (byte)'a';
                
                // Data size
                BitConverter.GetBytes(bytesData.Length).CopyTo(wav, 40);
                
                // Audio data
                bytesData.CopyTo(wav, headerSize);
                
                return wav;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Error converting audio: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Build JSON request for Google Speech API
        /// </summary>
        private string BuildSpeechRequestJson(string base64Audio)
        {
            return $@"{{
  ""config"": {{
    ""encoding"": ""LINEAR16"",
    ""sampleRateHertz"": {sampleRate},
    ""languageCode"": ""{languageCode}"",
    ""enableAutomaticPunctuation"": true,
    ""model"": ""default"",
    ""useEnhanced"": true
  }},
  ""audio"": {{
    ""content"": ""{base64Audio}""
  }}
}}";
        }
        
        /// <summary>
        /// Parse transcription from Google Speech API response
        /// </summary>
        private string ParseSpeechResponse(string json)
        {
            try
            {
                // Simple JSON parsing (you could use a JSON library for more robustness)
                if (json.Contains("\"transcript\""))
                {
                    int startIndex = json.IndexOf("\"transcript\"") + 15;
                    int endIndex = json.IndexOf("\"", startIndex);
                    
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        return json.Substring(startIndex, endIndex - startIndex);
                    }
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Error parsing response: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if currently recording
        /// </summary>
        public bool IsRecording()
        {
            return isRecording;
        }
        
        /// <summary>
        /// Get available microphone devices
        /// </summary>
        public string[] GetMicrophoneDevices()
        {
            return Microphone.devices;
        }
        
        /// <summary>
        /// Set active microphone device
        /// </summary>
        public void SetMicrophoneDevice(string deviceName)
        {
            if (isRecording)
            {
                Debug.LogWarning("[VoiceInputManager] Cannot change microphone while recording!");
                return;
            }
            
            microphoneDevice = deviceName;
            Debug.Log($"[VoiceInputManager] Microphone set to: {deviceName}");
        }
        
        /// <summary>
        /// Set language for speech recognition
        /// </summary>
        public void SetLanguage(string langCode)
        {
            languageCode = langCode;
            Debug.Log($"[VoiceInputManager] Language set to: {langCode}");
        }
        
        void OnDestroy()
        {
            // Clean up
            if (isRecording)
            {
                Microphone.End(microphoneDevice);
            }
        }
    }
}
