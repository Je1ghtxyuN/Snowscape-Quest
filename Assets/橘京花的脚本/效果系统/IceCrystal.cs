using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class IceCrystal : MonoBehaviour
{
    [SerializeField] private float attractRange = 3f;
    [SerializeField] private float attractSpeed = 5f;
    [SerializeField] private float floatHeight = 1f; // 悬浮高度
    [SerializeField] private float floatSpeed = 2f; // 浮动速度
    [SerializeField] private float minHeight = 0.5f;

    private Vector3 startPos;
    private bool isFloating = true;

    private Transform player;
    private bool isAttracting = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // 确保起始位置不低于最低高度
        float adjustedY = Mathf.Max(transform.position.y, minHeight);
        startPos = new Vector3(transform.position.x, adjustedY, transform.position.z);
        transform.position = startPos;

        // 禁用物理特性
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = false;
    }

    void Update()
    {
        // 悬浮动画
        if (isFloating)
        {
            FloatAnimation();
        }

        // 检测玩家距离，自动吸引
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attractRange && !isAttracting)
        {
            isAttracting = true;
            StartCoroutine(AttractToPlayer());
        }
    }

    private void FloatAnimation()
    {
        float oscillation = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        float newY = Mathf.Max(startPos.y + oscillation, minHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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