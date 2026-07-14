using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人系统
/// </summary>
public class Enemy : Character
{
    [Header("属性")]
    [SerializeField] private int defaultMaxHealth = 100;
    [SerializeField] protected float moveSpeed = 1f;
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("受击动画")]
    [SerializeField] private float jumpHeight = 0.2f; // 跳起高度
    [SerializeField] private float jumpDuration = 0.2f; // 跳起持续时间

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private GameObject shieldObject; // 盾牌显示对象（shield敌人使用）
    [SerializeField] protected SpriteRenderAnim spriteRenderAnim; // Sprite动画组件
    
    

    private int currentHealth;
    private int maxHealth;
    private Vector2Int gridPosition;
    private bool isDead = false;
    private Vector3 spriteRendererOriginalLocalPos; // spriteRenderer的原始本地位置
    private Tween jumpTween; // 当前的跳跃动画
    protected EnemyInfo enemyInfo; // 敌人信息
    protected BoardManager boardManager; // 棋盘管理器引用
    
    // 技能系统
    private string currentSkill = ""; // 当前技能名称
    private int skillValue = 0; // 技能值
    private int skillCooldown = 0; // 技能冷却时间（0表示被动技能，>0表示主动技能）
    private int currentCooldown = 0; // 当前冷却时间
    
    // Shield敌人系统
    private bool hasShield = false; // 是否有shield技能
    private bool shieldActive = false; // 盾牌是否激活（每回合开始时为true）
    private bool isShielded = false; // 是否处于防御状态（播放Shielded idle动画）
    private bool hasAttackedThisTurn = false; // 本回合是否已经攻击（如果攻击了，则不再执行防御操作）
    
    // LockHP敌人系统
    private bool hasLockHP = false; // 是否有lockHP技能
    
    // Buff/Debuff系统
    private int vulnerableStacks = 0; // vulnerable层数
    
    // 首次行动标记（每个敌人第一次行动时跳过）
    private bool isFirstAction = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public Vector2Int GridPosition => gridPosition;
    public EnemyInfo EnemyInfo => enemyInfo;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (enemyCollider == null)
            enemyCollider = GetComponentInChildren<Collider2D>();
        if (spriteRenderAnim == null)
            spriteRenderAnim = GetComponentInChildren<SpriteRenderAnim>();
        
        // 确保敌人有Collider2D和Rigidbody2D（用于碰撞检测）
        if (enemyCollider == null)
        {
            enemyCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // 确保Collider2D不是Trigger（敌人需要接收碰撞）
        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = false; // 敌人本身不是trigger，但可以接收trigger碰撞
        }
        
        // 确保有Rigidbody2D用于物理碰撞
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // 设置为运动学，不受物理影响，但可以接收碰撞事件
            rb.gravityScale = 0; // 不受重力影响
        }
        else
        {
            // 确保Rigidbody2D设置正确
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    /// <summary>
    /// 初始化敌人
    /// </summary>
    public void Init(Vector2Int gridPos, int health = -1, EnemyInfo info = null)
    {
        gridPosition = gridPos;
        maxHealth = health > 0 ? health : defaultMaxHealth;
        currentHealth = maxHealth;
        isDead = false;
        enemyInfo = info;
        
        spriteRenderAnim.SetIdentifier(enemyInfo.identifier);
        // 初始化shield系统
        hasShield = false;
        shieldActive = false;
        isShielded = false;
        if (enemyInfo != null && enemyInfo.identifier == "shield")
        {
            hasShield = true;
            shieldActive = true;
        }
        UpdateShieldDisplay();
        
        // 初始化lockHP系统
        hasLockHP = false;
        if (enemyInfo != null && enemyInfo.skill != null && enemyInfo.skill.Count > 0)
        {
            if (enemyInfo.skill[0] == "lockHP")
            {
                hasLockHP = true;
            }
        }
        
        // 初始化buff系统
        vulnerableStacks = 0;
        
        // 初始化首次行动标记
        
        if (isFirstAction && MainGameManager.Instance.IsFirstTurn())
        {
            isFirstAction = false;
        }
        else
        {
            isFirstAction = true;
        }
        
        // 初始化技能系统
        if (enemyInfo != null)
        {
            if (enemyInfo.skill != null && enemyInfo.skill.Count > 0 && !string.IsNullOrEmpty(enemyInfo.skill[0]))
            {
                currentSkill = enemyInfo.skill[0];
                skillValue = enemyInfo.skillValue;
                // 解析技能冷却时间（如果技能名称包含"|cooldown"格式）
                skillCooldown = enemyInfo.skillCD;
            }
            else
            {
                currentSkill = "";
                skillValue = 0;
                skillCooldown = 0;
            }
            currentCooldown = 0; // 初始冷却时间等于技能冷却时间
        }
        
        // 初始化动画系统
        if (enemyInfo != null && !string.IsNullOrEmpty(enemyInfo.identifier))
        {
            // 检查是否有对应的动画文件夹（首字母大写）
            string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
            bool hasAnimationFolder = SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier);
            
            if (hasAnimationFolder && spriteRenderAnim != null)
            {
                // 设置identifier并播放idle动画
                spriteRenderAnim.SetIdentifier(folderIdentifier);
                spriteRenderAnim.PlayIdle();
            }
            else if (spriteRenderer != null)
            {
                // 如果没有动画文件夹，使用原逻辑
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = info.icon;
                // 记录spriteRenderer的原始本地位置（如果spriteRenderer是transform的子对象）
                // 如果spriteRenderer直接挂载在transform上，使用localPosition
                spriteRendererOriginalLocalPos = spriteRenderer.transform.localPosition;
            }
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            if (info != null)
            {
                spriteRenderer.sprite = info.icon;
            }
            spriteRendererOriginalLocalPos = spriteRenderer.transform.localPosition;
        }
        
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        // 初始化血条
        if (healthBar != null)
        {
            healthBar.Init(this, maxHealth);
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        ShowSpawnEffect();
        if (enemyInfo.identifier == "shield")
        {
            PerformDefense();
        }
        
    }
    
    /// <summary>
    /// 解析技能冷却时间（从技能名称中解析，格式：skillName|cooldown 或 summon|identifier）
    /// </summary>
    private void ParseSkillCooldown()
    {
        skillCooldown = 0; // 默认为0（被动技能）
        
        if (string.IsNullOrEmpty(currentSkill))
            return;
            
        // 检查是否有"|"分隔符
        string[] parts = currentSkill.Split('|');
        if (parts.Length >= 2)
        {
            // 第一部分是技能名称
            string skillName = parts[0];
            
            // 对于summon技能，格式是summon|identifier，不需要解析冷却时间
            if (skillName == "summon")
            {
                // 保持currentSkill为完整格式，以便UseSummonSkill使用
                // currentSkill保持不变
                skillCooldown = 0; // summon技能默认无冷却（或根据需求设置）
            }
            else
            {
                // 其他技能，第二部分可能是冷却时间
                currentSkill = skillName; // 只保留技能名称
                
                // 尝试解析为冷却时间（数字）
                if (int.TryParse(parts[1], out int cooldown))
                {
                    skillCooldown = cooldown;
                }
            }
        }
    }

    /// <summary>
    /// 受到伤害。返回实际结算的伤害（被盾牌挡住时为 0）。
    /// </summary>
    public int TakeDamage(int damage, Vector3 attackDirection, bool shouldKnockback = false, int knockbackTiles = 0, float redWaveDamage = 0f, bool isCritical = false)
    {
        if (isDead)
            return 0;

        // Shield敌人：每回合第一次攻击被吃掉（但如果本回合已经攻击过，则不执行防御逻辑）
        if (hasShield && shieldActive && !hasAttackedThisTurn)
        {
            shieldActive = false;
            isShielded = false; // 被攻击后，重置防御状态，播放普通idle
            UpdateShieldDisplay();
            DamageNumber.CreateDamageNumber(0, transform.position, false, false);
            // 伤害被吃掉，不造成伤害
            return 0;
        }
        
        // 被攻击后，重置防御状态
        if (hasShield && isShielded)
        {
            isShielded = false;
        }
        
        // 应用lockHP技能：每次被攻击只掉1滴血
        if (hasLockHP)
        {
            damage = 1;
        }
        
        // 应用vulnerable debuff（每层增加5%伤害）
        if (vulnerableStacks > 0)
        {
            float vulnerableMultiplier = 1f + (vulnerableStacks * 0.05f);
            damage = Mathf.RoundToInt(damage * vulnerableMultiplier);
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // 显示伤害数字
        DamageNumber.CreateDamageNumber(damage, transform.position, false, isCritical);

        // 击退效果（只有shouldKnockback为true时才击退）
        if (shouldKnockback && knockbackTiles > 0)
        {
            ApplyKnockback(attackDirection, knockbackTiles, redWaveDamage);
        }

        // 跳起动画
        //ApplyJumpAnimation();
        
        // 检查死亡
        bool willDie = currentHealth <= 0;
        
        ShowHitEffect();
        // 播放受伤动画（会自动切换到idle）
        TryPlayHurtAnimation();

        // 更新视觉（可以添加血条等）
        UpdateVisual();

        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }

        // 检查死亡
        if (willDie)
        {
            Die();
        }

        return damage;
    }

    /// <summary>
    /// 斩杀：直接进入死亡流程（不额外飘字）
    /// </summary>
    public void KillByExecute()
    {
        if (isDead)
            return;

        currentHealth = 0;
        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        Die();
    }

    /// <summary>
    /// 应用跳起动画（spriteRenderer向上跳起然后落回原处）
    /// </summary>
    private void ApplyJumpAnimation()
    {
        if (spriteRenderer == null || isDead)
            return;

        // 获取spriteRenderer的transform（可能是transform本身或子对象）
        Transform spriteTransform = spriteRenderer.transform;
        Vector3 currentLocalPos = spriteTransform.localPosition;

        // 取消之前的跳跃动画，但保存当前的位置偏移
        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
            // 获取当前实际位置，用于计算新的起点
            currentLocalPos = spriteTransform.localPosition;
        }

        // 计算跳起的目标位置（向上）
        Vector3 jumpTargetPos = spriteRendererOriginalLocalPos + Vector3.up * jumpHeight;

        // 创建跳跃序列：向上 -> 向下回到原处
        Sequence jumpSequence = DOTween.Sequence();
        
        // 向上跳起
        jumpSequence.Append(spriteTransform.DOLocalMove(jumpTargetPos, jumpDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        
        // 向下落回原处
        jumpSequence.Append(spriteTransform.DOLocalMove(spriteRendererOriginalLocalPos, jumpDuration * 0.5f)
            .SetEase(Ease.InQuad));
        
        jumpSequence.OnComplete(() =>
        {
            // 确保最终位置正确
            spriteTransform.localPosition = spriteRendererOriginalLocalPos;
            jumpTween = null;
        });

        jumpTween = jumpSequence;
    }

    /// <summary>
    /// 击退效果（整数格子，检测碰撞和边界）
    /// </summary>
    private void ApplyKnockback(Vector3 direction, int tiles, float redWaveDamage)
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
        
        if (boardManager == null)
            return;

        direction.Normalize();
        
        // 确定击退方向（根据direction的x分量）
        bool knockbackRight = direction.x > 0;
        bool hitKeepMove = RuneManager.Instance != null && RuneManager.Instance.ShouldHitKeepMove();
        int maxSteps = hitKeepMove ? boardManager.Width : tiles;
        
        // 逐步检查每个格子，如果遇到敌人、随从或边界则停止
        Vector2Int currentPos = gridPosition;
        Vector2Int finalPos = currentPos;
        Enemy collidedEnemy = null;
        Ally collidedAlly = null;
        bool hasCollision = false;
        bool hitRightBorder = false;
        for (int i = 1; i <= maxSteps; i++)
        {
            Vector2Int checkPos = gridPosition;
            if (knockbackRight)
            {
                checkPos.x += i; // 向右击退
            }
            else
            {
                checkPos.x -= i; // 向左击退
            }
            
            // 检查是否超出边界
            if (knockbackRight)
            {
                if (checkPos.x >= boardManager.Width)
                {
                    hitRightBorder = true;
                    hasCollision = true;
                    break; // 到达右边界，停止
                }
            }
            else
            {
                if (checkPos.x < 0)
                {
                    hasCollision = true;
                    break; // 到达左边界，停止
                }
            }
            
            // 检查该位置是否有其他敌人
            EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
            if (enemyManager != null)
            {
                bool hasEnemy = false;
                foreach (var enemy in enemyManager.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsDead && enemy != this && 
                        enemy.GridPosition.x == checkPos.x && enemy.GridPosition.y == checkPos.y)
                    {
                        collidedEnemy = enemy;
                        hasEnemy = true;
                        hasCollision = true;
                        break;
                    }
                }
                if (hasEnemy)
                {
                    break; // 遇到敌人，停止
                }
            }
            
            // 检查该位置是否有随从
            AllyManager allyManager = FindObjectOfType<AllyManager>();
            if (allyManager != null)
            {
                Ally ally = allyManager.GetAllyAtPosition(checkPos);
                if (ally != null && !ally.IsDead)
                {
                    collidedAlly = ally;
                    hasCollision = true;
                    break; // 遇到随从，停止
                }
            }
            
            finalPos = checkPos;
        }
        
        // 如果有碰撞（包括碰撞随从），默认不移动；hitKeepMove 时滑到碰撞前一格
        if (hasCollision && !hitKeepMove)
        {
            finalPos = currentPos; // 保持原位置
        }

        if (hitRightBorder && RuneManager.Instance != null)
            RuneManager.Instance.TryApplyPushToBoss(redWaveDamage);
        
        // 计算世界坐标
        Vector3 targetWorldPos = boardManager.GridToWorldPosition(finalPos);
        // 敌人应该在格子上方，需要加上Y偏移（从EnemyManager获取）
        EnemyManager em = FindObjectOfType<EnemyManager>();
        if (em != null)
        {
            targetWorldPos += new Vector3(0, em.SpawnOffsetY, 0);
        }
        else
        {
            targetWorldPos += new Vector3(0, 0.5f, 0); // 默认偏移
        }
        
        // 更新网格位置
        gridPosition = finalPos;
        ShowSpawnEffect();
        // 移动到新位置
        transform.DOMove(targetWorldPos, knockbackDuration)
            .SetEase(Ease.OutQuad);
        
        // 如果有hitTakeDamage技能，对自己和碰撞的敌人或随从造成伤害
        if (SkillManager.Instance != null)
        {
            bool hasHitTakeDamage = false;
            int hitTakeDamageValue = 0;
            
            // 检查所有颜色的hitTakeDamage技能
            List<SkillInfo> allSkills = new List<SkillInfo>();
            foreach (var color in new[] { "red", "yellow", "blue", "green" })
            {
                allSkills.AddRange(SkillManager.Instance.GetOwnedSkillsByColor(color));
            }
            
            foreach (var skill in allSkills)
            {
                if (skill.effect == "hitTakeDamage")
                {
                    hasHitTakeDamage = true;
                    hitTakeDamageValue = SkillManager.Instance.GetSkillValue(skill.identifier);
                    break;
                }
            }
            
            if (hasHitTakeDamage && hitTakeDamageValue > 0 && redWaveDamage > 0 && hasCollision)
            {
                // 计算伤害：红色wave伤害 * value%
                float collisionDamage = redWaveDamage * (hitTakeDamageValue / 100f);
                
                // 确定hitTakeDamage是哪个颜色的技能（通常是黄色）
                TileColor hitTakeDamageColor = TileColor.Yellow;
                foreach (var skill in allSkills)
                {
                    if (skill.effect == "hitTakeDamage")
                    {
                        if (skill.color != null)
                        {
                            string colorStr = skill.color.ToLower();
                            if (colorStr == "red") hitTakeDamageColor = TileColor.Red;
                            else if (colorStr == "yellow") hitTakeDamageColor = TileColor.Yellow;
                            else if (colorStr == "blue") hitTakeDamageColor = TileColor.Blue;
                            else if (colorStr == "green") hitTakeDamageColor = TileColor.Green;
                        }
                        break;
                    }
                }
                
                // 对自己造成伤害（不触发击退，避免无限循环）
                TakeDamage((int)collisionDamage, Vector3.right, false, 0, 0f);
                
                // 对碰撞的敌人或随从造成伤害
                float totalDamage = collisionDamage;
                if (collidedEnemy != null && !collidedEnemy.IsDead)
                {
                    collidedEnemy.TakeDamage((int)collisionDamage, Vector3.left, false, 0, 0f);
                    totalDamage += collisionDamage; // 两个敌人都受到伤害
                }
                else if (collidedAlly != null && !collidedAlly.IsDead)
                {
                    // collidedAlly.TakeDamage((int)collisionDamage);
                    // totalDamage += collisionDamage; // 敌人和随从都受到伤害
                }
                
                // 记录统计
                if (StatisticsManager.Instance != null)
                {
                    StatisticsManager.Instance.RecordNonWaveDamage(hitTakeDamageColor, totalDamage);
                }
            }
        }

        if (RuneManager.Instance != null)
        {
            RuneManager.Instance.TryApplyHitTakeDamageOnCollision(
                this, collidedEnemy, collidedAlly, redWaveDamage, hasCollision);
        }
    }

    /// <summary>
    /// 向左移动（基于speed快速跳跃多次）
    /// </summary>
    public void MoveLeft(float duration = 0.3f)
    {
        if (isDead || enemyInfo == null)
            return;
// 如果是第一次行动，跳过并标记为已行动过
        if (isFirstAction)
        {
            isFirstAction = false;
            return;
        }
        int speed = enemyInfo.speed;
        if (speed <= 0)
            speed = 1; // 默认移动1格

        ShowSpawnEffect();
        // 获取EnemyManager和BoardManager
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
        
        if (boardManager == null)
            return;

        // 计算实际可以移动的步数（检查碰撞）
        int actualSteps = 0;
        for (int step = 1; step <= speed; step++)
        {
            int checkX = gridPosition.x - step;
            if (checkX < 0)
                break; // 超出左边界
                
            // 检查该位置是否有其他敌人或随从
            bool hasObstacle = false;
            if (enemyManager != null)
            {
                foreach (var enemy in enemyManager.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsDead && enemy != this && 
                        enemy.GridPosition.x == checkX && enemy.GridPosition.y == gridPosition.y)
                    {
                        hasObstacle = true;
                        break;
                    }
                }
            }
            
            // 检查是否有随从
            if (!hasObstacle)
            {
                AllyManager allyManager = FindObjectOfType<AllyManager>();
                if (allyManager != null && allyManager.HasAllyAtPosition(new Vector2Int(checkX, gridPosition.y)))
                {
                    hasObstacle = true;
                }
            }
            
            if (hasObstacle)
                break;
                
            actualSteps = step;
        }
        
        if (actualSteps == 0)
            return; // 无法移动

        // 每次跳跃的持续时间（快速跳跃）
        float singleJumpDuration = duration / actualSteps;
        float jumpHeight = 0.3f;
        float jumpUpDuration = singleJumpDuration * 0.4f;
        float jumpDownDuration = singleJumpDuration * 0.6f;
        
        Sequence moveSequence = DOTween.Sequence();
        
        // 进行多次快速跳跃
        for (int i = 0; i < actualSteps; i++)
        {
            int newX = gridPosition.x - (i + 1);
            Vector2Int newGridPos = new Vector2Int(newX, gridPosition.y);
            
            // 计算世界坐标
            Vector3 targetWorldPos = boardManager.GridToWorldPosition(newGridPos);
            if (enemyManager != null)
            {
                targetWorldPos += new Vector3(0, enemyManager.SpawnOffsetY, 0);
            }
            else
            {
                targetWorldPos += new Vector3(0, 0.5f, 0);
            }
            
            // 向上跳起
            moveSequence.Append(transform.DOMoveY(targetWorldPos.y + jumpHeight, jumpUpDuration)
                .SetEase(Ease.OutQuad));
            // 同时向左移动
            moveSequence.Join(transform.DOMoveX(targetWorldPos.x, singleJumpDuration)
                .SetEase(Ease.Linear));
            // 向下落回
            moveSequence.Append(transform.DOMoveY(targetWorldPos.y, jumpDownDuration)
                .SetEase(Ease.InQuad));
        }
        
        // 更新网格位置
        gridPosition.x -= actualSteps;
        
        // 播放移动动画（移动过程中）
        TryPlayMoveAnimation();
        
        // 移动完成后切换到idle
        moveSequence.OnComplete(() =>
        {
            // 移动后，重置防御状态，播放普通idle
            if (hasShield)
            {
                isShielded = false;
            }
            TryPlayIdleAnimation();
        });
    }
    
    /// <summary>
    /// 检查是否在攻击范围内（包括随从）
    /// </summary>
    public bool IsInAttackRange()
    {
        if (enemyInfo == null)
            return false;
            
        int range = enemyInfo.range;
        
        // 先检查是否有随从在攻击范围内
        AllyManager allyManager = FindObjectOfType<AllyManager>();
        if (allyManager != null)
        {
            for (int x = gridPosition.x; x >= 0 && x >= gridPosition.x - range; x--)
            {
                Ally ally = allyManager.GetAllyAtPosition(new Vector2Int(x, gridPosition.y));
                if (ally != null && !ally.IsDead)
                {
                    return true; // 有随从在攻击范围内
                }
            }
        }
        
        // 如果没有随从，检查是否在玩家攻击范围内
        // 远程敌人：检查自己到最左侧的距离，距离小于等于range就会攻击
        // 近战敌人（range <= 1）：只有在最左侧（x=0）时才能攻击玩家
        if (range > 1)
        {
            // 远程敌人：距离 = gridPosition.x - 0 = gridPosition.x
            return gridPosition.x <= range;
        }
        else
        {
            // 近战敌人：只有在最左侧（x=0）时才能攻击玩家
            return gridPosition.x == 0;
        }
    }
    
    /// <summary>
    /// 攻击玩家或随从
    /// </summary>
    public void AttackPlayer()
    {
        if (isDead || enemyInfo == null)
            return;
        
        
        // 如果是第一次行动，跳过并标记为已行动过
        if (isFirstAction)
        {
            isFirstAction = false;
            return;
        }
            
        int damage = enemyInfo.attack;
        if (damage <= 0)
            damage = 10; // 默认伤害
        
        // 应用敌人伤害加成（如果有）
        if (PlayerManager.Instance != null && PlayerManager.Instance.EnemyDamageBonus > 0)
        {
            float bonusPercent = PlayerManager.Instance.EnemyDamageBonus;
            damage = Mathf.RoundToInt(damage * (1f + bonusPercent / 100f));
        }
            
        int range = enemyInfo.range;
        
        // 先检查是否有随从在攻击范围内
        AllyManager allyManager = FindObjectOfType<AllyManager>();
        Ally targetAlly = null;
        if (allyManager != null)
        {
            for (int x = gridPosition.x; x >= 0 && x >= gridPosition.x - range; x--)
            {
                Ally ally = allyManager.GetAllyAtPosition(new Vector2Int(x, gridPosition.y));
                if (ally != null && !ally.IsDead)
                {
                    targetAlly = ally;
                    break;
                }
            }
        }
        
        // 攻击后，重置防御状态，播放普通idle
        if (hasShield)
        {
            isShielded = false;
            hasAttackedThisTurn = true; // 标记本回合已攻击，不再执行防御操作
        }
        
        // 原地doShake动画
        DoShake();
        
        // 播放攻击动画（会自动切换到idle）
        TryPlayAtkAnimation();
        
        if (targetAlly != null)
        {
            // 攻击随从
            if (range > 1)
            {
                // 远程攻击随从
                CreateProjectileToAlly(targetAlly, damage);
                FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_create_enemy_projectile");
            }
            else
            {
                // 近战攻击随从
                targetAlly.TakeDamage(damage);
            }
        }
        else if (PlayerManager.Instance != null)
        {
            // 攻击玩家
            if (range > 1)
            {
                CreateProjectile(damage);
                FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_create_enemy_projectile");
            }
            else
            {
                if (RuneManager.Instance != null)
                    RuneManager.Instance.TryApplyShieldDamageReflection(this, damage);

                // 近战攻击，直接造成伤害
                PlayerManager.Instance.TakeDamage(damage);
                // 显示伤害数字
                DamageNumber.CreateDamageNumber(damage, transform.position, false);
            }
        }
    }
    
    /// <summary>
    /// 创建投射物攻击随从
    /// </summary>
    private void CreateProjectileToAlly(Ally targetAlly, int damage)
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
        
        if (boardManager == null || targetAlly == null || enemyInfo == null)
            return;
            
        // 创建投射物GameObject（使用和攻击玩家一样的prefab）
        var projectPrefab = Resources.Load<GameObject>("Projectile/" + enemyInfo.identifier);
        if (projectPrefab == null)
        {
            // 如果没有找到对应的prefab，创建一个简单的可见投射物
            GameObject projectileObj = new GameObject("Projectile");
            SpriteRenderer sr = projectileObj.AddComponent<SpriteRenderer>();
            sr.color = Color.red;
            sr.sortingOrder = 5;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = targetAlly.transform.position;
            
            float travelDistance = Vector3.Distance(startPos, targetPos);
            float projectileSpeed = 10f;
            float travelTime = travelDistance / projectileSpeed;
            
            projectileObj.transform.position = startPos;
            projectileObj.transform.localScale = Vector3.one * 0.3f;
            
            projectileObj.transform.DOMove(targetPos, travelTime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 只调用TakeDamage，它会自己创建伤害数字
                    targetAlly.TakeDamage(damage);
                    Destroy(projectileObj);
                });
        }
        else
        {
            GameObject projectileObj = Instantiate(projectPrefab);
            projectileObj.transform.position = transform.position;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = targetAlly.transform.position;
            
            float travelDistance = Vector3.Distance(startPos, targetPos);
            float projectileSpeed = 10f;
            float travelTime = travelDistance / projectileSpeed;
            
            projectileObj.transform.DOMove(targetPos, travelTime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 只调用TakeDamage，它会自己创建伤害数字
                    targetAlly.TakeDamage(damage);
                    Destroy(projectileObj);
                });
        }
    }
    
    /// <summary>
    /// 执行震动动画
    /// </summary>
    private void DoShake()
    {
        if (spriteRenderer == null)
            return;
            
        float shakeDuration = 0.2f;
        float shakeStrength = 0.1f;
        
        // 使用DOTween的Shake功能
        transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false, true)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 创建投射物
    /// </summary>
    private void CreateProjectile(int damage)
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
        
        if (boardManager == null)
            return;
            
        // 创建投射物GameObject
        var projectPrefab = Resources.Load<GameObject>("Projectile/" + enemyInfo.identifier);;
        GameObject projectileObj = Instantiate(projectPrefab);
        projectileObj.transform.position = transform.position;
        
        
        // 计算目标位置（最左边）
        Vector3 startPos = transform.position;
        Vector3 targetPos = boardManager.GridToWorldPosition(new Vector2Int(0, gridPosition.y));
        if (EnemyManager.FindObjectOfType<EnemyManager>() != null)
        {
            EnemyManager em = FindObjectOfType<EnemyManager>();
            targetPos += new Vector3(0, em.SpawnOffsetY, 0);
        }
        else
        {
            targetPos += new Vector3(0, 0.5f, 0);
        }
        
        float travelDistance = Vector3.Distance(startPos, targetPos);
        float projectileSpeed = 10f; // 投射物速度
        float travelTime = travelDistance / projectileSpeed;
        
        // 移动投射物
        projectileObj.transform.DOMove(targetPos, travelTime)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 到达目标后造成伤害
                if (PlayerManager.Instance != null)
                {
                    if (RuneManager.Instance != null)
                        RuneManager.Instance.TryApplyShieldDamageReflection(this, damage);

                    PlayerManager.Instance.TakeDamage(damage);
                    // 显示伤害数字
                    DamageNumber.CreateDamageNumber(damage, targetPos, false);
                }
                
                // 销毁投射物
                Destroy(projectileObj);
            });
    }
    
    /// <summary>
    /// 敌人行动（每回合调用）
    /// </summary>
    public virtual void TakeAction()
    {
        if (isDead)
            return;
        
            
        // 1. 检查主动技能（冷却时间>0）
        if (skillCooldown > 0)
        {
            currentCooldown--;
            if (currentCooldown <= 0)
            {
                // 使用主动技能
                UseSkill();
                currentCooldown = skillCooldown; // 重置冷却
                return;
            }
        }
        
        // 2. 检查是否在攻击范围内
        if (IsInAttackRange())
        {
            AttackPlayer();
        }
        else
        {
            // 3. 否则向左移动
            MoveLeft();
        }
    }
    
    /// <summary>
    /// 使用技能
    /// </summary>
    private void UseSkill()
    {
        if (string.IsNullOrEmpty(currentSkill))
            return;
        
        // 播放特殊技能动画
        TryPlaySpecialAnimation();
            
        // 对于summon技能，currentSkill可能是"summon|identifier"格式
        if (currentSkill.StartsWith("summon"))
        {
            UseSummonSkill();
        }
        else if (currentSkill == "heal")
        {
            UseHealSkill();
        }
        else if (currentSkill == "createFog")
        {
            UseCreateFogSkill();
        }
        else if (currentSkill == "dirtyWater")
        {
            UseDirtyWaterSkill();
        }
    }
    
    /// <summary>
    /// 直接使用技能（不检查冷却，用于批次执行）
    /// </summary>
    /// <returns>是否成功使用了技能（heal技能如果没有可治疗的敌人会返回false）</returns>
    public bool UseSkillDirectly()
    {
        if (string.IsNullOrEmpty(currentSkill))
            return false;
        
        // 如果是第一次行动，跳过并标记为已行动过
        if (isFirstAction)
        {
            isFirstAction = false;
            return false;
        }
        
        bool skillUsed = false;
        
        // 对于summon技能，currentSkill可能是"summon|identifier"格式
        if (currentSkill.StartsWith("summon"))
        {
            UseSummonSkill();
            skillUsed = true;
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_swamp_attack");
        }
        else if (currentSkill == "heal")
        {
            skillUsed = UseHealSkill();
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_healer_cure");
            // 如果heal技能没有可治疗的敌人，不播放动画，也不重置冷却
            if (!skillUsed)
            {
                return false;
            }
        }
        else if (currentSkill == "createFog")
        {
            UseCreateFogSkill();
            skillUsed = true;
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_puffmo_attack");
        }
        else if (currentSkill == "dirtyWater")
        {
            UseDirtyWaterSkill();
            skillUsed = true;
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_dirt_attack");
        }
        
        // 如果技能成功使用，播放动画并重置冷却
        if (skillUsed)
        {
            // 播放特殊技能动画
            TryPlaySpecialAnimation();
            
            // 如果技能有冷却，重置冷却时间
            if (skillCooldown > 0)
            {
                currentCooldown = skillCooldown;
            }
        }
        
        return skillUsed;
    }
    
    /// <summary>
    /// 检查是否有主动技能且冷却完成
    /// </summary>
    public bool HasActiveSkillReady()
    {
        if (string.IsNullOrEmpty(currentSkill))
            return false;
        
        // 如果技能有冷却时间
        if (skillCooldown > 0)
        {
            // 检查冷却是否完成（currentCooldown <= 0表示冷却完成）
            return currentCooldown <= 0;
        }
        
        // 没有冷却时间的技能（被动技能）不算主动技能
        return false;
    }
    
    /// <summary>
    /// 获取当前技能名称
    /// </summary>
    public string GetCurrentSkill()
    {
        if (string.IsNullOrEmpty(currentSkill))
            return null;
        
        // 对于summon技能，返回完整格式
        if (currentSkill.StartsWith("summon"))
        {
            return currentSkill;
        }
        
        // 对于其他技能，返回技能名称
        return currentSkill;
    }
    
    /// <summary>
    /// 减少冷却时间（用于批次执行）
    /// </summary>
    public void ReduceCooldown()
    {

        hasAttackedThisTurn = false; // 重置攻击标志
        if (skillCooldown > 0 && currentCooldown > 0)
        {
            currentCooldown--;
        }
    }
    
    /// <summary>
    /// 使用heal技能：治疗血量不满，且血量百分比最少的敌人
    /// </summary>
    /// <returns>是否成功使用了技能（如果有可治疗的敌人返回true，否则返回false）</returns>
    private bool UseHealSkill()
    {
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
            return false;
            
        // 找到血量不满，且血量百分比最少的敌人
        Enemy targetEnemy = null;
        float minHealthPercent = 1f; // 初始化为1（满血）
        
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
                // 只考虑血量不满的敌人
                if (healthPercent < 1f && healthPercent < minHealthPercent)
                {
                    minHealthPercent = healthPercent;
                    targetEnemy = enemy;
                }
            }
        }
        
        // 如果没有血量不满的敌人，返回false
        if (targetEnemy == null)
        {
            return false;
        }
        
        // 如果有多个敌人血量百分比相同，随机选择一个
        List<Enemy> candidates = new List<Enemy>();
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
                if (healthPercent < 1f && Mathf.Approximately(healthPercent, minHealthPercent))
                {
                    candidates.Add(enemy);
                }
            }
        }
        
        if (candidates.Count > 0)
        {
            targetEnemy = candidates[Random.Range(0, candidates.Count)];
            // 恢复量 = 攻击力 * skillValue
            int healAmount = GetAttack() * skillValue;
            targetEnemy.Heal(healAmount);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 恢复血量
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead)
            return;
            
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        // 显示治疗数字
        DamageNumber.CreateDamageNumber(amount, transform.position, true);
        
        // 创建回血效果
        HealEffect.CreateHealEffect(transform);
    }
    
    /// <summary>
    /// 使用createFog技能：每回合随机在skillValue个tile上生成fog
    /// </summary>
    private void UseCreateFogSkill()
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null)
            return;
            
        // 获取所有有效的tile位置
        List<Vector2Int> availableTiles = new List<Vector2Int>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                TileCell tile = board.GetTile(new Vector2Int(x, y));
                if (tile != null && !tile.HasFog)
                {
                    availableTiles.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // 随机选择skillValue个tile生成fog
        int count = Mathf.Min(skillValue, availableTiles.Count);
        for (int i = 0; i < count; i++)
        {
            if (availableTiles.Count == 0)
                break;
                
            int index = Random.Range(0, availableTiles.Count);
            Vector2Int pos = availableTiles[index];
            availableTiles.RemoveAt(index);
            
            TileCell tile = board.GetTile(pos);
            if (tile != null)
            {
                tile.SetFog(true);
            }
        }
    }
    
    /// <summary>
    /// 使用dirtyWater技能：每回合随机在skillValue个tile上生成dirt
    /// </summary>
    private void UseDirtyWaterSkill()
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null)
            return;
            
        // 获取所有有效的tile位置（不包括已经有dirt的，不包括最左边一列）
        List<Vector2Int> availableTiles = new List<Vector2Int>();
        for (int x = 0; x < board.Width; x++)
        {
            // 跳过最左边一列（x=0）
            if (x <= 1)
                continue;
                
            for (int y = 0; y < board.Height; y++)
            {
                TileCell tile = board.GetTile(new Vector2Int(x, y));
                if (tile != null && !tile.IsDirty)
                {
                    availableTiles.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // 随机选择skillValue个tile生成dirt
        int count = Mathf.Min(skillValue, availableTiles.Count);
        for (int i = 0; i < count; i++)
        {
            if (availableTiles.Count == 0)
                break;
                
            int index = Random.Range(0, availableTiles.Count);
            Vector2Int pos = availableTiles[index];
            availableTiles.RemoveAt(index);
            
            TileCell tile = board.GetTile(pos);
            if (tile != null)
            {
                tile.SetDirty(true);
            }
        }
    }
    
    /// <summary>
    /// 使用summon技能：在离自己最近的格子召唤skillValue个identifier的enemy
    /// </summary>
    private void UseSummonSkill()
    {
        if (enemyInfo == null || enemyInfo.skill == null || enemyInfo.skill.Count == 0)
            return;
            
        if (enemyInfo.skill.Count < 2)
            return;
            
        string summonIdentifier = enemyInfo.skill[1];
        
        // 检查identifier是否存在
        if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(summonIdentifier))
        {
            Debug.LogWarning($"Summon enemy identifier not found: {summonIdentifier}");
            return;
        }
        
        EnemyInfo summonInfo = CSVLoader.Instance.enemyInfoMap[summonIdentifier];
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        BoardManager board = FindObjectOfType<BoardManager>();
        
        if (enemyManager == null || board == null)
            return;
            
        // 找到离自己最近的可用格子（右侧）
        List<Vector2Int> candidatePositions = new List<Vector2Int>();
        for (int x = gridPosition.x - 1; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                // 检查该位置是否有敌人
                bool hasEnemy = false;
                foreach (var enemy in enemyManager.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsDead && enemy.GridPosition == pos)
                    {
                        hasEnemy = true;
                        break;
                    }
                }
                if (!hasEnemy)
                {
                    candidatePositions.Add(pos);
                }
            }
        }
        
        // 按距离排序（最近的优先）
        candidatePositions.Sort((a, b) => 
        {
            int distA = Mathf.Abs(a.x - gridPosition.x) + Mathf.Abs(a.y - gridPosition.y);
            int distB = Mathf.Abs(b.x - gridPosition.x) + Mathf.Abs(b.y - gridPosition.y);
            return distA.CompareTo(distB);
        });
        
        // 召唤skillValue个敌人
        int count = Mathf.Min(skillValue, candidatePositions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int spawnPos = candidatePositions[i];
            Vector3 worldPos = board.GridToWorldPosition(spawnPos);
            worldPos += new Vector3(0, enemyManager.SpawnOffsetY, 0);
            
            // 获取EnemyManager的enemyParent（用于组织敌人对象）
            Transform enemyParent = enemyManager.transform.Find("Enemies");
            if (enemyParent == null)
            {
                enemyParent = enemyManager.transform;
            }
            
            GameObject enemyObj = Instantiate(enemyManager.enemyPrefab, worldPos, Quaternion.identity, enemyParent);
            Enemy newEnemy = enemyObj.GetComponent<Enemy>();
            if (newEnemy == null)
            {
                newEnemy = enemyObj.AddComponent<Enemy>();
            }
            
            newEnemy.Init(spawnPos, summonInfo.hp, summonInfo);
            enemyManager.ActiveEnemies.Add(newEnemy);
            
            // 召唤的敌人需要添加到remainingEnemies（因为未上场的敌人本来就在统计里）
            enemyManager.AddSummonedEnemy();
            
            // 创建血条
            enemyManager.CreateHealthBar(newEnemy);
        }
    }

    /// <summary>
    /// 更新视觉表现
    /// </summary>
    private void UpdateVisual()
    {
        // 可以根据血量改变颜色等
        // if (spriteRenderer != null)
        // {
        //     float healthPercent = (float)currentHealth / maxHealth;
        //     spriteRenderer.color = Color.Lerp(Color.red, Color.black, healthPercent);
        // }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 取消跳起动画
        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
            jumpTween = null;
        }

        // 隐藏血条
        if (healthBar != null)
        {
            healthBar.SetVisible(false);
        }

        // 敌人死亡时给玩家金币
        if (enemyInfo != null && enemyInfo.gold > 0 && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddGold(enemyInfo.gold);
            
            // 记录从chest获得的金钱（用于gold关卡统计）
            MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
            if (mainGameManager != null)
            {
                mainGameManager.RecordGoldFromChest(enemyInfo.gold);
            }
            
            Debug.Log($"敌人死亡，获得 {enemyInfo.gold} gold");
        }
        
        // 通知EnemyManager更新敌人计数
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            enemyManager.RemoveDeadEnemy(this);
        }
        
        // 播放死亡动画
        TryPlayDeadAnimation();
        
        // 等待1秒让玩家看到死亡动画，然后隐藏
        StartCoroutine(DieAfterDelay());
    }

    /// <summary>
    /// 延迟隐藏敌人（等待死亡动画播放）
    /// </summary>
    private System.Collections.IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        
        // 隐藏敌人
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        if (enemyCollider != null)
            enemyCollider.enabled = false;
        if (spriteRenderAnim)
        {
            spriteRenderAnim.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 清理动画
        if (jumpTween != null)
        {
            jumpTween.Kill();
            jumpTween = null;          
        }  
    }

    /// <summary>
    /// 设置血条引用
    /// </summary>
    public void SetHealthBar(EnemyHealthBar healthBar)
    {
        this.healthBar = healthBar;
    }

    /// <summary>
    /// 检查是否到达最左侧
    /// </summary>
    public bool IsAtLeftEdge()
    {
        return gridPosition.x <= 0;
    }
    
    private int calculatedAttack = 0; // 计算后的攻击力（考虑difficulty）
    
    /// <summary>
    /// 设置攻击力（考虑difficulty）
    /// </summary>
    public void SetAttack(int attack)
    {
        calculatedAttack = attack;
    }
    
    /// <summary>
    /// 获取攻击力（考虑buff/debuff和difficulty）
    /// </summary>
    public int GetAttack()
    {
        if (calculatedAttack > 0)
        {
            return calculatedAttack;
        }
        if (enemyInfo != null)
        {
            return enemyInfo.attack;
        }
        return 0;
    }
    
    /// <summary>
    /// 更新盾牌显示
    /// </summary>
    private void UpdateShieldDisplay()
    {
        if (shieldObject != null)
        {
            shieldObject.SetActive(shieldActive);
        }
    }
    
    /// <summary>
    /// 重置盾牌（敌人回合结束时调用）
    /// </summary>
    public void ResetShield()
    {
        if (hasShield)
        {
            shieldActive = true;
            //UpdateShieldDisplay();
        }
    }
    
    /// <summary>
    /// 添加vulnerable debuff
    /// </summary>
    public void AddVulnerable(int stacks)
    {
        vulnerableStacks += stacks;
        UpdateBuffDisplay();
    }
    
    /// <summary>
    /// 获取vulnerable层数
    /// </summary>
    public int GetVulnerableStacks()
    {
        return vulnerableStacks;
    }
    
    /// <summary>
    /// 更新buff显示（在敌人上显示buff图标）
    /// </summary>
    private void UpdateBuffDisplay()
    {
        // TODO: 实现buff图标显示
        // 可以在EnemyHealthBar或单独创建一个BuffDisplay组件
    }
    
    /// <summary>
    /// 尝试播放攻击动画
    /// </summary>
    protected void TryPlayAtkAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlayAtk();
        }
    }
    
    /// <summary>
    /// 尝试播放待机动画
    /// </summary>
    private void TryPlayIdleAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            // 如果是shield怪物且处于防御状态，播放Shielded idle动画
            if (hasShield && isShielded)
            {
                spriteRenderAnim.PlayIdleShielded();
            }
            else
            {
                spriteRenderAnim.PlayIdle();
            }
        }
    }
    
    /// <summary>
    /// 执行防御操作（shield怪物每回合开始时调用）
    /// </summary>
    public void PerformDefense()
    {
        if (!hasShield || isDead)
            return;
        
        // 如果本回合已经攻击了，则不执行防御操作（不播放special动画，不播放shielded idle，不设置isShielded）
        if (hasAttackedThisTurn)
            return;
            
        // 设置防御状态
        isShielded = true;
        
        // 播放special动画，然后自动切换到Shielded idle
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlaySpecialThenShielded();
        }
    }
    
    /// <summary>
    /// 检查是否是shield怪物
    /// </summary>
    public bool IsShieldEnemy()
    {
        return hasShield;
    }
    
    /// <summary>
    /// 尝试播放受伤动画
    /// </summary>
    private void TryPlayHurtAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlayHurt();
        }
        
        if (enemyInfo.identifier == "boss")
        {
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_huge_enemy_damaged");
            
        }else if (enemyInfo.identifier == "elite")
        {
            
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_huge_enemy_damaged");
        }
        else
        {
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_small_enemy_damaged");
        }
    }
    
    /// <summary>
    /// 尝试播放移动动画
    /// </summary>
    private void TryPlayMoveAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlayMove();
        }
    }
    
    /// <summary>
    /// 尝试播放特殊技能动画
    /// </summary>
    protected void TryPlaySpecialAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlaySpecial();
        }
    }
    
    /// <summary>
    /// 尝试播放死亡动画
    /// </summary>
    private void TryPlayDeadAnimation()
    {
        if (enemyInfo == null || string.IsNullOrEmpty(enemyInfo.identifier))
            return;
            
        string folderIdentifier = char.ToUpper(enemyInfo.identifier[0]) + enemyInfo.identifier.Substring(1);
        if (SpriteRenderAnim.HasAnimationFolder(enemyInfo.identifier) && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier(folderIdentifier);
            spriteRenderAnim.PlayDead();
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Enemies/sfx_basic_enemy_die");
        }
    }
}

