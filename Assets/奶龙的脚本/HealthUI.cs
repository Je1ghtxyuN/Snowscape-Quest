using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;

public class HealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UnityEngine.UI.Image healthFillImage; // 血条填充图
    [SerializeField] private GameObject healthBarObject; // 整个血条对象

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // 血条在头顶的位置偏移
    [SerializeField] private bool alwaysVisible = false; // 是否一直显示

    private Transform targetTransform; // 跟随的目标(敌人或玩家)
    private Health healthSystem; // 关联的生命值系统

    public void Initialize(Transform target, Health health)
    {
        targetTransform = target;
        healthSystem = health;

        // 注册血量变化事件
        healthSystem.OnHealthChanged += UpdateHealthBar;
        healthSystem.OnDeath += HideHealthBar;

        UpdateHealthBar();
    }

    private void Update()
    {
        // 更新血条位置，使其跟随目标
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;

            // 使血条始终朝向摄像机
            transform.rotation = Quaternion.LookRotation(
                Camera.main.transform.forward,
                Camera.main.transform.up
            );
        }
    }

    private void UpdateHealthBar()
    {
        // 更新血条填充比例
        healthFillImage.fillAmount = healthSystem.GetHealthPercentage();

        // 仅在受到伤害时显示血条（可选）
        if (!alwaysVisible)
        {
            StartCoroutine(ShowHealthBarTemporarily(1.5f));
        }
    }

    private IEnumerator ShowHealthBarTemporarily(float duration)
    {
        healthBarObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        healthBarObject.SetActive(false);
    }

    private void HideHealthBar()
    {
        healthBarObject.SetActive(false);
        // 可添加死亡动画效果
    }

    void OnDestroy()
    {
        // 注销事件监听
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
            healthSystem.OnDeath -= HideHealthBar;
        }
    }
}