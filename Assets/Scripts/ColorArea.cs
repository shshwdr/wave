using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 颜色区域数据类 - 包含按钮、slot文本和slot容器
/// </summary>
[System.Serializable]
public class ColorArea: MonoBehaviour
{
    public Button button;        // 每个颜色区域的详情按钮
    public TMP_Text slotText;    // 显示slot数量（如"3/4"）
    public Transform slotParent; // 每个颜色区域的slot容器
    public Image colorImage;     // 显示wave颜色的图片
}

