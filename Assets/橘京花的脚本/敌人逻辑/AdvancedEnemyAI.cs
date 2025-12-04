using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class AdvancedEnemyAI : MonoBehaviour
{
    [Header("移动速度设置")]
    public float patrolSpeed = 2f; // 巡逻慢一点
    public float chaseSpeed = 5f;  // ⭐ 新增：追逐快一点
    public float rotationSpeed = 15f; // 转向速度

    [Header("巡逻设置 (随机游荡)")]
    public float patrolRadius = 10f;
    public float waitTime = 2f;

    [Header("防卡死设置")]
    public float stuckCheckInterval = 0.5f;
    public float minMoveDistance = 0.1f;
    public float changeDirectionCooldown = 1.0f;

    [Header("玩家检测")]
    public float detectionRange = 10f;
    public float attackRange = 7f;
    public float losePlayerRange = 25f;
    [Tooltip("失去视线后的记忆时间")]
    public float memoryDuration = 3f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("攻击设置")]
    public GameObject snowballPrefab;
    public Transform firePoint;
    public float attackCooldown = 2f;
    public float launchAngle = 25f;

    [Header("动画")]
    public Animator animator;
    public string walkAnimParam = "isWalking";
    public string attackAnimParam = "Attack";
    public string dieAnimParam = "die";

    // 状态标志
    private bool isChasing = false;
    private bool isAttacking = false;
    public bool isDead = false;
    private bool isWaiting = false;

    // 内部变量
    private Transform player;
    private float lastAttackTime;
    private float lastSawPlayerTime;
    private Vector3 spawnPosition;
    private Vector3 currentPatrolTarget;

    // 引用
    private AdvancedGameAreaManager areaManager;
    private Rigidbody rb;
    private Collider myCollider;

    // 防卡死变量
    private Vector3 lastStuckCheckPos;
    private float nextStuckCheckTime;
    private float lastChangeDirectionTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (animator == null) animator = GetComponent<Animator>();

        areaManager = FindObjectOfType<AdvancedGameAreaManager>();

        spawnPosition = transform.position;

        lastStuckCheckPos = rb.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;

        StartCoroutine(RandomPatrolRoutine());
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 1. 核心检测逻辑
        DetectPlayerLogic();

        // 2. 攻击时停止物理移动
        if (isAttacking)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // 3. ⭐ 追逐移动逻辑 (移到了 FixedUpdate 以保证速度稳定)
        if (isChasing && player != null)
        {
            // 使用 chaseSpeed
            MoveTowardsTarget(player.position, chaseSpeed);
        }

        // 4. 防卡死 (只在巡逻时检测，追逐时不干扰)
        if (!isChasing && !isWaiting)
        {
            CheckIfStuck();
        }
    }

    void Update()
    {
        if (isDead) return;

        // 状态优先级：攻击 > 追逐
        // Update 中只处理【转向】(FaceTarget)，保证画面流畅，不处理位移

        // 1. 攻击状态
        if (isAttacking)
        {
            if (player != null) FaceTarget(player.position);
            return;
        }

        // 2. 追逐状态
        if (isChasing && player != null)
        {
            FaceTarget(player.position);
            // 注意：MoveTowardsTarget 已经移到了 FixedUpdate
        }

        // 3. 巡逻状态完全由协程控制
    }

    // --------------------------------------------------------------------------------
    // 🔍 智能检测
    // --------------------------------------------------------------------------------
    void DetectPlayerLogic()
    {
        if (isDead) return;

        if (!isChasing || player == null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
            if (hitColliders.Length > 0)
            {
                Transform potentialTarget = hitColliders[0].transform;
                if (CheckLineOfSight(potentialTarget))
                {
                    StartChasing(potentialTarget);
                }
            }
        }
        else
        {
            float dist = Vector3.Distance(transform.position, player.position);
            bool canSee = CheckLineOfSight(player);

            if (canSee) lastSawPlayerTime = Time.time;

            bool tooFar = dist > losePlayerRange;
            bool memoryExpired = !canSee && (Time.time > lastSawPlayerTime + memoryDuration);

            if (tooFar || memoryExpired)
            {
                StopChasing();
            }
            else
            {
                if (dist <= attackRange && canSee)
                {
                    if (Time.time >= lastAttackTime + attackCooldown)
                        StartCoroutine(AttackPlayer());
                }
            }
        }
    }

    void StartChasing(Transform target)
    {
        player = target;
        isChasing = true;
        isWaiting = false;
        lastSawPlayerTime = Time.time;
        // 打断巡逻协程的等待，防止逻辑冲突
        StopCoroutine("RandomPatrolRoutine");
        StartCoroutine(RandomPatrolRoutine());

        if (PetVoiceSystem.Instance != null)
        {
            PetVoiceSystem.Instance.TryPlayEnemySpottedVoice();
        }
    }

    void StopChasing()
    {
        isChasing = false;
        player = null;
        isWaiting = false;
    }

    bool CheckLineOfSight(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);
        return !Physics.Raycast(transform.position, direction, distance, obstacleLayer);
    }

    // --------------------------------------------------------------------------------
    // 🎲 随机巡逻
    // --------------------------------------------------------------------------------
    IEnumerator RandomPatrolRoutine()
    {
        WaitForFixedUpdate waitFixed = new WaitForFixedUpdate();
        SetNewRandomPatrolTarget();

        while (true)
        {
            if (isDead) yield break;

            // 只有在【完全闲置】时才执行巡逻移动
            if (!isChasing && !isAttacking)
            {
                float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                              new Vector3(currentPatrolTarget.x, 0, currentPatrolTarget.z));

                if (dist > 0.5f)
                {
                    FaceTarget(currentPatrolTarget);
                    // ⭐ 这里使用 patrolSpeed
                    MoveTowardsTarget(currentPatrolTarget, patrolSpeed);
                }
                else
                {
                    if (animator != null) animator.SetBool(walkAnimParam, false);

                    if (!isWaiting)
                    {
                        isWaiting = true;
                        float timer = 0f;
                        while (timer < waitTime && isWaiting)
                        {
                            if (isChasing || isAttacking) { isWaiting = false; break; }
                            timer += Time.deltaTime;
                            yield return null;
                        }

                        if (isWaiting)
                        {
                            isWaiting = false;
                            SetNewRandomPatrolTarget();
                        }
                    }
                }
            }
            yield return waitFixed;
        }
    }

    void SetNewRandomPatrolTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 potentialPos = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (areaManager != null)
        {
            potentialPos = areaManager.ClampPointToNearestArea(potentialPos);
        }
        currentPatrolTarget = potentialPos;
    }

    void SwitchPatrolPoint(string reason)
    {
        if (Time.time < lastChangeDirectionTime + changeDirectionCooldown) return;

        SetNewRandomPatrolTarget();
        isWaiting = false;

        lastChangeDirectionTime = Time.time;
        lastStuckCheckPos = rb.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
    }

    // --------------------------------------------------------------------------------
    // 移动与攻击
    // --------------------------------------------------------------------------------

    // ⭐ 修改：增加了 currentSpeed 参数，允许传入 patrolSpeed 或 chaseSpeed
    private void MoveTowardsTarget(Vector3 targetPos, float currentSpeed)
    {
        // 追逐时不限制边界
        if (!isChasing && areaManager != null)
        {
            targetPos = areaManager.ClampPointToNearestArea(targetPos);
        }

        if (animator != null) animator.SetBool(walkAnimParam, true);

        // 使用传入的 speed
        Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    IEnumerator AttackPlayer()
    {
        if (isDead || isAttacking || player == null) yield break;

        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null) animator.SetBool(walkAnimParam, false);
        if (animator != null) animator.Play(attackAnimParam, 0, 0f);

        yield return new WaitForSeconds(0.3f);

        if (player != null) FaceTarget(player.position);

        if (snowballPrefab != null && firePoint != null && player != null)
        {
            GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.identity);

            Collider snowballCollider = snowball.GetComponent<Collider>();
            if (snowballCollider != null && myCollider != null)
                Physics.IgnoreCollision(snowballCollider, myCollider);

            Rigidbody snowballRb = snowball.GetComponent<Rigidbody>();
            if (snowballRb != null)
            {
                Vector3 aimTarget = player.position + Vector3.up * 1.0f;
                Vector3 finalVelocity = CalculateProjectileVelocity(aimTarget, firePoint.position, launchAngle);

                snowballRb.velocity = finalVelocity;
                snowballRb.angularVelocity = Random.insideUnitSphere * 10f;
                Destroy(snowball, 5f);
            }
        }

        yield return new WaitForSeconds(attackCooldown - 0.3f);
        isAttacking = false;
    }

    Vector3 CalculateProjectileVelocity(Vector3 target, Vector3 origin, float angle)
    {
        Vector3 dir = target - origin;
        float h = dir.y;
        dir.y = 0;
        float dist = dir.magnitude;
        float a = angle * Mathf.Deg2Rad;
        dir.Normalize();

        float g = Physics.gravity.magnitude;
        float tanA = Mathf.Tan(a);
        float term = (dist * tanA) - h;

        if (term <= 0.01f) return (dir + Vector3.up).normalized * 15f;

        float v = Mathf.Sqrt((g * dist * dist) / (2 * Mathf.Cos(a) * Mathf.Cos(a) * term));
        return (dir * v * Mathf.Cos(a)) + (Vector3.up * v * Mathf.Sin(a));
    }

    // --------------------------------------------------------------------------------
    // 辅助
    // --------------------------------------------------------------------------------
    void OnCollisionStay(Collision collision)
    {
        if (isDead || isChasing || isAttacking) return;
        if (((1 << collision.gameObject.layer) & obstacleLayer) == 0) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y < 0.7f)
            {
                SwitchPatrolPoint("碰撞墙壁");
                return;
            }
        }
    }

    void CheckIfStuck()
    {
        if (Time.time >= nextStuckCheckTime)
        {
            float distanceMoved = Vector3.Distance(rb.position, lastStuckCheckPos);
            if (distanceMoved < minMoveDistance) SwitchPatrolPoint("位置卡死");
            lastStuckCheckPos = rb.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        if (Application.isPlaying) Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
        else Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }

    public void MarkAsDead() { isDead = true; }
}