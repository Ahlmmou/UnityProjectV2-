using UnityEngine;
using Assets.Scripts;

public class ChatPanelToggle : MonoBehaviour
{
    [Header("Chat References")]
    public GameObject chatPanel;          // The panel with GeminiChatbotUI
    public GeminiChatbotUI chatbotUI;     // Reference to script above

    [Header("Robot Control")]
    public AB_RobotController robotController; // Drag your robot controller here

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.X; // X opens AND closes chat

    void Start()
    {
        if (chatPanel != null)
            chatPanel.SetActive(false);
    }

    void Update()
    {
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(toggleKey))
        {
            if (chatPanel != null && chatPanel.activeSelf)
                CloseChat();
            else
                OpenChat();
        }
    }

    void OpenChat()
    {
        if (chatPanel == null) return;
        chatPanel.SetActive(true);

        // Unlock cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable robot controls
        if (robotController != null)
            robotController.enabled = false;

        // Focus input field
        if (chatbotUI != null)
            chatbotUI.FocusInput();
    }

    void CloseChat()
    {
        if (chatPanel == null) return;
        chatPanel.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable robot controls
        if (robotController != null)
            robotController.enabled = true;
    }
}
