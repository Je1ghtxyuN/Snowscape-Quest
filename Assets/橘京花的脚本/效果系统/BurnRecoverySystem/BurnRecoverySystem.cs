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
    public Color burnTint = new Color(0.7f, 0.5f, 0.5f);
    [ColorUsage(false, true)]
    public Color burnEmission = new Color(0.4f, 0.1f, 0.0f);

    [Header("视觉效果：完全恢复/冷白状态 (终点)")]
    [ColorUsage(false, true)]
    public Color healthyColdTint = new Color(1.1f, 1.15f, 1.4f);
    [ColorUsage(false, true)]
    public Color healthyEmission = Color.black;

    [Header("调试功能")]
    public bool debugPreviewFinalEffect = false;

    // --- 内部变量 ---
    private Image bodyFillImage;
    private int currentCrystals = 0;
    private Renderer targetRenderer;
    private Material runtimeMaterial;

    private Color originalBaseColor;
    private Color currentTargetBase;
    private Color currentTargetEmission;

    private bool _lastDebugState = false;
    private int idBaseColor;
    private int idEmissionColor;

    // ⭐ 新增：防止语音重复播放的开关
    private bool hasPlayedRecoveryVoice = false;

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
        if (debugPreviewFinalEffect != _lastDebugState)
        {
            _lastDebugState = debugPreviewFinalEffect;
            UpdateVisuals(debugPreviewFinalEffect ? 1.0f : GetRecoveryProgress());
        }

        if (runtimeMaterial != null)
        {
            Color currentBase = runtimeMaterial.GetColor(idBaseColor);
            Color smoothBase = Color.Lerp(currentBase, currentTargetBase, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idBaseColor, smoothBase);

            Color currentEmis = runtimeMaterial.GetColor(idEmissionColor);
            Color smoothEmis = Color.Lerp(currentEmis, currentTargetEmission, Time.deltaTime * colorSmoothSpeed);
            runtimeMaterial.SetColor(idEmissionColor, smoothEmis);
        }
    }

    public void AddRecoveryProgress()
    {
        currentCrystals++;

        float progress = GetRecoveryProgress();

        if (!debugPreviewFinalEffect)
        {
            UpdateVisuals(progress);
        }

        // ⭐ 新增逻辑：检查是否达到 100% 且没有播放过语音
        if (progress >= 1.0f && !hasPlayedRecoveryVoice)
        {
            if (PlayerVoiceSystem.Instance != null)
            {
                // 播放身体完全恢复的语音
                PlayerVoiceSystem.Instance.PlayVoice("Full_Recovery");
            }
            hasPlayedRecoveryVoice = true; // 锁死，防止吃第11个水晶时又播一遍
            Debug.Log("❄️ 身体完全恢复！播放暗示语音。");
        }
    }

    public float GetRecoveryProgress()
    {
        if (targetCrystalCount == 0) return 0f;
        return Mathf.Clamp01((float)currentCrystals / targetCrystalCount);
    }

    private void UpdateVisuals(float progress)
    {
        if (bodyFillImage != null) bodyFillImage.fillAmount = progress;
        currentTargetBase = Color.Lerp(originalBaseColor * burnTint, originalBaseColor * healthyColdTint, progress);
        currentTargetEmission = Color.Lerp(burnEmission, healthyEmission, progress);
    }

    void InitializeMaterial()
    {
        Transform foundObj = FindDeepChild(transform, targetRendererName);
        if (foundObj == null) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        else targetRenderer = foundObj.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;

            if (runtimeMaterial.HasProperty("_BaseColor")) idBaseColor = Shader.PropertyToID("_BaseColor");
            else if (runtimeMaterial.HasProperty("baseColorFactor")) idBaseColor = Shader.PropertyToID("baseColorFactor");
            else idBaseColor = Shader.PropertyToID("_Color");

            if (runtimeMaterial.HasProperty("_EmissionColor")) idEmissionColor = Shader.PropertyToID("_EmissionColor");
            else idEmissionColor = Shader.PropertyToID("emissiveFactor");

            if (runtimeMaterial.HasProperty(idBaseColor))
            {
                originalBaseColor = runtimeMaterial.GetColor(idBaseColor);
            }

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