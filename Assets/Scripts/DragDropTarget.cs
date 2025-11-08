using UnityEngine;

/// <summary>
/// 拖拽目标组件 - 标记可以放置拖拽物体的区域
/// </summary>
public class DragDropTarget : MonoBehaviour
{
    [Header("目标类型")]
    public TargetType targetType = TargetType.ColorArea;
    public int colorIndex = -1; // 如果是颜色区域，指定颜色索引（0-3）

    public enum TargetType
    {
        ColorArea,  // 颜色区域
        Backpack    // 背包
    }
}

