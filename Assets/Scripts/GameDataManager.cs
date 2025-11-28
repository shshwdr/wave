using UnityEngine;

/// <summary>
/// 游戏数据管理器 - 管理全局游戏数据（使用PlayerPrefs保存）
/// </summary>
public class GameDataManager : MonoBehaviour
{
    private static GameDataManager instance;
    public static GameDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GameDataManager");
                instance = obj.AddComponent<GameDataManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }
    
    private const string HAS_WON_GAME_KEY = "HasWonGame";
    private const string IS_IN_HARD_MODE_KEY = "IsInHardMode";
    
    private bool hasWonGame = false;
    private bool isInHardMode = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 加载数据
    /// </summary>
    private void LoadData()
    {
        hasWonGame = PlayerPrefs.GetInt(HAS_WON_GAME_KEY, 0) == 1;
        isInHardMode = PlayerPrefs.GetInt(IS_IN_HARD_MODE_KEY, 0) == 1;
    }
    
    /// <summary>
    /// 保存数据
    /// </summary>
    private void SaveData()
    {
        PlayerPrefs.SetInt(HAS_WON_GAME_KEY, hasWonGame ? 1 : 0);
        PlayerPrefs.SetInt(IS_IN_HARD_MODE_KEY, isInHardMode ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 获取是否已赢得游戏
    /// </summary>
    public bool HasWonGame()
    {
        return hasWonGame;
    }
    
    /// <summary>
    /// 设置是否已赢得游戏
    /// </summary>
    public void SetHasWonGame(bool value)
    {
        hasWonGame = value;
        SaveData();
    }
    
    /// <summary>
    /// 获取是否在困难模式
    /// </summary>
    public bool IsInHardMode()
    {
        return isInHardMode;
    }
    
    /// <summary>
    /// 设置是否在困难模式
    /// </summary>
    public void SetIsInHardMode(bool value)
    {
        isInHardMode = value;
        SaveData();
    }
}

