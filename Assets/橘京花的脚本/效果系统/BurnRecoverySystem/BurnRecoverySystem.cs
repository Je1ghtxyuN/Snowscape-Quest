using UnityEngine;
using UnityEngine.UI;

public class BurnRecoverySystem : MonoBehaviour
{
    public static BurnRecoverySystem Instance { get; private set; }

    [Header("UI 生成设置")]
    public GameObject bodyUIPrefab;
    public Vector3 uiPositionOffset = new Vector3(-0.4f, -0.2f, 1.5f);
    public float uiScale = 0.002f;

    [Header("康复逻辑设置")]
    public int targetCrystalCount = 10;
    public string targetRendererName = "Renderer_Body";
    public float colorSmoothSpeed = 2f;

    [Header("高级视觉设置 (透红效果)")]
    [Tooltip("烧伤时的皮肤底色倾向 (建议浅粉红)")]
    public Color burnTint = new Color(1f, 0.6f, 0.6f);

    [Tooltip("烧伤时的热度发光颜色 (建议暗红)")]
    [ColorUsage(false, true)] // 允许HDR颜色调节亮度
    public Color burnEmission = new Color(0.3f, 0.0f, 0.0f);

    // --- 内部变量 ---
    private Image bodyFillImage;
    private int currentCrystals = 0;
    private Renderer targetRenderer;
    private Material runtimeMaterial;

    private Color originalBaseColor; // 原始皮肤颜色
    private Color currentTargetBase;
    private Color currentTargetEmission;

    // 属性ID缓存 (比字符串更快)
    private int idBaseColor;
    private int idEmissionColor;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateRecoveryUI();
        InitializeMaterial();
        UpdateVisuals(0);
    }

    void Update()
    {
        if (runtimeMaterial != null)
        {
            // 1. 平滑过渡底色 (保持皮肤纹理，只是色调变回来)
            Color currentBase = runtimeMaterial.GetColor(idBaseColor);
            Color smoothBase = Color.Lerp(currentBase, currentTargetBase, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idBaseColor, smoothBase);

            // 2. 平滑过渡自发光 (模拟热气消退)
            Color currentEmis = runtimeMaterial.GetColor(idEmissionColor);
            Color smoothEmis = Color.Lerp(currentEmis, currentTargetEmission, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idEmissionColor, smoothEmis);
        }
    }

    public void AddRecoveryProgress()
    {
        currentCrystals++;
        float progress = Mathf.Clamp01((float)currentCrystals / targetCrystalCount);
        UpdateVisuals(progress);
    }

    public float GetRecoveryProgress()
    {
        if (targetCrystalCount == 0) return 0f;
        return Mathf.Clamp01((float)currentCrystals / targetCrystalCount);
    }

    private void UpdateVisuals(float progress)
    {
        // UI 更新
        if (bodyFillImage != null) bodyFillImage.fillAmount = progress;

        // --- 视觉计算核心 ---

        // 1. 底色计算: 
        // 进度0 (烧伤) = 原始颜色 * 烧伤滤镜 (变红一点)
        // 进度1 (健康) = 原始颜色
        currentTargetBase = Color.Lerp(originalBaseColor * burnTint, originalBaseColor, progress);

        // 2. 发光计算:
        // 进度0 (烧伤) = 设定的发光色 (透红)
        // 进度1 (健康) = 黑色 (不发光)
        currentTargetEmission = Color.Lerp(burnEmission, Color.black, progress);
    }

    void InitializeMaterial()
    {
        Transform foundObj = FindDeepChild(transform, targetRendererName);
        if (foundObj == null) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        else targetRenderer = foundObj.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;

            // 自动判断属性名 (兼容 URP 和 Standard)
            if (runtimeMaterial.HasProperty("_BaseColor")) idBaseColor = Shader.PropertyToID("_BaseColor");
            else if (runtimeMaterial.HasProperty("baseColorFactor")) idBaseColor = Shader.PropertyToID("baseColorFactor");
            else idBaseColor = Shader.PropertyToID("_Color");

            if (runtimeMaterial.HasProperty("_EmissionColor")) idEmissionColor = Shader.PropertyToID("_EmissionColor");
            else idEmissionColor = Shader.PropertyToID("emissiveFactor"); // glTF naming

            // 记录原始皮肤颜色
            if (runtimeMaterial.HasProperty(idBaseColor))
            {
                originalBaseColor = runtimeMaterial.GetColor(idBaseColor);
            }

            // 开启 Keyword 确保发光生效
            runtimeMaterial.EnableKeyword("_EMISSION");
        }
    }

    // ... [保留 CreateRecoveryUI 和 FindDeepChild 代码不变] ...
    private void CreateRecoveryUI()
    {
        if (bodyUIPrefab == null) return;
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        GameObject uiInstance = Instantiate(bodyUIPrefab, mainCam.transform);
        uiInstance.transform.localPosition = uiPositionOffset;
        uiInstance.transform.localRotation = Quaternion.identity;
        uiInstance.transform.localScale = Vector3.one * uiScale;

        Transform fillTransform = FindDeepChild(uiInstance.transform, "Fill_Blue");
        if (fillTransform != null) bodyFillImage = fillTransform.GetComponent<Image>();
        else
        {
            Image[] images = uiInstance.GetComponentsInChildren<Image>();
            foreach (var img in images) if (img.type == Image.Type.Filled) { bodyFillImage = img; break; }
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}