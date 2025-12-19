using UnityEngine;
using System.Collections;

public class PetCombatSystem : MonoBehaviour
{
    [Header("基础战斗参数")]
    public float attackRange = 15f;
    public float baseDamage = 5f;
    public float baseFireRate = 2f; // 基础射击间隔 (秒)
    public GameObject magicProjectilePrefab;
    public Transform firePoint;

    [Header("散射与追踪设置")]
    [Tooltip("多发子弹时的扇形角度")]
    public float spreadAngle = 30f;
    [Tooltip("子弹飞行速度")]
    public float projectileSpeed = 12f;
    [Tooltip("子弹追踪转向速度 (越大越灵敏，越小弧度越大)")]
    public float turnSpeed = 8f;

    [Header("目标层级")]
    public LayerMask enemyLayer;

    private float nextFireTime;
    private Transform currentTarget;

    void Update()
    {
        FindTarget();

        if (currentTarget != null)
        {
            // 小精灵面向敌人
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            if (Time.time >= nextFireTime)
            {
                Fire();
            }
        }
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        float minDst = float.MaxValue;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            // 确保只锁定敌人
            if (!hit.CompareTag("Enemy")) continue;

            // 确保有视线 (不隔墙输出)
            Vector3 dir = (hit.transform.position - transform.position).normalized;
            float dst = Vector3.Distance(transform.position, hit.transform.position);

            // 简单视线检查 (可选，如果想穿墙打可以去掉)
            if (!Physics.Raycast(transform.position, dir, dst, LayerMask.GetMask("Default", "Ground")))
            {
                if (dst < minDst)
                {
                    minDst = dst;
                    bestTarget = hit.transform;
                }
            }
        }
        currentTarget = bestTarget;
    }

    void Fire()
    {
        // 1. 获取升级数据
        int count = 1;
        float dmgMult = 1f;
        float rateMult = 1f;

        if (PlayerUpgradeHandler.Instance != null)
        {
            count = PlayerUpgradeHandler.Instance.petProjectileCount;
            dmgMult = PlayerUpgradeHandler.Instance.petDamageMultiplier;
            rateMult = PlayerUpgradeHandler.Instance.petFireRateMultiplier;
        }

        // 计算间隔
        nextFireTime = Time.time + (baseFireRate / rateMult);

        if (magicProjectilePrefab != null && firePoint != null)
        {
            // 2. 散射逻辑
            if (count == 1)
            {
                // 单发：直接朝向敌人发射
                SpawnHomingProjectile(firePoint.rotation, baseDamage * dmgMult);
            }
            else
            {
                // 多发：计算扇形
                // 例如：3发，角度30 -> -15, 0, 15
                float startAngle = -((count - 1) * spreadAngle) / 2f;

                for (int i = 0; i < count; i++)
                {
                    float currentAngle = startAngle + (i * spreadAngle);
                    // 初始朝向带有偏移
                    Quaternion rotation = firePoint.rotation * Quaternion.Euler(0, currentAngle, 0);

                    SpawnHomingProjectile(rotation, baseDamage * dmgMult);
                }
            }

            // 语音
            if (Random.value < 0.2f && PetVoiceSystem.Instance != null)
            {
                PetVoiceSystem.Instance.PlayVoice("Attack");
            }
        }
    }

    void SpawnHomingProjectile(Quaternion rotation, float damage)
    {
        GameObject bullet = Instantiate(magicProjectilePrefab, firePoint.position, rotation);

        PetProjectile proj = bullet.GetComponent<PetProjectile>();
        if (proj == null) proj = bullet.AddComponent<PetProjectile>();

        // 初始化追踪导弹
        // 传入 currentTarget，以及速度和转向参数
        proj.Initialize(currentTarget, projectileSpeed, damage, turnSpeed);
    }
}

// ⭐ 修改后的子弹脚本：追踪导弹逻辑
public class PetProjectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private float turnSpeed;
    private Vector3 targetOffset; // 随机偏移，防止重叠

    public void Initialize(Transform t, float s, float d, float turn)
    {
        target = t;
        speed = s;
        damage = d;
        turnSpeed = turn;

        // 给每个子弹一个微小的随机目标偏移，让它们打在敌人身上不同的点
        targetOffset = Random.insideUnitSphere * 0.5f;

        Destroy(gameObject, 5f); // 5秒后自毁
    }

    void Update()
    {
        // 1. 始终向前飞
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 2. 追踪逻辑 (如果有目标)
        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 1.0f + targetOffset; // 瞄准中心偏上 + 随机偏移
            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            // 平滑旋转向目标
            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 3. 碰撞检测 (射线检测防穿模)
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, speed * Time.deltaTime * 1.2f))
        {
            CheckHit(hit.collider.gameObject);
        }
    }

    // Trigger检测作为保底
    void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    void CheckHit(GameObject hitObj)
    {
        // 只处理敌人
        if (hitObj.CompareTag("Enemy"))
        {
            var hp = hitObj.GetComponent<EnemyHealth>();
            if (hp) hp.TakeDamage(damage);

            var boss = hitObj.GetComponent<EnemyBaofeng>();
            if (boss) boss.TakeDamage(damage);

            // 击中特效(如果有)
            // Instantiate(hitEffect, transform.position, ...);

            Destroy(gameObject);
        }
        // 如果撞墙也销毁
        else if (!hitObj.CompareTag("Player") && !hitObj.CompareTag("Bullet"))
        {
            Destroy(gameObject);
        }
    }
}