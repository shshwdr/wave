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
    
    [Header("波动设置")]
    [SerializeField] private float waveAmplitude = 0.5f; // 波动振幅
    [SerializeField] private float waveFrequency = 2f; // 波动频率（每秒周期数）

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
    private bool hasBounty = false; // 是否有bounty技能
    private int bountyValue = 0; // bounty的值
    private bool hasExchange = false; // 是否有exchange技能
    private int exchangeValue = 0; // exchange的值
    private bool hasPure = false; // 是否有pure技能
    private int pureValue = 0; // pure的值
    private bool hasLowHP = false; // 是否有lowHP技能
    private int lowHPValue = 0; // lowHP的值
    private bool hasHighHP = false; // 是否有highHP技能
    private int highHPValue = 0; // highHP的值
    private bool hasHealWhenPass = false; // 是否有healWhenPass技能
    private int healWhenPassValue = 0; // healWhenPass的值
    private bool hasHealWhenSpawn = false; // 是否有healWhenSpawn技能
    private int healWhenSpawnValue = 0; // healWhenSpawn的值
    private bool hasHitAddColor = false; // 是否有hitAddColor技能
    private bool hasFocus = false; // 是否有focus技能
    private int focusValue = 0; // focus的值
    private bool hasPassedSameColorTile = false; // 是否已经经过同色tile（用于healWhenPass和addDamageWhenPass）
    private bool hasTarget = false; // 是否有target技能
    private int targetValue = 0; // target的值
    private float waveStartTime = 0f; // 波动开始时间
    private float baseYPosition = 0f; // 基础Y位置（起始位置的Y坐标）
    private int columnIndex = 0; // 列索引（用于计算相位偏移）
    private bool isMoving = false; // 是否正在移动
    private int tilesUsed = 1; // 使用的tile数量（用于moreTileMoreDamage和encourageMoreTiles技能）
    private bool moveBackward = false; // 是否向后移动（向左）

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
    public void Init(Vector3 spawnPosition, TileColor color, float distance = 10f, Vector2Int gridPos = default, int groupId = 0, bool firstWave = false, bool hasDamageBottomSkillFlag = false, float damageMult = 1f, bool hasPureFlag = false, int pureValueParam = 0, int tilesUsedCount = 1, bool backward = false)
    {
        startPosition = spawnPosition;
        travelDistance = distance;
        moveBackward = backward;
        if (moveBackward)
        {
            targetPosition = spawnPosition + Vector3.left * travelDistance; // 向左移动
        }
        else
        {
            targetPosition = spawnPosition + Vector3.right * travelDistance; // 向右移动
        }
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
        hasPure = hasPureFlag;
        pureValue = pureValueParam;
        hasPassedSameColorTile = false; // 重置经过同色tile标志
        hitEnemyCount = 0; // 重置击中敌人计数
        hasTarget = false; // 重置target技能
        targetValue = 0;
        tilesUsed = tilesUsedCount; // 记录使用的tile数量
        
        // 初始化波动相关变量
        columnIndex = gridPos.x; // 列索引
        baseYPosition = spawnPosition.y; // 基础Y位置
        waveStartTime = Time.time; // 记录开始时间
        isMoving = false; // 初始状态为未移动

        transform.position = spawnPosition;
        gameObject.SetActive(true);
        spriteRenderer.sprite = MainGameManager.Instance.tileSprites.RandomItem();
        // 设置spriteRenderer的颜色为对应的tile颜色
        // if (spriteRenderer != null)
        // {
        //     spriteRenderer.color = TileColorUtil.GetUnityColor(waveColor) + new Color(0.2f, 0.2f, 0.2f);
        // }

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
        isMoving = true; // 开始移动

        // 如果有damageBottom技能且是第一个wave，监听是否离开最右列
        if (hasDamageBottomSkill && isFirstWave && boardManager != null)
        {
            CheckDamageBottomTrigger();
        }
        
        // 开始检测位置变化，清除经过的tile的fog和dirt（只有向前移动的波浪清除）
        if (boardManager != null && !moveBackward)
        {
            InvokeRepeating(nameof(CheckAndClearFogDirt), 0.05f, 0.05f);
        }

        // 使用协程或Update来处理带波动的移动
        // 不再使用DOMove，改为在Update中处理
    }
    
    /// <summary>
    /// Update方法：处理带波动的移动
    /// </summary>
    private void Update()
    {
        if (!isMoving)
            return;
        
        // 计算经过的时间
        float elapsedTime = Time.time - waveStartTime;
        
        // 计算水平移动进度（0到1）
        float horizontalProgress = elapsedTime / waveDuration;
        
        // 如果已经到达目标位置，检查是否击中boss，然后销毁wave
        if (horizontalProgress >= 1f)
        {
            CancelInvoke(nameof(CheckAndClearFogDirt));
            // 在boss战中，检查是否击中boss
            CheckBossCollision();
            DestroyWave();
            return;
        }
        
        // 计算水平位置（线性移动）
        float currentX;
        if (moveBackward)
        {
            // 向后移动（向左）
            currentX = Mathf.Lerp(startPosition.x, targetPosition.x, horizontalProgress);
        }
        else
        {
            // 向前移动（向右）
            currentX = Mathf.Lerp(startPosition.x, targetPosition.x, horizontalProgress);
        }
        
        // 计算垂直波动
        // 每两列之间相差半个周期（π），所以相位偏移 = columnIndex * π
        float phaseOffset = columnIndex * Mathf.PI; // 每列相差π（半个周期）
        float time = elapsedTime * waveFrequency * 2f * Mathf.PI; // 转换为角度
        float verticalOffset = Mathf.Sin(time + phaseOffset) * waveAmplitude;
        
        // 更新位置
        float currentY = baseYPosition + verticalOffset;
        transform.position = new Vector3(currentX, currentY, transform.position.z);
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
                // 检查是否经过同色tile（用于healWhenPass和addDamageWhenPass）
                if (tile.Color == waveColor && !hasPassedSameColorTile)
                {
                    hasPassedSameColorTile = true;
                    
                    // 应用healWhenPass技能
                    if (hasHealWhenPass && PlayerManager.Instance != null)
                    {
                        int maxHealth = PlayerManager.Instance.MaxHealth;
                        int healValue = (int)(maxHealth * healWhenPassValue / 100f);
                        PlayerManager.Instance.Heal(healValue);
                        DamageNumber.CreateDamageNumber(healValue, transform.position, true);
                        // 回血效果已在PlayerManager.Heal()中创建
                    }
                    
                    // 应用addDamageWhenPass技能（整个wave group共享）
                    if (PlayerManager.Instance != null && SkillManager.Instance != null)
                    {
                        int colorIndex = (int)waveColor;
                        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
                        foreach (var identifier in skillIdentifiers)
                        {
                            if (SkillManager.Instance.HasSkill(identifier))
                            {
                                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                                if (skillInfo != null && skillInfo.effect == "addDamageWhenPass")
                                {
                                    int value = SkillManager.Instance.GetSkillValue(identifier);
                                    MainGameManager.SetAddDamageWhenPass(waveGroupId, value);
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // 清除fog和dirt（和fog一样的逻辑）
                if (tile.HasFog)
                {
                    tile.SetFog(false);
                }
                if (tile.IsDirty)
                {
                    tile.SetDirty(false);
                }
                
                // 记录已清除的tile（无论是否有fog或dirt都记录，避免重复处理）
                clearedTiles.Add(currentGridPos);
            }
        }
    }

    /// <summary>
    /// 检查damageBottom触发条件（波浪离开最右列时）
    /// </summary>
    private void CheckDamageBottomTrigger()
    {
        // 向后移动的波浪不触发damageBottom
        if (damageBottomTriggered || boardManager == null || moveBackward)
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
        if (SkillManager.Instance == null || PlayerManager.Instance == null)
            return;

        // 从PlayerManager获取该颜色wave配置的技能列表
        int colorIndex = (int)waveColor; // TileColor枚举值：Red=0, Yellow=1, Blue=2, Green=3
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

        // 先处理moreTileMoreDamage技能（需要在最开始处理，因为它改变基础伤害）
        bool hasMoreTileMoreDamage = false;
        int moreTileMoreDamageValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "moreTileMoreDamage")
                {
                    hasMoreTileMoreDamage = true;
                    moreTileMoreDamageValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }
        
        if (hasMoreTileMoreDamage)
        {
            // 基础伤害变成 value% base damage × tiles used
            float baseDamage = 20f; // 基础伤害
            damage = baseDamage * (moreTileMoreDamageValue / 100f) * tilesUsed;
        }
        
        // 应用buffNextDamage的伤害加成
        damage = damage * damageMultiplier;

        foreach (var identifier in skillIdentifiers)
        {
            if (!SkillManager.Instance.HasSkill(identifier))
                continue;
                
            SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
            if (skillInfo == null)
                continue;
                
            int value = SkillManager.Instance.GetSkillValue(identifier);
            
            switch (skillInfo.effect)
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
                    
                // more和less技能现在在BoardManager.GetWeightedRandomColor中动态处理，不需要在这里处理
                    
                case "bounty":
                    // 标记有bounty技能
                    hasBounty = true;
                    bountyValue = value;
                    break;
                    
                case "exchange":
                    // 标记有exchange技能
                    hasExchange = true;
                    exchangeValue = value;
                    break;
                    
                case "pure":
                    // pure技能在EliminateConnectedTiles中处理，这里不需要处理
                    // 因为pure技能只在单个tile时生效，已经在CreateWave时传递了
                    break;
                    
                case "lowHP":
                    // 对血量少于30%的敌人造成额外伤害
                    hasLowHP = true;
                    lowHPValue = value;
                    break;
                    
                case "highHP":
                    // 对满血敌人造成额外伤害
                    hasHighHP = true;
                    highHPValue = value;
                    break;
                    
                case "healWhenPass":
                    // 经过同色tile时恢复血量
                    hasHealWhenPass = true;
                    healWhenPassValue = value;
                    break;
                    
                case "healWhenSpawn":
                    // 生成时恢复血量（每个tile恢复value%的已损失血量）
                    hasHealWhenSpawn = true;
                    healWhenSpawnValue = value;
                    break;
                    
                case "addDamageWhenPass":
                    // 经过同色tile时整个wave group增加伤害（在CheckAndClearFogDirt中处理）
                    // 这里只标记，实际处理在MainGameManager中
                    break;
                    
                case "hitAddColor":
                    // 击中敌人时改变场上tile颜色
                    hasHitAddColor = true;
                    break;
                    
                case "focus":
                    // 击中第一个敌人后移除，攻击力提高
                    hasFocus = true;
                    focusValue = value;
                    break;
                    
                case "damageIncreaseAll":
                    // 所有wave伤害提升（全局效果）
                    damage = damage * (1f + value / 100f);
                    break;
                    
                case "target":
                    // target技能在击中敌人时处理，这里只标记
                    hasTarget = true;
                    targetValue = value;
                    break;
                    
                case "encourageMoreTiles":
                    // encourageMoreTiles在最后处理，这里只标记
                    break;
            }
        }
        
        // 应用pure技能（如果只有一个tile）
        if (hasPure && pureValue > 0)
        {
            damage = damage * (1f + pureValue / 100f);
        }
        
        // 应用focus技能（攻击力提高）
        if (hasFocus && focusValue > 0)
        {
            damage = damage * (1f + focusValue / 100f);
        }
        
        // 最后处理encourageMoreTiles技能（如果生成的wave的方块数量>value，伤害增加150%）
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "encourageMoreTiles")
                {
                    int value = SkillManager.Instance.GetSkillValue(identifier);
                    if (tilesUsed > value)
                    {
                        damage = damage * 2.5f; // 增加150% = 乘以2.5
                    }
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// 应用healWhenSpawn技能（生成时恢复血量）
    /// </summary>
    private void ApplyHealWhenSpawn()
    {
        if (!hasHealWhenSpawn || PlayerManager.Instance == null)
            return;
        
        // 计算已损失血量
        int maxHealth = PlayerManager.Instance.MaxHealth;
        int currentHealth = PlayerManager.Instance.CurrentHealth;
        int lostHealth = maxHealth - currentHealth;
        
        // if (lostHealth <= 0)
        //     return; // 没有损失血量，不需要恢复
        //
        // 每个tile恢复 value% 的已损失血量，总回血量 = lostHealth × value% × tilesUsed
        float healPerTile = lostHealth * healWhenSpawnValue / 100f;
        int totalHeal = (int)(healPerTile * tilesUsed);
        
        //if (totalHeal > 0)
        {
            PlayerManager.Instance.Heal(totalHeal);
            DamageNumber.CreateDamageNumber(totalHeal, transform.position, true);
        }
    }
    
    /// <summary>
    /// 将颜色字符串转换为索引
    /// </summary>
    private int GetColorIndexFromString(string color)
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
                
                // 计算击退方向（向前移动的波浪向右击退，向后移动的波浪向左击退）
                Vector3 direction;
                if (moveBackward)
                {
                    direction = Vector3.left; // 向后移动的波浪向左击退
                }
                else
                {
                    direction = (enemy.transform.position - transform.position).normalized; // 向前移动的波浪正常计算方向
                }
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
                
                // 应用lowHP技能（对血量少于30%的敌人造成额外伤害）
                if (hasLowHP && enemy != null)
                {
                    float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
                    if (healthPercent < 0.3f)
                    {
                        finalDamage = finalDamage * (1f + lowHPValue / 100f);
                    }
                }
                
                // 应用highHP技能（对满血敌人造成额外伤害）
                if (hasHighHP && enemy != null)
                {
                    if (enemy.CurrentHealth >= enemy.MaxHealth)
                    {
                        finalDamage = finalDamage * (1f + highHPValue / 100f);
                    }
                }
                
                // 应用addDamageWhenPass技能（整个wave group共享）
                if (MainGameManager.HasAddDamageWhenPass(waveGroupId))
                {
                    float addDamageValue = MainGameManager.GetAddDamageWhenPassValue(waveGroupId);
                    finalDamage = finalDamage * (1f + addDamageValue / 100f);
                }
                
                // 应用击中时的技能效果
                ApplyHitSkillEffects(enemy, ref finalDamage, direction);
                
                // 应用target技能（给敌人添加vulnerable debuff）
                if (hasTarget && enemy != null)
                {
                    enemy.AddVulnerable(targetValue);
                }
                
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
                    if (SkillManager.Instance != null && PlayerManager.Instance != null)
                    {
                        List<string> redSkillIdentifiers = PlayerManager.Instance.GetWaveSkills(0); // 0=红色
                        float redDamage = 20f; // 基础伤害
                        foreach (var identifier in redSkillIdentifiers)
                        {
                            if (SkillManager.Instance.HasSkill(identifier))
                            {
                                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                                if (skillInfo != null && skillInfo.effect == "damageIncrease")
                                {
                                    int value = SkillManager.Instance.GetSkillValue(identifier);
                                    redDamage = redDamage * (1f + value / 100f);
                                }
                            }
                        }
                        redWaveBaseDamage = redDamage;
                    }
                }
                
                bool wasAlive = !enemy.IsDead;
                enemy.TakeDamage((int)finalDamage, direction, shouldKnockback, knockbackTiles, redWaveBaseDamage);
                
                // 检查敌人是否死亡（由这次伤害导致）
                if (wasAlive && enemy.IsDead)
                {
                    // 触发bounty技能
                    if (hasBounty && PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.AddGold(bountyValue);
                        Debug.Log($"Bounty: 获得 {bountyValue} gold");
                    }
                    
                    // 触发exchange技能
                    if (hasExchange && PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.AddTempSwapCount(exchangeValue);
                        Debug.Log($"Exchange: 获得 {exchangeValue} 临时交换次数");
                    }
                }
                
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
                    // 回血效果已在PlayerManager.Heal()中创建
                }
                
                // 应用hitAddColor技能（击中敌人时改变场上tile颜色）
                if (hasHitAddColor && boardManager != null)
                {
                    ApplyHitAddColor();
                }
                
                // 应用focus技能（击中第一个敌人后移除）
                if (hasFocus && hitEnemyCount == 1)
                {
                    DestroyWave();
                    return; // 立即返回，不再处理后续逻辑
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
    /// 应用hitAddColor技能（击中敌人时改变场上tile颜色）
    /// </summary>
    private void ApplyHitAddColor()
    {
        if (boardManager == null)
            return;
            
        // 找到场地上一个不是这个wave颜色的tile
        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;
        
        List<Vector2Int> candidateTiles = new List<Vector2Int>();
        
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                TileCell tile = boardManager.GetTile(gridPos);
                if (tile != null && tile.Color != waveColor)
                {
                    candidateTiles.Add(gridPos);
                }
            }
        }
        
        // 如果找到候选tile，随机选择一个并改变颜色
        if (candidateTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, candidateTiles.Count);
            Vector2Int targetPos = candidateTiles[randomIndex];
            TileCell targetTile = boardManager.GetTile(targetPos);
            if (targetTile != null)
            {
                targetTile.SetColor(waveColor);
            }
        }
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
    /// 检查是否与boss碰撞（在wave到达目标位置时调用）
    /// </summary>
    private void CheckBossCollision()
    {
        Enemy boss = MainGameManager.GetCurrentBoss() as Enemy;
        if (boss == null || boss.IsDead)
            return;
            
        // 检查wave是否与boss碰撞（使用bounds检测）
        Vector3 bossPos = boss.transform.position;
        float distanceX = Mathf.Abs(transform.position.x - bossPos.x);
        float distanceY = Mathf.Abs(transform.position.y - bossPos.y);
        float collisionRange = 0.5f; // 碰撞范围
        
        if (distanceX <= collisionRange && distanceY <= collisionRange)
        {
            // 击中boss
            if (!hitEnemies.Contains(boss))
            {
                hitEnemies.Add(boss);
                hitEnemyCount++;
            }
            
            // 计算伤害（和攻击敌人一样的逻辑）
            Vector3 direction = (boss.transform.position - transform.position).normalized;
            float finalDamage = damage;
            
            // 应用技能效果（简化版，只应用主要技能）
            if (hasDamageIncreaseWhenHitMore && hitEnemyCount > 1)
            {
                float increaseMultiplier = 1f + (hitEnemyCount - 1) * (damageIncreaseWhenHitMoreValue / 100f);
                finalDamage = finalDamage * increaseMultiplier;
            }
            
            // 应用addDamageWhenPass技能
            if (MainGameManager.HasAddDamageWhenPass(waveGroupId))
            {
                float addDamageValue = MainGameManager.GetAddDamageWhenPassValue(waveGroupId);
                finalDamage = finalDamage * (1f + addDamageValue / 100f);
            }
            
            // 对boss造成伤害
            float redWaveBaseDamage = 20f;
            if (waveColor == TileColor.Red)
            {
                redWaveBaseDamage = finalDamage;
            }
            boss.TakeDamage((int)finalDamage, direction, false, 0, redWaveBaseDamage);
            
            // 记录伤害
            MainGameManager.RecordWaveDamage(waveGroupId, finalDamage);
        }
    }
    
    /// <summary>
    /// 销毁波浪
    /// </summary>
    private void DestroyWave()
    {
        isMoving = false; // 停止移动
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


