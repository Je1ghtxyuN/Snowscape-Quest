using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
            GameObject healthBarObj = Instantiate(
                healthBarPrefab,
                transform.position + new Vector3(0, 2f, 0),
                Quaternion.identity
            );

            healthUI = healthBarObj.GetComponent<HealthUI>();
            healthUI.Initialize(transform, healthSystem);
        }

        // 添加并获取音频源组件
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D空间音效
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 测试用：3秒后开始受到伤害 (如果你不需要测试了可以注释掉这一行)
        // InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    // 受伤测试
    private void ApplyTestDamage()
    {
        // TakeDamage(50f);
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

        // ⭐ 修改：通知回合管理器敌人死亡
        if (GameRoundManager.Instance != null)
        {
            GameRoundManager.Instance.OnEnemyKilled();
        }

        // 掉落逻辑修改
        if (Random.value <= crystalDropChance && iceCrystalPrefab != null)
        {
            // 精英怪可能比较高大，生成位置再高一点，防止卡在身体里
            Vector3 spawnPos = transform.position + Vector3.up * 0.8f;
            Instantiate(iceCrystalPrefab, spawnPos, Quaternion.identity);
        }

        healthSystem.OnDeath -= Die;
        StartCoroutine(DeathRoutine());
    }


    // 死亡协程（处理动画和销毁）
    private IEnumerator DeathRoutine()
    {
        // ⭐ 核心修改：获取 AdvancedEnemyAI 并禁用它
        AdvancedEnemyAI enemyAI = GetComponent<AdvancedEnemyAI>();
        if (enemyAI != null)
        {
            // 调用 AdvancedEnemyAI 中的 MarkAsDead 方法
            // 这会停止它的移动、攻击和巡逻逻辑
            enemyAI.MarkAsDead();

            // 额外保险：禁用刚体物理模拟，防止尸体被推动
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true; // 设为运动学，不再受物理影响
            }

            // 额外保险：禁用碰撞体，防止尸体挡路
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // 触发死亡动画
        if (animator != null)
        {
            // 这里的 "die" 必须和 Animator 面板里的 Trigger 名字一致
            // 也可以使用 enemyAI.dieAnimParam 来保持同步，但直接写字符串更稳妥
            animator.SetTrigger("die");
            yield return null; // 确保动画触发器生效

            // 等待动画播放时间
            yield return new WaitForSeconds(1.6f);
        }
        else
        {
            // 没有动画组件时使用保底等待
            yield return new WaitForSeconds(0.5f);
        }

        // 在敌人位置生成粒子爆炸
        if (explosionParticlePrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionParticlePrefab,
                transform.position,
                Quaternion.identity
            );
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

        // 销毁逻辑
        if (healthUI != null) Destroy(healthUI.gameObject);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 安全取消事件订阅
        if (healthSystem != null)
            healthSystem.OnDeath -= Die;
    }
}