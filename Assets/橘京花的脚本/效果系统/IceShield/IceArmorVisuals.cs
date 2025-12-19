using UnityEngine;
using System.Collections.Generic;

public class IceArmorVisuals : MonoBehaviour
{
    [Header("设置")]
    public Material iceMaterial;
    public List<string> targetRendererNames = new List<string>();

    private List<SkinnedMeshRenderer> foundRenderers = new List<SkinnedMeshRenderer>();
    private List<GameObject> activeShells = new List<GameObject>();

    void Start()
    {
        InitializeTargets();
    }

    void InitializeTargets()
    {
        foundRenderers.Clear();
        if (targetRendererNames.Count == 0) targetRendererNames.Add("Renderer_Body");

        foreach (string nameKey in targetRendererNames)
        {
            Transform found = FindDeepChild(transform, nameKey);
            if (found != null)
            {
                SkinnedMeshRenderer smr = found.GetComponent<SkinnedMeshRenderer>();
                if (smr != null) foundRenderers.Add(smr);
            }
        }

        if (foundRenderers.Count == 0)
        {
            SkinnedMeshRenderer[] allSmrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            foundRenderers.AddRange(allSmrs);
        }
    }

    public void EnableArmor()
    {
        // ⭐ 修改：对照组禁止生成冰甲
        if (ExperimentVisualControl.Instance != null && !ExperimentVisualControl.Instance.ShouldShowVisuals())
        {
            return;
        }

        if (iceMaterial == null) return;
        if (activeShells.Count > 0) return;

        foreach (var target in foundRenderers)
        {
            CreateShellFor(target);
        }
    }

    void CreateShellFor(SkinnedMeshRenderer target)
    {
        GameObject shell = new GameObject($"IceShell_{target.name}");
        shell.transform.SetParent(target.transform.parent);
        shell.transform.localPosition = target.transform.localPosition;
        shell.transform.localRotation = target.transform.localRotation;
        shell.transform.localScale = target.transform.localScale;

        SkinnedMeshRenderer iceSMR = shell.AddComponent<SkinnedMeshRenderer>();
        iceSMR.sharedMesh = target.sharedMesh;
        iceSMR.rootBone = target.rootBone;
        iceSMR.bones = target.bones;
        iceSMR.material = iceMaterial;
        iceSMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

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