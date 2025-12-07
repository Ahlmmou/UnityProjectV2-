using Assets.Scripts;
using UnityEngine;

public class ChatbotActivator : MonoBehaviour
{
    // Assign your player's Transform here in the Inspector
    public Transform player;
    // Assign the main Chat UI Panel here
    public GameObject chatPanel;
    // The GeminiChat script instance
    public GeminiChat geminiChat;

    // Adjust this value in the Inspector to define the interaction range
    public float interactionDistance = 3.0f;
    // The key the user must press to activate the chat
    public KeyCode activationKey = KeyCode.X;

    private bool isPlayerNearby = false;

    void Start()
    {
        // Ensure the chat panel is hidden at the start
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        // Optional: Auto-find player if not assigned (requires player to be tagged "Player")
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Calculate the distance between the object and the player
        float distance = Vector3.Distance(transform.position, player.position);

        // Check if the player is within the interaction distance
        if (distance <= interactionDistance)
        {
            // Player is nearby
            isPlayerNearby = true;
            // Optionally show a prompt like "Press X to chat" (requires a UI Text element)

            // Check for the activation key press
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(activationKey))
            {
                // Toggle the chat panel visibility
                if (chatPanel != null)
                {
                    bool isPanelActive = chatPanel.activeSelf;
                    chatPanel.SetActive(!isPanelActive);
                    // Optional: You might want to pause the game or lock player movement
                }
            }
        }
        else
        {
            // Player is outside the distance
            if (isPlayerNearby)
            {
                // Player just moved away, hide the panel
                if (chatPanel != null)
                {
                    chatPanel.SetActive(false);
                }
                isPlayerNearby = false;
            }
        }
    }

    // You can also use OnTriggerEnter/Exit with a large sphere collider set to 'Is Trigger'
    // for proximity, but Vector3.Distance is often simpler for a single interaction point.
}