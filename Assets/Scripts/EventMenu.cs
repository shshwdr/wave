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
    private static HashSet<string> usedEventIdentifiers = new HashSet<string>(); // 已使用的事件标识符

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
                    
                default:
                    Debug.LogWarning($"未知的结果类型: {action}");
                    break;
            }
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
    }
    
    /// <summary>
    /// 重置已使用的事件列表（可选：用于新游戏或特定情况）
    /// </summary>
    public static void ResetUsedEvents()
    {
        usedEventIdentifiers.Clear();
    }
}

