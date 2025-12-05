using UnityEngine;
using System.Collections.Generic;

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard,
    Endless // 无尽模式
}

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("当前选择的难度")]
    public DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

    [Header("难度配置 (在Inspector中设置)")]
    public List<GameRoundManager.RoundData> easyRounds;
    public List<GameRoundManager.RoundData> normalRounds;
    public List<GameRoundManager.RoundData> hardRounds;

    [Header("无尽模式配置")]
    public int endlessBaseEnemyCount = 5; // 初始数量
    public float endlessGrowthFactor = 1.2f; // 每回合数量增长系数

    void Awake()
    {
        // 保证全场只有一个 GameSettings，且切场景不销毁
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 获取当前难度的回合数据
    public List<GameRoundManager.RoundData> GetRoundsForCurrentDifficulty()
    {
        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy: return easyRounds;
            case DifficultyLevel.Normal: return normalRounds;
            case DifficultyLevel.Hard: return hardRounds;
            default: return new List<GameRoundManager.RoundData>(); // 无尽模式不需要预设列表
        }
    }
}