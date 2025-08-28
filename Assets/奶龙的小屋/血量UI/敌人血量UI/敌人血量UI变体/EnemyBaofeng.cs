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

    // 死亡状态
    private bool isDead = false;

    // 创建对象
    private Health healthSystem;
    private HealthUI healthUI;

    private Animator animator;

    // Animator animator = GameObject.GetComponent<Animator>;

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

        // 测试用：3秒后开始受到伤害
        InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    // 受伤测试
    private void ApplyTestDamage()
    {
        //TakeDamage(50f);
        //UnityEngine.Debug.Log("Enemy测试受到伤害");
    }

    // 收到伤害的方法
    public void TakeDamage(float amount)
    {
        if (isDead) return; // 已死亡不再响应伤害
        healthSystem.TakeDamage(amount);
        // 可添加伤害特效、音效等
    }

    // 死亡处理（由OnDeath事件触发）
    private void Die()
    {
        if (isDead) return; // 避免重复触发
        isDead = true;

        // 取消事件订阅防止重复调用
        healthSystem.OnDeath -= Die;

        // 死亡逻辑
        StartCoroutine(DeathRoutine());
    }

    // 死亡协程（处理动画和销毁）
    private IEnumerator DeathRoutine()
    {
        // 标记敌人死亡（禁用AI行为）
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.MarkAsDead();
        }

        // 触发死亡动画
        if (animator != null)
        {
            animator.SetTrigger("die");
            yield return null; // 确保动画触发器生效

            // 直接使用固定1.6秒等待
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
                transform.position, // 敌人中心点
                Quaternion.identity
            );
            Destroy(explosion, 0.5f); // 粒子播放后自动销毁
        }

        // 播放音效
        if (deathSound != null && audioSource != null)
        {
            // 在敌人位置播放音效
            audioSource.PlayOneShot(deathSound);
            // 等待音效播放完毕（PlayOneShot不会阻塞，所以用等待时间）
            yield return new WaitForSeconds(deathSound.length);
        }
        else
        {
            // 如果没有音效，使用保底延迟
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
