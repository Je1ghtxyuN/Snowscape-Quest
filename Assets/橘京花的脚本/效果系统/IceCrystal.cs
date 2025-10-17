using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class IceCrystal : MonoBehaviour
{
    [SerializeField] private float attractRange = 3f;
    [SerializeField] private float attractSpeed = 5f;

    private Transform player;
    private bool isAttracting = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // 检测玩家距离，自动吸引
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attractRange && !isAttracting)
        {
            isAttracting = true;
            StartCoroutine(AttractToPlayer());
        }
    }

    private IEnumerator AttractToPlayer()
    {
        while (Vector3.Distance(transform.position, player.position) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );
            yield return null;
        }

        // 收集冰晶
        CollectCrystal();
    }

    private void CollectCrystal()
    {
        IceCrystalEffectSystem effectSystem = player.GetComponent<IceCrystalEffectSystem>();
        if (effectSystem != null)
        {
            effectSystem.CollectIceCrystal();
        }

        Destroy(gameObject);
    }
}