using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))] // 确保一定有碰撞体
[RequireComponent(typeof(Rigidbody))] // 确保一定有刚体(用于触发事件)
public class IceCrystal : MonoBehaviour
{
    [Header("吸附设置")]
    [SerializeField] private float attractRange = 5f;
    [SerializeField] private float attractSpeed = 8f;
    [SerializeField] private float minHeight = 0.5f;

    [Header("悬浮动画")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 2f;

    private Transform player;
    private Vector3 initialPos;
    private bool canBeCollected = false;
    private bool isAttracting = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // --- 核心修复 1: 物理设置 ---
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // ⭐ 关键：设为触发器，就不会把玩家顶飞了
            col.enabled = false;  // 先禁用，等落地动画播完再开启，防止生成时误触
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;   // 关闭重力
            rb.isKinematic = true;   // ⭐ 关键：设为运动学，完全由代码控制移动，防止物理引擎干扰
        }

        // 开始掉落表现
        StartCoroutine(SpawnAnimationRoutine());
    }

    void Update()
    {
        if (!canBeCollected || isAttracting) return;

        // 1. 悬浮动画
        float newY = initialPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up * 30 * Time.deltaTime);

        // 2. 检测玩家距离 (用于触发吸附飞行动画)
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attractRange)
            {
                StartCoroutine(AttractToPlayer());
            }
        }
    }

    // --- 核心修复 2: 碰撞即吸收 ---
    private void OnTriggerEnter(Collider other)
    {
        // 只有在落地动画完成后(canBeCollected)才允许触发
        if (!canBeCollected) return;

        // 只要碰到玩家的身体，就立刻收集
        if (other.CompareTag("Player"))
        {
            CollectCrystal();
        }
    }

    private IEnumerator SpawnAnimationRoutine()
    {
        float timer = 0f;
        float duration = 0.5f;
        Vector3 startP = transform.position;

        // --- 防穿模逻辑 ---
        // 我们通过射线检测找到地面，手动计算目标点
        // 这样即使它是 Trigger，代码也会让它停在地面上，而不会穿下去
        Vector3 groundPos = startP;
        // 向上抬一点再向下射，防止生成在地面以下导致射线检测失败
        if (Physics.Raycast(startP + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 10f))
        {
            groundPos = hit.point;
        }
        else
        {
            groundPos = startP - Vector3.up * 1.5f; // 保底逻辑
        }

        Vector3 targetP = groundPos + Vector3.up * minHeight;
        Vector2 randCircle = Random.insideUnitCircle.normalized * 0.5f;
        targetP += new Vector3(randCircle.x, 0, randCircle.y);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            Vector3 currentPos = Vector3.Lerp(startP, targetP, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 1.0f; // 抛物线高度
            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetP;
        initialPos = targetP;

        canBeCollected = true;

        // 落地后开启碰撞检测，等待玩家来碰
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    private IEnumerator AttractToPlayer()
    {
        isAttracting = true;
        float startTime = Time.time;

        // 飞向玩家，直到被 OnTriggerEnter 捕获或者距离极近
        // 这里条件改为距离 > 0.1f，实际上 OnTriggerEnter 会先触发
        while (player != null && Vector3.Distance(transform.position, player.position) > 0.1f)
        {
            float t = (Time.time - startTime) * 2f;
            // 飞向玩家的中心位置（通常是胸口）
            Vector3 target = player.position + Vector3.up * 1.0f;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                (attractSpeed + t * 5f) * Time.deltaTime
            );
            yield return null;
        }

        // 保底：如果 OnTriggerEnter 没触发（比如玩家没有 Collider），这里强制收集
        CollectCrystal();
    }

    private void CollectCrystal()
    {
        if (player != null)
        {
            IceCrystalEffectSystem effectSystem = player.GetComponent<IceCrystalEffectSystem>();
            if (effectSystem != null)
            {
                effectSystem.CollectIceCrystal();
            }
        }
        Destroy(gameObject);
    }
}