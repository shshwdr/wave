using System;
using UnityEngine;

/// <summary>
/// 确认对话框 - 用于显示确认信息
/// </summary>
public class ConfirmDialog : DialogBase
{
    public override void Init()
    {
        base.Init();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static void ShowConfirm(string title, string content, Action onYes = null, Action onNo = null)
    {
        ShowDialog(
            title: title,
            content: content,
            button1Label: "Yes",
            onButton1: onYes,
            button2Label: "No",
            onButton2: onNo
        );
    }
}

