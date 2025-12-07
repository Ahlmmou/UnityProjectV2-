using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts;   // to see GeminiChat

public class ChatUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject chatPanel;        // whole panel (enable/disable this)
    public TMP_InputField inputField;   // where you type
    public TMP_Text outputText;         // where messages appear

    [Header("Keys")]
    public KeyCode toggleChatKey = KeyCode.X;   // X opens & closes chat
    public KeyCode sendKey = KeyCode.Return;     // ENTER to send

    [Header("Agent")]
    public GeminiChat geminiChat;        // drag object with GeminiChat script

    private AB_RobotController robotController;  // robot controller reference
    private bool isOpen = false;
    private bool isSending = false;

    void Start()
    {
        if (chatPanel != null)
            chatPanel.SetActive(false);

        // Find the robot controller to disable it while typing
        robotController = FindObjectOfType<AB_RobotController>();
    }

    void Update()
    {
        // --- X toggles chat open / close ---
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(toggleChatKey))
        {
            if (isOpen) CloseChat();
            else OpenChat();
        }

        if (!isOpen) return;

        // --- ENTER sends message ---
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(sendKey))
        {
            TrySendMessage();
        }
    }

    // Called when near NPC & X pressed
    public void OpenChat()
    {
        if (chatPanel == null) return;

        isOpen = true;
        chatPanel.SetActive(true);

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable robot movement while typing
        if (robotController != null)
            robotController.enabled = false;

        StartCoroutine(FocusInputNextFrame());
    }

    public void CloseChat()
    {
        if (chatPanel == null) return;

        isOpen = false;
        chatPanel.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable robot movement
        if (robotController != null)
            robotController.enabled = true;

        // Clear UI selection
        EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (inputField != null && inputField.gameObject.activeInHierarchy)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    private void TrySendMessage()
    {
        if (inputField == null || geminiChat == null) return;
        if (isSending) return;

        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // Show user's message
        AppendLine($"<b>You:</b> {msg}");
        inputField.text = "";

        // Keep input focus
        inputField.ActivateInputField();
        inputField.Select();

        StartCoroutine(SendToGeminiRoutine(msg));
    }

    private IEnumerator SendToGeminiRoutine(string msg)
    {
        isSending = true;
        AppendLine("<b>Bot:</b> thinking...");

        // Use the new API with callbacks
        geminiChat.SendMessage(msg, 
            onComplete: (response) => {
                // Replace "thinking..." with the actual bot message
                ReplaceLastLine($"<b>Bot:</b> {response}");
                isSending = false;
                
                // Restore focus
                inputField.ActivateInputField();
                inputField.Select();
            },
            onError: (error) => {
                ReplaceLastLine($"<b>Bot:</b> Error: {error}");
                isSending = false;
                
                // Restore focus
                inputField.ActivateInputField();
                inputField.Select();
            }
        );
        
        yield break; // No need to wait, callbacks handle the response
    }

    private void AppendLine(string line)
    {
        if (outputText == null) return;

        if (string.IsNullOrEmpty(outputText.text))
            outputText.text = line;
        else
            outputText.text += "\n" + line;
    }

    private void ReplaceLastLine(string newLine)
    {
        if (outputText == null || string.IsNullOrEmpty(outputText.text))
        {
            outputText.text = newLine;
            return;
        }

        string text = outputText.text;
        int lastNewLine = text.LastIndexOf('\n');

        if (lastNewLine >= 0)
            text = text.Substring(0, lastNewLine);

        outputText.text = text + "\n" + newLine;
    }
    
    /// <summary>
    /// Check if this chat UI's input field is currently active
    /// </summary>
    public bool IsInputActive()
    {
        return isOpen && inputField != null && inputField.isFocused;
    }
}
