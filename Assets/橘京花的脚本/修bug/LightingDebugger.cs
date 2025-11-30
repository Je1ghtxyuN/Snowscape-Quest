using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

public class LightingDebugger : MonoBehaviour
{
    void Start()
    {
        // 延迟一帧检测，确保场景加载逻辑已完全跑完
        StartCoroutine(CheckLightingData());
    }

    System.Collections.IEnumerator CheckLightingData()
    {
        yield return null;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=========== 💡 光照环境诊断报告 ===========");
        sb.AppendLine($"当前场景: {SceneManager.GetActiveScene().name}");

        // 1. 检查环境光模式
        sb.AppendLine($"Ambient Mode (环境光模式): {RenderSettings.ambientMode}");

        // 2. 检查环境光颜色 (如果是 Flat 或 Trilight 模式)
        sb.AppendLine($"Ambient Light Color (环境光颜色): {RenderSettings.ambientLight} (RGB: {RenderSettings.ambientLight.r}, {RenderSettings.ambientLight.g}, {RenderSettings.ambientLight.b})");
        sb.AppendLine($"Ambient Sky Color: {RenderSettings.ambientSkyColor}");

        // 3. 检查天空盒
        sb.AppendLine($"Skybox Material: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "NULL (丢失!)")}");

        // 4. 检查主光源
        sb.AppendLine($"Sun Source: {(RenderSettings.sun != null ? RenderSettings.sun.name : "NULL")}");

        // 5. 检查光照贴图状态
        sb.AppendLine($"Lightmaps Mode: {LightmapSettings.lightmapsMode}");
        sb.AppendLine($"Lightmaps Count: {LightmapSettings.lightmaps.Length}");

        // 6. 检查是否开启了自动生成 (这是万恶之源)
        // 注意：这个API在运行时可能不准确，主要看上面的颜色

        sb.AppendLine("========================================");

        // 输出如果是黑色，那是大问题
        if (RenderSettings.ambientLight.r == 0 && RenderSettings.ambientLight.g == 0 && RenderSettings.ambientLight.b == 0 && RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Skybox)
        {
            Debug.LogError(sb.ToString() + "\n❌ 严重警告：环境光为全黑！这就是雪人变暗的原因！");
        }
        else if (RenderSettings.skybox == null)
        {
            Debug.LogError(sb.ToString() + "\n❌ 严重警告：天空盒材质丢失！");
        }
        else
        {
            Debug.Log(sb.ToString() + "\n✅ 光照数据看起来正常。");
        }
    }
}