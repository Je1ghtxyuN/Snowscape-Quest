using UnityEngine;
using System.Collections;

public class RPMHandWaterEffect : MonoBehaviour
{
    [Header("触发设置")]
    public string waterTag = "Water";
    public float dryDuration = 5.0f;

    [Header("水滴素材")]
    [Tooltip("你的水滴法线贴图 (记得在导入设置选 Normal Map)")]
    public Texture2D waterDropletNormal;

    [Header("湿润参数 (glTF PBR)")]
    [Tooltip("湿润时的粗糙度 (0=极光滑/水面，1=粗糙)")]
    [Range(0f, 1f)] public float wetRoughness = 0.05f; // 稍微给一点点值，比完全0更自然

    [Tooltip("湿润时的颜色变暗系数 (0.8表示变暗20%)")]
    [Range(0f, 1f)] public float wetDarkness = 0.75f;

    [Header("目标渲染器")]
    public Renderer handRenderer;

    // --- 内部变量 ---
    private Material runtimeMaterial;
    private Coroutine dryingCoroutine;
    private bool isInWater = false;

    // 原始数据备份
    private Color originalColor;
    private float originalRoughness;
    private Texture originalNormalMap;

    // 属性 ID
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
        if (handRenderer == null)
        {
            Debug.LogError("❌ [RPMHandWater] 找不到 Renderer，请手动赋值！");
            return;
        }

        runtimeMaterial = handRenderer.material;
        Shader shader = runtimeMaterial.shader;

        // --- 1. 智能查找属性 ID (增强版) ---

        // A. 查找颜色 (Color)
        // 优先级：glTF标准 -> URP标准 -> 截图中的命名风格 -> Built-in
        if (HasProp("baseColorFactor")) id_BaseColor = Shader.PropertyToID("baseColorFactor");
        else if (HasProp("_BaseColor")) id_BaseColor = Shader.PropertyToID("_BaseColor");
        else if (HasProp("BaseColor")) id_BaseColor = Shader.PropertyToID("BaseColor"); // 对应截图
        else id_BaseColor = Shader.PropertyToID("_Color");

        // B. 查找粗糙度 (Roughness)
        // 截图明确显示有 Roughness，所以我们在前面多加几个检测
        if (HasProp("roughnessFactor")) id_Roughness = Shader.PropertyToID("roughnessFactor");
        else if (HasProp("_Roughness")) id_Roughness = Shader.PropertyToID("_Roughness");
        else if (HasProp("Roughness")) id_Roughness = Shader.PropertyToID("Roughness"); // 对应截图
        else id_Roughness = Shader.PropertyToID("_Smoothness"); // 备用

        // C. 查找法线贴图 (Normal Map)
        // 截图中显示 "Normal Tex"，内部名可能是 _NormalTex 或 NormalTex
        if (HasProp("normalTexture")) id_NormalMap = Shader.PropertyToID("normalTexture");
        else if (HasProp("_NormalTex")) id_NormalMap = Shader.PropertyToID("_NormalTex"); // 对应截图可能的内部名
        else if (HasProp("NormalTex")) id_NormalMap = Shader.PropertyToID("NormalTex");   // 对应截图可能的内部名
        else if (HasProp("_BumpMap")) id_NormalMap = Shader.PropertyToID("_BumpMap");     // Unity标准
        else id_NormalMap = Shader.PropertyToID("_NormalMap");

        // --- 2. 备份原始值 ---
        if (runtimeMaterial.HasProperty(id_BaseColor))
            originalColor = runtimeMaterial.GetColor(id_BaseColor);
        else
            Debug.LogWarning($"⚠️ [RPMHandWater] 未找到颜色属性，尝试过的ID: {id_BaseColor}");

        if (runtimeMaterial.HasProperty(id_Roughness))
            originalRoughness = runtimeMaterial.GetFloat(id_Roughness);
        else
            Debug.LogWarning("⚠️ [RPMHandWater] 未找到粗糙度属性 (Roughness)");

        if (runtimeMaterial.HasProperty(id_NormalMap))
            originalNormalMap = runtimeMaterial.GetTexture(id_NormalMap);

        //// 调试日志：告诉你到底找到了哪些属性
        //Debug.Log($"✅ [RPMHandWater] 初始化完成.\n" +
        //          $"Color ID: {GetPropName(id_BaseColor)}\n" +
        //          $"Roughness ID: {GetPropName(id_Roughness)} (原值: {originalRoughness})\n" +
        //          $"NormalMap ID: {GetPropName(id_NormalMap)}");
    }

    // 辅助函数：简化代码
    bool HasProp(string name) => runtimeMaterial.HasProperty(name);
    //string GetPropName(int id) => Shader.PropertyToID(id).ToString(); // 简化的调试显示

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            isInWater = true;
            StopDrying();
            ApplyWetness(1.0f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            isInWater = false;
            StartDrying();
        }
    }

    private void ApplyWetness(float t)
    {
        if (runtimeMaterial == null) return;

        // 1. 颜色变暗
        if (runtimeMaterial.HasProperty(id_BaseColor))
        {
            Color targetColor = originalColor * wetDarkness;
            runtimeMaterial.SetColor(id_BaseColor, Color.Lerp(originalColor, targetColor, t));
        }

        // 2. 粗糙度降低 (变光滑)
        if (runtimeMaterial.HasProperty(id_Roughness))
        {
            runtimeMaterial.SetFloat(id_Roughness, Mathf.Lerp(originalRoughness, wetRoughness, t));
        }

        // 3. 切换法线贴图
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