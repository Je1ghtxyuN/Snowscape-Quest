using System.Collections.Generic;
using UnityEngine;

public class IceSword : MonoBehaviour
{
    [Header("武器参数")]
    public float damage = 40f; // 剑的基础伤害（比雪球高）

    // 防止一帧内多次触发伤害
    private List<GameObject> hitEnemies = new List<GameObject>();
    private float resetHitTimer = 0.5f;

    void OnEnable()
    {
        hitEnemies.Clear();
    }

    void Update()
    {
        // 定时清空已攻击列表，允许再次攻击同一敌人
        if (hitEnemies.Count > 0)
        {
            resetHitTimer -= Time.deltaTime;
            if (resetHitTimer <= 0)
            {
                hitEnemies.Clear();
                resetHitTimer = 0.5f;
            }
        }
    }

    // 必须确保剑模型上有 Collider，且 IsTrigger = true
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 防止短时间内重复伤害同一个敌人
            if (hitEnemies.Contains(other.gameObject)) return;

            // 获取升级后的伤害倍率
            float multiplier = PlayerUpgradeHandler.Instance != null ? PlayerUpgradeHandler.Instance.damageMultiplier : 1f;
            float finalDamage = damage * multiplier;

            // 造成伤害
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            EnemyBaofeng enemyBaofeng = other.GetComponent<EnemyBaofeng>();

            bool hitSuccess = false;

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
                hitSuccess = true;
            }
            else if (enemyBaofeng != null)
            {
                enemyBaofeng.TakeDamage(finalDamage);
                hitSuccess = true;
            }

            if (hitSuccess)
            {
                Debug.Log($"寒冰剑击中敌人！造成 {finalDamage} 点伤害");
                hitEnemies.Add(other.gameObject);
                // 这里可以播放砍击音效
            }
        }
    }
}