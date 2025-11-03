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

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private int maxHealth;
    private Vector2Int gridPosition;
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public Vector2Int GridPosition => gridPosition;

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
    public void Init(Vector2Int gridPos, int health = -1)
    {
        gridPosition = gridPos;
        maxHealth = health > 0 ? health : defaultMaxHealth;
        currentHealth = maxHealth;
        isDead = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
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
    public void TakeDamage(int damage, Vector3 attackDirection)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // 击退效果
        ApplyKnockback(attackDirection);

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
    /// 击退效果
    /// </summary>
    private void ApplyKnockback(Vector3 direction)
    {
        return;
        direction.Normalize();
        Vector3 knockbackPos = transform.position + direction * knockbackForce;
        
        transform.DOMove(knockbackPos, knockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 击退后可以添加回弹效果
                transform.DOMove(transform.position - direction * (knockbackForce * 0.3f), knockbackDuration * 0.5f)
                    .SetEase(Ease.InQuad);
            });
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
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

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

