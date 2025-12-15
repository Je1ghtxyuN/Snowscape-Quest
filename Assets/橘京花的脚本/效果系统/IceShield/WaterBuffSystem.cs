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

    // 用来存储“正在进行的倒计时”，以便随时打断它
    private Coroutine disableCoroutine;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (armorVisuals == null) armorVisuals = GetComponentInParent<IceArmorVisuals>();
    }

    // 1. 进入或者待在水里时：开启护盾，并打断消失倒计时
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            HandleEnterWater();
        }
    }

    // (可选) 为了防止Enter漏检测，Stay也可以加一份保障
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            // 如果倒计时正在跑（说明刚才可能误判退出了），立刻取消倒计时重置状态
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

        Debug.Log("🌊 接触水源：护盾持续保持中...");
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

        // 等待倒计时
        yield return new WaitForSeconds(buffDuration);

        // 时间到，关闭护盾
        Debug.Log("🛡️ 冰霜护盾效果结束");
        EnableShieldStatus(false);

        // 清空协程引用
        disableCoroutine = null;
    }
}