using UnityEngine;
using System.Collections; // 必须引用这个命名空间以使用协程

[RequireComponent(typeof(TrailRenderer))]
public class SnowballVisualEnhancer : MonoBehaviour
{
    [Header("拖尾升级设置")]
    [Tooltip("初始颜色 (康复进度 0)")]
    public Color startColorLevel0 = new Color(1f, 1f, 1f, 0.5f); // 白色微透

    [Tooltip("最终颜色 (康复进度 1)")]
    [ColorUsage(true, true)] // 允许HDR高亮
    public Color startColorLevelMax = new Color(0f, 1f, 1f, 1f); // 青色发光

    [Tooltip("初始宽度")]
    public float widthLevel0 = 0.1f;
    [Tooltip("最终宽度")]
    public float widthLevelMax = 0.3f;

    [Tooltip("是否只对玩家丢出的雪球生效？")]
    public bool playerSnowballOnly = true;

    [Header("防晃眼设置")]
    [Tooltip("生成后延迟多少秒才显示拖尾？(防止刚生成时速度过快导致拖尾乱飞)")]
    public float showTrailDelay = 0.5f;

    private TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null) return;

        // --- 1. 初始状态先禁用拖尾，防止刚生成时的乱飞 ---
        trail.enabled = false;
        trail.Clear(); // 清理掉可能存在的残留数据

        // 获取进度
        float progress = 0f;
        if (BurnRecoverySystem.Instance != null)
        {
            progress = BurnRecoverySystem.Instance.GetRecoveryProgress();
        }

        // 应用视觉参数 (虽然现在不可见，但参数先设好)
        ApplyVisuals(progress);

        // --- 2. 开启协程，延迟显示 ---
        StartCoroutine(EnableTrailAfterDelay());
    }

    // 延迟开启的协程
    IEnumerator EnableTrailAfterDelay()
    {
        // 等待指定时间
        yield return new WaitForSeconds(showTrailDelay);

        if (trail != null)
        {
            // 重要：在开启前再次Clear，确保没有记录这0.5秒内的移动轨迹
            // 否则开启瞬间会有一条线从出生点连到现在的位置
            trail.Clear();
            trail.enabled = true;
        }
    }

    void ApplyVisuals(float progress)
    {
        // 1. 颜色渐变 (Color Lerp)
        Gradient gradient = new Gradient();
        Color currentColor = Color.Lerp(startColorLevel0, startColorLevelMax, progress);

        // 设置拖尾颜色：头是当前色，尾巴透明
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(currentColor, 0.0f), new GradientColorKey(currentColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;

        // 2. 宽度渐变
        float currentWidth = Mathf.Lerp(widthLevel0, widthLevelMax, progress);
        trail.widthMultiplier = currentWidth;

        // 3. (可选) 如果满级了，可以加长拖尾时间
        if (progress >= 0.9f)
        {
            trail.time = 0.5f; // 拖尾更长
        }
        else
        {
            trail.time = 0.3f;
        }
    }
}