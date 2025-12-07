using UnityEngine;

public class InstructionPanelToggle : MonoBehaviour
{
    public GameObject panel;

    void Update()
    {
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.A))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
