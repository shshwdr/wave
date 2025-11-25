using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Boss类，继承Enemy
/// </summary>
public class Boss : Enemy
{
    [Header("Boss设置")]
    [SerializeField] private int bossHeight = 3; // boss高度（格子数）
    
    private Vector2Int startGridPos; // 起始网格位置
    private Vector3 startWorldPos; // 起始世界位置
    private bool movingDown = true; // 是否向下移动
    private int moveCount = 0; // 移动计数
    private bool isMoving = false; // 是否正在移动
    
    // Boss技能系统
    private int loopSkillIndex = 0; // loop技能的当前索引（0=blockColor, 1=healAll）
    private TileColor blockedColor = TileColor.Red; // 被block的颜色
    private int blockColorRemainingTurns = 0; // blockColor剩余回合数
    
    /// <summary>
    /// 初始化Boss
    /// </summary>
    public void InitBoss(Vector2Int gridPos, int health, EnemyInfo info, BoardManager board)
    {
        boardManager = board;
        startGridPos = gridPos;
        startWorldPos = boardManager != null ? boardManager.GridToWorldPosition(gridPos) : Vector3.zero;
        
        // 调用父类的Init方法
        Init(gridPos, health, info);
        
        // 设置初始位置
        if (boardManager != null)
        {
            transform.position = startWorldPos;
        }

        if (enemyInfo.identifier == "elite")
        {
            spriteRenderAnim.transform.localScale = new Vector3(-spriteRenderAnim.transform.localScale.x, spriteRenderAnim.transform.localScale.y);
            spriteRenderAnim.transform.Translate(0.3f,0,0);
        }
    }
    
    /// <summary>
    /// 开始Boss移动（每回合调用）
    /// </summary>
    public void StartMove()
    {
        if (isMoving || IsDead || boardManager == null)
            return;
            
        isMoving = true;
        
        // 计算目标位置（每次移动1格）
        Vector2Int targetGridPos;
        if (movingDown)
        {
            // 向下移动1格
            targetGridPos = new Vector2Int(startGridPos.x, startGridPos.y - 1);
            moveCount++;
            
            // 如果已经向下移动了bossHeight次，改为向上移动
            if (moveCount >= bossHeight)
            {
                movingDown = false;
                moveCount = 0;
            }
        }
        else
        {
            // 向上移动1格
            targetGridPos = new Vector2Int(startGridPos.x, startGridPos.y + 1);
            moveCount++;
            
            // 如果已经向上移动了bossHeight次，改为向下移动
            if (moveCount >= bossHeight)
            {
                movingDown = true;
                moveCount = 0;
            }
        }
        
        // 计算目标世界位置
        Vector3 targetWorldPos = boardManager.GridToWorldPosition(targetGridPos);
        
        // 执行移动动画（移动1格的时间，使用父类的moveSpeed）
        float moveDuration = 1f / base.moveSpeed;
        transform.DOMove(targetWorldPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;
                // 更新网格位置（用于碰撞检测）
                UpdateGridPosition(targetGridPos);
                startGridPos = targetGridPos; // 更新起始位置
            });
    }
    
    /// <summary>
    /// 更新Boss的网格位置（用于移动后更新）
    /// </summary>
    private void UpdateGridPosition(Vector2Int newPos)
    {
        // 使用反射来设置private字段gridPosition
        var field = typeof(Enemy).GetField("gridPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, newPos);
        }
    }
    
    /// <summary>
    /// 重写TakeAction方法，Boss执行技能（不执行攻击）
    /// </summary>
    public override void TakeAction()
    {
        if (IsDead)
            return;
            
        // 使用反射访问父类的技能相关字段
        var currentSkillField = typeof(Enemy).GetField("currentSkill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var skillValueField = typeof(Enemy).GetField("skillValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var skillCooldownField = typeof(Enemy).GetField("skillCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentCooldownField = typeof(Enemy).GetField("currentCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (currentSkillField == null || skillValueField == null || skillCooldownField == null || currentCooldownField == null)
            return;
            
        string currentSkill = (string)currentSkillField.GetValue(this);
        int skillValue = (int)skillValueField.GetValue(this);
        int skillCooldown = (int)skillCooldownField.GetValue(this);
        int currentCooldown = (int)currentCooldownField.GetValue(this);
        
        // 检查主动技能（冷却时间>0）
        if (skillCooldown > 0)
        {
            currentCooldown--;
            if (currentCooldown <= 0)
            {
                // 使用技能
                UseBossSkill(currentSkill, skillValue);
                currentCooldown = skillCooldown; // 重置冷却
                currentCooldownField.SetValue(this, currentCooldown);
            }
            else
            {
                currentCooldownField.SetValue(this, currentCooldown);
            }
        }
    }
    
    /// <summary>
    /// 使用Boss技能
    /// </summary>
    private void UseBossSkill(string skillName, int value)
    {
        if (string.IsNullOrEmpty(skillName))
            return;
            
        // 播放攻击动画（使用atk动画）
        TryPlayBossAtkAnimation();
        
        if (skillName == "blockColor")
        {
            UseBlockColorSkill(value);
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Elite/sfx_elite_block_tiles");
        }
        else if (skillName == "healAll")
        {
            UseHealAllSkill();
        }
        else if (skillName == "loop")
        {
            UseLoopSkill(value);
        }
    }
    
    /// <summary>
    /// 尝试播放Boss攻击动画（使用反射调用父类的TryPlayAtkAnimation）
    /// </summary>
    private void TryPlayBossAtkAnimation()
    {
        // 使用反射获取enemyInfo
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        // 使用反射调用父类的TryPlayAtkAnimation方法
        TryPlayAtkAnimation();
    }
    
    /// <summary>
    /// 使用blockColor技能：找到场面上最多的颜色，接下来的skillValue回合，玩家不能消除这个颜色
    /// </summary>
    private void UseBlockColorSkill(int turns)
    {
        if (boardManager == null)
            return;
            
        // 统计场上每种颜色的数量
        Dictionary<TileColor, int> colorCount = new Dictionary<TileColor, int>();
        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                if (tile != null)
                {
                    TileColor color = tile.Color;
                    if (colorCount.ContainsKey(color))
                    {
                        colorCount[color]++;
                    }
                    else
                    {
                        colorCount[color] = 1;
                    }
                }
            }
        }
        
        // 找到数量最多的颜色
        TileColor mostColor = TileColor.Red;
        int maxCount = 0;
        foreach (var kvp in colorCount)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostColor = kvp.Key;
            }
        }
        
        // 如果之前有被block的颜色，先恢复
        if (blockColorRemainingTurns > 0)
        {
            RestoreBlockedColor();
        }
        
        // 禁用该颜色的所有tile
        blockedColor = mostColor;
        blockColorRemainingTurns = turns;
        
        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                if (tile != null && tile.Color == mostColor)
                {
                    tile.SetDisabled(true);
                }
            }
        }
        
        Debug.Log($"Boss使用blockColor技能，禁用颜色: {mostColor}，持续{turns}回合");
    }
    
    /// <summary>
    /// 更新blockColor的剩余回合数（在玩家回合结束时调用）
    /// </summary>
    public void UpdateBlockColorTurns()
    {
        if (blockColorRemainingTurns > 0)
        {
            blockColorRemainingTurns--;
            if (blockColorRemainingTurns <= 0)
            {
                // 恢复被block的颜色
                RestoreBlockedColor();
            }
        }
    }
    
    /// <summary>
    /// 恢复被block的颜色
    /// </summary>
    private void RestoreBlockedColor()
    {
        if (boardManager == null)
            return;
            
        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                if (tile != null && tile.Color == blockedColor)
                {
                    tile.SetDisabled(false);
                }
            }
        }
        
        blockColorRemainingTurns = 0;
        Debug.Log($"Boss blockColor效果结束，恢复颜色: {blockedColor}");
    }
    
    /// <summary>
    /// 使用healAll技能：恢复场上所有怪物的已损失血量的一半
    /// </summary>
    private void UseHealAllSkill()
    {
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
            return;
            
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                int lostHealth = enemy.MaxHealth - enemy.CurrentHealth;
                int healAmount = lostHealth / 2; // 恢复已损失血量的一半
                if (healAmount > 0)
                {
                    enemy.Heal(healAmount);
                }
            }
        }
        
        Debug.Log("Boss使用healAll技能，恢复所有怪物已损失血量的一半");
    }
    
    /// <summary>
    /// 使用loop技能：循环执行blockColor和healAll
    /// </summary>
    private void UseLoopSkill(int value)
    {
        // 根据loopSkillIndex决定执行哪个技能
        if (loopSkillIndex == 0)
        {
            // 执行blockColor
            UseBlockColorSkill(value);
            loopSkillIndex = 1; // 下次执行healAll
        }
        else
        {
            // 执行healAll
            UseHealAllSkill();
            loopSkillIndex = 0; // 下次执行blockColor
        }
    }
    
    /// <summary>
    /// 重写Die方法，Boss死亡时立即结束战斗
    /// </summary>
    public override void Die()
    {
        // 先调用父类的Die方法
        base.Die();
        
        // Boss死亡时立即结束战斗（胜利）
        MainGameManager instance = UnityEngine.Object.FindObjectOfType<MainGameManager>();
        if (instance != null)
        {
            instance.CompleteLevel();
        }
    }
}

