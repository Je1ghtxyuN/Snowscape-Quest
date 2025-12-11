using UnityEngine;
using System.Collections.Generic; // 引入列表命名空间

public class IceArmorVisuals : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("请使用 Custom/URP_IceShield Shader 的材质")]
    public Material iceMaterial;

    [Header("目标部位列表")]
    [Tooltip("输入所有你想覆盖护甲的渲染器名字关键字 (例如: Renderer_Body, Renderer_Head)")]
    public List<string> targetRendererNames = new List<string>();

    // 内部列表：存储找到的真实渲染器
    private List<SkinnedMeshRenderer> foundRenderers = new List<SkinnedMeshRenderer>();
    // 内部列表：存储生成的冰壳物体
    private List<GameObject> activeShells = new List<GameObject>();

    void Start()
    {
        InitializeTargets();
    }

    void InitializeTargets()
    {
        foundRenderers.Clear();

        // 1. 如果列表为空，默认尝试找 "Renderer_Body" 作为保底
        if (targetRendererNames.Count == 0)
        {
            targetRendererNames.Add("Renderer_Body");
        }

        // 2. 遍历名字列表，去模型里找对应的组件
        foreach (string nameKey in targetRendererNames)
        {
            // 尝试按名字查找
            Transform found = FindDeepChild(transform, nameKey);
            if (found != null)
            {
                SkinnedMeshRenderer smr = found.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    foundRenderers.Add(smr);
                }
            }
        }

        // 3. 如果通过名字一个都没找到，就粗暴地获取所有子物体的 SMR
        if (foundRenderers.Count == 0)
        {
            SkinnedMeshRenderer[] allSmrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            foundRenderers.AddRange(allSmrs);
            Debug.LogWarning($"⚠️ 未按名字找到指定部位，已自动添加所有 {allSmrs.Length} 个渲染器作为护甲目标。");
        }
    }

    public void EnableArmor()
    {
        if (iceMaterial == null) return;
        if (activeShells.Count > 0) return; // 已经开启了

        // 对每一个找到的身体部位，都生成一个冰壳
        foreach (var target in foundRenderers)
        {
            CreateShellFor(target);
        }
    }

    void CreateShellFor(SkinnedMeshRenderer target)
    {
        // 1. 创建冰壳物体
        GameObject shell = new GameObject($"IceShell_{target.name}");
        shell.transform.SetParent(target.transform.parent);

        // 2. 对齐
        shell.transform.localPosition = target.transform.localPosition;
        shell.transform.localRotation = target.transform.localRotation;
        shell.transform.localScale = target.transform.localScale; // 保持一致，放大交给Shader

        // 3. 复制数据
        SkinnedMeshRenderer iceSMR = shell.AddComponent<SkinnedMeshRenderer>();
        iceSMR.sharedMesh = target.sharedMesh;
        iceSMR.rootBone = target.rootBone;
        iceSMR.bones = target.bones;
        iceSMR.material = iceMaterial;
        iceSMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 4. 加入管理列表
        activeShells.Add(shell);
    }

    public void DisableArmor()
    {
        foreach (var shell in activeShells)
        {
            if (shell != null) Destroy(shell);
        }
        activeShells.Clear();
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}