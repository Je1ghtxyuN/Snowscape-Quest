using UnityEngine;
using System.Collections.Generic;

public class AdvancedSnowmanManager : MonoBehaviour
{
    [Header("雪人预制体")]
    public GameObject snowmanPrefab;

    [Header("生成参数")]
    public float minDistanceFromPlayer = 8f;
    public float minDistanceBetweenSnowmen = 3f;
    [Tooltip("生成高度偏移量")]
    public float spawnHeightOffset = 2.0f;

    [Header("区域设置")]
    public List<string> allowedAreas = new List<string>();

    private AdvancedGameAreaManager areaManager;
    private Transform player;
    // 列表用于管理当前存在的雪人
    private List<GameObject> currentSnowmen = new List<GameObject>();

    void Awake()
    {
        areaManager = FindObjectOfType<AdvancedGameAreaManager>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (areaManager == null) Debug.LogError("❌ 未找到 AdvancedGameAreaManager！");
    }

    // ⭐ 修改：不再在 Start 中自动生成，而是由 GameManager 调用
    public void SpawnEnemies(int count)
    {
        // 清理上一轮可能残留的雪人（理论上都被打死了，但为了安全）
        ClearDeadSnowmen();

        if (player == null || areaManager == null) return;

        List<Vector3> spawnPositions = GenerateValidSpawnPositions(count);

        if (spawnPositions.Count == 0)
        {
            Debug.LogWarning("⚠️ 未能生成任何有效的雪人位置。");
            // 极端情况补救：如果生成失败，也要通知管理器减少存活数，否则游戏会卡死
            for (int i = 0; i < count; i++) GameRoundManager.Instance.OnEnemyKilled();
            return;
        }

        // 如果生成点少于请求数（位置不够），把没生成的补上死亡计数
        int missingCount = count - spawnPositions.Count;
        for (int i = 0; i < missingCount; i++) GameRoundManager.Instance.OnEnemyKilled();

        foreach (Vector3 position in spawnPositions)
        {
            GameObject snowman = Instantiate(snowmanPrefab, position, Quaternion.identity);
            currentSnowmen.Add(snowman);
        }

        Debug.Log($"✅ 本回合成功生成 {spawnPositions.Count} 个雪人");
    }

    private List<Vector3> GenerateValidSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 20;

        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPos = GetRandomPositionInAllowedAreas();
            if (randomPos == Vector3.zero) continue;

            if (IsValidSpawnPosition(randomPos, positions))
            {
                positions.Add(randomPos);
            }
        }
        return positions;
    }

    private Vector3 GetRandomPositionInAllowedAreas()
    {
        List<AdvancedGameAreaManager.GameArea> availableAreas = new List<AdvancedGameAreaManager.GameArea>();
        if (areaManager.gameAreas == null) return Vector3.zero;

        foreach (var area in areaManager.gameAreas)
        {
            if (area.isActive)
            {
                if (allowedAreas.Count == 0 || allowedAreas.Contains(area.areaName))
                    availableAreas.Add(area);
            }
        }

        if (availableAreas.Count == 0) return Vector3.zero;

        AdvancedGameAreaManager.GameArea selectedArea = availableAreas[Random.Range(0, availableAreas.Count)];
        return GetRandomPointInArea(selectedArea);
    }

    private Vector3 GetRandomPointInArea(AdvancedGameAreaManager.GameArea area)
    {
        List<Vector3> polygon = area.GetBoundaryPoints();
        if (polygon.Count < 3) return Vector3.zero;

        Vector3 min = polygon[0];
        Vector3 max = polygon[0];
        foreach (Vector3 point in polygon)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Vector3 randomPoint;
        int attempts = 0;
        bool isValid = false;

        do
        {
            randomPoint = new Vector3(
                Random.Range(min.x, max.x),
                spawnHeightOffset,
                Random.Range(min.z, max.z)
            );
            attempts++;
            if (areaManager.IsPointInArea(randomPoint, area)) isValid = true;
        }
        while (!isValid && attempts < 50);

        return isValid ? randomPoint : Vector3.zero;
    }

    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> existingPositions)
    {
        Vector3 flatPos = new Vector3(position.x, 0, position.z);
        Vector3 flatPlayerPos = new Vector3(player.position.x, 0, player.position.z);

        if (Vector3.Distance(flatPos, flatPlayerPos) < minDistanceFromPlayer) return false;

        foreach (Vector3 existingPos in existingPositions)
        {
            Vector3 flatExisting = new Vector3(existingPos.x, 0, existingPos.z);
            if (Vector3.Distance(flatPos, flatExisting) < minDistanceBetweenSnowmen) return false;
        }
        return true;
    }
        
    private void ClearDeadSnowmen()
    {
        for (int i = currentSnowmen.Count - 1; i >= 0; i--)
        {
            if (currentSnowmen[i] == null) currentSnowmen.RemoveAt(i);
        }
    }

    public void ClearAllSnowmen()
    {
        for (int i = currentSnowmen.Count - 1; i >= 0; i--)
        {
            if (currentSnowmen[i] != null)
            {
                Destroy(currentSnowmen[i]);
            }
        }
        currentSnowmen.Clear();
        Debug.Log("🧹 已清空所有雪人");
    }
}