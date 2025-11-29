using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class GeminiChatbotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textField;        // Chat log text
    [SerializeField] private TMP_InputField inputField; // Where you type
    [SerializeField] private Button send_button;        // Send button

    [Header("Gemini")]
    [SerializeField] private string apiKey = "AIzaSyD_r0Md5nzrywRHsGpwTw7B4vKmZMpI6Iw";
    [SerializeField] private string model = "gemini-2.5-flash";

    private string url;
    private bool isSending = false;

    void Start()
    {
        // Build the correct REST endpoint URL
        url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        Debug.Log($"[GeminiChatbotUI] Using URL: {url}");

        // Check connections
        if (textField == null) Debug.LogError("[GeminiChatbotUI] textField not assigned");
        if (inputField == null) Debug.LogError("[GeminiChatbotUI] inputField not assigned");

        // Initial message
        if (textField != null)
            textField.text = "I am your Unity robot helper!";

        // Button listener
        if (send_button != null)
            send_button.onClick.AddListener(OnSendClicked);
        else
            Debug.LogError("[GeminiChatbotUI] send_button not assigned");
    }

    void Update()
    {
        // ENTER key sends message if the input field is focused
        if (gameObject.activeInHierarchy &&
            inputField != null &&
            inputField.isFocused &&
            Input.GetKeyDown(KeyCode.Return))
        {
            OnSendClicked();
        }
    }

    public void OnSendClicked()
    {
        if (isSending || inputField == null)
            return;

        string userText = inputField.text.Trim();
        if (string.IsNullOrEmpty(userText))
            return;

        StartCoroutine(SendMessageRoutine(userText));
    }

    private IEnumerator SendMessageRoutine(string userText)
    {
        isSending = true;
        if (send_button != null) send_button.interactable = false;

        // Show user message + thinking status
        if (textField != null)
            textField.text = $"You: {userText}\n\nAI: thinking...";

        inputField.text = "";

        // --- Build JSON payload in current Gemini format ---
        JObject json = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["role"] = "user",
                    ["parts"] = new JArray
                    {
                        new JObject { ["text"] = userText }
                    }
                }
            }
        };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString());

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // Send request
            yield return req.SendWebRequest();

            string reply = "";

            if (req.result != UnityWebRequest.Result.Success)
            {
                reply = $"Error: {req.error}";
                Debug.LogError($"[GeminiChatbotUI] Net Error: {req.error}\nURL: {url}\nResponse: {req.downloadHandler.text}");
            }
            else
            {
                string raw = req.downloadHandler.text;
                Debug.Log($"[GeminiChatbotUI] Raw response: {raw}");

                try
                {
                    JObject response = JObject.Parse(raw);
                    var candidates = response["candidates"];

                    if (candidates != null && candidates.HasValues &&
                        candidates[0]["content"] != null &&
                        candidates[0]["content"]["parts"] != null &&
                        candidates[0]["content"]["parts"].HasValues &&
                        candidates[0]["content"]["parts"][0]["text"] != null)
                    {
                        reply = candidates[0]["content"]["parts"][0]["text"].ToString();
                    }
                    else
                    {
                        reply = "The AI declined to answer or returned no text.";
                        Debug.LogWarning($"[GeminiChatbotUI] No valid text found in response: {raw}");
                    }
                }
                catch (System.Exception e)
                {
                    reply = "Error parsing AI response.";
                    Debug.LogError($"[GeminiChatbotUI] Parse Error: {e.Message}\nResponse: {req.downloadHandler.text}");
                }
            }

            // Update UI with final reply
            if (textField != null)
                textField.text = $"You: {userText}\n\nAI: {reply}";
        }

        // Re-enable controls
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }

        isSending = false;
        if (send_button != null) send_button.interactable = true;
    }

    // --- REQUIRED BY CHATPANELTOGGLE / OTHER SCRIPTS ---
    public void FocusInput()
    {
        if (inputField != null && gameObject.activeInHierarchy)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }
}
