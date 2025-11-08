using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 波浪攻击系统
/// </summary>
public class Wave : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float range = 0.5f;

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D waveCollider;

    private List<Enemy> hitEnemies = new List<Enemy>();
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float travelDistance = 10f; // 向右飞行的距离
    private float waveDuration = 0f; // 波浪移动持续时间
    private TileColor waveColor; // 波浪颜色
    private int penetrateCount = 0; // 穿透次数（用于wavePenetrate技能）
    private bool buffNextDamage = false; // 下一个波浪伤害加成（用于buffNextDamage技能）
    private bool hasHitEnemyBack = false; // 是否有击退技能
    private int knockbackTiles = 0; // 击退格子数
    private bool hasHealWhenHit = false; // 是否有击中回血技能
    private int healAmount = 0; // 回血量
    private bool hasDamageBottom = false; // 是否有damageBottom技能
    private bool damageBottomTriggered = false; // damageBottom是否已触发
    private Vector2Int spawnGridPos; // 波浪生成的网格位置
    private BoardManager boardManager; // 棋盘管理器
    private int waveGroupId = 0; // 波浪组ID（用于damageBottom）
    private bool isFirstWave = false; // 是否是第一个wave（用于damageBottom）
    private bool hasDamageBottomSkill = false; // 是否有damageBottom技能（外部传入）
    private float damageMultiplier = 1f; // 伤害倍数（来自buffNextDamage）
    private HashSet<Vector2Int> clearedTiles = new HashSet<Vector2Int>(); // 已清除fog/dirt的tile
    private int hitEnemyCount = 0; // 已击中的敌人数量（用于damageIncreaseWhenHitMore）
    private bool hasDamageIncreaseWhenHitMore = false; // 是否有damageIncreaseWhenHitMore技能
    private int damageIncreaseWhenHitMoreValue = 0; // damageIncreaseWhenHitMore的值
    private bool hasAoeAttack = false; // 是否有aoeAttack技能
    private int aoeAttackValue = 0; // aoeAttack的值

    public float Duration => waveDuration; // 获取波浪持续时间
    public TileColor WaveColor => waveColor; // 获取波浪颜色

    private void Awake()
    {
        if (waveCollider == null)
            waveCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 确保Collider2D设置为Trigger
        if (waveCollider == null)
        {
            waveCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        if (waveCollider != null)
        {
            waveCollider.isTrigger = true;
        }
        
        // 确保有Rigidbody2D用于物理碰撞（Trigger需要Rigidbody2D才能触发OnTriggerEnter2D）
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // 设置为运动学
            rb.gravityScale = 0; // 不受重力影响
        }
        else
        {
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    /// <summary>
    /// 初始化波浪
    /// </summary>
    public void Init(Vector3 spawnPosition, TileColor color, float distance = 10f, Vector2Int gridPos = default, int groupId = 0, bool firstWave = false, bool hasDamageBottomSkillFlag = false, float damageMult = 1f)
    {
        startPosition = spawnPosition;
        travelDistance = distance;
        targetPosition = spawnPosition + Vector3.right * travelDistance;
        hitEnemies.Clear();
        clearedTiles.Clear(); // 重置已清除的tile列表
        waveColor = color;
        penetrateCount = 0;
        spawnGridPos = gridPos;
        damageBottomTriggered = false;
        waveGroupId = groupId;
        isFirstWave = firstWave;
        hasDamageBottomSkill = hasDamageBottomSkillFlag;
        damageMultiplier = damageMult;
        hitEnemyCount = 0; // 重置击中敌人计数

        transform.position = spawnPosition;
        gameObject.SetActive(true);

        // 获取BoardManager
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }

        // 确保Collider2D设置为Trigger
        if (waveCollider != null)
        {
            waveCollider.isTrigger = true;
        }

        // 应用技能效果（包括buffNextDamage的加成）
        ApplySkillEffects();

        // 开始移动
        StartWave();
    }

    /// <summary>
    /// 开始波浪移动
    /// </summary>
    private void StartWave()
    {
        waveDuration = travelDistance / moveSpeed;

        // 如果有damageBottom技能且是第一个wave，监听是否离开最右列
        if (hasDamageBottomSkill && isFirstWave && boardManager != null)
        {
            CheckDamageBottomTrigger();
        }
        
        // 开始检测位置变化，清除经过的tile的fog和dirt
        if (boardManager != null)
        {
            InvokeRepeating(nameof(CheckAndClearFogDirt), 0.05f, 0.05f);
        }

        transform.DOMove(targetPosition, waveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                CancelInvoke(nameof(CheckAndClearFogDirt));
                DestroyWave();
            });
    }
    
    /// <summary>
    /// 检测并清除经过的tile的fog和dirt（只清除wave实际重叠的tile）
    /// </summary>
    private void CheckAndClearFogDirt()
    {
        if (boardManager == null)
            return;
            
        // 获取当前wave所在的网格位置
        Vector2Int currentGridPos = boardManager.WorldToGridPosition(transform.position);
        
        // 检查位置是否有效
        if (!boardManager.IsValidPosition(currentGridPos))
            return;
        
        // 如果这个tile已经清除过，跳过
        if (clearedTiles.Contains(currentGridPos))
            return;
        
        // 获取当前tile
        TileCell tile = boardManager.GetTile(currentGridPos);
        if (tile != null)
        {
            // 检查wave是否真的与tile重叠（使用bounds检测）
            Vector3 tileWorldPos = boardManager.GridToWorldPosition(currentGridPos);
            float tileSize = 1f; // 假设tile大小为1
            
            // 计算wave和tile的距离
            float distanceX = Mathf.Abs(transform.position.x - tileWorldPos.x);
            float distanceY = Mathf.Abs(transform.position.y - tileWorldPos.y);
            
            // 如果wave在tile的范围内（考虑tile大小和wave大小）
            if (distanceX <= tileSize * 0.5f && distanceY <= tileSize * 0.5f)
            {
                // 清除fog和dirt
                if (tile.HasFog)
                {
                    tile.SetFog(false);
                }
                if (tile.IsDirty)
                {
                    tile.SetDirty(false);
                }
                
                // 记录已清除的tile
                clearedTiles.Add(currentGridPos);
            }
        }
    }

    /// <summary>
    /// 检查damageBottom触发条件（波浪离开最右列时）
    /// </summary>
    private void CheckDamageBottomTrigger()
    {
        if (damageBottomTriggered || boardManager == null)
            return;

        // 使用Update检测位置变化
        InvokeRepeating(nameof(CheckPositionForDamageBottom), 0.05f, 0.05f);
    }

    /// <summary>
    /// 检查位置以触发damageBottom
    /// </summary>
    private void CheckPositionForDamageBottom()
    {
        if (damageBottomTriggered || boardManager == null || !isFirstWave)
            return;

        Vector2Int currentGridPos = boardManager.WorldToGridPosition(transform.position);
        int rightmostX = boardManager.Width - 1;

        // 如果从最右列离开（当前位置x > 最右列x，且生成位置x == 最右列x）
        // 注意：波浪向右移动，所以当前x会增大
        if (spawnGridPos.x == rightmostX && currentGridPos.x > rightmostX)
        {
            // 调用MainGameManager的静态方法触发damageBottom
            MainGameManager.TriggerDamageBottom(rightmostX, damage, waveColor);
            damageBottomTriggered = true;
            CancelInvoke(nameof(CheckPositionForDamageBottom)); // 停止检查
        }
    }

    /// <summary>
    /// 应用技能效果
    /// </summary>
    private void ApplySkillEffects()
    {
        if (SkillManager.Instance == null)
            return;

        string colorStr = GetColorString(waveColor);
        List<SkillInfo> skills = SkillManager.Instance.GetOwnedSkillsByColor(colorStr);

        // 先应用buffNextDamage的伤害加成
        damage = damage * damageMultiplier;

        foreach (var skill in skills)
        {
            int value = SkillManager.Instance.GetSkillValue(skill.identifier);
            
            switch (skill.effect)
            {
                case "damageIncrease":
                    // 伤害增加百分比
                    damage = damage * (1f + value / 100f);
                    break;
                    
                case "buffNextDamage":
                    // 下一个波浪伤害加成（已在MainGameManager中处理）
                    buffNextDamage = true;
                    break;
                    
                case "wavePenetrate":
                    // 穿透次数增加
                    penetrateCount = value;
                    break;
                    
                case "hitEnemyBack":
                    // 击退敌人
                    hasHitEnemyBack = true;
                    knockbackTiles = value;
                    break;
                    
                case "healWhenHit":
                    // 击中回血
                    hasHealWhenHit = true;
                    healAmount = value;
                    break;
                    
                case "damageBottom":
                    // 最右列爆炸（由外部传入标志控制）
                    hasDamageBottom = hasDamageBottomSkill;
                    break;
                    
                case "damageIncreaseWhenHitMore":
                    // 每击中一个敌人，下一个敌人伤害增加
                    hasDamageIncreaseWhenHitMore = true;
                    damageIncreaseWhenHitMoreValue = value;
                    break;
                    
                case "aoeAttack":
                    // 对相邻敌人造成伤害
                    hasAoeAttack = true;
                    aoeAttackValue = value;
                    break;
            }
        }
    }

    /// <summary>
    /// 将TileColor转换为字符串
    /// </summary>
    private string GetColorString(TileColor color)
    {
        switch (color)
        {
            case TileColor.Red: return "red";
            case TileColor.Yellow: return "yellow";
            case TileColor.Blue: return "blue";
            case TileColor.Green: return "green";
            default: return "";
        }
    }

    /// <summary>
    /// 碰撞检测（使用Trigger）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log($"Wave OnTriggerEnter2D: {collision.name}");
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null)
        {
            // 尝试从父对象获取
            enemy = collision.GetComponentInParent<Enemy>();
        }
        
        if (enemy != null && !enemy.IsDead)
        {
            // 检查是否已经击中过（穿透技能允许再次击中）
            bool alreadyHit = hitEnemies.Contains(enemy);
            
            if (!alreadyHit || penetrateCount > 0)
            {
                if (!alreadyHit)
                {
                    hitEnemies.Add(enemy);
                    hitEnemyCount++; // 增加击中敌人计数
                }
                
                Vector3 direction = (enemy.transform.position - transform.position).normalized;
                float finalDamage = damage;
                
                // 应用damageIncreaseWhenHitMore技能（在应用其他技能之前）
                if (hasDamageIncreaseWhenHitMore && hitEnemyCount > 1)
                {
                    // 每击中一个敌人，下一个敌人伤害增加value%
                    // 第1个敌人：基础伤害
                    // 第2个敌人：基础伤害 * (1 + value%)
                    // 第3个敌人：基础伤害 * (1 + 2 * value%)
                    // ...
                    float increaseMultiplier = 1f + (hitEnemyCount - 1) * (damageIncreaseWhenHitMoreValue / 100f);
                    finalDamage = finalDamage * increaseMultiplier;
                }
                
                // 应用击中时的技能效果
                ApplyHitSkillEffects(enemy, ref finalDamage, direction);
                
                //Debug.Log($"Wave hit enemy: {enemy.name}, dealing {finalDamage} damage");
                
                // 应用击退和回血效果
                bool shouldKnockback = hasHitEnemyBack;
                int knockbackTiles = hasHitEnemyBack ? this.knockbackTiles : 0;
                
                // 获取红色wave的基础伤害（用于hitTakeDamage）
                float redWaveBaseDamage = 20f; // 默认基础伤害
                if (waveColor == TileColor.Red)
                {
                    // 如果是红色wave，使用当前伤害（已应用所有加成）
                    redWaveBaseDamage = finalDamage;
                }
                else
                {
                    // 如果不是红色wave，需要获取红色wave的基础伤害
                    if (SkillManager.Instance != null)
                    {
                        List<SkillInfo> redSkills = SkillManager.Instance.GetOwnedSkillsByColor("red");
                        float redDamage = 20f; // 基础伤害
                        foreach (var skill in redSkills)
                        {
                            int value = SkillManager.Instance.GetSkillValue(skill.identifier);
                            if (skill.effect == "damageIncrease")
                            {
                                redDamage = redDamage * (1f + value / 100f);
                            }
                        }
                        redWaveBaseDamage = redDamage;
                    }
                }
                
                enemy.TakeDamage((int)finalDamage, direction, shouldKnockback, knockbackTiles, redWaveBaseDamage);
                
                // 记录伤害到wave group（用于spawnAlly技能）
                MainGameManager.RecordWaveDamage(waveGroupId, finalDamage);
                
                // 应用aoeAttack技能 - 对四向相邻的敌人造成伤害
                if (hasAoeAttack)
                {
                    ApplyAoeAttack(enemy, finalDamage);
                }
                
                // 击中回血
                if (hasHealWhenHit && PlayerManager.Instance != null)
                {
                    int healValue = (int)(finalDamage * healAmount / 100f);
                    PlayerManager.Instance.Heal(healValue);
                    DamageNumber.CreateDamageNumber(healValue, transform.position, true);
                }
                
                // 如果没有穿透能力或穿透次数为0，销毁波浪
                if (penetrateCount <= 0)
                {
                    DestroyWave();
                }
                // 穿透次数减1
                if (penetrateCount > 0)
                {
                    penetrateCount--;
                }
                
            }
        }
    }

    /// <summary>
    /// 应用击中时的技能效果
    /// </summary>
    private void ApplyHitSkillEffects(Enemy enemy, ref float finalDamage, Vector3 direction)
    {
        if (SkillManager.Instance == null)
            return;

        string colorStr = GetColorString(waveColor);
        List<SkillInfo> skills = SkillManager.Instance.GetOwnedSkillsByColor(colorStr);

        // 击中时的技能效果在这里处理（目前没有需要在这里处理的技能）
        // 其他技能效果在ApplySkillEffects中处理
    }

    /// <summary>
    /// 应用AOE攻击 - 对四向相邻的敌人造成伤害
    /// </summary>
    private void ApplyAoeAttack(Enemy targetEnemy, float baseDamage)
    {
        if (boardManager == null || EnemyManager.FindObjectOfType<EnemyManager>() == null)
            return;
            
        EnemyManager enemyManager = EnemyManager.FindObjectOfType<EnemyManager>();
        Vector2Int targetGridPos = targetEnemy.GridPosition;
        
        // 四向相邻位置
        Vector2Int[] adjacentPositions = new Vector2Int[]
        {
            new Vector2Int(targetGridPos.x, targetGridPos.y + 1), // 上
            new Vector2Int(targetGridPos.x, targetGridPos.y - 1), // 下
            new Vector2Int(targetGridPos.x - 1, targetGridPos.y), // 左
            new Vector2Int(targetGridPos.x + 1, targetGridPos.y)  // 右
        };
        
        // 对每个相邻位置的敌人造成伤害
        foreach (var pos in adjacentPositions)
        {
            foreach (var enemy in enemyManager.ActiveEnemies)
            {
                if (enemy != null && !enemy.IsDead && enemy != targetEnemy && enemy.GridPosition == pos)
                {
                    float aoeDamage = baseDamage * (aoeAttackValue / 100f);
                    // 获取红色wave的基础伤害（用于hitTakeDamage）
                    float redWaveBaseDamage = 20f;
                    if (waveColor == TileColor.Red)
                    {
                        redWaveBaseDamage = baseDamage;
                    }
                    else
                    {
                        if (SkillManager.Instance != null)
                        {
                            List<SkillInfo> redSkills = SkillManager.Instance.GetOwnedSkillsByColor("red");
                            float redDamage = 20f;
                            foreach (var skill in redSkills)
                            {
                                int value = SkillManager.Instance.GetSkillValue(skill.identifier);
                                if (skill.effect == "damageIncrease")
                                {
                                    redDamage = redDamage * (1f + value / 100f);
                                }
                            }
                            redWaveBaseDamage = redDamage;
                        }
                    }
                    enemy.TakeDamage((int)aoeDamage, Vector3.right, false, 0, redWaveBaseDamage);
                    break; // 每个位置只攻击一个敌人
                }
            }
        }
    }

    /// <summary>
    /// 销毁波浪
    /// </summary>
    private void DestroyWave()
    {
        transform.DOKill();
        
        // 通知MainGameManager这个wave已完成（用于spawnAlly技能）
        MainGameManager.OnWaveDestroyed(waveGroupId);
        
        // 可以添加消失动画
        transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                // 如果使用对象池，可以回收
            });
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}


