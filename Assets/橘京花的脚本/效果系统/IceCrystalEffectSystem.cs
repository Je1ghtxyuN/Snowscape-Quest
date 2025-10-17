using UnityEngine;
using System.Collections;

public class IceCrystalEffectSystem : MonoBehaviour
{
    [Header("冰晶效果设置")]
    [SerializeField] private Texture2D iceCrystalNormalMap; // 冰晶法线贴图
    [SerializeField] private ParticleSystem iceCrystalParticles; // 冰晶粒子效果
    [SerializeField] private AudioClip collectSound; // 收集音效
    [SerializeField] private float healAmount = 20f; // 每次收集恢复血量
    [SerializeField] private float effectDuration = 5f; // 效果持续时间
    [SerializeField] private string skinPartName = "Skin"; // 皮肤部分子物体的名称

    private PlayerHealth playerHealth;
    private Renderer skinRenderer; // 皮肤部分的渲染器
    private Material originalSkinMaterial; // 皮肤原始材质
    private Material iceCrystalMaterial; // 冰晶材质实例
    private AudioSource audioSource;
    private bool isEffectActive = false;
    private int crystalCount = 0;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        audioSource = GetComponent<AudioSource>();

        // 查找皮肤部分的子物体
        FindSkinRenderer();

        if (skinRenderer != null)
        {
            originalSkinMaterial = skinRenderer.material;
            // 创建冰晶材质实例
            CreateIceCrystalMaterial();
        }
        else
        {
            Debug.LogWarning("未找到皮肤部分的渲染器，请检查子物体名称设置: " + skinPartName);
        }
    }

    // 查找皮肤部分的渲染器
    private void FindSkinRenderer()
    {
        Transform skinTransform = transform.Find(skinPartName);
        if (skinTransform == null)
        {
            // 如果直接查找失败，尝试递归查找
            skinTransform = FindDeepChild(transform, skinPartName);
        }

        if (skinTransform != null)
        {
            skinRenderer = skinTransform.GetComponent<Renderer>();
        }
    }

    // 递归查找子物体
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    // 创建冰晶材质
    private void CreateIceCrystalMaterial()
    {
        if (originalSkinMaterial != null)
        {
            // 创建材质实例，避免修改原始材质
            iceCrystalMaterial = new Material(originalSkinMaterial);

            // 应用冰晶法线贴图
            if (iceCrystalNormalMap != null)
            {
                iceCrystalMaterial.SetTexture("_BumpMap", iceCrystalNormalMap);
                // 确保法线贴图强度适中
                iceCrystalMaterial.SetFloat("_BumpScale", 1.0f);
            }
        }
    }

    // 收集冰晶的方法
    public void CollectIceCrystal()
    {
        crystalCount++;

        // 播放收集音效
        if (collectSound != null && audioSource != null)
            audioSource.PlayOneShot(collectSound);

        // 恢复血量
        if (playerHealth != null)
            playerHealth.Heal(healAmount);

        // 触发冰晶效果
        StartCoroutine(ActivateIceCrystalEffect());

        // 显示冰晶粒子效果
        if (iceCrystalParticles != null)
        {
            iceCrystalParticles.Play();
        }
    }

    private IEnumerator ActivateIceCrystalEffect()
    {
        if (isEffectActive || skinRenderer == null) yield break;

        isEffectActive = true;

        // 应用冰晶材质到皮肤部分
        if (iceCrystalMaterial != null)
        {
            skinRenderer.material = iceCrystalMaterial;
        }

        // 等待效果持续时间
        yield return new WaitForSeconds(effectDuration);

        // 恢复原始材质
        if (originalSkinMaterial != null)
        {
            skinRenderer.material = originalSkinMaterial;
        }

        isEffectActive = false;
    }

    public int GetCrystalCount() => crystalCount;

    // 清理资源
    void OnDestroy()
    {
        if (iceCrystalMaterial != null)
        {
            Destroy(iceCrystalMaterial);
        }
    }
}