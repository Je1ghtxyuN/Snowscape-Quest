using UnityEngine;
using System.Collections;

public class RPMHandWaterEffect : MonoBehaviour
{
    [Header("触发设置")]
    public string waterTag = "Water";
    public float dryDuration = 5.0f;

    [Header("水滴素材")]
    public Texture2D waterDropletNormal;

    [Header("湿润参数")]
    [Range(0f, 1f)] public float wetRoughness = 0.05f;
    [Range(0f, 1f)] public float wetDarkness = 0.75f;

    [Header("目标渲染器")]
    public Renderer handRenderer;

    private Material runtimeMaterial;
    private Coroutine dryingCoroutine;
    private bool isInWater = false;

    private Color originalColor;
    private float originalRoughness;
    private Texture originalNormalMap;

    private int id_BaseColor;
    private int id_Roughness;
    private int id_NormalMap;

    void Start()
    {
        InitializeMaterial();
    }

    void InitializeMaterial()
    {
        if (handRenderer == null) handRenderer = GetComponentInChildren<Renderer>();
        if (handRenderer == null) return;

        runtimeMaterial = handRenderer.material;

        if (HasProp("baseColorFactor")) id_BaseColor = Shader.PropertyToID("baseColorFactor");
        else if (HasProp("_BaseColor")) id_BaseColor = Shader.PropertyToID("_BaseColor");
        else id_BaseColor = Shader.PropertyToID("_Color");

        if (HasProp("roughnessFactor")) id_Roughness = Shader.PropertyToID("roughnessFactor");
        else if (HasProp("_Roughness")) id_Roughness = Shader.PropertyToID("_Roughness");
        else id_Roughness = Shader.PropertyToID("_Smoothness");

        if (HasProp("normalTexture")) id_NormalMap = Shader.PropertyToID("normalTexture");
        else if (HasProp("_NormalTex")) id_NormalMap = Shader.PropertyToID("_NormalTex");
        else id_NormalMap = Shader.PropertyToID("_NormalMap");

        if (runtimeMaterial.HasProperty(id_BaseColor)) originalColor = runtimeMaterial.GetColor(id_BaseColor);
        if (runtimeMaterial.HasProperty(id_Roughness)) originalRoughness = runtimeMaterial.GetFloat(id_Roughness);
        if (runtimeMaterial.HasProperty(id_NormalMap)) originalNormalMap = runtimeMaterial.GetTexture(id_NormalMap);
    }

    bool HasProp(string name) => runtimeMaterial.HasProperty(name);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            // ⭐ 修改：对照组不产生变湿效果
            if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
            {
                return;
            }

            isInWater = true;
            StopDrying();
            ApplyWetness(1.0f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            // 如果之前根本没变湿（对照组），这里其实无所谓，但保持逻辑完整
            isInWater = false;
            StartDrying();
        }
    }

    private void ApplyWetness(float t)
    {
        if (runtimeMaterial == null) return;

        if (runtimeMaterial.HasProperty(id_BaseColor))
        {
            Color targetColor = originalColor * wetDarkness;
            runtimeMaterial.SetColor(id_BaseColor, Color.Lerp(originalColor, targetColor, t));
        }

        if (runtimeMaterial.HasProperty(id_Roughness))
        {
            runtimeMaterial.SetFloat(id_Roughness, Mathf.Lerp(originalRoughness, wetRoughness, t));
        }

        if (runtimeMaterial.HasProperty(id_NormalMap))
        {
            if (t > 0.1f && waterDropletNormal != null)
                runtimeMaterial.SetTexture(id_NormalMap, waterDropletNormal);
            else
                runtimeMaterial.SetTexture(id_NormalMap, originalNormalMap);
        }
    }

    private void StopDrying()
    {
        if (dryingCoroutine != null) StopCoroutine(dryingCoroutine);
    }

    private void StartDrying()
    {
        StopDrying();
        dryingCoroutine = StartCoroutine(DryingRoutine());
    }

    private IEnumerator DryingRoutine()
    {
        float timer = 0f;
        yield return new WaitForSeconds(0.5f);

        while (timer < dryDuration)
        {
            timer += Time.deltaTime;
            float t = 1.0f - (timer / dryDuration);
            ApplyWetness(t);
            yield return null;
        }
        ApplyWetness(0f);
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}