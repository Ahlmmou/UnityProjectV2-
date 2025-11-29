//using Google.GenerativeAI; // REQUIRED for the GeminiChat type
using System.Collections;
using TMPro; // REQUIRED for TextMesh Pro UI types
using Assets.Scripts;
using UnityEngine;

public class NPCInteractor : MonoBehaviour
{
    // --- Public References (Drag-and-Drop in Inspector) ---

    // References to other GameObjects/Scripts
    [Tooltip("The Player's Transform for proximity checking.")]
    public Transform player;

    [Tooltip("The script component that handles the Gemini API calls.")]
    public GeminiChat geminiChat;

    [Header("Chat UI Panel & TMP Components")]
    [Tooltip("The parent GameObject that holds the entire chat interface.")]
    public GameObject chatUIPanel;

    // TextMesh Pro components for the main chat
    public TMP_InputField inputField;
    public TextMeshProUGUI chatOutputText;

    [Header("Interaction Prompt")]
    public GameObject promptCanvas;
    public TextMeshProUGUI promptText;

    // --- Private State Variables ---
    private bool isPlayerInRange = false;
    private bool isChatActive = false;

    // --- Interaction Settings ---
    private const KeyCode INTERACT_KEY = KeyCode.X;

    void Start()
    {
        // Ensure the UI panels are hidden when the game starts
        if (chatUIPanel != null)
        {
            chatUIPanel.SetActive(false);
        }
        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Handle Proximity and Activation Prompt (If chat is NOT active)
        if (isPlayerInRange && !isChatActive)
        {
            // Show the floating prompt
            if (promptCanvas != null) promptCanvas.SetActive(true);

            // Check for the Interaction Keypress
            if (Input.GetKeyDown(INTERACT_KEY))
            {
                ActivateChat();
            }
        }
        else
        {
            // Hide the prompt if out of range OR chat is already active
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }

        // 2. Handle sending the message when the chat IS active
        if (isChatActive)
        {
            // Check for Enter/Return key press
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendMessage();
            }
        }

        // 3. Allow player to close the chat using the same key
        if (isChatActive && Input.GetKeyDown(INTERACT_KEY))
        {
            DeactivateChat();
        }
    }

    // --- Chat Management Methods ---

    void ActivateChat()
    {
        isChatActive = true;
        chatUIPanel.SetActive(true); // Show the main UI

        // Set the initial message and focus the input field
        chatOutputText.text = "Hello! I am the Robot NPC. What is your question?";
        inputField.ActivateInputField(); // Focus the input
    }

    void DeactivateChat()
    {
        isChatActive = false;
        chatUIPanel.SetActive(false); // Hide the main UI
    }

    public void SendMessage() // Made public so it can be called by a UI Button too
    {
        // Use .text from the TMP_InputField
        string userMessage = inputField.text;

        if (string.IsNullOrWhiteSpace(userMessage))
            return; // Don't send empty messages

        // 1. Clear input and show a waiting message
        inputField.text = "";

        // Display the user's message and a waiting message for the AI
        chatOutputText.text = $"You: {userMessage}\n\nRobot NPC: ...Thinking...";

        // 2. Start the API call using the Coroutine from the GeminiChat script
        // Note: We check if the geminiChat script is linked before calling
        if (geminiChat != null)
        {
            StartCoroutine(geminiChat.SendMessageToGemini(userMessage, OnGeminiResponseReceived));
        }
        else
        {
            chatOutputText.text = "Error: GeminiChat script not linked!";
        }
    }

    // Callback function passed to GeminiChat.SendMessageToGemini
    void OnGeminiResponseReceived(string reply)
    {
        // 3. Update the UI with the final response
        chatOutputText.text = $"Robot NPC: {reply}";
        inputField.ActivateInputField(); // Re-focus the input for the next message
    }

    // --- Proximity Detection ---

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the Player (you might need to check for a Player tag/component here)
        // For now, we compare the entered collider's transform to the player reference.
        if (other.transform == player)
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            isPlayerInRange = false;
            if (promptCanvas != null) promptCanvas.SetActive(false);

            // If the player walks away, close the chat UI
            if (isChatActive)
            {
                DeactivateChat();
            }
        }
    }
}