using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdvancedEnemyAI : MonoBehaviour
{
    [Header("巡逻设置")]
    public float patrolRadius = 3f;
    public float patrolSpeed = 3f;
    public float waitTime = 1f;
    public float rotationSpeed = 5f;

    [Header("玩家检测")]
    public float detectionRange = 10f;
    public float attackRange = 7f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("攻击设置")]
    public GameObject snowballPrefab;
    public Transform firePoint;
    public float attackCooldown = 2f;
    public float projectileSpeed = 10f;

    [Header("动画")]
    public Animator animator;
    public string walkAnimParam = "isWalking";
    public string attackAnimParam = "Attack";
    public string dieAnimParam = "die";

    // 巡逻点相关
    private List<Vector3> patrolPoints = new List<Vector3>();
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;

    // 状态标志
    private bool isChasing = false;
    private bool isAttacking = false;
    public bool isDead = false;

    // 引用
    private Transform player;
    private float lastAttackTime;
    private AdvancedGameAreaManager areaManager;

    void Start()
    {
        // 初始化组件
        if (animator == null)
            animator = GetComponent<Animator>();

        areaManager = FindObjectOfType<AdvancedGameAreaManager>();
        if (areaManager == null)
        {
            Debug.LogError("未找到AdvancedGameAreaManager！");
            return;
        }

        // 生成巡逻点
        GeneratePatrolPoints();

        // 开始巡逻
        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        if (isDead) return;

        DetectPlayer();

        if (isChasing && player != null)
        {
            FaceTarget(player.position);
        }
    }

    // 生成基于当前位置的巡逻点
    private void GeneratePatrolPoints()
    {
        patrolPoints.Clear();

        // 以当前位置为中心生成三角形巡逻点
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;

            Vector3 point = transform.position + new Vector3(
                Mathf.Cos(angle) * patrolRadius,
                0,
                Mathf.Sin(angle) * patrolRadius
            );

            // 确保巡逻点在游戏区域内
            if (areaManager != null)
            {
                point = areaManager.ClampPointToNearestArea(point);
            }

            patrolPoints.Add(point);
        }

        Debug.Log($"为雪人生成 {patrolPoints.Count} 个巡逻点");
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (isDead) yield break;

            if (!isChasing && !isAttacking && patrolPoints.Count > 0)
            {
                Vector3 targetPos = patrolPoints[currentPatrolIndex];

                if (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    if (animator != null)
                        animator.SetBool(walkAnimParam, true);

                    FaceTarget(targetPos);
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);
                }
                else
                {
                    if (animator != null)
                        animator.SetBool(walkAnimParam, false);

                    if (!isWaiting)
                    {
                        isWaiting = true;
                        yield return new WaitForSeconds(waitTime);
                        isWaiting = false;

                        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                    }
                }
            }
            yield return null;
        }
    }

    void DetectPlayer()
    {
        if (isDead) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

        if (hitColliders.Length > 0)
        {
            player = hitColliders[0].transform;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                isChasing = true;

                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        StartCoroutine(AttackPlayer());
                    }
                }
                else
                {
                    ChasePlayer();
                }
            }
            else
            {
                isChasing = false;
            }
        }
        else
        {
            isChasing = false;
            player = null;
        }
    }

    void ChasePlayer()
    {
        if (isDead || player == null) return;

        if (animator != null)
            animator.SetBool(walkAnimParam, true);

        // 追逐时确保不离开游戏区域
        Vector3 targetPosition = player.position;
        if (areaManager != null)
        {
            targetPosition = areaManager.ClampPointToNearestArea(targetPosition);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, patrolSpeed * Time.deltaTime);
    }

    IEnumerator AttackPlayer()
    {
        if (isDead || isAttacking || player == null) yield break;

        isAttacking = true;
        lastAttackTime = Time.time;

        Debug.Log("开始攻击");

        if (animator != null)
        {
            animator.ResetTrigger(attackAnimParam);
            animator.SetTrigger(attackAnimParam);
        }

        float animationLeadTime = 0.3f;
        yield return new WaitForSeconds(animationLeadTime);

        // 发射雪球
        if (snowballPrefab != null && firePoint != null)
        {
            try
            {
                GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.identity);
                Rigidbody rb = snowball.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 horizontalDirection = (player.position - firePoint.position).normalized;
                    horizontalDirection.y = 0;

                    float launchAngle = 20f;
                    float radians = launchAngle * Mathf.Deg2Rad;

                    Vector3 launchDirection = new Vector3(
                        horizontalDirection.x * Mathf.Cos(radians),
                        Mathf.Sin(radians),
                        horizontalDirection.z * Mathf.Cos(radians)
                    ).normalized;

                    rb.velocity = launchDirection * projectileSpeed;
                    Destroy(snowball, 5f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"雪球发射失败: {e.Message}");
            }
        }

        float remainingCooldown = Mathf.Max(0, attackCooldown - animationLeadTime);
        yield return new WaitForSeconds(remainingCooldown);

        isAttacking = false;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制巡逻点
        Gizmos.color = Color.blue;
        foreach (Vector3 point in patrolPoints)
        {
            Gizmos.DrawWireSphere(point, 0.2f);
            Gizmos.DrawLine(transform.position, point);
        }
    }

    public void MarkAsDead()
    {
        isDead = true;
    }

    // 外部调用：重新生成巡逻点（用于雪人移动后）
    public void RegeneratePatrolPoints()
    {
        GeneratePatrolPoints();
    }
}