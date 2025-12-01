using UnityEngine;
using UnityEngine.UI;

public class ChatScrollController : MonoBehaviour
{
    // 1. Assign the ChatScrollView here (ScrollRect component is on this object)
    public ScrollRect scrollRect;

    // 2. NEW: Assign the main parent GameObject of your chatbot UI here.
    // This is the object you enable/disable to show/hide the chatbot.
    public GameObject chatbotPanel;

    public float scrollSpeed = 0.1f;

    void Update()
    {
        // ?? NEW CHECK: Only proceed if the chatbotPanel is active in the hierarchy.
        // This stops the key presses from scrolling the chat when it's hidden.
        if (chatbotPanel != null && chatbotPanel.activeInHierarchy)
        {
            // Check for the Down Arrow key press
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.DownArrow))
            {
                ScrollDown();
            }

            // Check for the Up Arrow key press
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.UpArrow))
            {
                ScrollUp();
            }
        }
        // If chatbotPanel is NOT active, the Update loop does nothing.
    }

    void ScrollUp()
    {
        if (scrollRect != null)
        {
            // Increase the normalized vertical position to scroll UP
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollSpeed);
        }
    }

    void ScrollDown()
    {
        if (scrollRect != null)
        {
            // Decrease the normalized vertical position to scroll DOWN
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition - scrollSpeed);
        }
    }
}