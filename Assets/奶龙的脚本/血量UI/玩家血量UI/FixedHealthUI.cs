using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class FixedHealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UnityEngine.UI.Image healthFillImage; //血条填充图
    [SerializeField] private UnityEngine.UI.Image healthBackground; //血条背景

    [Header("Color Settings")]
    [SerializeField] private Color fullHealthColor = Color.red; //满血颜色
    [SerializeField] private Color lowHealthColor = Color.blue; //低血颜色
    [SerializeField] private float criticalThreshold = 0.2f; //颜色渐变临界点

    private Health healthSystem; //关联的生命值系统

    public void Initialize(Health health)
    {
        healthSystem = health;

        //注册血量变化事件
        healthSystem.OnHealthChanged += UpdateHealthBar;
        healthSystem.OnDeath += HandleDeath;

        //初始更新
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSystem == null || healthFillImage == null) return;

        //更新血条填充比例
        float healthPercent = healthSystem.GetHealthPercentage();
        healthFillImage.fillAmount = healthPercent;

        //根据血量百分比更新颜色
        UpdateHealthColor(healthPercent);
    }

    private void UpdateHealthColor(float healthPercent)
    {
        //线性插值计算颜色（蓝到红渐变）
        if (healthPercent > criticalThreshold)
        {
            // 直接使用比例：高血量→接近1（红色），低血量→接近0（蓝色）
            float lerpValue = healthPercent;
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, lerpValue);
        }
        else
        {
            // 临界值以下保持蓝色
            healthFillImage.color = lowHealthColor;
        }
    }

    private void HandleDeath()
    {
        //死亡时隐藏血条或显示特殊效果
        healthFillImage.fillAmount = 0;
        healthFillImage.color = Color.gray;

        //可添加死亡动画效果
    }

    void OnDestroy()
    {
        //注销事件监听
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
            healthSystem.OnDeath -= HandleDeath;
        }
    }
}