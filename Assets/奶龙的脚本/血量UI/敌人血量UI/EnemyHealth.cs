using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float initialHealth = 100f;
    //血条UI预制体
    [SerializeField] private GameObject healthBarPrefab;


    //死亡状态
    private bool isDead = false;

    //创建对象
    private Health healthSystem;
    private HealthUI healthUI;

    void Start()
    {
        //初始化血量系统
        healthSystem = new Health(initialHealth);

        //订阅死亡事件
        healthSystem.OnDeath += Die;

        //实例化血条UI
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

        //测试用：3秒后开始受到伤害
        //InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    //受伤测试
    //private void ApplyTestDamage()
    //{
    //    TakeDamage(10f);
    //    UnityEngine.Debug.Log("Enemy测试受到伤害");
    //}

    //收到伤害的方法
    public void TakeDamage(float amount)
    {
        if (isDead) return; //已死亡不再响应伤害
        healthSystem.TakeDamage(amount);
        //可添加伤害特效、音效等
    }

    //死亡处理（由OnDeath事件触发）
    private void Die()
    {
        if (isDead) return; //避免重复触发
        isDead = true;

        //取消事件订阅防止重复调用
        healthSystem.OnDeath -= Die;

        //死亡逻辑
        StartCoroutine(DeathRoutine());
    }

    //死亡协程（处理动画和销毁）
    private IEnumerator DeathRoutine()
    {
        //播放死亡动画（伪代码）
        //animator.SetTrigger("Die");
        //yield return new WaitForSeconds(1.0f); //等待动画完成

        //销毁血条UI
        if (healthUI != null) Destroy(healthUI.gameObject);

        //销毁本体
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 安全取消事件订阅
        if (healthSystem != null)
            healthSystem.OnDeath -= Die;
    }
}