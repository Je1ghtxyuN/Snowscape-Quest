using UnityEngine;
using Unity.XR.CoreUtils;

[System.Serializable]
public class VREffectSettings
{
    [Header("水下效果设置")]
    [Range(0, 1)] public float waterMaxIntensity = 0.5f; // ⭐ 修改：水下不要太浓，0.5左右即可
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 1f;

    [Header("视觉颜色")]
    public Color tintColor = new Color(0.8f, 0.9f, 1.0f, 1.0f);
}

public class VRLensEffectManager : MonoBehaviour
{
    [Header("XR引用")]
    public XROrigin xrOrigin;
    public Camera xrCamera;

    [Header("通用设置")]
    public VREffectSettings effectSettings;

    [Header("🩸 低血量效果设置")]
    [Tooltip("当血量低于此百分比时触发 (建议 0.3 或 0.4 方便测试)")]
    [Range(0, 1)] public float healthThreshold = 0.3f;

    [Tooltip("血量极低时的最大强度 (建议设为 1.0，比水下更强)")]
    [Range(0, 1)] public float healthMaxIntensity = 1.0f; // ⭐ 确保这个值比 waterMaxIntensity 大

    [Header("资源引用")]
    public Material condensationMaterial;
    public Texture2D dropletPatternTexture;

    // 状态变量
    public bool isInWater = false;
    private float currentIntensity = 0f;

    // 内部计算变量
    private float targetWaterIntensity = 0f;
    private float targetHealthIntensity = 0f;

    private Material runtimeMaterial;
    private GameObject effectQuad;

    void Start()
    {
        InitializeEffects();
    }

    void InitializeEffects()
    {
        if (xrCamera == null)
        {
            if (xrOrigin != null) xrCamera = xrOrigin.Camera;
            else xrCamera = Camera.main;
        }

        if (xrCamera == null)
        {
            Debug.LogError("❌ [VRLensEffect] XR Camera未找到！无法生成视觉效果。");
            return;
        }

        if (condensationMaterial == null) return;

        runtimeMaterial = new Material(condensationMaterial);
        if (dropletPatternTexture != null)
        {
            runtimeMaterial.SetTexture("_MainTex", dropletPatternTexture);
        }

        CreateEffectQuad();
    }

    void CreateEffectQuad()
    {
        if (effectQuad != null) Destroy(effectQuad);

        effectQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        effectQuad.name = "VR_Lens_Effect_Overlay";
        Destroy(effectQuad.GetComponent<Collider>());
        effectQuad.transform.SetParent(xrCamera.transform);

        // 放在相机前面
        effectQuad.transform.localPosition = new Vector3(0, 0, 0.45f);
        effectQuad.transform.localRotation = Quaternion.identity;
        effectQuad.transform.localScale = new Vector3(1.8f, 1.2f, 1f);

        Renderer rend = effectQuad.GetComponent<Renderer>();
        rend.material = runtimeMaterial;

        // 初始隐藏
        effectQuad.SetActive(false);
    }

    void Update()
    {
        UpdateEffectIntensity();
    }

    void UpdateEffectIntensity()
    {
        // 1. 计算水下目标强度
        targetWaterIntensity = isInWater ? effectSettings.waterMaxIntensity : 0f;

        // 2. 取两者中的最大值 (Max 保证了如果血量效果更强，就会覆盖水下效果)
        float finalTarget = Mathf.Max(targetWaterIntensity, targetHealthIntensity);

        // 3. 平滑过渡
        float speed = (finalTarget > currentIntensity) ? effectSettings.fadeInSpeed : effectSettings.fadeOutSpeed;
        currentIntensity = Mathf.MoveTowards(currentIntensity, finalTarget, speed * Time.deltaTime);

        // 4. 控制 Quad 显隐
        if (effectQuad != null)
        {
            bool shouldShow = currentIntensity > 0.01f;
            if (effectQuad.activeSelf != shouldShow) effectQuad.SetActive(shouldShow);
        }

        // 5. 应用到材质
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat("_Intensity", currentIntensity);
            runtimeMaterial.SetColor("_TintColor", effectSettings.tintColor);
        }
    }

    public void EnterWaterEffect()
    {
        isInWater = true;
    }

    public void ExitWaterEffect()
    {
        isInWater = false;
    }

    // ⭐ 核心修改：接收血量百分比
    public void UpdateHealthEffect(float healthPercent)
    {
        // 增加 Debug 方便你排查
        // Debug.Log($"[VRLensEffect] 接收到血量: {healthPercent}, 阈值: {healthThreshold}");

        if (healthPercent <= healthThreshold)
        {
            // 计算过程：
            // 假设阈值 0.3，当前血量 0.15 (一半)
            // factor = 1 - (0.15 / 0.3) = 0.5
            // 强度 = 0.5 * 1.0 = 0.5
            float factor = 1.0f - (healthPercent / healthThreshold);

            // 限制一下，防止血量 < 0 时溢出
            factor = Mathf.Clamp01(factor);

            targetHealthIntensity = factor * healthMaxIntensity;
        }
        else
        {
            targetHealthIntensity = 0f;
        }
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        if (effectQuad != null) Destroy(effectQuad);
    }
}