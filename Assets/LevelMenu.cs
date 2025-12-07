using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenu : MonoBehaviour
{
    void Start()
    {
        // Ensure cursor is visible and unlocked for the menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        // Force unlock in case another script (like MouseLook) tries to lock it
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level_1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level_2");
    }

    public void LoadLevel3()
    {
        SceneManager.LoadScene("Level_3");   
    }

    public void LoadLevel4()
    {
        SceneManager.LoadScene("Level_4");
    }
}
