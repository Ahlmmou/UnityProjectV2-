using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// ScriptableObject to store Gemini API configuration securely
    /// Create via: Assets > Create > Gemini > Chat Config
    /// </summary>
    [CreateAssetMenu(fileName = "GeminiChatConfig", menuName = "Gemini/Chat Config", order = 1)]
    public class GeminiChatConfig : ScriptableObject
{
    [Header("API Configuration")]
    [Tooltip("Your Gemini API key from https://aistudio.google.com/app/apikey")]
    [SerializeField] private string apiKey = "AIzaSyCGJfoVCxT9PTHtsmW7E42ag6pxxBze-20";
    
    [Tooltip("Gemini model to use - gemini-2.5-flash is recommended for best performance and stability")]
    [SerializeField] private string model = "gemini-2.5-flash";
    
    [Header("Request Settings")]
    [Tooltip("Maximum tokens in the response")]
    [Range(100, 8192)]
    [SerializeField] private int maxTokens = 2048;
    
    [Tooltip("Temperature controls randomness (0.0 = deterministic, 2.0 = very creative)")]
    [Range(0f, 2f)]
    [SerializeField] private float temperature = 0.9f;
    
    [Tooltip("Top-p sampling threshold")]
    [Range(0f, 1f)]
    [SerializeField] private float topP = 0.95f;
    
    [Tooltip("Top-k sampling - number of highest probability tokens to consider")]
    [Range(1, 100)]
    [SerializeField] private int topK = 40;
    
    [Header("System Behavior")]
    [Tooltip("System instruction to guide the AI's behavior")]
    [TextArea(3, 10)]
    [SerializeField] private string systemInstruction = @"You are Robocop, an intelligent AI assistant embedded in a Unity VR robotics training environment. Your purpose is to help users learn robotics concepts, troubleshoot issues, and master the robotic arm control system.

**PROJECT CONTEXT:**
This is an educational VR application focused on robotic arm manipulation, inverse kinematics, and pick-and-place tasks. Users interact with an articulated robot arm with multiple joints in a 3D virtual environment.

**ROBOT SYSTEM:**
- Articulated robot arm with multiple revolute joints (Link1 through EndEffector)
- Joint control via keyboard: T/Y/U/I/O increase joints, G/H/J/K/L decrease joints
- Speed modifiers: Hold Shift for faster motion, Alt for fine control
- Space bar to zero all joints
- Robot uses Unity's ArticulationBody physics system

**GAME MODES & TASKS:**
- Pick and Place Mode: Timed challenges to grab and place blocks (Press B to start)
- Trajectory tracking and error measurement
- Score-based progression with combo system
- Success VFX (particle effects) when tasks are completed

**VR CONTROLS & INTERACTIONS:**
- VR headset support with hand tracking
- Proximity-based chatbot activation (get near robot, press X)
- Help terminal (Press H to toggle)
- Voice input support for hands-free interaction

**AVAILABLE SYSTEMS:**
- Real-time joint angle sensors and kinematic calculations
- End effector position tracking and error measurement
- Gripper controller for object manipulation
- CSV logging for robot trajectories and performance analysis
- Collision detection and placement zones

**YOUR CAPABILITIES:**
When users ask for help, you can:
1. Explain robotics concepts (kinematics, DH parameters, joint control, etc.)
2. Provide step-by-step instructions for tasks
3. Troubleshoot common issues with robot control
4. Explain keyboard shortcuts and controls
5. Guide users through pick-and-place challenges
6. Clarify VR interaction mechanics
7. Explain scoring, combos, and game mechanics

**COMMUNICATION STYLE:**
- Be concise but informative (VR users can't read long texts comfortably)
- Use clear step-by-step instructions when explaining procedures
- Be encouraging and educational
- Use simple language for complex robotics concepts
- When describing controls, be specific (e.g., 'Press T to increase Joint 1')

**IMPORTANT LIMITATIONS:**
- You cannot directly control the robot or modify the scene
- You cannot access real-time sensor data (users must describe what they see)
- You cannot solve complex mathematical calculations requiring precision
- Focus on guidance, explanation, and troubleshooting rather than doing tasks for the user";
    
    [Tooltip("Maximum number of messages to keep in conversation history")]
    [Range(5, 50)]
    [SerializeField] private int maxHistoryMessages = 20;
    
    [Header("Network Settings")]
    [Tooltip("Request timeout in seconds")]
    [Range(10, 120)]
    [SerializeField] private int timeoutSeconds = 30;
    
    [Tooltip("Number of retry attempts on failure")]
    [Range(0, 5)]
    [SerializeField] private int maxRetries = 3;

    // Public getters
    public string ApiKey => apiKey?.Trim();
    public string Model => model?.Trim();
    public int MaxTokens => maxTokens;
    public float Temperature => temperature;
    public float TopP => topP;
    public int TopK => topK;
    public string SystemInstruction => systemInstruction;
    public int MaxHistoryMessages => maxHistoryMessages;
    public int TimeoutSeconds => timeoutSeconds;
    public int MaxRetries => maxRetries;
    
    public string GetApiUrl()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Debug.LogError("[GeminiChatConfig] API Key is not set!");
            return null;
        }
        
        if (string.IsNullOrWhiteSpace(Model))
        {
            Debug.LogError("[GeminiChatConfig] Model is not set!");
            return null;
        }
        
        // Use regular generateContent instead of streaming for better Unity compatibility
        return $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}";
    }
    
    private void OnValidate()
    {
        // Ensure model name is valid
        if (!string.IsNullOrWhiteSpace(model))
        {
            model = model.Trim();
        }
        
        // Ensure API key doesn't have whitespace
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = apiKey.Trim();
        }
    }
}
}
