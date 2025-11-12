using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 统计菜单 - 可以在商店中打开显示上一回合的统计，或胜利时显示
/// </summary>
public class StatisticsMenu : MenuBase
{
    [Header("颜色区域")]
    [SerializeField] public ColorArea[] colorArea = new ColorArea[4]; // 0=红，1=黄，2=蓝，3=绿
    
    [Header("统计内容")]
    [SerializeField] private Transform statisticsContentParent;
    [SerializeField] private GameObject statisticItemPrefab;
    
    [Header("技能图标Prefab")]
    [SerializeField] private GameObject skillIconPrefab;
    
    [Header("标题")]
    [SerializeField] private TMP_Text titleText;
    
    [Header("按钮")]
    [SerializeField] private Button restartButton; // 胜利时显示
    [SerializeField] private Button closeButtonOverride; // 商店中显示（覆盖MenuBase的closeButton）
    
    private bool isWinMode = false; // true = 胜利模式, false = 商店模式
    
    protected override void Awake()
    {
        base.Awake();
        
        // 初始化颜色区域
        for (int i = 0; i < 4; i++)
        {
            int colorIndex = i;
            if (colorArea[i] != null)
            {
                // 设置颜色图片
                if (colorArea[i].colorImage != null)
                {
                    TileColor tileColor = (TileColor)colorIndex;
                    Color waveColor = TileColorUtil.GetUnityColor(tileColor);
                    colorArea[i].colorImage.color = waveColor;
                }
            }
        }
        
        // 初始化Restart按钮
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
            restartButton.gameObject.SetActive(false); // 默认隐藏
        }
        
        // 初始化Close按钮（如果提供了覆盖版本）
        if (closeButtonOverride != null)
        {
            closeButtonOverride.onClick.AddListener(() => Hide());
            closeButtonOverride.gameObject.SetActive(false); // 默认隐藏
        }
    }
    
    /// <summary>
    /// 显示上一回合的统计（商店模式）
    /// </summary>
    public void ShowLastRoundStatistics()
    {
        isWinMode = false;
        UpdateDisplay();
        Show();
    }
    
    /// <summary>
    /// 显示胜利统计（胜利模式）
    /// </summary>
    public void ShowWinStatistics()
    {
        isWinMode = true;
        UpdateDisplay();
        Show();
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        UpdateColorAreas();
        UpdateStatistics();
        UpdateButtons();
    }
    
    /// <summary>
    /// 更新按钮显示
    /// </summary>
    private void UpdateButtons()
    {
        if (isWinMode)
        {
            // 胜利模式：显示Restart按钮，隐藏Close按钮
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
            }
            if (closeButtonOverride != null)
            {
                closeButtonOverride.gameObject.SetActive(false);
            }
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }
            if (titleText != null)
            {
                titleText.text = "You Win";
            }
        }
        else
        {
            // 商店模式：显示Close按钮，隐藏Restart按钮
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }
            if (closeButtonOverride != null)
            {
                closeButtonOverride.gameObject.SetActive(true);
            }
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
            }
            if (titleText != null)
            {
                titleText.text = "Statistics";
            }
        }
    }
    
    /// <summary>
    /// 更新颜色区域显示
    /// </summary>
    private void UpdateColorAreas()
    {
        if (PlayerManager.Instance == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            if (colorArea[i] == null)
                continue;

            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(i);
            int maxSlots = PlayerManager.Instance.GetWaveMaxSlotCount(i);
            
            // 更新slot数量显示
            if (colorArea[i].slotText != null)
            {
                colorArea[i].slotText.text = $"{skillIdentifiers.Count}/{maxSlots}";
            }

            // 更新技能图标显示
            if (colorArea[i].slotParent != null)
            {
                // 清除旧的图标
                foreach (Transform child in colorArea[i].slotParent)
                {
                    Destroy(child.gameObject);
                }

                // 创建新的图标
                if (skillIconPrefab != null)
                {
                    foreach (var identifier in skillIdentifiers)
                    {
                        GameObject iconObj = Instantiate(skillIconPrefab, colorArea[i].slotParent);
                        SkillIconUI icon = iconObj.GetComponent<SkillIconUI>();
                        if (icon != null)
                        {
                            // 初始化图标（传入null作为menu参数，禁用拖拽功能）
                            icon.Init(identifier, i, null);
                            // 禁用拖拽事件
                            EventTrigger trigger = iconObj.GetComponent<EventTrigger>();
                            if (trigger != null)
                            {
                                trigger.enabled = false;
                            }
                            // 禁用CanvasGroup的交互（防止拖拽）
                            CanvasGroup canvasGroup = iconObj.GetComponent<CanvasGroup>();
                            if (canvasGroup != null)
                            {
                                canvasGroup.blocksRaycasts = false;
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 更新统计信息显示
    /// </summary>
    private void UpdateStatistics()
    {
        if (statisticsContentParent == null || statisticItemPrefab == null)
            return;
        
        // 清除旧的统计项
        foreach (Transform child in statisticsContentParent)
        {
            Destroy(child.gameObject);
        }
        
        // 获取要显示的统计
        List<ColorStatistic> stats = null;
        if (isWinMode)
        {
            // 胜利模式：显示最后一回合的统计
            if (StatisticsManager.Instance != null)
            {
                stats = StatisticsManager.Instance.GetLastRoundStatistics();
            }
        }
        else
        {
            // 商店模式：显示上一回合的统计
            if (StatisticsManager.Instance != null)
            {
                stats = StatisticsManager.Instance.GetLastRoundStatistics();
            }
        }
        
        if (stats == null || stats.Count == 0)
        {
            // 如果没有统计，显示提示
            GameObject noDataText = new GameObject("NoDataText");
            noDataText.transform.SetParent(statisticsContentParent);
            TMP_Text text = noDataText.AddComponent<TextMeshProUGUI>();
            text.text = "No statistics available";
            return;
        }
        
        // 为每个颜色创建统计项
        for (int i = 0; i < 4 && i < stats.Count; i++)
        {
            ColorStatistic stat = stats[i];
            if (stat == null)
                continue;
                
            GameObject statItem = Instantiate(statisticItemPrefab, statisticsContentParent);
            UpdateStatisticItem(statItem, stat);
        }
    }
    
    /// <summary>
    /// 更新统计项显示
    /// </summary>
    private void UpdateStatisticItem(GameObject item, ColorStatistic stat)
    {
        // 假设statisticItemPrefab有以下子对象：
        // - ColorNameText (TMP_Text): 颜色名称
        // - TotalTilesText (TMP_Text): 总tile数
        // - TotalWavesText (TMP_Text): 总wave数
        // - MaxWaveSizeText (TMP_Text): 最大wave大小
        // - AverageDamageText (TMP_Text): 平均伤害
        // - MaxDamageText (TMP_Text): 最大伤害
        // - TotalDamageText (TMP_Text): 总伤害
        
        //TMP_Text colorNameText = item.transform.Find("ColorNameText")?.GetComponent<TMP_Text>();
        TMP_Text totalTilesText = item.transform.Find("TotalTilesText")?.GetComponent<TMP_Text>();
        TMP_Text totalWavesText = item.transform.Find("TotalWavesText")?.GetComponent<TMP_Text>();
        TMP_Text maxWaveSizeText = item.transform.Find("MaxWaveSizeText")?.GetComponent<TMP_Text>();
        TMP_Text averageDamageText = item.transform.Find("AverageDamageText")?.GetComponent<TMP_Text>();
        TMP_Text maxDamageText = item.transform.Find("MaxDamageText")?.GetComponent<TMP_Text>();
        TMP_Text totalDamageText = item.transform.Find("TotalDamageText")?.GetComponent<TMP_Text>();
        
        string colorName = stat.color.ToString();
        
        // if (colorNameText != null)
        //     colorNameText.text = $"{colorName} Color";
        
        if (totalTilesText != null)
            totalTilesText.text = $"Tiles Generated\n{stat.totalTilesGenerated}";
        
        if (totalWavesText != null)
            totalWavesText.text = $"Waves Generated\n{stat.totalWavesGenerated}";
        
        if (maxWaveSizeText != null)
            maxWaveSizeText.text = $"Max Wave Size\n{stat.maxWaveSize}";
        
        if (averageDamageText != null)
            averageDamageText.text = $"Ave Wave Damage\n{stat.averageDamagePerWaveGroup:F1}";
        
        if (maxDamageText != null)
            maxDamageText.text = $"Max Wave Damage\n{stat.maxDamagePerWaveGroup:F1}";
        
        if (totalDamageText != null)
            totalDamageText.text = $"Total Damage\n{stat.totalDamage:F1}";
    }
    
    /// <summary>
    /// Restart按钮点击事件
    /// </summary>
    private void OnRestartClicked()
    {
        // 重新开始游戏
        MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager != null)
        {
            mainGameManager.Restart();
        }
        Hide();
    }
    
    public override void Show(bool immediate = false)
    {
        base.Show(immediate);
        UpdateDisplay();
    }
}

