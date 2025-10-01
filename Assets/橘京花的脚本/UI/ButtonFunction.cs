using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonFunction : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string sceneToSwitch;

    [Header("UI控制")]
    [SerializeField] private GameObject menuUI; // 拖入菜单UI的GameObject

    public void ClickDebug()
    {
        Debug.Log("Click!");
    }

    public void SwitchScene()
    {
        SceneManager.LoadScene(sceneToSwitch);
        CastleGate.ScoreSystem.ResetScore(); // 重置分数
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

    // 新增方法：关闭菜单（继续游戏）
    public void CloseMenu()
    {
        if (menuUI != null)
        {
            menuUI.SetActive(false);

            // 可选：恢复游戏时间（如果之前暂停了）
            Time.timeScale = 1f;

            Debug.Log("菜单已关闭，继续游戏");
        }
        else
        {
            Debug.LogWarning("未绑定菜单UI对象！");
        }
    }

    public void reLoadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneToSwitch);
        CastleGate.ScoreSystem.ResetScore(); // 重置分数
    }
}