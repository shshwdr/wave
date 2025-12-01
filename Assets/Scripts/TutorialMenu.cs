using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程菜单 - 负责显示教程UI和拦截点击
/// </summary>
public class TutorialMenu : MenuBase
{
    [Header("教程UI组件")]
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private GameObject textContainer;
    [SerializeField] private Image textBackground;
    [SerializeField] private Button tutorialButton; // 用于点击继续的按钮
    [SerializeField] private Transform normalPos; // 默认位置
    [SerializeField] private GameObject shopDrag; // 商店拖动提示GameObject
    
    private bool isBlockingInput = false; // 是否拦截输入
    private System.Action onTutorialClicked; // 教程点击回调
    private Dictionary<string, Transform> positionDict = new Dictionary<string, Transform>(); // 位置字典
    
    protected override void Awake()
    {
        base.Awake();
        
        // 如果没有指定tutorialButton，使用menu的Image作为按钮
        if (tutorialButton == null && blockImage != null)
        {
            tutorialButton = menu.GetComponent<Button>();
            if (tutorialButton == null)
            {
                tutorialButton = menu.AddComponent<Button>();
            }
            tutorialButton.targetGraphic = blockImage;
        }
        
        // 绑定点击事件
        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(OnTutorialPanelClicked);
        }
        
        // 初始化位置字典：在animatedRect下查找所有的transform
        InitializePositionDict();
        
        // 初始隐藏
        Hide(true);
    }
    
    /// <summary>
    /// 初始化位置字典：在animatedRect下查找所有的transform
    /// </summary>
    private void InitializePositionDict()
    {
        positionDict.Clear();
        
        if (animatedRect != null)
        {
            // 遍历animatedRect下的所有子对象
            for (int i = 0; i < animatedRect.childCount; i++)
            {
                Transform child = animatedRect.GetChild(i);
                if (child != null && !string.IsNullOrEmpty(child.name))
                {
                    positionDict[child.name] = child;
                }
            }
        }
        
        Debug.Log($"TutorialMenu: 初始化位置字典，共找到 {positionDict.Count} 个位置");
    }
    
    /// <summary>
    /// 显示教程
    /// </summary>
    /// <param name="text">教程文本</param>
    /// <param name="blocking">是否拦截输入</param>
    /// <param name="onClicked">点击回调</param>
    /// <param name="dialoguePosition">对话位置名称</param>
    public void ShowTutorial(string text, bool blocking, System.Action onClicked = null, string dialoguePosition = null)
    {
        isBlockingInput = blocking;
        onTutorialClicked = onClicked;
        
        // 设置文本
        if (tutorialText != null)
        {
            tutorialText.text = text ?? "";
        }
        
        // 根据dialoguePosition设置panel位置
        SetPanelPosition(dialoguePosition);
        
        // 设置背景透明度（如果拦截输入，背景更明显；否则更透明）
        if (blockImage != null)
        {
            blockImage.color = isBlockingInput 
                ? new Color(0, 0, 0, 0.5f) 
                : new Color(0, 0, 0, 0.1f);
        }
        
        // 设置按钮是否可交互（如果有wait，按钮不可点击，避免拦截）
        if (tutorialButton != null)
        {
            tutorialButton.interactable = isBlockingInput;
        }
        
        // 显示菜单
        Show(true);
    }
    
    /// <summary>
    /// 根据dialoguePosition设置panel位置
    /// </summary>
    /// <param name="dialoguePosition">对话位置名称，如果为空则使用normalPos</param>
    private void SetPanelPosition(string dialoguePosition)
    {
        if (textContainer == null)
        {
            Debug.LogWarning("TutorialMenu: textContainer为空，无法设置位置");
            return;
        }
        
        Transform targetParent = null;
        
        // 如果dialoguePosition为空，使用normalPos
        if (string.IsNullOrEmpty(dialoguePosition))
        {
            targetParent = normalPos;
        }
        else
        {
            // 从字典中查找对应名字的transform
            if (positionDict.TryGetValue(dialoguePosition, out Transform foundTransform))
            {
                targetParent = foundTransform;
            }
            else
            {
                Debug.LogWarning($"TutorialMenu: 找不到位置 '{dialoguePosition}'，使用normalPos");
                targetParent = normalPos;
            }
        }
        
        // 如果找到了目标父级，将panel放到对应的transform下，pos归零
        if (targetParent != null)
        {
            // 使用true保持本地坐标，这样设置localPosition更可靠
            textContainer.transform.SetParent(targetParent, true);
            RectTransform rectTransform = textContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localPosition = Vector3.zero;
            }
            else
            {
                textContainer.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("TutorialMenu: normalPos为空，无法设置panel位置");
        }
    }
    
    /// <summary>
    /// 隐藏教程
    /// </summary>
    public void HideTutorial()
    {
        Hide(true);
        isBlockingInput = false;
        onTutorialClicked = null;
    }
    
    /// <summary>
    /// 教程面板点击事件
    /// </summary>
    private void OnTutorialPanelClicked()
    {
        // 如果正在等待信号（不拦截输入），点击无效
        if (!isBlockingInput)
        {
            return;
        }

        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_skip_tutorial_note");
        // 触发回调
        onTutorialClicked?.Invoke();
    }
    
    /// <summary>
    /// 是否正在拦截输入
    /// </summary>
    public bool IsBlockingInput => isBlockingInput && IsActive;
    
    /// <summary>
    /// 显示商店拖动提示
    /// </summary>
    public void ShowShopDrag()
    {
        if (shopDrag != null)
        {
            shopDrag.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏商店拖动提示
    /// </summary>
    public void HideShopDrag()
    {
        if (shopDrag != null)
        {
            shopDrag.SetActive(false);
        }
    }
}

