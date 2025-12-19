using UnityEngine;
using System.Collections;

public class WaterBuffSystem : MonoBehaviour
{
    [Header("设置")]
    public string waterTag = "water";
    [Tooltip("离开水面后，护盾还能维持多久")]
    public float buffDuration = 20f;

    [Header("组件引用")]
    public PlayerHealth playerHealth;
    public IceArmorVisuals armorVisuals;

    private Coroutine disableCoroutine;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (armorVisuals == null) armorVisuals = GetComponentInParent<IceArmorVisuals>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            HandleEnterWater();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            if (disableCoroutine != null)
            {
                HandleEnterWater();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            HandleExitWater();
        }
    }

    private void HandleEnterWater()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        // 1. 机制：开启护盾 (机制保留)
        EnableShieldStatus(true);

        // 2. 语音：进入水中的语音 (获得护甲)
        // ⭐ 修改：对照组不播放“进入/获得”的积极语音
        if (ExperimentVisualControl.Instance == null || ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            if (PlayerVoiceSystem.Instance != null)
            {
                PlayerVoiceSystem.Instance.PlayVoiceOnce("Ice_Armor");
            }
        }

        // Debug.Log("🌊 接触水源...");
    }

    private void HandleExitWater()
    {
        if (disableCoroutine == null)
        {
            disableCoroutine = StartCoroutine(DisableShieldCountdown());
        }
    }

    private void EnableShieldStatus(bool isActive)
    {
        if (playerHealth != null) playerHealth.isInvincible = isActive;

        // 视觉特效由 IceArmorVisuals 内部自己判断对照组，这里只管调用
        if (armorVisuals != null)
        {
            if (isActive) armorVisuals.EnableArmor();
            else armorVisuals.DisableArmor();
        }
    }

    private IEnumerator DisableShieldCountdown()
    {
        Debug.Log($"⏳ 离开水源：护盾将在 {buffDuration} 秒后消失...");

        // 3. 语音：离开水流
        // ⭐ 需求：离开水的语音在对照组也要触发 (不做 ExperimentVisualControl 判断)
        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoiceOnce("Leave_Water");
        }

        yield return new WaitForSeconds(buffDuration);

        Debug.Log("🛡️ 冰霜护盾效果结束");
        EnableShieldStatus(false);

        disableCoroutine = null;
    }
}