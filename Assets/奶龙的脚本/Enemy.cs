using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float initialHealth = 100f;
    //血条UI预制体
    [SerializeField] private GameObject healthBarPrefab;

    private Health healthSystem;
    private HealthUI healthUI;

    void Start()
    {
        //初始化血量系统
        healthSystem = new Health(initialHealth);

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
        InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    //收到伤害的方法
    public void TakeDamage(float amount)
    {
        healthSystem.TakeDamage(amount);
        //可添加伤害特效、音效等
    }

    //死亡
    private void Die()
    {
        //死亡处理
        //1.播放死亡动画
        //2.移除碰撞体
        //3.掉落物品/经验值
        //4.销毁对象
        Destroy(gameObject, 1.5f);
    }
}