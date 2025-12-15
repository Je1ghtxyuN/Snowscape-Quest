using UnityEngine;
using System.Collections;

public class HandWaterEffect : MonoBehaviour
{
    [Header("触发设置")]
    public string waterTag = "Water"; // 瀑布的Tag
    public float dryDuration = 5.0f;  // 离开水后多久变干

    [Header("视觉素材")]
    [Tooltip("请在这里放入你的水滴法线贴图 (记得在导入设置里选Normal Map)")]
    public Texture2D waterDropletNormal;

    [Header("湿润参数")]
    [Range(0f, 1f)] public float wetSmoothness = 0.95f; // 湿润时的光滑度
    [Range(0f, 1f)] public float wetDarkness = 0.8f;    // 湿润时颜色变暗的程度 (1是不变，0.5是变黑一半)
    public float detailTiling = 3.0f; // 水滴的密度（重复次数）

    [Header("目标")]
    public Renderer handRenderer; // 手部渲染器

    // 内部状态
    private Material runtimeMaterial;
    private Coroutine dryingCoroutine;
    private bool isInWater = false;

    // 原始数据备份
    private float originalSmoothness;
    private Color originalColor;
    private float originalDetailScale;

    // Shader 属性 ID 缓存 (性能优化)
    private int _SmoothnessID;
    private int _BaseColorID;
    private int _DetailNormalMapID;
    private int _DetailNormalScaleID; // 控制细节法线的强度

    void Start()
    {
        InitializeMaterial();
    }

    void InitializeMaterial()
    {
        if (handRenderer == null) handRenderer = GetComponentInChildren<Renderer>();
        if (handRenderer == null) return;

        // 创建运行时材质实例，防止修改原文件
        runtimeMaterial = handRenderer.material;

        // 1. 查找属性 ID (URP Lit 标准属性名)
        // 注意：如果你用的是 Built-in Standard Shader，把 "_BaseColor" 改为 "_Color"
        _BaseColorID = Shader.PropertyToID("_BaseColor");
        _SmoothnessID = Shader.PropertyToID("_Smoothness");

        // 细节贴图相关 ID
        _DetailNormalMapID = Shader.PropertyToID("_DetailNormalMap");
        _DetailNormalScaleID = Shader.PropertyToID("_DetailNormalMapScale"); // 有些Shader叫 _DetailNormalMapScale

        // 2. 备份原始值
        if (runtimeMaterial.HasProperty(_BaseColorID))
            originalColor = runtimeMaterial.GetColor(_BaseColorID);

        if (runtimeMaterial.HasProperty(_SmoothnessID))
            originalSmoothness = runtimeMaterial.GetFloat(_SmoothnessID);

        originalDetailScale = 0f; // 默认一开始没有水滴，强度为0

        // 3. 预先设置好水滴贴图，但把强度设为0 (隐藏)
        if (waterDropletNormal != null)
        {
            runtimeMaterial.EnableKeyword("_DETAIL_MULX2"); // 开启细节贴图宏
            runtimeMaterial.SetTexture(_DetailNormalMapID, waterDropletNormal);
            runtimeMaterial.SetFloat(_DetailNormalScaleID, 0); // 强度为0 = 看不见

            // 设置平铺密度
            runtimeMaterial.SetTextureScale(_DetailNormalMapID, new Vector2(detailTiling, detailTiling));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            isInWater = true;
            StopDrying(); // 停止变干
            ApplyWetness(1.0f); // 瞬间变湿
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            isInWater = false;
            StartDrying(); // 开始变干
        }
    }

    // 设置湿润程度 (0 = 干, 1 = 湿透)
    private void ApplyWetness(float t)
    {
        if (runtimeMaterial == null) return;

        // 1. 颜色变暗 (湿的东西颜色深)
        Color targetColor = originalColor * wetDarkness;
        runtimeMaterial.SetColor(_BaseColorID, Color.Lerp(originalColor, targetColor, t));

        // 2. 变光滑 (高光)
        runtimeMaterial.SetFloat(_SmoothnessID, Mathf.Lerp(originalSmoothness, wetSmoothness, t));

        // 3. 显现水滴 (Detail Normal Scale)
        // 强度从 0 到 1
        runtimeMaterial.SetFloat(_DetailNormalScaleID, Mathf.Lerp(0f, 1.0f, t));
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

        // 保持全湿状态一小会儿
        yield return new WaitForSeconds(0.5f);

        // 慢慢变干
        while (timer < dryDuration)
        {
            timer += Time.deltaTime;
            float t = 1.0f - (timer / dryDuration); // 从 1 变到 0

            ApplyWetness(t);
            yield return null;
        }

        // 彻底变干
        ApplyWetness(0f);
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}