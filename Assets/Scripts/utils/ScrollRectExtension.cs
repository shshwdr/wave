using UnityEngine;
using UnityEngine.UI;

public static class ScrollRectExtension
{
    /// <summary>
    /// 滚动到目标子物体 RectTransform（必须是 Content 的子物体）
    /// </summary>
    /// <param name="scrollRect">ScrollRect 组件</param>
    /// <param name="target">要滚动到的元素</param>
    /// <param name="center">是否居中显示</param>
    public static void ScrollTo(this ScrollRect scrollRect, RectTransform target, bool center = false)
    {
        if (scrollRect == null || target == null || scrollRect.content == null) return;

        Canvas.ForceUpdateCanvases();

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.GetComponent<RectTransform>();

        Vector2 contentSize = content.rect.size;
        Vector2 viewportSize = viewport.rect.size;
        Vector2 localPos = content.InverseTransformPoint(target.position);

        if (scrollRect.vertical)
        {
            float elementY = localPos.y;
            float offset = center
                ? elementY - (viewportSize.y - target.rect.height) / 2f
                : elementY;
            float normalized = Mathf.Clamp01(-offset / (contentSize.y - viewportSize.y));
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }

        if (scrollRect.horizontal)
        {
            float elementX = -localPos.x;
            float offset = center
                ? elementX - (viewportSize.x - target.rect.width) / 2f
                : elementX;
            float normalized = Mathf.Clamp01(offset / (contentSize.x - viewportSize.x));
            scrollRect.horizontalNormalizedPosition = normalized;
        }
    }
}