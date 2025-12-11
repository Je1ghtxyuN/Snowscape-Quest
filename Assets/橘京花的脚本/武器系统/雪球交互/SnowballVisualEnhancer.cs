using UnityEngine;

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

    [Tooltip("是否只对玩家丢出的雪球生效？(如果不勾选，敌人的雪球也会变强)")]
    public bool playerSnowballOnly = true;

    private TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null) return;

        // 获取进度
        float progress = 0f;
        if (BurnRecoverySystem.Instance != null)
        {
            progress = BurnRecoverySystem.Instance.GetRecoveryProgress();
        }

        // 如果限制只对玩家生效，且当前物体层级或标签不是玩家的子弹，则不应用强化
        // (这里假设你可能用 Tag 或 Layer 区分，如果没区分，就全部应用)
        // 简单判断：如果进度 > 0，我们就应用效果
        ApplyVisuals(progress);
    }

    void ApplyVisuals(float progress)
    {
        // 1. 颜色渐变 (Color Lerp)
        // 创建一个新的 Gradient
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