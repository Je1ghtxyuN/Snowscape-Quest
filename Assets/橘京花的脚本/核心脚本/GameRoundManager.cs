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
    public GameObject startWall;

    [Header("难度优化")]
    [Tooltip("每回合额外生成的雪人数量，让玩家更容易凑齐击杀数，不用找最后一只。")]
    public int extraSpawnCount = 2;

    // 运行时数据
    private List<RoundData> currentRoundsConfig;
    private bool isEndlessMode = false;

    [Header("状态监控 (只读)")]
    public int currentRoundIndex = -1;
    [Tooltip("这里显示的是【剩余需要击杀】的数量，而不是场上存活的数量")]
    public int enemiesAlive = 0;
    public bool isGameComplete = false;

    private enum GameState { Waiting, Spawning, Fighting, UpgradePhase, Victory }
    private GameState currentState = GameState.Waiting;

    // 缓存当前回合的名称，用于UI刷新
    private string currentRoundNameDisplay;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (spawner == null) spawner = FindObjectOfType<AdvancedSnowmanManager>();

        // --- 从 GameSettings 读取配置 ---
        if (GameSettings.Instance != null)
        {
            isEndlessMode = (GameSettings.Instance.currentDifficulty == DifficultyLevel.Endless);

            if (!isEndlessMode)
            {
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
            Debug.LogWarning("⚠️ 未找到 GameSettings，启用默认无尽模式测试");
            isEndlessMode = true;
        }

        // 播放开场语音
        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Start", 1.0f);
            PetVoiceSystem.Instance.PlayVoice("Tutorial_Move", 24.5f);
            PetVoiceSystem.Instance.PlayVoice("Tutorial_Look", 34.5f);
        }

        StartCoroutine(WaitAndStartGameRoutine());
    }

    private IEnumerator WaitAndStartGameRoutine()
    {
        if (startWall != null) startWall.SetActive(true);

        Debug.Log("⏳ 游戏流程：等待新手语音播放 (43秒)...");
        yield return new WaitForSeconds(43.0f);

        if (startWall != null)
        {
            startWall.SetActive(false);
            Debug.Log("🔓 语音结束，空气墙已移除，玩家可自由移动。");
        }

        StartCoroutine(StartNextRoundRoutine());
    }

    private IEnumerator StartNextRoundRoutine()
    {
        currentRoundIndex++;

        // 检查是否通关 (普通模式)
        if (!isEndlessMode)
        {
            if (currentRoundIndex >= currentRoundsConfig.Count)
            {
                HandleVictory();
                yield break;
            }
        }

        currentState = GameState.Spawning;

        // --- ⭐ 1. 计算本回合的目标数据 ---
        int missionTargetCount = 0; // 玩家必须击杀的数量

        if (isEndlessMode)
        {
            // 无尽模式计算
            currentRoundNameDisplay = $"wave {currentRoundIndex + 1} (endless)";

            int baseCount = GameSettings.Instance ? GameSettings.Instance.endlessBaseEnemyCount : 5;
            float factor = GameSettings.Instance ? GameSettings.Instance.endlessGrowthFactor : 1.5f;

            missionTargetCount = Mathf.FloorToInt(baseCount + (currentRoundIndex * factor));
        }
        else
        {
            // 普通模式读取
            RoundData data = currentRoundsConfig[currentRoundIndex];
            currentRoundNameDisplay = data.roundName;
            missionTargetCount = data.enemyCount;
        }

        // --- ⭐ 2. 设置游戏状态 (UI显示剩余击杀数) ---
        // 我们只要求玩家击杀 missionTargetCount 个敌人
        enemiesAlive = missionTargetCount;

        // --- ⭐ 3. 执行生成 (生成数 = 目标数 + 缓冲数) ---
        // 多生成 extraSpawnCount 个，防止玩家找不到
        int actualSpawnCount = missionTargetCount + extraSpawnCount;

        UpdateUI(currentRoundNameDisplay);
        Debug.Log($"⚔️ 开始回合: {currentRoundNameDisplay}, 任务目标: {missionTargetCount}, 实际生成: {actualSpawnCount} (缓冲+{extraSpawnCount})");

        if (spawner != null)
        {
            spawner.SpawnEnemies(actualSpawnCount);
        }

        currentState = GameState.Fighting;
        UpdateUI(currentRoundNameDisplay);
    }

    public void OnEnemyKilled()
    {
        if (currentState != GameState.Fighting) return;

        // 减少剩余需要击杀的数量
        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;

        // 刷新UI显示
        if (gameInfoUI != null) gameInfoUI.UpdateInfo(currentRoundNameDisplay, enemiesAlive);

        // 如果击杀数达标 (enemiesAlive 归零)，即使场上还有那几个“缓冲用”的雪人，也直接结束
        if (enemiesAlive == 0)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        Debug.Log("🟢 回合目标达成！结束回合。");
        currentState = GameState.UpgradePhase;

        // ⭐ 关键：这里会清理掉场上所有剩下的雪人（包括那几个多生成的）
        // 这样玩家就会觉得“我刚好杀完了所有怪”，体验非常流畅
        if (spawner != null) spawner.ClearAllSnowmen();

        // 检查是否还有下一回合
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
            PetVoiceSystem.Instance.PlayVoice("Level_Complete", 1.0f);
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