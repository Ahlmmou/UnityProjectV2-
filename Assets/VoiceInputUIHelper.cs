using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts
{
    /// <summary>
    /// Helper script to programmatically add voice input UI elements to an existing chatbot
    /// This is optional - you can also create the UI manually in the Unity Editor
    /// </summary>
    [RequireComponent(typeof(GeminiChatbotUI))]
    public class VoiceInputUIHelper : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [Tooltip("Automatically create voice button and indicator on Start")]
        [SerializeField] private bool autoCreateUI = false;
        
        [Header("References (if manually created)")]
        [SerializeField] private Button voiceButton;
        [SerializeField] private TMP_Text recordingIndicator;
        
        [Header("UI Customization")]
        [SerializeField] private string voiceButtonText = "🎤 Voice";
        [SerializeField] private Color voiceButtonColor = new Color(0.6f, 0.2f, 0.8f, 1.0f);
        [SerializeField] private Vector2 voiceButtonSize = new Vector2(120f, 50f);
        
        private GeminiChatbotUI chatbotUI;
        
        void Start()
        {
            chatbotUI = GetComponent<GeminiChatbotUI>();
            
            if (autoCreateUI)
            {
                CreateVoiceInputUI();
            }
        }
        
        /// <summary>
        /// Programmatically create voice input UI elements
        /// </summary>
        [ContextMenu("Create Voice Input UI")]
        public void CreateVoiceInputUI()
        {
            // Find the canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[VoiceInputUIHelper] No Canvas found! This script must be on a GameObject that is a child of a Canvas.");
                return;
            }
            
            // Create voice button if it doesn't exist
            if (voiceButton == null)
            {
                voiceButton = CreateVoiceButton(canvas.transform);
                Debug.Log("[VoiceInputUIHelper] Created voice button");
            }
            
            // Create recording indicator if it doesn't exist
            if (recordingIndicator == null)
            {
                recordingIndicator = CreateRecordingIndicator(canvas.transform);
                Debug.Log("[VoiceInputUIHelper] Created recording indicator");
            }
            
            // Assign references to GeminiChatbotUI using reflection (since fields are private)
            AssignVoiceReferences();
            
            Debug.Log("[VoiceInputUIHelper] Voice input UI setup complete!");
        }
        
        private Button CreateVoiceButton(Transform parent)
        {
            // Create GameObject
            GameObject buttonObj = new GameObject("VoiceInputButton");
            buttonObj.transform.SetParent(parent, false);
            
            // Add RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = voiceButtonSize;
            
            // Position next to send button (you may need to adjust this)
            rectTransform.anchoredPosition = new Vector2(0, -100); // Adjust as needed
            
            // Add Image component
            Image image = buttonObj.AddComponent<Image>();
            image.color = voiceButtonColor;
            
            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = voiceButtonText;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18;
            text.color = Color.white;
            
            return button;
        }
        
        private TMP_Text CreateRecordingIndicator(Transform parent)
        {
            // Create GameObject
            GameObject indicatorObj = new GameObject("RecordingIndicator");
            indicatorObj.transform.SetParent(parent, false);
            
            // Add RectTransform
            RectTransform rectTransform = indicatorObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300f, 40f);
            rectTransform.anchoredPosition = new Vector2(0, 50); // Above input field
            
            // Add TextMeshProUGUI
            TMP_Text text = indicatorObj.AddComponent<TextMeshProUGUI>();
            text.text = "🎤 Recording...";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16;
            text.color = new Color(1f, 0.3f, 0.3f, 1f); // Red
            text.fontStyle = FontStyles.Bold;
            
            // Hide initially
            indicatorObj.SetActive(false);
            
            return text;
        }
        
        private void AssignVoiceReferences()
        {
            // Note: This uses reflection because the fields are private
            // You can also make them public in GeminiChatbotUI and assign directly
            
            var uiType = typeof(GeminiChatbotUI);
            
            // Assign voice button
            var voiceButtonField = uiType.GetField("voiceInputButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (voiceButtonField != null)
            {
                voiceButtonField.SetValue(chatbotUI, voiceButton);
                Debug.Log("[VoiceInputUIHelper] Voice button assigned");
            }
            
            // Assign recording indicator
            var indicatorField = uiType.GetField("recordingIndicator", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (indicatorField != null)
            {
                indicatorField.SetValue(chatbotUI, recordingIndicator);
                Debug.Log("[VoiceInputUIHelper] Recording indicator assigned");
            }
        }
        
        /// <summary>
        /// Get the created voice button
        /// </summary>
        public Button GetVoiceButton()
        {
            return voiceButton;
        }
        
        /// <summary>
        /// Get the created recording indicator
        /// </summary>
        public TMP_Text GetRecordingIndicator()
        {
            return recordingIndicator;
        }
    }
}
