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
    [Header("Fog和Dirt")]
    public GameObject fogObject; // fog的GameObject
    public GameObject dirtObject; // dirt的GameObject
    
    private TileColor currentColor;
    private Vector2Int gridPosition;
    private bool isHighlighted = false;
    private Color originalColor;
    private bool hasFog = false; // 是否有fog
    private bool isDirty = false; // 是否有dirt
    
    public TileColor Color => currentColor;
    public Vector2Int GridPosition => gridPosition;
    public bool IsHighlighted => isHighlighted;
    public bool HasFog => hasFog;
    public bool IsDirty => isDirty;

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
    
    /// <summary>
    /// 设置fog状态
    /// </summary>
    public void SetFog(bool fog)
    {
        hasFog = fog;
        UpdateFogVisual();
    }
    
    /// <summary>
    /// 设置dirt状态
    /// </summary>
    public void SetDirty(bool dirty)
    {
        isDirty = dirty;
        UpdateDirtVisual();
    }
    
    /// <summary>
    /// 更新fog视觉表现
    /// </summary>
    private void UpdateFogVisual()
    {
        if (fogObject != null)
        {
            fogObject.SetActive(hasFog);
        }
        else if (hasFog)
        {
            // 如果没有fogObject，创建一个简单的fog效果
            CreateFogObject();
        }
    }
    
    /// <summary>
    /// 更新dirt视觉表现
    /// </summary>
    private void UpdateDirtVisual()
    {
        if (dirtObject != null)
        {
            dirtObject.SetActive(isDirty);
        }
        else if (isDirty)
        {
            // 如果没有dirtObject，创建一个简单的dirt效果
            CreateDirtObject();
        }
    }
    
    /// <summary>
    /// 创建fog GameObject
    /// </summary>
    private void CreateFogObject()
    {
        if (fogObject != null)
            return;
            
        GameObject fog = new GameObject("Fog");
        fog.transform.SetParent(transform);
        fog.transform.localPosition = Vector3.zero;
        fog.transform.localScale = Vector3.one;
        
        SpriteRenderer fogRenderer = fog.AddComponent<SpriteRenderer>();
        fogRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // 灰色半透明
        fogRenderer.sortingOrder = 5; // 在tile上方
        
        // 创建一个简单的矩形sprite（如果没有sprite，使用颜色填充）
        fogObject = fog;
    }
    
    /// <summary>
    /// 创建dirt GameObject
    /// </summary>
    private void CreateDirtObject()
    {
        if (dirtObject != null)
            return;
            
        GameObject dirt = new GameObject("Dirt");
        dirt.transform.SetParent(transform);
        dirt.transform.localPosition = Vector3.zero;
        dirt.transform.localScale = Vector3.one;
        
        SpriteRenderer dirtRenderer = dirt.AddComponent<SpriteRenderer>();
        dirtRenderer.color = new Color(0.4f, 0.3f, 0.2f, 0.8f); // 棕色
        dirtRenderer.sortingOrder = 5; // 在tile上方
        
        // 创建一个简单的矩形sprite（如果没有sprite，使用颜色填充）
        dirtObject = dirt;
    }
}


