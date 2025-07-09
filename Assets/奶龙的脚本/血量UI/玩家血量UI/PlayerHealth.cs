using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject fixedHealthBarPrefab;
    private Health healthSystem;
    private FixedHealthUI healthUI;

    void Start()
    {
        // 初始化血量系统 (100点生命值)
        healthSystem = new Health(100f);

        // 创建固定血条UI
        if (fixedHealthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(fixedHealthBarPrefab);
            healthUI = healthBarObj.GetComponent<FixedHealthUI>();
            healthUI.Initialize(healthSystem);
        }

        //测试用：3秒后开始受到伤害
        InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    //受伤测试
    private void ApplyTestDamage()
    {
        TakeDamage(10f);
        UnityEngine.Debug.Log("Player测试受到伤害");
    }

    //收到伤害的方法
    public void TakeDamage(float amount)
    {
        healthSystem.TakeDamage(amount);
        //可添加伤害特效、音效等
    }
}