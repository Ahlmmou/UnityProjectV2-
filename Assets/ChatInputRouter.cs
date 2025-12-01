using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts;

public class ChatInputRouter : MonoBehaviour
{
    [Header("Chat UI Panel")]
    [Tooltip("Drag the panel GameObject that becomes active/inactive when chat opens.")]
    public GameObject chatPanel;

    [Header("Chatbot UI (optional)")]
    [Tooltip("If assigned, the router will auto-focus the input field when chat opens.")]
    public GeminiChatbotUI chatUI;

    [Header("Scripts to Disable While Chat Is Open")]
    [Tooltip("Drag any script here that reads keyboard input (AB_RobotController, player movement, camera controls, etc.).")]
    public MonoBehaviour[] scriptsToDisable;

    private bool wasChatOpen = false;

    void Start()
    {
        if (chatPanel == null)
        {
            Debug.LogError("[ChatInputRouter] ERROR: Chat Panel is NOT assigned!");
            return;
        }

        // In case chat starts open
        wasChatOpen = chatPanel.activeSelf;
        if (wasChatOpen)
            DisableGameplayInput();
    }

    void Update()
    {
        if (chatPanel == null) return;

        bool isOpen = chatPanel.activeSelf;

        // Detect open/close state changes
        if (isOpen != wasChatOpen)
        {
            if (isOpen)
                DisableGameplayInput();
            else
                EnableGameplayInput();

            wasChatOpen = isOpen;
        }
    }

    private void DisableGameplayInput()
    {
        Debug.Log("[ChatInputRouter] Chat OPEN → Disabling listed scripts.");

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null && script.enabled)
                script.enabled = false;
        }

        // Auto-focus the input field if provided
        if (chatUI != null)
            chatUI.FocusInput();
    }

    private void EnableGameplayInput()
    {
        Debug.Log("[ChatInputRouter] Chat CLOSED → Re-enabling listed scripts.");

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null && !script.enabled)
                script.enabled = true;
        }

        // Clear UI selection so gameplay input resumes cleanly
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
