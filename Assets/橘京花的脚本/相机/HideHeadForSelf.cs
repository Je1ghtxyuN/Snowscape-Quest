using UnityEngine;
using UnityEngine.Rendering; // 引入渲染命名空间
using System.Collections.Generic;

public class HideHeadForSelf : MonoBehaviour
{
    [Header("头部渲染器设置")]
    [Tooltip("请把模型头部、头发、眼镜等会遮挡视线的 Renderer 拖进去")]
    public List<Renderer> headRenderers = new List<Renderer>();

    [Header("身体渲染器 (可选)")]
    [Tooltip("如果你想隐藏整个身体只留手，可以把身体也拖进去")]
    public List<Renderer> bodyRenderers = new List<Renderer>();

    void Start()
    {
        HideRenderers(headRenderers);
    }

    private void HideRenderers(List<Renderer> renderers)
    {
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                // ⭐ 核心魔法：设置为 "ShadowsOnly"
                // 这意味着摄像机看不见它，但光照照在它身上依然会在地上产生影子
                rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    // 提供一个外部方法，方便在换装后重新隐藏
    public void RefreshHide()
    {
        HideRenderers(headRenderers);
    }
}