using UnityEngine;
using System.Collections;

public class WaterBuffSystem : MonoBehaviour
{
    [Header("设置")]
    public string waterTag = "water"; // 注意大小写，你的截图里Tag是小写water
    [Tooltip("离开水面后，护盾还能维持多久")]
    public float buffDuration = 20f;

    [Header("组件引用")]
    public PlayerHealth playerHealth;
    public IceArmorVisuals armorVisuals;

    // 用来存储“正在进行的倒计时”，以便随时打断它
    private Coroutine disableCoroutine;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (armorVisuals == null) armorVisuals = GetComponentInParent<IceArmorVisuals>();
    }

    // 1. 进入或者待在水里时：开启护盾
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

    // 2. 离开水面时：开始消失倒计时
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            HandleExitWater();
        }
    }

    private void HandleEnterWater()
    {
        // 如果之前正在准备取消护盾，立刻“撤回”这个命令
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        // 确保护盾是开启状态
        EnableShieldStatus(true);

        // ⭐ 改动：只播放一次“获得护甲”的语音
        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoiceOnce("Ice_Armor");
        }

        // Debug.Log("🌊 接触水源：护盾持续保持中...");
    }

    private void HandleExitWater()
    {
        // 只有当前没有在倒计时的时候，才开启一个新的倒计时
        if (disableCoroutine == null)
        {
            disableCoroutine = StartCoroutine(DisableShieldCountdown());
        }
    }

    // 开启或关闭护盾的具体逻辑封装
    private void EnableShieldStatus(bool isActive)
    {
        if (playerHealth != null) playerHealth.isInvincible = isActive;

        if (armorVisuals != null)
        {
            if (isActive) armorVisuals.EnableArmor();
            else armorVisuals.DisableArmor();
        }
    }

    private IEnumerator DisableShieldCountdown()
    {
        Debug.Log($"⏳ 离开水源：护盾将在 {buffDuration} 秒后消失...");

        // ⭐ 改动：只播放一次“离开水流”的语音
        if (PlayerVoiceSystem.Instance != null)
        {
            PlayerVoiceSystem.Instance.PlayVoiceOnce("Leave_Water");
        }

        // 等待倒计时
        yield return new WaitForSeconds(buffDuration);

        // 时间到，关闭护盾
        Debug.Log("🛡️ 冰霜护盾效果结束");
        EnableShieldStatus(false);

        disableCoroutine = null;
    }
}