using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunction : MonoBehaviour
{
    [SerializeField] private string sceneToSwitch;
    public void ClickDebug()
    {
        Debug.Log("Click!");
    }

    public void SwitchScene()
    {
        SceneManager.LoadScene(sceneToSwitch, LoadSceneMode.Additive);
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
