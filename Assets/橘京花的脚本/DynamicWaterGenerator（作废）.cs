using System.Collections.Generic;
using UnityEngine;

public class DynamicWaterGenerator : MonoBehaviour
{
    public GameObject WaterTilePrefab; // 水面 Prefab
    public float TileSize = 10f; // 每个水面格子的大小
    public List<GameObject> CustomPointObjects = new List<GameObject>(); // 存放空物体（用于定义水面范围）

    void Start()
    {
        if (CustomPointObjects.Count < 3)
        {
            Debug.LogError("至少需要 3 个点才能生成水面！");
            return;
        }

        // 把空物体的 Transform 转为坐标
        List<Vector3> customPoints = new List<Vector3>();
        foreach (GameObject pointObj in CustomPointObjects)
        {
            if (pointObj != null)
            {
                customPoints.Add(pointObj.transform.position);
            }
        }

        // 计算包围盒（决定水面范围）
        Bounds bounds = CalculateBounds(customPoints);

        // 生成水面格子
        GenerateWaterTiles(bounds, customPoints);
    }

    // 计算所有点的包围盒
    private Bounds CalculateBounds(List<Vector3> points)
    {
        if (points.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Vector3 min = points[0];
        Vector3 max = points[0];

        foreach (Vector3 point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    // 在包围盒内生成水面
    private void GenerateWaterTiles(Bounds bounds, List<Vector3> polygonPoints)
    {
        int tileCountX = Mathf.CeilToInt(bounds.size.x / TileSize);
        int tileCountZ = Mathf.CeilToInt(bounds.size.z / TileSize);

        for (int x = 0; x < tileCountX; x++)
        {
            for (int z = 0; z < tileCountZ; z++)
            {
                Vector3 tileCenter = new Vector3(
                    bounds.min.x + x * TileSize + TileSize * 0.5f,
                    0, // Y 轴固定为 0（水面高度）
                    bounds.min.z + z * TileSize + TileSize * 0.5f
                );

                // 检查这个格子是否在多边形内
                if (IsPointInPolygon(tileCenter, polygonPoints))
                {
                    Instantiate(WaterTilePrefab, tileCenter, Quaternion.identity, transform);
                }
            }
        }
    }

    // 判断点是否在多边形内（射线法）
    private bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        int intersections = 0;
        int pointCount = polygon.Count;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 p1 = polygon[i];
            Vector3 p2 = polygon[(i + 1) % pointCount];

            // 只考虑 XZ 平面
            if ((p1.z > point.z) != (p2.z > point.z))
            {
                float intersectX = (p2.x - p1.x) * (point.z - p1.z) / (p2.z - p1.z) + p1.x;
                if (point.x < intersectX)
                {
                    intersections++;
                }
            }
        }

        return (intersections % 2 == 1);
    }
}