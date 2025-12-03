using UnityEngine;
using System.Collections;

public class IceCrystalEffectSystem : MonoBehaviour
{
    [Header("核心设置")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioSource audioSource;

    [Header("视觉参数")]
    [SerializeField] private Texture2D iceNormalMap;
    [SerializeField] private Color frostColor = new Color(0.6f, 0.8f, 1f);

    [Tooltip("结冰时的平滑度 (对于glTF材质，会自动转换为低粗糙度)")]
    [Range(0f, 1f)][SerializeField] private float iceSmoothness = 0.85f;

    [SerializeField] private Color iceEmissionColor = new Color(0, 0.2f, 0.5f);

    [Header("动画参数")]
    [SerializeField] private float effectDuration = 5f;
    [SerializeField] private float transitionSpeed = 2f;

    [Header("其他")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private ParticleSystem iceCrystalParticles;
    [SerializeField] private float healAmount = 20f;

    [SerializeField] private string targetRendererName = "Renderer_Body";

    // 内部变量
    private Renderer targetRenderer;
    private Material runtimeMaterial;
    private Coroutine effectCoroutine;
    private int crystalCount = 0;

    // 记录原始值
    private Color originalBaseColor;
    private float originalVal; // 可能是平滑度，也可能是粗糙度
    private Texture originalNormalMap;
    private Color originalEmissionColor;

    // 属性名缓存 (自动适配)
    private string nameColor;
    private string nameSmoothness; // 或者是 Roughness
    private string nameNormal;
    private string nameEmission;
    private bool isRoughnessMode = false; // 标记是否为粗糙度模式 (glTF)

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        InitializeMaterialSystem();
    }

    void InitializeMaterialSystem()
    {
        Transform foundObj = FindDeepChild(transform, targetRendererName);
        if (foundObj != null) targetRenderer = foundObj.GetComponent<Renderer>();
        else targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            Debug.Log($"[IceEffect] 正在适配材质: {runtimeMaterial.shader.name}");

            // --- 1. 自动匹配颜色属性 ---
            if (runtimeMaterial.HasProperty("baseColorFactor")) nameColor = "baseColorFactor"; // glTF
            else if (runtimeMaterial.HasProperty("_BaseColor")) nameColor = "_BaseColor"; // URP
            else nameColor = "_Color"; // Built-in

            if (runtimeMaterial.HasProperty(nameColor))
                originalBaseColor = runtimeMaterial.GetColor(nameColor);
            else
                Debug.LogError("❌ 无法识别颜色属性名！");

            // --- 2. 自动匹配平滑度/粗糙度 ---
            if (runtimeMaterial.HasProperty("roughnessFactor"))
            {
                nameSmoothness = "roughnessFactor"; // glTF
                isRoughnessMode = true;
                Debug.Log("   -> 检测到 glTF 材质，使用粗糙度 (Roughness) 模式");
            }
            else if (runtimeMaterial.HasProperty("_Roughness"))
            {
                nameSmoothness = "_Roughness";
                isRoughnessMode = true;
            }
            else if (runtimeMaterial.HasProperty("_Smoothness"))
            {
                nameSmoothness = "_Smoothness"; // URP
                isRoughnessMode = false;
            }

            if (!string.IsNullOrEmpty(nameSmoothness))
                originalVal = runtimeMaterial.GetFloat(nameSmoothness);

            // --- 3. 自动匹配法线 ---
            if (runtimeMaterial.HasProperty("normalTexture")) nameNormal = "normalTexture"; // glTF
            else nameNormal = "_BumpMap"; // Unity Standard

            if (runtimeMaterial.HasProperty(nameNormal))
                originalNormalMap = runtimeMaterial.GetTexture(nameNormal);

            // --- 4. 自动匹配自发光 ---
            if (runtimeMaterial.HasProperty("emissiveFactor")) nameEmission = "emissiveFactor"; // glTF
            else nameEmission = "_EmissionColor";

            if (runtimeMaterial.HasProperty(nameEmission))
                originalEmissionColor = runtimeMaterial.GetColor(nameEmission);
        }
        else
        {
            Debug.LogError("[IceEffect] ❌ 未找到 Renderer");
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name)) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void CollectIceCrystal()
    {
        crystalCount++;
        if (collectSound != null && audioSource != null) audioSource.PlayOneShot(collectSound);
        if (iceCrystalParticles != null) iceCrystalParticles.Play();
        if (playerHealth != null) playerHealth.Heal(healAmount);

        if (runtimeMaterial != null)
        {
            if (effectCoroutine != null) StopCoroutine(effectCoroutine);
            effectCoroutine = StartCoroutine(IceEffectRoutine());
        }
    }

    private IEnumerator IceEffectRoutine()
    {
        // 设置法线
        if (iceNormalMap != null) runtimeMaterial.SetTexture(nameNormal, iceNormalMap);

        // 尝试开启自发光 (部分Shader需要)
        runtimeMaterial.EnableKeyword("_EMISSION");

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(timer);

            // 1. 颜色变化
            Color targetColor = originalBaseColor * frostColor;
            runtimeMaterial.SetColor(nameColor, Color.Lerp(originalBaseColor, targetColor, t));

            // 2. 平滑度/粗糙度变化
            // 如果是粗糙度模式：目标值 = 1 - 平滑度 (因为 0 是光滑)
            // 如果是平滑度模式：目标值 = 平滑度 (因为 1 是光滑)
            float targetSmoothVal = isRoughnessMode ? (1 - iceSmoothness) : iceSmoothness;
            runtimeMaterial.SetFloat(nameSmoothness, Mathf.Lerp(originalVal, targetSmoothVal, t));

            // 3. 自发光
            if (!string.IsNullOrEmpty(nameEmission))
                runtimeMaterial.SetColor(nameEmission, Color.Lerp(originalEmissionColor, iceEmissionColor, t));

            yield return null;
        }

        yield return new WaitForSeconds(effectDuration);

        // 融化
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(timer);

            Color currentBase = runtimeMaterial.GetColor(nameColor);
            runtimeMaterial.SetColor(nameColor, Color.Lerp(currentBase, originalBaseColor, t));

            float currentSmooth = runtimeMaterial.GetFloat(nameSmoothness);
            float targetSmoothVal = isRoughnessMode ? (1 - iceSmoothness) : iceSmoothness;
            runtimeMaterial.SetFloat(nameSmoothness, Mathf.Lerp(targetSmoothVal, originalVal, t));

            if (!string.IsNullOrEmpty(nameEmission))
                runtimeMaterial.SetColor(nameEmission, Color.Lerp(iceEmissionColor, originalEmissionColor, t));

            yield return null;
        }

        // 还原
        if (originalNormalMap != null) runtimeMaterial.SetTexture(nameNormal, originalNormalMap);
        else runtimeMaterial.SetTexture(nameNormal, null);
    }

    public int GetCrystalCount() => crystalCount;
    void OnDestroy() { if (runtimeMaterial != null) Destroy(runtimeMaterial); }
}