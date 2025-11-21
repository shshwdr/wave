using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 开场动画管理器 - 管理开场动画的显示和流程
/// </summary>
public class StartAnimManager : Singleton<StartAnimManager>
{
    [Header("设置")]
    [SerializeField] private bool enable = true; // 是否启用开场动画
    
    [Header("UI组件")]
    [SerializeField] private Transform parent; // 父对象，所有搜索都在此对象下进行
    [SerializeField] private TMP_Text textComponent; // 文本组件
    [SerializeField] private CanvasGroup textCanvasGroup; // 文本的CanvasGroup（用于fade）
    [SerializeField] private Button clickButton; // 点击按钮（用于继续动画，可选）
    
    private string currentScene = null; // 当前场景名称
    
    // 当前动画状态
    private bool isInAnimation = false; // 是否正在动画中
    private StartAnimInfo currentAnimInfo = null; // 当前动画信息
    private string currentAnimIdentifier = null; // 当前动画identifier
    
    /// <summary>
    /// 是否正在动画中
    /// </summary>
    public bool IsInAnimation => isInAnimation;
    
    /// <summary>
    /// 当前动画identifier
    /// </summary>
    public string CurrentAnimIdentifier => currentAnimIdentifier;
    
    /// <summary>
    /// 当前场景名称
    /// </summary>
    public string CurrentScene => currentScene;
    
    private void Awake()
    {
        // 如果没有指定parent，使用自身作为parent
        if (parent == null)
        {
            parent = transform;
        }
        
        // 如果没有指定textCanvasGroup，尝试从textComponent获取
        if (textCanvasGroup == null && textComponent != null)
        {
            textCanvasGroup = textComponent.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = textComponent.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 初始隐藏文本
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
        }
        
        // 设置点击按钮（如果没有指定，创建一个全屏透明按钮）
        if (clickButton == null)
        {
            // 创建一个全屏透明按钮用于点击继续
            GameObject buttonObj = new GameObject("StartAnimClickButton");
            buttonObj.transform.SetParent(parent);
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            clickButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0, 0, 0, 0); // 透明背景
            
            // 确保按钮在最上层
            buttonObj.transform.SetAsLastSibling();
        }
        
        // 绑定点击事件
        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(OnClickContinue);
        }
    }

    private void Start()
    {
        // 如果不启用，直接隐藏
        if (!enable)
        {
            if (parent != null)
            {
                parent.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }
        
        StartAnimation("1");
    }

    /// <summary>
    /// 开始开场动画
    /// </summary>
    /// <param name="animIdentifier">动画identifier</param>
    public void StartAnimation(string animIdentifier)
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.startAnimInfoMap == null)
        {
            Debug.LogError("CSVLoader未初始化或startAnimInfoMap为空！");
            return;
        }
        
        if (!CSVLoader.Instance.startAnimInfoMap.ContainsKey(animIdentifier))
        {
            Debug.LogError($"找不到开场动画: {animIdentifier}");
            return;
        }
        
        StartAnimInfo animInfo = CSVLoader.Instance.startAnimInfoMap[animIdentifier];
        ShowAnimation(animInfo);
    }
    
    /// <summary>
    /// 显示动画
    /// </summary>
    private void ShowAnimation(StartAnimInfo animInfo)
    {
        currentAnimInfo = animInfo;
        currentAnimIdentifier = animInfo.identifier;
        isInAnimation = true;
        
        // 先执行actions
        ExecuteActions(animInfo.actions);
        
        // 然后更新文本并fade in
        UpdateTextAndFadeIn(animInfo.text);
        
        Debug.Log($"显示开场动画: {animInfo.identifier}");
    }
    
    /// <summary>
    /// 更新文本并淡入
    /// </summary>
    private void UpdateTextAndFadeIn(string text)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("StartAnimManager的textComponent未设置！");
            return;
        }
        
        // 先fade out当前文本（如果可见）
        if (textCanvasGroup != null && textCanvasGroup.alpha > 0)
        {
            textCanvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                // 更新文本
                textComponent.text = text ?? "";
                // fade in新文本
                textCanvasGroup.DOFade(1f, 0.5f);
            });
        }
        else
        {
            // 直接更新文本并fade in
            textComponent.text = text ?? "";
            if (textCanvasGroup != null)
            {
                textCanvasGroup.DOFade(1f, 0.5f);
            }
        }
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
        // 解析action格式：actionName|value
        string[] parts = action.Split('_');
        if (parts.Length < 2)
        {
            Debug.LogWarning($"Action格式错误，应为 actionName|value: {action}");
            return;
        }
        
        string actionName = parts[0].Trim().ToLower();
        string value = parts[1].Trim();
        
        switch (actionName)
        {
            case "showgroup":
                ShowGroup(value);
                break;
                
            case "hidegroup":
                HideGroup(value);
                break;
                
            case "showobjects":
                ShowObjects(value);
                break;
                
            default:
                Debug.LogWarning($"未知的action: {actionName}");
                break;
        }
    }
    
    /// <summary>
    /// 显示CanvasGroup（fade in）
    /// </summary>
    private void ShowGroup(string groupName)
    {
        CanvasGroup group = FindCanvasGroupInChildren(groupName);
        if (group != null)
        {
            group.alpha = 0f;
            group.DOFade(1f, 0.5f);
            currentScene = groupName; // 设置当前场景
            Debug.Log($"显示Group: {groupName}");
        }
        else
        {
            Debug.LogWarning($"找不到名为 {groupName} 的CanvasGroup");
        }
    }
    
    /// <summary>
    /// 隐藏CanvasGroup（fade out）
    /// </summary>
    private void HideGroup(string groupName)
    {
        CanvasGroup group = FindCanvasGroupInChildren(groupName);
        if (group != null)
        {
            group.DOFade(0f, 0.5f);
            Debug.Log($"隐藏Group: {groupName}");
        }
        else
        {
            Debug.LogWarning($"找不到名为 {groupName} 的CanvasGroup");
        }

        if (group.GetComponent<Button>())
        {
            group.GetComponent<Button>().enabled = false;
        }
    }
    
    /// <summary>
    /// 显示Transform下的所有Image（fade in）
    /// </summary>
    private void ShowObjects(string transformName)
    {
        Transform targetTransform = FindTransformInScene(transformName);
        if (targetTransform != null)
        {
            Image[] images = targetTransform.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                img.DOFade(1, 0.5f);
                // 确保Image有CanvasGroup
                CanvasGroup canvasGroup = img.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = img.gameObject.AddComponent<CanvasGroup>();
                }
                
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.5f);
            }
            Debug.Log($"显示Objects: {transformName} ({images.Length} 个Image)");
        }
        else
        {
            Debug.LogWarning($"找不到名为 {transformName} 的Transform");
        }
    }
    
    /// <summary>
    /// 在当前场景中查找指定名称的CanvasGroup（在parent的子对象中查找）
    /// </summary>
    private CanvasGroup FindCanvasGroupInChildren(string name)
    {
        // 在parent的子对象中查找
        if (parent == null)
            return null;
            
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = child.gameObject.AddComponent<CanvasGroup>();
                }
                return group;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 在当前场景中查找指定名称的Transform（在parent下查找）
    /// </summary>
    private Transform FindTransformInScene(string name)
    {
        if (parent == null)
            return null;
            
        // 如果当前场景已设置，优先在当前场景中查找
        if (!string.IsNullOrEmpty(currentScene))
        {
            CanvasGroup sceneGroup = FindCanvasGroupInChildren(currentScene);
            if (sceneGroup != null)
            {
                Transform found = sceneGroup.transform.Find(name);
                if (found != null)
                    return found;
            }
        }
        
        // 在parent的子对象中查找
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                return child;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 继续下一个动画
    /// </summary>
    private void ContinueAnimation()
    {
        if (currentAnimInfo == null)
        {
            EndAnimation();
            return;
        }
        
        // 获取下一个动画引用
        StartAnimInfo nextAnim = currentAnimInfo.nextInfo;
        
        if (nextAnim == null)
        {
            // 没有下一个动画，结束
            EndAnimation();
            return;
        }
        
        // 显示下一个动画
        ShowAnimation(nextAnim);
    }
    
    /// <summary>
    /// 结束动画
    /// </summary>
    private void EndAnimation()
    {
        isInAnimation = false;
        currentAnimInfo = null;
        currentAnimIdentifier = null;
        
        Debug.Log("开场动画结束");
        
        // 延迟1秒后，将整个parent设为active=false
        if (parent != null)
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                if (parent != null)
                {
                    parent.gameObject.SetActive(false);
                }
            });
        }
    }
    
    /// <summary>
    /// 点击继续（由外部调用）
    /// </summary>
    public void OnClickContinue()
    {
        if (!isInAnimation || currentAnimInfo == null)
        {
            return;
        }
        
        // 如果isEnding，结束动画
        if (currentAnimInfo.isEnding)
        {
            EndAnimation();
        }
        else
        {
            // 继续下一个动画
            ContinueAnimation();
        }
    }
}

