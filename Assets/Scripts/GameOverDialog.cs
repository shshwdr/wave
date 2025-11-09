using System;
using UnityEngine;

/// <summary>
/// 游戏结束弹窗
/// </summary>
public class GameOverDialog : DialogBase
{
    public override void Init()
    {
        base.Init();
    }

    /// <summary>
    /// 显示游戏结束弹窗
    /// </summary>
    public static void ShowGameOver(Action onRetryLevel = null, Action onRestart = null, Action onQuit = null)
    {
        ShowDialog(
            title: "Game Over",
            content: "enmey eat your brain, try better next time",
            button1Label: "Retry This Level",
            onButton1: onRetryLevel,
            button2Label: "Restart",
            onButton2: onRestart
        );
    }
}

