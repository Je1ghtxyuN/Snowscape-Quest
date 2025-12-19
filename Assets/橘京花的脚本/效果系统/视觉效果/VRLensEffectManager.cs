using UnityEngine;
using Unity.XR.CoreUtils;

[System.Serializable]
public class VREffectSettings
{
    [Range(0, 1)] public float waterMaxIntensity = 0.5f;
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 1f;
    public Color tintColor = new Color(0.8f, 0.9f, 1.0f, 1.0f);
}

public class VRLensEffectManager : MonoBehaviour
{
    [Header("XR引用")]
    public XROrigin xrOrigin;
    public Camera xrCamera;
    public VREffectSettings effectSettings;

    [Header("🩸 低血量效果设置")]
    [Range(0, 1)] public float healthThreshold = 0.3f;
    [Range(0, 1)] public float healthMaxIntensity = 1.0f;

    [Header("资源引用")]
    public Material condensationMaterial;
    public Texture2D dropletPatternTexture;

    public bool isInWater = false;
    private float currentIntensity = 0f;
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

        if (xrCamera == null || condensationMaterial == null) return;

        runtimeMaterial = new Material(condensationMaterial);
        if (dropletPatternTexture != null)
            runtimeMaterial.SetTexture("_MainTex", dropletPatternTexture);

        CreateEffectQuad();
    }

    void CreateEffectQuad()
    {
        if (effectQuad != null) Destroy(effectQuad);
        effectQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        effectQuad.name = "VR_Lens_Effect_Overlay";
        Destroy(effectQuad.GetComponent<Collider>());
        effectQuad.transform.SetParent(xrCamera.transform);
        effectQuad.transform.localPosition = new Vector3(0, 0, 0.45f);
        effectQuad.transform.localRotation = Quaternion.identity;
        effectQuad.transform.localScale = new Vector3(1.8f, 1.2f, 1f);
        Renderer rend = effectQuad.GetComponent<Renderer>();
        rend.material = runtimeMaterial;
        effectQuad.SetActive(false);
    }

    void Update()
    {
        // ⭐ 修改：对照组强制关闭特效
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            if (effectQuad != null && effectQuad.activeSelf) effectQuad.SetActive(false);
            return;
        }

        UpdateEffectIntensity();
    }

    void UpdateEffectIntensity()
    {
        targetWaterIntensity = isInWater ? effectSettings.waterMaxIntensity : 0f;
        float finalTarget = Mathf.Max(targetWaterIntensity, targetHealthIntensity);
        float speed = (finalTarget > currentIntensity) ? effectSettings.fadeInSpeed : effectSettings.fadeOutSpeed;
        currentIntensity = Mathf.MoveTowards(currentIntensity, finalTarget, speed * Time.deltaTime);

        if (effectQuad != null)
        {
            bool shouldShow = currentIntensity > 0.01f;
            if (effectQuad.activeSelf != shouldShow) effectQuad.SetActive(shouldShow);
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat("_Intensity", currentIntensity);
            runtimeMaterial.SetColor("_TintColor", effectSettings.tintColor);
        }
    }

    public void EnterWaterEffect() { isInWater = true; }
    public void ExitWaterEffect() { isInWater = false; }

    public void UpdateHealthEffect(float healthPercent)
    {
        if (healthPercent <= healthThreshold)
        {
            float factor = 1.0f - (healthPercent / healthThreshold);
            targetHealthIntensity = Mathf.Clamp01(factor) * healthMaxIntensity;
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