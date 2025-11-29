using UnityEngine;

public class InstructionPanelToggle : MonoBehaviour
{
    public GameObject panel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
