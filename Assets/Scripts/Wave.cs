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

    public float Duration => waveDuration; // 获取波浪持续时间

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
    public void Init(Vector3 spawnPosition, float distance = 10f)
    {
        startPosition = spawnPosition;
        travelDistance = distance;
        targetPosition = spawnPosition + Vector3.right * travelDistance;
        hitEnemies.Clear();

        transform.position = spawnPosition;
        gameObject.SetActive(true);

        // 确保Collider2D设置为Trigger
        if (waveCollider != null)
        {
            waveCollider.isTrigger = true;
        }

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
        
        if (enemy != null && !hitEnemies.Contains(enemy) && !enemy.IsDead)
        {
            hitEnemies.Add(enemy);
            Vector3 direction = (enemy.transform.position - transform.position).normalized;
            Debug.Log($"Wave hit enemy: {enemy.name}, dealing {damage} damage");
            enemy.TakeDamage((int)damage, direction);
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


