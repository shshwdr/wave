using UnityEngine;
using DG.Tweening;

/// <summary>
/// 我方随从系统 - 不会攻击，但会被敌人攻击并阻挡敌人
/// </summary>
public class Ally : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 1f;

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D allyCollider;
    [SerializeField] private EnemyHealthBar healthBar;

    private int currentHealth;
    private Vector2Int gridPosition;
    private bool isDead = false;
    private BoardManager boardManager;
    private EnemyManager enemyManager;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public Vector2Int GridPosition => gridPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (allyCollider == null)
            allyCollider = GetComponentInChildren<Collider2D>();
        
        // 确保有Collider2D
        if (allyCollider == null)
        {
            allyCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // 确保Collider2D不是Trigger（随从需要阻挡敌人）
        if (allyCollider != null)
        {
            allyCollider.isTrigger = false;
        }
        
        // 确保有Rigidbody2D用于物理碰撞
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
        else
        {
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    /// <summary>
    /// 初始化随从
    /// </summary>
    public void Init(Vector2Int gridPos, int health)
    {
        gridPosition = gridPos;
        maxHealth = health;
        currentHealth = maxHealth;
        isDead = false;

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
        if (enemyManager == null)
            enemyManager = FindObjectOfType<EnemyManager>();

        // 设置位置
        if (boardManager != null)
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
            if (enemyManager != null)
            {
                worldPos += new Vector3(0, enemyManager.SpawnOffsetY, 0);
            }
            transform.position = worldPos;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            // 可以设置一个默认的随从sprite，或者使用资源加载
            // spriteRenderer.sprite = Resources.Load<Sprite>("ally/default");
        }
        if (allyCollider != null)
        {
            allyCollider.enabled = true;
        }

        // 初始化血条
        if (healthBar != null)
        {
           // healthBar.Init(this, maxHealth);
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // 显示伤害数字
        DamageNumber.CreateDamageNumber(damage, transform.position, false);

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
    /// 死亡
    /// </summary>
    public void Die()
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
                if (allyCollider != null)
                    allyCollider.enabled = false;
            });
    }

    /// <summary>
    /// 恢复血量
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead)
            return;
            
        int oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        int actualHeal = currentHealth - oldHealth;
        
        // 显示回血数字
        if (actualHeal > 0)
        {
            DamageNumber.CreateDamageNumber(actualHeal, transform.position, true);
            // 创建回血效果
            HealEffect.CreateHealEffect(transform);
        }
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// 设置血条引用
    /// </summary>
    public void SetHealthBar(EnemyHealthBar healthBar)
    {
        this.healthBar = healthBar;
    }
}

