using UnityEngine;

public class PetCombatSystem : MonoBehaviour
{
    [Header("战斗参数")]
    public float attackRange = 15f;
    public float attackDamage = 5f;
    public float fireRate = 2f; // 两秒一发
    public GameObject magicProjectilePrefab; // 子弹预制体
    public Transform firePoint; // 发射点 (小精灵的手或嘴)

    [Header("目标层级")]
    public LayerMask enemyLayer;

    private float nextFireTime;
    private Transform currentTarget;

    void Update()
    {
        // 1. 寻找最近的敌人
        FindTarget();

        // 2. 如果有目标，尝试攻击
        if (currentTarget != null)
        {
            // 面向敌人 (临时覆盖 Controller 的旋转)
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);

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
            float dst = Vector3.Distance(transform.position, hit.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                bestTarget = hit.transform;
            }
        }
        currentTarget = bestTarget;
    }

    void Fire()
    {
        nextFireTime = Time.time + fireRate;

        // 生成子弹
        if (magicProjectilePrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(magicProjectilePrefab, firePoint.position, Quaternion.identity);
            // 简单的子弹逻辑：飞向目标
            PetProjectile proj = bullet.AddComponent<PetProjectile>();
            proj.Initialize(currentTarget, 10f, attackDamage);

            // 触发语音：战斗语音
            if (Random.value < 0.3f) // 30% 几率喊话
            {
                PetVoiceSystem.Instance.PlayVoice("Attack");
            }
        }
    }
}

// 简单的子弹脚本 (不需要单独文件，可放在同一个文件末尾)
public class PetProjectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;

    public void Initialize(Transform t, float s, float d)
    {
        target = t;
        speed = s;
        damage = d;
        Destroy(gameObject, 5f); // 5秒后自毁
    }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            // 造成伤害
            var hp = target.GetComponent<EnemyHealth>(); // 假设这是你的敌人血量脚本
            if (hp) hp.TakeDamage(damage);

            var boss = target.GetComponent<EnemyBaofeng>();
            if (boss) boss.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}