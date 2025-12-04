using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class GameRoundManager : MonoBehaviour
{
    public static GameRoundManager Instance { get; private set; }

    [System.Serializable]
    public struct RoundData
    {
        public string roundName; // 回合名称，如 "第1波"
        public int enemyCount;   // 这一波生成的敌人数量
    }

    [Header("回合配置")]
    public List<RoundData> rounds = new List<RoundData>();

    [Header("引用设置")]
    public AdvancedSnowmanManager spawner;
    public UpgradeUIManager upgradeUI; // 下面会提供这个脚本
    public GameInfoUI gameInfoUI;      // 替代原来的 ScoreDisplayUI

    [Header("状态监控 (只读)")]
    public int currentRoundIndex = -1;
    public int enemiesAlive = 0;
    public bool isGameComplete = false;

    // 游戏状态
    private enum GameState { Waiting, Spawning, Fighting, UpgradePhase, Victory }
    private GameState currentState = GameState.Waiting;

    void Awake()
    {
        // 单例模式
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (spawner == null) spawner = FindObjectOfType<AdvancedSnowmanManager>();

        if (PetVoiceSystem.Instance != null)
        {
            // 1. 播放第一句 (例如：延迟 1秒 后开始，给玩家一点反应时间)
            // 语音ID: "Intro_1" (请确保你在Inspector里填了这个ID)
            PetVoiceSystem.Instance.PlayVoice("Start", 1.0f);

            // 2. 播放第二句 (例如：延迟 5秒 后开始)
            // ⚠️ 关键点：这个时间 = 第一句的开始时间(1s) + 第一句音频的时长(假设4s)
            // 如果你设置的时间太短，导致第一句还没说完，PetVoiceSystem 会因为防打断逻辑跳过第二句！
            PetVoiceSystem.Instance.PlayVoice("Tutorial1", 8.5f);
        }

        // 游戏开始，进入第一回合
        StartCoroutine(StartNextRoundRoutine());
    }

    // 开启下一回合的协程
    private IEnumerator StartNextRoundRoutine()
    {
        currentRoundIndex++;

        // 检查是否所有回合都结束了
        if (currentRoundIndex >= rounds.Count)
        {
            HandleVictory();
            yield break;
        }

        currentState = GameState.Spawning;
        UpdateUI();

        // 获取当前回合配置
        RoundData currentRound = rounds[currentRoundIndex];
        Debug.Log($"🔵 开始回合: {currentRound.roundName}, 敌人数量: {currentRound.enemyCount}");

        // 通知生成器干活
        if (spawner != null)
        {
            enemiesAlive = currentRound.enemyCount;
            spawner.SpawnEnemies(currentRound.enemyCount);
        }

        currentState = GameState.Fighting;
        UpdateUI();
    }

    // 当敌人死亡时被调用 (由 EnemyBaofeng/EnemyHealth 调用)
    public void OnEnemyKilled()
    {
        if (currentState != GameState.Fighting) return;

        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;

        UpdateUI();

        // 检查当前回合是否清空
        if (enemiesAlive == 0)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        Debug.Log("🟢 回合结束，进入休息/升级阶段");
        currentState = GameState.UpgradePhase;

        if (spawner != null)
        {
            spawner.ClearAllSnowmen();
        }

        // 如果还有下一回合，显示升级UI
        if (currentRoundIndex < rounds.Count - 1)
        {
            if (upgradeUI != null)
            {
                upgradeUI.ShowUpgradePanel();
            }
            else
            {
                // 如果没有设置UI，直接下一回合
                FinishUpgrade();
            }
        }
        else
        {
            // 如果是最后一回合打完，直接胜利
            HandleVictory();
        }
    }

    // 升级选择完毕，由 UI 按钮调用此方法
    public void FinishUpgrade()
    {
        if (upgradeUI != null) upgradeUI.HideUpgradePanel();
        StartCoroutine(StartNextRoundRoutine());
    }

    private void HandleVictory()
    {
        Debug.Log("🏆 游戏通关！请前往大门。");
        currentState = GameState.Victory;
        isGameComplete = true;
        UpdateUI();

        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.PlayVoice("Success", 1.0f); // 延迟1秒更有感觉
        }
    }

    private void UpdateUI()
    {
        if (gameInfoUI != null)
        {
            string roundText = (currentRoundIndex + 1) > rounds.Count ? "通关" : $"{currentRoundIndex + 1} / {rounds.Count}";
            gameInfoUI.UpdateInfo(roundText, enemiesAlive);
        }
    }
}