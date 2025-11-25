using UnityEngine;
using System.Collections.Generic;

public class AdvancedSnowmanManager : MonoBehaviour
{
    [Header("雪人生成设置")]
    public GameObject snowmanPrefab;
    public int snowmenPerRound = 5;
    public float minDistanceFromPlayer = 8f;
    public float minDistanceBetweenSnowmen = 3f;

    [Header("区域设置")]
    public List<string> allowedAreas = new List<string>(); // 允许生成雪人的区域名称

    private AdvancedGameAreaManager areaManager;
    private Transform player;
    private List<GameObject> currentSnowmen = new List<GameObject>();

    void Start()
    {
        areaManager = FindObjectOfType<AdvancedGameAreaManager>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (areaManager == null)
        {
            Debug.LogError("未找到AdvancedGameAreaManager！");
            return;
        }

        SpawnRoundSnowmen();
    }

    public void SpawnRoundSnowmen()
    {
        ClearCurrentSnowmen();

        List<Vector3> spawnPositions = GenerateValidSpawnPositions(snowmenPerRound);

        foreach (Vector3 position in spawnPositions)
        {
            GameObject snowman = Instantiate(snowmanPrefab, position, Quaternion.identity);

            // 设置高级AI组件
            AdvancedEnemyAI snowmanAI = snowman.GetComponent<AdvancedEnemyAI>();
            if (snowmanAI != null)
            {
                // 巡逻点会在AI的Start中自动生成
            }

            currentSnowmen.Add(snowman);
        }

        Debug.Log($"生成 {spawnPositions.Count} 个雪人");
    }

    private List<Vector3> GenerateValidSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        int maxAttempts = count * 10; // 防止无限循环

        for (int i = 0; i < count && positions.Count < maxAttempts; i++)
        {
            Vector3 randomPos = GetRandomPositionInAllowedAreas();

            if (IsValidSpawnPosition(randomPos, positions))
            {
                positions.Add(randomPos);
            }
        }

        return positions;
    }

    private Vector3 GetRandomPositionInAllowedAreas()
    {
        // 获取所有允许的区域
        List<AdvancedGameAreaManager.GameArea> availableAreas = new List<AdvancedGameAreaManager.GameArea>();

        foreach (var area in areaManager.gameAreas)
        {
            if (area.isActive && (allowedAreas.Count == 0 || allowedAreas.Contains(area.areaName)))
            {
                availableAreas.Add(area);
            }
        }

        if (availableAreas.Count == 0)
        {
            Debug.LogWarning("没有可用的生成区域！");
            return Vector3.zero;
        }

        // 随机选择一个区域
        AdvancedGameAreaManager.GameArea selectedArea = availableAreas[Random.Range(0, availableAreas.Count)];

        // 在区域内随机生成点
        return GetRandomPointInPolygon(selectedArea.boundaryPoints);
    }

    private Vector3 GetRandomPointInPolygon(List<Vector3> polygon)
    {
        if (polygon.Count < 3) return Vector3.zero;

        // 使用三角形剖分方法生成随机点
        Vector3 min = polygon[0];
        Vector3 max = polygon[0];

        foreach (Vector3 point in polygon)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Vector3 randomPoint;
        int attempts = 0;

        do
        {
            randomPoint = new Vector3(
                Random.Range(min.x, max.x),
                0,
                Random.Range(min.z, max.z)
            );
            attempts++;
        }
        while (!areaManager.IsPointInArea(randomPoint, areaManager.GetPointArea(randomPoint)) && attempts < 100);

        return randomPoint;
    }

    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> existingPositions)
    {
        // 检查距离玩家
        if (Vector3.Distance(position, player.position) < minDistanceFromPlayer)
            return false;

        // 检查距离其他雪人
        foreach (Vector3 existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenSnowmen)
                return false;
        }

        return true;
    }

    private void ClearCurrentSnowmen()
    {
        foreach (GameObject snowman in currentSnowmen)
        {
            if (snowman != null)
                Destroy(snowman);
        }
        currentSnowmen.Clear();
    }

    // 获取当前存活的雪人数量
    public int GetAliveSnowmenCount()
    {
        int count = 0;
        foreach (GameObject snowman in currentSnowmen)
        {
            if (snowman != null && !snowman.GetComponent<AdvancedEnemyAI>().isDead)
            {
                count++;
            }
        }
        return count;
    }
}