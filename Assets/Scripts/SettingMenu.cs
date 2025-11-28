using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 设置菜单 - 继承自MenuBase
/// </summary>
public class SettingMenu : MenuBase
{
    [Header("按钮")]
    [SerializeField] private Button resumeButton; // 继续按钮
    [SerializeField] private Button retryButton; // 重试当前关卡按钮
    [SerializeField] private Button restartButton; // 重新开始按钮
    [SerializeField] private Button backToMainMenuButton; // 返回主菜单按钮
    [SerializeField] private Button statisticButton; // 统计按钮
    
    protected override void Awake()
    {
        base.Awake();
        
        // 初始化按钮事件
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveAllListeners();
            backToMainMenuButton.onClick.AddListener(OnBackToMainMenuClicked);
        }
        
        if (statisticButton != null)
        {
            statisticButton.onClick.RemoveAllListeners();
            statisticButton.onClick.AddListener(OnStatisticClicked);
        }
        
        // 更新按钮显示
        UpdateButtonDisplay();
    }
    
    /// <summary>
    /// 更新按钮显示（根据困难模式）
    /// </summary>
    private void UpdateButtonDisplay()
    {
        // 困难模式下隐藏retry按钮
        bool isInHardMode = GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode();
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(!isInHardMode);
        }
    }
    
    public override void Show(bool immediate = false)
    {
        base.Show(immediate);
        // 每次显示时更新按钮显示
        UpdateButtonDisplay();
    }
    
    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnResumeClicked()
    {
        Hide();
    }
    
    /// <summary>
    /// 重试当前关卡按钮点击事件
    /// </summary>
    private void OnRetryClicked()
    {
        MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager != null)
        {
            mainGameManager.RetryLevel();
        }
        Hide();
    }
    
    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartClicked()
    {
        MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager != null)
        {
            mainGameManager.Restart();
        }
        Hide();
    }
    
    /// <summary>
    /// 返回主菜单按钮点击事件
    /// </summary>
    private void OnBackToMainMenuClicked()
    {
        // 加载Start场景
        SceneManager.LoadScene("Start");
    }
    
    /// <summary>
    /// 统计按钮点击事件
    /// </summary>
    private void OnStatisticClicked()
    {
        // 显示统计菜单
        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu != null)
        {
            statisticsMenu.ShowLastRoundStatistics();
        }
        Hide();
        // 不隐藏设置菜单，让用户可以在统计菜单关闭后继续使用设置菜单
    }
}

