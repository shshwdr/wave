using UnityEngine;

/// <summary>
/// 玩家管理器 - 管理玩家血量和交换次数
/// </summary>
public class PlayerManager : Singleton<PlayerManager>
{
    [Header("玩家属性")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxSwapCount = 2; // 每次战斗的交换次数

    private int currentHealth;
    private int currentSwapCount;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentSwapCount => currentSwapCount;
    public int MaxSwapCount => maxSwapCount;
    public bool IsDead => currentHealth <= 0;

    /// <summary>
    /// 初始化玩家管理器
    /// </summary>
    public void Init(int health = -1, int swapCount = -1)
    {
        maxHealth = health > 0 ? health : maxHealth;
        currentHealth = maxHealth;
        
        maxSwapCount = swapCount > 0 ? swapCount : maxSwapCount;
        currentSwapCount = maxSwapCount;
    }

    /// <summary>
    /// 开始新战斗时重置交换次数
    /// </summary>
    public void StartBattle()
    {
        currentSwapCount = maxSwapCount;
    }

    /// <summary>
    /// 消耗交换次数
    /// </summary>
    public bool ConsumeSwap()
    {
        if (currentSwapCount > 0)
        {
            currentSwapCount--;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查是否可以交换
    /// </summary>
    public bool CanSwap()
    {
        return currentSwapCount > 0;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"玩家受到 {damage} 伤害，当前血量: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 恢复血量
    /// </summary>
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }

    /// <summary>
    /// 设置最大血量
    /// </summary>
    public void SetMaxHealth(int health)
    {
        maxHealth = health;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
}

