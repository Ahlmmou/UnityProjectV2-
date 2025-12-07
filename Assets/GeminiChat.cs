using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts
{
    /// <summary>
    /// State-of-the-art Gemini 2.0 Flash chatbot with streaming support and conversation history
    /// </summary>
    public class GeminiChat : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GeminiChatConfig config;
        
        [Header("Conversation History")]
        private List<GeminiChatMessage> conversationHistory = new List<GeminiChatMessage>();
        
        // Events for UI updates
        public event Action<string> OnStreamingChunk;
        public event Action<string> OnResponseComplete;
        public event Action<string> OnError;
        
        private bool isProcessing = false;
        
        void Start()
        {
            if (config == null)
            {
                Debug.LogWarning("[GeminiChat] Config not assigned in Inspector. Creating default runtime instance.");
                config = ScriptableObject.CreateInstance<GeminiChatConfig>();
            }
            
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                Debug.LogError("[GeminiChat] API Key is not set in GeminiChatConfig!");
                return;
            }
            
            Debug.Log($"[GeminiChat] Initialized with model: {config.Model}");
            Debug.Log($"[GeminiChat] API URL: {config.GetApiUrl()?.Split('?')[0]}"); // Log URL without key
        }
        
        /// <summary>
        /// Send a message to Gemini with streaming response
        /// </summary>
        public void SendMessage(string message, Action<string> onComplete = null, Action<string> onError = null)
        {
            // Auto-initialize config if missing (Runtime fallback)
            if (config == null)
            {
                Debug.LogWarning("[GeminiChat] Config not assigned. Creating default runtime instance.");
                config = ScriptableObject.CreateInstance<GeminiChatConfig>();
            }

            if (isProcessing)
            {
                Debug.LogWarning("[GeminiChat] Already processing a request");
                onError?.Invoke("Please wait for the current response to complete");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("[GeminiChat] Cannot send empty message");
                onError?.Invoke("Message cannot be empty");
                return;
            }
            
            StartCoroutine(SendMessageCoroutine(message, onComplete, onError));
        }
        
        private IEnumerator SendMessageCoroutine(string userMessage, Action<string> onComplete, Action<string> onError)
        {
            isProcessing = true;
            string fullResponse = "";
            int retryCount = 0;
            
            // Add user message to history
            AddMessageToHistory(GeminiChatMessage.Role.User, userMessage);
            
            while (retryCount <= config.MaxRetries)
            {
                string url = config.GetApiUrl();
                if (string.IsNullOrEmpty(url))
                {
                    string errorMsg = "Failed to construct API URL. Check your configuration.";
                    Debug.LogError($"[GeminiChat] {errorMsg}");
                    OnError?.Invoke(errorMsg);
                    onError?.Invoke(errorMsg);
                    isProcessing = false;
                    yield break;
                }
                
                string requestBody = BuildRequestBody();
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                
                Debug.Log($"[GeminiChat] Sending request to: {url.Split('?')[0]}");
                Debug.Log($"[GeminiChat] Request body: {requestBody}");
                
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = config.TimeoutSeconds;
                    
                    yield return request.SendWebRequest();
                    
                    Debug.Log($"[GeminiChat] Response code: {request.responseCode}");
                    Debug.Log($"[GeminiChat] Response: {request.downloadHandler.text}");
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Process regular JSON response (not streaming)
                        fullResponse = ProcessResponse(request.downloadHandler.text);
                        
                        if (!string.IsNullOrEmpty(fullResponse))
                        {
                            // Add model response to history
                            AddMessageToHistory(GeminiChatMessage.Role.Model, fullResponse);
                            
                            OnResponseComplete?.Invoke(fullResponse);
                            onComplete?.Invoke(fullResponse);
                            isProcessing = false;
                            yield break;
                        }
                        else
                        {
                            string errorMsg = "Received empty response from API";
                            Debug.LogWarning($"[GeminiChat] {errorMsg}");
                            OnError?.Invoke(errorMsg);
                            onError?.Invoke(errorMsg);
                            isProcessing = false;
                            yield break;
                        }
                    }
                    else
                    {
                        retryCount++;
                        string errorMsg = $"Request failed: {request.error} (Attempt {retryCount}/{config.MaxRetries + 1})";
                        Debug.LogWarning($"[GeminiChat] {errorMsg}");
                        Debug.LogWarning($"[GeminiChat] Response: {request.downloadHandler.text}");
                        
                        if (retryCount > config.MaxRetries)
                        {
                            OnError?.Invoke($"Failed after {config.MaxRetries + 1} attempts: {request.error}");
                            onError?.Invoke($"Network error: {request.error}");
                            isProcessing = false;
                            yield break;
                        }
                        
                        // Wait before retry (exponential backoff)
                        yield return new WaitForSeconds(Mathf.Pow(2, retryCount - 1));
                    }
                }
            }
            
            isProcessing = false;
        }


        
        private string BuildRequestBody()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            
            // Build contents array
            json.Append("\"contents\":[");
            
            // Add conversation history (limit to max history messages)
            int startIndex = Mathf.Max(0, conversationHistory.Count - config.MaxHistoryMessages);
            for (int i = startIndex; i < conversationHistory.Count; i++)
            {
                var msg = conversationHistory[i];
                if (i > startIndex) json.Append(",");
                
                json.Append("{");
                json.AppendFormat("\"role\":\"{0}\",", msg.GetRoleString());
                json.Append("\"parts\":[{");
                json.AppendFormat("\"text\":{0}", JsonEscape(msg.content));
                json.Append("}]");
                json.Append("}");
            }
            json.Append("],");
            
            // Add generation config
            json.Append("\"generationConfig\":{");
            json.AppendFormat("\"temperature\":{0},", config.Temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            json.AppendFormat("\"topK\":{0},", config.TopK);
            json.AppendFormat("\"topP\":{0},", config.TopP.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            json.AppendFormat("\"maxOutputTokens\":{0}", config.MaxTokens);
            json.Append("}");
            
            // Add system instruction if provided
            if (!string.IsNullOrWhiteSpace(config.SystemInstruction))
            {
                json.Append(",\"systemInstruction\":{");
                json.Append("\"parts\":[{");
                json.AppendFormat("\"text\":{0}", JsonEscape(config.SystemInstruction));
                json.Append("}]");
                json.Append("}");
            }
            
            json.Append("}");
            return json.ToString();
        }
        
        private string JsonEscape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            
            StringBuilder escaped = new StringBuilder("\"");
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\"': escaped.Append("\\\""); break;
                    case '\\': escaped.Append("\\\\"); break;
                    case '\b': escaped.Append("\\b"); break;
                    case '\f': escaped.Append("\\f"); break;
                    case '\n': escaped.Append("\\n"); break;
                    case '\r': escaped.Append("\\r"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            escaped.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            escaped.Append(c);
                        }
                        break;
                }
            }
            escaped.Append("\"");
            return escaped.ToString();
        }
        
        private string ProcessResponse(string responseText)
        {
            try
            {
                Debug.Log($"[GeminiChat] Processing response, length: {responseText.Length}");
                
                // Extract text using simple JSON parsing
                string textValue = ExtractTextFromJson(responseText);
                
                if (!string.IsNullOrEmpty(textValue))
                {
                    Debug.Log($"[GeminiChat] Extracted text length: {textValue.Length}");
                    // For non-streaming, invoke the complete event with full text
                    OnStreamingChunk?.Invoke(textValue);
                    return textValue;
                }
                else
                {
                    Debug.LogWarning("[GeminiChat] No text found in response");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiChat] Error processing response: {e.Message}");
                return null;
            }
        }
        
        private string ExtractTextFromJson(string json)
        {
            try
            {
                // Look for "text":"..." pattern in the JSON
                // Handle the nested structure: candidates[0].content.parts[0].text
                int textIndex = json.IndexOf("\"text\"");
                if (textIndex == -1)
                {
                    Debug.LogWarning("[GeminiChat] No 'text' field found in JSON");
                    return null;
                }
                
                // Find the colon after "text"
                int colonIndex = json.IndexOf(":", textIndex);
                if (colonIndex == -1) return null;
                
                // Skip whitespace after colon
                int startQuote = colonIndex + 1;
                while (startQuote < json.Length && (json[startQuote] == ' ' || json[startQuote] == '\t'))
                {
                    startQuote++;
                }
                
                // Check if it's a quoted string
                if (startQuote >= json.Length || json[startQuote] != '\"')
                {
                    Debug.LogWarning("[GeminiChat] Text value is not a string");
                    return null;
                }
                
                startQuote++; // Move past the opening quote
                
                // Find the end of the text value (handle escaped quotes)
                StringBuilder textValue = new StringBuilder();
                int i = startQuote;
                while (i < json.Length)
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        // Handle escape sequences
                        char nextChar = json[i + 1];
                        switch (nextChar)
                        {
                            case 'n': textValue.Append('\n'); break;
                            case 'r': textValue.Append('\r'); break;
                            case 't': textValue.Append('\t'); break;
                            case '\"': textValue.Append('\"'); break;
                            case '\\': textValue.Append('\\'); break;
                            default: textValue.Append(nextChar); break;
                        }
                        i += 2;
                    }
                    else if (json[i] == '\"')
                    {
                        // Found the closing quote
                        return textValue.ToString();
                    }
                    else
                    {
                        textValue.Append(json[i]);
                        i++;
                    }
                }
                
                Debug.LogWarning("[GeminiChat] No closing quote found for text value");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiChat] Error extracting text from JSON: {e.Message}");
                return null;
            }
        }
        
        private void AddMessageToHistory(GeminiChatMessage.Role role, string content)
        {
            conversationHistory.Add(new GeminiChatMessage(role, content));
            
            if (config != null)
            {
                // Trim history if it exceeds max messages
                while (conversationHistory.Count > config.MaxHistoryMessages * 2) // *2 because each exchange is 2 messages
                {
                    conversationHistory.RemoveAt(0);
                }
            }
        }
        
        /// <summary>
        /// Clear conversation history
        /// </summary>
        public void ClearHistory()
        {
            conversationHistory.Clear();
            Debug.Log("[GeminiChat] Conversation history cleared");
        }
        
        /// <summary>
        /// Get current conversation history
        /// </summary>
        public List<GeminiChatMessage> GetHistory()
        {
            return new List<GeminiChatMessage>(conversationHistory);
        }
        
        /// <summary>
        /// Check if currently processing a request
        /// </summary>
        public bool IsProcessing()
        {
            return isProcessing;
        }
        
        /// <summary>
        /// Get the configuration (for use by VoiceInputManager and other components)
        /// </summary>
        public GeminiChatConfig GetConfig()
        {
            return config;
        }
    }
}
