using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject fixedHealthBarPrefab;
    [SerializeField] private GameObject deathUIPrefab;
    [SerializeField] private float deathTimeScale = 0.3f;
    [SerializeField] private GameObject leftHandController;

    private Health healthSystem;
    private FixedHealthUI healthUI;
    private Camera vrCamera;
    private GameObject deathUIInstance;
    private bool isDead = false;

    // 特效管理器引用
    private VRLensEffectManager lensEffectManager;

    void Start()
    {
        healthSystem = new Health(100f);
        healthSystem.OnDeath += Die;
        // 注册血量变化
        healthSystem.OnHealthChanged += OnHealthChanged;

        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Debug.LogError("Main Camera not found");
            return;
        }

        // ⭐ 自动查找并报错提示
        lensEffectManager = FindObjectOfType<VRLensEffectManager>();
        if (lensEffectManager == null)
        {
            Debug.LogWarning("⚠️ PlayerHealth: 场景中没找到 VRLensEffectManager，低血量特效将不显示！");
        }

        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        GameObject healthBarObj = Instantiate(
            fixedHealthBarPrefab,
            vrCamera.transform.position,
            vrCamera.transform.rotation,
            vrCamera.transform
        );

        healthBarObj.transform.localPosition = new Vector3(0, -0.2f, 1.5f);
        healthBarObj.transform.localRotation = Quaternion.identity;
        healthBarObj.transform.localScale = Vector3.one * 0.002f;

        healthUI = healthBarObj.GetComponent<FixedHealthUI>();
        healthUI.Initialize(healthSystem);
    }

    // ⭐ 核心：血量变化回调
    private void OnHealthChanged()
    {
        if (lensEffectManager != null)
        {
            float percent = healthSystem.GetHealthPercentage();
            // Debug.Log($"当前血量百分比: {percent}"); // 调试用
            lensEffectManager.UpdateHealthEffect(percent);
        }
    }

    public void TakeDamage(float amount)
    {
        if (!isDead)
        {
            healthSystem.TakeDamage(amount);
            // 这里不需要手动调用 OnHealthChanged，因为上面已经订阅了 healthSystem.OnHealthChanged
            // 前提是你的 Health.cs 在 TakeDamage 里正确 Invoke 了事件
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        var thrower = GetComponentInChildren<SnowballThrower>();
        if (thrower != null) thrower.canThrow = false;

        var snowmanManager = FindObjectOfType<AdvancedSnowmanManager>();
        if (snowmanManager != null)
        {
            snowmanManager.ClearAllSnowmen();
        }

        ShowDeathUI();
        Time.timeScale = deathTimeScale;

        healthSystem.OnDeath -= Die;
        healthSystem.OnHealthChanged -= OnHealthChanged;
    }

    public void Heal(float amount)
    {
        if (!isDead)
        {
            healthSystem.Heal(amount);
        }
    }

    // ... [ShowDeathUI 和 DisableLeftHandController 保持不变]
    private void ShowDeathUI()
    {
        deathUIInstance = Instantiate(
            deathUIPrefab,
            vrCamera.transform.position + vrCamera.transform.forward * 1.5f + (-vrCamera.transform.up) * 1f,
            vrCamera.transform.rotation
        );
    }
}