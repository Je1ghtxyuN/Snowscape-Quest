using UnityEngine;

public class SnowballCollision : MonoBehaviour
{
    [Header("击中特效")]
    public GameObject hitEffect; // 拖入特效预制体

    void OnCollisionEnter(Collision collision)
    {
        // 1. 首先检查碰撞是否有效
        if (collision == null || collision.gameObject == null)
        {
            Destroy(gameObject);
            return;
        }

        // 2. 击中任何物体都销毁雪球
        Destroy(gameObject);

        // 3. 生成击中特效（如果有特效且碰撞点有效）
        if (hitEffect != null && collision.contacts.Length > 0)
        {
            GameObject effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后自动清理特效
        }

        // 4. 处理敌人碰撞
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("击中敌人！");
            EnemyHealth enemyScript = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(20f);
            }
            else
            {
                Debug.LogWarning($"敌人对象 {collision.gameObject.name} 上没有 EnemyHealth 组件");
            }
        }
        // 5. 处理玩家碰撞
        else if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("玩家被击中！");
            PlayerHealth playerScript = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(20f);
            }
            else
            {
                Debug.LogWarning($"玩家对象 {collision.gameObject.name} 上没有 PlayerHealth 组件");
            }
        }
    }
}