using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("巡逻设置")]
    public Transform[] patrolPoints; // 巡逻点数组
    public float patrolSpeed = 3f; // 巡逻移动速度
    public float waitTime = 1f; // 到达巡逻点后的等待时间
    public float rotationSpeed = 5f; // 转向速度

    [Header("玩家检测")]
    public float detectionRange = 10f; // 检测玩家的范围
    public float attackRange = 7f; // 攻击玩家的范围
    public LayerMask playerLayer; // 玩家所在层
    public LayerMask obstacleLayer; // 障碍物层(用于视线检测)

    [Header("攻击设置")]
    public GameObject snowballPrefab; // 雪球预制体
    public Transform firePoint; // 发射点
    public float attackCooldown = 2f; // 攻击冷却时间
    public float projectileSpeed = 10f; // 雪球速度

    [Header("动画")]
    public Animator animator; // 动画控制器
    public string walkAnimParam = "isWalking"; // 行走动画参数
    public string attackAnimParam = "Attack"; // 攻击动画触发器

    private int currentPatrolIndex = 0; // 当前巡逻点索引
    private bool isWaiting = false; // 是否在等待
    private bool isChasing = false; // 是否在追逐玩家
    private bool isAttacking = false; // 是否在攻击
    private Transform player; // 玩家引用
    private float lastAttackTime; // 上次攻击时间

    void Start()
    {
        // 如果没有指定动画控制器，尝试获取
        if (animator == null)
            animator = GetComponent<Animator>();

        // 如果没有巡逻点，使用自身位置作为唯一巡逻点
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = new Transform[1];
            patrolPoints[0] = new GameObject("PatrolPoint").transform;
            patrolPoints[0].position = transform.position;
        }

        // 开始巡逻
        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        // 检测玩家
        DetectPlayer();

        // 如果正在追逐玩家，转向玩家
        if (isChasing && player != null)
        {
            FaceTarget(player.position);
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // 如果不在追逐或攻击状态，执行巡逻
            if (!isChasing && !isAttacking)
            {
                // 移动到当前巡逻点
                Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
                if (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    // 设置行走动画
                    if (animator != null)
                        animator.SetBool(walkAnimParam, true);

                    // 移动和转向
                    FaceTarget(targetPos);
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);
                }
                else
                {
                    // 到达巡逻点，等待一段时间
                    if (animator != null)
                        animator.SetBool(walkAnimParam, false);

                    if (!isWaiting)
                    {
                        isWaiting = true;
                        yield return new WaitForSeconds(waitTime);
                        isWaiting = false;

                        // 切换到下一个巡逻点
                        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    }
                }
            }
            yield return null;
        }
    }

    void DetectPlayer()
    {
        // 检测范围内的玩家
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

        if (hitColliders.Length > 0)
        {
            // 假设场景中只有一个玩家
            player = hitColliders[0].transform;

            // 检查是否有视线(没有障碍物阻挡)
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                // 玩家在检测范围内且可见
                isChasing = true;

                // 如果玩家在攻击范围内，尝试攻击
                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        StartCoroutine(AttackPlayer());
                    }
                }
                else
                {
                    // 追逐玩家
                    ChasePlayer();
                }
            }
            else
            {
                // 玩家被障碍物阻挡，返回巡逻
                isChasing = false;
            }
        }
        else
        {
            // 没有检测到玩家，返回巡逻
            isChasing = false;
            player = null;
        }
    }

    void ChasePlayer()
    {
        if (player != null)
        {
            // 设置行走动画
            if (animator != null)
                animator.SetBool(walkAnimParam, true);

            // 向玩家移动
            transform.position = Vector3.MoveTowards(transform.position, player.position, patrolSpeed * Time.deltaTime);
        }
    }

    IEnumerator AttackPlayer()
    {
        if (isAttacking || player == null)
        {
            Debug.Log($"攻击被阻止 - isAttacking:{isAttacking} player:{player != null}");
            yield break;
        }

        isAttacking = true;
        lastAttackTime = Time.time;

        Debug.Log($"开始攻击 - 时间:{Time.time}");

        // 触发攻击动画
        Debug.Log("重置并触发Attack触发器");
        animator.ResetTrigger(attackAnimParam); // 先重置
        animator.SetTrigger(attackAnimParam);  // 再触发
   

        // 等待动画前摇(根据实际动画调整)
        float animationLeadTime = 0.3f;
        yield return new WaitForSeconds(animationLeadTime);
        Debug.Log($"发射雪球 - 时间:{Time.time}");

        // 发射雪球
        if (snowballPrefab != null && firePoint != null)
        {
            try
            {
                GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.identity);
                Debug.Log($"雪球实例化成功 {snowball.name}");

                Rigidbody rb = snowball.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 计算基础方向（水平方向）
                    Vector3 horizontalDirection = (player.position - firePoint.position).normalized;
                    horizontalDirection.y = 0; // 保持水平

                    // 添加向上角度（建议15-30度）
                    float launchAngle = 20f; // 角度可调
                    float radians = launchAngle * Mathf.Deg2Rad;

                    // 最终发射方向（带抛物线）
                    Vector3 launchDirection = new Vector3(
                        horizontalDirection.x * Mathf.Cos(radians),
                        Mathf.Sin(radians),
                        horizontalDirection.z * Mathf.Cos(radians)
                    ).normalized;

                    // 可视化调试
                    Debug.DrawRay(firePoint.position, launchDirection * 5f, Color.cyan, 2f);

                    rb.velocity = launchDirection * projectileSpeed;

                    // 添加旋转效果
                    rb.angularVelocity = new Vector3(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f)
                    );

                    Destroy(snowball, 5f);
                }
                else
                {
                    Debug.LogError("雪球缺少Rigidbody组件");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"雪球实例化失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"雪球预制体:{snowballPrefab != null} 发射点:{firePoint != null}");
        }

        // 等待剩余冷却时间
        float remainingCooldown = Mathf.Max(0, attackCooldown - animationLeadTime);
        Debug.Log($"等待冷却时间: {remainingCooldown}秒");
        yield return new WaitForSeconds(remainingCooldown);

        isAttacking = false;
        Debug.Log($"攻击结束 - 时间:{Time.time} 下次可攻击时间:{lastAttackTime + attackCooldown}");
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // 保持水平旋转
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制检测范围和攻击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}