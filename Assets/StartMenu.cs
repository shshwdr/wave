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
    [SerializeField] private Button hardModeButton; // 困难模式按钮
    [SerializeField] private GameObject hardModeDisableImage; // 困难模式按钮
    
    [SerializeField] private TMPro.TMP_Text startButtonText; // 开始按钮文本（用于修改文字）
    
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
            
            // 如果没有指定startButtonText，尝试从按钮获取
            if (startButtonText == null)
            {
                startButtonText = startButton.GetComponentInChildren<TMPro.TMP_Text>();
            }
        }
        else
        {
            Debug.LogWarning("StartMenu: startButton未设置！");
        }
        
        // 设置困难模式按钮点击事件
        if (hardModeButton != null)
        {
            hardModeButton.onClick.RemoveAllListeners();
            hardModeButton.onClick.AddListener(OnHardModeButtonClicked);
        }
        
        // 更新按钮显示
        UpdateButtonDisplay();
    }
    
    /// <summary>
    /// 更新按钮显示
    /// </summary>
    private void UpdateButtonDisplay()
    {
        // 检查是否已赢得游戏
        bool hasWonGame = GameDataManager.Instance != null && GameDataManager.Instance.HasWonGame();
        
        // 如果已赢得游戏，显示困难模式按钮，并将开始按钮文字改为"Normal Mode"
        if (hasWonGame)
        {
            if (hardModeButton != null)
            {
                hardModeDisableImage.SetActive(false);
                hardModeButton.enabled=true;
            }
            
            if (startButtonText != null)
            {
                startButtonText.text = "Normal Mode";
            }
        }
        else
        {
            if (hardModeButton != null)
            {
                hardModeDisableImage.SetActive(true);
                hardModeButton.enabled=false;
            }
            
            if (startButtonText != null)
            {
                startButtonText.text = "Start";
            }
        }
    }
    
    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 设置普通模式
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetIsInHardMode(false);
        }
        
        // 加载Game场景
        SceneManager.LoadScene(gameSceneName);
    }

    IEnumerator onclick()
    {
        yield return new WaitForSeconds(0.3f);
        
    }
    
    /// <summary>
    /// 困难模式按钮点击事件
    /// </summary>
    private void OnHardModeButtonClicked()
    {
        
        // 设置困难模式
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetIsInHardMode(true);
        }
        
        // 加载Game场景
        SceneManager.LoadScene(gameSceneName);
    }
}
