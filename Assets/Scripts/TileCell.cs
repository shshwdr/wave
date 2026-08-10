using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 棋盘格子类
/// </summary>
public class TileCell : MonoBehaviour
{
    [Header("视觉组件")]
    public SpriteRenderer spriteRenderer;
    [Header("框显示")]
    public GameObject frameObject; // 框的GameObject（可以是一个带SpriteRenderer的子对象）

    public SpriteRenderer note;
    public GameObject selectHighlight;
    [Header("Fog和Dirt")]
    public GameObject fogObject; // fog的GameObject
    public GameObject dirtObject; // dirt的GameObject
    [Header("Disable")]
    public GameObject disableObject; // disable的GameObject
    
    private TileColor currentColor;
    private Vector2Int gridPosition;
    private bool isHighlighted = false;
    private Color noteColor;
    private bool hasFog = false; // 是否有fog
    private bool isDirty = false; // 是否有dirt
    private bool isDisabled = false; // 是否被禁用
    private bool isBeingDestroyed = false; // 是否正在被消除
    private Tween bounceTween; // 当前的弹动动画
    private Vector3 originalWorldPosition; // 原始世界位置
    private bool isPressColorPulsing = false;
    private bool hasColorPreview = false;
    private TileColor previewColor;
    private const float PressColorPulseMinAlpha = 0.5f;
    private const float PressColorPulseMaxAlpha = 1f;
    private const float PressColorPulseDuration = 0.45f;
    private const float PressColorRestoreDuration = 0.15f;

    [Header("生成效果")]
    [SerializeField] private GameObject spawnEffect; // 生成效果对象

    
    public TileColor Color => currentColor;
    public Vector2Int GridPosition => gridPosition;
    public bool IsHighlighted => isHighlighted;
    public bool HasFog => hasFog;
    public bool IsDirty => isDirty;
    public bool IsDisabled => isDisabled;
    public bool IsBeingDestroyed => isBeingDestroyed;

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
        hasColorPreview = false;
        currentColor = color;
        UpdateVisual();
    }

    /// <summary>
    /// 仅预览变色，不改变实际颜色。
    /// </summary>
    public void SetPreviewColor(TileColor color)
    {
        hasColorPreview = true;
        previewColor = color;
        UpdateVisual();
    }

    /// <summary>
    /// 取消预览变色，恢复实际颜色。
    /// </summary>
    public void ClearPreviewColor()
    {
        if (!hasColorPreview)
            return;

        hasColorPreview = false;
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
        TileColor visualColor = hasColorPreview ? previewColor : currentColor;
        noteColor = TileColorUtil.GetBattleNoteColor(visualColor);

        if (spriteRenderer != null)
            spriteRenderer.color = noteColor;

        if (note != null)
            note.color = TileColorUtil.GetInnerBattleNoteColor(visualColor);

        if (selectHighlight != null)
        {
            Image highlightImage = selectHighlight.GetComponent<Image>();
            if (highlightImage != null)
                highlightImage.color = noteColor * 2f;

            if (!isHighlighted)
                selectHighlight.SetActive(false);
        }
    }

    /// <summary>
    /// 高亮显示
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        // 如果正在被消除，不允许清除高亮
        if (!highlight && isBeingDestroyed)
            return;
            
        isHighlighted = highlight;
        if (selectHighlight != null)
            selectHighlight.SetActive(highlight);

        if (!highlight && isPressColorPulsing)
            StopPressColorPulse();
    }
    
    /// <summary>
    /// 标记tile正在被消除
    /// </summary>
    public void MarkAsBeingDestroyed()
    {
        isBeingDestroyed = true;
        // 确保高亮是开启的
        if (!isHighlighted)
        {
            SetHighlight(true);
        }
    }

    public void StartPressColorPulse()
    {
        if (spriteRenderer == null || isPressColorPulsing)
            return;

        isPressColorPulsing = true;
        spriteRenderer.DOKill();

        Color color = noteColor;
        color.a = PressColorPulseMaxAlpha;
        spriteRenderer.color = color;

        spriteRenderer.DOFade(PressColorPulseMinAlpha, PressColorPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopPressColorPulse()
    {
        if (spriteRenderer == null || !isPressColorPulsing)
            return;

        isPressColorPulsing = false;
        spriteRenderer.DOKill();

        Color color = noteColor;
        color.a = PressColorPulseMaxAlpha;
        spriteRenderer.color = color;
        spriteRenderer.DOFade(PressColorPulseMaxAlpha, PressColorRestoreDuration)
            .SetEase(Ease.OutQuad);
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
    public Tween DestroyAnimation(float duration = 0.3f)
    {
        // 取消之前的动画
        transform.DOKill();
        
        // 处理spawnEffect：移动到外层并激活，1秒后移除
        if (spawnEffect != null)
        {
            // 保存spawnEffect的世界位置
            Vector3 spawnEffectWorldPos = spawnEffect.transform.position;
            Quaternion spawnEffectWorldRot = spawnEffect.transform.rotation;
            
            // 将spawnEffect移动到tileCell的外层（父对象）
            spawnEffect.transform.SetParent(transform.parent);
            
            // 保持世界位置和旋转不变
            spawnEffect.transform.position = spawnEffectWorldPos;
            spawnEffect.transform.rotation = spawnEffectWorldRot;
            
            // 激活spawnEffect
            spawnEffect.SetActive(true);
            
            // 1秒后移除spawnEffect
            DOVirtual.DelayedCall(1f, () =>
            {
                if (spawnEffect != null)
                {
                    Destroy(spawnEffect);
                }
            });
        }
        
        // 提高sort order，让消除的方块显示在其他方块上方
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder += 1;
        }
        
        // 创建动画序列：先放大 -> 旋转 -> 缩小消失
        Sequence destroySequence = DOTween.Sequence();
        
        // 第一步：快速放大到1.2倍（减小放大比例）
        destroySequence.Append(transform.DOScale(Vector3.one * 1.2f, 0.2f)
            .SetEase(Ease.OutQuad));
        
        // 第二步：同时旋转360度并缩小到0（0.2秒）
        destroySequence.Append(transform.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360)
            .SetEase(Ease.InBack));
        destroySequence.Join(transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack));
        
        // 如果有spriteRenderer，添加淡出效果
        if (spriteRenderer != null)
        {
            destroySequence.Join(spriteRenderer.DOFade(0f, 0.3f)
                .SetEase(Ease.InQuad));
        }
        
        return destroySequence;
    }

    /// <summary>
    /// 显示/隐藏框
    /// </summary>
    public void ShowFrame(bool show)
    {
        if (frameObject != null)
        {
            frameObject.SetActive(show);
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Waves/sfx__select_tile");
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
    
    /// <summary>
    /// 设置disable状态
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        UpdateDisableVisual();
    }
    
    /// <summary>
    /// 更新disable视觉表现
    /// </summary>
    private void UpdateDisableVisual()
    {
        if (disableObject != null)
        {
            disableObject.SetActive(isDisabled);
        }
        else if (isDisabled)
        {
            // 如果没有disableObject，创建一个简单的disable效果
            CreateDisableObject();
        }
    }
    
    /// <summary>
    /// 创建disable GameObject
    /// </summary>
    private void CreateDisableObject()
    {
        if (disableObject != null)
            return;
            
        GameObject disable = new GameObject("Disable");
        disable.transform.SetParent(transform);
        disable.transform.localPosition = Vector3.zero;
        disable.transform.localScale = Vector3.one;
        
        SpriteRenderer disableRenderer = disable.AddComponent<SpriteRenderer>();
        disableRenderer.color = new Color(1f, 0f, 0f, 0.5f); // 红色半透明
        disableRenderer.sortingOrder = 6; // 在tile和fog/dirt上方
        
        // 创建一个简单的矩形sprite（如果没有sprite，使用颜色填充）
        disableObject = disable;
    }
    
    /// <summary>
    /// 敌人踩到方块时的弹动效果：向下弹动一下然后恢复原位置
    /// </summary>
    public void BounceWhenStepped()
    {
        // 取消之前的弹动动画
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Kill();
        }
        
        // 记录当前世界位置作为原始位置
        originalWorldPosition = transform.position;
        
        // 向下弹动的距离
        float bounceDownDistance = 0.2f;
        float bounceDuration = 0.3f;
        
        // 创建弹动序列：向下 -> 向上恢复
        Sequence bounceSequence = DOTween.Sequence();
        
        // 向下弹动
        bounceSequence.Append(transform.DOMoveY(originalWorldPosition.y - bounceDownDistance, bounceDuration * 0.4f)
            .SetEase(Ease.OutQuad));
        
        // 向上恢复（带一点弹性）
        bounceSequence.Append(transform.DOMoveY(originalWorldPosition.y, bounceDuration * 0.6f)
            .SetEase(Ease.OutBounce));
        
        bounceSequence.OnComplete(() =>
        {
            // 确保最终位置准确
            transform.position = originalWorldPosition;
            bounceTween = null;
        });
        
        bounceTween = bounceSequence;
    }
    
    private void OnDestroy()
    {
        // 清理动画
        if (bounceTween != null)
        {
            bounceTween.Kill();
        }
        transform.DOKill();
    }
}


