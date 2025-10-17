using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCrystalMaterialAnim : MonoBehaviour
{
    [SerializeField] private float glowSpeed = 2f;
    private Material material;
    private float glowIntensity = 0f;

    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        // 脉动发光效果
        glowIntensity = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;
        material.SetFloat("_GlowIntensity", glowIntensity);
    }
}

