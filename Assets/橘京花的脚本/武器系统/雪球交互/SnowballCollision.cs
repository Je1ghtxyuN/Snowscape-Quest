using UnityEngine;

public class SnowballCollision : MonoBehaviour
{
    [Header("击中特效")]
    public GameObject hitEffect;

    [Header("击中音效")] // ⭐ 新增
    public AudioClip impactSound; // ⭐ 拖入雪球碎裂的声音
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    // 基础伤害
    private float baseDamage = 20f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            PlaySoundAndDestroy(); // ⭐ 改用封装方法
            return;
        }

        // 1. 生成特效
        if (hitEffect != null && collision.contacts.Length > 0)
        {
            GameObject effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 2. 计算伤害
        float multiplier = PlayerUpgradeHandler.Instance != null ? PlayerUpgradeHandler.Instance.damageMultiplier : 1f;
        float finalDamage = baseDamage * multiplier;

        // 3. 处理敌人碰撞
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyScript = collision.gameObject.GetComponent<EnemyHealth>();
            EnemyBaofeng enemyBaofengScript = collision.gameObject.GetComponent<EnemyBaofeng>();

            if (enemyScript != null) enemyScript.TakeDamage(finalDamage);
            else if (enemyBaofengScript != null) enemyBaofengScript.TakeDamage(finalDamage);
        }
        // 4. 处理玩家碰撞
        else if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerScript = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerScript != null)
            {
                // 玩家被砸固定扣20血，或者你可以改成 enemyDamage
                playerScript.TakeDamage(20f);
            }
        }

        // 5. 播放声音并销毁
        PlaySoundAndDestroy();
    }

    void PlaySoundAndDestroy()
    {
        // ⭐ 核心：在销毁前，在原地播放一个声音
        if (impactSound != null)
        {
            // PlayClipAtPoint 会自动在位置处生成一个临时 AudioSource 并在播放完后自动销毁
            AudioSource.PlayClipAtPoint(impactSound, transform.position, soundVolume);
        }

        Destroy(gameObject);
    }
}