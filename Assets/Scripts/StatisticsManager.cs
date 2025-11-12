using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统计管理器 - 管理每个回合和整个游戏的统计信息
/// </summary>
public class StatisticsManager : MonoBehaviour
{
    private static StatisticsManager instance;
    public static StatisticsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<StatisticsManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("StatisticsManager");
                    instance = obj.AddComponent<StatisticsManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }
    
    // 当前回合的统计（每个颜色一个）
    private List<ColorStatistic> currentRoundStatistics = new List<ColorStatistic>();
    
    // 整个游戏的统计（每个颜色一个）
    private List<ColorStatistic> totalGameStatistics = new List<ColorStatistic>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStatistics();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化统计列表
    /// </summary>
    private void InitializeStatistics()
    {
        currentRoundStatistics.Clear();
        totalGameStatistics.Clear();
        
        for (int i = 0; i < 4; i++)
        {
            ColorStatistic roundStat = new ColorStatistic();
            roundStat.color = (TileColor)i;
            currentRoundStatistics.Add(roundStat);
            
            ColorStatistic totalStat = new ColorStatistic();
            totalStat.color = (TileColor)i;
            totalGameStatistics.Add(totalStat);
        }
    }
    
    /// <summary>
    /// 获取当前回合的统计
    /// </summary>
    public List<ColorStatistic> GetCurrentRoundStatistics()
    {
        return currentRoundStatistics;
    }
    
    /// <summary>
    /// 获取整个游戏的统计
    /// </summary>
    public List<ColorStatistic> GetTotalGameStatistics()
    {
        return totalGameStatistics;
    }
    
    /// <summary>
    /// 获取指定颜色的当前回合统计
    /// </summary>
    public ColorStatistic GetCurrentRoundStatistic(TileColor color)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            return currentRoundStatistics[index];
        }
        return null;
    }
    
    /// <summary>
    /// 获取指定颜色的整个游戏统计
    /// </summary>
    public ColorStatistic GetTotalGameStatistic(TileColor color)
    {
        int index = (int)color;
        if (index >= 0 && index < totalGameStatistics.Count)
        {
            return totalGameStatistics[index];
        }
        return null;
    }
    
    /// <summary>
    /// 记录生成的tile数量
    /// </summary>
    public void RecordTilesGenerated(TileColor color, int count)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            currentRoundStatistics[index].RecordTilesGenerated(count);
            totalGameStatistics[index].RecordTilesGenerated(count);
        }
    }
    
    /// <summary>
    /// 记录生成的wave数量
    /// </summary>
    public void RecordWaveGenerated(TileColor color)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            currentRoundStatistics[index].RecordWaveGenerated();
            totalGameStatistics[index].RecordWaveGenerated();
        }
    }
    
    /// <summary>
    /// 记录wave group的大小
    /// </summary>
    public void RecordWaveGroupSize(TileColor color, int size)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            currentRoundStatistics[index].RecordWaveGroupSize(size);
            totalGameStatistics[index].RecordWaveGroupSize(size);
        }
    }
    
    /// <summary>
    /// 记录wave group造成的伤害
    /// </summary>
    public void RecordWaveGroupDamage(TileColor color, float damage)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            currentRoundStatistics[index].RecordWaveGroupDamage(damage);
            totalGameStatistics[index].RecordWaveGroupDamage(damage);
        }
    }
    
    /// <summary>
    /// 记录非wave造成的伤害（overhealDoDamage, hitTakeDamage, spawnAlly等）
    /// </summary>
    public void RecordNonWaveDamage(TileColor color, float damage)
    {
        int index = (int)color;
        if (index >= 0 && index < currentRoundStatistics.Count)
        {
            currentRoundStatistics[index].RecordNonWaveDamage(damage);
            totalGameStatistics[index].RecordNonWaveDamage(damage);
        }
    }
    
    /// <summary>
    /// 开始新回合（重置当前回合统计）
    /// </summary>
    public void StartNewRound()
    {
        foreach (var stat in currentRoundStatistics)
        {
            stat.Reset();
        }
        Debug.Log("[StatisticsManager] Started new round, reset current round statistics");
    }
    
    /// <summary>
    /// 获取上一回合的统计（返回当前回合统计的副本，因为会在新回合时重置）
    /// </summary>
    public List<ColorStatistic> GetLastRoundStatistics()
    {
        List<ColorStatistic> lastRoundStats = new List<ColorStatistic>();
        foreach (var stat in currentRoundStatistics)
        {
            lastRoundStats.Add(stat.Clone());
        }
        return lastRoundStats;
    }
}

