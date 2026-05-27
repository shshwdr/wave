using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 关卡管理器 - 管理关卡进度和敌人生成
/// </summary>
public class LevelManager : Singleton<LevelManager>
{

    /// <summary>
    /// 获取当前关卡
    /// </summary>
    public int CurrentLevel => MainGameManager.Instance.PlayerLevel+1;

    /// <summary>
    /// 初始化关卡管理器
    /// </summary>
    public void Init()
    {
    }

    /// <summary>
    /// 获取当前关卡信息
    /// </summary>
    public LevelInfo GetCurrentLevelInfo()
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null || CSVLoader.Instance.levelInfoMap.Count == 0)
        {
            return null;
        }

        // 查找当前关卡
        if (CSVLoader.Instance.levelInfoMap.ContainsKey(CurrentLevel))
        {
            return CSVLoader.Instance.levelInfoMap[CurrentLevel];
        }

        return null;
    }

    /// <summary>
    /// 获取下一个关卡（根据玩家等级）
    /// </summary>
    public LevelInfo GetNextLevel(int playerLevel)
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null || CSVLoader.Instance.levelInfoMap.Count == 0)
        {
            return null;
        }

        // 查找所有匹配玩家等级的关卡
        List<LevelInfo> matchingLevels = new List<LevelInfo>();
        foreach (var levelInfo in CSVLoader.Instance.levelInfoMap.Values)
        {
            if (levelInfo.level == playerLevel)
            {
                matchingLevels.Add(levelInfo);
            }
        }

        // 如果找到匹配的关卡，随机选择一个
        if (matchingLevels.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, matchingLevels.Count);
            return matchingLevels[randomIndex];
        }

        // 如果没有找到匹配的关卡，选择等级最高的那个
        LevelInfo highestLevel = null;
        int highestLevelValue = -1;
        foreach (var levelInfo in CSVLoader.Instance.levelInfoMap.Values)
        {
            if (levelInfo.level > highestLevelValue)
            {
                highestLevelValue = levelInfo.level;
                highestLevel = levelInfo;
            }
        }

        return highestLevel;
    }

    /// <summary>
    /// 是否为 Boss 关卡（type == boss）
    /// </summary>
    public static bool IsBossLevel(LevelInfo levelInfo)
    {
        return levelInfo != null
            && levelInfo.type != null
            && levelInfo.type.Equals("boss", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取指定岛屿的 Boss 关卡（地图 Boss 节点用）
    /// </summary>
    public bool TryGetBossLevelForIsland(int islandId, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;

        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null)
        {
            return false;
        }

        foreach (var kvp in CSVLoader.Instance.levelInfoMap)
        {
            if (kvp.Value.island == islandId && IsBossLevel(kvp.Value))
            {
                levelInfo = kvp.Value;
                levelIndex = kvp.Key;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 按顺序获取关卡（0开始）
    /// </summary>
    public LevelInfo GetLevelByIndex(int levelIndex)
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null || CSVLoader.Instance.levelInfoMap.Count == 0)
        {
            return null;
        }

        if (CSVLoader.Instance.levelInfoMap.TryGetValue(levelIndex, out LevelInfo levelInfo))
        {
            return levelInfo;
        }

        return null;
    }

    /// <summary>
    /// 获取关卡总数
    /// </summary>
    public int GetLevelCount()
    {
        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null)
        {
            return 0;
        }

        return CSVLoader.Instance.levelInfoMap.Count;
    }

    /// <summary>
    /// 解析关卡敌人字符串
    /// 格式: identifier|数量|identifier|数量...
    /// </summary>
    public List<EnemySpawnInfo> ParseEnemies(string enemiesString)
    {
        List<EnemySpawnInfo> result = new List<EnemySpawnInfo>();
        
        if (string.IsNullOrEmpty(enemiesString))
        {
            return result;
        }

        
        string[] parts = enemiesString.Split('|');
        
        // 每两个元素为一组：identifier和数量
        for (int i = 0; i < parts.Length - 1; i += 2)
        {
            string identifier = parts[i].Trim();
            string countStr = parts[i + 1].Trim().TrimEnd(',', ' '); // 移除可能的逗号
            if (int.TryParse(countStr, out int count))
            {
                result.Add(new EnemySpawnInfo
                {
                    identifier = identifier,
                    count = count
                });
            }
            else
            {
                Debug.LogWarning($"无法解析敌人数量: {countStr}");
            }
        }

        return result;
    }

}

/// <summary>
/// 敌人生成信息
/// </summary>
public class EnemySpawnInfo
{
    public string identifier;
    public int count;
}

