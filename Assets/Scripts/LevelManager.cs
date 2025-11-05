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
    /// 获取下一个关卡
    /// </summary>
    public LevelInfo GetNextLevel()
    {
        // 随机从所有关卡中选择一个
        if (CSVLoader.Instance != null && CSVLoader.Instance.levelInfoMap != null && CSVLoader.Instance.levelInfoMap.Count > 0)
        {
            var levels = CSVLoader.Instance.levelInfoMap.Values.ToList();
            int randomIndex = Random.Range(0, levels.Count);
            return levels[randomIndex];
        }
        
        return null;
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
            if (int.TryParse(parts[i + 1].Trim(), out int count))
            {
                result.Add(new EnemySpawnInfo
                {
                    identifier = identifier,
                    count = count
                });
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

