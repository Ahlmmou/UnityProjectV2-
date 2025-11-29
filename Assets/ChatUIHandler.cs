using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // Needed for IEnumerator for async API calls

/// <summary>
/// Handles all UI logic for the chat panel, including input focus, 
/// sending messages via the Enter key, and clearing the input field on send.
/// NOTE: This script should be placed on a persistent GameObject (like UIManager)
/// that remains active when the chat panel is hidden.
/// </summary>
public class ChatUIHandler : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("The TMP InputField component where the user types.")]
    public TMP_InputField chatInputField;

    [Tooltip("The Button component that sends the message (usually linked to the SendButton() method).")]
    public Button sendButton;

    [Tooltip("The TextMeshPro Text component where chat output/responses appear.")]
    public TMP_Text chatOutputText;

    [Tooltip("The Scroll Rect to scroll the output text to the bottom.")]
    public ScrollRect scrollRect;

    // Internal state tracking
    private bool isChatActive = false;
    private bool isAITyping = false; // Prevents sending multiple prompts while waiting

    // LLM API Configuration
    private const string GeminiModel = "gemini-2.5-flash-preview-09-2025";
    private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/" + GeminiModel + ":generateContent?key=";
    private const string ApiKey = ""; // Canvas will provide this at runtime

    void Start()
    {
        // Ensure components are linked
        if (chatInputField == null) Debug.LogError("ChatUIHandler: InputField is not assigned.");
        if (sendButton == null) Debug.LogError("ChatUIHandler: Send Button is not assigned.");
        if (chatOutputText == null) Debug.LogError("ChatUIHandler: Output Text is not assigned.");

        // --- ENTER KEY INTEGRATION ---
        if (chatInputField != null)
        {
            // CRITICAL: This executes SendButton() when the user presses 'Enter' or 'Return' while typing.
            chatInputField.onSubmit.AddListener(delegate { SendButton(); });
        }

        // Set initial mouse state to hidden/locked (standard for 3D navigation)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- CRITICAL FIX FOR TYPING: PERSISTENT FOCUS ---
    void Update()
    {
        if (isChatActive)
        {
            // 1. Mouse visibility: Ensure cursor is unlocked to click the Send button
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 2. Robust Input Focus Check: If the field is active but not focused, re-focus it.
            // This loop is what ensures persistence (stickiness) after any click or activation.
            if (chatInputField != null && chatInputField.gameObject.activeInHierarchy && !chatInputField.isFocused)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }
    }

    /// <summary>
    /// Toggles input focus and mouse visibility, called by ProximityChatActivator.
    /// </summary>
    /// <param name="active">True to activate chat, False to deactivate.</param>
    public void ToggleInputFocus(bool active)
    {
        isChatActive = active;

        if (active)
        {
            // 1. Give Keyboard Focus immediately.
            // The Update loop will continuously re-select it, ensuring persistence.
            if (chatInputField != null)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }

            // 2. Show cursor for clicking the Send button (handled by Update)
        }
        else
        {
            // 1. Clear focus and hide cursor to return control to the player/game
            if (chatInputField != null)
            {
                chatInputField.DeactivateInputField();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// This method is linked to the Send Button's OnClick event AND the InputField's onSubmit event (Enter key).
    /// </summary>
    public void SendButton()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text) || isAITyping)
        {
            return; // Don't send empty messages or if AI is busy
        }

        string userMessage = chatInputField.text;

        // --- 1. Display Message and Clear Input ---
        DisplayMessage("User", userMessage);
        chatInputField.text = ""; // Clear input field

        // Ensure the input field's RectTransform is rebuilt, which sometimes fixes blocking issues
        if (chatInputField.transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        // Re-focus the input field immediately after clearing to allow continuous typing
        chatInputField.ActivateInputField();
        chatInputField.Select();

        // --- 2. Call Gemini API ---
        StartCoroutine(CallGeminiAPI(userMessage));
    }

    private IEnumerator CallGeminiAPI(string prompt)
    {
        isAITyping = true;
        // Show "Thinking" status before API call
        DisplayMessage("Gemini", "Thinking...", true);

        string apiUrl = GeminiUrl + ApiKey;

        // Construct the payload
        string payloadJson = JsonUtility.ToJson(new RequestPayload(prompt));

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payloadJson);

        // Use UnityWebRequest for the API call
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
                // Successful API call
                string responseText = request.downloadHandler.text;
                HandleGeminiResponse(responseText);
                isAITyping = false;
                yield break;
            }
            else if (request.responseCode == 429) // Too Many Requests - Retry with exponential backoff
            {
                Debug.LogWarning($"API rate limit hit. Retrying in {delay} seconds...");
                yield return new WaitForSeconds(delay);
                delay *= 2; // Exponential backoff
            }
            else
            {
                // Other errors (400, 500 etc.)
                RemoveLastMessage(); // Remove "Thinking..."
                DisplayMessage("Gemini", $"[Error] API failed: {request.error} | Response: {request.downloadHandler.text}");
                isAITyping = false;
                yield break;
            }
        }

        // If all retries fail
        RemoveLastMessage(); // Remove "Thinking..."
        DisplayMessage("Gemini", "[Error] API call failed after multiple retries.");
        isAITyping = false;
    }

    private void HandleGeminiResponse(string jsonResponse)
    {
        // Simple JSON parsing to get the text content
        // This assumes a standard Gemini response structure
        // For simplicity, we search for the content part.

        RemoveLastMessage(); // Remove "Thinking..."

        try
        {
            ResponsePayload response = JsonUtility.FromJson<ResponsePayload>(jsonResponse);
            string generatedText = response.candidates[0].content.parts[0].text;
            DisplayMessage("Gemini", generatedText);
        }
        catch (System.Exception e)
        {
            DisplayMessage("Gemini", $"[Error] Failed to parse API response: {e.Message}");
            Debug.LogError("Failed JSON Response: " + jsonResponse);
        }

        // Scroll to the bottom after the final message is added
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Appends a message to the chat output display.
    /// </summary>
    /// <param name="isStatus">If true, does not add a newline (used for 'Thinking...').</param>
    private void DisplayMessage(string sender, string message, bool isStatus = false)
    {
        if (chatOutputText != null)
        {
            string prefix = isStatus ? "" : "\n";
            chatOutputText.text += $"{prefix}<b>{sender}:</b> {message}";
            // Force the layout rebuild when text changes for ScrollRect to work correctly
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatOutputText.rectTransform.parent.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Removes the last line of text, specifically used to delete the "Thinking..." status.
    /// This is a simple implementation assuming "Thinking..." is always the last line.
    /// </summary>
    private void RemoveLastMessage()
    {
        if (chatOutputText != null && !string.IsNullOrEmpty(chatOutputText.text))
        {
            string text = chatOutputText.text;
            int lastNewline = text.LastIndexOf('\n');
            if (lastNewline >= 0)
            {
                chatOutputText.text = text.Substring(0, lastNewline);
            }
            else
            {
                chatOutputText.text = ""; // Clear if it's the only line
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatOutputText.rectTransform.parent.GetComponent<RectTransform>());
        }
    }

    // --- JSON Utility Classes for API ---

    [System.Serializable]
    private class RequestPayload
    {
        public Content[] contents;
        public Tools[] tools = new Tools[] { new Tools() }; // Enable Google Search grounding by default

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

    // --- JSON Response Classes ---

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