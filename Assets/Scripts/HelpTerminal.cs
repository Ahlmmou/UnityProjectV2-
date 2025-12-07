using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    using System.Collections;
    using UnityEngine;
    using TMPro;

    public class HelpTerminal : MonoBehaviour
    {
        public GameObject terminalPanel;
        public TMP_InputField inputField;
        public TMP_Text outputText;
        public GeminiChat gemini;

        private bool isOpen = false;

        void Update()
        {
            if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.H))
            {
                isOpen = !isOpen;
                terminalPanel.SetActive(isOpen);
                if (isOpen) inputField.ActivateInputField();
            }

            // Submit on Enter
            if (isOpen && Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Return))
            {
                string message = inputField.text;
                if (!string.IsNullOrEmpty(message))
                {
                    outputText.text += "\n> " + message;
                    inputField.text = "";
                    inputField.ActivateInputField();
                    
                    // Use new SendMessage API with callbacks
                    gemini.SendMessage(message, 
                        onComplete: DisplayResponse,
                        onError: (error) => {
                            outputText.text += "\nError: " + error;
                        }
                    );
                }
            }
        }

        void DisplayResponse(string response)
        {
            outputText.text += "\n" + response;
        }
        
        /// <summary>
        /// Check if this terminal's input field is currently active
        /// </summary>
        public bool IsInputActive()
        {
            return isOpen && inputField != null && inputField.isFocused;
        }
    }

}
