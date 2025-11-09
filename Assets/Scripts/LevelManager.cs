using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 关卡管理器 - 管理关卡进度和敌人生成
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    private int currentLevel = 1;

    /// <summary>
    /// 获取当前关卡
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// 初始化关卡管理器
    /// </summary>
    public void Init()
    {
        currentLevel = 1;
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
        if (CSVLoader.Instance.levelInfoMap.ContainsKey(currentLevel))
        {
            return CSVLoader.Instance.levelInfoMap[currentLevel];
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
            int randomIndex = Random.Range(0, matchingLevels.Count);
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

    /// <summary>
    /// 进入下一关
    /// </summary>
    public void NextLevel()
    {
        currentLevel++;
    }

    /// <summary>
    /// 重置关卡
    /// </summary>
    public void ResetLevel()
    {
        currentLevel = 1;
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

