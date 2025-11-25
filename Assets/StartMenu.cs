using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using FMODUnity;

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
        // 初始化FMOD - 通过访问RuntimeManager来触发初始化
        // 这对于Web平台很重要，需要提前初始化FMOD系统
        try
        {
            // 访问RuntimeManager会触发FMOD的懒加载初始化
            var _ = RuntimeManager.StudioSystem;
            Debug.Log("[StartMenu] FMOD initialized");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[StartMenu] FMOD initialization warning: {e.Message}");
        }
        
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
        // // 播放按钮点击音效 - 这对于Web平台很重要，必须在用户交互时触发音频来解锁浏览器音频权限
        // // 如果按钮有自动音效，这里也会触发；如果没有，可以播放一个通用的UI音效
        // try
        // {
        //     // 尝试播放按钮点击音效（如果存在）
        //     RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
        // }
        // catch (System.Exception e)
        // {
        //     // 如果音效不存在，至少触发一次FMOD操作来解锁浏览器音频权限
        //     Debug.LogWarning($"[StartMenu] Button sound not found, but FMOD operation triggered: {e.Message}");
        // }
        
        // 加载Game场景
        SceneManager.LoadScene(gameSceneName);
    }
}
