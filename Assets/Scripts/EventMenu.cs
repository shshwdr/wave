using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 事件菜单 - 战斗后显示随机事件
/// </summary>
public class EventMenu : MenuBase
{
    [Header("事件显示")]
    [SerializeField] private TMP_Text eventNameText;
    [SerializeField] private TMP_Text eventDescriptionText;
    
    [Header("选项按钮")]
    [SerializeField] private Button option0Button;
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private TMP_Text option0Text;
    [SerializeField] private TMP_Text option1Text;
    [SerializeField] private TMP_Text option2Text;
    
    [Header("结果显示")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultInfoText;
    [SerializeField] private Button nextButton;
    
    private EventInfo currentEvent;
    private int selectedOptionIndex = -1;
    private Action onEventComplete; // 事件完成后的回调（进入商店）
    private bool isFinalEvent = false; // 是否是最终event
    private TMP_Text nextButtonText; // nextButton的文本组件
    private static HashSet<string> usedEventIdentifiers = new HashSet<string>(); // 已使用的事件标识符

    /// <summary>
    /// 检查事件是否已被使用
    /// </summary>
    public static bool IsEventUsed(string identifier)
    {
        return usedEventIdentifiers.Contains(identifier);
    }

    protected override void Awake()
    {
        base.Awake();
        
        // 初始化选项按钮
        if (option0Button != null)
        {
            option0Button.onClick.AddListener(() => OnOptionSelected(0));
        }
        if (option1Button != null)
        {
            option1Button.onClick.AddListener(() => OnOptionSelected(1));
        }
        if (option2Button != null)
        {
            option2Button.onClick.AddListener(() => OnOptionSelected(2));
        }
        
        // 初始化next按钮
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            // 获取nextButton的文本组件
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>();
        }
        
        // 初始隐藏结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // 如果Text组件未在Inspector中设置，尝试自动获取
        if (option0Text == null && option0Button != null)
        {
            option0Text = option0Button.GetComponentInChildren<TMP_Text>();
        }
        if (option1Text == null && option1Button != null)
        {
            option1Text = option1Button.GetComponentInChildren<TMP_Text>();
        }
        if (option2Text == null && option2Button != null)
        {
            option2Text = option2Button.GetComponentInChildren<TMP_Text>();
        }
    }
    
    /// <summary>
    /// 显示事件菜单
    /// </summary>
    public void ShowEvent(Action onComplete = null)
    {
        onEventComplete = onComplete;
        
        // 随机选择一个未使用的事件
        currentEvent = GetRandomAvailableEvent();
        if (currentEvent == null)
        {
            Debug.LogWarning("没有可用的事件，直接进入商店");
            // 如果没有可用事件，直接进入商店
            onEventComplete?.Invoke();
            return;
        }
        
        // 标记为已使用
        usedEventIdentifiers.Add(currentEvent.identifier);
        
        // 显示事件信息
        DisplayEvent();      
        
        // 显示菜单
        Show();
    }
    
    /// <summary>
    /// 根据eventType显示事件菜单
    /// </summary>
    public void ShowEventByType(string eventType, Action onComplete = null, bool isFinal = false)
    {
        onEventComplete = onComplete;
        isFinalEvent = isFinal;
        
        // 根据eventType查找匹配的事件
        currentEvent = GetEventByType(eventType);
        if (currentEvent == null)
        {
            Debug.LogWarning($"没有找到eventType为{eventType}的事件，跳过事件");
            // 如果没有找到匹配的事件，直接调用回调
            onEventComplete?.Invoke();
            return;
        }
        
        // 标记为已使用
        usedEventIdentifiers.Add(currentEvent.identifier);
        
        // 显示事件信息
        DisplayEvent();
        
        // 显示菜单
        Show();
    }
    
    /// <summary>
    /// 获取随机可用事件
    /// </summary>
    private EventInfo GetRandomAvailableEvent()
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.eventInfoMap == null)
        {
            return null;
        }
        
        // 获取所有可用且未使用的事件
        List<EventInfo> availableEvents = new List<EventInfo>();
        foreach (var eventInfo in CSVLoader.Instance.eventInfoMap.Values)
        {
            if (eventInfo.isAvailable && !usedEventIdentifiers.Contains(eventInfo.identifier))
            {
                availableEvents.Add(eventInfo);
            }
        }
        
        if (availableEvents.Count == 0)
        {
            // 如果所有事件都已使用，重置已使用列表（可选：或者返回null）
            // usedEventIdentifiers.Clear();
            return null;
        }
        
        // 随机选择一个
        int randomIndex = Random.Range(0, availableEvents.Count);
        return availableEvents[randomIndex];
    }
    
    /// <summary>
    /// 根据eventType获取匹配的事件
    /// </summary>
    private EventInfo GetEventByType(string eventType)
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.eventInfoMap == null || string.IsNullOrEmpty(eventType))
        {
            return null;
        }
        
        // 获取所有type和eventType相同且未使用的事件
        List<EventInfo> matchingEvents = new List<EventInfo>();
        List<EventInfo> matchingEventsNoUsed = new List<EventInfo>();
        
        foreach (var eventInfo in CSVLoader.Instance.eventInfoMap.Values)
        {
            if (eventInfo.isAvailable && 
                !string.IsNullOrEmpty(eventInfo.type) && 
                eventInfo.type == eventType)
            {
                if (!usedEventIdentifiers.Contains(eventInfo.identifier))
                {
                    
                    matchingEvents.Add(eventInfo);
                }
                matchingEventsNoUsed.Add(eventInfo);
            }
        }

        if (matchingEvents.Count == 0)
        {
            matchingEvents = matchingEventsNoUsed;
        }
        // 随机选择一个
        if (matchingEvents.Count > 0)
        {
            int randomIndex = Random.Range(0, matchingEvents.Count);
            return matchingEvents[randomIndex];
        }
        else
        {
            Debug.LogError($"no event ${eventType}");
        }
        
        return null;
    }
    
    /// <summary>
    /// 显示事件信息
    /// </summary>
    private void DisplayEvent()
    {
        if (currentEvent == null)
            return;
        
        // 显示事件名称和描述
        if (eventNameText != null)
        {
            eventNameText.text = currentEvent.name;
        }
        if (eventDescriptionText != null)
        {
            
            eventDescriptionText.text = currentEvent.description.Replace("\\n", "\n");
        }
        
        // 显示所有选项按钮
        DisplayOptions();
        
        // 隐藏结果信息文本
        if (resultInfoText != null)
        {
            resultInfoText.gameObject.SetActive(false);
        }
        
        // 隐藏结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 隐藏next按钮（显示事件时不显示）
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_show_event");
    }
    
    /// <summary>
    /// 显示选项按钮
    /// </summary>
    private void DisplayOptions()
    {
        // 获取所有选项
        List<string> options = new List<string>();
        if (!string.IsNullOrEmpty(currentEvent.option0))
            options.Add(currentEvent.option0);
        if (!string.IsNullOrEmpty(currentEvent.option1))
            options.Add(currentEvent.option1);
        if (!string.IsNullOrEmpty(currentEvent.option2))
            options.Add(currentEvent.option2);
        
        // 显示所有选项按钮（根据不为空的option显示）
        if (option0Button != null)
        {
            bool show = options.Count > 0;
            option0Button.gameObject.SetActive(show);
            if (show && option0Text != null)
            {
                option0Text.text = options[0];
            }
        }
        
        if (option1Button != null)
        {
            bool show = options.Count > 1;
            option1Button.gameObject.SetActive(show);
            if (show && option1Text != null)
            {
                option1Text.text = options[1];
            }
        }
        
        if (option2Button != null)
        {
            bool show = options.Count > 2;
            option2Button.gameObject.SetActive(show);
            if (show && option2Text != null)
            {
                option2Text.text = options[2];
            }
        }
    }
    
    /// <summary>
    /// 选项被选择
    /// </summary>
    private void OnOptionSelected(int optionIndex)
    {
        if (currentEvent == null)
            return;
        
        selectedOptionIndex = optionIndex;
        
        // 隐藏所有选项按钮
        if (option0Button != null) option0Button.gameObject.SetActive(false);
        if (option1Button != null) option1Button.gameObject.SetActive(false);
        if (option2Button != null) option2Button.gameObject.SetActive(false);
        
        // 获取对应的描述和结果
        string optionDesc = "";
        List<string> result = null;
        
        switch (optionIndex)
        {
            case 0:
                optionDesc = currentEvent.desc0;
                result = currentEvent.result0;
                break;
            case 1:
                optionDesc = currentEvent.desc1;
                result = currentEvent.result1;
                break;
            case 2:
                optionDesc = currentEvent.desc2;
                result = currentEvent.result2;
                break;
        }
        
        // 将事件描述替换为选项描述
        if (eventDescriptionText != null)
        {
            eventDescriptionText.text = optionDesc.Replace("\\n", "\n");
        }
        
        // 处理结果
        string resultInfo = ProcessResult(result);
        
        // 显示结果信息文本
        if (resultInfoText != null)
        {
            resultInfoText.text = resultInfo;
            resultInfoText.gameObject.SetActive(true);
        }
        
        // 显示结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
        
        // 显示next按钮（显示结果时显示）
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            // 如果是最终event，显示"The End"，否则显示"Continue"
            if (nextButtonText != null)
            {
                nextButtonText.text = isFinalEvent ? "The End" : "Continue";
            }
        }
    }
    
    /// <summary>
    /// 处理结果并返回显示信息
    /// </summary>
    private string ProcessResult(List<string> result)
    {
        if (result == null || result.Count == 0)
        {
            return "";
        }
        
        List<string> resultMessages = new List<string>();
        
        // 逐个处理result
        for (int i = 0; i < result.Count; i += 2)
        {
            if (i >= result.Count)
                break;
                
            string action = result[i];
            string value = i + 1 < result.Count ? result[i + 1] : "";
            
            if (string.IsNullOrEmpty(action))
                continue;
            
            switch (action.ToLower())
            {
                case "addgold":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int goldAmount))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddGold(goldAmount);
                            resultMessages.Add($"You get {goldAmount} gold.");
                        }
                    }
                    break;
                    
                case "heal":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int healPercent))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            int maxHealth = PlayerManager.Instance.MaxHealth;
                            int healAmount = (int)(maxHealth * healPercent / 100f);
                            PlayerManager.Instance.Heal(healAmount);
                            resultMessages.Add($"You healed {healPercent}% hp.");
                        }
                    }
                    break;
                    
                case "addtempdamage":
                    if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float tempDamagePercent))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddTempWaveDamageBonus(tempDamagePercent);
                            resultMessages.Add($"Next battle wave damage increased by {tempDamagePercent}%.");
                        }
                    }
                    break;
                    
                case "adddamage":
                    if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float damagePercent))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddPermanentWaveDamageBonus(damagePercent);
                            resultMessages.Add($"Wave damage permanently increased by {damagePercent}%.");
                        }
                    }
                    break;
                    
                case "addexchange":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int exchangeAmount))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddTempSwapCount(exchangeAmount);
                            if (exchangeAmount > 0)
                            {
                                resultMessages.Add($"You gained {exchangeAmount} swap count.");
                            }
                            else
                            {
                                resultMessages.Add($"You lost {Mathf.Abs(exchangeAmount)} swap count.");
                            }
                        }
                    }
                    break;
                    
                case "fillcolor":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int columnCount))
                    {
                        FillColorColumns(columnCount);
                        resultMessages.Add($"Leftmost {columnCount} columns filled with first color.");
                    }
                    break;
                    
                case "addmaxhp":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int maxHpAmount))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddMaxHealth(maxHpAmount);
                            if (maxHpAmount > 0)
                            {
                                resultMessages.Add($"Max HP increased by {maxHpAmount}.");
                            }
                            else
                            {
                                resultMessages.Add($"Max HP decreased by {Mathf.Abs(maxHpAmount)}.");
                            }
                        }
                    }
                    break;
                    
                case "damage":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int damagePercent2))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            int maxHealth2 = PlayerManager.Instance.MaxHealth;
                            int damageAmount = (int)(maxHealth2 * damagePercent2 / 100f);
                            PlayerManager.Instance.TakeDamage(damageAmount);
                            resultMessages.Add($"You took {damagePercent2}% damage.");
                        }
                    }
                    break;
                    
                case "addskill":
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int skillCount))
                    {
                        for (int j = 0; j < skillCount; j++)
                        {
                            string skillId = GetRandomUnownedSkill();
                            if (!string.IsNullOrEmpty(skillId))
                            {
                                if (SkillManager.Instance != null)
                                {
                                    SkillManager.Instance.UpgradeSkill(skillId);
                                    resultMessages.Add($"You gained a random skill.");
                                }
                            }
                        }
                    }
                    break;
                    
                case "damageboss":
                    if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float bossDamagePercent))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.SetBossDamageReduction(bossDamagePercent);
                            resultMessages.Add($"Next boss battle initial HP reduced by {bossDamagePercent}%.");
                        }
                    }
                    break;
                    
                case "addenemydamage":
                    if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float enemyDamagePercent))
                    {
                        if (PlayerManager.Instance != null)
                        {
                            PlayerManager.Instance.AddEnemyDamageBonus(enemyDamagePercent);
                            resultMessages.Add($"Next battle all enemy damage increased by {enemyDamagePercent}%.");
                        }
                    }
                    break;
                    
                default:
                    Debug.LogWarning($"未知的结果类型: {action}");
                    break;
            }
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
        }
        
        return string.Join("\n", resultMessages);
    }
    
    /// <summary>
    /// Next按钮点击
    /// </summary>
    private void OnNextClicked()
    {
        Hide();
        onEventComplete?.Invoke();
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
    }
    
    /// <summary>
    /// 重置已使用的事件列表（可选：用于新游戏或特定情况）
    /// </summary>
    public static void ResetUsedEvents()
    {
        usedEventIdentifiers.Clear();
    }
    
    /// <summary>
    /// 填充指定数量的列（从最左开始）为第一个颜色
    /// </summary>
    private void FillColorColumns(int columnCount)
    {
        BoardManager boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
            return;
            
        // 获取第一列第一个格子的颜色
        TileColor firstColor = TileColor.Red; // 默认红色
        for (int y = 0; y < boardManager.Height; y++)
        {
            TileCell tile = boardManager.GetTile(new Vector2Int(0, y));
            if (tile != null)
            {
                firstColor = tile.Color;
                break;
            }
        }
        
        // 填充指定数量的列
        for (int x = 0; x < columnCount && x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                TileCell tile = boardManager.GetTile(new Vector2Int(x, y));
                if (tile != null)
                {
                    tile.SetColor(firstColor);
                }
            }
        }
    }
    
    /// <summary>
    /// 获取一个随机未拥有的技能
    /// </summary>
    private string GetRandomUnownedSkill()
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.cardInfoMap == null || SkillManager.Instance == null)
            return null;
            
        List<string> unownedSkills = new List<string>();
        foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
        {
            if (skillInfo.available && !SkillManager.Instance.HasSkill(skillInfo.identifier))
            {
                unownedSkills.Add(skillInfo.identifier);
            }
        }
        
        if (unownedSkills.Count == 0)
            return null;
            
        int randomIndex = Random.Range(0, unownedSkills.Count);
        return unownedSkills[randomIndex];
    }
}

