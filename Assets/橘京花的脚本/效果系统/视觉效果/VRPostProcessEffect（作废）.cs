using UnityEngine;

[ExecuteInEditMode]
public class VRPostProcessEffect : MonoBehaviour
{
    [HideInInspector] public Material effectMaterial;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null && effectMaterial.HasProperty("_Intensity"))
        {
            float intensity = effectMaterial.GetFloat("_Intensity");
            if (intensity > 0.01f) // 只有强度足够时才应用效果
            {
                Graphics.Blit(source, destination, effectMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}