using UnityEngine;
using System.Collections;

public class RPMIceEffect : MonoBehaviour
{
    [Header("设置")]
    public float freezeDuration = 5.0f;
    public float transitionSpeed = 2.0f;

    [Header("冰晶素材")]
    public Texture2D iceNormalMap;

    [Header("冰霜视觉参数 (关键调整)")]
    [Tooltip("颜色叠加：设为白色(1,1,1,1)以防止变暗。或者稍微带点蓝。")]
    public Color frostTint = new Color(0.9f, 0.95f, 1.0f); // ⭐ 改得更白了

    [Tooltip("粗糙度：霜是粗糙的！设为 0.4-0.6 会看起来更白；设为 0 会像黑镜子。")]
    [Range(0f, 1f)]
    public float iceRoughness = 0.45f; // ⭐ 调高了，模拟磨砂质感

    [Tooltip("自发光：这是变白的关键！使用高强度的青白色。")]
    [ColorUsage(false, true)]
    public Color iceEmission = new Color(0.4f, 0.7f, 1.0f) * 1.5f; // ⭐ 颜色改亮，强度增加

    [Header("目标渲染器")]
    public Renderer targetRenderer;

    // --- 内部变量 ---
    private Material runtimeMaterial;
    private Coroutine effectCoroutine;

    // 原始数据
    private Color originalColor;
    private float originalRoughness;
    private Texture originalNormalMap;
    private Color originalEmission;

    // 属性 ID
    private int id_BaseColor;
    private int id_Roughness;
    private int id_NormalMap;
    private int id_Emission;

    void Start()
    {
        InitializeMaterial();
    }

    void InitializeMaterial()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer == null) return;

        runtimeMaterial = targetRenderer.material;

        // 1. 查找属性 (保持原样)
        if (HasProp("baseColorFactor")) id_BaseColor = Shader.PropertyToID("baseColorFactor");
        else if (HasProp("BaseColor")) id_BaseColor = Shader.PropertyToID("BaseColor");
        else id_BaseColor = Shader.PropertyToID("_BaseColor");

        if (HasProp("roughnessFactor")) id_Roughness = Shader.PropertyToID("roughnessFactor");
        else if (HasProp("Roughness")) id_Roughness = Shader.PropertyToID("Roughness");
        else id_Roughness = Shader.PropertyToID("_Roughness");

        if (HasProp("normalTexture")) id_NormalMap = Shader.PropertyToID("normalTexture");
        else if (HasProp("NormalTex")) id_NormalMap = Shader.PropertyToID("NormalTex");
        else id_NormalMap = Shader.PropertyToID("_NormalMap");

        if (HasProp("emissiveFactor")) id_Emission = Shader.PropertyToID("emissiveFactor");
        else if (HasProp("EmissiveColor")) id_Emission = Shader.PropertyToID("EmissiveColor");
        else if (HasProp("_EmissionColor")) id_Emission = Shader.PropertyToID("_EmissionColor");
        else id_Emission = Shader.PropertyToID("Emissive");

        // 2. 备份
        if (HasPropID(id_BaseColor)) originalColor = runtimeMaterial.GetColor(id_BaseColor);
        if (HasPropID(id_Roughness)) originalRoughness = runtimeMaterial.GetFloat(id_Roughness);
        if (HasPropID(id_NormalMap)) originalNormalMap = runtimeMaterial.GetTexture(id_NormalMap);

        // 备份自发光 (如果是黑色，我们默认它是关的)
        if (HasPropID(id_Emission)) originalEmission = runtimeMaterial.GetColor(id_Emission);
        else originalEmission = Color.black;
    }

    bool HasProp(string name) => runtimeMaterial.HasProperty(name);
    bool HasPropID(int id) => runtimeMaterial.HasProperty(id);

    public void ActivateIceEffect()
    {
        if (effectCoroutine != null) StopCoroutine(effectCoroutine);
        effectCoroutine = StartCoroutine(IceProcessRoutine());
    }

    private IEnumerator IceProcessRoutine()
    {
        // 1. 瞬间切换法线 (让冰的纹理立马出来)
        if (iceNormalMap != null && HasPropID(id_NormalMap))
        {
            runtimeMaterial.SetTexture(id_NormalMap, iceNormalMap);
        }

        // 2. 结冰过程
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            ApplyIceMaterial(Mathf.Clamp01(timer));
            yield return null;
        }

        // 3. 保持
        yield return new WaitForSeconds(freezeDuration);

        // 4. 融化过程
        timer = 1f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime * transitionSpeed;
            ApplyIceMaterial(Mathf.Clamp01(timer));
            yield return null;
        }

        // 5. 还原
        if (HasPropID(id_NormalMap))
        {
            runtimeMaterial.SetTexture(id_NormalMap, originalNormalMap);
        }
        ApplyIceMaterial(0f);
    }

    private void ApplyIceMaterial(float t)
    {
        if (runtimeMaterial == null) return;

        // A. 颜色：稍微混合一点点就好，不要让它变得太黑
        if (HasPropID(id_BaseColor))
        {
            // 使用 LerpUnclamped 稍微允许一点过曝(如果Shader支持)，或者常规Lerp
            // 这里我们尽量保持原色亮度，只做微调
            Color targetColor = originalColor * frostTint;
            runtimeMaterial.SetColor(id_BaseColor, Color.Lerp(originalColor, targetColor, t));
        }

        // B. 粗糙度：这是关键！
        if (HasPropID(id_Roughness))
        {
            // 从原来的皮肤粗糙度 -> 目标的冰霜粗糙度 (建议 0.4 - 0.5)
            runtimeMaterial.SetFloat(id_Roughness, Mathf.Lerp(originalRoughness, iceRoughness, t));
        }

        // C. 自发光：这是“变白”的核心动力
        if (HasPropID(id_Emission))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");

            // 将原来的自发光(通常是黑) 混合到 强烈的青白色
            runtimeMaterial.SetColor(id_Emission, Color.Lerp(originalEmission, iceEmission, t));
        }
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}