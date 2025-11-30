using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程管理器 - 管理教程的显示和流程
/// </summary>
public class TutorialManager : Singleton<TutorialManager>
{
    [Header("教程设置")]
    [SerializeField] private bool showTutorial = true; // 是否显示教程
    
    private TutorialMenu tutorialMenu; // 教程菜单
    
    // 当前教程状态
    private bool isInTutorial = false; // 是否正在教程中
    private TutorialInfo currentTutorialInfo = null; // 当前教程信息
    private string currentTutorialIdentifier = null; // 当前教程identifier
    private string waitingForSignal = null; // 当前等待的信号（如果为null则不等待）
    
    // 教程完成标志（整个游戏只发生一次）
    private Dictionary<string, bool> tutorialCompleted = new  Dictionary<string, bool>();
    
    // Action状态管理
    private bool dragEnabled = true; // 拖动是否启用
    private bool rightClickEnabled = true; // 右键点击是否启用
    
    // 事件：当教程状态改变时
    public System.Action<bool> OnTutorialStateChanged; // 参数：是否在教程中
    
    /// <summary>
    /// 是否正在教程中
    /// </summary>
    public bool IsInTutorial => isInTutorial;
    
    /// <summary>
    /// 当前教程identifier
    /// </summary>
    public string CurrentTutorialIdentifier => currentTutorialIdentifier;
    
    /// <summary>
    /// 是否拦截输入（用于MainGameManager判断是否应该处理鼠标输入）
    /// </summary>
    public bool IsBlockingInput => tutorialMenu != null && tutorialMenu.IsBlockingInput;
    
    private void Awake()
    {
        // 查找TutorialMenu
        tutorialMenu = FindObjectOfType<TutorialMenu>();
        if (tutorialMenu == null)
        {
            Debug.LogWarning("未找到TutorialMenu，教程功能可能无法正常工作！");
        }
#if !UNITY_EDITOR
        showTutorial = true;
#endif
    }

    private void Start()
    {
        StartTutorial("start");
    }

    /// <summary>
    /// 开始教程
    /// </summary>
    /// <param name="tutorialIdentifier">教程identifier</param>
    public void StartTutorial(string tutorialIdentifier)
    {
        // 如果教程已经完成，不再进行
        if (tutorialCompleted.ContainsKey(tutorialIdentifier))
        {
            Debug.Log("教程已完成，跳过");
            return;
        }

        tutorialCompleted[tutorialIdentifier] = true;
        
        if (!showTutorial)
        {
            Debug.Log($"教程已禁用，跳过教程: {tutorialIdentifier}");
            return;
        }

        if (GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode())
        {
            return;
        }
        
        if (CSVLoader.Instance == null || CSVLoader.Instance.tutorialInfoMap == null)
        {
            Debug.LogError("CSVLoader未初始化或tutorialInfoMap为空！");
            return;
        }
        
        if (!CSVLoader.Instance.tutorialInfoMap.ContainsKey(tutorialIdentifier))
        {
            Debug.LogError($"找不到教程: {tutorialIdentifier}");
            return;
        }
        
        TutorialInfo tutorialInfo = CSVLoader.Instance.tutorialInfoMap[tutorialIdentifier];
        ShowTutorial(tutorialInfo);
    }
    
    /// <summary>
    /// 显示教程
    /// </summary>
    private void ShowTutorial(TutorialInfo tutorialInfo)
    {
        if (tutorialMenu == null)
        {
            Debug.LogError("TutorialMenu未找到，无法显示教程！");
            return;
        }
        
        currentTutorialInfo = tutorialInfo;
        currentTutorialIdentifier = tutorialInfo.identifier;
        isInTutorial = true;
        
        // 检查是否有wait信号
        waitingForSignal = string.IsNullOrEmpty(tutorialInfo.wait) ? null : tutorialInfo.wait;
        
        // 设置是否拦截输入
        // 如果有wait，不拦截；否则拦截
        bool blocking = (waitingForSignal == null);
        
        // 通过TutorialMenu显示教程
        tutorialMenu.ShowTutorial(
            tutorialInfo.text ?? "",
            blocking,
            OnTutorialPanelClicked,
            tutorialInfo.dialoguePosition
        );
        
        // 触发事件
        OnTutorialStateChanged?.Invoke(true);
        
        // 执行action
        ExecuteActions(tutorialInfo.actions);
        
        Debug.Log($"显示教程: {tutorialInfo.identifier}, wait: {waitingForSignal}, blocking: {blocking}");
    }
    
    /// <summary>
    /// 执行actions列表
    /// </summary>
    private void ExecuteActions(List<string> actions)
    {
        if (actions == null || actions.Count == 0)
            return;
        
        foreach (string action in actions)
        {
            if (string.IsNullOrEmpty(action))
                continue;
            
            ExecuteAction(action.Trim());
        }
    }
    
    /// <summary>
    /// 执行单个action
    /// </summary>
    private void ExecuteAction(string action)
    {
        switch (action.ToLower())
        {
            case "disabledrag":
                dragEnabled = false;
                Debug.Log("教程：禁用拖动");
                break;
                
            case "enabledrag":
                dragEnabled = true;
                Debug.Log("教程：启用拖动");
                break;
                
            case "disableshopclose":
                SetShopCloseButtonEnabled(false);
                Debug.Log("教程：隐藏商店关闭按钮");
                break;
                
            case "enableshopclose":
                SetShopCloseButtonEnabled(true);
                Debug.Log("教程：显示商店关闭按钮");
                break;
                
            case "disablerefresh":
                SetShopRefreshButtonEnabled(false);
                Debug.Log("教程：隐藏商店刷新按钮");
                break;
                
            case "enablerefresh":
                SetShopRefreshButtonEnabled(true);
                Debug.Log("教程：显示商店刷新按钮");
                break;
                
            case "disablerightclick":
                rightClickEnabled = false;
                Debug.Log("教程：禁用右键点击");
                break;
                
            case "enablerightclick":
                rightClickEnabled = true;
                Debug.Log("教程：启用右键点击");
                break;
                
            default:
                Debug.LogWarning($"未知的action: {action}");
                break;
        }
    }
    
    /// <summary>
    /// 设置商店关闭按钮的启用状态
    /// </summary>
    private void SetShopCloseButtonEnabled(bool enabled)
    {
        SkillSelectMenu shopMenu = FindObjectOfType<SkillSelectMenu>();
        if (shopMenu != null)
        {
            shopMenu.SetCloseButtonEnabled(enabled);
        }
    }
    
    /// <summary>
    /// 设置商店刷新按钮的启用状态
    /// </summary>
    private void SetShopRefreshButtonEnabled(bool enabled)
    {
        SkillSelectMenu shopMenu = FindObjectOfType<SkillSelectMenu>();
        if (shopMenu != null)
        {
            shopMenu.SetRefreshButtonEnabled(enabled);
        }
    }
    
    /// <summary>
    /// 是否允许拖动（用于MainGameManager判断）
    /// </summary>
    public bool IsDragEnabled => dragEnabled;
    
    /// <summary>
    /// 是否允许右键点击（用于MainGameManager判断）
    /// </summary>
    public bool IsRightClickEnabled => rightClickEnabled;
    
    /// <summary>
    /// 发送信号（用于继续教程）
    /// </summary>
    /// <param name="signal">信号名称</param>
    public void SendSignal(string signal)
    {
        // 检查是否在教程中
        if (!isInTutorial || currentTutorialInfo == null)
        {
            return;
        }
        
        // 检查是否正在等待这个信号
        if (waitingForSignal != null && waitingForSignal == signal)
        {
            Debug.Log($"收到信号: {signal}，继续教程");
            waitingForSignal = null;
            
            // 继续到下一个教程
            ContinueTutorial();
        }
    }
    
    /// <summary>
    /// 教程面板点击事件（由TutorialMenu调用）
    /// </summary>
    private void OnTutorialPanelClicked()
    {
        if (!isInTutorial || currentTutorialInfo == null)
        {
            return;
        }
        
        // 如果正在等待信号，点击无效（这个检查在TutorialMenu中已经做了，但这里再检查一次确保安全）
        if (waitingForSignal != null)
        {
            return;
        }
        
        // 如果isEnding，结束教程
        if (currentTutorialInfo.isEnding)
        {
            EndTutorial();
        }
        else
        {
            // 继续下一个教程
            ContinueTutorial();
        }
    }
    
    /// <summary>
    /// 继续下一个教程
    /// </summary>
    private void ContinueTutorial()
    {
        if (currentTutorialInfo == null)
        {
            EndTutorial();
            return;
        }
        
        // 获取下一个教程引用
        TutorialInfo nextTutorial = currentTutorialInfo.nextInfo;
        
        if (nextTutorial == null)
        {
            // 没有下一个教程，结束
            EndTutorial();
            return;
        }
        
        // 显示下一个教程
        ShowTutorial(nextTutorial);
    }
    
    /// <summary>
    /// 结束教程
    /// </summary>
    private void EndTutorial()
    {
        isInTutorial = false;
        currentTutorialInfo = null;
        currentTutorialIdentifier = null;
        waitingForSignal = null;
        
        // 恢复所有action状态
        dragEnabled = true;
        rightClickEnabled = true;
        SetShopCloseButtonEnabled(true);
        SetShopRefreshButtonEnabled(true);
        
        
        // 隐藏教程菜单
        if (tutorialMenu != null)
        {
            tutorialMenu.HideTutorial();
        }
        
        // 触发事件
        OnTutorialStateChanged?.Invoke(false);
        
        Debug.Log("教程结束");
    }
    
    /// <summary>
    /// 设置是否显示教程
    /// </summary>
    public void SetShowTutorial(bool show)
    {
        showTutorial = show;
        if (!show && isInTutorial)
        {
            EndTutorial();
        }
    }
}

