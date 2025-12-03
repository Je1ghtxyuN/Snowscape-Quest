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

    [Header("升级池设置")]
    [SerializeField] private List<UpgradeOption> upgradePool = new List<UpgradeOption>();

    [Header("VR显示设置")]
    [SerializeField] private float displayDistance = 2f;
    [SerializeField] private float heightOffset = -0.3f;

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
        if (upgradePool.Count < 2) return;

        int index1 = Random.Range(0, upgradePool.Count);
        int index2 = index1;
        while (index2 == index1)
        {
            index2 = Random.Range(0, upgradePool.Count);
        }

        currentLeftUpgrade = upgradePool[index1];
        currentRightUpgrade = upgradePool[index2];

        if (leftButtonText != null) leftButtonText.text = currentLeftUpgrade.displayText;
        if (rightButtonText != null) rightButtonText.text = currentRightUpgrade.displayText;
    }

    private void ApplyUpgradeEffect(UpgradeOption upgrade)
    {
        Debug.Log($"执行升级: {upgrade.id}");

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

            default:
                Debug.LogWarning("未知的升级ID");
                break;
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