using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 环境管理器 V2.0：支持物体显隐控制 + Tag 动态替换
/// </summary>
public class ExperimentEnvironmentManager : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("原本是水，但在无水组中要伪装成的Tag (通常填 Snow 或 Ground)")]
    public string targetSnowTag = "Snow";
    [Tooltip("原始的水Tag (确保和你场景里的一致)")]
    public string originalWaterTag = "Water"; // 注意大小写！你的截图里可能是 "Water" 或 "water"

    [Header("列表1：纯环境物体 (在无水组中将被直接隐藏)")]
    [Tooltip("拖入瀑布模型、水花粒子、单独的水流音效AudioSource")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("列表2：功能性地面 (在无水组中将保留但换Tag)")]
    [Tooltip("拖入玩家需要踩在上面的河流表面、水面Collider。这些物体不会消失，但Tag会变。")]
    public List<GameObject> objectsToRetag = new List<GameObject>();

    [Tooltip("是否在控制台输出日志")]
    public bool showDebugLog = true;

    void Start()
    {
        // 稍微延迟一点执行，确保其他单例都初始化完毕
        Invoke(nameof(ApplyEnvironmentSettings), 0.1f);
    }

    void ApplyEnvironmentSettings()
    {
        if (ExperimentVisualControl.Instance == null)
        {
            Debug.LogError("❌ 未找到 ExperimentVisualControl，无法设定环境！");
            return;
        }

        // 询问总控：当前组别是否允许水环境存在？
        // (A组和B组允许，C组不允许)
        bool enableWaterEnv = ExperimentVisualControl.Instance.ShouldEnableWaterEnvironment();

        if (showDebugLog)
        {
            string status = enableWaterEnv ? "保留水环境 (Group A/B)" : "移除水环境 (Group C)";
            Debug.Log($"🧪 [环境控制] 执行模式: {status}");
        }

        // --- 1. 处理需要隐藏的物体 (瀑布/音效) ---
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                // 如果需要水环境 -> 显示；否则 -> 隐藏
                obj.SetActive(enableWaterEnv);
            }
        }

        // --- 2. 处理需要换 Tag 的物体 (河流表面) ---
        foreach (GameObject obj in objectsToRetag)
        {
            if (obj != null)
            {
                if (enableWaterEnv)
                {
                    // 恢复为水 (Water)
                    obj.tag = originalWaterTag;
                }
                else
                {
                    // 伪装成雪 (Snow)
                    // 这样脚步声系统会以为踩在雪上，且 WaterBuffSystem 不会触发
                    obj.tag = targetSnowTag;
                }
            }
        }
    }
}