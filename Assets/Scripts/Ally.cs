using UnityEngine;
using DG.Tweening;


public class Character : MonoBehaviour
{
    
    [SerializeField] protected ParticleSystem showSmoke;
    [SerializeField] protected ParticleSystem hitEffect;

    public void ShowSpawnEffect()
    {
        var effect = Instantiate(showSmoke,transform);
        effect.transform.parent = effect.transform.parent.parent;
        effect.gameObject.SetActive(true);
        // showSmoke.transform.parent = showSmoke.transform.parent.parent;
        // showSmoke.gameObject.SetActive(true);
        // showSmoke.Play();
    }

    public void ShowHitEffect()
    {
        //hitEffect.gameObject.SetActive(true);
        var effect = Instantiate(hitEffect,transform);
        effect.gameObject.SetActive(true);
        //hitEffect.Play();
    }

    /// <summary>
    /// 在指定位置播放 Resources/effect 下的一次性特效（世界空间）。
    /// </summary>
    public static void PlayEffectAt(string resourcePath, Vector3 position, float destroyAfter = 2f)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
            return;

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        Destroy(effect, destroyAfter);
    }

    /// <summary>
    /// 在当前位置播放 Resources/effect 下的一次性特效（世界空间，不随角色缩放/销毁）。
    /// </summary>
    protected void PlayEffectAtSelf(string resourcePath, float destroyAfter = 2f)
    {
        PlayEffectAt(resourcePath, transform.position, destroyAfter);
    }
}
/// <summary>
/// 我方随从系统 - 不会攻击，但会被敌人攻击并阻挡敌人
/// </summary>
public class Ally : Character
{
    [Header("属性")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveDuration = 0.25f;

    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D allyCollider;
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] protected SpriteRenderAnim spriteRenderAnim; // Sprite动画组件

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
        if (spriteRenderAnim == null)
            spriteRenderAnim = GetComponentInChildren<SpriteRenderAnim>();
        
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

        // 初始化动画系统
        if (SpriteRenderAnim.HasAnimationFolder("Ally") && spriteRenderAnim != null)
        {
            // 设置identifier并播放idle动画
            spriteRenderAnim.SetIdentifier("Ally");
            spriteRenderAnim.PlayIdle();
        }
        else if (spriteRenderer != null)
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

        ShowSpawnEffect();
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

        // 检查死亡
        bool willDie = currentHealth <= 0;

        if (!willDie)
            PlayEffectAtSelf("effect/hit");

        // 播放受伤动画（会自动切换到idle）
        TryPlayHurtAnimation();

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
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/PlayerStatus/sfx_ally_damaged");
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        PlayEffectAtSelf("effect/allyPoof");

        MainGameManager.NotifyAllyDied();

        if (RuneManager.Instance != null)
            RuneManager.Instance.TryExplodeAllyOnDeath(this);

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

                AllyManager allyManager = FindObjectOfType<AllyManager>();
                if (allyManager != null)
                    allyManager.RemoveAlly(this);
            });
    }

    /// <summary>
    /// 同时提高当前生命和最大生命。
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        if (isDead || amount <= 0)
            return;

        maxHealth += amount;
        currentHealth += amount;

        if (healthBar != null)
            healthBar.UpdateHealthBar(currentHealth, maxHealth);

        DamageNumber.CreateDamageNumber(amount, transform.position, true);
    }

    /// <summary>
    /// 将随从移动到新的棋盘位置，返回预计移动时间。
    /// </summary>
    public float MoveTo(Vector2Int newGridPosition)
    {
        if (isDead || boardManager == null)
            return 0f;

        gridPosition = newGridPosition;
        Vector3 worldPosition = boardManager.GridToWorldPosition(newGridPosition);
        if (enemyManager != null)
            worldPosition += new Vector3(0, enemyManager.SpawnOffsetY, 0);

        float duration = Mathf.Max(0.05f, moveDuration);
        transform.DOKill(false);
        transform.DOMove(worldPosition, duration)
            .SetEase(Ease.OutQuad);
        return duration;
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
    
    /// <summary>
    /// 尝试播放攻击动画
    /// </summary>
    public void TryPlayAtkAnimation()
    {
        if (SpriteRenderAnim.HasAnimationFolder("Ally") && spriteRenderAnim != null)
        {
            spriteRenderAnim.SetIdentifier("Ally");
            spriteRenderAnim.PlayAtk();
        }
    }
    
    /// <summary>
    /// 尝试播放受伤动画
    /// </summary>
    private void TryPlayHurtAnimation()
    {
        if (spriteRenderAnim != null)
        {
            // 如果有动画文件夹，设置identifier；否则使用当前sprite执行闪烁
            if (SpriteRenderAnim.HasAnimationFolder("Ally"))
            {
                spriteRenderAnim.SetIdentifier("Ally");
            }
            spriteRenderAnim.PlayHurt();
        }
    }
}

