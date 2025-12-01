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
        
        // Apply modern styling
        ApplyModernStyle();
    }
    
    /// <summary>
    /// Applies modern dark theme with glassmorphism and vibrant colors
    /// </summary>
    private void ApplyModernStyle()
    {
        if (chatUIPanel == null) return;
        
        // Create rounded sprite
        Sprite roundedSprite = CreateRoundedSprite();
        
        // Style the main chat panel
        UnityEngine.UI.Image panelBg = chatUIPanel.GetComponent<UnityEngine.UI.Image>();
        if (panelBg == null) panelBg = chatUIPanel.AddComponent<UnityEngine.UI.Image>();
        panelBg.sprite = roundedSprite;
        panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f); // Deep dark background
        
        // Style the output text area
        if (chatOutputText != null)
        {
            UnityEngine.UI.Image outputBg = chatOutputText.GetComponent<UnityEngine.UI.Image>();
            if (outputBg == null && chatOutputText.transform.parent != null)
            {
                outputBg = chatOutputText.transform.parent.GetComponent<UnityEngine.UI.Image>();
                if (outputBg == null) outputBg = chatOutputText.transform.parent.gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            if (outputBg != null)
            {
                outputBg.sprite = roundedSprite;
                outputBg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            }
            
            chatOutputText.color = new Color(0.95f, 0.95f, 0.95f);
            chatOutputText.fontSize = Mathf.Max(chatOutputText.fontSize, 16);
        }
        
        // Style the input field
        if (inputField != null)
        {
            UnityEngine.UI.Image inputBg = inputField.GetComponent<UnityEngine.UI.Image>();
            if (inputBg == null) inputBg = inputField.gameObject.AddComponent<UnityEngine.UI.Image>();
            inputBg.sprite = roundedSprite;
            inputBg.color = new Color(0.2f, 0.2f, 0.25f, 1.0f);
            
            if (inputField.textComponent != null)
            {
                inputField.textComponent.color = Color.white;
                inputField.textComponent.fontSize = Mathf.Max(inputField.textComponent.fontSize, 16);
            }
            
            if (inputField.placeholder != null && inputField.placeholder is TMP_Text placeholder)
            {
                placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                placeholder.text = "Type your message...";
            }
        }
        
        // Find and style the Send button if it exists
        UnityEngine.UI.Button sendBtn = chatUIPanel.GetComponentInChildren<UnityEngine.UI.Button>();
        if (sendBtn != null)
        {
            UnityEngine.UI.Image btnBg = sendBtn.GetComponent<UnityEngine.UI.Image>();
            if (btnBg == null) btnBg = sendBtn.gameObject.AddComponent<UnityEngine.UI.Image>();
            btnBg.sprite = roundedSprite;
            btnBg.color = new Color(0.0f, 0.5f, 1.0f, 1.0f); // Vibrant Blue
            
            TMP_Text btnText = sendBtn.GetComponentInChildren<TMP_Text>();
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

    void Update()
    {
        // 1. Handle Proximity and Activation Prompt (If chat is NOT active)
        if (isPlayerInRange && !isChatActive)
        {
            // Show the floating prompt
            if (promptCanvas != null) promptCanvas.SetActive(true);

            // Check for the Interaction Keypress
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(INTERACT_KEY))
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
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Return) || Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendChatMessage(); // Renamed to avoid conflict with Unity's SendMessage
            }
        }

        // 3. Allow player to close the chat using the same key
        if (isChatActive && Assets.Scripts.GlobalInputManager.GetKeyDown(INTERACT_KEY))
        {
            DeactivateChat();
        }
    }

    // --- Chat Management Methods ---

    void ActivateChat()
    {
        isChatActive = true;
        chatUIPanel.SetActive(true); // Show the main UI

        // Set the initial message with modern colors
        string colorHex = ColorUtility.ToHtmlStringRGB(new Color(0.0f, 1.0f, 0.6f));
        chatOutputText.text = $"<color=#{colorHex}><b>Robot NPC:</b></color> Hello! I am your VR assistant. How can I help you today?";
        inputField.ActivateInputField(); // Focus the input
    }

    void DeactivateChat()
    {
        isChatActive = false;
        chatUIPanel.SetActive(false); // Hide the main UI
    }

    public void SendChatMessage() // Renamed from SendMessage to avoid Unity conflicts
    {
        // Use .text from the TMP_InputField
        string userMessage = inputField.text;

        if (string.IsNullOrWhiteSpace(userMessage))
            return; // Don't send empty messages

        // 1. Clear input and show a waiting message
        inputField.text = "";

        // Display the user's message with vibrant blue color
        string userColorHex = ColorUtility.ToHtmlStringRGB(new Color(0.3f, 0.65f, 1.0f));
        string botColorHex = ColorUtility.ToHtmlStringRGB(new Color(0.0f, 1.0f, 0.6f));
        chatOutputText.text = $"<color=#{userColorHex}><b>You:</b></color> {userMessage}\n\n<color=#{botColorHex}><b>Robot NPC:</b></color> Thinking...";

        // 2. Use the new GeminiChat.SendMessage API
        if (geminiChat != null)
        {
            geminiChat.SendMessage(userMessage, 
                onComplete: OnGeminiResponseReceived,
                onError: (error) => {
                    string errorColorHex = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.3f, 0.3f));
                    chatOutputText.text = $"<color=#{errorColorHex}><b>Error:</b></color> {error}";
                    inputField.ActivateInputField();
                }
            );
        }
        else
        {
            string errorColorHex = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.3f, 0.3f));
            chatOutputText.text = $"<color=#{errorColorHex}><b>Error:</b></color> GeminiChat script not linked!";
        }
    }

    // Callback function for when response is received
    void OnGeminiResponseReceived(string reply)
    {
        // 3. Update the UI with the final response
        string botColorHex = ColorUtility.ToHtmlStringRGB(new Color(0.0f, 1.0f, 0.6f));
        chatOutputText.text = $"<color=#{botColorHex}><b>Robot NPC:</b></color> {reply}";
        inputField.ActivateInputField(); // Re-focus the input for the next message
    }

    // --- Proximity Detection ---

    private void OnTriggerEnter(Collider other)
    {
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

            // NOTE: Chat will NOT auto-close when player moves away
            // User must manually press X to close the chat interface
            // This prevents accidental closure while typing (WASD movement keys)
        }
    }

    /// <summary>
    /// Check if the chat input field is currently focused.
    /// Used by GlobalInputManager to block other inputs while typing.
    /// </summary>
    public bool IsInputActive()
    {
        return isChatActive && inputField != null && inputField.isFocused;
    }
}
