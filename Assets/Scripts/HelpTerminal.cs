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

            if (Input.GetKeyDown(KeyCode.H))
            {
                isOpen = !isOpen;
                terminalPanel.SetActive(isOpen);
                if (isOpen) inputField.ActivateInputField();
            }

            // Submit on Enter
            if (isOpen && Input.GetKeyDown(KeyCode.Return))
            {
                string message = inputField.text;
                if (!string.IsNullOrEmpty(message))
                {
                    outputText.text += "\n> " + message;
                    inputField.text = "";
                    inputField.ActivateInputField();
                    StartCoroutine(gemini.SendMessageToGemini(message, DisplayResponse));
                }
            }
        }

        void DisplayResponse(string response)
        {
            outputText.text += "\n" + response;
        }
    }

}
