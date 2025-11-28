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
    public List<EnemySpawnInfo> remainingEnemies = new List<EnemySpawnInfo>(); // 剩余的敌人列表
    public int currentSpawnIndex = 0; // 当前生成索引
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

        // 敌人生成在底线（最右侧，x = boardWidth - 1）
        int x = boardWidth - 1;

        for (int i = 0; i < enemyCount; i++)
        {
            // 找到一个不与其他敌人重叠的y位置
            int y = FindAvailableYPosition(x, boardHeight);
            if (y < 0)
            {
                // 如果找不到可用位置，跳过这个敌人
                Debug.LogWarning($"无法找到可用的敌人生成位置，跳过第 {i + 1} 个敌人");
                continue;
            }

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
        
        // 敌人生成在底线（最右侧，x = boardWidth - 1）
        int x = boardWidth - 1;
        
        // 找到一个不与其他敌人重叠的y位置
        int y = FindAvailableYPosition(x, boardHeight);
        if (y < 0)
        {
            // 如果找不到可用位置，不生成敌人
            Debug.LogWarning("无法找到可用的敌人生成位置");
            return;
        }

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
        worldPos += new Vector3(0, spawnOffsetY, 0);

        GameObject enemyObj = Instantiate(enemyPrefab, worldPos, Quaternion.identity, enemyParent);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        // 根据difficulty计算敌人属性
        int difficulty = currentLevelInfo != null ? currentLevelInfo.difficulty : 0;
        // 困难模式：所有level的difficulty自动加1
        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode())
        {
            difficulty += 1;
        }
        int calculatedHP = enemyInfo.hp + difficulty * enemyInfo.hpIncrease;
        int calculatedAttack = enemyInfo.attack + difficulty * enemyInfo.attackIncrease;
        
        // 使用计算后的hp初始化
        enemy.Init(gridPos, calculatedHP, enemyInfo);
        // 设置计算后的攻击力
        enemy.SetAttack(calculatedAttack);
        
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

        // 在最右侧（底线）生成
        int targetX = boardWidth - 1;
        
        // 找到一个不与其他敌人重叠的y位置
        int y = FindAvailableYPosition(targetX, boardHeight);
        if (y < 0)
        {
            // 如果找不到可用位置，不生成敌人
            Debug.LogWarning("无法找到可用的敌人生成位置");
            return;
        }
        
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
    /// 按批次执行敌人行动：先生成新敌人，然后所有移动的敌人移动，然后所有攻击的敌人攻击，然后所有使用特殊技能的敌人按技能顺序执行
    /// </summary>
    /// <param name="onComplete">所有行动完成后的回调</param>
    public void ExecuteEnemyTurnBatch(System.Action onComplete = null)
    {
        // 1. 先生成新敌人（如果需要）
        // 注意：生成新敌人应该在外部调用，这里只处理已存在的敌人行动
        
        // 2. 分类敌人
        List<Enemy> moveEnemies = new List<Enemy>();
        List<Enemy> attackEnemies = new List<Enemy>();
        List<Enemy> healEnemies = new List<Enemy>();
        List<Enemy> createFogEnemies = new List<Enemy>();
        List<Enemy> dirtyWaterEnemies = new List<Enemy>();
        List<Enemy> summonEnemies = new List<Enemy>();
        List<Enemy> shieldEnemies = new List<Enemy>(); // shield怪物列表
        
        foreach (var enemy in activeEnemies.ToList())
        {
            if (enemy == null || enemy.IsDead)
                continue;
            
            // 检查是否是shield怪物
            if (enemy.IsShieldEnemy())
            {
                shieldEnemies.Add(enemy);
            }
            
            // 先减少冷却时间（模拟TakeAction中的冷却减少逻辑）
            enemy.ReduceCooldown();
            
            // 检查敌人是否有主动技能且冷却完成
            bool hasActiveSkill = enemy.HasActiveSkillReady();
            string skillName = enemy.GetCurrentSkill();
            
            if (hasActiveSkill)
            {
                if (skillName == "heal")
                {
                    // 检查是否有血量不满的敌人可以治疗
                    bool hasInjuredEnemy = HasInjuredEnemy();
                    if (hasInjuredEnemy)
                    {
                        healEnemies.Add(enemy);
                    }
                    else
                    {
                        // 如果所有敌人都是满血，执行移动
                        if (enemy.IsInAttackRange())
                        {
                            attackEnemies.Add(enemy);
                        }
                        else
                        {
                            moveEnemies.Add(enemy);
                        }
                    }
                }
                else if (skillName == "createFog")
                {
                    createFogEnemies.Add(enemy);
                }
                else if (skillName == "dirtyWater")
                {
                    dirtyWaterEnemies.Add(enemy);
                }
                else if (skillName != null && skillName.StartsWith("summon"))
                {
                    summonEnemies.Add(enemy);
                }
                else
                {
                    // 其他技能，按攻击处理
                    if (enemy.IsInAttackRange())
                    {
                        attackEnemies.Add(enemy);
                    }
                    else
                    {
                        moveEnemies.Add(enemy);
                    }
                }
            }
            else
            {
                // 没有主动技能或冷却未完成，检查攻击范围
                if (enemy.IsInAttackRange())
                {
                    attackEnemies.Add(enemy);
                }
                else
                {
                    moveEnemies.Add(enemy);
                }
            }
        }
        
        // 3. 按顺序执行
        float actionDelay = 0.5f; // 每个批次之间的延迟
        float currentDelay = 0f;
        
        // 3.1 所有移动的敌人移动
        if (moveEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in moveEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.MoveLeft();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay; // 移动持续时间 + 延迟
        }
        
        // 3.2 所有攻击的敌人攻击
        if (attackEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in attackEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.AttackPlayer();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay; // 攻击持续时间 + 延迟
        }
        
        // 3.3 所有使用特殊技能的敌人，按技能顺序执行
        // 3.3.1 先治疗
        if (healEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in healEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        // 如果heal技能没有可治疗的敌人，UseSkillDirectly会返回false
                        // 但这种情况不应该发生，因为我们在分类时已经检查过了
                        enemy.UseSkillDirectly();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay;
        }
        
        // 3.3.2 再生成云朵
        if (createFogEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in createFogEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.UseSkillDirectly();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay;
        }
        
        // 3.3.3 其他技能（dirtyWater, summon等）
        if (dirtyWaterEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in dirtyWaterEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.UseSkillDirectly();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay;
        }
        
        if (summonEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in summonEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.UseSkillDirectly();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay;
        }
        
        // 3.4 shield怪物执行防御操作（在移动/攻击完成后）
        if (shieldEnemies.Count > 0)
        {
            DOVirtual.DelayedCall(currentDelay, () =>
            {
                foreach (var enemy in shieldEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.PerformDefense();
                    }
                }
            });
            currentDelay += 0.5f + actionDelay; // 防御动画持续时间 + 延迟
        }
        
        // 4. 所有行动完成后调用回调
        DOVirtual.DelayedCall(currentDelay, () =>
        {
            onComplete?.Invoke();
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
    /// 检查是否有血量不满的敌人（用于heal技能判断）
    /// </summary>
    private bool HasInjuredEnemy()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                float healthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
                if (healthPercent < 1f)
                {
                    return true;
                }
            }
        }
        return false;
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
    
    /// <summary>
    /// 查找可用的Y位置（不与其他敌人重叠）
    /// </summary>
    public int FindAvailableYPosition(int x, int boardHeight)
    {
        // 收集所有已占用的y位置
        HashSet<int> occupiedY = new HashSet<int>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead && enemy.GridPosition.x == x)
            {
                occupiedY.Add(enemy.GridPosition.y);
            }
        }
        
        // 收集所有可用的y位置
        List<int> availableY = new List<int>();
        for (int y = 0; y < boardHeight; y++)
        {
            if (!occupiedY.Contains(y))
            {
                availableY.Add(y);
            }
        }
        
        // 如果没有可用位置，返回-1
        if (availableY.Count == 0)
        {
            return -1;
        }
        
        // 随机选择一个可用位置
        return availableY[Random.Range(0, availableY.Count)];
    }
    
    /// <summary>
    /// 更新方法：检测作弊按键
    /// </summary>
    private void Update()
    {
        // 按P键杀死所有敌人（作弊功能）
        if (Input.GetKeyDown(KeyCode.P))
        {
            KillAllEnemies();
        }
    }
    
    /// <summary>
    /// 杀死所有敌人（作弊功能）
    /// </summary>
    private void KillAllEnemies()
    {
        Debug.Log("作弊：杀死所有敌人");
        
        // 1. 杀死所有场上的敌人
        int killedCount = 0;
        foreach (var enemy in activeEnemies.ToList())
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.Die();
                killedCount++;
            }
        }
        
        // 2. 处理未上场的敌人：将它们也标记为死亡
        if (currentLevelInfo != null && remainingEnemies != null)
        {
            // 计算未上场的敌人数量
            int remainingCount = remainingEnemies.Count - currentSpawnIndex;
            if (remainingCount > 0)
            {
                // 将未上场的敌人也计入死亡
                deadEnemyCount += remainingCount;
                Debug.Log($"作弊：标记 {remainingCount} 个未上场的敌人为死亡");
            }
            
            // 清空剩余敌人列表，使 HasRemainingEnemiesToSpawn() 返回 false
            remainingEnemies.Clear();
            currentSpawnIndex = 0;
        }
        
        Debug.Log($"作弊完成：杀死了 {killedCount} 个场上敌人，剩余敌人数量：{GetRemainingEnemyCount()}");
        
        // 3. 延迟检查是否可以完成关卡（等待敌人死亡动画完成）
        DOVirtual.DelayedCall(0.5f, () =>
        {
            // 检查是否可以完成关卡
            if (CanCompleteLevel())
            {
                Debug.Log("作弊：所有敌人已死亡，触发战斗结束");
                // 通过MainGameManager触发战斗结束
                MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
                if (mainGameManager != null)
                {
                    mainGameManager.CompleteLevel();
                }
            }
        });
    }
}

