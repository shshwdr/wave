using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 技能图标UI组件 - 支持拖拽和悬停显示详情
/// </summary>
public class SkillIconUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image noteImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image highlightImage; // 高亮图片

    private string skillIdentifier;
    private int colorIndex = -1; // -1表示在背包中，0-3表示在哪个颜色区域
    private SkillSelectMenu parentMenu;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isHighlighting = false;

    public string SkillIdentifier => skillIdentifier;
    public int ColorIndex => colorIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 如果没有高亮图片，创建一个
        if (highlightImage == null)
        {
            GameObject highlightObj = new GameObject("HighlightImage");
            highlightObj.transform.SetParent(transform);
            RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.sizeDelta = Vector2.zero;
            highlightRect.anchoredPosition = Vector2.zero;
            
            highlightImage = highlightObj.AddComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 0f, 0f); // 黄色，初始透明
            highlightImage.raycastTarget = false;
        }
    }

    /// <summary>
    /// 初始化技能图标
    /// </summary>
    public void Init(string identifier, int colorIdx, SkillSelectMenu menu)
    {
        skillIdentifier = identifier;
        colorIndex = colorIdx;
        parentMenu = menu;

        // 更新显示
        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.cardInfoMap.ContainsKey(skillIdentifier))
            return;

        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[skillIdentifier];
        
        // 更新标题（显示当前等级）
        if (titleText != null)
        {
            titleText.text = SkillManager.Instance.GetSkillName(skillIdentifier, false);
        }

        // 设置iconImage的颜色
        if (iconImage != null)
        {
            // 根据skill实际分配到的颜色区域（colorIndex）来判断颜色
            // colorIndex = -1 表示在背包中（不属于任何颜色）
            // colorIndex = 0-3 表示在哪个颜色区域（0=红，1=黄，2=蓝，3=绿）
            if (colorIndex < 0 || colorIndex >= 4)
            {
                iconImage.color = TileColorUtil.GetDefaultColor();
            }
            else
            {
                // 根据colorIndex获取对应的颜色
                TileColor tileColor = (TileColor)colorIndex;
                Color colorValue = TileColorUtil.GetShopSkillColor(tileColor);
                iconImage.color = colorValue;
            }

            // 计算noteImage的颜色：取iconImage中最大的channel翻倍，其他两个channel/2
            noteImage.color = CalculateDarkerColor(iconImage.color);
        }

        // TODO: 更新图标图片（如果有）
        // if (iconImage != null && skillInfo.icon != null)
        // {
        //     iconImage.sprite = skillInfo.icon;
        // }
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 只有在SkillSelectMenu中才允许拖拽
        if (parentMenu != null)
        {
            parentMenu.StartDragSkill(this);
        }
        else
        {
            // 如果在StatisticsMenu中，不允许拖拽，直接返回
            return;
        }

        // 设置透明度
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        // 设置为顶层
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // 只有在SkillSelectMenu中才允许拖拽
        if (parentMenu == null)
        {
            // 如果在StatisticsMenu中，不允许拖拽，直接返回
            return;
        }
        
        if (rectTransform == null || canvas == null)
            return;

        // 如果是原始图标，通知SkillSelectMenu更新临时图标位置
        if (parentMenu != null)
        {
            parentMenu.UpdateDragPosition(eventData);
            return;
        }

        // 临时图标，跟随鼠标移动
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint);

        rectTransform.position = canvas.transform.TransformPoint(localPoint);
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 只有在SkillSelectMenu中才允许拖拽
        if (parentMenu == null)
        {
            // 如果在StatisticsMenu中，不允许拖拽，直接返回
            // 检查是否是临时拖拽图标（在SkillSelectMenu中）
            SkillSelectMenu menu = GetComponentInParent<SkillSelectMenu>();
            if (menu != null)
            {
                // 临时图标，需要通知SkillSelectMenu处理
                menu.EndDragSkill(eventData);
            }
            return;
        }

        // 原始图标，恢复透明度
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // 通知SkillSelectMenu处理拖拽结束
        if (parentMenu != null)
        {
            parentMenu.EndDragSkill(eventData);
        }
    }

    /// <summary>
    /// 鼠标悬停
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentMenu != null)
        {
            parentMenu.ShowSkillDetail(skillIdentifier, colorIndex);
        }
        else
        {
            // 如果没有parentMenu（在StatisticsMenu中），尝试找到StatisticsMenu
            StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
            if (statisticsMenu != null)
            {
                statisticsMenu.ShowSkillDetail(skillIdentifier);
            }
        }
    }

    /// <summary>
    /// 鼠标离开
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentMenu != null)
        {
            parentMenu.HideSkillDetail();
        }
        else
        {
            // 如果没有parentMenu（在StatisticsMenu中），尝试找到StatisticsMenu
            StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
            if (statisticsMenu != null)
            {
                statisticsMenu.HideSkillDetail();
            }
        }
    }

    /// <summary>
    /// 获取颜色区域的slot容器
    /// </summary>
    private Transform GetColorSlotParent(int colorIndex)
    {
        if (parentMenu == null)
            return null;

        if (colorIndex >= 0 && colorIndex < parentMenu.colorArea.Length && parentMenu.colorArea[colorIndex] != null)
        {
            return parentMenu.colorArea[colorIndex].slotParent;
        }

        return null;
    }

    /// <summary>
    /// 获取背包容器
    /// </summary>
    private Transform GetBackpackParent()
    {
        if (parentMenu == null)
            return null;

        return parentMenu.backpackParent;
    }

    /// <summary>
    /// 计算slot索引
    /// </summary>
    private int GetSlotIndex(Transform target, Transform parent)
    {
        // 如果目标就是parent，返回-1（添加到末尾）
        if (target == parent)
            return -1;

        // 查找目标在parent中的位置
        int index = 0;
        foreach (Transform child in parent)
        {
            if (child == target)
                return index;
            index++;
        }

        return -1;
    }

    /// <summary>
    /// 计算更深的颜色：取最大channel翻倍，其他两个channel/2
    /// </summary>
    private Color CalculateDarkerColor(Color originalColor)
    {
        float r = originalColor.r;
        float g = originalColor.g;
        float b = originalColor.b;
        
        // 找到最大的channel
        float maxChannel = Mathf.Max(r, Mathf.Max(g, b));
        var v = 1.3f;
        // 确定哪个是最大channel
        Color darkerColor = new Color();
        if (maxChannel == r)
        {
            // R是最大的，翻倍R，G和B除以2
            darkerColor.r = Mathf.Clamp01(r * v);
            darkerColor.g = g / v;
            darkerColor.b = b / v;
        }
        else if (maxChannel == g)
        {
            // G是最大的，翻倍G，R和B除以2
            darkerColor.r = r / v;
            darkerColor.g = Mathf.Clamp01(g * v);
            darkerColor.b = b / v;
        }
        else
        {
            // B是最大的，翻倍B，R和G除以2
            darkerColor.r = r / v;
            darkerColor.g = g / v;
            darkerColor.b = Mathf.Clamp01(b * v);
        }
        
        darkerColor.a = originalColor.a;
        return darkerColor;
    }
    
    /// <summary>
    /// 开始高亮动画（fade in and out）
    /// </summary>
    public void StartHighlight()
    {
        if (isHighlighting || highlightImage == null)
            return;

        isHighlighting = true;
        StartCoroutine(HighlightAnimation());
    }

    /// <summary>
    /// 高亮动画协程
    /// </summary>
    private System.Collections.IEnumerator HighlightAnimation()
    {
        float duration = 0.5f; // 每次fade的持续时间
        int cycles = 3; // 闪烁次数

        for (int i = 0; i < cycles; i++)
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.8f, elapsed / duration);
                Color color = highlightImage.color;
                color.a = alpha;
                highlightImage.color = color;
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.8f, 0f, elapsed / duration);
                Color color = highlightImage.color;
                color.a = alpha;
                highlightImage.color = color;
                yield return null;
            }
        }

        // 确保最后是透明的
        Color finalColor = highlightImage.color;
        finalColor.a = 0f;
        highlightImage.color = finalColor;

        isHighlighting = false;
    }
}

