using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets; // 如果你的移动脚本在Sample命名空间下
using UnityEngine.XR.Interaction.Toolkit;
// 注意：如果你找不到 ActionBasedContinuousMoveProvider，请检查你的 Locomotion 物体上的组件名

public class PlayerUpgradeHandler : MonoBehaviour
{
    public static PlayerUpgradeHandler Instance { get; private set; }

    [Header("引用")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider moveProvider;

    [Header("当前属性")]
    public float damageMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public int maxAmmoLevel = 0; // 弹药逻辑暂时预留

    [Header("精灵升级属性")]
    public int petProjectileCount = 1;      // 子弹数量
    public float petFireRateMultiplier = 1.0f; // 射速倍率 
    public float petDamageMultiplier = 1.0f;   // 伤害倍率

    private float initialMoveSpeed = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 自动尝试获取组件
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        // 尝试在子物体中寻找 WeaponController (它在右手上)
        if (weaponController == null) weaponController = GetComponentInChildren<PlayerWeaponController>();

        // 尝试寻找移动脚本 (通常在 Locomotion 子物体上)
        if (moveProvider == null) moveProvider = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider>();

        // 记录初始速度
        if (moveProvider != null)
        {
            initialMoveSpeed = moveProvider.moveSpeed;
        }
        else
        {
            Debug.LogError("未找到移动控制脚本 (ActionBasedContinuousMoveProvider)，速度升级将无效！");
        }
    }

    // --- 升级执行方法 ---

    public void UpgradeHeal(float amount)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(amount);
            Debug.Log($"玩家回复了 {amount} 点血量");
        }
    }

    public void UpgradeDamage(float percentage)
    {
        damageMultiplier += percentage; // 例如传入 0.2f，倍率变为 1.2
        Debug.Log($"伤害提升！当前倍率: {damageMultiplier}");
    }

    public void UpgradeSpeed(float percentage)
    {
        if (moveProvider != null)
        {
            speedMultiplier += percentage;
            moveProvider.moveSpeed = initialMoveSpeed * speedMultiplier;
            Debug.Log($"速度提升！当前速度: {moveProvider.moveSpeed}");
        }
    }

    public void UnlockSword()
    {
        if (weaponController != null)
        {
            weaponController.UnlockSword();
        }
    }

    public bool IsSwordUnlocked()
    {
        if (weaponController != null) return weaponController.hasUnlockedSword;
        return false;
    }

    // --- 精灵升级方法 ---
    public void UpgradePetMultishot()
    {
        petProjectileCount++;
        Debug.Log($"精灵升级：多重射击！当前数量: {petProjectileCount}");
    }

    public void UpgradePetFireRate(float amount) // amount 比如 0.2 表示快20%
    {
        petFireRateMultiplier += amount;
        Debug.Log($"精灵升级：射速提升！当前倍率: {petFireRateMultiplier}");
    }

    public void UpgradePetDamage(float amount)
    {
        petDamageMultiplier += amount;
        Debug.Log($"精灵升级：伤害提升！当前倍率: {petDamageMultiplier}");
    }
}