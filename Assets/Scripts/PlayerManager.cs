using System.Collections.Generic;
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
        int oldHealth = currentHealth;
        currentHealth += amount;
        int actualHeal = currentHealth - oldHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        // 检查是否有overhealDoDamage技能
        if (SkillManager.Instance != null)
        {
            // 检查所有颜色的overhealDoDamage技能
            List<SkillInfo> allSkills = new List<SkillInfo>();
            foreach (var color in new[] { "red", "yellow", "blue", "green" })
            {
                allSkills.AddRange(SkillManager.Instance.GetOwnedSkillsByColor(color));
            }
            
            foreach (var skill in allSkills)
            {
                if (skill.effect == "overhealDoDamage")
                {
                    int value = SkillManager.Instance.GetSkillValue(skill.identifier);
                    
                    // 计算溢出的治疗量
                    int overheal = amount - actualHeal;
                    if (overheal > 0)
                    {
                        // 对随机敌人造成伤害
                        int damage = (int)(overheal * (value / 100f));
                        ApplyOverhealDamage(damage);
                    }
                    break; // 只应用一次
                }
            }
        }
    }
    
    /// <summary>
    /// 应用溢出治疗造成的伤害
    /// </summary>
    private void ApplyOverhealDamage(int damage)
    {
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null || damage <= 0)
            return;
            
        // 获取所有活着的敌人
        List<Enemy> aliveEnemies = new List<Enemy>();
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                aliveEnemies.Add(enemy);
            }
        }
        
        // 如果没有活着的敌人，不造成伤害
        if (aliveEnemies.Count == 0)
            return;
            
        // 随机选择一个敌人
        Enemy targetEnemy = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
        targetEnemy.TakeDamage(damage, Vector3.right, false, 0, 0f);
        
        // 显示伤害数字
        DamageNumber.CreateDamageNumber(damage, targetEnemy.transform.position + Vector3.up * 0.5f, false);
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

