using UnityEngine;
using System.Collections;

public class FixLighting : MonoBehaviour
{
    [Tooltip("必须赋值！这是正确的天空盒材质 (FS013_Snowy)")]
    public Material fallbackSkybox;

    void Start()
    {
        StartCoroutine(RefreshLightingRoutine());
    }

    IEnumerator RefreshLightingRoutine()
    {
        // 1. 稍微等待，让场景加载完
        yield return null;

        Material currentSky = RenderSettings.skybox;

        // 🔍 智能判断：如果当前已经是正确的天空盒，就不要捣乱了
        if (currentSky != null && fallbackSkybox != null && currentSky.name == fallbackSkybox.name)
        {
            Debug.Log("✅ [FixLighting] 检测到光照正常 (直接启动模式)，跳过强制重置。");

            // 仅仅轻微刷新一下，保底
            DynamicGI.UpdateEnvironment();
            yield break; // <--- 直接结束，不再执行下面的暴力操作
        }

        // ========================================================
        // 以下逻辑仅在“光照丢失”或“天空盒错误”时执行 (从菜单跳转时)
        // ========================================================

        Debug.Log("⚠️ [FixLighting] 检测到光照异常 (菜单跳转模式)，开始强制修复...");

        // 如果当前是空的，或者错的，就用备用的
        if (fallbackSkybox != null)
        {
            // 暴力重置流程
            RenderSettings.skybox = null;
            DynamicGI.UpdateEnvironment(); // 告诉Unity现在没天了

            yield return null; // 等一帧

            RenderSettings.skybox = fallbackSkybox; // 赋上正确的天
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        }

        // 强制刷新反射 (解决黑雪人)
        GL.Clear(false, true, Color.clear);
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;

        DynamicGI.UpdateEnvironment(); // 最后刷新

        Debug.Log("✅ [FixLighting] 修复完成");
    }
}