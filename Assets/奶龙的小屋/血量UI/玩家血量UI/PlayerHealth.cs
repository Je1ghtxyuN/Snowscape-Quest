using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject fixedHealthBarPrefab;
    [SerializeField] private GameObject deathUIPrefab; // 新增死亡UI预制体
    [SerializeField] private Material deathFilterMaterial; // 新增死亡滤镜材质(红色或黑白)
    [SerializeField] private float deathTimeScale = 0.3f; // 死亡时的时间缩放
    [SerializeField] private GameObject leftHandController;

    private Health healthSystem;
    private FixedHealthUI healthUI;
    private Camera vrCamera;
    private GameObject deathUIInstance;
    private bool isDead = false;

    void Start()
    {
        healthSystem = new Health(100f);
        healthSystem.OnDeath += Die;

        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Debug.LogError("Main Camera not found");
            return;
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

    public void TakeDamage(float amount)
    {
        if (!isDead) healthSystem.TakeDamage(amount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;


        // 显示死亡UI
        ShowDeathUI();

        // 放慢游戏时间
        Time.timeScale = deathTimeScale;

        // 取消事件订阅
        healthSystem.OnDeath -= Die;
    }

    public void Heal(float amount)
    {
        if (!isDead)
        {
            // 假设Health类有Heal方法，如果没有需要添加
            healthSystem.Heal(amount);
        }
    }

    private void ShowDeathUI()
    {
        // 实例化死亡UI
        deathUIInstance = Instantiate(
            deathUIPrefab,
            vrCamera.transform.position + vrCamera.transform.forward * 1.5f + (-vrCamera.transform.up) * 1f,
            vrCamera.transform.rotation
        );

        // 调整UI位置和大小
        //deathUIInstance.transform.localScale = Vector3.one * 0.003f;

    }

    private void DisableLeftHandController()
    {
        if (leftHandController != null)
        {
            // 禁用控制器游戏对象
            leftHandController.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Left hand controller reference not set in PlayerHealth");
        }
    }
}