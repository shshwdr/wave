using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 关卡管理器 - 管理关卡进度和敌人生成
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    private readonly HashSet<int> usedLevelKeys = new HashSet<int>();

    /// <summary>
    /// 获取当前关卡
    /// </summary>
    public int CurrentLevel => MainGameManager.Instance.PlayerLevel+1;

    /// <summary>
    /// 初始化关卡管理器
    /// </summary>
    public void Init()
    {
        ClearUsedLevels();
    }

    public void ClearUsedLevels()
    {
        usedLevelKeys.Clear();
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
    /// 是否为 Elite 关卡（type == elite）
    /// </summary>
    public static bool IsEliteLevel(LevelInfo levelInfo)
    {
        return levelInfo != null
            && levelInfo.type != null
            && levelInfo.type.Equals("elite", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNormalBattleLevel(LevelInfo levelInfo)
    {
        if (levelInfo == null)
            return false;
        if (string.IsNullOrEmpty(levelInfo.type))
            return true;
        string type = levelInfo.type.ToLowerInvariant();
        return type == "normal";
    }

    /// <summary>
    /// 按地图节点 battleDifficulty 抽取普通关（本局尽量不重复；抽空后允许重复）
    /// </summary>
    public bool TryPickNormalLevelForIsland(int islandId, int battleDifficulty, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;

        List<KeyValuePair<int, LevelInfo>> matched = CollectIslandLevels(islandId, info =>
            IsNormalBattleLevel(info) && info.battleDifficulty == battleDifficulty);

        if (matched.Count == 0)
        {
            int maxBattleDifficulty = GetMaxBattleDifficultyForIsland(islandId, IsNormalBattleLevel);
            Debug.LogError($"岛屿 {islandId} 没有 battleDifficulty={battleDifficulty} 的 normal 关，回退到最大 battleDifficulty={maxBattleDifficulty}");
            matched = CollectIslandLevels(islandId, info =>
                IsNormalBattleLevel(info) && info.battleDifficulty == maxBattleDifficulty);
        }

        return TryPickFromCandidates(matched, out levelInfo, out levelIndex);
    }

    /// <summary>
    /// 抽取岛屿 Elite 关；没有则回退该岛最高 battleDifficulty，并报错
    /// </summary>
    public bool TryPickEliteLevelForIsland(int islandId, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;

        List<KeyValuePair<int, LevelInfo>> matched = CollectIslandLevels(islandId, IsEliteLevel);
        if (matched.Count == 0)
        {
            int maxBattleDifficulty = GetMaxBattleDifficultyForIsland(islandId, info => true);
            Debug.LogError($"岛屿 {islandId} 没有 type=elite 的关卡，回退到最大 battleDifficulty={maxBattleDifficulty}");
            matched = CollectIslandLevels(islandId, info => info.battleDifficulty == maxBattleDifficulty);
        }

        return TryPickFromCandidates(matched, out levelInfo, out levelIndex);
    }

    /// <summary>
    /// 获取指定岛屿的 Boss 关卡（地图 Boss 节点用，本局尽量不重复）
    /// </summary>
    public bool TryGetBossLevelForIsland(int islandId, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;

        List<KeyValuePair<int, LevelInfo>> matched = CollectIslandLevels(islandId, IsBossLevel);
        if (matched.Count == 0)
            return false;

        return TryPickFromCandidates(matched, out levelInfo, out levelIndex);
    }

    /// <summary>
    /// 按 CSV 顺序取指定岛屿的第 n 个非 Boss 关（n 从 0 起）
    /// </summary>
    public bool TryGetNthNonBossLevelForIsland(int islandId, int n, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;

        if (n < 0 || CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null)
        {
            return false;
        }

        int count = 0;
        foreach (int key in CSVLoader.Instance.levelInfoMap.Keys.OrderBy(k => k))
        {
            LevelInfo info = CSVLoader.Instance.levelInfoMap[key];
            if (info == null || info.island != islandId || IsBossLevel(info))
                continue;

            if (count == n)
            {
                levelInfo = info;
                levelIndex = key;
                return true;
            }

            count++;
        }

        return false;
    }

    private List<KeyValuePair<int, LevelInfo>> CollectIslandLevels(int islandId, Func<LevelInfo, bool> predicate)
    {
        var result = new List<KeyValuePair<int, LevelInfo>>();
        if (CSVLoader.Instance == null || CSVLoader.Instance.levelInfoMap == null || predicate == null)
            return result;

        foreach (int key in CSVLoader.Instance.levelInfoMap.Keys.OrderBy(k => k))
        {
            LevelInfo info = CSVLoader.Instance.levelInfoMap[key];
            if (info == null || info.island != islandId || !predicate(info))
                continue;
            result.Add(new KeyValuePair<int, LevelInfo>(key, info));
        }

        return result;
    }

    private int GetMaxBattleDifficultyForIsland(int islandId, Func<LevelInfo, bool> predicate)
    {
        int max = 0;
        foreach (var pair in CollectIslandLevels(islandId, predicate ?? (info => true)))
        {
            if (pair.Value.battleDifficulty > max)
                max = pair.Value.battleDifficulty;
        }
        return max;
    }

    private bool TryPickFromCandidates(List<KeyValuePair<int, LevelInfo>> candidates, out LevelInfo levelInfo, out int levelIndex)
    {
        levelInfo = null;
        levelIndex = -1;
        if (candidates == null || candidates.Count == 0)
            return false;

        List<KeyValuePair<int, LevelInfo>> unused = candidates.FindAll(pair => !usedLevelKeys.Contains(pair.Key));
        List<KeyValuePair<int, LevelInfo>> pool = unused.Count > 0 ? unused : candidates;
        int pick = UnityEngine.Random.Range(0, pool.Count);
        levelIndex = pool[pick].Key;
        levelInfo = pool[pick].Value;
        usedLevelKeys.Add(levelIndex);
        return true;
    }

    /// <summary>
    /// 下一个岛屿 id，没有则返回 -1
    /// </summary>
    public int GetNextIslandId(int islandId)
    {
        int next = int.MaxValue;
        bool found = false;

        if (CSVLoader.Instance != null && CSVLoader.Instance.levelInfoMap != null)
        {
            foreach (var info in CSVLoader.Instance.levelInfoMap.Values)
            {
                if (info != null && info.island > islandId && info.island < next)
                {
                    next = info.island;
                    found = true;
                }
            }
        }

        if (CSVLoader.Instance != null && CSVLoader.Instance.islandInfoMap != null)
        {
            foreach (int id in CSVLoader.Instance.islandInfoMap.Keys)
            {
                if (id > islandId && id < next)
                {
                    next = id;
                    found = true;
                }
            }
        }

        return found ? next : -1;
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

