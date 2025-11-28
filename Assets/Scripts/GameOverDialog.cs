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
        // 检查是否在困难模式
        bool isInHardMode = GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode();
        
        if (isInHardMode)
        {
            // 困难模式：显示"Back to Main Menu"而不是"Retry This Level"
            ShowDialog(
                title: "Game Over",
                content: "The monsters have declared today 'Pitch Freedom Day.'",
                button1Label: "Back to Main Menu",
                onButton1: () =>
                {
                    // 加载Start场景
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
                },
                button2Label: "Restart",
                onButton2: onRestart
            );
        }
        else
        {
            // 普通模式：显示"Retry This Level"
            ShowDialog(
                title: "Game Over",
                content: "The monsters have declared today 'Pitch Freedom Day.'",
                button1Label: "Retry This Level",
                onButton1: onRetryLevel,
                button2Label: "Restart",
                onButton2: onRestart
            );
        }
    }
}

