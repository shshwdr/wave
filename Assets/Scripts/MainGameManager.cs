using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// 一场战斗的游戏控制 - 回合制战斗系统
/// </summary>
public class MainGameManager : Singleton<MainGameManager>
{
    [Header("引用")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Camera mainCamera;
    private AllyManager allyManager;
    
    [Header("Boss设置")]
    [SerializeField] private GameObject bossPrefab; // Boss prefab（和enemy使用同一个prefab）
    private Boss currentBoss = null; // 当前boss
    private LevelInfo currentLevelInfo = null; // 当前关卡信息
    private List<EnemySpawnInfo> bossBattleEnemies = new List<EnemySpawnInfo>(); // boss战中的敌人列表
    private int bossBattleEnemyIndex = 0; // boss战中当前敌人索引

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
    
    [Header("回合横幅UI")]
    [SerializeField] private TurnBanner turnBanner;

    public bool showShopAtBeginning;
    public bool showEventAtBeginning;
    [SerializeField] public GameObject allyPrefab;
    [SerializeField] public GameObject allyProjectile;
    [SerializeField] public GameObject damagePrefab;
    
    [Header("Puzzle设置")]
    public bool isPublish = false; // 发布模式，禁用所有编辑快捷键
    
    public List<Sprite> tileSprites;
    
    private enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Processing,        // 处理中（波浪攻击、敌人移动等）
        GameOver,
        LevelComplete,     // 关卡完成
        PuzzleEdit,        // Puzzle编辑模式
        PuzzlePlay         // Puzzle游戏模式
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
    
    // 是否是第一次战斗（用于决定是否清除和生成格子）
    private bool isFirstBattle = true;
    
    // 保存关卡开始时的血量（用于重试关卡）
    private int levelStartHealth = -1;
    
    // 保存关卡开始时的金币（用于重试关卡）
    private int levelStartGold = -1;
    
    // 回合计数（用于奖励关卡）
    private int currentTurn = 0;
    
    // 跟踪gold关卡中从chest（敌人）获得的金钱
    private int goldFromChests = 0;
    
    // Puzzle编辑模式相关
    private bool isPuzzleEditMode = false;
    private int[,] currentPuzzleData = null; // 当前编辑的puzzle数据（8x6）

    [SerializeField] private EventReference waveBlue;
    [SerializeField] private EventReference waveCyan;
    [SerializeField] private EventReference wavePurple;
    [SerializeField] private EventReference waveRed;

    public List<Color> waveColorOutline;

    /// <summary>
    /// 获取当前关卡信息
    /// </summary>
    public LevelInfo GetCurrentLevelInfo()
    {
        return currentLevelInfo;
    }
    
    /// <summary>
    /// 获取剩余回合数（如果turns为0则返回-1，表示不显示）
    /// </summary>
    public int GetRemainingTurns()
    {
        if (currentLevelInfo == null || currentLevelInfo.turns == 0)
        {
            return -1; // 不显示回合数
        }
        return Mathf.Max(0, currentLevelInfo.turns - currentTurn);
    }
    
    /// <summary>
    /// 记录从chest（敌人）获得的金钱（用于gold关卡统计）
    /// </summary>
    public void RecordGoldFromChest(int gold)
    {
        if (currentLevelInfo != null && currentLevelInfo.type == "gold")
        {
            goldFromChests += gold;
        }
    }
    
    /// <summary>
    /// 玩家等级提升
    /// </summary>
    public void PlayerLevelUp()
    {
        playerLevel++;
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
        
        // 初始化回合横幅UI
        InitTurnBanner();

        // 如果需要在开始时显示事件，先显示事件
        if (showEventAtBeginning)
        {
            ShowEventAtStart();
        }
        else
        {
            // 否则直接开始战斗
            StartBattle();

            if (showShopAtBeginning)
            {
                SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
                if (skillMenu != null)
                {
                    skillMenu.ShowSkillSelection(
                        () =>
                        {
                            // 确认按钮点击，进入下一关
                            Debug.Log("确认配置，进入下一关");
                        }
                    );
                }
            }
        }
    }
    
    /// <summary>
    /// 在游戏开始时显示事件
    /// </summary>
    private void ShowEventAtStart()
    {
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("EventMenu");
            eventMenu = menuObj.AddComponent<EventMenu>();
        }
        
        eventMenu.ShowEvent(() =>
        {
            // 事件完成后，如果需要在开始时显示商店，显示商店
            if (showShopAtBeginning)
            {
                ShowShopAtStart();
            }
            else
            {
                // 否则直接开始战斗
                StartBattle();
            }
        });
    }
    
    /// <summary>
    /// 在游戏开始时显示商店
    /// </summary>
    private void ShowShopAtStart()
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu != null)
        {
            skillMenu.ShowSkillSelection(
                () =>
                {
                    // 确认按钮点击，开始战斗
                    StartBattle();
                }
            );
        }
        else
        {
            // 如果找不到商店菜单，直接开始战斗
            StartBattle();
        }
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
            skillDisplayText.fontSize = 26;
            skillDisplayText.color = Color.white;
            skillDisplayText.alignment = TextAlignmentOptions.TopLeft;
            // 从CSVLoader获取font
            if (CSVLoader.Instance != null && CSVLoader.Instance.font != null)
            {
                skillDisplayText.font = CSVLoader.Instance.font;
            }
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
            enemyDescriptionText.fontSize = 26;
            enemyDescriptionText.color = Color.white;
            enemyDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            // 从CSVLoader获取font
            if (CSVLoader.Instance != null && CSVLoader.Instance.font != null)
            {
                enemyDescriptionText.font = CSVLoader.Instance.font;
            }
        }

        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化回合横幅UI
    /// </summary>
    private void InitTurnBanner()
    {
        if (turnBanner == null)
        {
            // 创建TurnBanner
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            GameObject bannerObj = new GameObject("TurnBanner");
            bannerObj.transform.SetParent(canvasObj.transform);
            turnBanner = bannerObj.AddComponent<TurnBanner>();
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
            // 保存关卡开始时的血量和金币（用于重试关卡）
            levelStartHealth = PlayerManager.Instance.CurrentHealth;
            levelStartGold = PlayerManager.Instance.Gold;
            PlayerManager.Instance.StartBattle();
        }

        // 只在第一次战斗时清空棋盘并重新生成
        if (isFirstBattle && boardManager != null)
        {
            boardManager.ClearBoard();
            boardManager.InitializeBoard();
            boardManager.GenerateRandomColors();
            isFirstBattle = false;
        }

        // 进入战斗模式时载入敌人
        // 从关卡管理器获取关卡信息并生成敌人
        currentLevelInfo = LevelManager.Instance.GetNextLevel(playerLevel);
        if (currentLevelInfo != null && enemyManager != null)
        {
            enemyManager.SpawnEnemiesFromLevel(currentLevelInfo);
            
            // 检查是否有boss
            if (!string.IsNullOrEmpty(currentLevelInfo.bossIdentifier))
            {
                SpawnBoss(currentLevelInfo.bossIdentifier);
                // 初始化boss战敌人列表
                bossBattleEnemies = LevelManager.Instance.ParseEnemies(currentLevelInfo.enemies);
                bossBattleEnemyIndex = 0;
            }
            else
            {
                currentBoss = null;
            }
        }
        else if (enemyManager != null)
        {
            // 如果没有关卡信息，使用随机生成
            enemyManager.SpawnEnemiesRandomly();
            currentBoss = null;
        }

        // 开始新回合统计
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.StartNewRound();
        }

        // 初始化回合计数（用于奖励关卡）
        currentTurn = 0;
        
        // 重置从chest获得的金钱计数
        goldFromChests = 0;

        currentState = GameState.PlayerTurn;
        
        // 每回合开始时恢复所有shield敌人的盾牌
        ResetAllEnemyShields();
        
        // 显示关卡开始toast
        ShowLevelStartToast();
    }
    
    /// <summary>
    /// 显示关卡开始toast
    /// </summary>
    private void ShowLevelStartToast()
    {
        if (currentLevelInfo == null || ToastManager.Instance == null)
            return;
        
        string message = "";
        string type = currentLevelInfo.type != null ? currentLevelInfo.type.ToLower() : "";
        
        switch (type)
        {
            case "gold":
                int turns = currentLevelInfo.turns > 0 ? currentLevelInfo.turns : 0;
                message = $"Destroy chests to collect gold in {turns} turns!";
                break;
            case "boss":
                message = "Defeat the boss to win!";
                break;
            case "puzzle":
                int puzzleTurns = currentLevelInfo.turns > 0 ? currentLevelInfo.turns : 0;
                message = $"Clear all tiles in {puzzleTurns} turns!";
                break;
            case "normal":
            default:
                message = "Eliminate all enemies to win!";
                break;
        }
        
        // if (!string.IsNullOrEmpty(message))
        // {
        //     ToastManager.Instance.ShowToast(message);
        // }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
        if (currentState == GameState.GameOver)
            return;

        // 处理编辑模式快捷键（只在非发布模式下）
        if (!isPublish)
        {
            HandleEditModeInput();
        }

        // 敌人攻击逻辑现在由Enemy.TakeAction()处理，不再需要在这里检查

        // 检查玩家是否死亡
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsDead)
        {
            GameOver();
            return;
        }

        // 编辑模式或puzzle游戏模式
        if (currentState == GameState.PuzzleEdit)
        {
            HandlePuzzleEditInput();
        }
        else if (currentState == GameState.PuzzlePlay)
        {
            // Puzzle游戏模式：只能右键消除，禁用左键移动
            HandlePuzzlePlayInput();
        }
        // 玩家回合 - 处理输入和鼠标悬停（只有在没有处理中时）
        else if (currentState == GameState.PlayerTurn && !isProcessing)
        {
            HandlePlayerInput();
            
            // 区分鼠标和触屏输入，避免冲突
            if (Input.touchCount > 0)
            {
                // 有触屏输入时，只处理触屏
                HandleTouchInput();
            }
            else
            {
                // 没有触屏输入时，处理鼠标悬停
                HandleMouseHover();
            }
        }
        else if (isProcessing)
        {
            // 处理中时清除高亮
            ClearHighlights();
        }
        
        // 如果事件菜单打开，清除高亮
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
        {
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
        
        // 如果教程正在拦截输入，不响应输入
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsBlockingInput)
        {
            return;
        }
        
        // 如果统计菜单打开，不响应输入
        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu != null && statisticsMenu.IsActive)
        {
            return;
        }
        
        // 如果事件菜单打开，不响应输入
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
        {
            return;
        }

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
            // 检查教程是否禁用拖动
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsDragEnabled)
            {
                // 教程禁用拖动，不处理
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    dragStartPos = gridPos;
                    isDragging = true;
                    selectedTilePos = gridPos;
                    
                    // 清除所有高亮效果，只保留showFrame
                    ClearHighlights();
                    
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
                
                // 延迟后在鼠标当前位置恢复highlight效果
                Vector2Int finalGridPos = gridPos; // 保存最终位置
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    if (boardManager != null && boardManager.IsValidPosition(finalGridPos))
                    {
                        UpdateHighlightTiles(finalGridPos);
                        lastHighlightPos = finalGridPos;
                    }
                });
                
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
                    
                    // 触发教程信号：拖动交换方块
                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.SendSignal("dragTile");
                    }
                    
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
        // 检查教程是否禁用右键点击
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsRightClickEnabled)
        {
            // 教程禁用右键点击，不处理
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                // 播放玩家攻击动画
                TryPlayPlayerAttackAnimation();
                
                // 不调用ClearHighlights()，让高亮保持显示
                EliminateConnectedTiles(gridPos);
            }
        }
    }

    /// <summary>
    /// 处理鼠标悬停
    /// </summary>
    private void HandleMouseHover()
    {
        // 如果教程正在拦截输入，不响应hover
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsBlockingInput)
        {
            return;
        }
        
        // 如果技能选择界面打开，不响应hover
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu != null && skillMenu.IsActive)
        {
            return;
        }
        
        // 如果统计菜单打开，不响应hover
        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu != null && statisticsMenu.IsActive)
        {
            return;
        }
        
        // 如果事件菜单打开，不响应hover
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
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
            // 如果鼠标位置改变了，更新高亮和技能显示（拖动时不更新高亮）
            if (gridPos != lastHighlightPos && !waitingForSecondSwap && !isDragging)
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
    
    private bool isTouching = false; // 是否正在触摸
    private Vector2Int touchGridPos = new Vector2Int(-1, -1); // 触摸的格子位置
    
    /// <summary>
    /// 处理触屏输入
    /// </summary>
    private void HandleTouchInput()
    {
        // 如果技能选择界面打开，不响应touch
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu != null && skillMenu.IsActive)
        {
            return;
        }
        
        // 如果事件菜单打开，不响应touch
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
        {
            return;
        }
        
        // 处理中时不允许操作
        if (isProcessing)
            return;
        
        // 确保相机已初始化
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("MainGameManager: 无法找到主相机，触屏输入无法工作");
                return;
            }
        }
        
        // 检查触摸输入
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // 正确转换屏幕坐标到世界坐标（需要设置z值）
            Vector3 touchScreenPos = touch.position;
            // 对于正交相机，使用相机的z位置；对于透视相机，使用nearClipPlane
            if (mainCamera.orthographic)
            {
                touchScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
            }
            else
            {
                touchScreenPos.z = mainCamera.nearClipPlane;
            }
            Vector3 touchWorldPos = mainCamera.ScreenToWorldPoint(touchScreenPos);
            touchWorldPos.z = 0;
            
            // 检查是否触摸到敌人或tile
            Vector2Int gridPos = GetGridPositionFromWorld(touchWorldPos);
            
            if (touch.phase == TouchPhase.Began)
            {
                isTouching = true;
                touchGridPos = gridPos;
                
                // 检查是否触摸到敌人
                Enemy touchedEnemy = GetEnemyAtPosition(touchWorldPos);
                if (touchedEnemy != null && !touchedEnemy.IsDead)
                {
                    UpdateEnemyDescription(touchedEnemy);
                }
                // 检查是否触摸到tile
                else if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    UpdateSkillDisplay(gridPos);
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouching = false;
                touchGridPos = new Vector2Int(-1, -1);
                
                // 隐藏详细信息
                if (enemyDescriptionPanel != null)
                {
                    enemyDescriptionPanel.SetActive(false);
                }
                if (skillDisplayPanel != null)
                {
                    skillDisplayPanel.SetActive(false);
                }
            }
            else if (touch.phase == TouchPhase.Moved && isTouching)
            {
                // 触摸移动时更新显示
                Vector2Int newGridPos = GetGridPositionFromWorld(touchWorldPos);
                if (newGridPos != touchGridPos)
                {
                    touchGridPos = newGridPos;
                    
                    // 检查是否触摸到敌人
                    Enemy touchedEnemy = GetEnemyAtPosition(touchWorldPos);
                    if (touchedEnemy != null && !touchedEnemy.IsDead)
                    {
                        UpdateEnemyDescription(touchedEnemy);
                    }
                    // 检查是否触摸到tile
                    else if (boardManager != null && boardManager.IsValidPosition(newGridPos))
                    {
                        UpdateSkillDisplay(newGridPos);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 从世界坐标获取格子位置
    /// </summary>
    private Vector2Int GetGridPositionFromWorld(Vector3 worldPos)
    {
        if (boardManager == null)
            return new Vector2Int(-1, -1);
        return boardManager.WorldToGridPosition(worldPos);
    }
    
    /// <summary>
    /// 获取指定位置的敌人（基于spriteRenderer的实际sprite区域）
    /// </summary>
    private Enemy GetEnemyAtPosition(Vector3 worldPos)
    {
        // 首先检查Boss
        if (currentBoss != null && !currentBoss.IsDead)
        {
            if (IsPositionInEnemyBounds(currentBoss, worldPos))
            {
                return currentBoss;
            }
        }
        
        // 然后检查普通敌人
        if (enemyManager != null)
        {
            foreach (var enemy in enemyManager.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead)
                    continue;
                    
                if (IsPositionInEnemyBounds(enemy, worldPos))
                {
                    return enemy;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 检查位置是否在敌人的sprite范围内
    /// </summary>
    private bool IsPositionInEnemyBounds(Enemy enemy, Vector3 worldPos)
    {
        if (enemy == null)
            return false;
            
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = enemy.GetComponentInChildren<SpriteRenderer>();
            
        if (sr != null && sr.enabled && sr.sprite != null)
        {
            // 检查世界坐标是否在sprite的bounds内
            Bounds spriteBounds = sr.bounds;
            if (spriteBounds.Contains(worldPos))
            {
                // 进一步检查是否点击在sprite的实际像素上（非透明区域）
                // 将世界坐标转换为sprite的本地坐标
                Vector3 localPos = sr.transform.InverseTransformPoint(worldPos);
                
                // 获取sprite的像素坐标
                Rect spriteRect = sr.sprite.rect;
                Vector2 pixelPos = new Vector2(
                    (localPos.x / spriteBounds.size.x) * spriteRect.width + spriteRect.width * 0.5f,
                    (localPos.y / spriteBounds.size.y) * spriteRect.height + spriteRect.height * 0.5f
                );
                
                // 检查像素是否在sprite范围内（简化检查，只检查bounds）
                if (pixelPos.x >= 0 && pixelPos.x < spriteRect.width &&
                    pixelPos.y >= 0 && pixelPos.y < spriteRect.height)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取鼠标下的敌人（基于spriteRenderer的实际sprite区域）
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
        
        // 使用与GetEnemyAtPosition相同的逻辑
        return GetEnemyAtPosition(mouseWorldPos);
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
        
        // 构建详细信息文本
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // 基本信息
        sb.AppendLine($"<b>{enemy.EnemyInfo.name}</b>");
        sb.AppendLine();
        
        // 攻击力和血量
        int baseAttack = enemy.GetAttack();
        int displayAttack = baseAttack;
        
        // 应用敌人伤害加成（如果有）
        if (PlayerManager.Instance != null && PlayerManager.Instance.EnemyDamageBonus > 0)
        {
            float bonusPercent = PlayerManager.Instance.EnemyDamageBonus;
            displayAttack = Mathf.RoundToInt(baseAttack * (1f + bonusPercent / 100f));
        }
        
        if (displayAttack != baseAttack)
        {
            sb.AppendLine($"Attack: {baseAttack} → <color=red>{displayAttack}</color> (+{PlayerManager.Instance.EnemyDamageBonus:F0}%)");
        }
        else
        {
            sb.AppendLine($"Attack: {displayAttack}");
        }
        sb.AppendLine($"HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
        sb.AppendLine();
        
        // Buff/Debuff信息
        int vulnerableStacks = enemy.GetVulnerableStacks();
        if (vulnerableStacks > 0)
        {
            float damageIncrease = vulnerableStacks * 0.05f * 100f;
            sb.AppendLine($"<color=yellow>Vulnerable: {vulnerableStacks}层</color>");
            sb.AppendLine($"伤害提升: +{damageIncrease:F0}%");
            sb.AppendLine();
        }
        
        // 显示敌人伤害加成（如果有）
        if (PlayerManager.Instance != null && PlayerManager.Instance.EnemyDamageBonus > 0)
        {
            sb.AppendLine($"<color=red>Enemy Damage Bonus: +{PlayerManager.Instance.EnemyDamageBonus:F0}%</color>");
            sb.AppendLine();
        }
        
        // 描述
        if (!string.IsNullOrEmpty(enemy.EnemyInfo.description))
        {
            sb.AppendLine(enemy.EnemyInfo.description);
        }
        
        enemyDescriptionText.text = sb.ToString();
        enemyDescriptionPanel.SetActive(true);
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
                // SetHighlight方法内部会检查是否正在被消除，如果是则不会清除高亮
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

        // 标记正在被消除的tile，这样它们的高亮就不会被清除
        foreach (var pos in connectedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            if (tile != null)
            {
                tile.MarkAsBeingDestroyed();
            }
        }

        // 标记为处理中
        isProcessing = true;
        currentState = GameState.Processing;

        // 获取起始格子的颜色
        TileCell startTile = boardManager.GetTile(startPos);
        TileColor waveColor = startTile != null ? startTile.Color : TileColor.Red;
        int colorIndex = (int)waveColor; // TileColor枚举值：Red=0, Yellow=1, Blue=2, Green=3

        EventReference waveEvent;

        switch (colorIndex)
        {
            case 0: // red
                waveEvent = waveRed;
                break;

            case 1: // cyan
                waveEvent = waveCyan;
                break;

            case 2: // blue
                waveEvent = waveBlue;
                break;

            case 3: // purple
                waveEvent = wavePurple;
                break;

            default:
                waveEvent = waveRed;
                break;
        }

        FMOD.Studio.EventInstance waveInstance = RuntimeManager.CreateInstance(waveEvent);

        // Calculate parameter value based on tile count
        int tileCount = connectedTiles.Count;
        float tileNumberParam;

        if (tileCount == 1)
            tileNumberParam = 0f;  // weak
        else if (tileCount <= 5)
            tileNumberParam = 1f;  // normal
        else
            tileNumberParam = 2f;  // strong

        // Set parameter (all events use the same parameter name: "Tile Number")
        waveInstance.setParameterByName("Tile Number", tileNumberParam);

        // Play sound
        waveInstance.start();
        waveInstance.release();

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
        
        // 记录统计：tiles生成和wave group大小
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RecordTilesGenerated(waveColor, connectedTiles.Count);
            StatisticsManager.Instance.RecordWaveGroupSize(waveColor, connectedTiles.Count);
        }
        
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
        
        // 检查是否有frontAndBack技能
        bool hasFrontAndBack = false;
        if (PlayerManager.Instance != null && SkillManager.Instance != null)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
            foreach (var identifier in skillIdentifiers)
            {
                if (SkillManager.Instance.HasSkill(identifier))
                {
                    SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                    if (skillInfo != null && skillInfo.effect == "frontAndBack")
                    {
                        hasFrontAndBack = true;
                        break;
                    }
                }
            }
        }
        
        // 如果有frontAndBack技能，需要创建两倍的wave（向前和向后）
        int waveCountMultiplier = hasFrontAndBack ? 2 : 1;
        waveGroupActiveWaveCount[currentWaveGroupId] = connectedTiles.Count * waveCountMultiplier;
        
        // 记录统计：wave group生成（总共生成/消除了几次）
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RecordWaveGenerated(waveColor);
        }
        
        int waveIndex = 0;
        int tilesUsed = connectedTiles.Count; // 使用的tile数量
        Vector2Int firstTilePos = connectedTiles.Count > 0 ? connectedTiles[0] : Vector2Int.zero; // 保存第一个tile的位置用于显示回血数字
        foreach (var pos in connectedTiles)
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(pos);
            bool isFirstWave = (waveIndex == 0);
            // 如果只有一个tile且有pure技能，传递pure信息
            // 创建向前移动的波浪
            CreateWave(worldPos, waveColor, pos, currentWaveGroupId, isFirstWave, hasDamageBottom, currentWaveDamageMultiplier, hasPure, pureValue, tilesUsed, false);

            // 如果有frontAndBack技能，同时创建向后移动的波浪
            if (hasFrontAndBack)
            {
                CreateWave(worldPos, waveColor, pos, currentWaveGroupId, isFirstWave, false, currentWaveDamageMultiplier, hasPure, pureValue, tilesUsed, true);
            }

            boardManager.RemoveTile(pos);
            waveIndex++;
        }
        
        // 应用healWhenSpawn技能（整个wave group只回一次血）
        ApplyHealWhenSpawnForWaveGroup(waveColor, tilesUsed, firstTilePos);

        // 立即应用重力（与波浪移动同时进行）
        // 等待一小段时间让消除动画完成，然后开始重力
        // puzzle模式不生成新tiles
        bool isPuzzleMode = currentLevelInfo != null && currentLevelInfo.type != null && currentLevelInfo.type.ToLower() == "puzzle";
        DOVirtual.DelayedCall(0.3f, () =>
        {
            boardManager.ApplyGravity(!isPuzzleMode);
        });

        // 不再在这里直接调用EndPlayerTurn
        // EndPlayerTurn会在所有wave group都完成结算后，在CheckSpawnAlly中调用
    }

    /// <summary>
    /// 尝试播放玩家攻击动画
    /// </summary>
    private void TryPlayPlayerAttackAnimation()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.anim != null)
        {
            // 检查是否有 Player 动画文件夹
            if (SpriteRenderAnim.HasAnimationFolder("player"))
            {
                PlayerManager.Instance.anim.SetIdentifier("Player");
                PlayerManager.Instance.anim.PlayAtk();
            }
        }
    }
    
    /// <summary>
    /// 创建波浪攻击
    /// </summary>
    private void CreateWave(Vector3 spawnPosition, TileColor color, Vector2Int gridPos, int waveGroupId, bool isFirstWave, bool hasDamageBottomSkill, float damageMultiplier = 1f, bool hasPure = false, int pureValue = 0, int tilesUsed = 1, bool backward = false)
    {
        if (wavePrefab == null)
            return;

        GameObject waveObj = Instantiate(wavePrefab, spawnPosition, Quaternion.identity, waveParent);
        Wave wave = waveObj.GetComponent<Wave>();
        if (wave == null)
        {
            wave = waveObj.AddComponent<Wave>();
        }

        wave.Init(spawnPosition, color, 10f, gridPos, waveGroupId, isFirstWave, hasDamageBottomSkill, damageMultiplier, hasPure, pureValue, tilesUsed, backward);
    }

    /// <summary>
    /// 应用healWhenSpawn技能（整个wave group只回一次血）
    /// </summary>
    private void ApplyHealWhenSpawnForWaveGroup(TileColor waveColor, int tilesUsed, Vector2Int firstTilePos)
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;
        
        int colorIndex = (int)waveColor;
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
        
        // 检查是否有healWhenSpawn技能
        bool hasHealWhenSpawn = false;
        int healWhenSpawnValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "healWhenSpawn")
                {
                    hasHealWhenSpawn = true;
                    healWhenSpawnValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }
        
        if (!hasHealWhenSpawn)
            return;
        
        // 计算已损失血量
        int maxHealth = PlayerManager.Instance.MaxHealth;
        int currentHealth = PlayerManager.Instance.CurrentHealth;
        int lostHealth = maxHealth - currentHealth;
        
        if (lostHealth <= 0)
            return; // 没有损失血量，不需要恢复
        
        // 每个tile恢复 value% 的已损失血量，总回血量 = lostHealth × value% × tilesUsed
        float healPerTile = lostHealth * healWhenSpawnValue / 100f;
        int totalHeal = (int)(healPerTile * tilesUsed);
        
        if (totalHeal > 0)
        {
            PlayerManager.Instance.Heal(totalHeal);
            // 在第一个wave的位置显示回血数字
            Vector3 firstWavePos = boardManager.GridToWorldPosition(firstTilePos);
            DamageNumber.CreateDamageNumber(totalHeal, firstWavePos, true);
            // 回血效果已在PlayerManager.Heal()中创建
        }
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
        
        // 如果这个wave group的所有wave都完成了，记录wave group伤害并检查spawnAlly技能
        if (waveGroupActiveWaveCount[waveGroupId] <= 0)
        {
            // 记录wave group的总伤害
            if (waveGroupTotalDamage.ContainsKey(waveGroupId) && waveGroupColor.ContainsKey(waveGroupId))
            {
                float totalDamage = waveGroupTotalDamage[waveGroupId];
                TileColor waveColor = waveGroupColor[waveGroupId];
                if (StatisticsManager.Instance != null && totalDamage > 0)
                {
                    StatisticsManager.Instance.RecordWaveGroupDamage(waveColor, totalDamage);
                }
            }
            
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
                    
                    // 计算随从血量：总伤害 * value%（value是百分比值，例如50表示50%）
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
        
        // 所有wave group都完成了
        // 重置noAttackNoCost触发标志（新的玩家回合）
        noAttackNoCostTriggeredThisTurn = false;
        
        DOVirtual.DelayedCall(0.1f, () =>
        {
            isProcessing = false;
            
            // 检查puzzle模式：如果所有tiles都消除了，完成关卡
            if (currentLevelInfo != null && currentLevelInfo.type != null && currentLevelInfo.type.ToLower() == "puzzle")
            {
                if (CheckAllTilesCleared())
                {
                    CompleteLevel();
                    return;
                }
            }
            
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
        //目前只考虑基础伤害就好
        // if (waveGroupTotalDamage.ContainsKey(waveGroupId) && waveGroupActiveWaveCount.ContainsKey(waveGroupId))
        // {
        //     // 计算平均伤害（总伤害 / wave数量）
        //     int waveCount = waveGroupActiveWaveCount[waveGroupId];
        //     if (waveCount > 0)
        //     {
        //         waveDamage = waveGroupTotalDamage[waveGroupId] / waveCount;
        //     }
        // }
        
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
        if (boardManager == null || ally == null || allyProjectile == null)
            return;
            
        // 创建投射物GameObject
        GameObject projectileObj = Instantiate(allyProjectile);
        
        Vector3 startPos = ally.transform.position;
        // 目标位置：右侧（假设向右飞行10个单位）
        Vector3 targetPos = startPos + Vector3.right * 10f;
        
        projectileObj.transform.position = startPos;
        // 保持prefab的原始scale，不强制设置
        
        float projectileSpeed = 10f;
        float travelTime = 10f / projectileSpeed;
        
        Vector2Int allyGridPos = ally.GridPosition;
        bool hasHitEnemy = false; // 标记是否已击中敌人
        
        projectileObj.transform.DOMove(targetPos, travelTime)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                // 在移动过程中每帧检查是否碰到敌人
                if (hasHitEnemy || enemyManager == null)
                    return;
                
                // 将当前世界坐标转换为网格坐标
                Vector3 currentWorldPos = projectileObj.transform.position;
                Vector2Int currentGridPos = boardManager.WorldToGridPosition(currentWorldPos);
                
                // 检查当前位置是否有敌人
                foreach (var enemy in enemyManager.ActiveEnemies)
                {
                    if (enemy != null && !enemy.IsDead && enemy.GridPosition == currentGridPos)
                    {
                        // 击中敌人，立即停止移动并造成伤害
                        hasHitEnemy = true;
                        projectileObj.transform.DOKill(); // 停止移动
                        enemy.TakeDamage(damage, Vector3.right, false, 0, 0f);
                        Destroy(projectileObj); // 销毁投射物
                        return;
                    }
                }
            })
            .OnComplete(() =>
            {
                // 如果到达目标位置还没击中敌人，检查是否击中boss
                if (!hasHitEnemy)
                {
                    CheckAllyProjectileBossCollision(projectileObj, damage);
                }
                
                // 如果到达目标位置还没击中敌人，检查路径上的敌人（作为备用检查）
                if (!hasHitEnemy && enemyManager != null)
                {
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
    /// 检查ally投射物是否击中boss
    /// </summary>
    private void CheckAllyProjectileBossCollision(GameObject projectileObj, int damage)
    {
        if (currentBoss == null || currentBoss.IsDead || projectileObj == null)
            return;
            
        // 检查投射物是否与boss碰撞
        Vector3 projectilePos = projectileObj.transform.position;
        Vector3 bossPos = currentBoss.transform.position;
        float distanceX = Mathf.Abs(projectilePos.x - bossPos.x);
        float distanceY = Mathf.Abs(projectilePos.y - bossPos.y);
        float collisionRange = 0.5f; // 碰撞范围
        
        if (distanceX <= collisionRange && distanceY <= collisionRange)
        {
            // 击中boss
            projectileObj.transform.DOKill(); // 停止移动
            currentBoss.TakeDamage(damage, Vector3.right, false, 0, 0f);
            Destroy(projectileObj); // 销毁投射物
        }
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
        // 增加回合计数
        currentTurn++;
        
        // 检查回合数限制（奖励关卡）
        if (currentLevelInfo != null && currentLevelInfo.turns > 0)
        {
            if (currentTurn >= currentLevelInfo.turns)
            {
                // 回合数达到限制，结束关卡并默认胜利
                Debug.Log($"回合数达到限制 ({currentLevelInfo.turns})，关卡结束");
                CompleteLevel();
                return;
            }
        }
        
        // 检查是否已经赢了（在显示banner之前）
        // 如果是boss战，检查boss是否已死亡
        if (currentBoss != null && currentBoss.IsDead)
        {
            CompleteLevel();
            return;
        }
        
        // 如果是普通战斗，检查是否所有敌人已死亡且没有剩余敌人可生成
        if (currentBoss == null && enemyManager != null && enemyManager.CanCompleteLevel())
        {
            CompleteLevel();
            return;
        }
        
        currentState = GameState.EnemyTurn;
        isProcessing = true; // 敌人移动时也禁止操作

        // 显示"Enemy Turn" banner，等banner离开后再开始敌人行动
        // if (turnBanner != null)
        // {
        //     turnBanner.ShowBanner("Enemy Turn", () =>
        //     {
        //         // Banner离开后开始敌人行动
        //         ExecuteEnemyTurn();
        //     });
        // }
        // else
        {
            // 如果没有banner，直接执行敌人行动
            ExecuteEnemyTurn();
        }
    }
    
    /// <summary>
    /// 执行敌人回合
    /// </summary>
    private void ExecuteEnemyTurn()
    {
        // 如果是boss战，处理boss移动和召唤小怪
        if (currentBoss != null && !currentBoss.IsDead)
        {
            // 更新blockColor的剩余回合数（在玩家回合结束时减少）
            currentBoss.UpdateBlockColorTurns();
            
            // Boss执行技能（TakeAction）
            currentBoss.TakeAction();
            
            // Boss移动
            currentBoss.StartMove();
            
            // 先生成新敌人（如果需要）
            bool normalSpawnSucceeded = false;
            if (enemyManager != null)
            {
                // 检查是否还有敌人可以生成
                if (enemyManager.currentSpawnIndex < enemyManager.remainingEnemies.Count)
                {
                    enemyManager.SpawnEnemyEachTurn();
                    normalSpawnSucceeded = true;
                }
            }
            
            // 如果正常生成失败（所有敌人已生成完），则从boss战敌人列表循环生成
            if (!normalSpawnSucceeded)
            {
                SpawnBossBattleEnemy();
            }
            
            // 等待一小段时间让新敌人生成完成，然后执行敌人批次行动
            DOVirtual.DelayedCall(0.2f, () =>
            {
                if (enemyManager != null)
                {
                    enemyManager.ExecuteEnemyTurnBatch(() =>
                    {
                        // 敌人行动完成后，检查胜利条件
                        DOVirtual.DelayedCall(0.1f, () =>
                        {
                            // 检查boss是否死亡
                            if (currentBoss != null && currentBoss.IsDead)
                            {
                                CompleteLevel();
                                return;
                            }
                            
                            // 否则显示"Player Turn" banner，然后进入玩家回合
                            ShowPlayerTurnBanner();
                        });
                    });
                }
                else
                {
                    ShowPlayerTurnBanner();
                }
            });
        }
        else
        {
            // 普通战斗：先生成新敌人，然后执行敌人批次行动
            if (enemyManager != null)
            {
                // 先生成新敌人
                enemyManager.SpawnEnemyEachTurn();
                
                // 等待一小段时间让新敌人生成完成，然后执行敌人批次行动
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    enemyManager.ExecuteEnemyTurnBatch(() =>
                    {
                        // 敌人行动完成后，检查胜利条件
                        DOVirtual.DelayedCall(0.1f, () =>
                        {
                            // 检查是否可以完成关卡（所有敌人死亡且没有剩余敌人可生成）
                            if (enemyManager != null && enemyManager.CanCompleteLevel())
                            {
                                CompleteLevel();
                                return;
                            }
                            
                            // 否则显示"Player Turn" banner，然后进入玩家回合
                            ShowPlayerTurnBanner();
                        });
                    });
                });
            }
            else
            {
                ShowPlayerTurnBanner();
            }
        }
    }
    
    /// <summary>
    /// 显示"Player Turn" banner，然后进入玩家回合
    /// </summary>
    private void ShowPlayerTurnBanner()
    {
        // 触发教程信号：敌人回合结束
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SendSignal("endTurn");
        }
        // 每回合开始时恢复所有shield敌人的盾牌
        ResetAllEnemyShields();
        
        // 显示"Player Turn" banner
        // if (turnBanner != null)
        // {
        //     turnBanner.ShowBanner("Player Turn", () =>
        //     {
        //         // Banner离开后，允许玩家操作
        //         isProcessing = false;
        //         currentState = GameState.PlayerTurn;
        //     });
        // }
        // else
        {
            // 如果没有banner，直接进入玩家回合
            isProcessing = false;
            currentState = GameState.PlayerTurn;
        }
    }
    
    /// <summary>
    /// 生成Boss
    /// </summary>
    private void SpawnBoss(string bossIdentifier)
    {
        if (boardManager == null || string.IsNullOrEmpty(bossIdentifier))
            return;
            
        // 从enemyInfoMap获取boss信息
        if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(bossIdentifier))
        {
            Debug.LogWarning($"Boss identifier not found: {bossIdentifier}");
            return;
        }
        
        EnemyInfo bossInfo = CSVLoader.Instance.enemyInfoMap[bossIdentifier];
        
        // Boss位置：从上往下数第二行（y = boardHeight - 2），最右列再往右两格（x = boardWidth + 1）
        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;
        Vector2Int bossGridPos = new Vector2Int(boardWidth + 1, boardHeight - 2);
        Vector3 bossWorldPos = boardManager.GridToWorldPosition(bossGridPos);
        
        // 使用enemyPrefab或bossPrefab（如果设置了）
        GameObject prefabToUse = bossPrefab != null ? bossPrefab : (enemyManager != null ? enemyManager.enemyPrefab : null);
        if (prefabToUse == null)
        {
            Debug.LogError("Boss prefab not found!");
            return;
        }
        
        GameObject bossObj = Instantiate(prefabToUse, bossWorldPos, Quaternion.identity);
        Boss boss = bossObj.GetComponent<Boss>();
        if (boss == null)
        {
            boss = bossObj.AddComponent<Boss>();
        }
        
        // 根据difficulty计算boss属性
        int difficulty = currentLevelInfo != null ? currentLevelInfo.difficulty : 0;
        int calculatedHP = bossInfo.hp + difficulty * bossInfo.hpIncrease;
        
        // 应用boss初始血量减少（如果有）
        if (PlayerManager.Instance != null && PlayerManager.Instance.BossDamageReduction > 0)
        {
            float reductionPercent = PlayerManager.Instance.BossDamageReduction;
            calculatedHP = Mathf.RoundToInt(calculatedHP * (1f - reductionPercent / 100f));
            calculatedHP = Mathf.Max(1, calculatedHP); // 确保至少为1
        }
        
        // 初始化boss
        boss.InitBoss(bossGridPos, calculatedHP, bossInfo, boardManager);
        
        // 创建血条
        if (enemyManager != null)
        {
            enemyManager.CreateHealthBar(boss);
        }
        
        currentBoss = boss;
        Debug.Log($"Boss spawned: {bossIdentifier}, HP: {calculatedHP}");
    }
    
    /// <summary>
    /// Boss战中每回合召唤小怪
    /// </summary>
    private void SpawnBossBattleEnemy()
    {
        if (currentBoss == null || currentBoss.IsDead || bossBattleEnemies.Count == 0)
            return;
            
        // 如果所有敌人都召唤完了，从头开始
        if (bossBattleEnemyIndex >= bossBattleEnemies.Count)
        {
            bossBattleEnemyIndex = 0;
        }
        
        EnemySpawnInfo spawnInfo = bossBattleEnemies[bossBattleEnemyIndex];
        bossBattleEnemyIndex++;
        
        // 每次只生成一个敌人（从spawnInfo.count中取1个）
        if (enemyManager != null)
        {
            SpawnBossBattleEnemyFromInfo(spawnInfo.identifier);
        }
    }
    
    /// <summary>
    /// 从信息生成Boss战中的敌人
    /// </summary>
    private void SpawnBossBattleEnemyFromInfo(string identifier)
    {
        if (enemyManager == null || boardManager == null || string.IsNullOrEmpty(identifier))
            return;
            
        // 从enemyInfoMap获取敌人信息
        if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(identifier))
        {
            Debug.LogWarning($"Enemy identifier not found: {identifier}");
            return;
        }
        
        EnemyInfo enemyInfo = CSVLoader.Instance.enemyInfoMap[identifier];
        
        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;
        
        // 敌人生成在底线（最右侧，x = boardWidth - 1）
        int x = boardWidth - 1;
        
        // 找到一个不与其他敌人重叠的y位置
        int y = enemyManager.FindAvailableYPosition(x, boardHeight);
        if (y < 0)
        {
            Debug.LogWarning("无法找到可用的敌人生成位置");
            return;
        }
        
        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
        worldPos += new Vector3(0, enemyManager.SpawnOffsetY, 0);
        
        GameObject enemyObj = Instantiate(enemyManager.enemyPrefab, worldPos, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }
        
        // 根据difficulty计算敌人属性
        int difficulty = currentLevelInfo != null ? currentLevelInfo.difficulty : 0;
        int calculatedHP = enemyInfo.hp + difficulty * enemyInfo.hpIncrease;
        int calculatedAttack = enemyInfo.attack + difficulty * enemyInfo.attackIncrease;
        
        // 使用计算后的hp初始化
        enemy.Init(gridPos, calculatedHP, enemyInfo);
        // 设置计算后的攻击力
        enemy.SetAttack(calculatedAttack);
        
        // 创建血条
        enemyManager.CreateHealthBar(enemy);
        
        // 添加到EnemyManager
        enemyManager.ActiveEnemies.Add(enemy);
    }
    
    /// <summary>
    /// 获取当前Boss（用于wave和ally攻击）
    /// </summary>
    public static Boss GetCurrentBoss()
    {
        MainGameManager instance = FindObjectOfType<MainGameManager>();
        return instance != null ? instance.currentBoss : null;
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
                onRetryLevel: () =>
                {
                    // 重试当前关卡
                    RetryLevel();
                },
                onRestart: () =>
                {
                    // 重新开始游戏
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
    /// 重试当前关卡
    /// </summary>
    private void RetryLevel()
    {
        // 如果玩家等级 > 0，回到这场战斗前的商店页面
        // 如果玩家等级 == 0（第一关），重新开始这场战斗
        if (playerLevel > 0)
        {
            playerLevel--;
            // 回到商店页面
            // 先重置游戏状态
            isProcessing = false;
            currentState = GameState.PlayerTurn;
            
            // 清空棋盘（重试时需要重新开始）
            if (boardManager != null)
            {
                boardManager.ClearBoard();
            }
            
            // 清除战斗场景上的所有内容
            ClearBattleScene();
            
            // 关闭技能显示和敌人描述显示
            if (skillDisplayPanel != null)
            {
                skillDisplayPanel.SetActive(false);
            }
            if (enemyDescriptionPanel != null)
            {
                enemyDescriptionPanel.SetActive(false);
            }

            if (PlayerManager.Instance != null)
            {
                if (levelStartHealth >= 0)
                {
                    PlayerManager.Instance.SetHealth(levelStartHealth);
                }
                if (levelStartGold >= 0)
                {
                    PlayerManager.Instance.SetGold(levelStartGold);
                }
            }
            // 显示商店页面
            SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
            if (skillMenu != null)
            {
                skillMenu.ShowSkillSelection(
                    () =>
                    {
                        // 确认按钮点击，进入下一关
                        Debug.Log("确认配置，进入下一关");
                    }
                );
            }
            else
            {
                Debug.LogWarning("SkillSelectMenu not found, cannot show shop");
            }
        }
        else
        {
            // 第一关死亡，重新开始战斗
            // 恢复血量和金币到关卡开始前
            if (PlayerManager.Instance != null)
            {
                if (levelStartHealth >= 0)
                {
                    PlayerManager.Instance.SetHealth(levelStartHealth);
                }
                if (levelStartGold >= 0)
                {
                    PlayerManager.Instance.SetGold(levelStartGold);
                }
            }
            
            // 清除战斗场景上的所有内容
            ClearBattleScene();
            
            // 重新开始当前关卡
            StartBattle();
        }
    }

    /// <summary>
    /// 关卡完成
    /// </summary>
    public void CompleteLevel()
    {
        currentState = GameState.LevelComplete;
        isProcessing = true;

        int totalGoldEarned = 0;
        int levelGold = 0;
        
        // 掉落gold（使用当前关卡信息）
        if (currentLevelInfo != null && currentLevelInfo.gold > 0 && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddGold(currentLevelInfo.gold);
            levelGold = currentLevelInfo.gold;
            totalGoldEarned += currentLevelInfo.gold;
            Debug.Log($"关卡完成，获得 {currentLevelInfo.gold} gold");
        }
        
        // 如果是gold关卡，添加从chest获得的金钱
        if (currentLevelInfo != null && currentLevelInfo.type == "gold" && goldFromChests > 0)
        {
            totalGoldEarned += goldFromChests;
        }
        
        // 显示获得的金钱toast
        if (totalGoldEarned > 0 && ToastManager.Instance != null)
        {
            string message = $"Earned {totalGoldEarned} gold!";
            // if (currentLevelInfo != null && currentLevelInfo.type == "gold" && goldFromChests > 0)
            // {
            //     message = $"Earned {totalGoldEarned} gold! ({levelGold} from level + {goldFromChests} from chests)";
            // }
            ToastManager.Instance.ShowToast(message);
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
        
        // 离开战斗模式时清除战场
        ClearBattleScene();
        
        // 战斗结束后清除临时伤害加成并恢复exchange
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.EndBattle();
        }

        // 检查是否是最终胜利（战胜了levelInfo中的最后一个level）
        bool isGameWin = false;
        if (CSVLoader.Instance != null && CSVLoader.Instance.levelInfoMap != null && CSVLoader.Instance.levelInfoMap.Count > 0)
        {
            // 找到levelInfoMap中level值最大的那个
            int maxLevel = -1;
            foreach (var kvp in CSVLoader.Instance.levelInfoMap)
            {
                if (kvp.Value.level > maxLevel)
                {
                    maxLevel = kvp.Value.level;
                }
            }
            
            // 如果当前玩家等级已经达到或超过最大level，说明战胜了最后一个level
            if (maxLevel >= 0 && playerLevel >= maxLevel)
            {
                isGameWin = true;
            }
        }
        
        if (isGameWin)
        {
            // 显示胜利统计菜单
            StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
            if (statisticsMenu == null)
            {
                // 如果没有找到，创建一个新的
                GameObject menuObj = new GameObject("StatisticsMenu");
                statisticsMenu = menuObj.AddComponent<StatisticsMenu>();
            }
            statisticsMenu.ShowWinStatistics();
        }
        else
        {
            // 战斗-事件-商店-战斗的循环
            // 先显示事件，然后显示商店
            ShowEventMenu();
        }
    }
    
    /// <summary>
    /// 显示事件菜单
    /// </summary>
    private void ShowEventMenu()
    {
        // 检查是否有eventType，如果没有则直接进入商店
        if (currentLevelInfo == null || string.IsNullOrEmpty(currentLevelInfo.eventType))
        {
            // eventType为空，不显示事件，直接进入商店
            ShowShopMenu();
            return;
        }
        
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("EventMenu");
            eventMenu = menuObj.AddComponent<EventMenu>();
        }
        
        // 显示对应类型的事件
        eventMenu.ShowEventByType(currentLevelInfo.eventType, () =>
        {
            // 事件完成后，显示商店
            ShowShopMenu();
        });
    }
    
    /// <summary>
    /// 显示商店菜单
    /// </summary>
    private void ShowShopMenu()
    {
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
                // 商店完成后，进入下一关战斗
                PlayerLevelUp();
            }
        );
    }
    
    /// <summary>
    /// 清除战斗场景上的所有内容（敌人、ally、boss、fog、dirt等）
    /// </summary>
    private void ClearBattleScene()
    {
        // 清除所有敌人
        if (enemyManager != null)
        {
            enemyManager.ClearAllEnemies();
        }
        
        // 清除boss
        if (currentBoss != null)
        {
            Destroy(currentBoss.gameObject);
            currentBoss = null;
        }
        
        // 清除所有随从
        if (allyManager == null)
        {
            allyManager = FindObjectOfType<AllyManager>();
        }
        if (allyManager != null)
        {
            allyManager.ClearAllAllies();
        }
        
        // 清除所有fog和dirt
        if (boardManager != null)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                for (int y = 0; y < boardManager.Height; y++)
                {
                    TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                    if (tile != null)
                    {
                        if (tile.HasFog)
                        {
                            tile.SetFog(false);
                        }
                        if (tile.IsDirty)
                        {
                            tile.SetDirty(false);
                        }
                    }
                }
            }
        }
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
                DamageNumber.CreateDamageNumber(damage, attackPos, false);
                
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

    /// <summary>
    /// 恢复所有shield敌人的盾牌（每回合开始时调用）
    /// </summary>
    private void ResetAllEnemyShields()
    {
        if (enemyManager == null)
            return;
            
        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.ResetShield();
            }
        }
    }
    
    /// <summary>
    /// 重新开始（重新加载场景）
    /// </summary>
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>
    /// 处理编辑模式快捷键输入（x进入编辑，s保存，l加载，p进入puzzle游戏）
    /// </summary>
    private void HandleEditModeInput()
    {
        // X键：进入编辑模式
        if (Input.GetKeyDown(KeyCode.X))
        {
            EnterPuzzleEditMode();
        }
        
        // S键：保存当前puzzle
        if (Input.GetKeyDown(KeyCode.S) && isPuzzleEditMode)
        {
            SaveCurrentPuzzle();
        }
        
        // L键：加载第一个puzzle
        if (Input.GetKeyDown(KeyCode.L) && isPuzzleEditMode)
        {
            LoadFirstPuzzle();
        }
        
        // P键：进入puzzle游戏模式
        if (Input.GetKeyDown(KeyCode.P) && isPuzzleEditMode)
        {
            EnterPuzzlePlayMode();
        }
    }
    
    /// <summary>
    /// 进入puzzle编辑模式
    /// </summary>
    private void EnterPuzzleEditMode()
    {
        isPuzzleEditMode = true;
        currentState = GameState.PuzzleEdit;
        isProcessing = false;
        
        // 清除所有敌人
        if (enemyManager != null)
        {
            enemyManager.ClearAllEnemies();
        }
        if (currentBoss != null)
        {
            Destroy(currentBoss.gameObject);
            currentBoss = null;
        }
        
        // 初始化puzzle数据（8x6）
        currentPuzzleData = new int[8, 6];
        
        // 将所有格子变为黑色（删除所有格子，颜色值-1表示黑色/空）
        if (boardManager != null)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                    if (tile != null)
                    {
                        // 删除格子
                        Destroy(tile.gameObject);
                        boardManager.SetTile(new Vector2Int(x, y), null);
                    }
                    currentPuzzleData[x, y] = -1; // -1表示空/黑色
                }
            }
        }
        
        Debug.Log("进入Puzzle编辑模式");
    }
    
    /// <summary>
    /// 处理puzzle编辑模式输入
    /// </summary>
    private void HandlePuzzleEditInput()
    {
        if (boardManager == null || currentPuzzleData == null)
            return;
        
        Vector2Int gridPos = GetMouseGridPosition();
        if (!boardManager.IsValidPosition(gridPos))
            return;
        
        // 检查是否按住数字键1-4
        int colorIndex = -1;
        if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1))
            colorIndex = 0;
        else if (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2))
            colorIndex = 1;
        else if (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3))
            colorIndex = 2;
        else if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
            colorIndex = 3;
        
        if (colorIndex >= 0)
        {
            // 左键：设置格子颜色
            if (Input.GetMouseButtonDown(0))
            {
                SetTileColor(gridPos, colorIndex);
            }
            // 右键：插入格子（向右挤）
            else if (Input.GetMouseButtonDown(1))
            {
                InsertTileAtPosition(gridPos, colorIndex);
            }
        }
    }
    
    /// <summary>
    /// 设置格子颜色
    /// </summary>
    private void SetTileColor(Vector2Int gridPos, int colorIndex)
    {
        if (boardManager == null || currentPuzzleData == null)
            return;
        
        TileCell tile = boardManager.GetTile(gridPos);
        if (tile == null)
        {
            // 如果格子不存在，创建一个
            if (boardManager.TileCellPrefab != null)
            {
                GameObject tileObj = Instantiate(boardManager.TileCellPrefab, boardManager.BoardParent);
                tile = tileObj.GetComponent<TileCell>();
                if (tile == null)
                {
                    tile = tileObj.AddComponent<TileCell>();
                }
                
                Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
                tileObj.transform.position = worldPos;
                tile.Init((TileColor)colorIndex, gridPos);
                boardManager.SetTile(gridPos, tile);
            }
        }
        else
        {
            tile.SetColor((TileColor)colorIndex);
        }
        
        currentPuzzleData[gridPos.x, gridPos.y] = colorIndex;
    }
    
    /// <summary>
    /// 在指定位置插入格子（向右挤）
    /// </summary>
    private void InsertTileAtPosition(Vector2Int gridPos, int colorIndex)
    {
        if (boardManager == null || currentPuzzleData == null)
            return;
        
        // 检查该行是否已满
        bool isRowFull = true;
        for (int x = 0; x < 8; x++)
        {
            if (boardManager.GetTile(new Vector2Int(x, gridPos.y)) == null)
            {
                isRowFull = false;
                break;
            }
        }
        
        if (isRowFull)
        {
            Debug.Log("该行已满，无法插入");
            return;
        }
        
        // 从右往左移动所有格子（从插入位置右侧的所有格子都向右移动）
        for (int x = 7; x > gridPos.x; x--)
        {
            Vector2Int sourcePos = new Vector2Int(x - 1, gridPos.y);
            TileCell tile = boardManager.GetTile(sourcePos);
            
            Vector2Int newPos = new Vector2Int(x, gridPos.y);
            
            // 移动tile到新位置
            if (tile != null)
            {
                boardManager.SetTile(newPos, tile);
                boardManager.SetTile(sourcePos, null);
                tile.SetGridPosition(newPos);
                
                Vector3 targetPos = boardManager.GridToWorldPosition(newPos);
                tile.FallAnimation(targetPos);
            }
            else
            {
                // 即使tile为null，也要更新puzzle数据
                boardManager.SetTile(newPos, null);
            }
            
            // 更新puzzle数据
            currentPuzzleData[x, gridPos.y] = currentPuzzleData[x - 1, gridPos.y];
        }
        
        // 在指定位置创建新格子
        SetTileColor(gridPos, colorIndex);
    }
    
    /// <summary>
    /// 保存当前puzzle
    /// </summary>
    private void SaveCurrentPuzzle()
    {
        if (currentPuzzleData == null || PuzzleManager.Instance == null)
            return;
        
        PuzzleManager.Instance.SavePuzzle(currentPuzzleData);
        Debug.Log("Puzzle已保存");
    }
    
    /// <summary>
    /// 加载第一个puzzle
    /// </summary>
    private void LoadFirstPuzzle()
    {
        if (PuzzleManager.Instance == null || boardManager == null)
            return;
        
        int[,] puzzleData = PuzzleManager.Instance.LoadFirstPuzzle();
        if (puzzleData == null)
            return;
        
        currentPuzzleData = puzzleData;
        
        // 清空棋盘
        boardManager.ClearBoard();
        boardManager.InitializeBoard();
        
        // 根据puzzle数据生成tiles
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                int colorValue = puzzleData[x, y];
                if (colorValue >= 0 && colorValue < 4) // 有效的颜色值
                {
                    GameObject tileObj = Instantiate(boardManager.TileCellPrefab, boardManager.BoardParent);
                    TileCell tile = tileObj.GetComponent<TileCell>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileCell>();
                    }
                    
                    Vector3 worldPos = boardManager.GridToWorldPosition(new Vector2Int(x, y));
                    tileObj.transform.position = worldPos;
                    tile.Init((TileColor)colorValue, new Vector2Int(x, y));
                    boardManager.SetTile(new Vector2Int(x, y), tile);
                }
            }
        }
        
        Debug.Log("Puzzle已加载");
    }
    
    /// <summary>
    /// 进入puzzle游戏模式
    /// </summary>
    private void EnterPuzzlePlayMode()
    {
        if (currentPuzzleData == null)
        {
            Debug.LogWarning("没有puzzle数据，无法进入游戏模式");
            return;
        }
        
        isPuzzleEditMode = false;
        currentState = GameState.PuzzlePlay;
        isProcessing = false;
        
        // 重置回合计数
        currentTurn = 0;
        
        Debug.Log("进入Puzzle游戏模式");
    }
    
    /// <summary>
    /// 处理puzzle游戏模式输入（只能右键消除，禁用左键移动）
    /// </summary>
    private void HandlePuzzlePlayInput()
    {
        if (isProcessing)
            return;
        
        // 只允许右键消除
        if (Input.GetMouseButtonDown(1))
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                ClearHighlights();
                EliminateConnectedTiles(gridPos);
            }
        }
        
        // 禁用左键移动和交换
        // 可以保留鼠标悬停高亮
        HandleMouseHover();
    }
    
    /// <summary>
    /// 加载puzzle关卡
    /// </summary>
    private void LoadPuzzleLevel()
    {
        if (currentLevelInfo == null || string.IsNullOrEmpty(currentLevelInfo.typeIdentifier))
        {
            Debug.LogWarning("Puzzle关卡缺少typeIdentifier");
            return;
        }
        
        if (PuzzleManager.Instance == null || boardManager == null)
            return;
        
        // 加载puzzle数据
        int[,] puzzleData = PuzzleManager.Instance.LoadPuzzle(currentLevelInfo.typeIdentifier);
        if (puzzleData == null)
        {
            Debug.LogWarning($"无法加载puzzle: {currentLevelInfo.typeIdentifier}");
            return;
        }
        
        // 清空棋盘
        boardManager.ClearBoard();
        boardManager.InitializeBoard();
        
        // 根据puzzle数据生成tiles
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                int colorValue = puzzleData[x, y];
                if (colorValue >= 0 && colorValue < 4) // 有效的颜色值
                {
                    GameObject tileObj = Instantiate(boardManager.TileCellPrefab, boardManager.BoardParent);
                    TileCell tile = tileObj.GetComponent<TileCell>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileCell>();
                    }
                    
                    Vector3 worldPos = boardManager.GridToWorldPosition(new Vector2Int(x, y));
                    tileObj.transform.position = worldPos;
                    tile.Init((TileColor)colorValue, new Vector2Int(x, y));
                    boardManager.SetTile(new Vector2Int(x, y), tile);
                }
            }
        }
        
        // 不生成敌人
        if (enemyManager != null)
        {
            enemyManager.ClearAllEnemies();
        }
    }
    
    /// <summary>
    /// 检查所有tiles是否都已清除（puzzle模式）
    /// </summary>
    private bool CheckAllTilesCleared()
    {
        if (boardManager == null)
            return false;
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                if (tile != null)
                {
                    return false; // 还有tile存在
                }
            }
        }
        
        return true; // 所有tiles都已清除
    }
}


