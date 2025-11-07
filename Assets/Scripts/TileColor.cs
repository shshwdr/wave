using UnityEngine;

/// <summary>
/// 格子颜色枚举
/// </summary>
public enum TileColor
{
    Red = 0,
    Yellow = 1,
    Blue = 2,
    Green = 3
}

/// <summary>
/// 格子颜色工具类
/// </summary>
public static class TileColorUtil
{
    public static Color HexToColor(string hex)
    {
        Color res;
        ColorUtility.TryParseHtmlString(hex, out res);
        return res;
    }
    private static readonly Color[] Colors = new Color[]
    {
        HexToColor("#FFC8E2"),//red
        HexToColor("#7CACF6"),
        HexToColor("#B5E6F5"),
        HexToColor("#BEBFF5")//purple
        // Color.red,      // Red
        // Color.yellow,   // Yellow
        // Color.blue,      // Blue
        // Color.green     // Green
    };

    /// <summary>
    /// 获取颜色对应的Unity Color
    /// </summary>
    public static Color GetUnityColor(TileColor color)
    {
        return Colors[(int)color];
    }

    /// <summary>
    /// 随机获取一个颜色
    /// </summary>
    public static TileColor GetRandomColor()
    {
        return (TileColor)Random.Range(0, System.Enum.GetValues(typeof(TileColor)).Length);
    }
}


