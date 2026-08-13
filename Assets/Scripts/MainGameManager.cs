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
    
    [Header("随从描述显示UI")]
    [SerializeField] private GameObject allyDescriptionPanel;
    [SerializeField] private TMP_Text allyDescriptionText;
    
    [Header("回合横幅UI")]
    [SerializeField] private TurnBanner turnBanner;
    [Header("地图UI")]
    [SerializeField] private MapController mapController;

    private GameObject alwaysForBattleAndUi;
    private AlwaysBattleAndUiController alwaysBattleAndUiController;

    public bool showShopAtBeginning;
    public bool showEventAtBeginning;
    [SerializeField] public GameObject allyPrefab;
    [SerializeField] public GameObject allyProjectile;
    [SerializeField] public GameObject damagePrefab;
    
    [Header("Puzzle设置")]
    public bool isPublish = false; // 发布模式，禁用所有编辑快捷键
    
    public List<Sprite> tileSprites;

    public float switchTime = 0.25f;
    
    
    
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
    public bool IsProcessing => isProcessing;
    private Vector2Int selectedTilePos = new Vector2Int(-1, -1);
    private Vector2Int dragStartPos = new Vector2Int(-1, -1);
    private bool isDragging = false;
    private Vector2Int currentHoverTilePos = new Vector2Int(-1, -1); // 当前鼠标悬停的格子
    
    // 消耗品相邻交换模式
    private bool isConsumableSwapMode;
    private string consumableSwapIdentifier;
    private Vector2Int consumableSwapStartPos = new Vector2Int(-1, -1);
    private bool isConsumableSwapDragging;
    private Vector2Int consumableSwapHoverPos = new Vector2Int(-1, -1);

    // 右键 toggle 详情
    private Vector2Int toggledSkillGridPos = new Vector2Int(-1, -1);
    private Enemy toggledEnemy;
    private Ally toggledAlly;
    
    // 高亮显示相关
    private Vector2Int lastHighlightPos = new Vector2Int(-1, -1);
    private Vector2Int highlightSourcePos = new Vector2Int(-1, -1);
    private List<Vector2Int> highlightedTiles = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> allySkillPreviewTiles = new HashSet<Vector2Int>();
    private bool leftMouseDownInBattle;

    // 技能显示相关
    private TileColor currentDisplayColor = TileColor.Red;

    // 玩家等级（起始为0，打一场架升一级）
    private int playerLevel = 0;
    public int PlayerLevel => playerLevel;
    private int nextBattleLevelIndex = 0;
    public int NextBattleLevelIndex => nextBattleLevelIndex;
    private int currentIslandId = 0;
    public int CurrentIslandId => currentIslandId;
    private int islandNonBossProgress = 0;
    private bool battleFromBossMapNode;
    private bool activeBattleFromBossMapNode;
    private MapNode activeBattleMapNode;
    private int activeBattleLevelIndex = -1;
    private bool waitingForNextIslandSelection;
    private int waitingIslandId = -1;
    private int waitingNextIslandId = -1;
    
    // 是否是第一次战斗（用于决定是否清除和生成格子）
    private bool isFirstBattle = true;
    private bool isMapOpen = false;
    
    // 保存关卡开始时的血量（用于重试关卡）
    private int levelStartHealth = -1;
    
    // 保存关卡开始时的金币（用于重试关卡）
    private int levelStartGold = -1;
    
    // 回合计数（用于奖励关卡）
    private int currentTurn = 0;
    
    // 跟踪gold关卡中从chest（敌人）获得的金钱
    private int goldFromChests = 0;

    // 战斗结果界面待领取奖励
    private int pendingBattleDisplayGold;
    private int pendingBattleGoldToGrant;
    private string pendingBattleConsumableId;
    
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
    /// 检查是否是战斗的第一回合
    /// </summary>
    /// <returns>如果是第一回合返回true，否则返回false</returns>
    public bool IsFirstTurn()
    {
        // currentTurn == 0 表示第一回合的玩家回合
        // currentTurn == 1 表示第一回合的敌人回合
        // 所以 currentTurn <= 1 表示第一回合（包括玩家和敌人回合）
        return currentTurn <= 0;
    }
    
    /// <summary>
    /// 获取当前回合数
    /// </summary>
    /// <returns>当前回合数（从0开始，0表示第一回合的玩家回合）</returns>
    public int GetCurrentTurn()
    {
        return currentTurn;
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
        AdvanceIslandBattleProgress();
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
    
    // 经过同色tile效果：每个 wave group 内每格只触发一次
    private static Dictionary<int, HashSet<Vector2Int>> waveGroupPassedSameColorTiles = new Dictionary<int, HashSet<Vector2Int>>();

    // hitSameIncreaseDamage：同一 wave group 内对同一敌人的命中次数
    private static Dictionary<int, Dictionary<Enemy, int>> waveGroupEnemyHitCounts = new Dictionary<int, Dictionary<Enemy, int>>();

    // allyIncreaseDamage：整组固定增伤平均分配到实际生成的 wave。
    private static Dictionary<int, float> waveGroupAllyDamageBonusPerWave = new Dictionary<int, float>();
    // allyDieIncreaseDamage：每个颜色在当前战斗内累计的永久增伤百分比。
    private static readonly float[] allyDeathDamageBonusByColor = new float[4];
    
    // noAttackNoCost技能管理 - 跟踪每个wave group是否造成伤害
    private static Dictionary<int, bool> waveGroupHasDamage = new Dictionary<int, bool>();
    private static bool noAttackNoCostTriggeredThisTurn = false; // 一个回合只会触发一次
    private static HashSet<int> pendingWaveGroups = new HashSet<int>(); // 等待结算完成的wave group
    private static int pendingDelayedSkillEffects = 0; // bounce 等延迟特效未完成数量
    private bool earlyTurnEndScheduled = false; // 是否已因 note 离盘而提前调度敌人回合
    private Coroutine pendingCompleteTurnRoutine;
    private Coroutine pendingEarlyTurnEndRoutine;
    private int stuckProcessingFrames;
    private static bool hasLastManualWaveColor = false;
    private static TileColor lastManualWaveColor;

    public static bool HasPendingDelayedSkillEffects => pendingDelayedSkillEffects > 0;

    public static void BeginDelayedSkillEffect()
    {
        pendingDelayedSkillEffects++;
    }

    public static void EndDelayedSkillEffect()
    {
        pendingDelayedSkillEffects = Mathf.Max(0, pendingDelayedSkillEffects - 1);
        if (pendingDelayedSkillEffects > 0 || Instance == null)
            return;

        Instance.TryEarlyEndPlayerTurnFromWaves();
        Instance.CheckAllWaveGroupsCompleted();
    }

    public static float GetAllyDamageBonusPerWave(int waveGroupId)
    {
        return waveGroupAllyDamageBonusPerWave.TryGetValue(waveGroupId, out float bonus) ? bonus : 0f;
    }

    public static float GetAllyDeathDamageBonus(TileColor color)
    {
        int colorIndex = (int)color;
        return colorIndex >= 0 && colorIndex < allyDeathDamageBonusByColor.Length
            ? allyDeathDamageBonusByColor[colorIndex]
            : 0f;
    }

    /// <summary>
    /// Ally 死亡时，让所有装备了 allyDieIncreaseDamage 的颜色获得本场战斗增伤。
    /// </summary>
    public static void NotifyAllyDied()
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null || CSVLoader.Instance == null)
            return;

        for (int colorIndex = 0; colorIndex < allyDeathDamageBonusByColor.Length; colorIndex++)
        {
            foreach (string identifier in PlayerManager.Instance.GetWaveSkills(colorIndex))
            {
                if (!SkillManager.Instance.HasSkill(identifier)
                    || !CSVLoader.Instance.cardInfoMap.TryGetValue(identifier, out SkillInfo skillInfo)
                    || skillInfo == null
                    || skillInfo.effect != "allyDieIncreaseDamage")
                    continue;

                allyDeathDamageBonusByColor[colorIndex] += SkillManager.Instance.GetSkillValue(identifier);
                break;
            }
        }
    }

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
        
        // 初始化随从描述显示UI
        InitAllyDescriptionUI();
        
        // 初始化回合横幅UI
        InitTurnBanner();

        GameObject battleDecor = GameObject.Find("always for battle and ui");
        if (battleDecor != null)
        {
            alwaysForBattleAndUi = battleDecor;
            alwaysBattleAndUiController = battleDecor.GetComponent<AlwaysBattleAndUiController>();
            if (alwaysBattleAndUiController == null)
            {
                alwaysBattleAndUiController = battleDecor.AddComponent<AlwaysBattleAndUiController>();
            }

        }

        StartCoroutine(OpenMapAfterTutorialFlow());
    }

    public bool IsInPuzzleEditMode => isPuzzleEditMode;
    public bool IsPublishMode => isPublish;
    public bool IsInActiveBattle => !isMapOpen
        && currentState != GameState.GameOver
        && boardManager != null
        && boardManager.gameObject.activeSelf;
    
    private IEnumerator OpenMapAfterTutorialFlow()
    {
        // 等一帧，避免和TutorialManager.Start执行顺序冲突
        yield return null;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInTutorial)
        {
            while (TutorialManager.Instance != null && TutorialManager.Instance.IsInTutorial)
            {
                yield return null;
            }
        }

        OpenMap();
    }

    public void StartBattleFromMap(MapNode node = null)
    {
        activeBattleMapNode = node;
        battleFromBossMapNode = node != null && node.IsBossNode;
        StartBattle();
    }

    /// <summary>
    /// 地图商店节点：可刷新、正常扣费
    /// </summary>
    public void ShowMapShop(System.Action onComplete)
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>(true);
        if (skillMenu == null)
        {
            onComplete?.Invoke();
            return;
        }

        skillMenu.ShowSkillSelection(onComplete, SkillSelectMenu.ShopMode.MapShop);
    }

    /// <summary>
    /// 战斗胜利奖励商店：免费三选一、无刷新、选后隐藏其余
    /// </summary>
    public void ShowBattleRewardShop(System.Action onComplete)
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>(true);
        if (skillMenu == null)
        {
            onComplete?.Invoke();
            return;
        }

        skillMenu.ShowSkillSelection(onComplete, SkillSelectMenu.ShopMode.BattleReward);
    }

    public void OpenMap()
    {
        isMapOpen = true;
        ClearHighlights();
        SetBattleViewVisible(false);

        if (mapController == null)
        {
            mapController = FindObjectOfType<MapController>(true);
        }

        if (mapController != null)
        {
            if (waitingForNextIslandSelection && waitingIslandId >= 0)
            {
                mapController.SetForcedIsland(waitingIslandId);
            }
            else
            {
                mapController.ClearForcedIsland();
            }
            mapController.OpenMap();
        }
        else
        {
            Debug.LogWarning("MainGameManager: 未找到MapController，回退为直接开战");
            StartBattle();
        }

        RefreshAlwaysBattleAndUi();
    }

    private void SetBattleViewVisible(bool visible)
    {
        BattleUI battleUI = FindObjectOfType<BattleUI>(true);
        if (battleUI != null)
        {
            battleUI.gameObject.SetActive(visible);
        }

        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
        if (boardManager != null)
        {
            boardManager.gameObject.SetActive(visible);
        }
    }

    private void RefreshAlwaysBattleAndUi()
    {
        if (alwaysForBattleAndUi != null && !alwaysForBattleAndUi.activeSelf)
        {
            alwaysForBattleAndUi.SetActive(true);
        }

        alwaysBattleAndUiController?.RefreshDisplay();

        ConsumableView consumableView = alwaysForBattleAndUi != null
            ? alwaysForBattleAndUi.GetComponentInChildren<ConsumableView>(true)
            : FindObjectOfType<ConsumableView>(true);
        consumableView?.Refresh();
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
        // 从BattleUI获取固定的UI元素
        BattleUI battleUI = FindObjectOfType<BattleUI>();
        if (battleUI != null)
        {
            skillDisplayPanel = battleUI.GetSkillDisplayPanel();
            skillDisplayText = battleUI.GetSkillDisplayText();
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
        // 从BattleUI获取固定的UI元素
        BattleUI battleUI = FindObjectOfType<BattleUI>();
        if (battleUI != null)
        {
            enemyDescriptionPanel = battleUI.GetEnemyDescriptionPanel();
            enemyDescriptionText = battleUI.GetEnemyDescriptionText();
        }

        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化随从描述显示UI
    /// </summary>
    private void InitAllyDescriptionUI()
    {
        // 从BattleUI获取固定的UI元素
        BattleUI battleUI = FindObjectOfType<BattleUI>();
        if (battleUI != null)
        {
            allyDescriptionPanel = battleUI.GetAllyDescriptionPanel();
            allyDescriptionText = battleUI.GetAllyDescriptionText();
        }

        if (allyDescriptionPanel != null)
        {
            allyDescriptionPanel.SetActive(false);
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
        isMapOpen = false;
        SetBattleViewVisible(true);
        RefreshAlwaysBattleAndUi();

        // 重置游戏状态
        isProcessing = false;
        currentState = GameState.PlayerTurn;
        noAttackNoCostTriggeredThisTurn = false; // 重置noAttackNoCost触发标志
        pendingDelayedSkillEffects = 0;
        earlyTurnEndScheduled = false;
        stuckProcessingFrames = 0;
        if (pendingCompleteTurnRoutine != null)
        {
            StopCoroutine(pendingCompleteTurnRoutine);
            pendingCompleteTurnRoutine = null;
        }
        if (pendingEarlyTurnEndRoutine != null)
        {
            StopCoroutine(pendingEarlyTurnEndRoutine);
            pendingEarlyTurnEndRoutine = null;
        }
        hasLastManualWaveColor = false;
        waveGroupAllyDamageBonusPerWave.Clear();
        for (int i = 0; i < allyDeathDamageBonusByColor.Length; i++)
            allyDeathDamageBonusByColor[i] = 0f;
        isDragging = false;
        dragStartPos = new Vector2Int(-1, -1);
        currentHoverTilePos = new Vector2Int(-1, -1);
        lastHighlightPos = new Vector2Int(-1, -1);
        leftMouseDownInBattle = false;
        ClearHighlights();
        ExitConsumableSwapMode();
        HideAllDetailPanels();

        // 初始化PlayerManager并恢复交换次数
        if (PlayerManager.Instance != null)
        {
            // 保存关卡开始时的血量和金币（用于重试关卡）
            levelStartHealth = PlayerManager.Instance.CurrentHealth;
            levelStartGold = PlayerManager.Instance.Gold;
            PlayerManager.Instance.StartBattle();
        }

        // 进入战斗：普通节点按进度顺序；地图 Boss 节点强制使用当前岛屿的 type=boss 关卡
        activeBattleFromBossMapNode = battleFromBossMapNode;
        int islandId = GetBattleIslandId();
        currentIslandId = islandId;
        int levelIndex = -1;

        if (battleFromBossMapNode)
        {
            if (LevelManager.Instance.TryGetBossLevelForIsland(islandId, out LevelInfo bossLevel, out int bossLevelIndex))
            {
                currentLevelInfo = bossLevel;
                levelIndex = bossLevelIndex;
            }
            else
            {
                Debug.LogWarning($"Boss 地图节点但岛屿 {islandId} 未配置 type=boss 的关卡，回退为该岛顺序关卡");
                LevelManager.Instance.TryGetNthNonBossLevelForIsland(islandId, islandNonBossProgress, out currentLevelInfo, out levelIndex);
            }
        }
        else if (!LevelManager.Instance.TryGetNthNonBossLevelForIsland(islandId, islandNonBossProgress, out currentLevelInfo, out levelIndex))
        {
            Debug.LogWarning($"岛屿 {islandId} 第 {islandNonBossProgress} 个非 Boss 关不存在，尝试 Boss 关");
            if (!LevelManager.Instance.TryGetBossLevelForIsland(islandId, out currentLevelInfo, out levelIndex))
            {
                currentLevelInfo = LevelManager.Instance.GetLevelByIndex(0);
                levelIndex = 0;
            }
        }

        if (levelIndex < 0)
            levelIndex = currentLevelInfo != null ? currentLevelInfo.level : 0;

        activeBattleLevelIndex = levelIndex;
        nextBattleLevelIndex = levelIndex;

        int levelHeight = currentLevelInfo != null ? currentLevelInfo.height : 0;
        bool heightChanged = boardManager != null && levelHeight > 0 && boardManager.Height != levelHeight;
        if (boardManager != null && (isFirstBattle || heightChanged))
        {
            boardManager.ClearBoard();
            boardManager.InitializeBoard(-1, levelHeight > 0 ? levelHeight : -1);
            boardManager.GenerateRandomColors();
            isFirstBattle = false;
        }

        bool isBossFight = LevelManager.IsBossLevel(currentLevelInfo);
        
        // 如果是boss战斗，先显示"Boss Fight!"横幅
        if (isBossFight && turnBanner != null)
        {
            turnBanner.ShowBanner("Boss Fight!", () =>
            {
                // 横幅显示完成后，继续初始化战斗
                InitializeBattleAfterBanner();
            });
        }
        else
        {
            // 不是boss战斗，直接初始化
            InitializeBattleAfterBanner();
        }
    }
    
    /// <summary>
    /// 在显示Boss Fight横幅后初始化战斗（或直接调用，如果不是boss战斗）
    /// </summary>
    private void InitializeBattleAfterBanner()
    {
        
        // 初始化回合计数（用于奖励关卡）
        currentTurn = 0;
        if (currentLevelInfo != null && enemyManager != null)
        {
            enemyManager.SpawnEnemiesFromLevel(currentLevelInfo);
            
            // Boss 关卡：生成 Boss 实体
            if (LevelManager.IsBossLevel(currentLevelInfo) && !string.IsNullOrEmpty(currentLevelInfo.bossIdentifier))
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

        battleFromBossMapNode = false;

        // 开始新回合统计
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.StartNewRound();
        }

        
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
        if (currentState == GameState.GameOver)
            return;

        // 敌人攻击逻辑现在由Enemy.TakeAction()处理，不再需要在这里检查

        // 检查玩家是否死亡
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsDead)
        {
            GameOver();
            return;
        }

        // 编辑模式或puzzle游戏模式
        if (currentState == GameState.PuzzlePlay)
        {
            // Puzzle游戏模式：只能右键消除，禁用左键移动
            HandlePuzzlePlayInput();
        }
        // 玩家回合 - 处理输入和鼠标悬停（只有在没有处理中时，且不在地图界面）
        else if (currentState == GameState.PlayerTurn && !isProcessing && !isMapOpen)
        {
            HandlePlayerInput();

            if (Input.touchCount == 0)
                HandleMouseHover();
            
            // 区分鼠标和触屏输入，避免冲突
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
        }
        else if (isProcessing)
        {
            // 其他处理中状态时清除高亮
            ClearHighlights();

            if (currentState == GameState.Processing && pendingWaveGroups.Count > 0)
                TryEarlyEndPlayerTurnFromWaves();
        }

        if (currentState == GameState.Processing
            && pendingWaveGroups.Count == 0
            && pendingDelayedSkillEffects == 0)
        {
            stuckProcessingFrames++;
            if (stuckProcessingFrames >= 60)
            {
                stuckProcessingFrames = 0;
                Debug.LogWarning("Processing 卡住，强制进入敌人回合");
                EndPlayerTurn();
            }
        }
        else
        {
            stuckProcessingFrames = 0;
        }
        
        // 如果事件菜单打开，清除高亮
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
        {
            ClearHighlights();
        }
        
        // 如果设置菜单打开，清除高亮
        SettingMenu settingMenu = FindObjectOfType<SettingMenu>();
        if (settingMenu != null && settingMenu.IsActive)
        {
            ClearHighlights();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandlePlayerInput()
    {
        if (isProcessing)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsBlockingInput)
            return;

        if (StartAnimManager.Instance != null && StartAnimManager.Instance.isBlocking)
            return;

        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu != null && statisticsMenu.IsActive)
            return;

        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
            return;

        SettingMenu settingMenu = FindObjectOfType<SettingMenu>();
        if (settingMenu != null && settingMenu.IsActive)
            return;

        if (isConsumableSwapMode)
        {
            HandleConsumableSwapInput();
            return;
        }

        if (Input.GetMouseButtonDown(1) && !ConsumableView.IsPointerOverConsumableUI())
        {
            HandleRightClickToggleDetail();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !ConsumableView.IsPointerOverConsumableUI())
        {
            leftMouseDownInBattle = true;
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                Vector2Int highlightPos = ResolveHighlightSourcePos(gridPos);
                lastHighlightPos = highlightPos;
                currentHoverTilePos = highlightPos;
                UpdateHighlightTiles(highlightPos);
                StartPressColorPulseOnHighlightedTiles();
            }
        }

        if (Input.GetMouseButtonUp(0) && !ConsumableView.IsPointerOverConsumableUI())
        {
            bool hadBattleMouseDown = leftMouseDownInBattle;
            leftMouseDownInBattle = false;

            Vector2Int gridPos = GetMouseGridPosition();
            bool isValidGridPos = boardManager != null && boardManager.IsValidPosition(gridPos);
            Vector2Int eliminatePos = ResolveHighlightSourcePos(gridPos);

            StopPressColorPulseOnHighlightedTiles();
            HideAllDetailPanels();

            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsRightClickEnabled)
                return;

            if (hadBattleMouseDown && isValidGridPos)
            {
                TryPlayPlayerAttackAnimation();
                EliminateConnectedTiles(eliminatePos);
            }
        }
    }

    /// <summary>
    /// 处理鼠标悬停高亮
    /// </summary>
    private void HandleMouseHover()
    {
        if (isProcessing || isConsumableSwapMode)
        {
            ClearHighlights();
            return;
        }

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsBlockingInput)
            return;

        if (StartAnimManager.Instance != null && StartAnimManager.Instance.isBlocking)
            return;

        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu != null && statisticsMenu.IsActive)
            return;

        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
            return;

        SettingMenu settingMenu = FindObjectOfType<SettingMenu>();
        if (settingMenu != null && settingMenu.IsActive)
            return;

        if (ConsumableView.IsPointerOverConsumableUI())
        {
            if (lastHighlightPos.x >= 0)
            {
                ClearHighlights();
                lastHighlightPos = new Vector2Int(-1, -1);
                currentHoverTilePos = new Vector2Int(-1, -1);
            }
            return;
        }

        Vector2Int gridPos = GetMouseGridPosition();
        if (boardManager == null || !boardManager.IsValidPosition(gridPos))
        {
            if (lastHighlightPos.x >= 0)
            {
                ClearHighlights();
                lastHighlightPos = new Vector2Int(-1, -1);
                currentHoverTilePos = new Vector2Int(-1, -1);
            }
            return;
        }

        if (gridPos == lastHighlightPos)
            return;

        if (lastHighlightPos.x >= 0
            && highlightedTiles.Contains(lastHighlightPos)
            && highlightedTiles.Contains(gridPos))
        {
            lastHighlightPos = gridPos;
            return;
        }

        if (lastHighlightPos.x >= 0 && IsInSameConnectedGroup(lastHighlightPos, gridPos))
        {
            lastHighlightPos = gridPos;
            return;
        }

        lastHighlightPos = gridPos;
        currentHoverTilePos = gridPos;
        UpdateHighlightTiles(gridPos);

        if (Input.GetMouseButton(0) && !ConsumableView.IsPointerOverConsumableUI())
            StartPressColorPulseOnHighlightedTiles();
    }

    public void EnterConsumableSwapMode(string identifier)
    {
        if (!IsInActiveBattle || isProcessing || string.IsNullOrEmpty(identifier))
            return;

        if (ConsumableManager.Instance == null || ConsumableManager.Instance.GetCount(identifier) <= 0)
            return;

        HideAllDetailPanels();
        ConsumableView consumableView = FindObjectOfType<ConsumableView>(true);
        consumableView?.HideAllPanels();

        isConsumableSwapMode = true;
        consumableSwapIdentifier = identifier;
        consumableSwapStartPos = new Vector2Int(-1, -1);
        consumableSwapHoverPos = new Vector2Int(-1, -1);
        isConsumableSwapDragging = false;

        BattleUI battleUI = FindObjectOfType<BattleUI>();
        battleUI?.ShowConsumableSwapHint();
    }

    private void ExitConsumableSwapMode()
    {
        if (!isConsumableSwapMode && string.IsNullOrEmpty(consumableSwapIdentifier))
            return;

        ClearConsumableSwapFrames();

        isConsumableSwapMode = false;
        consumableSwapIdentifier = null;
        consumableSwapStartPos = new Vector2Int(-1, -1);
        consumableSwapHoverPos = new Vector2Int(-1, -1);
        isConsumableSwapDragging = false;

        BattleUI battleUI = FindObjectOfType<BattleUI>();
        battleUI?.HideConsumableSwapHint();

        ConsumableView consumableView = FindObjectOfType<ConsumableView>(true);
        consumableView?.Refresh();
    }

    private void HandleConsumableSwapInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ExitConsumableSwapMode();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                consumableSwapStartPos = gridPos;
                isConsumableSwapDragging = true;
                ClearConsumableSwapFrames();

                TileCell startTile = boardManager.GetTile(gridPos);
                startTile?.ShowFrame(true);
            }
        }
        else if (Input.GetMouseButton(0) && isConsumableSwapDragging)
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                if (gridPos != consumableSwapHoverPos)
                {
                    if (consumableSwapHoverPos.x >= 0 && consumableSwapHoverPos != consumableSwapStartPos)
                    {
                        TileCell prevTile = boardManager.GetTile(consumableSwapHoverPos);
                        prevTile?.ShowFrame(false);
                    }

                    if (gridPos != consumableSwapStartPos && IsAdjacent(consumableSwapStartPos, gridPos))
                    {
                        TileCell newTile = boardManager.GetTile(gridPos);
                        newTile?.ShowFrame(true);
                    }

                    consumableSwapHoverPos = gridPos;
                }
            }
            else
            {
                if (consumableSwapHoverPos.x >= 0 && consumableSwapHoverPos != consumableSwapStartPos)
                {
                    TileCell hoverTile = boardManager.GetTile(consumableSwapHoverPos);
                    hoverTile?.ShowFrame(false);
                }

                consumableSwapHoverPos = new Vector2Int(-1, -1);
            }
        }
        else if (Input.GetMouseButtonUp(0) && isConsumableSwapDragging)
        {
            Vector2Int gridPos = GetMouseGridPosition();
            ClearConsumableSwapFrames();

            bool swapSucceeded = false;
            if (boardManager != null
                && boardManager.IsValidPosition(gridPos)
                && consumableSwapStartPos.x >= 0
                && gridPos != consumableSwapStartPos
                && IsAdjacent(consumableSwapStartPos, gridPos)
                && boardManager.SwapTiles(consumableSwapStartPos, gridPos))
            {
                ConsumableManager.Instance?.RemoveConsumable(consumableSwapIdentifier, 1);
                swapSucceeded = true;

                isProcessing = true;
                currentState = GameState.Processing;
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    isProcessing = false;
                    currentState = GameState.PlayerTurn;
                });
            }

            isConsumableSwapDragging = false;
            consumableSwapStartPos = new Vector2Int(-1, -1);
            consumableSwapHoverPos = new Vector2Int(-1, -1);
            ExitConsumableSwapMode();

            if (!swapSucceeded)
            {
                ConsumableView consumableView = FindObjectOfType<ConsumableView>(true);
                consumableView?.Refresh();
            }
        }
    }

    private void ClearConsumableSwapFrames()
    {
        if (boardManager == null)
            return;

        if (consumableSwapStartPos.x >= 0)
        {
            TileCell startTile = boardManager.GetTile(consumableSwapStartPos);
            startTile?.ShowFrame(false);
        }

        if (consumableSwapHoverPos.x >= 0)
        {
            TileCell hoverTile = boardManager.GetTile(consumableSwapHoverPos);
            hoverTile?.ShowFrame(false);
        }
    }

    private void HandleRightClickToggleDetail()
    {
        Enemy enemy = GetEnemyUnderMouse();
        if (enemy != null && !enemy.IsDead && enemy.EnemyInfo != null)
        {
            if (toggledEnemy == enemy && enemyDescriptionPanel != null && enemyDescriptionPanel.activeSelf)
            {
                HideAllDetailPanels();
                return;
            }

            HideAllDetailPanels();
            toggledEnemy = enemy;
            toggledAlly = null;
            toggledSkillGridPos = new Vector2Int(-1, -1);
            UpdateEnemyDescription(enemy);
            return;
        }

        Ally ally = GetAllyUnderMouse();
        if (ally != null && !ally.IsDead)
        {
            if (toggledAlly == ally && allyDescriptionPanel != null && allyDescriptionPanel.activeSelf)
            {
                HideAllDetailPanels();
                return;
            }

            HideAllDetailPanels();
            toggledAlly = ally;
            toggledEnemy = null;
            toggledSkillGridPos = new Vector2Int(-1, -1);
            UpdateAllyDescription(ally);
            return;
        }

        Vector2Int gridPos = GetMouseGridPosition();
        if (boardManager != null && boardManager.IsValidPosition(gridPos))
        {
            if (toggledSkillGridPos == gridPos && skillDisplayPanel != null && skillDisplayPanel.activeSelf)
            {
                HideAllDetailPanels();
                return;
            }

            HideAllDetailPanels();
            toggledSkillGridPos = gridPos;
            toggledEnemy = null;
            toggledAlly = null;
            UpdateSkillDisplay(gridPos);
            return;
        }

        HideAllDetailPanels();
    }

    private void HideAllDetailPanels()
    {
        toggledEnemy = null;
        toggledAlly = null;
        toggledSkillGridPos = new Vector2Int(-1, -1);

        if (skillDisplayPanel != null)
            skillDisplayPanel.SetActive(false);
        if (enemyDescriptionPanel != null)
            enemyDescriptionPanel.SetActive(false);
        if (allyDescriptionPanel != null)
            allyDescriptionPanel.SetActive(false);

        ConsumableView consumableView = FindObjectOfType<ConsumableView>(true);
        consumableView?.HideAllPanels();

        ClearHighlights();
        lastHighlightPos = new Vector2Int(-1, -1);
    }

    /// <summary>
    /// 处理触屏输入
    /// </summary>
    private void HandleTouchInput()
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>();
        if (skillMenu != null && skillMenu.IsActive)
            return;

        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu != null && eventMenu.IsActive)
            return;

        SettingMenu settingMenu = FindObjectOfType<SettingMenu>();
        if (settingMenu != null && settingMenu.IsActive)
            return;

        if (isProcessing || isConsumableSwapMode)
            return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
            return;

        Vector3 touchScreenPos = touch.position;
        if (mainCamera.orthographic)
            touchScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        else
            touchScreenPos.z = mainCamera.nearClipPlane;

        Vector3 touchWorldPos = mainCamera.ScreenToWorldPoint(touchScreenPos);
        touchWorldPos.z = 0;
        Vector2Int gridPos = GetGridPositionFromWorld(touchWorldPos);

        HideAllDetailPanels();

        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsRightClickEnabled)
            return;

        if (boardManager != null && boardManager.IsValidPosition(gridPos))
        {
            TryPlayPlayerAttackAnimation();
            EliminateConnectedTiles(gridPos);
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
    /// 检查位置是否在随从的sprite范围内
    /// </summary>
    private bool IsPositionInAllyBounds(Ally ally, Vector3 worldPos)
    {
        if (ally == null)
            return false;
            
        SpriteRenderer sr = ally.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = ally.GetComponentInChildren<SpriteRenderer>();
            
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
    /// 从世界坐标获取随从
    /// </summary>
    private Ally GetAllyAtPosition(Vector3 worldPos)
    {
        if (allyManager == null)
            return null;
            
        foreach (var ally in allyManager.ActiveAllies)
        {
            if (ally == null || ally.IsDead)
                continue;
                
            if (IsPositionInAllyBounds(ally, worldPos))
            {
                return ally;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取鼠标下的随从（基于spriteRenderer的实际sprite区域）
    /// </summary>
    private Ally GetAllyUnderMouse()
    {
        if (mainCamera == null || allyManager == null)
            return null;
            
        // 将鼠标屏幕坐标转换为世界坐标
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.nearClipPlane;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        
        // 使用与GetAllyAtPosition相同的逻辑
        return GetAllyAtPosition(mouseWorldPos);
    }
    
    /// <summary>
    /// 更新随从描述显示
    /// </summary>
    private void UpdateAllyDescription(Ally ally)
    {
        if (ally == null || allyDescriptionPanel == null || allyDescriptionText == null)
        {
            if (allyDescriptionPanel != null)
            {
                allyDescriptionPanel.SetActive(false);
            }
            return;
        }
        
        // 显示固定的描述文本
        allyDescriptionText.text = "This is the Speaker you summoned. It will help you fend off the enemy's attacks.";
        allyDescriptionPanel.SetActive(true);
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
        //sb.AppendLine();
        
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
        //sb.AppendLine();
        
        // // Buff/Debuff信息
        // int vulnerableStacks = enemy.GetVulnerableStacks();
        // if (vulnerableStacks > 0)
        // {
        //     float damageIncrease = vulnerableStacks * 0.05f * 100f;
        //     sb.AppendLine($"<color=yellow>Vulnerable: {vulnerableStacks}层</color>");
        //     sb.AppendLine($"伤害提升: +{damageIncrease:F0}%");
        //     //sb.AppendLine();
        // }
        
        // // 显示敌人伤害加成（如果有）
        // if (PlayerManager.Instance != null && PlayerManager.Instance.EnemyDamageBonus > 0)
        // {
        //     sb.AppendLine($"<color=red>Enemy Damage Bonus: +{PlayerManager.Instance.EnemyDamageBonus:F0}%</color>");
        //     //sb.AppendLine();
        // }
        
        // 描述
        if (!string.IsNullOrEmpty(enemy.EnemyInfo.description))
        {
            sb.AppendLine(enemy.EnemyInfo.description);
        }
        
        enemyDescriptionText.text = sb.ToString().Replace("\\n", "\n");
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
                int tilesUsed = boardManager.GetConnectedSameColorTiles(gridPos).Count;
                string skillText = SkillManager.Instance.BuildColorAreaSkillDescriptions(
                    skillIdentifiers, false, tilesUsed, true, colorIndex);

                if (!string.IsNullOrEmpty(skillText))
                {
                    skillDisplayText.text = skillText;
                    SetSkillPanelColorByIndex(skillDisplayPanel, colorIndex);
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
    /// 根据颜色索引设置panel背景色（用于颜色区域悬停）
    /// </summary>
    private void SetSkillPanelColorByIndex(GameObject panel, int colorIndex)
    {
        if (panel == null)
            return;

        Image bgImage = panel.GetComponent<Image>();
        if (bgImage == null)
            return;

        if (colorIndex >= 0 && colorIndex < 4)
        {
            TileColor tileColor = (TileColor)colorIndex;
            bgImage.color = TileColorUtil.GetBattleNoteColor(tileColor);
        }
        else
        {
            bgImage.color = Color.white;
        }
    }

    /// <summary>
    /// 根据技能颜色设置panel背景色
    /// </summary>
    private void SetSkillPanelColor(GameObject panel, string skillIdentifier)
    {
        if (panel == null || string.IsNullOrEmpty(skillIdentifier))
            return;
        
        // 获取panel的Image组件（背景）
        Image bgImage = panel.GetComponent<Image>();
        if (bgImage == null)
            return;
        
        // 获取技能信息
        if (CSVLoader.Instance == null || !CSVLoader.Instance.cardInfoMap.ContainsKey(skillIdentifier))
        {
            // 如果没有找到技能信息，使用默认颜色（白色）
            bgImage.color = Color.white;
            return;
        }
        
        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[skillIdentifier];
        
        // 如果技能有颜色，使用对应颜色；否则使用默认颜色（白色）
        if (!string.IsNullOrEmpty(skillInfo.color))
        {
            // 将颜色字符串转换为TileColor
            string colorLower = skillInfo.color.ToLower();
            if (colorLower == "red" || colorLower == "yellow" || colorLower == "blue" || colorLower == "green")
            {
                TileColor tileColor = GetTileColorFromString(skillInfo.color);
                Color colorValue = TileColorUtil.GetBattleNoteColor(tileColor);
                bgImage.color = colorValue;
            }
            else
            {
                // 无效颜色，使用默认颜色（白色）
                bgImage.color = Color.white;
            }
        }
        else
        {
            // 没有颜色，使用默认颜色（白色）
            bgImage.color = Color.white;
        }
    }
    
    /// <summary>
    /// 将颜色字符串转换为TileColor
    /// </summary>
    private TileColor GetTileColorFromString(string colorStr)
    {
        switch (colorStr.ToLower())
        {
            case "red": return TileColor.Red;
            case "yellow": return TileColor.Yellow;
            case "blue": return TileColor.Blue;
            case "green": return TileColor.Green;
            default: return TileColor.Red;
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

        if (boardManager == null || !boardManager.IsBoardInitialized)
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(mousePos);

        // 高亮所有连通的格子
        foreach (var pos in connectedTiles)
        {
            TileCell connectedTile = boardManager.GetTile(pos);
            if (connectedTile != null)
            {
                connectedTile.SetHighlight(true);
                //connectedTile.SetHighlightColor(Color.cyan); // 使用青色高亮
                highlightedTiles.Add(pos);
            }
        }

        highlightSourcePos = mousePos;
        ApplyAllySkillHoverPreview(connectedTiles);
    }

    /// <summary>
    /// hover 预览 allyChangeColorAndUse / allyTileUse 会额外作用的召唤物脚下格。
    /// </summary>
    private void ApplyAllySkillHoverPreview(List<Vector2Int> connectedTiles)
    {
        if (connectedTiles == null || connectedTiles.Count == 0 || allyManager == null)
            return;

        TileCell hoverTile = boardManager.GetTile(highlightSourcePos.x >= 0 ? highlightSourcePos : connectedTiles[0]);
        if (hoverTile == null)
            return;

        TileColor waveColor = hoverTile.Color;
        bool hasAllyChangeColorAndUse = HasEquippedColorSkill(waveColor, "allyChangeColorAndUse", out _);
        bool hasAllyTileUse = HasEquippedColorSkill(waveColor, "allyTileUse", out _);
        if (!hasAllyChangeColorAndUse && !hasAllyTileUse)
            return;

        foreach (Ally ally in allyManager.GetLivingAllies())
        {
            Vector2Int allyPos = ally.GridPosition;
            // 已在原本连通组内的格子不会额外生成，无需预览
            if (connectedTiles.Contains(allyPos))
                continue;

            TileCell allyTile = boardManager.GetTile(allyPos);
            if (allyTile == null || allyTile.IsDirty || allyTile.IsDisabled)
                continue;

            if (hasAllyChangeColorAndUse)
                allyTile.SetPreviewColor(waveColor);

            allyTile.SetHighlight(true);
            if (!highlightedTiles.Contains(allyPos))
                highlightedTiles.Add(allyPos);
            allySkillPreviewTiles.Add(allyPos);
        }
    }

    private Vector2Int ResolveHighlightSourcePos(Vector2Int gridPos)
    {
        if (allySkillPreviewTiles.Contains(gridPos) && highlightSourcePos.x >= 0)
            return highlightSourcePos;
        return gridPos;
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
                if (!tile.IsBeingDestroyed)
                    tile.ClearPreviewColor();
            }
        }
        highlightedTiles.Clear();
        allySkillPreviewTiles.Clear();
        highlightSourcePos = new Vector2Int(-1, -1);
    }

    private bool IsInSameConnectedGroup(Vector2Int posA, Vector2Int posB)
    {
        if (boardManager == null || !boardManager.IsValidPosition(posA) || !boardManager.IsValidPosition(posB))
            return false;

        TileCell tileA = boardManager.GetTile(posA);
        TileCell tileB = boardManager.GetTile(posB);
        if (tileA == null || tileB == null || tileA.Color != tileB.Color)
            return false;

        return boardManager.GetConnectedSameColorTiles(posA).Contains(posB);
    }

    private void StartPressColorPulseOnTiles(List<Vector2Int> tiles)
    {
        if (boardManager == null)
            return;

        foreach (var pos in tiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            tile?.StartPressColorPulse();
        }
    }

    private void StartPressColorPulseOnHighlightedTiles()
    {
        StartPressColorPulseOnTiles(highlightedTiles);
    }

    private void StopPressColorPulseOnHighlightedTiles()
    {
        if (boardManager == null)
            return;

        foreach (var pos in highlightedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            tile?.StopPressColorPulse();
        }
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
    private void EliminateConnectedTiles(Vector2Int startPos, bool isManualWave = true)
    {
        if (boardManager == null || (isManualWave && isProcessing))
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(startPos);

        if (connectedTiles.Count == 0) // 如果没有连通格子，不执行消除
            return;

        TileCell startTile = boardManager.GetTile(startPos);
        TileColor waveColor = startTile != null ? startTile.Color : TileColor.Red;
        List<Vector2Int> originalConnectedTiles = new List<Vector2Int>(connectedTiles);
        bool hasAllyChangeColorAndUse = HasEquippedColorSkill(waveColor, "allyChangeColorAndUse", out _);
        bool hasAllyTileUse = HasEquippedColorSkill(waveColor, "allyTileUse", out _);
        Dictionary<Vector2Int, TileColor> delayedAllyTileWaves = new Dictionary<Vector2Int, TileColor>();

        if ((hasAllyChangeColorAndUse || hasAllyTileUse) && allyManager != null)
        {
            foreach (Ally ally in allyManager.GetLivingAllies())
            {
                Vector2Int allyPos = ally.GridPosition;
                TileCell allyTile = boardManager.GetTile(allyPos);
                if (allyTile == null || allyTile.IsDirty || allyTile.IsDisabled)
                    continue;

                bool alreadyInOriginal = originalConnectedTiles.Contains(allyPos);
                TileColor independentWaveColor = allyTile.Color;
                if (hasAllyChangeColorAndUse)
                {
                    allyTile.SetColor(waveColor);
                    independentWaveColor = waveColor;
                    if (!connectedTiles.Contains(allyPos))
                        connectedTiles.Add(allyPos);
                }

                // 已在原本连通组内的格子不再额外生成
                if (hasAllyTileUse && !alreadyInOriginal)
                    delayedAllyTileWaves[allyPos] = independentWaveColor;
            }
        }

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
        earlyTurnEndScheduled = false;
        currentState = GameState.Processing;

        int colorIndex = (int)waveColor; // TileColor枚举值：Red=0, Yellow=1, Blue=2, Green=3
        bool recreateSameColorAfterGravity = isManualWave
            && HasEquippedColorSkill(waveColor, "recreateSameColor", out string recreateSameIdentifier)
            && connectedTiles.Count > SkillManager.Instance.GetSkillValue(recreateSameIdentifier);
        bool recreateDifferentColorAfterGravity = isManualWave
            && hasLastManualWaveColor
            && lastManualWaveColor == waveColor
            && HasEquippedColorSkill(waveColor, "recreateDifferentColor", out _);

        if (isManualWave)
        {
            lastManualWaveColor = waveColor;
            hasLastManualWaveColor = true;
        }

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
            waveGroupPassedSameColorTiles[currentWaveGroupId] = new HashSet<Vector2Int>();
            waveGroupEnemyHitCounts[currentWaveGroupId] = new Dictionary<Enemy, int>();
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

        int groupId = currentWaveGroupId;
        int tilesUsed = connectedTiles.Count;
        Vector2Int firstTilePos = connectedTiles.Count > 0 ? connectedTiles[0] : Vector2Int.zero;

        // 召唤物前移/相邻攻击在出波前完成；结束后再创建波浪
        ApplyAllyMoveForwardDamage(groupId, waveColor, () =>
        {
            CreateWavesAfterAllyAction(
                connectedTiles,
                delayedAllyTileWaves,
                waveColor,
                groupId,
                hasDamageBottom,
                currentWaveDamageMultiplier,
                hasPure,
                pureValue,
                tilesUsed,
                waveCountMultiplier,
                firstTilePos,
                isManualWave,
                recreateSameColorAfterGravity,
                recreateDifferentColorAfterGravity);
        });
    }

    private void CreateWavesAfterAllyAction(
        List<Vector2Int> connectedTiles,
        Dictionary<Vector2Int, TileColor> delayedAllyTileWaves,
        TileColor waveColor,
        int waveGroupId,
        bool hasDamageBottom,
        float currentWaveDamageMultiplier,
        bool hasPure,
        int pureValue,
        int tilesUsed,
        int waveCountMultiplier,
        Vector2Int firstTilePos,
        bool isManualWave,
        bool recreateSameColorAfterGravity,
        bool recreateDifferentColorAfterGravity)
    {
        if (boardManager == null)
            return;

        waveGroupActiveWaveCount[waveGroupId] = connectedTiles.Count * waveCountMultiplier;
        SetAllyDamageBonusForWaveGroup(
            waveGroupId,
            waveColor,
            waveGroupActiveWaveCount[waveGroupId]);

        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RecordTilesGenerated(waveColor, connectedTiles.Count);
            StatisticsManager.Instance.RecordWaveGroupSize(waveColor, connectedTiles.Count);
            StatisticsManager.Instance.RecordWaveGenerated(waveColor);
        }

        int waveIndex = 0;
        foreach (var pos in connectedTiles)
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(pos);
            bool isFirstWave = (waveIndex == 0);
            CreateWave(worldPos, waveColor, pos, waveGroupId, isFirstWave, hasDamageBottom, currentWaveDamageMultiplier, hasPure, pureValue, tilesUsed, false, isManualWave);

            if (waveCountMultiplier > 1)
                CreateWave(worldPos, waveColor, pos, waveGroupId, isFirstWave, false, currentWaveDamageMultiplier, hasPure, pureValue, tilesUsed, true, isManualWave);

            boardManager.RemoveTile(pos);
            waveIndex++;
        }

        foreach (var delayedWave in delayedAllyTileWaves)
        {
            if (!connectedTiles.Contains(delayedWave.Key))
                boardManager.RemoveTile(delayedWave.Key);
        }
        ScheduleIndependentAllyTileWaves(delayedAllyTileWaves);

        ApplyShieldExplosionForWaveGroup(waveColor, firstTilePos);
        ApplyHealWhenSpawnForWaveGroup(waveColor, tilesUsed, firstTilePos);
        ApplySoloHealForWaveGroup(waveColor, tilesUsed, firstTilePos);
        ApplyShieldWhenSpawnForWaveGroup(waveColor, tilesUsed, firstTilePos);
        ApplySoloShieldForWaveGroup(waveColor, tilesUsed, firstTilePos);
        ApplyAllyShieldForWaveGroup(waveColor, firstTilePos);
        ApplyKillAllWithAlly(waveGroupId, waveColor);

        bool isPuzzleMode = currentLevelInfo != null && currentLevelInfo.type != null && currentLevelInfo.type.ToLower() == "puzzle";
        DOVirtual.DelayedCall(0.3f, () =>
        {
            boardManager.ApplyGravity(!isPuzzleMode);

            if (recreateSameColorAfterGravity)
                CreateWaveFromLargestTileGroup(waveColor, null);
            if (recreateDifferentColorAfterGravity)
                CreateWaveFromLargestTileGroup(null, waveColor);
        });
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
    private void CreateWave(Vector3 spawnPosition, TileColor color, Vector2Int gridPos, int waveGroupId, bool isFirstWave, bool hasDamageBottomSkill, float damageMultiplier = 1f, bool hasPure = false, int pureValue = 0, int tilesUsed = 1, bool backward = false, bool allowRecreateOnKill = true)
    {
        if (wavePrefab == null)
            return;

        GameObject waveObj = Instantiate(wavePrefab, spawnPosition, Quaternion.identity, waveParent);
        Wave wave = waveObj.GetComponent<Wave>();
        if (wave == null)
        {
            wave = waveObj.AddComponent<Wave>();
        }

        wave.Init(spawnPosition, color, 10f, gridPos, waveGroupId, isFirstWave, hasDamageBottomSkill, damageMultiplier, hasPure, pureValue, tilesUsed, backward, allowRecreateOnKill);
    }

    private bool HasEquippedColorSkill(TileColor color, string effect, out string identifier)
    {
        identifier = null;
        if (PlayerManager.Instance == null || SkillManager.Instance == null || CSVLoader.Instance == null)
            return false;

        foreach (var skillIdentifier in PlayerManager.Instance.GetWaveSkills((int)color))
        {
            if (!SkillManager.Instance.HasSkill(skillIdentifier))
                continue;
            if (!CSVLoader.Instance.cardInfoMap.TryGetValue(skillIdentifier, out SkillInfo skillInfo))
                continue;
            if (skillInfo != null && skillInfo.effect == effect)
            {
                identifier = skillIdentifier;
                return true;
            }
        }

        return false;
    }

    private void ScheduleIndependentAllyTileWaves(Dictionary<Vector2Int, TileColor> delayedWaves)
    {
        if (delayedWaves == null || delayedWaves.Count == 0)
            return;

        List<KeyValuePair<Vector2Int, TileColor>> waveSnapshot =
            new List<KeyValuePair<Vector2Int, TileColor>>(delayedWaves);
        BeginDelayedSkillEffect();
        bool finished = false;
        void FinishDelayedAllyTileWaves()
        {
            if (finished)
                return;
            finished = true;
            EndDelayedSkillEffect();
        }

        Tween delayTween = DOVirtual.DelayedCall(0.5f, () =>
        {
            try
            {
                foreach (var delayedWave in waveSnapshot)
                    CreateIndependentAllyTileWave(delayedWave.Key, delayedWave.Value);
            }
            finally
            {
                FinishDelayedAllyTileWaves();
            }
        });
        delayTween.OnKill(FinishDelayedAllyTileWaves);
    }

    private void CreateIndependentAllyTileWave(Vector2Int gridPos, TileColor color)
    {
        if (boardManager == null || wavePrefab == null)
            return;

        currentWaveGroupId++;
        int groupId = currentWaveGroupId;
        waveGroupTotalDamage[groupId] = 0f;
        waveGroupActiveWaveCount[groupId] = 1;
        waveGroupColor[groupId] = color;
        waveGroupAddDamageWhenPass[groupId] = 0f;
        waveGroupHasDamage[groupId] = false;
        waveGroupPassedSameColorTiles[groupId] = new HashSet<Vector2Int>();
        waveGroupEnemyHitCounts[groupId] = new Dictionary<Enemy, int>();
        pendingWaveGroups.Add(groupId);
        SetAllyDamageBonusForWaveGroup(groupId, color, 1);

        try
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(gridPos);
            CreateWave(worldPos, color, gridPos, groupId, true, false, 1f, false, 0, 1, false, false);
        }
        catch
        {
            OnWaveDestroyed(groupId);
            throw;
        }
    }

    private void SetAllyDamageBonusForWaveGroup(int groupId, TileColor color, int waveCount)
    {
        waveGroupAllyDamageBonusPerWave[groupId] = 0f;
        if (waveCount <= 0
            || allyManager == null
            || !HasEquippedColorSkill(color, "allyIncreaseDamage", out string identifier))
            return;

        int value = SkillManager.Instance.GetSkillValue(identifier);
        float groupBonus = allyManager.GetTotalCurrentHealth() * value / 100f;
        waveGroupAllyDamageBonusPerWave[groupId] = groupBonus / waveCount;
    }

    private void ApplyAllyShieldForWaveGroup(TileColor color, Vector2Int displayPos)
    {
        if (PlayerManager.Instance == null
            || allyManager == null
            || !HasEquippedColorSkill(color, "allyIncreaseShield", out string identifier))
            return;

        int value = SkillManager.Instance.GetSkillValue(identifier);
        int shield = Mathf.FloorToInt(allyManager.GetTotalCurrentHealth() * value / 100f);
        if (shield <= 0)
            return;

        PlayerManager.Instance.AddShield(shield);
        DamageNumber.CreateDamageNumber(shield, boardManager.GridToWorldPosition(displayPos), true);
    }

    private void CreateWaveFromLargestTileGroup(TileColor? requiredColor, TileColor? excludedColor)
    {
        if (boardManager == null)
            return;

        List<Vector2Int> group = boardManager.GetLargestConnectedSameColorGroup(requiredColor, excludedColor);
        if (group.Count > 0)
            EliminateConnectedTiles(group[0], false);
    }

    public static void TryRecreateSameColorOnDirectKill(TileColor color)
    {
        MainGameManager instance = FindObjectOfType<MainGameManager>();
        if (instance == null || !instance.HasEquippedColorSkill(color, "recreateSameColorOnKill", out _))
            return;

        instance.CreateWaveFromLargestTileGroup(color, null);
    }

    public static bool WasLastManualWaveColor(int colorIndex)
    {
        return hasLastManualWaveColor && (int)lastManualWaveColor == colorIndex;
    }

    /// <summary>
    /// 应用shieldExplosion技能（生成波时对全体敌人造成护盾×value%伤害并清空护盾）
    /// </summary>
    private void ApplyShieldExplosionForWaveGroup(TileColor waveColor, Vector2Int firstTilePos)
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null || enemyManager == null)
            return;

        int colorIndex = (int)waveColor;
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

        bool hasShieldExplosion = false;
        int shieldExplosionValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "shieldExplosion")
                {
                    hasShieldExplosion = true;
                    shieldExplosionValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }

        if (!hasShieldExplosion)
            return;

        int shield = PlayerManager.Instance.CurrentShield;
        if (shield <= 0)
            return;

        int damagePerEnemy = (int)(shield * shieldExplosionValue / 100f);
        if (damagePerEnemy <= 0)
            return;

        PlayerManager.Instance.ClearShield();

        float totalDamage = 0f;
        Vector3 damageNumberPos = boardManager != null
            ? boardManager.GridToWorldPosition(firstTilePos)
            : Vector3.zero;

        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            enemy.TakeDamage(damagePerEnemy, Vector3.right, false, 0, damagePerEnemy);
            totalDamage += damagePerEnemy;
        }

        if (currentBoss != null && !currentBoss.IsDead)
        {
            currentBoss.TakeDamage(damagePerEnemy, Vector3.right, false, 0, damagePerEnemy);
            totalDamage += damagePerEnemy;
        }

        if (totalDamage > 0)
        {
            RecordWaveDamage(currentWaveGroupId, totalDamage);
            DamageNumber.CreateDamageNumber(damagePerEnemy, damageNumberPos, false);
        }
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
    /// 应用soloHeal技能（如果生成来自只有一个tile，恢复失去血量的value%）
    /// </summary>
    private void ApplySoloHealForWaveGroup(TileColor waveColor, int tilesUsed, Vector2Int firstTilePos)
    {
        // 只有单个tile时才触发
        if (tilesUsed != 1)
            return;
            
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;
        
        int colorIndex = (int)waveColor;
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
        
        // 检查是否有soloHeal技能
        bool hasSoloHeal = false;
        int soloHealValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "soloHeal")
                {
                    hasSoloHeal = true;
                    soloHealValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }
        
        if (!hasSoloHeal)
            return;
        
        // 计算已损失血量
        int maxHealth = PlayerManager.Instance.MaxHealth;
        int currentHealth = PlayerManager.Instance.CurrentHealth;
        int lostHealth = maxHealth - currentHealth;
        
        if (lostHealth <= 0)
            return; // 没有损失血量，不需要恢复
        
        // 恢复 value% 的已损失血量
        int totalHeal = (int)(lostHealth * soloHealValue / 100f);
        
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
    /// 应用shieldWhenSpawn技能（整个wave group只获得一次护盾）
    /// </summary>
    private void ApplyShieldWhenSpawnForWaveGroup(TileColor waveColor, int tilesUsed, Vector2Int firstTilePos)
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;

        int colorIndex = (int)waveColor;
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

        bool hasShieldWhenSpawn = false;
        int shieldWhenSpawnValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "shieldWhenSpawn")
                {
                    hasShieldWhenSpawn = true;
                    shieldWhenSpawnValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }

        if (!hasShieldWhenSpawn)
            return;

        int maxHealth = PlayerManager.Instance.MaxHealth;
        int totalShield = (int)(maxHealth * shieldWhenSpawnValue / 100f * tilesUsed);
        if (totalShield > 0)
        {
            PlayerManager.Instance.AddShield(totalShield);
            Vector3 firstWavePos = boardManager.GridToWorldPosition(firstTilePos);
            DamageNumber.CreateDamageNumber(totalShield, firstWavePos, true);
        }
    }

    /// <summary>
    /// 应用soloShield技能（单格波获得最大生命值 value% 的护盾）
    /// </summary>
    private void ApplySoloShieldForWaveGroup(TileColor waveColor, int tilesUsed, Vector2Int firstTilePos)
    {
        if (tilesUsed != 1)
            return;

        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;

        int colorIndex = (int)waveColor;
        List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

        bool hasSoloShield = false;
        int soloShieldValue = 0;
        foreach (var identifier in skillIdentifiers)
        {
            if (SkillManager.Instance.HasSkill(identifier))
            {
                SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                if (skillInfo != null && skillInfo.effect == "soloShield")
                {
                    hasSoloShield = true;
                    soloShieldValue = SkillManager.Instance.GetSkillValue(identifier);
                    break;
                }
            }
        }

        if (!hasSoloShield)
            return;

        int maxHealth = PlayerManager.Instance.MaxHealth;
        int totalShield = (int)(maxHealth * soloShieldValue / 100f);
        if (totalShield > 0)
        {
            PlayerManager.Instance.AddShield(totalShield);
            Vector3 firstWavePos = boardManager.GridToWorldPosition(firstTilePos);
            DamageNumber.CreateDamageNumber(totalShield, firstWavePos, true);
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
    /// 登记 wave group 首次经过的格子（同色经过类技能）。返回 true 表示本格首次经过，可触发效果。
    /// </summary>
    public static bool TryRegisterPassTileForWaveGroup(int waveGroupId, Vector2Int gridPos)
    {
        if (!waveGroupPassedSameColorTiles.ContainsKey(waveGroupId))
        {
            waveGroupPassedSameColorTiles[waveGroupId] = new HashSet<Vector2Int>();
        }
        return waveGroupPassedSameColorTiles[waveGroupId].Add(gridPos);
    }

    /// <summary>
    /// 累加addDamageWhenPass（每经过一个同色tile增加 value%）
    /// </summary>
    public static void AddAddDamageWhenPass(int waveGroupId, float value)
    {
        if (!waveGroupAddDamageWhenPass.ContainsKey(waveGroupId))
        {
            waveGroupAddDamageWhenPass[waveGroupId] = 0f;
        }
        waveGroupAddDamageWhenPass[waveGroupId] += value;
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
    /// 登记同一 wave group 内对同一敌人的命中次数（含本组所有波浪）。
    /// </summary>
    public static int RegisterEnemyHitForWaveGroup(int waveGroupId, Enemy enemy)
    {
        if (enemy == null)
            return 0;

        if (!waveGroupEnemyHitCounts.ContainsKey(waveGroupId))
            waveGroupEnemyHitCounts[waveGroupId] = new Dictionary<Enemy, int>();

        Dictionary<Enemy, int> hitCounts = waveGroupEnemyHitCounts[waveGroupId];
        if (!hitCounts.TryGetValue(enemy, out int hitCount))
            hitCount = 0;

        hitCount++;
        hitCounts[enemy] = hitCount;
        return hitCount;
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
        if (!waveGroupActiveWaveCount.TryGetValue(waveGroupId, out int remaining))
            return;

        remaining--;
        waveGroupActiveWaveCount[waveGroupId] = remaining;
        if (remaining != 0)
            return;

        if (waveGroupAddDamageWhenPass.ContainsKey(waveGroupId))
            waveGroupAddDamageWhenPass.Remove(waveGroupId);
        if (waveGroupPassedSameColorTiles.ContainsKey(waveGroupId))
            waveGroupPassedSameColorTiles.Remove(waveGroupId);
        if (waveGroupEnemyHitCounts.ContainsKey(waveGroupId))
            waveGroupEnemyHitCounts.Remove(waveGroupId);
        waveGroupAllyDamageBonusPerWave.Remove(waveGroupId);

        if (waveGroupTotalDamage.ContainsKey(waveGroupId) && waveGroupColor.ContainsKey(waveGroupId))
        {
            float totalDamage = waveGroupTotalDamage[waveGroupId];
            TileColor waveColor = waveGroupColor[waveGroupId];
            if (StatisticsManager.Instance != null && totalDamage > 0)
                StatisticsManager.Instance.RecordWaveGroupDamage(waveColor, totalDamage);
        }

        MainGameManager instance = Instance;
        if (instance != null)
        {
            instance.CheckSpawnAlly(waveGroupId);
            return;
        }

        pendingWaveGroups.Remove(waveGroupId);
        waveGroupTotalDamage.Remove(waveGroupId);
        waveGroupActiveWaveCount.Remove(waveGroupId);
        waveGroupColor.Remove(waveGroupId);
        waveGroupHasDamage.Remove(waveGroupId);
    }

    /// <summary>
    /// 检查并生成随从（spawnAlly技能）
    /// </summary>
    private void CheckSpawnAlly(int waveGroupId)
    {
        bool settlementHandled = false;
        try
        {
            if (!waveGroupTotalDamage.TryGetValue(waveGroupId, out float totalDamage)
                || !waveGroupColor.TryGetValue(waveGroupId, out TileColor waveColor)
                || PlayerManager.Instance == null
                || SkillManager.Instance == null)
                return;

            int colorIndex = (int)waveColor;
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);

            foreach (var identifier in skillIdentifiers)
            {
                if (!SkillManager.Instance.HasSkill(identifier)
                    || CSVLoader.Instance == null
                    || !CSVLoader.Instance.cardInfoMap.TryGetValue(identifier, out SkillInfo skillInfo)
                    || skillInfo == null
                    || skillInfo.effect != "spawnAlly")
                    continue;

                int value = SkillManager.Instance.GetSkillValue(identifier);
                int allyHealth = (int)(totalDamage * (value / 100f));
                SpawnAlly(allyHealth);
                break;
            }

            ApplyAllyHealthIncrease(waveColor, totalDamage);
            CheckSummonAttack(waveGroupId);

            bool skipEnemyTurn = waveGroupHasDamage.ContainsKey(waveGroupId)
                && !waveGroupHasDamage[waveGroupId]
                && !noAttackNoCostTriggeredThisTurn
                && HasNoAttackNoCostSkill();
            if (skipEnemyTurn)
                noAttackNoCostTriggeredThisTurn = true;

            FinishWaveGroupSettlement(waveGroupId, skipEnemyTurn);
            settlementHandled = true;
        }
        finally
        {
            if (!settlementHandled)
                FinishWaveGroupSettlement(waveGroupId, false);
        }
    }

    private void FinishWaveGroupSettlement(int waveGroupId, bool skipEnemyTurn)
    {
        bool wasPending = pendingWaveGroups.Remove(waveGroupId);
        waveGroupTotalDamage.Remove(waveGroupId);
        waveGroupActiveWaveCount.Remove(waveGroupId);
        waveGroupColor.Remove(waveGroupId);
        waveGroupHasDamage.Remove(waveGroupId);
        waveGroupAllyDamageBonusPerWave.Remove(waveGroupId);

        if (!wasPending)
            return;

        if (skipEnemyTurn)
        {
            StartCoroutine(SkipEnemyTurnAfterDelay());
            return;
        }

        CheckAllWaveGroupsCompleted();
    }

    private IEnumerator SkipEnemyTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (pendingWaveGroups.Count > 0 || pendingDelayedSkillEffects > 0)
        {
            CheckAllWaveGroupsCompleted();
            yield break;
        }

        isProcessing = false;
        earlyTurnEndScheduled = false;
        currentState = GameState.PlayerTurn;
        Debug.Log("noAttackNoCost: 没有造成伤害，继续玩家回合");
    }

    private bool HasNoAttackNoCostSkill()
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null || CSVLoader.Instance == null)
            return false;

        for (int i = 0; i < 4; i++)
        {
            foreach (var identifier in PlayerManager.Instance.GetWaveSkills(i))
            {
                if (!SkillManager.Instance.HasSkill(identifier)
                    || !CSVLoader.Instance.cardInfoMap.TryGetValue(identifier, out SkillInfo skillInfo)
                    || skillInfo == null
                    || skillInfo.effect != "noAttackNoCost")
                    continue;
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// 所有 note 已离开棋盘且不会再击中 boss 时，提前切换回合逻辑（note 继续飞行）
    /// </summary>
    private void TryEarlyEndPlayerTurnFromWaves()
    {
        if (earlyTurnEndScheduled)
            return;
        if (!isProcessing || currentState != GameState.Processing)
            return;
        if (pendingWaveGroups.Count == 0)
            return;
        if (pendingDelayedSkillEffects > 0)
            return;
        if (WouldNoAttackNoCostBlockEarlyTurnEnd())
            return;
        if (!AreAllActiveWavesReadyForTurnEnd())
            return;

        earlyTurnEndScheduled = true;
        if (pendingEarlyTurnEndRoutine != null)
            StopCoroutine(pendingEarlyTurnEndRoutine);
        pendingEarlyTurnEndRoutine = StartCoroutine(EarlyEndPlayerTurnAfterDelay());
    }

    private IEnumerator EarlyEndPlayerTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        pendingEarlyTurnEndRoutine = null;

        if (currentState != GameState.Processing)
            yield break;

        if (pendingDelayedSkillEffects > 0)
        {
            earlyTurnEndScheduled = false;
            CheckAllWaveGroupsCompleted();
            yield break;
        }

        if (currentLevelInfo != null && currentLevelInfo.type != null && currentLevelInfo.type.ToLower() == "puzzle")
        {
            if (CheckAllTilesCleared())
            {
                CompleteLevel();
                yield break;
            }
        }

        EndPlayerTurn();
    }

    private bool AreAllActiveWavesReadyForTurnEnd()
    {
        if (waveParent == null)
            return false;

        bool hasActiveWave = false;
        for (int i = 0; i < waveParent.childCount; i++)
        {
            Wave wave = waveParent.GetChild(i).GetComponent<Wave>();
            if (wave == null || !wave.IsMoving)
                continue;

            hasActiveWave = true;
            if (!wave.HasClearedBoard())
                return false;
            if (wave.CanStillHitBoss())
                return false;
        }

        return hasActiveWave;
    }

    private bool WouldNoAttackNoCostBlockEarlyTurnEnd()
    {
        if (noAttackNoCostTriggeredThisTurn)
            return false;
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return false;

        bool hasNoAttackNoCost = false;
        for (int i = 0; i < 4; i++)
        {
            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(i);
            foreach (var identifier in skillIdentifiers)
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
            if (hasNoAttackNoCost)
                break;
        }

        if (!hasNoAttackNoCost)
            return false;

        foreach (int groupId in pendingWaveGroups)
        {
            bool hasDamage = waveGroupHasDamage.ContainsKey(groupId) && waveGroupHasDamage[groupId];
            if (!hasDamage)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查所有wave group是否都完成了，如果是则进入敌人回合
    /// </summary>
    private void CheckAllWaveGroupsCompleted()
    {
        if (pendingWaveGroups.Count > 0 || pendingDelayedSkillEffects > 0)
            return;

        noAttackNoCostTriggeredThisTurn = false;

        if (pendingCompleteTurnRoutine != null)
            StopCoroutine(pendingCompleteTurnRoutine);
        pendingCompleteTurnRoutine = StartCoroutine(CompletePlayerTurnAfterDelay());
    }

    private IEnumerator CompletePlayerTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        pendingCompleteTurnRoutine = null;

        if (pendingWaveGroups.Count > 0 || pendingDelayedSkillEffects > 0)
            yield break;

        if (currentState != GameState.Processing)
            yield break;

        if (currentLevelInfo != null && currentLevelInfo.type != null && currentLevelInfo.type.ToLower() == "puzzle")
        {
            if (CheckAllTilesCleared())
            {
                CompleteLevel();
                yield break;
            }
        }

        EndPlayerTurn();
    }
    
    private void ApplyAllyHealthIncrease(TileColor color, float totalWaveDamage)
    {
        if (allyManager == null
            || !HasEquippedColorSkill(color, "allyHPIncrease", out string identifier))
            return;

        int value = SkillManager.Instance.GetSkillValue(identifier);
        int healthIncrease = Mathf.FloorToInt(totalWaveDamage * value / 100f);
        if (healthIncrease <= 0)
            return;

        foreach (Ally ally in allyManager.GetLivingAllies())
            ally.IncreaseMaxHealth(healthIncrease);
    }

    private void ApplyAllyMoveForwardDamage(int waveGroupId, TileColor color, System.Action onComplete)
    {
        if (allyManager == null
            || boardManager == null
            || enemyManager == null
            || !HasEquippedColorSkill(color, "allyMoveForwardDamgeVerticle", out _))
        {
            onComplete?.Invoke();
            return;
        }

        List<Ally> allies = allyManager.GetLivingAllies();
        allies.Sort((a, b) => b.GridPosition.x.CompareTo(a.GridPosition.x));

        float maxMoveDuration = 0f;
        foreach (Ally ally in allies)
        {
            Vector2Int targetPos = ally.GridPosition + Vector2Int.right;
            if (targetPos.x >= boardManager.Width
                || allyManager.HasAllyAtPosition(targetPos)
                || HasLivingEnemyAtPosition(targetPos))
                continue;

            maxMoveDuration = Mathf.Max(maxMoveDuration, ally.MoveTo(targetPos));
        }

        if (maxMoveDuration <= 0.001f)
        {
            DealAllyAdjacentDamage(waveGroupId, allies);
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(FinishAllyMoveForwardAfterDelay(maxMoveDuration, waveGroupId, allies, onComplete));
    }

    private IEnumerator FinishAllyMoveForwardAfterDelay(
        float delay,
        int waveGroupId,
        List<Ally> allies,
        System.Action onComplete)
    {
        BeginDelayedSkillEffect();
        try
        {
            yield return new WaitForSeconds(delay);
            DealAllyAdjacentDamage(waveGroupId, allies);
        }
        finally
        {
            try
            {
                onComplete?.Invoke();
            }
            finally
            {
                EndDelayedSkillEffect();
            }
        }
    }

    private void DealAllyAdjacentDamage(int waveGroupId, List<Ally> allies)
    {
        if (allies == null || enemyManager == null)
            return;

        float totalDamage = 0f;
        foreach (Ally ally in allies)
        {
            if (ally == null || ally.IsDead)
                continue;

            int damage = ally.CurrentHealth;
            if (damage <= 0)
                continue;

            bool hitAnyone = false;
            foreach (Enemy enemy in enemyManager.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead || !IsAdjacent(ally.GridPosition, enemy.GridPosition))
                    continue;

                hitAnyone = true;
                PlayAllyAttackEffect(enemy);
                totalDamage += enemy.TakeDamage(damage, Vector3.right, false, 0, 0f);
            }

            if (currentBoss != null
                && !currentBoss.IsDead
                && IsAdjacent(ally.GridPosition, currentBoss.GridPosition))
            {
                hitAnyone = true;
                PlayAllyAttackEffect(currentBoss);
                totalDamage += currentBoss.TakeDamage(damage, Vector3.right, false, 0, 0f);
            }

            if (hitAnyone)
                ally.TryPlayAtkAnimation();
        }

        if (totalDamage > 0)
            RecordWaveDamage(waveGroupId, totalDamage);
    }

    private void PlayAllyAttackEffect(Enemy target)
    {
        if (target == null)
            return;

        GameObject prefab = Resources.Load<GameObject>("effect/allyAttack");
        if (prefab == null)
        {
            Debug.LogWarning("找不到特效 Resources/effect/allyAttack");
            return;
        }

        GameObject effect = Instantiate(prefab, target.transform.position, Quaternion.identity);
        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        Destroy(effect, 2f);
    }

    private bool HasLivingEnemyAtPosition(Vector2Int gridPos)
    {
        if (enemyManager != null)
        {
            foreach (Enemy enemy in enemyManager.ActiveEnemies)
            {
                if (enemy != null && !enemy.IsDead && enemy.GridPosition == gridPos)
                    return true;
            }
        }

        return currentBoss != null && !currentBoss.IsDead && currentBoss.GridPosition == gridPos;
    }

    private void ApplyKillAllWithAlly(int waveGroupId, TileColor color)
    {
        if (allyManager == null
            || !HasEquippedColorSkill(color, "killAllWithAlly", out string identifier))
            return;

        List<Ally> allies = allyManager.GetLivingAllies();
        if (allies.Count == 0)
            return;

        int value = SkillManager.Instance.GetSkillValue(identifier);
        int currentHealthTotal = 0;
        foreach (Ally ally in allies)
            currentHealthTotal += ally.CurrentHealth;

        int damage = Mathf.FloorToInt(currentHealthTotal * value / 100f);
        float totalDamage = 0f;
        if (damage > 0 && enemyManager != null)
        {
            foreach (Enemy enemy in enemyManager.ActiveEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                    totalDamage += enemy.TakeDamage(damage, Vector3.right, false, 0, damage);
            }

            if (currentBoss != null && !currentBoss.IsDead)
                totalDamage += currentBoss.TakeDamage(damage, Vector3.right, false, 0, damage);
        }

        if (totalDamage > 0)
            RecordWaveDamage(waveGroupId, totalDamage);

        foreach (Ally ally in allies)
            ally.Die();
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
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/PlayerStatus/sfx_ally_attack");
        // 所有ally向右侧发射投射物
        foreach (var ally in allyManager.ActiveAllies)
        {
            if (ally != null && !ally.IsDead)
            {
                // 播放攻击动画
                ally.TryPlayAtkAnimation();
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

        if (allyPrefab == null)
            return;

        GameObject allyObj = Instantiate(allyPrefab);
        Ally ally = allyObj.GetComponent<Ally>();
        if (ally == null)
        {
            Destroy(allyObj);
            return;
        }
        
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
        if (currentState == GameState.EnemyTurn
            || currentState == GameState.LevelComplete
            || currentState == GameState.GameOver)
            return;

        stuckProcessingFrames = 0;

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

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.NotifyEnemyTurnStart();

        // 隐藏所有技能、敌人、随从面板
        if (skillDisplayPanel != null)
        {
            skillDisplayPanel.SetActive(false);
        }
        if (enemyDescriptionPanel != null)
        {
            enemyDescriptionPanel.SetActive(false);
        }
        if (allyDescriptionPanel != null)
        {
            allyDescriptionPanel.SetActive(false);
        }

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
        // 如果是boss战，处理boss移动和行动（单独批次）
        if (currentBoss != null && !currentBoss.IsDead)
        {
            // 更新blockColor的剩余回合数（在玩家回合结束时减少）
            currentBoss.UpdateBlockColorTurns();
            
            // Boss单独执行两个批次：
            // Batch 1: Boss移动
            // Batch 2: Boss行动（使用技能），等待0.5秒
            
            // 使用反射获取moveSpeed（protected字段）
            var moveSpeedField = typeof(Enemy).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
            float bossMoveSpeed = 2f; // 默认值
            if (moveSpeedField != null)
            {
                bossMoveSpeed = (float)moveSpeedField.GetValue(currentBoss);
            }
            float bossMoveDuration = bossMoveSpeed > 0 ? 1f / bossMoveSpeed : 0.5f; // 获取boss移动速度
            float actionDelay = 0f; // 每个批次之间的延迟
            
            // Batch 1: Boss移动
            currentBoss.StartMove();
            
            // Batch 2: Boss行动（使用技能），在移动完成后执行
            DOVirtual.DelayedCall(bossMoveDuration + actionDelay, () =>
            {
                if (currentBoss != null && !currentBoss.IsDead)
                {
                    // Boss执行技能（TakeAction）
                    currentBoss.TakeAction();
                    
                    // 使用技能后等待，再执行小怪批次，最后出兵
                    DOVirtual.DelayedCall(1.2f, () =>
                    {
                        if (enemyManager != null)
                        {
                            enemyManager.ExecuteEnemyTurnBatch(() =>
                            {
                                bool spawned = SpawnEnemiesAfterEnemyActions();
                                float spawnWait = spawned ? Enemy.SpawnEnterDuration + 0.05f : 0.05f;
                                DOVirtual.DelayedCall(spawnWait, () =>
                                {
                                    if (currentBoss != null && currentBoss.IsDead)
                                    {
                                        CompleteLevel();
                                        return;
                                    }
                                    
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
                    // Boss已死亡，直接完成关卡
                    CompleteLevel();                  
                }
            });
        }
        else
        {
            // 普通战斗：先行动，再按 enemyPerRound 出兵
            if (enemyManager != null)
            {
                enemyManager.ExecuteEnemyTurnBatch(() =>
                {
                    bool spawned = SpawnEnemiesAfterEnemyActions();
                    float spawnWait = spawned ? Enemy.SpawnEnterDuration + 0.05f : 0.05f;
                    DOVirtual.DelayedCall(spawnWait, () =>
                    {
                        if (enemyManager != null && enemyManager.CanCompleteLevel())
                        {
                            CompleteLevel();
                            return;
                        }
                        
                        ShowPlayerTurnBanner();
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

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.NotifyPlayerTurnStart();
            PlayerManager.Instance.DecayShieldAtTurnStart();
        }
        
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
            earlyTurnEndScheduled = false;
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
        
        // Boss位置：地图 Boss 节点战斗时生成在棋盘最右侧（离玩家最远）
        int boardWidth = boardManager.Width;
        int boardHeight = boardManager.Height;
        int bossX = activeBattleFromBossMapNode ? boardWidth + 1 : boardWidth;
        Vector2Int bossGridPos = new Vector2Int(bossX, boardHeight - 2);
        Vector3 bossWorldPos = boardManager.GridToWorldPosition(bossGridPos);
        // 调整y坐标到 boardHeight - 1.5 的位置
        
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
        // 困难模式：所有level的difficulty自动加1
        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode())
        {
            difficulty += 1;
        }
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
        
        // 创建boss显示图片（1x3，黑色，显示在boss中心位置）
        if (boardManager != null)
        {
            boardManager.CreateBossDisplayImage(bossGridPos);
            boardManager.SetCurrentBoss(boss); // 设置boss引用，用于实时同步位置
        }
        
        currentBoss = boss;
        Debug.Log($"Boss spawned: {bossIdentifier}, HP: {calculatedHP}");
    }
    
    /// <summary>
    /// 敌人行动结束后加载新敌人
    /// </summary>
    private bool SpawnEnemiesAfterEnemyActions()
    {
        if (enemyManager == null)
            return false;
        
        if (enemyManager.HasRemainingEnemiesToSpawn())
            return enemyManager.SpawnEnemyEachTurn();
        
        if (currentBoss != null && !currentBoss.IsDead)
            return SpawnBossBattleEnemy();
        
        return false;
    }
    
    /// <summary>
    /// Boss战中每回合召唤小怪
    /// </summary>
    private bool SpawnBossBattleEnemy()
    {
        if (currentBoss == null || currentBoss.IsDead || bossBattleEnemies.Count == 0 || enemyManager == null)
            return false;
        
        int count = enemyManager.GetEnemyPerRound();
        bool spawned = false;
        for (int i = 0; i < count; i++)
        {
            if (bossBattleEnemies.Count == 0)
                break;
            
            if (bossBattleEnemyIndex >= bossBattleEnemies.Count)
                bossBattleEnemyIndex = 0;
            
            EnemySpawnInfo spawnInfo = bossBattleEnemies[bossBattleEnemyIndex];
            bossBattleEnemyIndex++;
            if (SpawnBossBattleEnemyFromInfo(spawnInfo.identifier))
                spawned = true;
        }
        return spawned;
    }
    
    /// <summary>
    /// 从信息生成Boss战中的敌人
    /// </summary>
    private bool SpawnBossBattleEnemyFromInfo(string identifier)
    {
        if (enemyManager == null || boardManager == null || string.IsNullOrEmpty(identifier))
            return false;
            
        // 从enemyInfoMap获取敌人信息
        if (!CSVLoader.Instance.enemyInfoMap.ContainsKey(identifier))
        {
            Debug.LogWarning($"Enemy identifier not found: {identifier}");
            return false;
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
            return false;
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
        enemyManager.CreateHealthBar(enemy);
        
        // 添加到EnemyManager
        enemyManager.ActiveEnemies.Add(enemy);
        enemy.PlaySpawnEnterAnimation();
        return true;
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
        
        // 关闭随从描述显示
        if (allyDescriptionPanel != null)
        {
            allyDescriptionPanel.SetActive(false);
        }
        GameManager.Instance.MusicGameOver();

        // 等待敌人移动动画完成后显示弹窗
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GameOverDialog.ShowGameOver(
                onRetryLevel: () =>
                {
                    // 重试当前关卡
                    RetryLevel();
                    GameManager.Instance.MusicGameRestart();
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
    public void RetryLevel()
    {
        // 重新开始当前战斗（地图系统下不再回退到商店）
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
        battleFromBossMapNode = activeBattleFromBossMapNode;
        StartBattle();
    }

    /// <summary>
    /// 关卡完成
    /// </summary>
    public void CompleteLevel()
    {
        // 防止重复调用：如果已经是完成状态，直接返回
        if (currentState == GameState.LevelComplete)
        {
            return;
        }
        
        currentState = GameState.LevelComplete;
        isProcessing = true;

        int totalGoldEarned = 0;
        int levelGold = 0;
        pendingBattleDisplayGold = 0;
        pendingBattleGoldToGrant = 0;
        pendingBattleConsumableId = null;
        
        // 关卡完成金币延后到战斗结果界面领取
        if (currentLevelInfo != null && currentLevelInfo.gold > 0)
        {
            int goldToAdd = currentLevelInfo.gold;
            levelGold = goldToAdd;
            totalGoldEarned += goldToAdd;
            pendingBattleGoldToGrant = goldToAdd;
            pendingBattleDisplayGold += goldToAdd;
            Debug.Log($"关卡完成，待领取 {goldToAdd} gold");
        }
        
        // 如果是gold关卡，添加从chest获得的金钱（战斗中已入账，仅用于展示）
        if (currentLevelInfo != null && currentLevelInfo.type == "gold" && goldFromChests > 0)
        {
            totalGoldEarned += goldFromChests;
            pendingBattleDisplayGold += goldFromChests;
        }

        if (activeBattleMapNode != null && activeBattleMapNode.HasConsumableReward && ConsumableManager.Instance != null)
        {
            pendingBattleConsumableId = ConsumableManager.Instance.GetRandomAvailableConsumable();
        }
        
        // 显示获得的金钱toast
        if (totalGoldEarned > 0 && ToastManager.Instance != null)
        {
            string message = $"Earned {totalGoldEarned} gold!";
            // if (currentLevelInfo != null && currentLevelInfo.type == "gold" && goldFromChests > 0)
            // {
            //     message = $"Earned {totalGoldEarned} gold! ({levelGold} from level + {goldFromChests} from chests)";
            // }
            //ToastManager.Instance.ShowToast(message);
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
        
        // 关闭随从描述显示
        if (allyDescriptionPanel != null)
        {
            allyDescriptionPanel.SetActive(false);
        }

        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_win_level");
        bool defeatedBossLevel = LevelManager.IsBossLevel(currentLevelInfo);
        bool defeatedFinalBoss = defeatedBossLevel
            && !string.IsNullOrEmpty(currentLevelInfo?.bossIdentifier)
            && currentLevelInfo.bossIdentifier.Equals("boss", System.StringComparison.OrdinalIgnoreCase);
        // 显示Victory横幅，停留2秒，期间不可操作
        if (turnBanner != null)
        {
            turnBanner.ShowBanner("Victory!", 2f, () =>
            
            {
                // 横幅显示完成后继续
                // 离开战斗模式时清除战场
                ClearBattleScene();                

                // 战斗结束后清除临时伤害加成并恢复exchange
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.EndBattle();
                }

                AdvanceBattleLevelProgress();
                CacheNextIslandTransitionState(defeatedBossLevel, defeatedFinalBoss);

                if (TryHandleGameWin())
                {
                    return;
                }

                OnBattleVictoryContinue();
            });
        }
        else
        {
            // 如果没有banner，延迟2秒后继续
            DOVirtual.DelayedCall(2f, () =>
            {
                // 离开战斗模式时清除战场
                ClearBattleScene();
                
                // 战斗结束后清除临时伤害加成并恢复exchange
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.EndBattle();
                }

                AdvanceBattleLevelProgress();
                CacheNextIslandTransitionState(defeatedBossLevel, defeatedFinalBoss);

                if (TryHandleGameWin())
                {
                    return;
                }

                OnBattleVictoryContinue();
            });
        }
    }
    
    private int GetBattleIslandId()
    {
        if (mapController != null)
        {
            return mapController.GetCurrentIslandId();
        }

        return currentIslandId;
    }

    /// <summary>
    /// 战斗胜利后推进岛屿内非 Boss 关进度
    /// </summary>
    private void AdvanceBattleLevelProgress()
    {
        AdvanceIslandBattleProgress();
        activeBattleLevelIndex = -1;
        activeBattleFromBossMapNode = false;
    }

    private void AdvanceIslandBattleProgress()
    {
        if (currentLevelInfo == null || !LevelManager.IsBossLevel(currentLevelInfo))
            islandNonBossProgress++;

        playerLevel++;
    }

    private bool TryHandleGameWin()
    {
        if (LevelManager.IsBossLevel(currentLevelInfo)
            && !string.IsNullOrEmpty(currentLevelInfo?.bossIdentifier)
            && currentLevelInfo.bossIdentifier.Equals("boss", System.StringComparison.OrdinalIgnoreCase))
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetHasWonGame(true);
            }
            ShowFinalEventMenu();
            return true;
        }

        bool isGameWin = false;
        if (LevelManager.IsBossLevel(currentLevelInfo)
            && LevelManager.Instance != null
            && LevelManager.Instance.GetNextIslandId(currentLevelInfo.island) < 0)
        {
            isGameWin = true;
        }

        if (isGameWin && GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetHasWonGame(true);
        }

        if (isGameWin)
        {
            ShowFinalEventMenu();
            return true;
        }

        return false;
    }

    private void OnBattleVictoryContinue()
    {
        ShowBattleResultMenu();
    }

    private void ShowBattleResultMenu()
    {
        BattleResultMenu menu = BattleResultMenu.GetOrCreate();
        if (menu == null)
        {
            activeBattleMapNode = null;
            OpenMap();
            return;
        }

        var rewards = new BattleResultMenu.RewardData
        {
            displayGold = pendingBattleDisplayGold,
            goldToGrant = pendingBattleGoldToGrant,
            consumableId = pendingBattleConsumableId,
            includeCardReward = true,
            includeRelicReward = activeBattleMapNode != null && activeBattleMapNode.IsBossNode
        };

        menu.ShowRewards(rewards, () =>
        {
            pendingBattleDisplayGold = 0;
            pendingBattleGoldToGrant = 0;
            pendingBattleConsumableId = null;
            activeBattleMapNode = null;
            OpenMap();
        });
    }

    private void CacheNextIslandTransitionState(bool defeatedBossLevel, bool defeatedFinalBoss)
    {
        if (!defeatedBossLevel || defeatedFinalBoss || currentLevelInfo == null)
        {
            waitingForNextIslandSelection = false;
            waitingIslandId = -1;
            waitingNextIslandId = -1;
            return;
        }

        waitingForNextIslandSelection = true;
        waitingIslandId = currentLevelInfo.island;
        int nextIsland = LevelManager.Instance != null
            ? LevelManager.Instance.GetNextIslandId(currentLevelInfo.island)
            : waitingIslandId + 1;
        waitingNextIslandId = nextIsland >= 0 ? nextIsland : waitingIslandId + 1;
    }

    public bool ShouldShowNextIslandButton(int currentIslandId)
    {
        return waitingForNextIslandSelection
            && waitingIslandId >= 0
            && currentIslandId == waitingIslandId;
    }

    public void OnNextIslandButtonClicked()
    {
        if (!waitingForNextIslandSelection || mapController == null)
        {
            return;
        }

        int targetIslandId = waitingNextIslandId >= 0 ? waitingNextIslandId : waitingIslandId + 1;
        waitingForNextIslandSelection = false;
        waitingIslandId = -1;
        waitingNextIslandId = -1;
        currentIslandId = targetIslandId;
        islandNonBossProgress = 0;

        mapController.ClearForcedIsland();
        mapController.EnterIslandAndReset(targetIslandId);
    }

    /// <summary>
    /// 显示事件菜单
    /// </summary>
    private void ShowEventMenu()
    {
        // 检查事件菜单是否已经显示，防止重复调用
        EventMenu existingEventMenu = FindObjectOfType<EventMenu>();
        if (existingEventMenu != null && existingEventMenu.IsActive)
        {
            Debug.LogWarning("EventMenu已经显示，跳过重复调用");
            return;
        }
        
        // 检查是否有eventType，如果没有则直接进入商店
        if (currentLevelInfo == null || string.IsNullOrEmpty(currentLevelInfo.eventType))
        {
            // eventType为空，不显示事件，直接进入商店
            ShowShopMenu();
            return;
        }
        
        EventMenu eventMenu = existingEventMenu;
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
    /// 显示最终事件菜单（游戏胜利后的最终event）
    /// </summary>
    private void ShowFinalEventMenu()
    {
        // 检查是否有eventType，如果没有则直接显示统计菜单
        if (currentLevelInfo == null || string.IsNullOrEmpty(currentLevelInfo.eventType))
        {
            // eventType为空，不显示事件，直接显示统计菜单
            ShowWinStatistics();
            return;
        }
        
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("EventMenu");
            eventMenu = menuObj.AddComponent<EventMenu>();
        }
        
        // 显示对应类型的最终事件（isFinal = true）
        eventMenu.ShowEventByType(currentLevelInfo.eventType, () =>
        {
            // 最终事件完成后，显示统计菜单
            ShowWinStatistics();
        }, isFinal: true);
    }
    
    /// <summary>
    /// 显示胜利统计菜单
    /// </summary>
    private void ShowWinStatistics()
    {
        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("StatisticsMenu");
            statisticsMenu = menuObj.AddComponent<StatisticsMenu>();
        }
        statisticsMenu.ShowWinStatistics();
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
        
        // 清除boss显示图片
        if (boardManager != null)
        {
            boardManager.ClearBossDisplayImage();
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
        
        // 清除所有fog、dirt和blockColor技能block的状态
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
                        // 清除blockColor技能block的状态
                        tile.SetDisabled(false);
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
    
    public void CheatEnterPuzzleEditMode() => EnterPuzzleEditMode();
    public void CheatSaveCurrentPuzzle() => SaveCurrentPuzzle();
    public void CheatLoadFirstPuzzle() => LoadFirstPuzzle();
    public void CheatEnterPuzzlePlayMode() => EnterPuzzlePlayMode();

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
    /// Puzzle 编辑模式作弊输入（1-4 + 鼠标）
    /// </summary>
    public void CheatProcessPuzzleEditInput()
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




