using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人管理器
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] public GameObject enemyPrefab; // 改为public以便Enemy类访问
    [SerializeField] private int minEnemyCount = 1;
    [SerializeField] private int maxEnemyCount = 3;

    [Header("生成区域")]
    [SerializeField] private float spawnOffsetX = 1f;
    [SerializeField] private float spawnOffsetY = 1f;

    public float SpawnOffsetY => spawnOffsetY;

    [Header("血条设置")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Canvas healthBarCanvas;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private BoardManager boardManager;
    private Transform enemyParent;
    
    // 分批次加载敌人相关
    private LevelInfo currentLevelInfo;
    private List<EnemySpawnInfo> remainingEnemies = new List<EnemySpawnInfo>(); // 剩余的敌人列表
    private int currentSpawnIndex = 0; // 当前生成索引
    private int deadEnemyCount = 0; // 已死亡的敌人数量

    public List<Enemy> ActiveEnemies => activeEnemies;
    public int EnemyCount => activeEnemies.Count;
    
    /// <summary>
    /// 获取总敌人数（包括已生成和未生成的）
    /// </summary>
    public int GetTotalEnemyCount()
    {
        // 如果使用关卡信息生成敌人，总数为remainingEnemies.Count
        if (currentLevelInfo != null)
        {
            return remainingEnemies.Count;
        }
        
        // 如果使用随机生成，总数为当前活跃敌人数 + 已死亡敌人数（因为所有敌人都在开始时生成）
        return activeEnemies.Count + deadEnemyCount;
    }
    
    /// <summary>
    /// 获取已死亡的敌人数量
    /// </summary>
    public int GetDeadEnemyCount()
    {
        return deadEnemyCount;
    }
    
    /// <summary>
    /// 获取剩余敌人数量（总数 - 已死亡）
    /// </summary>
    public int GetRemainingEnemyCount()
    {
        int total = GetTotalEnemyCount();
        int dead = GetDeadEnemyCount();
        return total - dead;
    }
    
    /// <summary>
    /// 移除已死亡的敌人（并更新死亡计数）
    /// </summary>
    public void RemoveDeadEnemy(Enemy enemy)
    {
        if (enemy != null && enemy.IsDead)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
                deadEnemyCount++;
                // 敌人被杀死时，更新enemies的当前值（通过增加deadEnemyCount）
            }
        }
    }
    
    /// <summary>
    /// 添加召唤的敌人（召唤的敌人需要更新remainingEnemies计数）
    /// </summary>
    public void AddSummonedEnemy()
    {
        // 召唤的敌人需要添加到remainingEnemies，因为未上场的敌人本来就在统计里
        remainingEnemies.Add(new EnemySpawnInfo
        {
            identifier = "",
            count = 1
        });
    }

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
        deadEnemyCount = 0;
        remainingEnemies.Clear();
        currentSpawnIndex = 0;
        currentLevelInfo = null;
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

        deadEnemyCount = 0; // 重置死亡计数
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
    /// 根据关卡信息生成敌人（分批次加载）
    /// </summary>
    public void SpawnEnemiesFromLevel(LevelInfo levelInfo)
    {
        if (boardManager == null || enemyPrefab == null || levelInfo == null)
        {
            Debug.LogError("BoardManager, enemyPrefab or levelInfo not set!");
            return;
        }

        currentLevelInfo = levelInfo;
        deadEnemyCount = 0; // 重置死亡计数
        
        // 解析敌人信息
        List<EnemySpawnInfo> allSpawnInfos = LevelManager.Instance.ParseEnemies(levelInfo.enemies);
        
        // 初始化剩余敌人列表（展开所有敌人）
        remainingEnemies.Clear();
        foreach (var spawnInfo in allSpawnInfos)
        {
            if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(spawnInfo.identifier))
            {
                Debug.LogWarning($"Enemy identifier not found: {spawnInfo.identifier}");
                continue;
            }
            
            // 将每个敌人展开到列表中
            for (int i = 0; i < spawnInfo.count; i++)
            {
                remainingEnemies.Add(new EnemySpawnInfo
                {
                    identifier = spawnInfo.identifier,
                    count = 1
                });
            }
        }
        
        currentSpawnIndex = 0;
        
        // 根据startEnemyCount先加载初始敌人
        int startCount = Mathf.Min(levelInfo.startEnemyCount, remainingEnemies.Count);
        for (int i = 0; i < startCount; i++)
        {
            SpawnNextEnemy();
        }
    }

    /// <summary>
    /// 生成下一个敌人（从剩余列表中）
    /// </summary>
    private void SpawnNextEnemy()
    {
        if (currentSpawnIndex >= remainingEnemies.Count || boardManager == null || enemyPrefab == null)
            return;

        EnemySpawnInfo spawnInfo = remainingEnemies[currentSpawnIndex];
        currentSpawnIndex++;

        // 从enemyInfoMap获取敌人信息
        if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(spawnInfo.identifier))
        {
            Debug.LogWarning($"Enemy identifier not found: {spawnInfo.identifier}");
            return;
        }

        EnemyInfo enemyInfo = CSVLoader.Instance.enemyInfoMap[spawnInfo.identifier];

        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;
        
        // 初始加载的敌人可以在右侧任意位置
        int rightHalfStartX = boardWidth / 2;
        int rightHalfEndX = boardWidth - 1;
        int x = Random.Range(rightHalfStartX, rightHalfEndX + 1);
        int y = Random.Range(0, boardHeight);

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
        worldPos += new Vector3(0, spawnOffsetY, 0);

        GameObject enemyObj = Instantiate(enemyPrefab, worldPos, Quaternion.identity, enemyParent);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        // 使用enemyInfo中的hp初始化
        enemy.Init(gridPos, enemyInfo.hp, enemyInfo);
        
        // 创建血条
        CreateHealthBar(enemy);
        
        activeEnemies.Add(enemy);
    }

    /// <summary>
    /// 每回合生成一个新敌人
    /// </summary>
    public void SpawnEnemyEachTurn()
    {
        if (currentSpawnIndex < remainingEnemies.Count)
        {
            SpawnNextEnemy();
        }
    }

    /// <summary>
    /// 刷新一个新敌人（从最右端右边生成，走到最右侧格子）
    /// </summary>
    public void SpawnNewEnemy()
    {
        if (boardManager == null || enemyPrefab == null)
            return;

        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;

        // 在最右侧随机位置
        int targetX = boardWidth - 1;
        int y = Random.Range(0, boardHeight);
        Vector2Int targetGridPos = new Vector2Int(targetX, y);

        // 从最右端右边生成（在棋盘外面）
        Vector3 spawnWorldPos = boardManager.GridToWorldPosition(new Vector2Int(boardWidth, y));
        spawnWorldPos += new Vector3(0, spawnOffsetY, 0);

        GameObject enemyObj = Instantiate(enemyPrefab, spawnWorldPos, Quaternion.identity, enemyParent);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        // 初始化敌人（使用目标位置）
        enemy.Init(targetGridPos);
        
        // 创建血条
        CreateHealthBar(enemy);
        
        activeEnemies.Add(enemy);
        
        // 移动到目标位置
        Vector3 targetWorldPos = boardManager.GridToWorldPosition(targetGridPos);
        targetWorldPos += new Vector3(0, spawnOffsetY, 0);
        
        float moveDuration = 0.5f;
        enemyObj.transform.DOMove(targetWorldPos, moveDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 为敌人创建血条
    /// </summary>
    public void CreateHealthBar(Enemy enemy)
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
    /// 为随从创建血条
    /// </summary>
    public void CreateHealthBarForAlly(Ally ally)
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

        ally.SetHealthBar(healthBar);
    }

    /// <summary>
    /// 所有敌人行动（每回合调用）
    /// </summary>
    public void AllEnemiesTakeAction(float duration = 0.5f)
    {
        List<Enemy> enemiesToRemove = new List<Enemy>();

        foreach (var enemy in activeEnemies.ToList())
        {
            if (enemy == null || enemy.IsDead)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            // 使用新的TakeAction方法
            enemy.TakeAction();
        }

        // 等待行动完成后检查死亡（不再移除到达边缘的敌人，因为敌人攻击后应该继续存在）
        DOVirtual.DelayedCall(duration + 0.1f, () =>
        {
            // 只移除死亡的敌人
            foreach (var enemy in enemiesToRemove)
            {
                if (enemy != null)
                {
                    activeEnemies.Remove(enemy);
                    if (enemy.IsDead)
                    {
                        deadEnemyCount++; // 增加已死亡敌人计数
                        Destroy(enemy.gameObject, 1f);
                    }
                }
            }
        });
    }
    
    /// <summary>
    /// 所有敌人向左移动（保留旧方法以兼容）
    /// </summary>
    public void MoveAllEnemiesLeft(float distance = 1f, float duration = 0.5f)
    {
        // 使用新的行动系统
        AllEnemiesTakeAction(duration);
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

    /// <summary>
    /// 检查是否所有敌人都已死亡
    /// </summary>
    public bool AreAllEnemiesDead()
    {
        if (activeEnemies.Count == 0)
        {
            return true;
        }

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检查是否还有剩余的敌人可以被生成
    /// </summary>
    public bool HasRemainingEnemiesToSpawn()
    {
        // 如果使用关卡信息生成敌人，检查是否还有剩余的敌人
        if (currentLevelInfo != null)
        {
            return currentSpawnIndex < remainingEnemies.Count;
        }
        
        // 如果使用随机生成，没有剩余敌人列表，返回false（随机生成模式下，所有敌人都在开始时生成）
        return false;
    }

    /// <summary>
    /// 检查是否可以完成关卡（所有敌人死亡且没有剩余敌人可生成）
    /// </summary>
    public bool CanCompleteLevel()
    {
        // 首先检查所有活跃敌人是否都死了
        if (!AreAllEnemiesDead())
        {
            return false;
        }
        
        // 然后检查是否还有剩余的敌人可以被生成
        if (HasRemainingEnemiesToSpawn())
        {
            return false;
        }
        
        return true;
    }
}

