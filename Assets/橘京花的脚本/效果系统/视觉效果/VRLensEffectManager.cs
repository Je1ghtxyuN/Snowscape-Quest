using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class VREffectSettings
{
    [Header("效果设置")]
    [Range(0, 1)] public float maxIntensity = 0.7f;
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 1f;

    [Header("视觉设置")]
    public Color tintColor = new Color(0.8f, 0.9f, 1.0f, 1.0f);
    public float blurAmount = 0.02f;
    public float distortionAmount = 0.03f;
}

public class VRLensEffectManager : MonoBehaviour
{
    [Header("XR引用")]
    public XROrigin xrOrigin;
    public Camera xrCamera;

    [Header("效果设置")]
    public VREffectSettings effectSettings;

    [Header("资源引用")]
    public Material condensationMaterial;
    public Texture2D dropletPatternTexture;

    // 私有变量
    private VRPostProcessEffect postProcessEffect;
    public bool isInWater = false;
    private float currentIntensity = 0f;
    private Material runtimeMaterial;

    void Start()
    {
        InitializeEffects();
    }

    void InitializeEffects()
    {
        if (xrCamera == null && xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera;
        }

        if (xrCamera == null)
        {
            Debug.LogError("XR Camera未找到！");
            return;
        }

        // 创建运行时材质实例
        runtimeMaterial = new Material(condensationMaterial);
        runtimeMaterial.SetTexture("_DropletPattern", dropletPatternTexture);

        // 添加后处理组件
        postProcessEffect = xrCamera.gameObject.AddComponent<VRPostProcessEffect>();
        postProcessEffect.effectMaterial = runtimeMaterial;

        UpdateMaterialProperties();
    }

    void Update()
    {
        UpdateEffectIntensity();
    }

    void UpdateEffectIntensity()
    {
        float targetIntensity = isInWater ? effectSettings.maxIntensity : 0f;
        float fadeSpeed = isInWater ? effectSettings.fadeInSpeed : effectSettings.fadeOutSpeed;

        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity,
                                           fadeSpeed * Time.deltaTime);

        UpdateMaterialProperties();
    }

    void UpdateMaterialProperties()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat("_Intensity", currentIntensity);
            runtimeMaterial.SetFloat("_BlurAmount", effectSettings.blurAmount);
            runtimeMaterial.SetFloat("_Distortion", effectSettings.distortionAmount);
            runtimeMaterial.SetColor("_TintColor", effectSettings.tintColor);
        }
    }

    public void EnterWaterEffect()
    {
        isInWater = true;
        Debug.Log("进入水域效果");
    }

    public void ExitWaterEffect()
    {
        isInWater = false;
        Debug.Log("退出水域效果");
    }

    // 手动控制效果强度（可选）
    public void SetEffectIntensity(float intensity)
    {
        currentIntensity = Mathf.Clamp01(intensity);
        UpdateMaterialProperties();
    }

    void OnDestroy()
    {
        // 清理运行时材质
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (runtimeMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
#endif
}