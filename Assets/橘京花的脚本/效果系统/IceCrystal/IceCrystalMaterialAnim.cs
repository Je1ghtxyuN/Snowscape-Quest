using UnityEngine;

public class IceCrystalMaterialAnim : MonoBehaviour
{
    [SerializeField] private float glowSpeed = 2f;
    private Material material;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 使用 material 而不是 sharedMaterial，确保实例化独立
            material = rend.material;
        }
    }

    void Update()
    {
        if (material != null)
        {
            float glowIntensity = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;
            // 确保你的 Shader 里有这个属性，如果是标准材质可能是 _EmissionColor
            if (material.HasProperty("_GlowIntensity"))
            {
                material.SetFloat("_GlowIntensity", glowIntensity);
            }
        }
    }
}