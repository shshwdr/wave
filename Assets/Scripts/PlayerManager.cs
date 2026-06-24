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
    [SerializeField] private int hardModeSwapCount = 2; // 困难模式下的交换次数

    [Header("动画")]
    public SpriteRenderAnim anim; // 玩家动画组件

    private int currentHealth;
    private int currentShield;
    private int currentSwapCount;
    private int gold = 0; // 金币
    public int startGold = 3;
    private int tempSwapCount = 0; // 临时交换次数（可以突破上限）
    
    // Wave基础伤害（默认20）
    private float baseWaveDamage = 20f; // 永久基础伤害
    private float tempWaveDamageBonus = 0f; // 临时伤害加成（下次战斗）
    
    // Boss和敌人临时效果
    private float bossDamageReduction = 0f; // Boss初始血量减少百分比（下次战斗）
    private float enemyDamageBonus = 0f; // 敌人伤害加成百分比（下次战斗）

    // Wave技能配置：List<List<string>>，索引0,1,2,3对应红、黄、蓝、绿
    // 每个内层List存储该颜色wave的技能identifier列表（顺序=玩家拖入顺序）
    private List<List<string>> waveSkillsDict = new List<List<string>>();

    // 背包中技能的显示顺序（越晚加入越靠后）
    private List<string> backpackSkillOrder = new List<string>();

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentShield => currentShield;
    public int CurrentSwapCount => currentSwapCount;
    public int MaxSwapCount
    {
        get
        {
            // 困难模式下使用hardModeSwapCount
            int baseSwapCount = (GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode()) 
                ? hardModeSwapCount 
                : maxSwapCount;
            return baseSwapCount + tempSwapCount; // 临时交换次数可以突破上限
        }
    }
    public int Gold => gold;
    public bool IsDead => currentHealth <= 0;
    public float BaseWaveDamage => baseWaveDamage;
    public float TempWaveDamageBonus => tempWaveDamageBonus;
    public float BossDamageReduction => bossDamageReduction;
    public float EnemyDamageBonus => enemyDamageBonus;
    
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
    /// 获取背包技能列表（按玩家加入顺序）
    /// </summary>
    public List<string> GetBackpackSkills()
    {
        EnsureBackpackSkillOrderSynced();
        return new List<string>(backpackSkillOrder);
    }

    /// <summary>
    /// 将技能加入背包末尾，或插入到指定位置
    /// </summary>
    public void AddBackpackSkill(string identifier, int index = -1)
    {
        if (string.IsNullOrEmpty(identifier))
            return;

        backpackSkillOrder.Remove(identifier);
        if (index >= 0 && index <= backpackSkillOrder.Count)
            backpackSkillOrder.Insert(index, identifier);
        else
            backpackSkillOrder.Add(identifier);
    }

    public void RemoveBackpackSkill(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return;

        backpackSkillOrder.Remove(identifier);
    }

    /// <summary>
    /// 在背包内调整顺序
    /// </summary>
    public void ReorderBackpackSkill(string identifier, int newIndex)
    {
        if (string.IsNullOrEmpty(identifier))
            return;

        backpackSkillOrder.Remove(identifier);
        if (newIndex < 0 || newIndex >= backpackSkillOrder.Count)
            backpackSkillOrder.Add(identifier);
        else
            backpackSkillOrder.Insert(newIndex, identifier);
    }

    /// <summary>
    /// 在颜色区域内调整技能顺序
    /// </summary>
    public void ReorderWaveSkill(int colorIndex, string identifier, int newIndex)
    {
        if (string.IsNullOrEmpty(identifier) || colorIndex < 0 || colorIndex >= 4)
            return;

        List<string> skills = GetWaveSkills(colorIndex);
        skills.Remove(identifier);
        if (newIndex < 0 || newIndex >= skills.Count)
            skills.Add(identifier);
        else
            skills.Insert(newIndex, identifier);

        SetWaveSkills(colorIndex, skills);
    }

    /// <summary>
    /// 同步背包列表：移除已分配到颜色区域的技能，补上遗漏的未分配技能
    /// </summary>
    public void EnsureBackpackSkillOrderSynced()
    {
        if (SkillManager.Instance == null)
            return;

        HashSet<string> assignedSkills = new HashSet<string>();
        for (int i = 0; i < 4; i++)
        {
            foreach (var skillId in GetWaveSkills(i))
                assignedSkills.Add(skillId);
        }

        backpackSkillOrder.RemoveAll(id =>
            string.IsNullOrEmpty(id)
            || !SkillManager.Instance.HasSkill(id)
            || assignedSkills.Contains(id));

        if (CSVLoader.Instance == null || CSVLoader.Instance.cardInfoMap == null)
            return;

        foreach (var kvp in CSVLoader.Instance.cardInfoMap)
        {
            string id = kvp.Key;
            if (SkillManager.Instance.HasSkill(id)
                && !assignedSkills.Contains(id)
                && !backpackSkillOrder.Contains(id))
            {
                backpackSkillOrder.Add(id);
            }
        }
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
        currentShield = 0;
        
        // 根据困难模式选择基础交换次数
        int baseSwapCount = (GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode()) 
            ? hardModeSwapCount 
            : maxSwapCount;
        maxSwapCount = swapCount > 0 ? swapCount : baseSwapCount;
        currentSwapCount = MaxSwapCount;
        
        // 初始化waveSkillsDict（如果还未初始化）
        InitializeWaveSkillsDict();
        
        AddGold(startGold);
        
        // 初始化动画
        InitAnimation();
    }
    
    /// <summary>
    /// 初始化玩家动画
    /// </summary>
    private void InitAnimation()
    {
        if (anim == null)
        {
            // 尝试从子对象或自身获取 SpriteRenderAnim 组件
            anim = GetComponentInChildren<SpriteRenderAnim>();
            if (anim == null)
            {
                anim = GetComponent<SpriteRenderAnim>();
            }
        }
        
        // 检查是否有 Player 动画文件夹
        if (SpriteRenderAnim.HasAnimationFolder("player") && anim != null)
        {
            // 设置 identifier 为 "Player"（首字母大写）
            anim.SetIdentifier("Player");
            anim.PlayIdle();
        }
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
        // 注意：不重置currentSwapCount，exchange在战斗结束时恢复
        tempSwapCount = 0; // 重置临时交换次数
        // 注意：不重置tempWaveDamageBonus，它会在战斗结束后清除
    }
    
    /// <summary>
    /// 战斗结束后清除临时伤害加成并恢复exchange
    /// </summary>
    public void EndBattle()
    {
        tempWaveDamageBonus = 0f;
        bossDamageReduction = 0f;
        enemyDamageBonus = 0f;
        ClearShield();
        // 战斗结束时恢复exchange到最大值（根据困难模式）
        currentSwapCount = MaxSwapCount;
    }
    
    /// <summary>
    /// 添加临时伤害加成（下次战斗）
    /// </summary>
    public void AddTempWaveDamageBonus(float percent)
    {
        tempWaveDamageBonus += percent;
    }
    
    /// <summary>
    /// 添加永久伤害加成
    /// </summary>
    public void AddPermanentWaveDamageBonus(float percent)
    {
        baseWaveDamage = baseWaveDamage * (1f + percent / 100f);
    }
    
    /// <summary>
    /// 设置Boss初始血量减少百分比（下次战斗）
    /// </summary>
    public void SetBossDamageReduction(float percent)
    {
        bossDamageReduction = percent;
    }
    
    /// <summary>
    /// 添加敌人伤害加成百分比（下次战斗）
    /// </summary>
    public void AddEnemyDamageBonus(float percent)
    {
        enemyDamageBonus += percent;
    }
    
    /// <summary>
    /// 获取当前战斗的基础伤害（包含临时加成）
    /// </summary>
    public float GetCurrentBattleBaseDamage()
    {
        return baseWaveDamage * (1f + tempWaveDamageBonus / 100f);
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
        if (damage <= 0)
            return;

        int remaining = damage;
        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining <= 0)
        {
            Debug.Log($"护盾抵挡 {damage} 伤害，剩余护盾: {currentShield}");
            return;
        }

        currentHealth -= remaining;
        currentHealth = Mathf.Max(0, currentHealth);
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/PlayerStatus/sfx_player_hurt");

        // 播放受伤动画
        TryPlayHurtAnimation();

        Debug.Log($"玩家受到 {remaining} 伤害（护盾抵消 {damage - remaining}），当前血量: {currentHealth}/{maxHealth}，护盾: {currentShield}");
    }

    /// <summary>
    /// 获得护盾（可叠加，战斗结束时清零）
    /// </summary>
    public void AddShield(int amount)
    {
        if (amount <= 0)
            return;

        currentShield += amount;
        Debug.Log($"获得 {amount} 护盾，当前护盾: {currentShield}");
    }

    /// <summary>
    /// 清空护盾（战斗结束时调用）
    /// </summary>
    public void ClearShield()
    {
        currentShield = 0;
    }

    /// <summary>
    /// 玩家回合开始时护盾减半（向下取整）
    /// </summary>
    public void DecayShieldAtTurnStart()
    {
        if (currentShield <= 0)
            return;

        if (RuneManager.Instance != null && RuneManager.Instance.ShouldKeepShieldAtTurnStart())
        {
            Debug.Log("符文 keepShieldValue：回合开始护盾不衰减");
            return;
        }

        currentShield = currentShield / 2;
        Debug.Log($"回合开始护盾衰减，当前护盾: {currentShield}");
    }
    
    /// <summary>
    /// 尝试播放受伤动画
    /// </summary>
    private void TryPlayHurtAnimation()
    {
        if (SpriteRenderAnim.HasAnimationFolder("player") && anim != null)
        {
            anim.SetIdentifier("Player");
            anim.PlayHurt();
        }
    }

    public void CheatKill()
    {
        currentHealth = 0;
        Debug.Log("Cheat: 玩家已死亡");
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
        
        // 如果有实际回血，创建回血效果
        if (actualHeal > 0 && transform != null)
        {
            HealEffect.CreateHealEffect(transform);
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/PlayerStatus/sfx_cure_effect");
        }
        
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
            
            TileColor overhealColor = TileColor.Red; // 默认红色
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
                        // 确定是哪个颜色的技能
                        if (skill.color != null)
                        {
                            string colorStr = skill.color.ToLower();
                            if (colorStr == "red") overhealColor = TileColor.Red;
                            else if (colorStr == "yellow") overhealColor = TileColor.Yellow;
                            else if (colorStr == "blue") overhealColor = TileColor.Blue;
                            else if (colorStr == "green") overhealColor = TileColor.Green;
                        }
                        ApplyOverhealDamage(damage, overhealColor);
                    }
                    break; // 只应用一次
                }
            }
        }
    }
    
    /// <summary>
    /// 应用溢出治疗造成的伤害
    /// </summary>
    private void ApplyOverhealDamage(int damage, TileColor color)
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
        
        // 记录统计
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RecordNonWaveDamage(color, damage);
        }
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
    /// 增加最大血量（保持当前血量比例）
    /// </summary>
    public void AddMaxHealth(int amount)
    {
        if (maxHealth <= 0)
            return;
            
        float healthPercent = (float)currentHealth / maxHealth;
        maxHealth += amount;
        maxHealth = Mathf.Max(1, maxHealth); // 确保至少为1
        
        // 按比例更新当前血量
        int diff = (int)(maxHealth * healthPercent - currentHealth);
        Heal(diff);
    }
    
    /// <summary>
    /// 设置当前血量（用于重试关卡）
    /// </summary>
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
    
    /// <summary>
    /// 设置金币（用于重试关卡）
    /// </summary>
    public void SetGold(int amount)
    {
        gold = Mathf.Max(0, amount);
    }
}

