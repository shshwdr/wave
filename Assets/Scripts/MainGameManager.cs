using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 一场战斗的游戏控制 - 回合制战斗系统
/// </summary>
public class MainGameManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Camera mainCamera;
    private AllyManager allyManager;

    [Header("波浪设置")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform waveParent;

    [Header("游戏设置")]
    [SerializeField] private float tileMoveDistance = 1f;
    [SerializeField] private float enemyMoveDistance = 1f;
    [SerializeField] private float enemyMoveDuration = 0.5f;

    [Header("技能显示UI")]
    [SerializeField] private GameObject skillDisplayPanel;
    [SerializeField] private TMP_Text skillDisplayText;
    
    [Header("敌人描述显示UI")]
    [SerializeField] private GameObject enemyDescriptionPanel;
    [SerializeField] private TMP_Text enemyDescriptionText;

    public bool showShopAtBeginning;
    [SerializeField] public GameObject allyPrefab;
    [SerializeField] public GameObject allyProjectile;
    
    private enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Processing,        // 处理中（波浪攻击、敌人移动等）
        GameOver,
        LevelComplete      // 关卡完成
    }

    private GameState currentState = GameState.PlayerTurn;
    private bool isProcessing = false;  // 是否正在处理（波浪、移动等）
    private Vector2Int selectedTilePos = new Vector2Int(-1, -1);
    private Vector2Int dragStartPos = new Vector2Int(-1, -1);
    private bool isDragging = false;
    private Vector2Int currentHoverTilePos = new Vector2Int(-1, -1); // 当前鼠标悬停的格子
    
    // 任意位置交换相关
    private Vector2Int firstSwapTilePos = new Vector2Int(-1, -1);
    private bool waitingForSecondSwap = false;
    
    // 高亮显示相关
    private Vector2Int lastHighlightPos = new Vector2Int(-1, -1);
    private List<Vector2Int> highlightedTiles = new List<Vector2Int>();

    // 技能显示相关
    private TileColor currentDisplayColor = TileColor.Red;

    // 玩家等级（起始为0，打一场架升一级）
    private int playerLevel = 0;
    public int PlayerLevel => playerLevel;
    
    /// <summary>
    /// 玩家等级提升
    /// </summary>
    public void PlayerLevelUp()
    {
        playerLevel++;
        LevelManager.Instance.NextLevel();
        StartBattle();
    }

    // damageBottom技能触发管理（确保整个wave只触发一次）
    private static bool damageBottomTriggeredThisWave = false;
    private static int currentWaveGroupId = 0;

    // buffNextDamage技能管理（下一个wave的伤害加成）
    private static float nextWaveDamageMultiplier = 1f;

    // spawnAlly技能管理 - 跟踪每个wave group的总伤害
    private static Dictionary<int, float> waveGroupTotalDamage = new Dictionary<int, float>();
    private static Dictionary<int, int> waveGroupActiveWaveCount = new Dictionary<int, int>();
    private static Dictionary<int, TileColor> waveGroupColor = new Dictionary<int, TileColor>();
    
    // addDamageWhenPass技能管理 - 跟踪每个wave group的伤害加成
    private static Dictionary<int, float> waveGroupAddDamageWhenPass = new Dictionary<int, float>();
    
    // noAttackNoCost技能管理 - 跟踪每个wave group是否造成伤害
    private static Dictionary<int, bool> waveGroupHasDamage = new Dictionary<int, bool>();
    private static bool noAttackNoCostTriggeredThisTurn = false; // 一个回合只会触发一次
    private static HashSet<int> pendingWaveGroups = new HashSet<int>(); // 等待结算完成的wave group

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
        if (enemyManager == null)
            enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager != null)
            enemyManager.Init(boardManager);
            
        // 初始化AllyManager
        allyManager = FindObjectOfType<AllyManager>();
        if (allyManager == null)
        {
            GameObject allyManagerObj = new GameObject("AllyManager");
            allyManager = allyManagerObj.AddComponent<AllyManager>();
        }

        if (waveParent == null)
        {
            GameObject waveParentObj = new GameObject("Waves");
            waveParentObj.transform.SetParent(transform);
            waveParent = waveParentObj.transform;
        }

        // 初始化技能显示UI
        InitSkillDisplayUI();
        
        // 初始化敌人描述显示UI
        InitEnemyDescriptionUI();

        StartBattle();

        if (showShopAtBeginning)
        {
            SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
            skillMenu.ShowSkillSelection(
                () =>
                {
                    // 确认按钮点击，进入下一关
                    Debug.Log("确认配置，进入下一关");
                }
            );
        }
        // 初始打开商店
        
    }

    /// <summary>
    /// 初始化技能显示UI
    /// </summary>
    private void InitSkillDisplayUI()
    {
        if (skillDisplayPanel == null)
        {
            // 创建技能显示面板
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            skillDisplayPanel = new GameObject("SkillDisplayPanel");
            skillDisplayPanel.transform.SetParent(canvasObj.transform);
            RectTransform rectTransform = skillDisplayPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(400, 300);

            // 添加背景
            Image bg = skillDisplayPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // 添加文本
            GameObject textObj = new GameObject("SkillText");
            textObj.transform.SetParent(skillDisplayPanel.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            skillDisplayText = textObj.AddComponent<TextMeshProUGUI>();
            skillDisplayText.fontSize = 30;
            skillDisplayText.color = Color.white;
            skillDisplayText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (skillDisplayPanel != null)
        {
            skillDisplayPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化敌人描述显示UI
    /// </summary>
    private void InitEnemyDescriptionUI()
    {
        if (enemyDescriptionPanel == null)
        {
            // 创建敌人描述显示面板
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            enemyDescriptionPanel = new GameObject("EnemyDescriptionPanel");
            enemyDescriptionPanel.transform.SetParent(canvasObj.transform);
            RectTransform rectTransform = enemyDescriptionPanel.AddComponent<RectTransform>();
            // 右上角：anchorMin和anchorMax都是(1,1)，pivot是(1,1)
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-20, -20); // 距离右上角20像素
            rectTransform.sizeDelta = new Vector2(400, 300);

            // 添加背景
            Image bg = enemyDescriptionPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // 添加文本
            GameObject textObj = new GameObject("EnemyDescriptionText");
            textObj.transform.SetParent(enemyDescriptionPanel.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            enemyDescriptionText = textObj.AddComponent<TextMeshProUGUI>();
            enemyDescriptionText.fontSize = 30;
            enemyDescriptionText.color = Color.white;
            enemyDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        // 重置游戏状态
        isProcessing = false;
        currentState = GameState.PlayerTurn;
        noAttackNoCostTriggeredThisTurn = false; // 重置noAttackNoCost触发标志
        isDragging = false;
        waitingForSecondSwap = false;
        dragStartPos = new Vector2Int(-1, -1);
        firstSwapTilePos = new Vector2Int(-1, -1);
        currentHoverTilePos = new Vector2Int(-1, -1);
        lastHighlightPos = new Vector2Int(-1, -1);
        ClearHighlights();

        // 初始化PlayerManager并恢复交换次数
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.StartBattle();
        }

        // 清空棋盘
        if (boardManager != null)
        {
            boardManager.ClearBoard();
            boardManager.InitializeBoard();
            boardManager.GenerateRandomColors();
        }

        // 清空敌人
        if (enemyManager != null)
        {
            enemyManager.ClearAllEnemies();
        }

        // 从关卡管理器获取关卡信息并生成敌人
        LevelInfo levelInfo = LevelManager.Instance.GetNextLevel(playerLevel);
        if (levelInfo != null && enemyManager != null)
        {
            enemyManager.SpawnEnemiesFromLevel(levelInfo);
        }
        else if (enemyManager != null)
        {
            // 如果没有关卡信息，使用随机生成
            enemyManager.SpawnEnemiesRandomly();
        }

        currentState = GameState.PlayerTurn;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
        if (currentState == GameState.GameOver)
            return;

        // 敌人攻击逻辑现在由Enemy.TakeAction()处理，不再需要在这里检查

        // 检查玩家是否死亡
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsDead)
        {
            GameOver();
            return;
        }


        // 玩家回合 - 处理输入和鼠标悬停（只有在没有处理中时）
        if (currentState == GameState.PlayerTurn && !isProcessing)
        {
            HandlePlayerInput();
            HandleMouseHover();
        }
        else if (isProcessing)
        {
            // 处理中时清除高亮
            ClearHighlights();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandlePlayerInput()
    {
        // 处理中时不允许操作
        if (isProcessing)
            return;

        // Shift + 鼠标左键 - 任意位置交换
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    if (!waitingForSecondSwap)
                    {
                        // 选择第一个格子
                        firstSwapTilePos = gridPos;
                        waitingForSecondSwap = true;
                        HighlightTile(gridPos, Color.yellow);
                    }
                    else
                    {
                        // 选择第二个格子并交换
                        if (gridPos != firstSwapTilePos)
                        {
                            // 检查是否可以交换
                            if (PlayerManager.Instance != null && !PlayerManager.Instance.CanSwap())
                            {
                                Debug.Log("交换次数不足！");
                                waitingForSecondSwap = false;
                                firstSwapTilePos = new Vector2Int(-1, -1);
                                return;
                            }

                            ClearHighlights();
                            
                            // 消耗交换次数
                            if (PlayerManager.Instance != null)
                            {
                                PlayerManager.Instance.ConsumeSwap();
                            }
                            
                            // 标记为处理中
                            isProcessing = true;
                            currentState = GameState.Processing;
                            
                            boardManager.SwapTiles(firstSwapTilePos, gridPos);
                            
                            // 交换不进入敌人回合，直接完成
                            DOVirtual.DelayedCall(0.5f, () =>
                            {
                                isProcessing = false;
                                currentState = GameState.PlayerTurn;
                            });
                        }
                        waitingForSecondSwap = false;
                        firstSwapTilePos = new Vector2Int(-1, -1);
                    }
                }
            }
        }
        else
        {
            // 取消任意位置交换选择
            if (waitingForSecondSwap)
            {
                ClearHighlights();
                waitingForSecondSwap = false;
                firstSwapTilePos = new Vector2Int(-1, -1);
            }

            // 鼠标左键 - 拖动交换（任意位置）
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    dragStartPos = gridPos;
                    isDragging = true;
                    selectedTilePos = gridPos;
                    
                    // 显示开始格子的框
                    TileCell startTile = boardManager.GetTile(gridPos);
                    if (startTile != null)
                    {
                        startTile.ShowFrame(true);
                    }
                }
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                // 拖动中：跟踪鼠标所在的新格子
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    // 如果鼠标移动到新的格子上
                    if (gridPos != currentHoverTilePos)
                    {
                        // 隐藏之前格子的框（如果不是开始格子）
                        if (currentHoverTilePos.x >= 0 && currentHoverTilePos != dragStartPos)
                        {
                            TileCell prevTile = boardManager.GetTile(currentHoverTilePos);
                            if (prevTile != null)
                            {
                                prevTile.ShowFrame(false);
                            }
                        }
                        
                        // 显示新格子的框（如果不是开始格子）
                        if (gridPos != dragStartPos)
                        {
                            TileCell newTile = boardManager.GetTile(gridPos);
                            if (newTile != null)
                            {
                                newTile.ShowFrame(true);
                            }
                        }
                        
                        currentHoverTilePos = gridPos;
                    }
                }
                else
                {
                    // 鼠标不在有效位置上，隐藏当前悬停格子的框
                    if (currentHoverTilePos.x >= 0 && currentHoverTilePos != dragStartPos)
                    {
                        TileCell hoverTile = boardManager.GetTile(currentHoverTilePos);
                        if (hoverTile != null)
                        {
                            hoverTile.ShowFrame(false);
                        }
                    }
                    currentHoverTilePos = new Vector2Int(-1, -1);
                }
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector2Int gridPos = GetMouseGridPosition();
                
                // 隐藏开始格子的框
                if (dragStartPos.x >= 0)
                {
                    TileCell startTile = boardManager.GetTile(dragStartPos);
                    if (startTile != null)
                    {
                        startTile.ShowFrame(false);
                    }
                }
                
                // 隐藏当前悬停格子的框（如果不是开始格子）
                if (currentHoverTilePos.x >= 0 && currentHoverTilePos != dragStartPos)
                {
                    TileCell hoverTile = boardManager.GetTile(currentHoverTilePos);
                    if (hoverTile != null)
                    {
                        hoverTile.ShowFrame(false);
                    }
                }
                
                // 如果选中了两个不同的格子，交换位置
                if (boardManager != null && 
                    boardManager.IsValidPosition(gridPos) && 
                    gridPos != dragStartPos)
                {
                    // 检查是否可以交换
                    if (PlayerManager.Instance != null && !PlayerManager.Instance.CanSwap())
                    {
                        Debug.Log("交换次数不足！");
                        isDragging = false;
                        currentHoverTilePos = new Vector2Int(-1, -1);
                        return;
                    }

                    // 消耗交换次数
                    if (PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.ConsumeSwap();
                    }

                    // 标记为处理中
                    isProcessing = true;
                    currentState = GameState.Processing;

                    // 交换格子
                    boardManager.SwapTiles(dragStartPos, gridPos);
                    
                    // 交换不进入敌人回合，直接完成
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        isProcessing = false;
                        currentState = GameState.PlayerTurn;
                    });
                }
                
                // 重置拖拽状态
                isDragging = false;
                currentHoverTilePos = new Vector2Int(-1, -1);
            }
        }

        // 鼠标右键 - 消除同色格子
        if (Input.GetMouseButtonDown(1))
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                ClearHighlights();
                EliminateConnectedTiles(gridPos);
            }
        }
    }

    /// <summary>
    /// 处理鼠标悬停
    /// </summary>
    private void HandleMouseHover()
    {
        // 如果技能选择界面打开，不响应hover
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu != null && skillMenu.IsActive)
        {
            return;
        }

        // 首先检查是否悬停在敌人上
        Enemy hoveredEnemy = GetEnemyUnderMouse();
        if (hoveredEnemy != null && !hoveredEnemy.IsDead && hoveredEnemy.EnemyInfo != null)
        {
            // 显示敌人描述
            UpdateEnemyDescription(hoveredEnemy);
            // 隐藏技能显示
            if (skillDisplayPanel != null)
            {
                skillDisplayPanel.SetActive(false);
            }
            return;
        }
        else
        {
            // 没有悬停在敌人上，隐藏敌人描述
            if (enemyDescriptionPanel != null)
            {
                enemyDescriptionPanel.SetActive(false);
            }
        }
        
        Vector2Int gridPos = GetMouseGridPosition();
        if (boardManager != null && boardManager.IsValidPosition(gridPos))
        {
            // 如果鼠标位置改变了，更新高亮和技能显示
            if (gridPos != lastHighlightPos && !waitingForSecondSwap)
            {
                UpdateHighlightTiles(gridPos);
                UpdateSkillDisplay(gridPos);
                lastHighlightPos = gridPos;
            }
        }
        else
        {
            // 鼠标不在有效位置，隐藏技能显示
            if (skillDisplayPanel != null)
            {
                skillDisplayPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 获取鼠标下的敌人
    /// </summary>
    private Enemy GetEnemyUnderMouse()
    {
        if (mainCamera == null || enemyManager == null)
            return null;
            
        // 将鼠标屏幕坐标转换为世界坐标
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.nearClipPlane;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        
        // 使用OverlapPoint检测鼠标位置下的碰撞体
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);
        if (hitCollider != null)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy == null)
            {
                // 尝试从父对象获取
                enemy = hitCollider.GetComponentInParent<Enemy>();
            }
            if (enemy != null && !enemy.IsDead)
            {
                return enemy;
            }
        }
        
        // 如果OverlapPoint没有检测到，尝试遍历所有敌人检查距离
        // 这对于某些collider设置可能更可靠
        float checkRadius = 0.5f; // 检查半径
        Collider2D[] colliders = Physics2D.OverlapCircleAll(mouseWorldPos, checkRadius);
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = collider.GetComponentInParent<Enemy>();
            }
            if (enemy != null && !enemy.IsDead)
            {
                return enemy;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 更新敌人描述显示
    /// </summary>
    private void UpdateEnemyDescription(Enemy enemy)
    {
        if (enemy == null || enemy.EnemyInfo == null || enemyDescriptionPanel == null || enemyDescriptionText == null)
        {
            if (enemyDescriptionPanel != null)
            {
                enemyDescriptionPanel.SetActive(false);
            }
            return;
        }
        
        string description = enemy.EnemyInfo.description;
        if (!string.IsNullOrEmpty(description))
        {
            enemyDescriptionText.text = description;
            enemyDescriptionPanel.SetActive(true);
        }
        else
        {
            enemyDescriptionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 更新技能显示
    /// </summary>
    private void UpdateSkillDisplay(Vector2Int gridPos)
    {
        if (boardManager == null || skillDisplayPanel == null || skillDisplayText == null)
            return;

        TileCell tile = boardManager.GetTile(gridPos);
        if (tile == null)
        {
            skillDisplayPanel.SetActive(false);
            return;
        }

        // 获取当前格子的颜色
        TileColor tileColor = tile.Color;
        int colorIndex = (int)tileColor; // TileColor枚举值：Red=0, Yellow=1, Blue=2, Green=3

        // 从PlayerManager获取该颜色wave配置的技能列表
        if (PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

            if (skillIdentifiers.Count > 0)
            {
                // 显示技能描述
                string skillText = "";
                foreach (var identifier in skillIdentifiers)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
                        skillText += description + "\n";
                    }
                }

                if (!string.IsNullOrEmpty(skillText))
                {
                    skillDisplayText.text = skillText;
                    skillDisplayPanel.SetActive(true);
                }
                else
                {
                    skillDisplayPanel.SetActive(false);
                }
            }
            else
            {
                skillDisplayPanel.SetActive(false);
            }
        }
        else
        {
            skillDisplayPanel.SetActive(false);
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
    /// 更新高亮显示的格子
    /// </summary>
    private void UpdateHighlightTiles(Vector2Int mousePos)
    {
        ClearHighlights();

        if (boardManager == null)
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(mousePos);

        // 高亮所有连通的格子
        foreach (var pos in connectedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            if (tile != null)
            {
                tile.SetHighlight(true);
                //tile.SetHighlightColor(Color.cyan); // 使用青色高亮
                highlightedTiles.Add(pos);
            }
        }
    }

    /// <summary>
    /// 高亮单个格子
    /// </summary>
    private void HighlightTile(Vector2Int pos, Color color)
    {
        if (boardManager == null)
            return;

        TileCell tile = boardManager.GetTile(pos);
        if (tile != null)
        {
            tile.SetHighlight(true);
            tile.SetHighlightColor(color);
            highlightedTiles.Add(pos);
        }
    }

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    private void ClearHighlights()
    {
        if (boardManager == null)
            return;

        foreach (var pos in highlightedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            if (tile != null)
            {
                tile.SetHighlight(false);
            }
        }
        highlightedTiles.Clear();
    }

    /// <summary>
    /// 获取鼠标所在的网格位置
    /// </summary>
    private Vector2Int GetMouseGridPosition()
    {
        if (mainCamera == null)
            return Vector2Int.zero;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (boardManager != null)
        {
            return boardManager.WorldToGridPosition(mouseWorldPos);
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// 检查两个位置是否相邻
    /// </summary>
    private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    /// <summary>
    /// 消除连通同色格子
    /// </summary>
    private void EliminateConnectedTiles(Vector2Int startPos)
    {
        if (boardManager == null || isProcessing)
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(startPos);

        if (connectedTiles.Count == 0) // 如果没有连通格子，不执行消除
            return;

        // 标记为处理中
        isProcessing = true;
        currentState = GameState.Processing;

        // 获取起始格子的颜色
        TileCell startTile = boardManager.GetTile(startPos);
        TileColor waveColor = startTile != null ? startTile.Color : TileColor.Red;
        int colorIndex = (int)waveColor; // TileColor枚举值：Red=0, Yellow=1, Blue=2, Green=3

        // 从PlayerManager获取该颜色wave配置的技能列表，检查是否有damageBottom技能
        bool hasDamageBottom = false;
        if (PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
            foreach (var identifier in skillIdentifiers)
            {
                if (SkillManager.Instance.HasSkill(identifier))
                {
                    SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                    if (skillInfo != null && skillInfo.effect == "damageBottom")
                    {
                        hasDamageBottom = true;
                        break;
                    }
                }
            }
        }

        // 重置damageBottom触发标志（新的wave group）
        currentWaveGroupId++;
        damageBottomTriggeredThisWave = false;
        
        // 初始化新的wave group的伤害跟踪
        if (!waveGroupTotalDamage.ContainsKey(currentWaveGroupId))
        {
            waveGroupTotalDamage[currentWaveGroupId] = 0f;
            waveGroupActiveWaveCount[currentWaveGroupId] = 0;
            waveGroupColor[currentWaveGroupId] = waveColor;
            waveGroupAddDamageWhenPass[currentWaveGroupId] = 0f; // 初始化addDamageWhenPass
            waveGroupHasDamage[currentWaveGroupId] = false; // 初始化伤害标志
            pendingWaveGroups.Add(currentWaveGroupId); // 添加到待结算列表
        }

        // 检查是否有buffNextDamage技能（用于下一个wave group）
        // 注意：这个检查的是当前wave group的技能，buff会应用到下一个wave group
        float damageMultiplier = 1f;
        if (PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
            foreach (var identifier in skillIdentifiers)
            {
                if (SkillManager.Instance.HasSkill(identifier))
                {
                    SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                    if (skillInfo != null && skillInfo.effect == "buffNextDamage")
                    {
                        int value = SkillManager.Instance.GetSkillValue(identifier);
                        damageMultiplier = 1f + value / 100f;
                        break;
                    }
                }
            }
        }

        // 应用当前wave group的伤害加成（来自上一个wave group的buffNextDamage）
        float currentWaveDamageMultiplier = nextWaveDamageMultiplier;
        nextWaveDamageMultiplier = damageMultiplier; // 设置下一个wave group的加成

        // 消除所有连通的格子并创建波浪
        // 更新wave group的活跃wave数量
        waveGroupActiveWaveCount[currentWaveGroupId] = connectedTiles.Count;
        
        // 检查是否有pure技能（如果只有一个tile，伤害增加）
        bool hasPure = false;
        int pureValue = 0;
        if (connectedTiles.Count == 1 && PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
            foreach (var identifier in skillIdentifiers)
            {
                if (SkillManager.Instance.HasSkill(identifier))
                {
                    SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                    if (skillInfo != null && skillInfo.effect == "pure")
                    {
                        hasPure = true;
                        pureValue = SkillManager.Instance.GetSkillValue(identifier);
                        break;
                    }
                }
            }
        }
        
        int waveIndex = 0;
        foreach (var pos in connectedTiles)
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(pos);
            bool isFirstWave = (waveIndex == 0);
            // 如果只有一个tile且有pure技能，传递pure信息
            CreateWave(worldPos, waveColor, pos, currentWaveGroupId, isFirstWave, hasDamageBottom, currentWaveDamageMultiplier, hasPure, pureValue);

            boardManager.RemoveTile(pos);
            waveIndex++;
        }

        // 立即应用重力（与波浪移动同时进行）
        // 等待一小段时间让消除动画完成，然后开始重力
        DOVirtual.DelayedCall(0.3f, () =>
        {
            boardManager.ApplyGravity();
        });

        // 不再在这里直接调用EndPlayerTurn
        // EndPlayerTurn会在所有wave group都完成结算后，在CheckSpawnAlly中调用
    }

    /// <summary>
    /// 创建波浪攻击
    /// </summary>
    private void CreateWave(Vector3 spawnPosition, TileColor color, Vector2Int gridPos, int waveGroupId, bool isFirstWave, bool hasDamageBottomSkill, float damageMultiplier = 1f, bool hasPure = false, int pureValue = 0)
    {
        if (wavePrefab == null)
            return;

        GameObject waveObj = Instantiate(wavePrefab, spawnPosition, Quaternion.identity, waveParent);
        Wave wave = waveObj.GetComponent<Wave>();
        if (wave == null)
        {
            wave = waveObj.AddComponent<Wave>();
        }

        wave.Init(spawnPosition, color, 10f, gridPos, waveGroupId, isFirstWave, hasDamageBottomSkill, damageMultiplier, hasPure, pureValue);
    }

    /// <summary>
    /// 触发damageBottom效果（最右列上下爆炸）
    /// </summary>
    public static void TriggerDamageBottom(int rightmostX, float damage, TileColor waveColor)
    {
        if (damageBottomTriggeredThisWave)
            return;

        damageBottomTriggeredThisWave = true;

        BoardManager boardManager = FindObjectOfType<BoardManager>();
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        
        if (boardManager == null || enemyManager == null)
            return;

        int value = 0;
        if (PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            int colorIndex = (int)waveColor;
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
            foreach (var identifier in skillIdentifiers)
            {
                if (SkillManager.Instance.HasSkill(identifier))
                {
                    SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                    if (skillInfo != null && skillInfo.effect == "damageBottom")
                    {
                        value = SkillManager.Instance.GetSkillValue(identifier);
                        break;
                    }
                }
            }
        }

        // 对最右列的所有格子创建上下爆炸效果，并对敌人造成伤害
        int boardHeight = boardManager.Height;
        
        for (int y = 0; y < boardHeight; y++)
        {
            Vector2Int gridPos = new Vector2Int(rightmostX, y);
            Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
            
            // 创建爆炸效果（向上下扩展）
            CreateExplosionEffect(worldPos, boardManager);
        }
        
        // 对最右列的所有敌人造成伤害
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead && enemy.GridPosition.x == rightmostX)
            {
                float finalDamage = damage * (1f + value / 100f);
                // 获取红色wave的基础伤害（用于hitTakeDamage）
                float redWaveBaseDamage = (waveColor == TileColor.Red) ? finalDamage : damage;
                enemy.TakeDamage((int)finalDamage, Vector3.right, false, 0, redWaveBaseDamage);
            }
        }
    }

    /// <summary>
    /// 创建爆炸效果（向上下扩展）
    /// </summary>
    private static void CreateExplosionEffect(Vector3 position, BoardManager boardManager)
    {
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = position;
        
        SpriteRenderer sr = explosion.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙红色
        sr.sortingOrder = 10;
        
        explosion.transform.localScale = Vector3.zero;
        
        Sequence explosionSeq = DOTween.Sequence();
        
        // 向上扩展
        GameObject explosionUp = new GameObject("ExplosionUp");
        explosionUp.transform.position = position;
        explosionUp.transform.SetParent(explosion.transform);
        SpriteRenderer srUp = explosionUp.AddComponent<SpriteRenderer>();
        srUp.color = new Color(1f, 0.3f, 0f, 0.8f);
        srUp.sortingOrder = 11;
        explosionUp.transform.localScale = Vector3.zero;
        
        // 向下扩展
        GameObject explosionDown = new GameObject("ExplosionDown");
        explosionDown.transform.position = position;
        explosionDown.transform.SetParent(explosion.transform);
        SpriteRenderer srDown = explosionDown.AddComponent<SpriteRenderer>();
        srDown.color = new Color(1f, 0.3f, 0f, 0.8f);
        srDown.sortingOrder = 11;
        explosionDown.transform.localScale = Vector3.zero;
        
        // 主爆炸缩放
        explosionSeq.Append(explosion.transform.DOScale(Vector3.one * 1.5f, 0.15f).SetEase(Ease.OutQuad));
        
        // 向上扩展
        float tileSize = boardManager != null ? 1f : 1f; // 使用BoardManager的tileSize
        explosionSeq.Join(explosionUp.transform.DOMoveY(position.y + tileSize, 0.2f).SetEase(Ease.OutQuad));
        explosionSeq.Join(explosionUp.transform.DOScale(Vector3.one * 1.2f, 0.2f).SetEase(Ease.OutQuad));
        
        // 向下扩展
        explosionSeq.Join(explosionDown.transform.DOMoveY(position.y - tileSize, 0.2f).SetEase(Ease.OutQuad));
        explosionSeq.Join(explosionDown.transform.DOScale(Vector3.one * 1.2f, 0.2f).SetEase(Ease.OutQuad));
        
        // 淡出
        explosionSeq.Append(explosion.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InQuad));
        explosionSeq.Join(sr.DOFade(0f, 0.1f));
        explosionSeq.Join(srUp.DOFade(0f, 0.1f));
        explosionSeq.Join(srDown.DOFade(0f, 0.1f));
        
        explosionSeq.OnComplete(() => Destroy(explosion));
    }

    /// <summary>
    /// 将TileColor转换为字符串（静态方法）
    /// </summary>
    private static string GetColorStringStatic(TileColor color)
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
    /// 设置addDamageWhenPass技能的值（整个wave group共享）
    /// </summary>
    public static void SetAddDamageWhenPass(int waveGroupId, float value)
    {
        if (!waveGroupAddDamageWhenPass.ContainsKey(waveGroupId))
        {
            waveGroupAddDamageWhenPass[waveGroupId] = 0f;
        }
        waveGroupAddDamageWhenPass[waveGroupId] = value;
    }
    
    /// <summary>
    /// 检查wave group是否有addDamageWhenPass技能
    /// </summary>
    public static bool HasAddDamageWhenPass(int waveGroupId)
    {
        return waveGroupAddDamageWhenPass.ContainsKey(waveGroupId) && waveGroupAddDamageWhenPass[waveGroupId] > 0f;
    }
    
    /// <summary>
    /// 获取wave group的addDamageWhenPass技能值
    /// </summary>
    public static float GetAddDamageWhenPassValue(int waveGroupId)
    {
        if (waveGroupAddDamageWhenPass.ContainsKey(waveGroupId))
        {
            return waveGroupAddDamageWhenPass[waveGroupId];
        }
        return 0f;
    }
    
    /// <summary>
    /// 记录wave造成的伤害（用于spawnAlly技能和noAttackNoCost技能）
    /// </summary>
    public static void RecordWaveDamage(int waveGroupId, float damage)
    {
        if (waveGroupTotalDamage.ContainsKey(waveGroupId))
        {
            waveGroupTotalDamage[waveGroupId] += damage;
        }
        
        // 标记该wave group造成了伤害（用于noAttackNoCost技能）
        if (damage > 0 && waveGroupHasDamage.ContainsKey(waveGroupId))
        {
            waveGroupHasDamage[waveGroupId] = true;
        }
    }

    /// <summary>
    /// 当wave销毁时调用（用于spawnAlly技能）
    /// </summary>
    public static void OnWaveDestroyed(int waveGroupId)
    {
        if (!waveGroupActiveWaveCount.ContainsKey(waveGroupId))
            return;
            
        waveGroupActiveWaveCount[waveGroupId]--;
        
        // 清理addDamageWhenPass数据（wave group完成后）
        if (waveGroupAddDamageWhenPass.ContainsKey(waveGroupId))
        {
            waveGroupAddDamageWhenPass.Remove(waveGroupId);
        }
        
        // 如果这个wave group的所有wave都完成了，检查spawnAlly技能和noAttackNoCost技能
        if (waveGroupActiveWaveCount[waveGroupId] <= 0)
        {
            MainGameManager instance = FindObjectOfType<MainGameManager>();
            if (instance != null)
            {
                instance.CheckSpawnAlly(waveGroupId);
            }
        }
    }

    /// <summary>
    /// 检查并生成随从（spawnAlly技能）
    /// </summary>
    private void CheckSpawnAlly(int waveGroupId)
    {
        if (!waveGroupTotalDamage.ContainsKey(waveGroupId) || !waveGroupColor.ContainsKey(waveGroupId))
            return;
            
        float totalDamage = waveGroupTotalDamage[waveGroupId];
        TileColor waveColor = waveGroupColor[waveGroupId];
        int colorIndex = (int)waveColor;
        
        // 从PlayerManager获取该颜色wave配置的技能列表，检查是否有spawnAlly技能
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;
            
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
        
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "spawnAlly")
                {
                    int value = SkillManager.Instance.GetSkillValue(identifier);
                    
                    // 计算随从血量：总伤害 * value%
                    int allyHealth = (int)(totalDamage * (value / 100f));
                    
                    // 生成随从
                    SpawnAlly(allyHealth);
                    break;
                }
            }
        }
        
        // 检查summonAttack技能（召唤物集体向右侧发射投射物）
        CheckSummonAttack(waveGroupId);
        
        // 检查noAttackNoCost技能（如果整个wave group没有造成伤害，不进入敌人回合）
        bool hasDamage = waveGroupHasDamage.ContainsKey(waveGroupId) && waveGroupHasDamage[waveGroupId];
        if (!hasDamage && !noAttackNoCostTriggeredThisTurn)
        {
            // 检查是否有noAttackNoCost技能
            bool hasNoAttackNoCost = false;
            for (int i = 0; i < 4; i++)
            {
                List<string> skillIdentifiers2 = PlayerManager.Instance.GetWaveSkills(i);
                foreach (var identifier in skillIdentifiers2)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                        if (skillInfo != null && skillInfo.effect == "noAttackNoCost")
                        {
                            hasNoAttackNoCost = true;
                            break;
                        }
                    }
                }
                if (hasNoAttackNoCost) break;
            }
            
            if (hasNoAttackNoCost)
            {
                // 标记已触发，一个回合只会触发一次
                noAttackNoCostTriggeredThisTurn = true;
                
                // 清理已完成的wave group数据
                waveGroupTotalDamage.Remove(waveGroupId);
                waveGroupActiveWaveCount.Remove(waveGroupId);
                waveGroupColor.Remove(waveGroupId);
                waveGroupHasDamage.Remove(waveGroupId);
                pendingWaveGroups.Remove(waveGroupId);
                
                
                // 不进入敌人回合，直接重置为玩家回合
                DOVirtual.DelayedCall(0.1f, () =>
                {
                    isProcessing = false;
                    currentState = GameState.PlayerTurn;
                    Debug.Log("noAttackNoCost: 没有造成伤害，继续玩家回合");
                });
                return;
            }
            // 检查是否所有wave group都完成了
            CheckAllWaveGroupsCompleted();
        }
        
        // 清理已完成的wave group数据
        waveGroupTotalDamage.Remove(waveGroupId);
        waveGroupActiveWaveCount.Remove(waveGroupId);
        waveGroupColor.Remove(waveGroupId);
        waveGroupHasDamage.Remove(waveGroupId);
        pendingWaveGroups.Remove(waveGroupId);
        
        // 检查是否所有wave group都完成了
        CheckAllWaveGroupsCompleted();
    }
    
    /// <summary>
    /// 检查所有wave group是否都完成了，如果是则进入敌人回合
    /// </summary>
    private void CheckAllWaveGroupsCompleted()
    {
        // 如果还有待结算的wave group，等待
        if (pendingWaveGroups.Count > 0)
        {
            return;
        }
        
        // 所有wave group都完成了，进入敌人回合
        // 重置noAttackNoCost触发标志（新的玩家回合）
        noAttackNoCostTriggeredThisTurn = false;
        
        DOVirtual.DelayedCall(0.1f, () =>
        {
            isProcessing = false;
            EndPlayerTurn();
        });
    }
    
    /// <summary>
    /// 检查并执行summonAttack技能（召唤物集体向右侧发射投射物）
    /// </summary>
    private void CheckSummonAttack(int waveGroupId)
    {
        if (!waveGroupColor.ContainsKey(waveGroupId))
            return;
            
        TileColor waveColor = waveGroupColor[waveGroupId];
        int colorIndex = (int)waveColor;
        
        // 检查是否有summonAttack技能
        if (PlayerManager.Instance == null || SkillManager.Instance == null || allyManager == null)
            return;
            
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
        bool hasSummonAttack = false;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "summonAttack")
                {
                    hasSummonAttack = true;
                    break;
                }
            }
        }
        
        if (!hasSummonAttack)
            return;
        
        // 获取wave的攻击力（使用当前wave的基础伤害，如果有多个wave则使用平均）
        float waveDamage = 20f; // 基础伤害
        if (waveGroupTotalDamage.ContainsKey(waveGroupId) && waveGroupActiveWaveCount.ContainsKey(waveGroupId))
        {
            // 计算平均伤害（总伤害 / wave数量）
            int waveCount = waveGroupActiveWaveCount[waveGroupId];
            if (waveCount > 0)
            {
                waveDamage = waveGroupTotalDamage[waveGroupId] / waveCount;
            }
        }
        
        // 所有ally向右侧发射投射物
        foreach (var ally in allyManager.ActiveAllies)
        {
            if (ally != null && !ally.IsDead)
            {
                CreateAllyProjectile(ally, (int)waveDamage);
            }
        }
    }
    
    /// <summary>
    /// 创建ally的投射物（向右侧发射）
    /// </summary>
    private void CreateAllyProjectile(Ally ally, int damage)
    {
        if (boardManager == null || ally == null)
            return;
            
        // 创建投射物GameObject
        GameObject projectileObj = Instantiate(allyProjectile);
        SpriteRenderer sr = projectileObj.GetComponentInChildren<SpriteRenderer>();
        
        Vector3 startPos = ally.transform.position;
        // 目标位置：右侧（假设向右飞行10个单位）
        Vector3 targetPos = startPos + Vector3.right * 10f;
        
        projectileObj.transform.position = startPos;
        projectileObj.transform.localScale = Vector3.one * 0.3f;
        
        float projectileSpeed = 10f;
        float travelTime = 10f / projectileSpeed;
        
        projectileObj.transform.DOMove(targetPos, travelTime)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 检查是否击中敌人
                if (enemyManager != null)
                {
                    // 检查投射物路径上的敌人
                    Vector2Int allyGridPos = ally.GridPosition;
                    for (int x = allyGridPos.x + 1; x < boardManager.Width; x++)
                    {
                        Vector2Int checkPos = new Vector2Int(x, allyGridPos.y);
                        foreach (var enemy in enemyManager.ActiveEnemies)
                        {
                            if (enemy != null && !enemy.IsDead && enemy.GridPosition == checkPos)
                            {
                                // 击中敌人
                                enemy.TakeDamage(damage, Vector3.right, false, 0, 0f);
                                break;
                            }
                        }
                    }
                }
                
                Destroy(projectileObj);
            });
    }

    /// <summary>
    /// 生成随从
    /// </summary>
    private void SpawnAlly(int health)
    {
        if (health <= 0)
        {
            return;
        }
        if (boardManager == null)
            return;
            
        int boardHeight = boardManager.Height;
        
        // 收集所有有敌人的行（优先选择这些行）
        HashSet<int> enemyRows = new HashSet<int>();
        if (enemyManager != null)
        {
            foreach (var enemy in enemyManager.ActiveEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    enemyRows.Add(enemy.GridPosition.y);
                }
            }
        }
        
        // 收集所有可用的行（最左侧x=0没有障碍物的行）
        List<int> availableRows = new List<int>();
        List<int> enemyAvailableRows = new List<int>(); // 有敌人的可用行（优先）
        
        for (int y = 0; y < boardHeight; y++)
        {
            Vector2Int checkPos = new Vector2Int(0, y);
            bool rowHasObstacle = false;
            
            // 检查敌人
            if (enemyManager != null)
            {
                foreach (var enemy in enemyManager.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsDead && enemy.GridPosition == checkPos)
                    {
                        rowHasObstacle = true;
                        break;
                    }
                }
            }
            
            // 检查随从
            if (!rowHasObstacle && allyManager != null)
            {
                rowHasObstacle = allyManager.HasAllyAtPosition(checkPos);
            }
            
            if (!rowHasObstacle)
            {
                if (enemyRows.Contains(y))
                {
                    enemyAvailableRows.Add(y); // 优先：有敌人的行
                }
                else
                {
                    availableRows.Add(y); // 普通可用行
                }
            }
        }
        
        // 如果所有行都有障碍物，不生成随从
        if (enemyAvailableRows.Count == 0 && availableRows.Count == 0)
        {
            return;
        }
        
        // 优先选择有敌人的行，否则选择其他可用行
        int spawnY;
        if (enemyAvailableRows.Count > 0)
        {
            spawnY = enemyAvailableRows[Random.Range(0, enemyAvailableRows.Count)];
        }
        else
        {
            spawnY = availableRows[Random.Range(0, availableRows.Count)];
        }
        
        Vector2Int spawnGridPos = new Vector2Int(0, spawnY);
        
        // 创建随从对象
        GameObject allyObj = Instantiate(allyPrefab);
        Ally ally = allyObj.GetComponent<Ally>();
        
        // 设置位置
        Vector3 worldPos = boardManager.GridToWorldPosition(spawnGridPos);
        if (enemyManager != null)
        {
            worldPos += new Vector3(0, enemyManager.SpawnOffsetY, 0);
        }
        allyObj.transform.position = worldPos;
        
        // 初始化随从
        ally.Init(spawnGridPos, health);
        
        // 添加到AllyManager
        if (allyManager != null)
        {
            allyManager.AddAlly(ally);
        }
        
        // 创建血条
        if (enemyManager != null)
        {
            enemyManager.CreateHealthBarForAlly(ally);
        }
        
        Debug.Log($"生成随从，血量: {health}");
    }

    /// <summary>
    /// 结束玩家回合
    /// </summary>
    private void EndPlayerTurn()
    {
        currentState = GameState.EnemyTurn;
        isProcessing = true; // 敌人移动时也禁止操作

        // 敌人向左移动
        if (enemyManager != null)
        {
            enemyManager.MoveAllEnemiesLeft(enemyMoveDistance, enemyMoveDuration);

            // 每回合生成一个新敌人
            DOVirtual.DelayedCall(enemyMoveDuration + 0.1f, () =>
            {
                enemyManager.SpawnEnemyEachTurn();
                
                // 敌人回合结束后，检查胜利条件
                DOVirtual.DelayedCall(0.1f, () =>
                {
                    // 检查是否可以完成关卡（所有敌人死亡且没有剩余敌人可生成）
                    if (enemyManager != null && enemyManager.CanCompleteLevel())
                    {
                        CompleteLevel();
                        return;
                    }
                    
                    // 否则进入玩家回合
                    isProcessing = false;
                    currentState = GameState.PlayerTurn;
                });
            });
        }
        else
        {
            isProcessing = false;
            currentState = GameState.PlayerTurn;
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    private void GameOver()
    {
        currentState = GameState.GameOver;
        isProcessing = true; // 游戏结束时禁止操作

        // 关闭技能显示
        if (skillDisplayPanel != null)
        {
            skillDisplayPanel.SetActive(false);
        }
        
        // 关闭敌人描述显示
        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }
        
        // 等待敌人移动动画完成后显示弹窗
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GameOverDialog.ShowGameOver(
                onRestart: () =>
                {
                    // 重新开始游戏
                    //StartBattle();
                    Restart();
                },
                onQuit: () =>
                {
                    // 退出游戏
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }
            );
        });
    }

    /// <summary>
    /// 关卡完成
    /// </summary>
    private void CompleteLevel()
    {
        currentState = GameState.LevelComplete;
        isProcessing = true;

        // 掉落gold（使用当前玩家等级对应的关卡信息）
        if (LevelManager.Instance != null && PlayerManager.Instance != null && CSVLoader.Instance != null)
        {
            // 查找当前玩家等级对应的关卡信息
            LevelInfo levelInfo = null;
            if (CSVLoader.Instance.levelInfoMap != null)
            {
                // 查找匹配玩家等级的关卡
                foreach (var kvp in CSVLoader.Instance.levelInfoMap)
                {
                    if (kvp.Value.level == playerLevel)
                    {
                        levelInfo = kvp.Value;
                        break;
                    }
                }
            }
            
            if (levelInfo != null && levelInfo.gold > 0)
            {
                PlayerManager.Instance.AddGold(levelInfo.gold);
                Debug.Log($"关卡完成，获得 {levelInfo.gold} gold");
            }
        }

        // 关闭技能显示
        if (skillDisplayPanel != null)
        {
            skillDisplayPanel.SetActive(false);
        }
        
        // 关闭敌人描述显示
        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }

        // 显示技能选择界面
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("SkillSelectMenu");
            skillMenu = menuObj.AddComponent<SkillSelectMenu>();
        }

        skillMenu.ShowSkillSelection(
            () =>
            {
                // 确认按钮点击，进入下一关
                Debug.Log("确认配置，进入下一关");
            }
        );
    }

    /// <summary>
    /// 敌人攻击玩家（已废弃，现在由Enemy.TakeAction()处理）
    /// </summary>
    [System.Obsolete("敌人攻击现在由Enemy.TakeAction()处理，此方法不再使用")]
    private void AttackPlayer()
    {
        if (PlayerManager.Instance == null || enemyManager == null)
            return;

        // 计算到达最左边的敌人数量
        int attackCount = 0;
        List<Enemy> enemiesToRemove = new List<Enemy>();
        
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead && enemy.IsAtLeftEdge())
            {
                attackCount++;
                // 从敌人的EnemyInfo中获取攻击伤害
                int damage = 10; // 默认伤害
                if (enemy.EnemyInfo != null)
                {
                    damage = enemy.EnemyInfo.attack;
                }
                
                PlayerManager.Instance.TakeDamage(damage);
                
                // 显示伤害数字
                Vector3 attackPos = enemy.transform.position;
                DamageNumber.CreateDamageNumber(damage, attackPos + Vector3.left * 0.5f, false);
                
                // 销毁到达最左边的敌人
                enemy.Die();
                enemiesToRemove.Add(enemy);
            }
        }

        // 从列表中移除已死亡的敌人
        foreach (var enemy in enemiesToRemove)
        {
            enemyManager.RemoveDeadEnemy(enemy);
        }

        if (attackCount > 0)
        {
            Debug.Log($"玩家受到 {attackCount} 个敌人攻击");
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}


