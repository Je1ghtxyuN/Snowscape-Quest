using UnityEngine;

public class Health
{
    //血量成员变量
    private float currentHealth;
    private float maxHealth;

    //事件：血量变化时触发
    public event System.Action OnHealthChanged;

    //事件：死亡时触发
    public event System.Action OnDeath;

    //构造函数
    public Health(float maxHealth)
    {
        this.maxHealth = maxHealth;
        currentHealth = maxHealth;
    }

    //减少血量
    public void TakeDamage(float damageAmount)
    {
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0f);

        //触发血量变化事件
        OnHealthChanged?.Invoke();

        //检测死亡
        if (currentHealth <= 0f)
        {
            OnDeath?.Invoke();
        }
    }

    //增加血量
    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

        //触发血量变化事件
        OnHealthChanged?.Invoke();
    }

    //获取当前血量
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    //设置血量
    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
    }

    //检测是否死亡（血量≤0）
    public bool IsDead()
    {
        return currentHealth <= 0f;
    }

    //获取血量百分比（用于UI血条）
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    //增加血量上限
    public void IncreaseMaxHealth(float increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth += increaseAmount;
    }

    //事件触发方法
    public void ForceUpdate()
    {
        OnHealthChanged?.Invoke();
    }
}