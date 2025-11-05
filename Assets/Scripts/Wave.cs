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
    public void Init(Vector3 spawnPosition, TileColor color, float distance = 10f)
    {
        startPosition = spawnPosition;
        travelDistance = distance;
        targetPosition = spawnPosition + Vector3.right * travelDistance;
        hitEnemies.Clear();
        waveColor = color;
        penetrateCount = 0;

        transform.position = spawnPosition;
        gameObject.SetActive(true);

        // 确保Collider2D设置为Trigger
        if (waveCollider != null)
        {
            waveCollider.isTrigger = true;
        }

        // 应用技能效果
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

        transform.DOMove(targetPosition, waveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                DestroyWave();
            });
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
                    // 下一个波浪伤害加成（这个需要在MainGameManager中处理）
                    buffNextDamage = true;
                    break;
                    
                case "wavePenetrate":
                    // 穿透次数增加
                    penetrateCount = value;
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
        Debug.Log($"Wave OnTriggerEnter2D: {collision.name}");
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
                }
                
                Vector3 direction = (enemy.transform.position - transform.position).normalized;
                float finalDamage = damage;
                
                // 应用击中时的技能效果
                ApplyHitSkillEffects(enemy, ref finalDamage, direction);
                
                Debug.Log($"Wave hit enemy: {enemy.name}, dealing {finalDamage} damage");
                enemy.TakeDamage((int)finalDamage, direction);
                
                // 穿透次数减1
                if (penetrateCount > 0)
                {
                    penetrateCount--;
                }
                
                // 如果没有穿透能力或穿透次数为0，销毁波浪
                if (penetrateCount <= 0)
                {
                    DestroyWave();
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

        foreach (var skill in skills)
        {
            int value = SkillManager.Instance.GetSkillValue(skill.identifier);
            
            switch (skill.effect)
            {
                case "damageBottom":
                    // 如果击中最右侧，对整行敌人造成伤害
                    BoardManager boardManager = FindObjectOfType<BoardManager>();
                    if (boardManager != null && enemy.GridPosition.x >= boardManager.Width - 1)
                    {
                        // 这个效果需要在MainGameManager中处理，因为需要知道整行的敌人
                        // 这里暂时只增加伤害
                        finalDamage = finalDamage * (1f + value / 100f);
                    }
                    break;
                    
                case "hitEnemyBack":
                    // 击退敌人
                    // 这个效果在Enemy.TakeDamage中已经有击退，这里可以调整力度
                    break;
                    
                case "healWhenHit":
                    // 击中敌人时回血（这个需要在MainGameManager中处理）
                    break;
            }
        }
    }

    /// <summary>
    /// 销毁波浪
    /// </summary>
    private void DestroyWave()
    {
        transform.DOKill();
        
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


