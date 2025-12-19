using UnityEngine;
using System.Collections;

public class IglooBuffSystem : MonoBehaviour
{
    [Header("特效配置")]
    public GameObject icePillarPrefab;

    [Header("音效配置")]
    public AudioClip activationSound;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("游戏逻辑")]
    public float buffDuration = 30f;
    public float cooldownDuration = 60f;

    [Header("语音 ID 配置")]
    public string playerVoiceID = "Igloo_Effect";

    private Coroutine buffCoroutine;
    private PlayerHealth playerHealth;
    private IceArmorVisuals armorVisuals;
    private float lastActivationTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time < lastActivationTime + cooldownDuration) return;

            if (playerHealth == null) playerHealth = other.GetComponent<PlayerHealth>();
            if (armorVisuals == null) armorVisuals = other.GetComponentInChildren<IceArmorVisuals>();

            ActivateIglooBuff(other.transform);
        }
    }

    private void ActivateIglooBuff(Transform playerTransform)
    {
        lastActivationTime = Time.time;

        // ⭐ 修改：只有在实验组才生成光柱特效
        if (ExperimentVisualControl.Instance == null || ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            if (icePillarPrefab != null)
            {
                Instantiate(icePillarPrefab, playerTransform.position, Quaternion.identity);
            }
            // 开启冰霜护甲视觉
            if (armorVisuals != null)
            {
                armorVisuals.EnableArmor();
            }
        }

        // --- 音效和语音保持播放（如果对照组也要静音，也可以包进 if 里） ---
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position, soundVolume);
        }

        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoice(playerVoiceID);
        }

        // --- 逻辑：无敌Buff (始终生效) ---
        if (buffCoroutine != null) StopCoroutine(buffCoroutine);
        buffCoroutine = StartCoroutine(BuffDurationRoutine());
    }

    private IEnumerator BuffDurationRoutine()
    {
        Debug.Log($"❄️ 冰屋庇护激活！无敌 {buffDuration} 秒");

        if (playerHealth != null) playerHealth.isInvincible = true;

        yield return new WaitForSeconds(buffDuration);

        if (playerHealth != null) playerHealth.isInvincible = false;

        // 关闭护甲
        if (armorVisuals != null) armorVisuals.DisableArmor();

        buffCoroutine = null;
    }
}