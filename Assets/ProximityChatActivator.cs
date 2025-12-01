using UnityEngine;
using TMPro; // Required to access the TMP_InputField component
using UnityEditor; // Required for MonoScript (only for Editor, but safe to leave here)

/// <summary>
/// ProximityChatActivator manages showing/hiding the chat UI based on player proximity
/// to this object and a key press, and handles switching between game control and UI input modes.
/// </summary>
public class ProximityChatActivator : MonoBehaviour
{
    [Header("UI & Input References")]
    [Tooltip("Drag the main parent GameObject of your Chat UI here (the one to enable/disable).")]
    public GameObject chatPanel;

    [Tooltip("Drag the TMP InputField component from your UI here.")]
    public TMP_InputField chatInputField;

    // NEW: Reference to the script that handles chat logic, Enter key, and API calls
    [Tooltip("Drag the ChatUIHandler script component from the Chat Panel GameObject here.")]
    public ChatUIHandler chatHandler;

    [Header("Player Control Hijack (Drag Script Asset & GameObject)")]
    [Tooltip("1. DRAG THE .CS FILE ASSET HERE: Drag the AB_RobotController.cs file from the Project window.")]
    public MonoScript robotControllerAsset;

    [Tooltip("2. DRAG THE PLAYER GAMEOBJECT HERE: Drag the Player Robot's root GameObject here.")]
    public GameObject playerRobotRoot;

    // Internal variable to hold the actual running component instance
    private MonoBehaviour robotControllerScript;

    [Header("Physics Setup")]
    [Tooltip("The tag of the player GameObject (e.g., 'Player').")]
    public string playerTag = "Player";

    [Tooltip("The key the player must press to open/close the UI when close.")]
    public KeyCode activationKey = KeyCode.X;

    // Internal state tracking whether the player is currently inside the trigger zone
    private bool isPlayerInRange = false;

    void Start()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("ProximityChatActivator: Chat Panel is missing.");
        }

        // --- 1. Runtime Component Lookup (using the dragged Asset) ---
        if (robotControllerAsset != null && playerRobotRoot != null)
        {
            // Use GetComponentInChildren to find the script component instance
            // We use GetClass() to get the System.Type from the MonoScript asset
            if (robotControllerAsset.GetClass() != null)
            {
                robotControllerScript = playerRobotRoot.GetComponentInChildren(robotControllerAsset.GetClass()) as MonoBehaviour;
            }

            if (robotControllerScript == null)
            {
                Debug.LogError($"ProximityChatActivator: Could not find the component of type '{robotControllerAsset.name}' on the Player Robot or its children. Check that the script is actually attached.");
            }
        }
        else
        {
            Debug.LogError("ProximityChatActivator: Both the Script Asset and Player Root GameObject must be assigned for movement toggling to work.");
        }

        // Check the new handler reference
        if (chatHandler == null)
        {
            Debug.LogError("ProximityChatActivator: Chat Handler is missing. Enter key and mouse control fix will fail.");
        }


        Collider collider = GetComponent<Collider>();
        if (collider == null || !collider.isTrigger)
        {
            Debug.LogError("ProximityChatActivator: This GameObject requires a Collider component set to 'Is Trigger'.");
        }
    }

    void Update()
    {
        // Continuous override logic is now handled in ChatUIHandler's Update to manage focus

        if (isPlayerInRange)
        {
            // Toggle chat visibility when the activation key is pressed
            if (chatPanel != null && chatInputField != null && Assets.Scripts.GlobalInputManager.GetKeyDown(activationKey))
            {
                SetChatActive(!chatPanel.activeSelf);
            }
        }
    }

    /// <summary>
    /// Handles enabling/disabling the UI panel, robot movement, and input focus.
    /// </summary>
    private void SetChatActive(bool active)
    {
        if (chatPanel == null || chatInputField == null) return;

        chatPanel.SetActive(active);

        if (active)
        {
            // 1. Give Keyboard Focus (Now delegated to ChatUIHandler)
            if (chatHandler != null) chatHandler.ToggleInputFocus(true);

            // 2. Disable Robot Movement
            if (robotControllerScript != null)
            {
                robotControllerScript.enabled = false;
                Debug.Log("Robot controls disabled.");
            }
        }
        else
        {
            // 1. Clear Focus (Now delegated to ChatUIHandler)
            if (chatHandler != null) chatHandler.ToggleInputFocus(false);

            // 2. Enable Robot Movement
            if (robotControllerScript != null)
            {
                robotControllerScript.enabled = true;
                Debug.Log("Robot controls enabled.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = true;
            Debug.Log("Player entered chat range. Press 'X' to open chat.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = false;
            // Force-hide the chat panel and resume movement if the player walks away
            if (chatPanel != null && chatPanel.activeSelf)
            {
                SetChatActive(false);
            }
            Debug.Log("Player left chat range.");
        }
    }
}