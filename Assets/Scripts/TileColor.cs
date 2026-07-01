using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
/// 游戏全局配置数据（颜色等）
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "Wave/GameData")]
public class GameData : ScriptableObject
{
    [Header("Shop Skill Colors")]
    [FormerlySerializedAs("tileColors")]
    [SerializeField] private List<Color> shopSkillColors = new List<Color>
    {
        new Color(1f, 0.78431374f, 0.8862745f, 1f),       // Red  #FFC8E2
        new Color(0.4862745f, 0.6745098f, 0.9647059f, 1f), // Yellow #7CACF6
        new Color(0.70980394f, 0.9019608f, 0.9607843f, 1f), // Blue  #B5E6F5
        new Color(0.74509805f, 0.7490196f, 0.9607843f, 1f), // Green #BEBFF5
    };

    [Header("Battle Note Colors")]
    [SerializeField] private List<Color> battleNoteColors = new List<Color>
    {
        new Color(1f, 0.78431374f, 0.8862745f, 1f),       // Red  #FFC8E2
        new Color(0.4862745f, 0.6745098f, 0.9647059f, 1f), // Yellow #7CACF6
        new Color(0.70980394f, 0.9019608f, 0.9607843f, 1f), // Blue  #B5E6F5
        new Color(0.74509805f, 0.7490196f, 0.9607843f, 1f), // Green #BEBFF5
    };

    [Header("Battle Note Outer Colors")]
    [SerializeField] private List<Color> battleNoteOuterColors = new List<Color>
    {
        new Color(1f, 0.78431374f, 0.8862745f, 1f),       // Red  #FFC8E2
        new Color(0.4862745f, 0.6745098f, 0.9647059f, 1f), // Yellow #7CACF6
        new Color(0.70980394f, 0.9019608f, 0.9607843f, 1f), // Blue  #B5E6F5
        new Color(0.74509805f, 0.7490196f, 0.9607843f, 1f), // Green #BEBFF5
    };

    [SerializeField] private Color defaultColor = new Color(1f, 0.9411765f, 0.654902f, 1f); // #FFF0A7
    [SerializeField] private Color unaffordableTextColor = new Color(0.99215686f, 0.5529412f, 0.45490196f, 1f); // #FD8D74

    [Header("Map Rewards")]
    [Range(0f, 1f)]
    [SerializeField] private float consumableDropChance = 0.5f;

    private static GameData instance;

    public static GameData Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = Resources.Load<GameData>("GameData");
            if (instance == null)
            {
                instance = CreateInstance<GameData>();
                Debug.LogWarning("GameData asset not found in Resources/GameData. Using runtime defaults.");
            }

            return instance;
        }
    }

    public Color DefaultColor => defaultColor;
    public Color UnaffordableTextColor => unaffordableTextColor;
    public float ConsumableDropChance => consumableDropChance;

    public Color GetShopSkillColor(int colorIndex)
    {
        return GetColorFromList(shopSkillColors, colorIndex);
    }

    public Color GetBattleNoteColor(int colorIndex)
    {
        return GetColorFromList(battleNoteColors, colorIndex);
    }

    public Color GetBattleNoteOuterColor(int colorIndex)
    {
        return GetColorFromList(battleNoteOuterColors, colorIndex);
    }

    private Color GetColorFromList(List<Color> colors, int colorIndex)
    {
        if (colors == null || colorIndex < 0 || colorIndex >= colors.Count)
            return defaultColor;

        return colors[colorIndex];
    }
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

    public static Color GetShopSkillColor(TileColor color)
    {
        return GameData.Instance.GetShopSkillColor((int)color);
    }

    public static Color GetBattleNoteColor(TileColor color)
    {
        return GameData.Instance.GetBattleNoteColor((int)color);
    }

    public static Color GetBattleNoteOuterColor(TileColor color)
    {
        return GameData.Instance.GetBattleNoteOuterColor((int)color);
    }

    /// <summary>
    /// 获取无颜色区域（背包等）使用的默认颜色
    /// </summary>
    public static Color GetDefaultColor()
    {
        return GameData.Instance.DefaultColor;
    }

    /// <summary>
    /// 获取商店金币不足时的文本颜色
    /// </summary>
    public static Color GetUnaffordableTextColor()
    {
        return GameData.Instance.UnaffordableTextColor;
    }

    /// <summary>
    /// 获取战斗音符内层颜色（向白色偏移）
    /// </summary>
    public static Color GetInnerBattleNoteColor(TileColor color, float whiteBlend = 0.5f)
    {
        return Color.Lerp(GetBattleNoteColor(color), Color.white, whiteBlend);
    }
}
