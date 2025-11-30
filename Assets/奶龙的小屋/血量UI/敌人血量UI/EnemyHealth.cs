using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
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
        if (isDead) return;
        isDead = true;

        healthSystem.OnDeath -= Die;

        // ⭐ 修改：移除分数调用，改为通知管理器
        if (GameRoundManager.Instance != null)
        {
            //GameRoundManager.Instance.OnEnemyKilled();//普通雪人不计入
        }

        // 掉落逻辑修改：只负责生成，不负责控制移动
        if (Random.value <= crystalDropChance && iceCrystalPrefab != null)
        {
            // 生成位置：在怪物的中心偏上一点
            Vector3 spawnPos = transform.position + Vector3.up * 0.8f;
            Instantiate(iceCrystalPrefab, spawnPos, Quaternion.identity);
        }

        StartCoroutine(DeathRoutine());
    }


    // 死亡协程（处理动画和销毁）
    private IEnumerator DeathRoutine()
    {
        // 1. 禁用碰撞体（显式判空）
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 2. 触发所有视觉效果（动画/音效/粒子）
        if (animator != null)
            animator.SetTrigger("die");

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        GameObject explosion = null;
        if (explosionParticlePrefab != null)
        {
            explosion = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 2f); // 延长粒子生命周期
        }

        // 3. 立即销毁主物体(保留视觉效果继续播放）
        if (healthUI != null)
            Destroy(healthUI.gameObject);

        // 隐藏物体但保留组件运行
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.enabled = false;
        }
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        // 4. 仅等待动画时长（不等待音效/粒子）
        if (animator != null)
        {
            // 强制动画立即更新
            animator.Update(0f);

            yield return new WaitUntil(() =>
                animator.GetCurrentAnimatorStateInfo(0).IsName("DeathState")
            );

            // 精确等待动画长度
            yield return new WaitForSeconds(
                animator.GetCurrentAnimatorStateInfo(0).length
            );
        }
        else
        {
            yield return new WaitForSeconds(0.3f); // 最小等待时间
        }

        // 5. 最终销毁
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 安全取消事件订阅
        if (healthSystem != null)
            healthSystem.OnDeath -= Die;
    }
}