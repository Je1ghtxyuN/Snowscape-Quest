using UnityEngine;
using System.Collections;

public class WaterBuffSystem : MonoBehaviour
{
    [Header("设置")]
    public string waterTag = "Water"; // 记得给水体设置 Tag
    public float buffDuration = 20f;

    [Header("组件引用")]
    public PlayerHealth playerHealth;
    public IceArmorVisuals armorVisuals;

    private Coroutine buffCoroutine;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (armorVisuals == null) armorVisuals = GetComponentInParent<IceArmorVisuals>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检测是否碰到水
        if (other.CompareTag(waterTag))
        {
            ActivateShield();
        }
    }

    public void ActivateShield()
    {
        // 如果正在进行，重置时间
        if (buffCoroutine != null) StopCoroutine(buffCoroutine);

        buffCoroutine = StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        Debug.Log("🛡️ 获得冰霜护盾！无敌 20秒！");

        // 1. 开启无敌
        if (playerHealth != null) playerHealth.isInvincible = true;

        // 2. 开启视觉特效 (生成蓝色大手)
        if (armorVisuals != null) armorVisuals.EnableArmor();

        // 3. 倒计时
        yield return new WaitForSeconds(buffDuration);

        // 4. 结束
        Debug.Log("🛡️ 冰霜护盾消失...");
        if (playerHealth != null) playerHealth.isInvincible = false;
        if (armorVisuals != null) armorVisuals.DisableArmor();

        buffCoroutine = null;
    }
}