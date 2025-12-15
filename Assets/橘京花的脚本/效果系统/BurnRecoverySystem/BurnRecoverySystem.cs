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

    [Header("视觉效果：烧伤状态 (起点)")]
    [Tooltip("烧伤时的皮肤底色倾向 (建议暗红/焦黑以增加对比)")]
    public Color burnTint = new Color(0.7f, 0.5f, 0.5f);

    [Tooltip("烧伤时的热度发光颜色 (建议暗红)")]
    [ColorUsage(false, true)] // 允许HDR颜色调节亮度
    public Color burnEmission = new Color(0.4f, 0.1f, 0.0f);

    [Header("视觉效果：完全恢复/冷白状态 (终点)")]
    [Tooltip("恢复后的皮肤底色倾向 (RGB大于1可以实现过曝的冷白效果)")]
    [ColorUsage(false, true)] // ⭐ 关键：允许 HDR，设为 (1.2, 1.2, 1.5) 这种值可以让皮肤看起来发光般白皙
    public Color healthyColdTint = new Color(1.1f, 1.15f, 1.4f);

    [Tooltip("恢复后的微弱发光 (建议黑色不发光，或者极微弱的冰蓝光)")]
    [ColorUsage(false, true)]
    public Color healthyEmission = Color.black;

    [Header("调试功能")]
    [Tooltip("🔥 勾选此项，直接预览最终的冷白手部效果")]
    public bool debugPreviewFinalEffect = false;

    // --- 内部变量 ---
    private Image bodyFillImage;
    private int currentCrystals = 0;
    private Renderer targetRenderer;
    private Material runtimeMaterial;

    private Color originalBaseColor; // 原始皮肤颜色
    private Color currentTargetBase;
    private Color currentTargetEmission;

    private bool _lastDebugState = false; // 用于检测 checkbox 变化
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
        UpdateVisuals(0); // 初始化为烧伤状态
    }

    void Update()
    {
        // ⭐ 实时检测调试开关的变化
        if (debugPreviewFinalEffect != _lastDebugState)
        {
            _lastDebugState = debugPreviewFinalEffect;
            // 如果勾选，强制设为 1.0 (满状态)；如果取消，恢复当前实际进度
            UpdateVisuals(debugPreviewFinalEffect ? 1.0f : GetRecoveryProgress());
        }

        if (runtimeMaterial != null)
        {
            // 1. 平滑过渡底色
            Color currentBase = runtimeMaterial.GetColor(idBaseColor);
            Color smoothBase = Color.Lerp(currentBase, currentTargetBase, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idBaseColor, smoothBase);

            // 2. 平滑过渡自发光
            Color currentEmis = runtimeMaterial.GetColor(idEmissionColor);
            Color smoothEmis = Color.Lerp(currentEmis, currentTargetEmission, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idEmissionColor, smoothEmis);
        }
    }

    public void AddRecoveryProgress()
    {
        currentCrystals++;

        // 如果正在调试模式，就不更新视觉目标（以免覆盖调试效果）
        if (!debugPreviewFinalEffect)
        {
            UpdateVisuals(GetRecoveryProgress());
        }
    }

    public float GetRecoveryProgress()
    {
        if (targetCrystalCount == 0) return 0f;
        // 允许进度超过 1 (如果你想让手越来越亮)，或者限制在 1
        return Mathf.Clamp01((float)currentCrystals / targetCrystalCount);
    }

    private void UpdateVisuals(float progress)
    {
        // UI 更新 (UI 始终显示真实进度，不受调试开关影响，方便对比)
        if (bodyFillImage != null) bodyFillImage.fillAmount = GetRecoveryProgress();

        // --- 视觉计算核心 ---

        // 1. 底色计算: 
        // 进度0 = 原始色 * 烧伤红 (变暗变红)
        // 进度1 = 原始色 * 冷白Tint (变亮变蓝)
        // ⭐ 这里用 Color.Lerp 实现从“焦黑”到“冷白”的平滑转变
        currentTargetBase = Color.Lerp(originalBaseColor * burnTint, originalBaseColor * healthyColdTint, progress);

        // 2. 发光计算:
        // 进度0 = 烧伤红光
        // 进度1 = 黑色 (或自定义的冰蓝光)
        currentTargetEmission = Color.Lerp(burnEmission, healthyEmission, progress);
    }

    void InitializeMaterial()
    {
        Transform foundObj = FindDeepChild(transform, targetRendererName);
        if (foundObj == null) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        else targetRenderer = foundObj.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // 必须使用 material (实例)，不能用 sharedMaterial，否则会改坏资源文件
            runtimeMaterial = targetRenderer.material;

            // 自动判断属性名
            if (runtimeMaterial.HasProperty("_BaseColor")) idBaseColor = Shader.PropertyToID("_BaseColor");
            else if (runtimeMaterial.HasProperty("baseColorFactor")) idBaseColor = Shader.PropertyToID("baseColorFactor");
            else idBaseColor = Shader.PropertyToID("_Color");

            if (runtimeMaterial.HasProperty("_EmissionColor")) idEmissionColor = Shader.PropertyToID("_EmissionColor");
            else idEmissionColor = Shader.PropertyToID("emissiveFactor");

            // 记录原始皮肤颜色
            if (runtimeMaterial.HasProperty(idBaseColor))
            {
                originalBaseColor = runtimeMaterial.GetColor(idBaseColor);
            }

            // 开启 Keyword 确保发光生效
            runtimeMaterial.EnableKeyword("_EMISSION");
        }
    }

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