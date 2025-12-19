using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogRenderFeature : ScriptableRendererFeature
{
    public Shader m_shader;
    public Color m_FogColor;
    public float m_FogStart;
    public float m_FogEnd;
    public float m_NoiseCellSize;
    public float m_NoiseRoughness;
    public float m_NoisePersistance;
    public Vector3 m_NoiseSpeed;
    public float m_NoiseScale;

    Material m_Material;

    FogPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_RenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);

            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle, m_FogColor, m_NoiseCellSize, m_NoiseRoughness, m_NoisePersistance, m_NoiseSpeed, m_NoiseScale, m_FogStart, m_FogEnd);
        }
    }


    public override void Create()
    {
        if (m_shader != null)
            m_Material = new Material(m_shader);

        m_RenderPass = new FogPass(m_Material);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

}