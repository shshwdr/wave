using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 回合横幅显示组件 - 显示"Player Turn"和"Enemy Turn"
/// </summary>
public class TurnBanner : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private Image bannerImage;
    [SerializeField] private TMP_Text bannerText;
    
    [Header("动画设置")]
    [SerializeField] private float moveInDuration = 0.5f;
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private float moveOutDuration = 0.3f;
    
    private Canvas canvas;
    private float screenWidth;
    private float screenHeight;
    private Vector2 centerPosition;
    private Vector2 leftStartPosition;
    private Vector2 rightEndPosition;
    
    private void Awake()
    {
        // 获取或创建Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            else
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
        }
        
        // 初始化UI组件
        if (bannerRect == null)
        {
            bannerRect = GetComponent<RectTransform>();
        }
        
        if (bannerImage == null)
        {
            bannerImage = GetComponent<Image>();
            if (bannerImage == null)
            {
                bannerImage = gameObject.AddComponent<Image>();
                bannerImage.color = new Color(0, 0, 0, 0.8f);
            }
        }
        
        if (bannerText == null)
        {
            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            bannerText = textObj.AddComponent<TextMeshProUGUI>();
            bannerText.fontSize = 48;
            bannerText.color = Color.white;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.fontStyle = FontStyles.Bold;
            
            
        }
        
        // 设置初始状态
        gameObject.SetActive(false);
    }
    
    private void Start()
    {
        // 计算屏幕尺寸（使用Canvas的RectTransform）
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                screenWidth = canvasRect.rect.width;
                screenHeight = canvasRect.rect.height;
            }
            else
            {
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }
        }
        else
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }
        
        // 设置banner的尺寸（和屏幕一样宽，固定高度）
        bannerRect.anchorMin = new Vector2(0, 0.5f);
        bannerRect.anchorMax = new Vector2(1, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(0, 100); // 宽度自动填充，高度100像素
        bannerRect.anchoredPosition = Vector2.zero;
        
        // 初始scale.y设为0
        transform.localScale = new Vector3(1, 0, 1);
        
        // 计算位置
        centerPosition = Vector2.zero; // 屏幕中心
        leftStartPosition = new Vector2(-screenWidth, 0); // 屏幕左侧外
        rightEndPosition = new Vector2(screenWidth, 0); // 屏幕右侧外
    }
    
    /// <summary>
    /// 显示回合横幅
    /// </summary>
    /// <param name="text">显示的文本（如"Player Turn"或"Enemy Turn"）</param>
    /// <param name="onComplete">动画完成后的回调</param>
    public void ShowBanner(string text, System.Action onComplete = null)
    {
        if (bannerText != null)
        {
            bannerText.text = text;
        }
        
        gameObject.SetActive(true);
        
        // 重置状态：从左侧开始，scale.y为0
        bannerRect.anchoredPosition = leftStartPosition;
        transform.localScale = new Vector3(1, 0, 1);
        
        // 停止所有动画
        bannerRect.DOKill();
        transform.DOKill();
        
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 1. 移动到中间，同时scale.y从0到1
        sequence.Append(bannerRect.DOAnchorPos(centerPosition, moveInDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOScaleY(1f, moveInDuration).SetEase(Ease.OutQuad));
        
        // 2. 停顿
        sequence.AppendInterval(holdDuration);
        
        // 3. 迅速向右移动离开，同时scale.y回到0
        sequence.Append(bannerRect.DOAnchorPos(rightEndPosition, moveOutDuration).SetEase(Ease.InQuad));
        sequence.Join(transform.DOScaleY(0f, moveOutDuration).SetEase(Ease.InQuad));
        
        // 4. 动画完成后隐藏并调用回调
        sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
    
    private void OnDestroy()
    {
        bannerRect.DOKill();
        transform.DOKill();
    }
}

