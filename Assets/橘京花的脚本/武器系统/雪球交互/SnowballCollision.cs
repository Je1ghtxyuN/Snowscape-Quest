using UnityEngine;

public class SnowballCollision : MonoBehaviour
{
    [Header("击中特效")]
    public GameObject hitEffect;

    // 基础伤害
    private float baseDamage = 20f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);

        if (hitEffect != null && collision.contacts.Length > 0)
        {
            GameObject effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 计算最终伤害：基础伤害 * 全局倍率
        float multiplier = PlayerUpgradeHandler.Instance != null ? PlayerUpgradeHandler.Instance.damageMultiplier : 1f;
        float finalDamage = baseDamage * multiplier;

        // 4. 处理敌人碰撞
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Debug.Log($"击中敌人！造成 {finalDamage} 点伤害"); 
            EnemyHealth enemyScript = collision.gameObject.GetComponent<EnemyHealth>();
            EnemyBaofeng enemyBaofengScript = collision.gameObject.GetComponent<EnemyBaofeng>();

            if (enemyScript != null)
            {
                enemyScript.TakeDamage(finalDamage);
            }
            else if (enemyBaofengScript != null)
            {
                enemyBaofengScript.TakeDamage(finalDamage);
            }
        }
        // 5. 处理玩家碰撞 (玩家被砸一般不算伤害倍率，或者是敌人砸的)
        else if (collision.gameObject.CompareTag("Player"))
        {
            // 保持原样或根据需求修改
            PlayerHealth playerScript = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(20f);
            }
        }
    }
}