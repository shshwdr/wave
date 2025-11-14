using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Toast UI组件 - 单个toast的显示和动画
/// </summary>
public class Toast : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private GameObject panel; // Toast面板
    [SerializeField] private TMP_Text text; // 文字组件
    
    private ToastManager toastManager;
    private float duration;
    private Tween fadeTween;
    private Tween moveTween;
    private float targetY = 0f;
    
    /// <summary>
    /// 初始化toast
    /// </summary>
    public void Init(string message, float duration, ToastManager manager)
    {
        this.duration = duration;
        this.toastManager = manager;
        
        // 设置文字
        if (text != null)
        {
            text.text = message;
        }
        else
        {
            // 如果没有指定text组件，尝试查找
            text = GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = message;
            }
        }
        
        // 如果没有指定panel，使用自身
        if (panel == null)
        {
            panel = gameObject;
        }
        
        // 初始位置（屏幕下方）
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -100f);
        }
        
        // 淡入动画
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        fadeTween = canvasGroup.DOFade(1f, 0.3f).SetEase(DG.Tweening.Ease.OutQuad);
        
        // 等待指定时间后淡出并销毁
        DOVirtual.DelayedCall(duration, () =>
        {
            FadeOut();
        });
    }
    
    /// <summary>
    /// 设置目标Y位置
    /// </summary>
    public void SetTargetPosition(float y)
    {
        targetY = y;
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && moveTween == null)
        {
            moveTween = rectTransform.DOAnchorPosY(y, 0.3f).SetEase(DG.Tweening.Ease.OutQuad);
        }
        else if (rectTransform != null)
        {
            moveTween.Kill();
            moveTween = rectTransform.DOAnchorPosY(y, 0.3f).SetEase(DG.Tweening.Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// 淡出并销毁
    /// </summary>
    private void FadeOut()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            fadeTween?.Kill();
            fadeTween = canvasGroup.DOFade(0f, 0.3f).SetEase(DG.Tweening.Ease.InQuad)
                .OnComplete(() =>
                {
                    if (toastManager != null)
                    {
                        toastManager.RemoveToast(this);
                    }
                    Destroy(gameObject);
                });
        }
        else
        {
            if (toastManager != null)
            {
                toastManager.RemoveToast(this);
            }
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        fadeTween?.Kill();
        moveTween?.Kill();
    }
}

