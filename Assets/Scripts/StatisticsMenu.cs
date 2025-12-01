using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

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
    [SerializeField] private Button totalToggle; // 切换显示当前波次/所有波次统计
    [SerializeField] private Button tryHardModeButton; // 尝试困难模式按钮（胜利时显示）
    
    [Header("技能详情显示")]
    [SerializeField] private GameObject skillDetailPanel;
    [SerializeField] private TMP_Text skillDetailText;
    
    [FormerlySerializedAs("backgroundColor1")]
    [Header("背景设置")]
    [SerializeField] private Color backgroundColorWin; // 商店模式的背景颜色
    [SerializeField] private Color backgroundColor2; // 胜利模式的背景颜色
    [SerializeField] private Image backgroundImage; // 胜利模式的背景图片
    [SerializeField] private GameObject victoryGO; // 胜利模式的背景图片
    
    
    
    private bool isWinMode = false; // true = 胜利模式, false = 商店模式
    private bool showTotalStatistics = false; // true = 显示所有波次统计, false = 显示当前波次统计
    private List<ColorStatistic> currentStats = new List<ColorStatistic>(); // 当前显示的统计列表
    
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
        
        // 初始化TryHardMode按钮
        if (tryHardModeButton != null)
        {
            tryHardModeButton.onClick.AddListener(OnTryHardModeClicked);
            tryHardModeButton.gameObject.SetActive(false); // 默认隐藏
        }
        
        // 初始化Close按钮（如果提供了覆盖版本）
        if (closeButtonOverride != null)
        {
            closeButtonOverride.onClick.AddListener(() => Hide());
            closeButtonOverride.gameObject.SetActive(false); // 默认隐藏
        }
        
        // 初始化TotalToggle按钮
        if (totalToggle != null)
        {
            totalToggle.onClick.AddListener(OnTotalToggleClicked);
            UpdateTotalToggleButtonText();
        }
        
        // 初始化技能详情面板
        InitSkillDetailPanel();
        
        // 初始化颜色区域的悬停事件
        for (int i = 0; i < 4; i++)
        {
            int colorIndex = i;
            if (colorArea[i] != null && colorArea[i].button != null)
            {
                EventTrigger trigger = colorArea[i].button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = colorArea[i].button.gameObject.AddComponent<EventTrigger>();
                }
                
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => OnColorAreaButtonHover(colorIndex, true));
                trigger.triggers.Add(entryEnter);
                
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => OnColorAreaButtonHover(colorIndex, false));
                trigger.triggers.Add(entryExit);
            }
        }
    }
    
    /// <summary>
    /// 初始化技能详情面板
    /// </summary>
    private void InitSkillDetailPanel()
    {
        if (skillDetailPanel == null)
        {
            // 创建技能详情面板（左上角）
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            skillDetailPanel = new GameObject("SkillDetailPanel");
            skillDetailPanel.transform.SetParent(canvasObj.transform);
            RectTransform rectTransform = skillDetailPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(400, 300);
            
            // 添加背景
            Image bg = skillDetailPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            
            // 添加文本
            GameObject textObj = new GameObject("SkillDetailText");
            textObj.transform.SetParent(skillDetailPanel.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            skillDetailText = textObj.AddComponent<TextMeshProUGUI>();
            skillDetailText.fontSize = 26;
            skillDetailText.color = Color.white;
            skillDetailText.alignment = TextAlignmentOptions.TopLeft;
            if (CSVLoader.Instance != null && CSVLoader.Instance.font != null)
            {
                skillDetailText.font = CSVLoader.Instance.font;
            }
        }
        
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 颜色区域按钮悬停事件
    /// </summary>
    private void OnColorAreaButtonHover(int colorIndex, bool isEntering)
    {
        if (skillDetailPanel == null || skillDetailText == null)
            return;
        
        if (isEntering)
        {
            // 显示该颜色所有技能的详情
            if (PlayerManager.Instance != null && SkillManager.Instance != null)
            {
                List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
                string detailText = "";
                foreach (var identifier in skillIdentifiers)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
                        detailText += description + "\n";
                    }
                }
                
                if (!string.IsNullOrEmpty(detailText))
                {
                    skillDetailText.text = detailText;
                    // 根据颜色索引设置panel背景色
                    SetSkillPanelColorByIndex(skillDetailPanel, colorIndex);
                    skillDetailPanel.SetActive(true);
                }
            }
        }
        else
        {
            skillDetailPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示技能详情（用于技能图标悬停）
    /// </summary>
    public void ShowSkillDetail(string identifier)
    {
        if (skillDetailPanel == null || skillDetailText == null || SkillManager.Instance == null)
            return;
        
        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
        if (!string.IsNullOrEmpty(description))
        {
            skillDetailText.text = description;
            // 根据技能颜色设置panel背景色
            SetSkillPanelColor(skillDetailPanel, identifier);
            skillDetailPanel.SetActive(true);
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
                Color colorValue = TileColorUtil.GetUnityColor(tileColor);
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
    /// 根据颜色索引设置panel背景色（用于颜色区域悬停）
    /// </summary>
    private void SetSkillPanelColorByIndex(GameObject panel, int colorIndex)
    {
        if (panel == null)
            return;
        
        // 获取panel的Image组件（背景）
        Image bgImage = panel.GetComponent<Image>();
        if (bgImage == null)
            return;
        
        // 根据颜色索引设置颜色（0=红，1=黄，2=蓝，3=绿）
        if (colorIndex >= 0 && colorIndex < 4)
        {
            TileColor tileColor = (TileColor)colorIndex;
            Color colorValue = TileColorUtil.GetUnityColor(tileColor);
            bgImage.color = colorValue;
        }
        else
        {
            // 无效索引，使用默认颜色（白色）
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
    /// 隐藏技能详情
    /// </summary>
    public void HideSkillDetail()
    {
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
        }
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
    }
    
    /// <summary>
    /// 显示上一回合的统计（商店模式）
    /// </summary>
    public void ShowLastRoundStatistics()
    {
        isWinMode = false;
        showTotalStatistics = false; // 重置为显示当前波次
        UpdateDisplay();
        Show();
    }
    
    /// <summary>
    /// 显示胜利统计（胜利模式）
    /// </summary>
    public void ShowWinStatistics()
    {
        isWinMode = true;
        showTotalStatistics = false; // 重置为显示当前波次
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
        UpdateBackground();
        UpdateTotalToggleButtonText();
    }
    
    /// <summary>
    /// 更新背景显示
    /// </summary>
    private void UpdateBackground()
    {
        if (isWinMode)
        {
            // 胜利模式：显示backgroundColor2和backgroundImage
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
                backgroundImage.color = backgroundColorWin;
                victoryGO.SetActive(true);
            }
        }
        else
        {
            // 商店模式：显示backgroundColor1，隐藏backgroundImage
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
                backgroundImage.color = backgroundColor2;
                victoryGO.SetActive(false);
            }
        }
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
            // 显示TryHardMode按钮（如果GameDataManager存在且hasWonGame为true）
            if (tryHardModeButton != null)
            {
                bool showHardModeButton = GameDataManager.Instance != null && GameDataManager.Instance.HasWonGame();
                tryHardModeButton.gameObject.SetActive(showHardModeButton);
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
                titleText.text = "YOU WIN!";
            }
        }
        else
        {
            // 商店模式：显示Close按钮，隐藏Restart按钮和TryHardMode按钮
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }
            if (tryHardModeButton != null)
            {
                tryHardModeButton.gameObject.SetActive(false);
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
                            // 注意：SkillIconUI的OnBeginDrag已经检查了parentMenu，如果为null会直接返回，所以不需要额外禁用拖拽
                            // 保留悬停功能（IPointerEnterHandler和IPointerExitHandler会自动工作）
                            CanvasGroup canvasGroup = iconObj.GetComponent<CanvasGroup>();
                            if (canvasGroup != null)
                            {
                                canvasGroup.blocksRaycasts = true; // 允许射线检测以支持悬停
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
        currentStats = null;
        if (showTotalStatistics)
        {
            // 显示所有波次的统计
            if (StatisticsManager.Instance != null)
            {
                currentStats = StatisticsManager.Instance.GetTotalGameStatistics();
            }
        }
        else
        {
            // 显示当前波次的统计
            if (isWinMode)
            {
                // 胜利模式：显示最后一回合的统计
                if (StatisticsManager.Instance != null)
                {
                    currentStats = StatisticsManager.Instance.GetLastRoundStatistics();
                }
            }
            else
            {
                // 商店模式：显示上一回合的统计
                if (StatisticsManager.Instance != null)
                {
                    currentStats = StatisticsManager.Instance.GetLastRoundStatistics();
                }
            }
        }
        
        if (currentStats == null || currentStats.Count == 0)
        {
            // 如果没有统计，显示提示
            GameObject noDataText = new GameObject("NoDataText");
            noDataText.transform.SetParent(statisticsContentParent);
            TMP_Text text = noDataText.AddComponent<TextMeshProUGUI>();
            text.text = "No statistics available";
            return;
        }
        
        // 为每个颜色创建统计项
        for (int i = 0; i < 4 && i < currentStats.Count; i++)
        {
            ColorStatistic stat = currentStats[i];
            if (stat == null)
                continue;
                
            GameObject statItem = Instantiate(statisticItemPrefab, statisticsContentParent);
            UpdateStatisticItem(statItem, stat, currentStats);
        }
    }
    
    /// <summary>
    /// 更新统计项显示
    /// </summary>
    private void UpdateStatisticItem(GameObject item, ColorStatistic stat, List<ColorStatistic> allStats)
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
        
        // 找出每个项目的最大值
        int maxTiles = 0;
        int maxWaves = 0;
        int maxWaveSize = 0;
        float maxAverageDamage = 0f;
        float maxMaxDamage = 0f;
        float maxTotalDamage = 0f;
        
        foreach (var s in allStats)
        {
            if (s == null) continue;
            if (s.totalTilesGenerated > maxTiles) maxTiles = s.totalTilesGenerated;
            if (s.totalWavesGenerated > maxWaves) maxWaves = s.totalWavesGenerated;
            if (s.maxWaveSize > maxWaveSize) maxWaveSize = s.maxWaveSize;
            if (s.averageDamagePerWaveGroup > maxAverageDamage) maxAverageDamage = s.averageDamagePerWaveGroup;
            if (s.maxDamagePerWaveGroup > maxMaxDamage) maxMaxDamage = s.maxDamagePerWaveGroup;
            if (s.totalDamage > maxTotalDamage) maxTotalDamage = s.totalDamage;
        }
        
        string colorName = stat.color.ToString();
        
        // if (colorNameText != null)
        //     colorNameText.text = $"{colorName} Color";
        
        if (totalTilesText != null)
        {
            totalTilesText.text = $"Tiles Cleared\n{stat.totalTilesGenerated}";
            // 如果是最大值，显示为红色
            if (stat.totalTilesGenerated == maxTiles && maxTiles > 0)
            {
                totalTilesText.color = Color.red;
            }
            else
            {
                totalTilesText.color = Color.white;
            }
        }
        
        if (totalWavesText != null)
        {
            totalWavesText.text = $"Waves Generated\n{stat.totalWavesGenerated}";
            // 如果是最大值，显示为红色
            if (stat.totalWavesGenerated == maxWaves && maxWaves > 0)
            {
                totalWavesText.color = Color.red;
            }
            else
            {
                totalWavesText.color = Color.white;
            }
        }
        
        if (maxWaveSizeText != null)
        {
            maxWaveSizeText.text = $"Max Size\n{stat.maxWaveSize}";
            // 如果是最大值，显示为红色
            if (stat.maxWaveSize == maxWaveSize && maxWaveSize > 0)
            {
                maxWaveSizeText.color = Color.red;
            }
            else
            {
                maxWaveSizeText.color = Color.white;
            }
        }
        
        if (averageDamageText != null)
        {
            averageDamageText.text = $"Ave Damage\n{stat.averageDamagePerWaveGroup:F1}";
            // 如果是最大值，显示为红色
            if (Mathf.Approximately(stat.averageDamagePerWaveGroup, maxAverageDamage) && maxAverageDamage > 0)
            {
                averageDamageText.color = Color.red;
            }
            else
            {
                averageDamageText.color = Color.white;
            }
        }
        
        if (maxDamageText != null)
        {
            maxDamageText.text = $"Max Damage\n{stat.maxDamagePerWaveGroup:F1}";
            // 如果是最大值，显示为红色
            if (Mathf.Approximately(stat.maxDamagePerWaveGroup, maxMaxDamage) && maxMaxDamage > 0)
            {
                maxDamageText.color = Color.red;
            }
            else
            {
                maxDamageText.color = Color.white;
            }
        }
        
        if (totalDamageText != null)
        {
            totalDamageText.text = $"Total Damage\n{stat.totalDamage:F1}";
            // 如果是最大值，显示为红色
            if (Mathf.Approximately(stat.totalDamage, maxTotalDamage) && maxTotalDamage > 0)
            {
                totalDamageText.color = Color.red;
            }
            else
            {
                totalDamageText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// TotalToggle按钮点击事件
    /// </summary>
    private void OnTotalToggleClicked()
    {
        showTotalStatistics = !showTotalStatistics;
        UpdateTotalToggleButtonText();
        UpdateStatistics();
    }
    
    /// <summary>
    /// 更新TotalToggle按钮的文字
    /// </summary>
    private void UpdateTotalToggleButtonText()
    {
        if (totalToggle == null)
            return;
        
        TMP_Text buttonText = totalToggle.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            if (showTotalStatistics)
            {
                buttonText.text = "CURRENT";
            }
            else
            {
                buttonText.text = "TOTAL";
            }
        }
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
    
    /// <summary>
    /// TryHardMode按钮点击事件
    /// </summary>
    private void OnTryHardModeClicked()
    {
        // 设置困难模式标识
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetIsInHardMode(true);
        }
        
        // 重新开始游戏（困难模式）
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
    
    public override void Hide(bool immediate = false)
    {
        // 隐藏技能详情面板
        HideSkillDetail();
        base.Hide(immediate);
    }
}

