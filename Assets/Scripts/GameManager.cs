using UnityEngine;

/// <summary>
/// 游戏主管理器 - 控制游戏主逻辑，调用MainGameManager和其他manager
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("管理器引用")]
    [SerializeField] private MainGameManager mainGameManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnemyManager enemyManager;

    private void Start()
    {
        InitializeManagers();
    }

    /// <summary>
    /// 初始化所有管理器
    /// </summary>
    private void InitializeManagers()
    {
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
}


