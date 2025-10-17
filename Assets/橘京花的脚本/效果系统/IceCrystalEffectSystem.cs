using UnityEngine;
using System.Collections;

public class IceCrystalEffectSystem : MonoBehaviour
{
    [Header("冰晶效果设置")]
    [SerializeField] private Material iceCrystalMaterial; // 冰晶覆盖材质
    [SerializeField] private ParticleSystem iceCrystalParticles; // 冰晶粒子效果
    [SerializeField] private AudioClip collectSound; // 收集音效
    [SerializeField] private float healAmount = 20f; // 每次收集恢复血量
    [SerializeField] private float effectDuration = 5f; // 效果持续时间

    private PlayerHealth playerHealth;
    private Renderer playerRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;
    private bool isEffectActive = false;
    private int crystalCount = 0;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRenderer = GetComponentInChildren<Renderer>(); // 获取角色渲染器
        audioSource = GetComponent<AudioSource>();

        if (playerRenderer != null)
            originalMaterial = playerRenderer.material;
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
        if (isEffectActive) yield break;

        isEffectActive = true;

        // 应用冰晶材质
        if (playerRenderer != null && iceCrystalMaterial != null)
        {
            playerRenderer.material = iceCrystalMaterial;
        }

        // 等待效果持续时间
        yield return new WaitForSeconds(effectDuration);

        // 恢复原始材质
        if (playerRenderer != null && originalMaterial != null)
        {
            playerRenderer.material = originalMaterial;
        }

        isEffectActive = false;
    }

    public int GetCrystalCount() => crystalCount;
}