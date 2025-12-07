using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// State-of-the-art UI for Gemini chatbot with streaming text animation
    /// </summary>
    public class GeminiChatbotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel; // Optional: Panel to enable/disable for chat toggle
    [SerializeField] private TMP_Text chatDisplay;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button voiceInputButton; // Voice input button
    [SerializeField] private Button clearHistoryButton;
    [SerializeField] private ScrollRect scrollRect; // For scrolling the chat display
    [SerializeField] private TMP_Text recordingIndicator; // Optional: shows "Recording..." text
    
    [Header("Chatbot Reference")]
    [SerializeField] private GeminiChat geminiChat;
    [SerializeField] private VoiceInputManager voiceInputManager; // Voice input manager
    
    [Header("Chat Toggle Settings (Optional)")]
    [Tooltip("Enable chat panel toggle with keyboard shortcut")]
    [SerializeField] private bool enableChatToggle = true;
    [SerializeField] private KeyCode toggleChatKey = KeyCode.X;
    [SerializeField] private KeyCode sendKey = KeyCode.Return;
    [Tooltip("Disable robot controller while chat is open")]
    [SerializeField] private bool disableRobotWhileTyping = true;
    
    [Header("UI Settings")]
    [SerializeField] private float typingSpeed = 0.01f;
    [SerializeField] private string userPrefix = "You: ";
    [SerializeField] private string aiPrefix = "AI: ";
    [SerializeField] private Color userColor = new Color(0.3f, 0.65f, 1.0f); // Vibrant Blue
    [SerializeField] private Color aiColor = new Color(0.0f, 1.0f, 0.6f);   // Neon Green
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f); // Soft Red
    
    private StringBuilder chatHistory = new StringBuilder();
    private Coroutine typingCoroutine;
    private string currentUserMessage;
    private bool isTyping = false;
    
    // Voice input state
    private bool isRecording = false;
    private Color originalVoiceButtonColor;
    private Coroutine recordingAnimationCoroutine;
    
    // Chat toggle state
    private bool isOpen = false; // Start closed by default
    private AB_RobotController robotController;
    
    void Start()
    {
        // Auto-discover references if missing
        if (chatDisplay == null)
        {
            chatDisplay = GetComponentInChildren<TMP_Text>();
            if (chatDisplay == null)
                Debug.LogError("[GeminiChatbotUI] Chat display text not assigned and could not be found in children!");
        }
        
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            if (inputField == null)
                Debug.LogError("[GeminiChatbotUI] Input field not assigned and could not be found in children!");
        }
        
        if (geminiChat == null)
        {
            geminiChat = GetComponent<GeminiChat>();
            if (geminiChat == null)
                Debug.LogError("[GeminiChatbotUI] GeminiChat component not assigned and could not be found on this GameObject!");
        }

        // Auto-discover VoiceInputManager if not assigned
        if (voiceInputManager == null)
        {
            voiceInputManager = GetComponent<VoiceInputManager>();
            if (voiceInputManager == null)
            {
                // Try to find in scene
                voiceInputManager = FindObjectOfType<VoiceInputManager>();
            }
            
            if (voiceInputManager == null)
            {
                Debug.LogWarning("[GeminiChatbotUI] VoiceInputManager not found. Voice input will not be available.");
            }
        }

        // Auto-discover ScrollRect if not assigned
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect == null)
                Debug.LogWarning("[GeminiChatbotUI] ScrollRect not found. Scrolling may not work properly.");
        }

        // Validate references
        if (chatDisplay == null || inputField == null || geminiChat == null)
        {
            return;
        }
        
        // Configure chat display for scrolling
        ConfigureChatDisplayForScrolling();
        
        // Configure input field for Enter key submission
        if (inputField != null)
        {
            // Set to single line mode so Enter submits instead of adding newlines
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            
            // Add OnSubmit listener - fires when user presses Enter
            inputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
        
        // Setup button listeners
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }
        
        if (voiceInputButton != null)
        {
            // Store original color
            Image voiceBtnImg = voiceInputButton.GetComponent<Image>();
            if (voiceBtnImg != null)
            {
                originalVoiceButtonColor = voiceBtnImg.color;
            }
            
            voiceInputButton.onClick.AddListener(OnVoiceButtonClicked);
        }
        
        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.AddListener(OnClearHistoryClicked);
        }
        
        // Subscribe to GeminiChat events
        geminiChat.OnStreamingChunk += OnStreamingChunk;
        geminiChat.OnResponseComplete += OnResponseComplete;
        geminiChat.OnError += OnError;
        
        // Subscribe to VoiceInputManager events
        if (voiceInputManager != null)
        {
            voiceInputManager.OnTranscriptionComplete += OnVoiceTranscriptionComplete;
            voiceInputManager.OnTranscriptionError += OnVoiceTranscriptionError;
            voiceInputManager.OnRecordingStarted += OnVoiceRecordingStarted;
            voiceInputManager.OnRecordingStopped += OnVoiceRecordingStopped;
            voiceInputManager.OnRecordingProgress += OnVoiceRecordingProgress;
        }
        
        // Hide recording indicator initially
        if (recordingIndicator != null)
        {
            recordingIndicator.gameObject.SetActive(false);
        }
        
        // Initialize chat toggle features (ChatUIManager compatibility)
        if (enableChatToggle)
        {
            // Find robot controller
            if (disableRobotWhileTyping)
            {
                robotController = FindObjectOfType<AB_RobotController>();
                if (robotController == null)
                {
                    Debug.LogWarning("[GeminiChatbotUI] Robot controller not found. Robot disable feature won't work.");
                }
            }
            
            // Set initial chat panel state
            if (chatPanel != null)
            {
                chatPanel.SetActive(isOpen);
            }
        }
        
        // Initial welcome message
        ShowWelcomeMessage();
        
        // Apply modern UI styling
        ApplyModernStyle();
    }
    
    private void ApplyModernStyle()
    {
        // Create a rounded rectangle sprite (white, so we can tint it)
        Sprite roundedSprite = CreateRoundedSprite();
        
        // 1. Style the Main Panel (if this script is on the panel)
        Image mainPanel = GetComponent<Image>();
        if (mainPanel != null)
        {
            mainPanel.sprite = roundedSprite;
            // Transparent but blurry background - use a low alpha for transparency
            mainPanel.color = new Color(0.1f, 0.1f, 0.12f, 0.3f); // Very transparent dark background
            
            // Add padding to prevent full-screen suffocation
            RectTransform panelRect = GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Add margins around the panel (50 units on all sides)
                panelRect.offsetMin = new Vector2(50, 50); // left, bottom
                panelRect.offsetMax = new Vector2(-50, -50); // right, top
            }
        }
        
        // 2. Style the ScrollRect Background (Chat Area)
        if (scrollRect != null)
        {
            Image scrollBg = scrollRect.GetComponent<Image>();
            if (scrollBg != null)
            {
                scrollBg.sprite = roundedSprite;
                scrollBg.color = new Color(0.15f, 0.15f, 0.18f, 0.8f); // Slightly lighter
            }
        }
        
        // 3. Style the Input Field
        if (inputField != null)
        {
            Image inputBg = inputField.GetComponent<Image>();
            if (inputBg != null)
            {
                inputBg.sprite = roundedSprite;
                inputBg.color = new Color(0.2f, 0.2f, 0.25f, 1.0f);
            }
            
            if (inputField.textComponent != null)
            {
                inputField.textComponent.color = Color.white;
            }
            
            if (inputField.placeholder != null && inputField.placeholder is TMP_Text placeholder)
            {
                placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            }
        }
        
        // 4. Style the Send Button
        if (sendButton != null)
        {
            Image btnBg = sendButton.GetComponent<Image>();
            if (btnBg != null)
            {
                btnBg.sprite = roundedSprite;
                btnBg.color = new Color(0.0f, 0.4f, 0.8f, 1.0f); // Modern Blue
            }
            
            TMP_Text btnText = sendButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.color = Color.white;
                btnText.fontStyle = FontStyles.Bold;
            }
        }
        
        // 5. Style the Voice Input Button
        if (voiceInputButton != null)
        {
            Image btnBg = voiceInputButton.GetComponent<Image>();
            if (btnBg != null)
            {
                btnBg.sprite = roundedSprite;
                btnBg.color = new Color(0.6f, 0.2f, 0.8f, 1.0f); // Purple/Magenta
                originalVoiceButtonColor = btnBg.color;
            }
            
            TMP_Text btnText = voiceInputButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.color = Color.white;
                btnText.fontStyle = FontStyles.Bold;
            }
        }
        
        // 6. Style the Clear History Button
        if (clearHistoryButton != null)
        {
            Image btnBg = clearHistoryButton.GetComponent<Image>();
            if (btnBg != null)
            {
                btnBg.sprite = roundedSprite;
                btnBg.color = new Color(0.8f, 0.2f, 0.2f, 1.0f); // Red
            }
            
            TMP_Text btnText = clearHistoryButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.color = Color.white;
            }
        }
        
        // 7. Style the Recording Indicator
        if (recordingIndicator != null)
        {
            recordingIndicator.color = new Color(1f, 0.3f, 0.3f, 1f); // Bright red
            recordingIndicator.fontStyle = FontStyles.Bold;
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
                // Check corners
                bool inCorner = false;
                float dist = 0;
                
                if (x < radius && y < radius) // Bottom-Left
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    inCorner = true;
                }
                else if (x > size - radius && y < radius) // Bottom-Right
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, radius));
                    inCorner = true;
                }
                else if (x < radius && y > size - radius) // Top-Left
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius));
                    inCorner = true;
                }
                else if (x > size - radius && y > size - radius) // Top-Right
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, size - radius));
                    inCorner = true;
                }
                
                if (inCorner)
                {
                    if (dist <= radius - 1) colors[y * size + x] = c; // Inside
                    else if (dist <= radius) colors[y * size + x] = new Color(c.r, c.g, c.b, c.a * (radius - dist)); // Anti-aliasing
                    else colors[y * size + x] = Color.clear;
                }
                else
                {
                    colors[y * size + x] = c;
                }
            }
        }
        
        tex.SetPixels(colors);
        tex.Apply();
        
        // Create sprite with 9-slice borders
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (geminiChat != null)
        {
            geminiChat.OnStreamingChunk -= OnStreamingChunk;
            geminiChat.OnResponseComplete -= OnResponseComplete;
            geminiChat.OnError -= OnError;
        }
        
        // Unsubscribe from voice input events
        if (voiceInputManager != null)
        {
            voiceInputManager.OnTranscriptionComplete -= OnVoiceTranscriptionComplete;
            voiceInputManager.OnTranscriptionError -= OnVoiceTranscriptionError;
            voiceInputManager.OnRecordingStarted -= OnVoiceRecordingStarted;
            voiceInputManager.OnRecordingStopped -= OnVoiceRecordingStopped;
            voiceInputManager.OnRecordingProgress -= OnVoiceRecordingProgress;
        }
    }
    
    
    void Update()
    {
        // Handle chat toggle (ChatUIManager compatibility)
        if (enableChatToggle && Assets.Scripts.GlobalInputManager.GetKeyDown(toggleChatKey))
        {
            if (isOpen)
                CloseChat();
            else
                OpenChat();
        }
    }
    
    
    private void ShowWelcomeMessage()
    {
        chatHistory.Clear();
        chatHistory.AppendLine(GetColoredText("Welcome! How can I help you?", aiColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
    }
    
    /// <summary>
    /// Called when user presses Enter in the input field
    /// </summary>
    private void OnInputFieldSubmit(string text)
    {
        // Send the message
        OnSendClicked();
        
        // Refocus the input field so user can immediately type again
        if (inputField != null && gameObject.activeInHierarchy)
        {
            inputField.ActivateInputField();
        }
    }
    
    public void OnSendClicked()
    {
        if (geminiChat == null)
        {
            Debug.LogError("[GeminiChatbotUI] GeminiChat is not assigned!");
            return;
        }
        
        if (chatHistory == null)
        {
            Debug.LogError("[GeminiChatbotUI] chatHistory is null! Reinitializing...");
            chatHistory = new StringBuilder();
        }
        
        if (geminiChat.IsProcessing() || isTyping)
        {
            Debug.Log("[GeminiChatbotUI] Already processing a request");
            return;
        }
        
        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage))
        {
            return;
        }
        
        // Store current user message
        currentUserMessage = userMessage;
        
        // Clear input field
        inputField.text = "";
        
        // Add user message to chat display
        chatHistory.AppendLine(GetColoredText($"{userPrefix}{userMessage}", userColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
        
        // Disable send button while processing
        if (sendButton != null)
        {
            sendButton.interactable = false;
        }
        
        // Show "thinking" indicator
        chatHistory.Append(GetColoredText($"{aiPrefix}???", aiColor));
        UpdateChatDisplay(true);
        
        // Send to Gemini
        geminiChat.SendMessage(userMessage);
    }
    
    public void OnClearHistoryClicked()
    {
        if (geminiChat.IsProcessing() || isTyping)
        {
            Debug.Log("[GeminiChatbotUI] Cannot clear history while processing");
            return;
        }
        
        geminiChat.ClearHistory();
        ShowWelcomeMessage();
        Debug.Log("[GeminiChatbotUI] Chat history cleared");
    }
    
    private void OnStreamingChunk(string chunk)
    {
        // This is called for each chunk received from the API
        // We'll collect all chunks and display them when complete
        Debug.Log($"[GeminiChatbotUI] Received chunk: {chunk.Substring(0, Mathf.Min(50, chunk.Length))}...");
    }
    
    private void OnResponseComplete(string fullResponse)
    {
        Debug.Log($"[GeminiChatbotUI] Response complete: {fullResponse.Length} characters");
        
        // Remove "thinking" indicator
        RemoveLastLine();
        
        // Start typing animation for AI response
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(fullResponse));
        
        // Re-enable send button
        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
        
        // Focus input field
        FocusInput();
    }
    
    private void OnError(string errorMessage)
    {
        Debug.LogError($"[GeminiChatbotUI] Error: {errorMessage}");
        
        // Remove "thinking" indicator
        RemoveLastLine();
        
        // Show error message
        chatHistory.AppendLine(GetColoredText($"{aiPrefix}Error: {errorMessage}", errorColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
        
        // Re-enable send button
        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
        
        // Focus input field
        FocusInput();
    }
    
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        
        // Add AI prefix
        chatHistory.Append(GetColoredText(aiPrefix, aiColor));
        
        // Type out each character with delay
        foreach (char c in text)
        {
            chatHistory.Append(GetColoredText(c.ToString(), aiColor));
            UpdateChatDisplay(false); // Don't force scroll while typing, only if at bottom
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Add spacing
        chatHistory.AppendLine();
        chatHistory.AppendLine();
        UpdateChatDisplay(false);
        
        isTyping = false;
    }
    
    private void UpdateChatDisplay(bool forceScrollToBottom = false)
    {
        if (chatDisplay != null)
        {
            chatDisplay.text = chatHistory.ToString();
            
            // Force scroll to bottom if using ScrollRect
            Canvas.ForceUpdateCanvases();
            
            // Scroll to bottom - run this on next frame to ensure layout is updated
            if (scrollRect != null)
            {
                StartCoroutine(ScrollToBottomNextFrame(forceScrollToBottom));
            }
        }
    }
    
    private void RemoveLastLine()
    {
        if (chatHistory == null)
        {
            Debug.LogError("[GeminiChatbotUI] chatHistory is null!");
            return;
        }
        
        string currentText = chatHistory.ToString();
        if (string.IsNullOrEmpty(currentText))
        {
            return;
        }
        
        int lastNewline = currentText.LastIndexOf('\n');
        if (lastNewline > 0)
        {
            chatHistory.Clear();
            chatHistory.Append(currentText.Substring(0, lastNewline + 1));
            UpdateChatDisplay();
        }
    }
    
    private string GetColoredText(string text, Color color)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hexColor}>{text}</color>";
    }
    
    
    /// <summary>
    /// Scroll to bottom on the next frame (after layout update)
    /// </summary>
    private IEnumerator ScrollToBottomNextFrame(bool force)
    {
        // Check if we are already near the bottom (within 5%)
        bool isNearBottom = false;
        if (scrollRect != null)
        {
            // 0 is bottom, 1 is top
            isNearBottom = scrollRect.verticalNormalizedPosition <= 0.05f;
        }

        yield return null; // Wait one frame for layout to update
        
        if (scrollRect != null)
        {
            // Scroll if forced OR if we were already at the bottom
            if (force || isNearBottom)
            {
                scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom, 1 = top
            }
        }
    }
    
    /// <summary>
    /// Focus the input field (can be called from other scripts)
    /// </summary>
    public void FocusInput()
    {
        if (inputField != null && gameObject.activeInHierarchy)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }
    
    /// <summary>
    /// Set typing speed for animation
    /// </summary>
    public void SetTypingSpeed(float speed)
    {
        typingSpeed = Mathf.Max(0.001f, speed);
    }
    
    private void ConfigureChatDisplayForScrolling()
    {
        if (chatDisplay != null)
        {
            // Ensure text wraps
            chatDisplay.enableWordWrapping = true;
            chatDisplay.overflowMode = TextOverflowModes.Overflow;
            
            if (scrollRect != null && scrollRect.content != null)
            {
                RectTransform contentRect = scrollRect.content;
                
                // 1. Ensure Content pivots from the top so it grows downwards
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                
                // 2. Ensure Content has a ContentSizeFitter
                ContentSizeFitter contentFitter = contentRect.GetComponent<ContentSizeFitter>();
                if (contentFitter == null)
                {
                    contentFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                }
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // 3. If the text is a child of Content (not the content itself), 
                // we need a VerticalLayoutGroup on Content to drive the child text
                if (chatDisplay.transform != contentRect && chatDisplay.transform.IsChildOf(contentRect))
                {
                    VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    
                    layoutGroup.childControlHeight = true;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                    
                    // Add some padding if needed
                    layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                }
            }
        }
    }

    /// <summary>
    /// Check if the input field is currently focused (blocks other input)
    /// Other scripts should call this to check if they should ignore keyboard input
    /// </summary>
    public bool IsInputFieldFocused()
    {
        return inputField != null && inputField.isFocused;
    }
    
    /// <summary>
    /// Check if this chatbot UI is active and blocking input
    /// </summary>
    public bool IsBlockingInput()
    {
        return gameObject.activeInHierarchy && IsInputFieldFocused();
    }
    
    #region Chat Toggle Methods (ChatUIManager Compatibility)
    
    /// <summary>
    /// Open chat panel, unlock cursor, disable robot controller
    /// Compatible with ChatUIManager.OpenChat()
    /// </summary>
    public void OpenChat()
    {
        if (!enableChatToggle)
        {
            Debug.LogWarning("[GeminiChatbotUI] Chat toggle is not enabled. Set enableChatToggle = true.");
            return;
        }
        
        isOpen = true;
        
        // Show chat panel
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }
        
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable robot movement while typing
        if (disableRobotWhileTyping && robotController != null)
        {
            robotController.enabled = false;
        }
        
        // Focus input field
        StartCoroutine(FocusInputNextFrame());
        
        Debug.Log("[GeminiChatbotUI] Chat opened");
    }
    
    /// <summary>
    /// Close chat panel, lock cursor, enable robot controller
    /// Compatible with ChatUIManager.CloseChat()
    /// </summary>
    public void CloseChat()
    {
        if (!enableChatToggle)
        {
            Debug.LogWarning("[GeminiChatbotUI] Chat toggle is not enabled. Set enableChatToggle = true.");
            return;
        }
        
        isOpen = false;
        
        // Hide chat panel
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        
        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable robot movement
        if (disableRobotWhileTyping && robotController != null)
        {
            robotController.enabled = true;
        }
        
        // Clear UI selection
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        
        Debug.Log("[GeminiChatbotUI] Chat closed");
    }
    
    /// <summary>
    /// Check if chat input is currently active (for GlobalInputManager)
    /// Compatible with ChatUIManager.IsInputActive()
    /// </summary>
    public bool IsInputActive()
    {
        if (enableChatToggle)
        {
            return isOpen && inputField != null && inputField.isFocused;
        }
        else
        {
            // If toggle is disabled, just check if input is focused
            return inputField != null && inputField.isFocused;
        }
    }
    
    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        FocusInput();
    }
    
    #endregion
    
    #region Voice Input Event Handlers
    
    /// <summary>
    /// Handle voice button click - toggle recording
    /// </summary>
    private void OnVoiceButtonClicked()
    {
        if (voiceInputManager == null)
        {
            Debug.LogWarning("[GeminiChatbotUI] VoiceInputManager not available!");
            return;
        }
        
        if (isRecording)
        {
            // Stop recording
            voiceInputManager.StopRecording();
        }
        else
        {
            // Start recording
            voiceInputManager.StartRecording();
        }
    }
    
    /// <summary>
    /// Called when voice recording starts
    /// </summary>
    private void OnVoiceRecordingStarted()
    {
        isRecording = true;
        
        // Show recording indicator
        if (recordingIndicator != null)
        {
            recordingIndicator.gameObject.SetActive(true);
            recordingIndicator.text = "🎤 Recording...";
        }
        
        // Start pulsing animation on button
        if (voiceInputButton != null && recordingAnimationCoroutine == null)
        {
            recordingAnimationCoroutine = StartCoroutine(PulseVoiceButton());
        }
        
        Debug.Log("[GeminiChatbotUI] Voice recording started");
    }
    
    /// <summary>
    /// Called when voice recording stops
    /// </summary>
    private void OnVoiceRecordingStopped()
    {
        isRecording = false;
        
        // Update recording indicator
        if (recordingIndicator != null)
        {
            recordingIndicator.text = "🔄 Processing...";
        }
        
        // Stop pulsing animation
        if (recordingAnimationCoroutine != null)
        {
            StopCoroutine(recordingAnimationCoroutine);
            recordingAnimationCoroutine = null;
            
            // Restore original button color
            if (voiceInputButton != null)
            {
                Image btnImg = voiceInputButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = originalVoiceButtonColor;
                }
            }
        }
        
        Debug.Log("[GeminiChatbotUI] Voice recording stopped");
    }
    
    /// <summary>
    /// Called with recording progress (0-1)
    /// </summary>
    private void OnVoiceRecordingProgress(float progress)
    {
        if (recordingIndicator != null && isRecording)
        {
            // Update progress indicator
            int percentage = Mathf.RoundToInt(progress * 100);
            recordingIndicator.text = $"🎤 Recording... {percentage}%";
        }
    }
    
    /// <summary>
    /// Called when voice transcription completes successfully
    /// </summary>
    private void OnVoiceTranscriptionComplete(string transcription)
    {
        Debug.Log($"[GeminiChatbotUI] Transcription received: {transcription}");
        
        // Hide recording indicator
        if (recordingIndicator != null)
        {
            recordingIndicator.gameObject.SetActive(false);
        }
        
        // Set the transcribed text in the input field
        if (inputField != null)
        {
            inputField.text = transcription;
            
            // Optionally auto-send the message (uncomment if desired)
            // OnSendClicked();
        }
        
        // Show feedback in chat
        chatHistory.AppendLine(GetColoredText("🎤 Voice input received", new Color(0.6f, 0.2f, 0.8f)));
        UpdateChatDisplay(true);
    }
    
    /// <summary>
    /// Called when voice transcription fails
    /// </summary>
    private void OnVoiceTranscriptionError(string errorMessage)
    {
        Debug.LogError($"[GeminiChatbotUI] Voice transcription error: {errorMessage}");
        
        // Hide recording indicator
        if (recordingIndicator != null)
        {
            recordingIndicator.gameObject.SetActive(false);
        }
        
        // Show error in chat
        chatHistory.AppendLine(GetColoredText($"🎤 Voice Error: {errorMessage}", errorColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
    }
    
    /// <summary>
    /// Animate the voice button with a pulsing effect while recording
    /// </summary>
    private IEnumerator PulseVoiceButton()
    {
        if (voiceInputButton == null) yield break;
        
        Image btnImg = voiceInputButton.GetComponent<Image>();
        if (btnImg == null) yield break;
        
        Color pulseColor = new Color(1f, 0.2f, 0.2f, 1f); // Bright red
        float pulseSpeed = 2f;
        
        while (isRecording)
        {
            // Pulse between original color and red
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            btnImg.color = Color.Lerp(originalVoiceButtonColor, pulseColor, t);
            
            yield return null;
        }
        
        // Restore original color when done
        btnImg.color = originalVoiceButtonColor;
    }
    
    #endregion
    }
}
