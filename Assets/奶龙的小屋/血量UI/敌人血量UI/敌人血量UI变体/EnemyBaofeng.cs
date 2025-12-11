using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBaofeng : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float initialHealth = 100f;
    // 血条UI预制体
    [SerializeField] private GameObject healthBarPrefab;

    [Header("死亡特效")]
    // 粒子预制体
    [SerializeField] private GameObject explosionParticlePrefab;

    [Header("声音特效")]
    [SerializeField] private AudioClip deathSound; // 音效资源
    private AudioSource audioSource; // 音频源组件

    [Header("冰晶掉落")]
    [SerializeField] private GameObject iceCrystalPrefab; // 冰晶预制体
    [SerializeField] private float crystalDropChance = 0.7f; // 掉落概率

    // 死亡状态
    private bool isDead = false;

    // 创建对象
    private Health healthSystem;
    private HealthUI healthUI;

    // ⭐ 新增：用于存储实例化的血条对象，以便销毁
    private GameObject healthBarInstance;

    private Animator animator;

    void Start()
    {
        // 初始化血量系统
        healthSystem = new Health(initialHealth);

        // 订阅死亡事件
        healthSystem.OnDeath += Die;

        animator = GetComponent<Animator>();

        // 实例化血条UI
        if (healthBarPrefab != null)
        {
            // ⭐ 修改：将实例化结果保存到变量 healthBarInstance
            healthBarInstance = Instantiate(
                healthBarPrefab,
                transform.position + new Vector3(0, 2f, 0),
                Quaternion.identity
            );

            healthUI = healthBarInstance.GetComponent<HealthUI>();
            healthUI.Initialize(transform, healthSystem);
        }

        // 添加并获取音频源组件
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D空间音效
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    // 收到伤害的方法
    public void TakeDamage(float amount)
    {
        if (isDead) return; // 已死亡不再响应伤害
        healthSystem.TakeDamage(amount);
    }

    // 死亡处理（由OnDeath事件触发）
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 通知回合管理器敌人死亡
        if (GameRoundManager.Instance != null)
        {
            GameRoundManager.Instance.OnEnemyKilled();
        }

        // 掉落逻辑
        if (Random.value <= crystalDropChance && iceCrystalPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.8f;
            Instantiate(iceCrystalPrefab, spawnPos, Quaternion.identity);

            if (PetVoiceSystem.Instance != null)
            {
                PetVoiceSystem.Instance.TryPlayFirstDropVoice();
            }
        }

        healthSystem.OnDeath -= Die;
        StartCoroutine(DeathRoutine());
    }

    // 死亡协程（处理动画和销毁）
    private IEnumerator DeathRoutine()
    {
        // 获取 AdvancedEnemyAI 并禁用它
        AdvancedEnemyAI enemyAI = GetComponent<AdvancedEnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.MarkAsDead();
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // 触发死亡动画
        if (animator != null)
        {
            animator.SetTrigger("die");
            yield return null;
            yield return new WaitForSeconds(1.6f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 粒子爆炸
        if (explosionParticlePrefab != null)
        {
            GameObject explosion = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 0.5f);
        }

        // 播放音效
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
            yield return new WaitForSeconds(deathSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 销毁物体 (OnDestroy 会自动被调用)
        Destroy(gameObject);
    }

    // ⭐ 核心修改：当怪物被销毁时（无论是被打死还是被系统清除），顺手把血条也销毁
    void OnDestroy()
    {
        // 安全取消事件订阅
        if (healthSystem != null)
            healthSystem.OnDeath -= Die;

        // 如果血条实例还在，就销毁它
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }
}