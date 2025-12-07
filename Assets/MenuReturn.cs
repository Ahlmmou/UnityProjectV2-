using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuReturn : MonoBehaviour
{
    void Update()
    {
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.W))
        {
            Debug.Log("BACK TO MENU (W KEY)");
            SceneManager.LoadScene("BasicScene");
        }
    }
}
