// GraphSystemRepairTool.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GraphSystemRepairTool
{
    static GraphSystemRepairTool()
    {
        EditorApplication.delayCall += SafeRefreshGraphs;
    }

    private static void SafeRefreshGraphs()
    {
        // 通过资源重载方式安全刷新
        var guids = AssetDatabase.FindAssets("t:ShaderGraph t:VFXGraph");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log($"已安全刷新 {guids.Length} 个图形资源");
    }

    [MenuItem("Tools/安全修复图形系统")]
    public static void ManualRepair() => SafeRefreshGraphs();
}
#endif