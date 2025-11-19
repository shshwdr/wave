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
    
    private bool isBlockingInput = false; // 是否拦截输入
    private System.Action onTutorialClicked; // 教程点击回调
    
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
        
        // 初始隐藏
        Hide(true);
    }
    
    /// <summary>
    /// 显示教程
    /// </summary>
    /// <param name="text">教程文本</param>
    /// <param name="blocking">是否拦截输入</param>
    /// <param name="onClicked">点击回调</param>
    public void ShowTutorial(string text, bool blocking, System.Action onClicked = null)
    {
        isBlockingInput = blocking;
        onTutorialClicked = onClicked;
        
        // 设置文本
        if (tutorialText != null)
        {
            tutorialText.text = text ?? "";
        }
        
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
        
        // 触发回调
        onTutorialClicked?.Invoke();
    }
    
    /// <summary>
    /// 是否正在拦截输入
    /// </summary>
    public bool IsBlockingInput => isBlockingInput && IsActive;
}

