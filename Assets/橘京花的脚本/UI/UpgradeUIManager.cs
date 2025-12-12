using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeOption
    {
        public string id;
        public string displayText;
        [TextArea] public string description;
    }

    [Header("UI组件引用")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TextMeshProUGUI leftButtonText;
    [SerializeField] private TextMeshProUGUI rightButtonText;

    [Header("特效引用 ")]
    [SerializeField] private GameObject levelUpEffectPrefab;

    [Tooltip("请选择地面的Layer")]
    [SerializeField] private LayerMask groundLayer;

    [Header("升级池设置")]
    [SerializeField] private List<UpgradeOption> upgradePool = new List<UpgradeOption>();

    [Header("VR显示设置")]
    [SerializeField] private float displayDistance = 2f;
    [SerializeField] private float heightOffset = -0.3f;
    [SerializeField] private float playerFeetOffset = -1.7f;

    private UpgradeOption currentLeftUpgrade;
    private UpgradeOption currentRightUpgrade;
    private Transform playerCamera;

    void Start()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;
        if (upgradePanel != null) upgradePanel.SetActive(false);

        // 如果没有配置，添加默认配置（包含寒冰剑）
        if (upgradePool.Count == 0)
        {
            upgradePool.Add(new UpgradeOption { id = "HEAL", displayText = "恢复生命\n<size=60%>回复 30 点血量</size>" });
            upgradePool.Add(new UpgradeOption { id = "DAMAGE", displayText = "力量强化\n<size=60%>提升 20% 伤害</size>" });
            upgradePool.Add(new UpgradeOption { id = "SPEED", displayText = "迅捷步伐\n<size=60%>提升 20% 移速</size>" });
            // 只有当玩家还没解锁剑的时候，才应该放入池子，这里简化处理，逻辑里判断
            upgradePool.Add(new UpgradeOption { id = "SWORD", displayText = "寒冰之剑\n<size=60%>解锁近战武器 (按B切换)</size>" });

            upgradePool.Add(new UpgradeOption { id = "PET_MULTI", displayText = "精灵散射\n<size=60%>精灵子弹数量 +1</size>" });
            upgradePool.Add(new UpgradeOption { id = "PET_RATE", displayText = "精灵急速\n<size=60%>精灵射速提升 25%</size>" });
            upgradePool.Add(new UpgradeOption { id = "PET_DMG", displayText = "精灵强化\n<size=60%>精灵伤害提升 30%</size>" });
        }
    }

    public void ShowUpgradePanel()
    {
        if (upgradePanel == null) return;

        if (playerCamera != null)
        {
            Vector3 newPos = playerCamera.position + playerCamera.forward * displayDistance + Vector3.up * heightOffset;
            upgradePanel.transform.position = newPos;
            upgradePanel.transform.LookAt(playerCamera);
            upgradePanel.transform.Rotate(0, 180f, 0);
        }

        RandomizeUpgrades();
        upgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void HideUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void RandomizeUpgrades()
    {
        // 1. 创建一个临时列表，用于筛选有效的升级
        List<UpgradeOption> validPool = new List<UpgradeOption>();

        foreach (var option in upgradePool)
        {
            // ⭐ 核心逻辑：如果是寒冰剑，且已经解锁了，就不加进池子
            if (option.id == "SWORD")
            {
                if (PlayerUpgradeHandler.Instance != null && !PlayerUpgradeHandler.Instance.IsSwordUnlocked())
                {
                    validPool.Add(option); // 还没解锁，可以加
                }
            }
            else
            {
                validPool.Add(option); // 其他升级都可以无限加
            }
        }

        if (validPool.Count < 2)
        {
            Debug.LogWarning("有效升级选项不足2个！");
            return;
        }

        // 2. 从有效池中随机
        int index1 = Random.Range(0, validPool.Count);
        int index2 = index1;
        while (index2 == index1)
        {
            index2 = Random.Range(0, validPool.Count);
        }

        currentLeftUpgrade = validPool[index1];
        currentRightUpgrade = validPool[index2];

        if (leftButtonText != null) leftButtonText.text = currentLeftUpgrade.displayText;
        if (rightButtonText != null) rightButtonText.text = currentRightUpgrade.displayText;
    }

    private void ApplyUpgradeEffect(UpgradeOption upgrade)
    {
        Debug.Log($"执行升级: {upgrade.id}");

        SpawnVisualEffect();

        // 获取 PlayerUpgradeHandler 单例
        var handler = PlayerUpgradeHandler.Instance;
        if (handler == null)
        {
            Debug.LogError("场景中找不到 PlayerUpgradeHandler，升级无法生效！");
            return;
        }

        switch (upgrade.id)
        {
            case "HEAL":
                handler.UpgradeHeal(30f);
                break;

            case "DAMAGE":
                handler.UpgradeDamage(0.25f); // 增加 25%
                break;

            case "SPEED":
                handler.UpgradeSpeed(0.2f); // 增加 20%
                break;

            case "SWORD":
                handler.UnlockSword();
                break;

            //case "AMMO":
            //    Debug.Log("弹药升级暂未实现");
            //    break;
            case "PET_MULTI":
                handler.UpgradePetMultishot();
                break;
            case "PET_RATE":
                handler.UpgradePetFireRate(0.25f);
                break;
            case "PET_DMG":
                handler.UpgradePetDamage(0.3f);
                break;

            default:
                Debug.LogWarning("未知的升级ID");
                break;
        }
    }

    private void SpawnVisualEffect()
    {
        if (levelUpEffectPrefab != null && playerCamera != null)
        {
            // 1. 生成特效，并将 playerCamera 设置为父物体
            // 这样特效就会随着相机（玩家）移动而移动
            GameObject effect = Instantiate(levelUpEffectPrefab, playerCamera);

            // 2. 设置局部坐标
            // X=0, Z=0 保证光柱在玩家中心
            // Y使用 playerFeetOffset (比如 -1.7)，让光柱脚底对齐玩家脚底
            effect.transform.localPosition = new Vector3(0, playerFeetOffset, 0);

            // 3. 重置旋转
            effect.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("LevelUpEffectPrefab 未赋值 或 找不到玩家相机！");
        }
    }

    public void OnLeftButtonClicked()
    {
        if (currentLeftUpgrade != null) ApplyUpgradeEffect(currentLeftUpgrade);
        GameRoundManager.Instance.FinishUpgrade();
    }

    public void OnRightButtonClicked()
    {
        if (currentRightUpgrade != null) ApplyUpgradeEffect(currentRightUpgrade);
        GameRoundManager.Instance.FinishUpgrade();
    }
}