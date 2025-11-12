using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private int gold = 0; // 金币
    public int startGold = 3;
    private int tempSwapCount = 0; // 临时交换次数（可以突破上限）

    // Wave技能配置：List<List<string>>，索引0,1,2,3对应红、黄、蓝、绿
    // 每个内层List存储该颜色wave的技能identifier列表
    private List<List<string>> waveSkillsDict = new List<List<string>>();

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentSwapCount => currentSwapCount;
    public int MaxSwapCount => maxSwapCount + tempSwapCount; // 临时交换次数可以突破上限
    public int Gold => gold;
    public bool IsDead => currentHealth <= 0;
    
    /// <summary>
    /// 获取wave技能配置字典
    /// </summary>
    public List<List<string>> WaveSkillsDict => waveSkillsDict;
    
    /// <summary>
    /// 获取指定颜色的技能列表（索引0=红，1=黄，2=蓝，3=绿）
    /// </summary>
    public List<string> GetWaveSkills(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < waveSkillsDict.Count)
        {
            return waveSkillsDict[colorIndex];
        }
        return new List<string>();
    }
    
    /// <summary>
    /// 设置指定颜色的技能列表
    /// </summary>
    public void SetWaveSkills(int colorIndex, List<string> skills)
    {
        // 确保waveSkillsDict有足够的元素
        while (waveSkillsDict.Count <= colorIndex)
        {
            waveSkillsDict.Add(new List<string>());
        }
        waveSkillsDict[colorIndex] = new List<string>(skills);
    }
    
    /// <summary>
    /// 获取指定颜色的slot数量（当前技能数量）
    /// </summary>
    public int GetWaveSlotCount(int colorIndex)
    {
        if (colorIndex >= 0 && colorIndex < waveSkillsDict.Count)
        {
            return waveSkillsDict[colorIndex].Count;
        }
        return 0;
    }
    
    /// <summary>
    /// 获取指定颜色的最大slot数量（初始为4，之后可能通过技能改变）
    /// </summary>
    public int GetWaveMaxSlotCount(int colorIndex)
    {
        // 初始为4，之后可以通过技能改变
        int baseMaxSlots = 4;
        // TODO: 检查是否有增加slot数量的技能
        return baseMaxSlots;
    }

    /// <summary>
    /// 初始化玩家管理器
    /// </summary>
    public void Init(int health = -1, int swapCount = -1)
    {
        maxHealth = health > 0 ? health : maxHealth;
        currentHealth = maxHealth;
        
        maxSwapCount = swapCount > 0 ? swapCount : maxSwapCount;
        currentSwapCount = maxSwapCount;
        
        // 初始化waveSkillsDict（如果还未初始化）
        InitializeWaveSkillsDict();
        
        AddGold(startGold);
    }
    
    /// <summary>
    /// 初始化wave技能配置字典
    /// </summary>
    private void InitializeWaveSkillsDict()
    {
        // 如果已经初始化过，不重复初始化
        if (waveSkillsDict.Count > 0)
        {
            return;
        }
        
        // 初始化四个颜色的列表（红、黄、蓝、绿）
        for (int i = 0; i < 4; i++)
        {
            waveSkillsDict.Add(new List<string>());
        }
        
        // 如果有isStart技能，按原color字段自动分配
        if (SkillManager.Instance != null && CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
            {
                if (skillInfo.isStart && SkillManager.Instance.HasSkill(skillInfo.identifier))
                {
                    // 根据color字段分配到对应颜色
                    int colorIndex = GetColorIndex(skillInfo.color);
                    if (colorIndex >= 0 && colorIndex < 4)
                    {
                        // 检查是否超过最大slot数量
                        int maxSlots = GetWaveMaxSlotCount(colorIndex);
                        if (waveSkillsDict[colorIndex].Count < maxSlots)
                        {
                            waveSkillsDict[colorIndex].Add(skillInfo.identifier);
                            Debug.Log($"自动分配isStart技能: {skillInfo.identifier} -> 颜色索引 {colorIndex} ({skillInfo.color})");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 将颜色字符串转换为索引（0=红，1=黄，2=蓝，3=绿）
    /// </summary>
    private int GetColorIndex(string color)
    {
        switch (color.ToLower())
        {
            case "red": return 0;
            case "yellow": return 1;
            case "blue": return 2;
            case "green": return 3;
            default: return -1;
        }
    }

    /// <summary>
    /// 开始新战斗时重置交换次数
    /// </summary>
    public void StartBattle()
    {
        currentSwapCount = maxSwapCount;
        tempSwapCount = 0; // 重置临时交换次数
    }
    
    /// <summary>
    /// 添加金币
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
    }
    
    /// <summary>
    /// 消耗金币
    /// </summary>
    public bool ConsumeGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 添加临时交换次数（增加当前可用的交换次数，而非最大值）
    /// </summary>
    public void AddTempSwapCount(int amount)
    {
        currentSwapCount += amount;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            TakeDamage(40);
        }
    }

    /// <summary>
    /// 恢复血量
    /// </summary>
    public void Heal(int amount)
    {
        int oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        int actualHeal = currentHealth - oldHealth;
        
        // 检查是否有summonHeal技能，如果有则给所有ally也回血
        if (SkillManager.Instance != null)
        {
            bool hasSummonHeal = false;
            // 检查所有颜色的wave技能
            for (int i = 0; i < 4; i++)
            {
                List<string> skillIdentifiers = GetWaveSkills(i);
                foreach (var identifier in skillIdentifiers)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                        if (skillInfo != null && skillInfo.effect == "summonHeal")
                        {
                            hasSummonHeal = true;
                            break;
                        }
                    }
                }
                if (hasSummonHeal) break;
            }
            
            if (hasSummonHeal)
            {
                // 给所有ally回血
                AllyManager allyManager = UnityEngine.Object.FindObjectOfType<AllyManager>();
                if (allyManager != null)
                {
                    foreach (var ally in allyManager.ActiveAllies)
                    {
                        if (ally != null && !ally.IsDead)
                        {
                            ally.Heal(amount);
                        }
                    }
                }
            }
        }
        
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
        DamageNumber.CreateDamageNumber(damage, targetEnemy.transform.position, false);
    }

    /// <summary>
    /// 设置最大血量
    /// </summary>
    public void SetMaxHealth(int health)
    {
        maxHealth = health;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 设置当前血量（用于重试关卡）
    /// </summary>
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
}

