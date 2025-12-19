using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TrailRenderer))]
public class SnowballVisualEnhancer : MonoBehaviour
{
    [Header("拖尾升级设置")]
    public Color startColorLevel0 = new Color(1f, 1f, 1f, 0.5f);
    [ColorUsage(true, true)]
    public Color startColorLevelMax = new Color(0f, 1f, 1f, 1f);
    public float widthLevel0 = 0.1f;
    public float widthLevelMax = 0.3f;
    public bool playerSnowballOnly = true;
    public float showTrailDelay = 0.5f;

    private TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null) return;

        // ⭐ 修改：检测对照组。如果禁用特效，直接关闭组件并返回。
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            trail.enabled = false;
            this.enabled = false; // 禁用脚本自身
            return;
        }

        trail.enabled = false;
        trail.Clear();

        float progress = 0f;
        if (BurnRecoverySystem.Instance != null)
        {
            progress = BurnRecoverySystem.Instance.GetRecoveryProgress();
        }

        ApplyVisuals(progress);
        StartCoroutine(EnableTrailAfterDelay());
    }

    IEnumerator EnableTrailAfterDelay()
    {
        yield return new WaitForSeconds(showTrailDelay);
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = true;
        }
    }

    void ApplyVisuals(float progress)
    {
        Gradient gradient = new Gradient();
        Color currentColor = Color.Lerp(startColorLevel0, startColorLevelMax, progress);

        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(currentColor, 0.0f), new GradientColorKey(currentColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;

        float currentWidth = Mathf.Lerp(widthLevel0, widthLevelMax, progress);
        trail.widthMultiplier = currentWidth;

        if (progress >= 0.9f) trail.time = 0.5f;
        else trail.time = 0.3f;
    }
}