using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// 游戏主管理器 - 控制游戏主逻辑，调用MainGameManager和其他manager
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("管理器引用")]
    [SerializeField] private MainGameManager mainGameManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnemyManager enemyManager;

    public FMOD.Studio.EventInstance levelMusic;

    private void Awake()
    {
        InitializeManagers();
    }

    private void Start()
    {
        levelMusic = FMODUnity.RuntimeManager.CreateInstance("event:/Music/mus_gameplay_basic_level");
        levelMusic.start();
    }

    /// <summary>
    /// 初始化所有管理器
    /// </summary>
    private void InitializeManagers()
    {
        CSVLoader.Instance.Init();
        
        // 初始化技能管理器
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.Init();
        }
        
        // 初始化关卡管理器
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.Init();
        }

        // 初始化玩家管理器
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.Init();
        }
        
        // 如果引用为空，尝试自动查找
        if (mainGameManager == null)
            mainGameManager = FindObjectOfType<MainGameManager>();
        
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
        
        if (enemyManager == null)
            enemyManager = FindObjectOfType<EnemyManager>();

        // 可以在这里进行初始化配置等
        Debug.Log("GameManager initialized");
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        if (mainGameManager != null)
        {
            mainGameManager.StartBattle();
        }
        else
        {
            Debug.LogWarning("MainGameManager not found!");
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        levelMusic.setParameterByName("Game Over", 0);
        levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        levelMusic.start();

        StartNewGame();
    }

    /// <summary>
    /// 游戏结束回调
    /// </summary>
    public void OnGameOver()
    {
        Debug.Log("Game Over triggered from GameManager");
        // 可以在这里显示游戏结束UI等 
    }

    private void OnDestroy()
    {
        levelMusic.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        levelMusic.release();
    }

    public void MusicGameOver()
    {
        levelMusic.setParameterByName("Game Over", 1);
    }

    public void MusicGameRestart()
    {
        levelMusic.setParameterByName("Game Over", 0);
    }

    public void MusicBoss()
    {
        levelMusic.setParameterByName("Music Level", 1);
    }

    public void MusicNormal()
    {
        levelMusic.setParameterByName("Music Level", 0);
    }
}


