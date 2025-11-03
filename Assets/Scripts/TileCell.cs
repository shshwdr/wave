using UnityEngine;
using DG.Tweening;

/// <summary>
/// 棋盘格子类
/// </summary>
public class TileCell : MonoBehaviour
{
    [Header("视觉组件")]
    public SpriteRenderer spriteRenderer;
    [Header("框显示")]
    public GameObject frameObject; // 框的GameObject（可以是一个带SpriteRenderer的子对象）
    
    private TileColor currentColor;
    private Vector2Int gridPosition;
    private bool isHighlighted = false;
    private Color originalColor;
    
    public TileColor Color => currentColor;
    public Vector2Int GridPosition => gridPosition;
    public bool IsHighlighted => isHighlighted;

    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Init(TileColor color, Vector2Int position)
    {
        currentColor = color;
        gridPosition = position;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        UpdateVisual();
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetColor(TileColor color)
    {
        currentColor = color;
        UpdateVisual();
    }

    /// <summary>
    /// 设置网格位置
    /// </summary>
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }

    /// <summary>
    /// 更新视觉表现
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            originalColor = TileColorUtil.GetUnityColor(currentColor);
            if (!isHighlighted)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 高亮显示
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        isHighlighted = highlight;
        if (spriteRenderer != null)
        {
            if (highlight)
            {
                spriteRenderer.color = originalColor*2/3; // 高亮时显示为白色
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 设置高亮颜色
    /// </summary>
    public void SetHighlightColor(Color highlightColor)
    {
        if (isHighlighted && spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }
    }

    /// <summary>
    /// 交换动画
    /// </summary>
    public Tween SwapAnimation(Vector3 targetPosition, float duration = 0.3f)
    {
        return transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 掉落动画
    /// </summary>
    public Tween FallAnimation(Vector3 targetPosition, float duration = 0.3f)
    {
        return transform.DOMove(targetPosition, duration).SetEase(Ease.OutBounce);
    }

    /// <summary>
    /// 消除动画
    /// </summary>
    public Tween DestroyAnimation(float duration = 0.2f)
    {
        return transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }

    /// <summary>
    /// 显示/隐藏框
    /// </summary>
    public void ShowFrame(bool show)
    {
        if (frameObject != null)
        {
            frameObject.SetActive(show);
        }
    }
}


