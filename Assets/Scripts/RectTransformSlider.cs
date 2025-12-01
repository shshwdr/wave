using UnityEngine;
using DG.Tweening;

/// <summary>
/// RectTransform滑动器 - 让一个RectTransform在两个RectTransform之间滑动
/// </summary>
public class RectTransformSlider : MonoBehaviour
{
    [Header("滑动设置")]
    [SerializeField] private RectTransform targetRect; // 要滑动的RectTransform
    [SerializeField] private RectTransform startRect; // 起始位置RectTransform
    [SerializeField] private RectTransform endRect; // 结束位置RectTransform
    
    [Header("动画设置")]
    [SerializeField] private float duration = 1f; // 滑动持续时间
    [SerializeField] private Ease easeType = Ease.InOutQuad; // 缓动类型
    [SerializeField] private bool loop = false; // 是否循环
    [SerializeField] private LoopType loopType = LoopType.Yoyo; // 循环类型
    [SerializeField] private bool autoStart = false; // 是否自动开始
    
    private Tween currentTween; // 当前动画
    
    private void Awake()
    {
        // 如果没有指定targetRect，使用当前GameObject的RectTransform
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }
    }
    
    private void Start()
    {
        if (autoStart)
        {
            StartSlide();
        }
    }
    
    /// <summary>
    /// 开始滑动
    /// </summary>
    public void StartSlide()
    {
        if (targetRect == null || startRect == null || endRect == null)
        {
            Debug.LogError("RectTransformSlider: targetRect、startRect或endRect为空！");
            return;
        }
        
        // 停止之前的动画
        StopSlide();
        
        // 设置初始位置
        SetPositionToStart();
        
        // 获取目标位置（世界坐标）
        Vector3 startPos = GetWorldPosition(startRect);
        Vector3 endPos = GetWorldPosition(endRect);
        
        // 创建滑动动画
        if (loop)
        {
            // 循环模式：使用Yoyo循环
            currentTween = targetRect.DOMove(endPos, duration)
                .SetEase(easeType)
                .SetLoops(-1, loopType);
        }
        else
        {
            // 单次模式：从start到end
            currentTween = targetRect.DOMove(endPos, duration)
                .SetEase(easeType);
        }
    }
    
    /// <summary>
    /// 停止滑动
    /// </summary>
    public void StopSlide()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
    }
    
    /// <summary>
    /// 设置位置到起始位置
    /// </summary>
    public void SetPositionToStart()
    {
        if (targetRect == null || startRect == null)
        {
            Debug.LogError("RectTransformSlider: targetRect或startRect为空！");
            return;
        }
        
        Vector3 startPos = GetWorldPosition(startRect);
        targetRect.position = startPos;
    }
    
    /// <summary>
    /// 设置位置到结束位置
    /// </summary>
    public void SetPositionToEnd()
    {
        if (targetRect == null || endRect == null)
        {
            Debug.LogError("RectTransformSlider: targetRect或endRect为空！");
            return;
        }
        
        Vector3 endPos = GetWorldPosition(endRect);
        targetRect.position = endPos;
    }
    
    /// <summary>
    /// 获取RectTransform的世界坐标位置
    /// </summary>
    private Vector3 GetWorldPosition(RectTransform rect)
    {
        if (rect == null)
            return Vector3.zero;
        
        // 对于UI元素，使用position属性即可
        return rect.position;
    }
    
    /// <summary>
    /// 设置滑动持续时间
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }
    
    /// <summary>
    /// 设置缓动类型
    /// </summary>
    public void SetEaseType(Ease newEaseType)
    {
        easeType = newEaseType;
    }
    
    /// <summary>
    /// 设置是否循环
    /// </summary>
    public void SetLoop(bool newLoop)
    {
        loop = newLoop;
    }
    
    private void OnDestroy()
    {
        // 清理动画
        StopSlide();
    }
    
    private void OnDisable()
    {
        // 禁用时停止动画
        StopSlide();
    }
}

