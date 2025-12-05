using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameRoundManager : MonoBehaviour
{
    public static GameRoundManager Instance { get; private set; }

    [System.Serializable]
    public struct RoundData
    {
        public string roundName;
        public int enemyCount;
    }

    [Header("引用设置")]
    public AdvancedSnowmanManager spawner;
    public UpgradeUIManager upgradeUI;
    public GameInfoUI gameInfoUI;

    // 运行时数据
    private List<RoundData> currentRoundsConfig;
    private bool isEndlessMode = false;

    [Header("状态监控 (只读)")]
    public int currentRoundIndex = -1;
    public int enemiesAlive = 0;
    public bool isGameComplete = false;

    private enum GameState { Waiting, Spawning, Fighting, UpgradePhase, Victory }
    private GameState currentState = GameState.Waiting;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (spawner == null) spawner = FindObjectOfType<AdvancedSnowmanManager>();

        // --- ⭐ 核心修改：从 GameSettings 读取配置 ---
        if (GameSettings.Instance != null)
        {
            isEndlessMode = (GameSettings.Instance.currentDifficulty == DifficultyLevel.Endless);

            if (!isEndlessMode)
            {
                // 普通模式：读取预设列表
                currentRoundsConfig = GameSettings.Instance.GetRoundsForCurrentDifficulty();
                Debug.Log($"🔵 已加载难度: {GameSettings.Instance.currentDifficulty}, 总回合数: {currentRoundsConfig.Count}");
            }
            else
            {
                Debug.Log("🟣 已启动无尽模式");
            }
        }
        else
        {
            // 保底：如果直接运行场景没有 GameSettings，就用无尽模式或默认数据
            Debug.LogWarning("⚠️ 未找到 GameSettings，启用默认无尽模式测试");
            isEndlessMode = true;
        }
        // -------------------------------------------

        // 播放开场语音
        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Start", 1.0f);
            PetVoiceSystem.Instance.PlayVoice("Tutorial1", 8.5f);
        }

        StartCoroutine(StartNextRoundRoutine());
    }

    private IEnumerator StartNextRoundRoutine()
    {
        currentRoundIndex++;

        // --- ⭐ 修改：判断游戏是否结束 ---
        if (!isEndlessMode)
        {
            // 普通模式：如果索引超过配置数量，游戏胜利
            if (currentRoundIndex >= currentRoundsConfig.Count)
            {
                HandleVictory();
                yield break;
            }
        }

        currentState = GameState.Spawning;

        // --- ⭐ 修改：计算当前回合数据 ---
        RoundData currentRoundData;

        if (isEndlessMode)
        {
            // 无尽模式：程序化生成数据
            currentRoundData = new RoundData();
            currentRoundData.roundName = $"Wave {currentRoundIndex + 1} (Endless)";

            // 算法：基础数量 + (回合数 * 系数)
            // 例如：5 + (0 * 2) = 5
            // 例如：5 + (5 * 2) = 15
            int baseCount = GameSettings.Instance ? GameSettings.Instance.endlessBaseEnemyCount : 5;
            float factor = GameSettings.Instance ? GameSettings.Instance.endlessGrowthFactor : 1.5f;

            currentRoundData.enemyCount = Mathf.FloorToInt(baseCount + (currentRoundIndex * factor));
        }
        else
        {
            // 普通模式：直接读取
            currentRoundData = currentRoundsConfig[currentRoundIndex];
        }

        UpdateUI(currentRoundData.roundName);
        Debug.Log($"⚔️ 开始回合: {currentRoundData.roundName}, 敌人: {currentRoundData.enemyCount}");

        if (spawner != null)
        {
            enemiesAlive = currentRoundData.enemyCount;
            spawner.SpawnEnemies(currentRoundData.enemyCount);
        }

        currentState = GameState.Fighting;
        UpdateUI(currentRoundData.roundName);
    }

    public void OnEnemyKilled()
    {
        if (currentState != GameState.Fighting) return;

        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;

        // 刷新UI显示
        string roundName = isEndlessMode ? $"第 {currentRoundIndex + 1} 波" : currentRoundsConfig[currentRoundIndex].roundName;
        if (gameInfoUI != null) gameInfoUI.UpdateInfo(roundName, enemiesAlive);

        if (enemiesAlive == 0)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        Debug.Log("🟢 回合结束");
        currentState = GameState.UpgradePhase;

        if (spawner != null) spawner.ClearAllSnowmen();

        // 检查是否还有下一回合（无尽模式永远有，普通模式看列表）
        bool hasNextRound = isEndlessMode || (currentRoundIndex < currentRoundsConfig.Count - 1);

        if (hasNextRound)
        {
            if (upgradeUI != null) upgradeUI.ShowUpgradePanel();
            else FinishUpgrade();
        }
        else
        {
            HandleVictory();
        }
    }

    public void FinishUpgrade()
    {
        if (upgradeUI != null) upgradeUI.HideUpgradePanel();
        StartCoroutine(StartNextRoundRoutine());
    }

    private void HandleVictory()
    {
        Debug.Log("🏆 游戏通关！");
        currentState = GameState.Victory;
        isGameComplete = true;

        if (gameInfoUI != null) gameInfoUI.UpdateInfo("任务完成", 0);

        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Success", 1.0f);
        }
    }

    private void UpdateUI(string roundName)
    {
        if (gameInfoUI != null)
        {
            gameInfoUI.UpdateInfo(roundName, enemiesAlive);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}