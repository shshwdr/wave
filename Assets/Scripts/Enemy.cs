using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人系统
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] private int defaultMaxHealth = 100;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("受击动画")]
    [SerializeField] private float jumpHeight = 0.2f; // 跳起高度
    [SerializeField] private float jumpDuration = 0.2f; // 跳起持续时间

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private int maxHealth;
    private Vector2Int gridPosition;
    private bool isDead = false;
    private Vector3 spriteRendererOriginalLocalPos; // spriteRenderer的原始本地位置
    private Tween jumpTween; // 当前的跳跃动画
    private EnemyInfo enemyInfo; // 敌人信息
    private BoardManager boardManager; // 棋盘管理器引用

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
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = info.icon;
            // 记录spriteRenderer的原始本地位置（如果spriteRenderer是transform的子对象）
            // 如果spriteRenderer直接挂载在transform上，使用localPosition
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
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage, Vector3 attackDirection, bool shouldKnockback = false, int knockbackTiles = 0, float redWaveDamage = 0f)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // 显示伤害数字
        DamageNumber.CreateDamageNumber(damage, transform.position + Vector3.up * 0.5f, false);

        // 击退效果（只有shouldKnockback为true时才击退）
        if (shouldKnockback && knockbackTiles > 0)
        {
            ApplyKnockback(attackDirection, knockbackTiles, redWaveDamage);
        }

        // 跳起动画
        ApplyJumpAnimation();

        // 更新视觉（可以添加血条等）
        UpdateVisual();

        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }

        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
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
        
        // 逐步检查每个格子，如果遇到敌人或边界则停止
        Vector2Int currentPos = gridPosition;
        Vector2Int finalPos = currentPos;
        Enemy collidedEnemy = null;
        
        for (int i = 1; i <= tiles; i++)
        {
            Vector2Int checkPos = gridPosition;
            checkPos.x += i; // 向右击退
            
            // 检查是否超出边界
            if (checkPos.x >= boardManager.Width)
            {
                break; // 到达边界，停止
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
                        break;
                    }
                }
                if (hasEnemy)
                {
                    break; // 遇到敌人，停止
                }
            }
            
            finalPos = checkPos;
        }
        
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
        
        // 移动到新位置
        transform.DOMove(targetWorldPos, knockbackDuration)
            .SetEase(Ease.OutQuad);
        
        // 如果有hitTakeDamage技能，对自己和碰撞的敌人造成伤害
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
            
            if (hasHitTakeDamage && hitTakeDamageValue > 0 && redWaveDamage > 0)
            {
                // 计算伤害：红色wave伤害 * value%
                float collisionDamage = redWaveDamage * (hitTakeDamageValue / 100f);
                
                // 对自己造成伤害（不触发击退，避免无限循环）
                TakeDamage((int)collisionDamage, Vector3.right, false, 0, 0f);
                
                // 对碰撞的敌人造成伤害
                if (collidedEnemy != null && !collidedEnemy.IsDead)
                {
                    collidedEnemy.TakeDamage((int)collisionDamage, Vector3.left, false, 0, 0f);
                }
            }
        }
    }

    /// <summary>
    /// 向左移动
    /// </summary>
    public void MoveLeft(float distance, float duration)
    {
        if (isDead)
            return;

        Vector3 targetPos = transform.position + Vector3.left * distance;
        gridPosition.x -= 1; // 向左移动一格

        transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear);
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
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 取消跳起动画并确保位置正确
        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
            jumpTween = null;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition = spriteRendererOriginalLocalPos;
        }

        // 隐藏血条
        if (healthBar != null)
        {
            healthBar.SetVisible(false);
        }

        // 死亡动画
        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
                if (enemyCollider != null)
                    enemyCollider.enabled = false;
            });
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
}

