using UnityEngine;

public class IceCrystalEffectSystem : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioSource audioSource;

    [Header("视觉表现引用")]
    [Tooltip("请把挂载了 RPMIceEffect 脚本的物体拖到这里 (或者直接拖手部模型)")]
    public RPMIceEffect iceVisualEffect;

    [SerializeField] private ParticleSystem iceCrystalParticles;

    [Header("游戏逻辑参数")]
    [SerializeField] private AudioClip collectSound;

    [Tooltip("收集音效的音量大小 (默认1，想大声就填 2 或 3)")]
    [Range(0f, 3f)]
    public float collectVolume = 1.5f; // ⭐ 新增：默认设为1.5倍音量

    [SerializeField] private float healAmount = 20f;

    // 内部变量
    private int crystalCount = 0;
    private bool hasPlayedFirstAbsorb = false;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (iceVisualEffect == null) iceVisualEffect = GetComponentInChildren<RPMIceEffect>();
    }

    public void CollectIceCrystal()
    {
        crystalCount++;

        // 1. 播放声音 (加入了音量参数)
        if (collectSound != null && audioSource != null)
        {
            // ⭐ 这里的 collectVolume 就是你设置的音量倍数
            audioSource.PlayOneShot(collectSound, collectVolume);
        }

        // 2. 播放粒子特效
        if (iceCrystalParticles != null)
        {
            iceCrystalParticles.Play();
        }

        // 3. 玩家回血
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // 4. 触发手部结冰视觉效果
        if (iceVisualEffect != null)
        {
            iceVisualEffect.ActivateIceEffect();
        }

        // 5. 触发语音
        if (!hasPlayedFirstAbsorb && PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Tutorial_Absorb", 1.0f);
            hasPlayedFirstAbsorb = true;
        }

        // 6. 触发烧伤恢复进度
        if (BurnRecoverySystem.Instance != null)
        {
            BurnRecoverySystem.Instance.AddRecoveryProgress();
        }
    }

    public int GetCrystalCount() => crystalCount;
}