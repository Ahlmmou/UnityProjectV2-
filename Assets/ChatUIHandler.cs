using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles all UI logic for the chat panel with modern dark theme styling
/// </summary>
public class ChatUIHandler : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("The TMP InputField component where the user types.")]
    public TMP_InputField chatInputField;

    [Tooltip("The Button component that sends the message.")]
    public Button sendButton;

    [Tooltip("The TextMeshPro Text component where chat output/responses appear.")]
    public TMP_Text chatOutputText;

    [Tooltip("The Scroll Rect to scroll the output text to the bottom.")]
    public ScrollRect scrollRect;

    // Internal state tracking
    private bool isChatActive = false;
    private bool isAITyping = false;

    // LLM API Configuration
    private const string GeminiModel = "gemini-2.5-flash-preview-09-2025";
    private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/" + GeminiModel + ":generateContent?key=";
    private const string ApiKey = "";

    void Start()
    {
        // Ensure components are linked
        if (chatInputField == null) Debug.LogError("ChatUIHandler: InputField is not assigned.");
        if (sendButton == null) Debug.LogError("ChatUIHandler: Send Button is not assigned.");
        if (chatOutputText == null) Debug.LogError("ChatUIHandler: Output Text is not assigned.");

        // --- ENTER KEY INTEGRATION ---
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(delegate { SendButton(); });
        }

        // Set initial mouse state to hidden/locked
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Apply modern UI styling
        ApplyModernStyle();
    }

    void Update()
    {
        if (isChatActive)
        {
            // Mouse visibility: Ensure cursor is unlocked
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Robust Input Focus Check
            if (chatInputField != null && chatInputField.gameObject.activeInHierarchy && !chatInputField.isFocused)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }
    }
    
    /// <summary>
    /// Applies modern dark theme with glassmorphism and vibrant accents
    /// </summary>
    private void ApplyModernStyle()
    {
        Sprite roundedSprite = CreateRoundedSprite();
        
        // Style Chat Output Area
        if (chatOutputText != null)
        {
            Transform parent = chatOutputText.transform.parent;
            if (parent != null)
            {
                Image outputBg = parent.GetComponent<Image>();
                if (outputBg == null) outputBg = parent.gameObject.AddComponent<Image>();
                outputBg.sprite = roundedSprite;
                outputBg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            }
            
            chatOutputText.color = new Color(0.95f, 0.95f, 0.95f);
            chatOutputText.fontSize = Mathf.Max(chatOutputText.fontSize, 16);
        }
        
        // Style ScrollRect
        if (scrollRect != null)
        {
            Image scrollBg = scrollRect.GetComponent<Image>();
            if (scrollBg == null) scrollBg = scrollRect.gameObject.AddComponent<Image>();
            scrollBg.sprite = roundedSprite;
            scrollBg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        }
        
        // Style Input Field
        if (chatInputField != null)
        {
            Image inputBg = chatInputField.GetComponent<Image>();
            if (inputBg == null) inputBg = chatInputField.gameObject.AddComponent<Image>();
            inputBg.sprite = roundedSprite;
            inputBg.color = new Color(0.2f, 0.2f, 0.25f, 1.0f);
            
            if (chatInputField.textComponent != null)
            {
                chatInputField.textComponent.color = Color.white;
                chatInputField.textComponent.fontSize = Mathf.Max(chatInputField.textComponent.fontSize, 16);
            }
            
            if (chatInputField.placeholder != null && chatInputField.placeholder is TMP_Text placeholder)
            {
                placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                placeholder.text = "Type your message...";
            }
        }
        
        // Style Send Button
        if (sendButton != null)
        {
            Image btnBg = sendButton.GetComponent<Image>();
            if (btnBg == null) btnBg = sendButton.gameObject.AddComponent<Image>();
            btnBg.sprite = roundedSprite;
            btnBg.color = new Color(0.0f, 0.5f, 1.0f, 1.0f); // Vibrant Blue
            
            TMP_Text btnText = sendButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.color = Color.white;
                btnText.fontStyle = FontStyles.Bold;
                btnText.fontSize = Mathf.Max(btnText.fontSize, 16);
            }
        }
    }
    
    private Sprite CreateRoundedSprite()
    {
        int size = 128;
        int radius = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color[] colors = new Color[size * size];
        Color c = Color.white;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inCorner = false;
                float dist = 0;
                
                if (x < radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    inCorner = true;
                }
                else if (x > size - radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, radius));
                    inCorner = true;
                }
                else if (x < radius && y > size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius));
                    inCorner = true;
                }
                else if (x > size - radius && y > size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, size - radius));
                    inCorner = true;
                }
                
                if (inCorner)
                {
                    if (dist <= radius - 1)
                        colors[y * size + x] = c;
                    else if (dist <= radius)
                        colors[y * size + x] = new Color(c.r, c.g, c.b, c.a * (radius - dist));
                    else
                        colors[y * size + x] = Color.clear;
                }
                else
                {
                    colors[y * size + x] = c;
                }
            }
        }
        
        tex.SetPixels(colors);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    public void ToggleInputFocus(bool active)
    {
        isChatActive = active;

        if (active)
        {
            if (chatInputField != null)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }
        else
        {
            if (chatInputField != null)
            {
                chatInputField.DeactivateInputField();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SendButton()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text) || isAITyping)
            return;

        string userMessage = chatInputField.text;

        DisplayMessage("You", userMessage, new Color(0.3f, 0.65f, 1.0f));
        chatInputField.text = "";

        if (chatInputField.transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        chatInputField.ActivateInputField();
        chatInputField.Select();

        StartCoroutine(CallGeminiAPI(userMessage));
    }

    private IEnumerator CallGeminiAPI(string prompt)
    {
        isAITyping = true;
        DisplayMessage("Bot", "Thinking...", new Color(0.0f, 1.0f, 0.6f), true);

        string apiUrl = GeminiUrl + ApiKey;
        string payloadJson = JsonUtility.ToJson(new RequestPayload(prompt));
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payloadJson);

        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.PostWwwForm(apiUrl, "");
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        int maxRetries = 3;
        float delay = 1f;

        for (int i = 0; i < maxRetries; i++)
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                HandleGeminiResponse(responseText);
                isAITyping = false;
                yield break;
            }
            else if (request.responseCode == 429)
            {
                Debug.LogWarning($"API rate limit hit. Retrying in {delay} seconds...");
                yield return new WaitForSeconds(delay);
                delay *= 2;
            }
            else
            {
                RemoveLastMessage();
                DisplayMessage("Bot", $"[Error] {request.error}", new Color(1f, 0.3f, 0.3f));
                isAITyping = false;
                yield break;
            }
        }

        RemoveLastMessage();
        DisplayMessage("Bot", "[Error] API call failed after retries.", new Color(1f, 0.3f, 0.3f));
        isAITyping = false;
    }

    private void HandleGeminiResponse(string jsonResponse)
    {
        RemoveLastMessage();

        try
        {
            ResponsePayload response = JsonUtility.FromJson<ResponsePayload>(jsonResponse);
            string generatedText = response.candidates[0].content.parts[0].text;
            DisplayMessage("Bot", generatedText, new Color(0.0f, 1.0f, 0.6f));
        }
        catch (System.Exception e)
        {
            DisplayMessage("Bot", $"[Error] Failed to parse: {e.Message}", new Color(1f, 0.3f, 0.3f));
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void DisplayMessage(string sender, string message, Color senderColor, bool isStatus = false)
    {
        if (chatOutputText != null)
        {
            string prefix = isStatus ? "" : "\n";
            string colorHex = ColorUtility.ToHtmlStringRGB(senderColor);
            chatOutputText.text += $"{prefix}<color=#{colorHex}><b>{sender}:</b></color> {message}";
            
            if (chatOutputText.rectTransform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatOutputText.rectTransform.parent.GetComponent<RectTransform>());
            }
        }
    }

    private void RemoveLastMessage()
    {
        if (chatOutputText != null && !string.IsNullOrEmpty(chatOutputText.text))
        {
            string text = chatOutputText.text;
            int lastNewline = text.LastIndexOf('\n');
            chatOutputText.text = lastNewline >= 0 ? text.Substring(0, lastNewline) : "";
            
            if (chatOutputText.rectTransform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatOutputText.rectTransform.parent.GetComponent<RectTransform>());
            }
        }
    }

    // JSON Utility Classes
    [System.Serializable]
    private class RequestPayload
    {
        public Content[] contents;
        public Tools[] tools = new Tools[] { new Tools() };

        public RequestPayload(string userQuery)
        {
            contents = new Content[] {
                new Content {
                    parts = new Part[] { new Part { text = userQuery } }
                }
            };
        }
    }

    [System.Serializable]
    private class Tools
    {
        public GoogleSearch google_search = new GoogleSearch();
    }

    [System.Serializable]
    private class GoogleSearch { }

    [System.Serializable]
    private class Content
    {
        public string role = "user";
        public Part[] parts;
    }

    [System.Serializable]
    private class Part
    {
        public string text;
    }

    [System.Serializable]
    private class ResponsePayload
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    private class Candidate
    {
        public Content content;
    }
}