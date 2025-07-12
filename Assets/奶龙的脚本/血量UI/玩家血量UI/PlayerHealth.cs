using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject fixedHealthBarPrefab;
    private Health healthSystem;
    private FixedHealthUI healthUI;
    private Camera vrCamera;

    void Start()
    {
        // 初始化血量系统 (100点生命值)
        healthSystem = new Health(100f);

        // 获取VR主摄像机
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            UnityEngine.Debug.LogError("Main Camera not found");
            return;
        }

        // 实例化并配置血条
        CreateHealthBar();

        //测试用：3秒后开始受到伤害
        //InvokeRepeating("ApplyTestDamage", 3f, 1f);
    }

    private void CreateHealthBar()
    {
        // 将血条实例化为VR摄像机的子物体
        GameObject healthBarObj = Instantiate(
            fixedHealthBarPrefab,
            vrCamera.transform.position,
            vrCamera.transform.rotation,
            vrCamera.transform // 设置父物体为VR摄像机
        );

        // 调整血条位置（摄像机前方1米，下方0.2米）
        healthBarObj.transform.localPosition = new Vector3(0, -0.2f, 1.5f);
        healthBarObj.transform.localRotation = Quaternion.identity;

        // 调整Canvas尺寸（VR中World Space的UI需要小尺寸）
        healthBarObj.transform.localScale = Vector3.one * 0.002f;

        healthUI = healthBarObj.GetComponent<FixedHealthUI>();
        healthUI.Initialize(healthSystem);
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