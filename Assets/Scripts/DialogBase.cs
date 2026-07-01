using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用弹窗基类 - 一个标题，一个正文，最多两个按钮
/// </summary>
public class DialogBase : MenuBase
{
    [Header("UI组件")]
    [SerializeField] protected TMP_Text titleTMP_Text;
    [SerializeField] protected TMP_Text contentTMP_Text;
    [SerializeField] protected Button button1;
    [SerializeField] protected Button button2;
    [SerializeField] protected TMP_Text button1TMP_Text;
    [SerializeField] protected TMP_Text button2TMP_Text;

    protected Action onButton1Click;
    protected Action onButton2Click;

    protected override void Awake()
    {
        UiSortOrder.ApplySorting(transform, UiSortOrder.Popup, enableRaycast: true);
        base.Awake();
        
        // 绑定按钮事件
        if (button1 != null)
        {
            button1.onClick.RemoveAllListeners();
            button1.onClick.AddListener(() =>
            {
                onButton1Click?.Invoke();
                Hide();
            });
            button1TMP_Text = button1.GetComponentInChildren<TMP_Text>();
        }

        if (button2 != null)
        {
            button2.onClick.RemoveAllListeners();
            button2.onClick.AddListener(() =>
            {
                onButton2Click?.Invoke();
                Hide();
            });
            button2TMP_Text = button2.GetComponentInChildren<TMP_Text>();
        }
    }

    /// <summary>
    /// 设置弹窗内容
    /// </summary>
    public virtual void Setup(string title, string content, 
        string button1Label = null, Action onButton1 = null,
        string button2Label = null, Action onButton2 = null)
    {
        // 设置标题
        if (titleTMP_Text != null)
        {
            titleTMP_Text.text = title;
        }

        // 设置正文
        if (contentTMP_Text != null)
        {
            contentTMP_Text.text = content;
        }

        // 设置按钮1
        onButton1Click = onButton1;
        if (button1 != null)
        {
            button1.gameObject.SetActive(!string.IsNullOrEmpty(button1Label));
            if (button1TMP_Text != null)
            {
                button1TMP_Text.text = button1Label ?? "";
            }
        }

        // 设置按钮2
        onButton2Click = onButton2;
        if (button2 != null)
        {
            bool hasButton2 = !string.IsNullOrEmpty(button2Label);
            button2.gameObject.SetActive(hasButton2);
            if (button2TMP_Text != null)
            {
                button2TMP_Text.text = button2Label ?? "";
            }
        }
    }

    /// <summary>
    /// 显示弹窗（静态方法）
    /// </summary>
    public static void ShowDialog(string title, string content,
        string button1Label = null, Action onButton1 = null,
        string button2Label = null, Action onButton2 = null)
    {
        var dialog = FindFirstInstance<DialogBase>();
        if (dialog != null)
        {
            dialog.Setup(title, content, button1Label, onButton1, button2Label, onButton2);
            dialog.Show();
        }
        else
        {
            Debug.LogWarning("DialogBase not found in scene!");
        }
    }

    public override void Show(bool immediate = false)
    {
        if (animatedRect != null)
            ShowAnim(immediate);

        menu.SetActive(true);
        UiSortOrder.BringPopupToFront(transform);
    }
}

