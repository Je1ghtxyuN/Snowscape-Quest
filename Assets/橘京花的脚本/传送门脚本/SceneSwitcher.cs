using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 引入协程命名空间

public class SceneSwitcher : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("输入你要切换到的场景名字")]
    public string targetSceneName;

    [Tooltip("指定触发者的Tag")]
    public string playerTag = "Player";

    // 防止重复触发的锁
    private bool isSwitching = false;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查是否已经在切换中，防止多次触发
        if (isSwitching) return;

        if (other.CompareTag(playerTag))
        {
            Debug.Log($"[SceneSwitcher] 检测到玩家，准备前往: {targetSceneName}");

            // 2. 锁定状态
            isSwitching = true;

            // 3. 使用协程延迟一帧切换
            // 这样可以让物理引擎先把这一帧的碰撞逻辑跑完，避免“销毁后访问”的报错
            StartCoroutine(SwitchSceneRoutine());
        }
    }

    private IEnumerator SwitchSceneRoutine()
    {
        // 等待当前帧结束 (或者等待物理帧结束)
        yield return new WaitForEndOfFrame();

        // 确保时间流速恢复 (防止之前的暂停导致新场景卡住)
        Time.timeScale = 1f;

        // 加载新场景
        SceneManager.LoadScene(targetSceneName);
    }
}