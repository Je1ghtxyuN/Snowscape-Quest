using UnityEngine;

public class SnowballCollision : MonoBehaviour
{
    [Header("击中特效")]
    public GameObject hitEffect; // 拖入特效预制体

    void OnCollisionEnter(Collision collision)
    {
        EnemyHealth enemyScript = collision.gameObject.GetComponent<EnemyHealth>();
        PlayerHealth playerScript = collision.gameObject.GetComponent<PlayerHealth>();
        //击中任何物体都销毁雪球
        Destroy(gameObject);

        
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后自动清理特效
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("击中敌人！");
            // 敌人受伤接口
            enemyScript.TakeDamage(20f);
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("玩家被击中！");
            playerScript.TakeDamage(20f);

        }
    }
}