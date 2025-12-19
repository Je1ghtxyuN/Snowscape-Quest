using UnityEngine;
using System.Collections.Generic;

public class AdvancedGameAreaManager : MonoBehaviour
{
    [System.Serializable]
    public class GameArea
    {
        public string areaName = "New Area";
        public List<Transform> boundaryTransforms = new List<Transform>();
        [HideInInspector]
        public List<Vector3> boundaryPoints = new List<Vector3>();
        public float minY = -10f;
        public float maxY = 10f;
        public bool isActive = true;

        // 获取边界点（带空值检查的稳健版本）
        public List<Vector3> GetBoundaryPoints()
        {
            List<Vector3> points = new List<Vector3>();

            // 1. 优先尝试从 Transforms 获取实时坐标
            if (boundaryTransforms != null && boundaryTransforms.Count > 0)
            {
                foreach (Transform t in boundaryTransforms)
                {
                    if (t != null)
                    {
                        points.Add(t.position);
                    }
                }
            }

            // 2. 如果 Transforms 无效（比如没拖物体），尝试使用缓存的坐标
            if (points.Count == 0 && boundaryPoints != null && boundaryPoints.Count > 0)
            {
                points.AddRange(boundaryPoints);
            }

            return points;
        }

        public void UpdatePointsFromTransforms()
        {
            if (boundaryTransforms == null || boundaryTransforms.Count == 0) return;

            // 只有当存在有效 Transform 时才清除旧数据
            bool hasValidData = false;
            foreach (var t in boundaryTransforms) if (t != null) hasValidData = true;

            if (hasValidData)
            {
                boundaryPoints.Clear();
                foreach (Transform t in boundaryTransforms)
                {
                    if (t != null) boundaryPoints.Add(t.position);
                }
            }
        }
    }

    [Header("核心设置")]
    public List<GameArea> gameAreas = new List<GameArea>();
    public bool autoUpdatePoints = true;

    [Header("可视化调试")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    public bool showBoundaryLabels = true;
    public float gizmoSphereSize = 0.5f;

    // 将 Start 改为 Awake，确保数据比雪人生成器先准备好
    void Awake()
    {
        if (autoUpdatePoints)
        {
            UpdateAllAreaPoints();
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        // 编辑器模式下实时同步
        if (!Application.isPlaying && autoUpdatePoints)
        {
            UpdateAllAreaPoints();
        }
#endif
    }

    public void UpdateAllAreaPoints()
    {
        foreach (GameArea area in gameAreas)
        {
            if (area.isActive) area.UpdatePointsFromTransforms();
        }
    }

    // ---------------------------------------------------------
    // 核心逻辑：判断点是否在区域内
    // ---------------------------------------------------------

    public bool IsPointInAnyArea(Vector3 point)
    {
        foreach (GameArea area in gameAreas)
        {
            if (area.isActive && IsPointInArea(point, area)) return true;
        }
        return false;
    }

    public bool IsPointInArea(Vector3 point, GameArea area)
    {
        List<Vector3> points = area.GetBoundaryPoints();
        if (points.Count < 3) return false;

        if (point.y < area.minY || point.y > area.maxY) return false;

        return IsPointInPolygon(new Vector2(point.x, point.z), ConvertToVector2List(points));
    }

    // 获取点所在的区域
    public GameArea GetPointArea(Vector3 point)
    {
        foreach (GameArea area in gameAreas)
        {
            if (area.isActive && IsPointInArea(point, area)) return area;
        }
        return null;
    }

    // 将点限制在最近的区域内
    public Vector3 ClampPointToNearestArea(Vector3 point)
    {
        GameArea nearestArea = GetNearestArea(point);
        return nearestArea != null ? ClampPointToArea(point, nearestArea) : point;
    }

    public GameArea GetNearestArea(Vector3 point)
    {
        GameArea nearestArea = null;
        float minDistance = float.MaxValue;

        foreach (GameArea area in gameAreas)
        {
            if (!area.isActive) continue;
            List<Vector3> points = area.GetBoundaryPoints();
            if (points.Count < 3) continue;

            Vector3 closest = GetClosestPointOnBoundary(point, points);
            float d = Vector3.Distance(point, closest);
            if (d < minDistance)
            {
                minDistance = d;
                nearestArea = area;
            }
        }
        return nearestArea;
    }

    public Vector3 GetClosestPointOnBoundary(Vector3 point, List<Vector3> boundaryPoints)
    {
        Vector3 closestPoint = point;
        float minDistance = float.MaxValue;

        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            Vector3 p1 = boundaryPoints[i];
            Vector3 p2 = boundaryPoints[(i + 1) % boundaryPoints.Count];
            Vector3 segmentPoint = GetClosestPointOnLineSegment(p1, p2, point);
            float d = Vector3.Distance(point, segmentPoint);

            if (d < minDistance)
            {
                minDistance = d;
                closestPoint = segmentPoint;
            }
        }
        return closestPoint;
    }

    public Vector3 ClampPointToArea(Vector3 point, GameArea area)
    {
        if (IsPointInArea(point, area)) return point;

        List<Vector3> points = area.GetBoundaryPoints();
        Vector3 boundaryPoint = GetClosestPointOnBoundary(point, points);
        Vector3 center = GetAreaCenter(points);
        return boundaryPoint + (center - boundaryPoint).normalized * 0.1f;
    }

    public Vector3 GetAreaCenter(List<Vector3> points)
    {
        if (points.Count == 0) return Vector3.zero;
        Vector3 center = Vector3.zero;
        foreach (var p in points) center += p;
        return center / points.Count;
    }

    // ---------------------------------------------------------
    // 数学辅助方法
    // ---------------------------------------------------------

    private bool IsPointInPolygon(Vector2 p, List<Vector2> poly)
    {
        if (poly.Count < 3) return false;
        int j = poly.Count - 1;
        bool inside = false;
        for (int i = 0; i < poly.Count; j = i++)
        {
            if (((poly[i].y <= p.y && p.y < poly[j].y) || (poly[j].y <= p.y && p.y < poly[i].y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }

    private List<Vector2> ConvertToVector2List(List<Vector3> v3)
    {
        List<Vector2> v2 = new List<Vector2>();
        foreach (var v in v3) v2.Add(new Vector2(v.x, v.z));
        return v2;
    }

    private Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
        return a + Mathf.Clamp01(t) * ab;
    }

    // ---------------------------------------------------------
    // Gizmos 绘制
    // ---------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        foreach (GameArea area in gameAreas)
        {
            if (!area.isActive) continue;

            List<Vector3> points = area.GetBoundaryPoints();

            // 如果点太少，无法绘制区域，跳过
            if (points.Count < 3) continue;

            Gizmos.color = gizmoColor;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p1 = points[i];
                Vector3 p2 = points[(i + 1) % points.Count];

                Gizmos.DrawSphere(p1, gizmoSphereSize);
                Gizmos.DrawLine(p1, p2);
            }

            if (showBoundaryLabels)
            {
#if UNITY_EDITOR
                Vector3 center = GetAreaCenter(points);
                UnityEditor.Handles.Label(center, area.areaName);
#endif
            }
        }
    }

    // ---------------------------------------------------------
    // 🛠️ 强力调试工具 (ContextMenu)
    // ---------------------------------------------------------

    [ContextMenu("🛠️ 打印调试信息 (Print Debug Info)")]
    public void PrintDebugStatus()
    {
        Debug.Log("============= 开始诊断 GameAreaManager =============");

        if (!showGizmos)
            Debug.LogWarning("⚠️ Show Gizmos 选项未勾选！请在 Inspector 面板中勾选。");

        if (gizmoColor.a == 0)
            Debug.LogWarning("⚠️ Gizmo Color 的透明度 (Alpha) 为 0，线条将不可见！");

        if (gameAreas == null || gameAreas.Count == 0)
        {
            Debug.LogError("❌ 错误：Game Areas 列表为空！请点击 '+' 号添加一个区域。");
            return;
        }

        for (int i = 0; i < gameAreas.Count; i++)
        {
            GameArea area = gameAreas[i];
            Debug.Log($"--- 检查区域 {i}: {area.areaName} ---");

            if (!area.isActive)
            {
                Debug.LogWarning($"   ⚠️ 区域 {area.areaName} 未激活 (Is Active = false)。");
                continue;
            }

            // 检查 Transform 列表
            int transformCount = area.boundaryTransforms != null ? area.boundaryTransforms.Count : 0;
            Debug.Log($"   ℹ️ Transform 物体数量: {transformCount}");

            if (transformCount > 0)
            {
                for (int t = 0; t < area.boundaryTransforms.Count; t++)
                {
                    if (area.boundaryTransforms[t] == null)
                        Debug.LogError($"   ❌ 错误：第 {t} 个 Transform 是空引用 (Missing)！请重新拖拽。");
                }
            }

            // 检查最终获取的点
            List<Vector3> finalPoints = area.GetBoundaryPoints();
            Debug.Log($"   ℹ️ 最终计算出的边界点数量: {finalPoints.Count}");

            if (finalPoints.Count < 3)
            {
                Debug.LogError($"   ❌ 错误：有效点不足 3 个（当前 {finalPoints.Count} 个）。无法构成多边形，无法显示。");
            }
            else
            {
                Debug.Log($"   ✅ 状态良好。第一个点坐标: {finalPoints[0]}");

                // 检查点是否重叠
                if (Vector3.Distance(finalPoints[0], finalPoints[1]) < 0.01f)
                {
                    Debug.LogWarning("   ⚠️ 警告：前两个点的位置几乎重叠，请确保物体位置已拉开。");
                }
            }
        }

        Debug.Log("============= 诊断结束 =============");
    }
}