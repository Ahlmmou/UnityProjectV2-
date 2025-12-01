using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Global input manager that wraps Unity's Input class to automatically block
    /// keyboard input when any text field is focused.
    /// Use GlobalInputManager.GetKeyDown() instead of Input.GetKeyDown() in your scripts.
    /// </summary>
    public static class GlobalInputManager
    {
        /// <summary>
        /// Wrapper for Input.GetKeyDown that respects text field focus.
        /// Returns false if any chat/text input is currently active.
        /// </summary>
        public static bool GetKeyDown(KeyCode key)
        {
            // Block all keyboard input if any text field is focused
            if (ChatbotInputHelper.IsChatbotInputActive())
            {
                return false;
            }
            
            return Input.GetKeyDown(key);
        }
        
        /// <summary>
        /// Wrapper for Input.GetKey that respects text field focus.
        /// Returns false if any chat/text input is currently active.
        /// </summary>
        public static bool GetKey(KeyCode key)
        {
            // Block all keyboard input if any text field is focused
            if (ChatbotInputHelper.IsChatbotInputActive())
            {
                return false;
            }
            
            return Input.GetKey(key);
        }
        
        /// <summary>
        /// Wrapper for Input.GetKeyUp that respects text field focus.
        /// Returns false if any chat/text input is currently active.
        /// </summary>
        public static bool GetKeyUp(KeyCode key)
        {
            // Block all keyboard input if any text field is focused
            if (ChatbotInputHelper.IsChatbotInputActive())
            {
                return false;
            }
            
            return Input.GetKeyUp(key);
        }
        
        /// <summary>
        /// Check if text input is currently being blocked (for debugging)
        /// </summary>
        public static bool IsInputBlocked()
        {
            return ChatbotInputHelper.IsChatbotInputActive();
        }
    }
}
