using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Helper class to check if any chatbot UI is currently blocking input.
    /// Use this in Update() methods before processing keyboard shortcuts to prevent
    /// conflicts when typing in chat fields.
    /// </summary>
    public static class ChatbotInputHelper
    {
        /// <summary>
        /// Check if any chatbot UI in the scene is currently blocking keyboard input.
        /// Returns true if the user is typing in a chatbot input field.
        /// </summary>
        public static bool IsChatbotInputActive()
        {
            // Check GeminiChatbotUI instances
            var geminiChatbotUIs = Object.FindObjectsOfType<GeminiChatbotUI>();
            foreach (var ui in geminiChatbotUIs)
            {
                if (ui != null && ui.IsBlockingInput())
                {
                    return true;
                }
            }
            
            // Check ChatUIManager instances
            var chatUIManagers = Object.FindObjectsOfType<ChatUIManager>();
            foreach (var manager in chatUIManagers)
            {
                if (manager != null && manager.IsInputActive())
                {
                    return true;
                }
            }
            
            // Check for other input fields that might be active (like HelpTerminal)
            var helpTerminals = Object.FindObjectsOfType<HelpTerminal>();
            foreach (var terminal in helpTerminals)
            {
                if (terminal != null && terminal.IsInputActive())
                {
                    return true;
                }
            }

            // Check NPCInteractor instances
            var npcInteractors = Object.FindObjectsOfType<NPCInteractor>();
            foreach (var npc in npcInteractors)
            {
                if (npc != null && npc.IsInputActive())
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
