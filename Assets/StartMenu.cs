using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class StartMenu : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private CanvasGroup groupCanvas; // 整个group的CanvasGroup（用于fade in）
    [SerializeField] private Button startButton; // 开始游戏按钮
    
    [Header("设置")]
    [SerializeField] private float fadeInDuration = 0.5f; // fade in持续时间
    [SerializeField] private string gameSceneName = "Game"; // 游戏场景名称
    
    void Start()
    {
        // 如果没有指定groupCanvas，尝试从自身获取
        if (groupCanvas == null)
        {
            groupCanvas = GetComponent<CanvasGroup>();
            if (groupCanvas == null)
            {
                // 如果还没有，创建一个
                groupCanvas = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 初始设置为透明
        if (groupCanvas != null)
        {
            groupCanvas.alpha = 0f;
            // 执行fade in动画
            groupCanvas.DOFade(1f, fadeInDuration);
        }
        
        // 设置按钮点击事件
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogWarning("StartMenu: startButton未设置！");
        }
    }
    
    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 加载Game场景
        SceneManager.LoadScene(gameSceneName);
    }
}
