using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人管理器
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int minEnemyCount = 1;
    [SerializeField] private int maxEnemyCount = 3;

    [Header("生成区域")]
    [SerializeField] private float spawnOffsetX = 1f;
    [SerializeField] private float spawnOffsetY = 1f;

    [Header("血条设置")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Canvas healthBarCanvas;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private BoardManager boardManager;
    private Transform enemyParent;

    public List<Enemy> ActiveEnemies => activeEnemies;
    public int EnemyCount => activeEnemies.Count;

    private void Awake()
    {
        enemyParent = new GameObject("Enemies").transform;
        enemyParent.SetParent(transform);

        // 确保有血条Canvas
        if (healthBarCanvas == null)
        {
            GameObject canvasObj = new GameObject("EnemyHealthBarCanvas");
            healthBarCanvas = canvasObj.AddComponent<Canvas>();
            healthBarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.transform.SetParent(transform);
        }
    }

    public void Init(BoardManager board)
    {
        boardManager = board;
    }

    /// <summary>
    /// 清空所有敌人
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }

    /// <summary>
    /// 在棋盘右半部分随机生成敌人
    /// </summary>
    public void SpawnEnemiesRandomly()
    {
        if (boardManager == null || enemyPrefab == null)
        {
            Debug.LogError("BoardManager or enemyPrefab not set!");
            return;
        }

        int enemyCount = Random.Range(minEnemyCount, maxEnemyCount + 1);
        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;

        // 右半部分的x范围
        int rightHalfStartX = boardWidth / 2;
        int rightHalfEndX = boardWidth - 1;

        for (int i = 0; i < enemyCount; i++)
        {
            // 随机在右半部分生成
            int x = Random.Range(rightHalfStartX, rightHalfEndX + 1);
            int y = Random.Range(0, boardHeight);

            Vector2Int gridPos = new Vector2Int(x, y);
            Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
            // 敌人应该在格子上方或旁边，但spawnOffsetY应该是向上的偏移，而不是超出棋盘
            // 如果spawnOffsetX和spawnOffsetY太大，会让敌人生成在棋盘外面
            // 可以设置较小的偏移，或者设置为0让敌人生成在格子位置
            worldPos += new Vector3(0, spawnOffsetY, 0); // 只在Y方向偏移（上方），X方向不偏移

            GameObject enemyObj = Instantiate(enemyPrefab, worldPos, Quaternion.identity, enemyParent);
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = enemyObj.AddComponent<Enemy>();
            }

            enemy.Init(gridPos);
            
            // 创建血条
            CreateHealthBar(enemy);
            
            activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// 刷新一个新敌人
    /// </summary>
    public void SpawnNewEnemy()
    {
        if (boardManager == null || enemyPrefab == null)
            return;

        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;

        // 在最右侧随机位置生成
        int x = boardWidth - 1;
        int y = Random.Range(0, boardHeight);

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
        // 敌人应该在格子上方或旁边，但spawnOffsetX应该设置为0或很小的值
        worldPos += new Vector3(0, spawnOffsetY, 0); // 只在Y方向偏移（上方），X方向不偏移

        GameObject enemyObj = Instantiate(enemyPrefab, worldPos, Quaternion.identity, enemyParent);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        enemy.Init(gridPos);
        
        // 创建血条
        CreateHealthBar(enemy);
        
        activeEnemies.Add(enemy);
    }

    /// <summary>
    /// 为敌人创建血条
    /// </summary>
    private void CreateHealthBar(Enemy enemy)
    {
        if (healthBarPrefab == null || healthBarCanvas == null)
        {
            Debug.LogWarning("HealthBar prefab or canvas not set!");
            return;
        }

        GameObject healthBarObj = Instantiate(healthBarPrefab, healthBarCanvas.transform);
        EnemyHealthBar healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            healthBar = healthBarObj.AddComponent<EnemyHealthBar>();
        }

        enemy.SetHealthBar(healthBar);
    }

    /// <summary>
    /// 所有敌人向左移动
    /// </summary>
    public void MoveAllEnemiesLeft(float distance = 1f, float duration = 0.5f)
    {
        List<Enemy> enemiesToRemove = new List<Enemy>();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            enemy.MoveLeft(distance, duration);
        }

        // 等待移动完成后检查边缘
        DOVirtual.DelayedCall(duration + 0.1f, () =>
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && !enemy.IsDead && enemy.IsAtLeftEdge())
                {
                    enemiesToRemove.Add(enemy);
                }
            }

            // 移除死亡的敌人和到达边缘的敌人
            foreach (var enemy in enemiesToRemove)
            {
                if (enemy != null)
                {
                    activeEnemies.Remove(enemy);
                    if (enemy.IsDead)
                    {
                        Destroy(enemy.gameObject, 1f);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 检查是否有敌人到达最左侧
    /// </summary>
    public bool HasEnemyAtLeftEdge()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead && enemy.IsAtLeftEdge())
            {
                return true;
            }
        }
        return false;
    }
}

