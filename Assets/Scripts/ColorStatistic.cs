using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每个颜色的统计信息
/// </summary>
[System.Serializable]
public class ColorStatistic
{
    public TileColor color;
    
    // 总共生成多少这个颜色的方块
    public int totalTilesGenerated = 0;
    
    // 总共生成了几个wave
    public int totalWavesGenerated = 0;
    
    // 最大生成的多大（单个wave group的最大tile数）
    public int maxWaveSize = 0;
    
    // 整个group of wave造成的平均伤害多少
    public float averageDamagePerWaveGroup = 0f;
    
    // 最大伤害多少（单个wave group的最大伤害）
    public float maxDamagePerWaveGroup = 0f;
    
    // 总共伤害多少
    public float totalDamage = 0f;
    
    // 用于计算平均伤害的wave group列表
    private List<float> waveGroupDamages = new List<float>();
    
    /// <summary>
    /// 记录生成的tile数量
    /// </summary>
    public void RecordTilesGenerated(int count)
    {
        totalTilesGenerated += count;
    }
    
    /// <summary>
    /// 记录生成的wave数量
    /// </summary>
    public void RecordWaveGenerated()
    {
        totalWavesGenerated++;
    }
    
    /// <summary>
    /// 记录wave group的大小
    /// </summary>
    public void RecordWaveGroupSize(int size)
    {
        if (size > maxWaveSize)
        {
            maxWaveSize = size;
        }
    }
    
    /// <summary>
    /// 记录wave group造成的伤害
    /// </summary>
    public void RecordWaveGroupDamage(float damage)
    {
        if (damage > 0)
        {
            totalDamage += damage;
            waveGroupDamages.Add(damage);
            
            if (damage > maxDamagePerWaveGroup)
            {
                maxDamagePerWaveGroup = damage;
            }
            
            // 计算平均伤害
            if (waveGroupDamages.Count > 0)
            {
                float sum = 0f;
                foreach (var dmg in waveGroupDamages)
                {
                    sum += dmg;
                }
                averageDamagePerWaveGroup = sum / waveGroupDamages.Count;
            }
            
            Debug.Log($"[ColorStatistic] {color} - Recorded wave group damage: {damage}, Total: {totalDamage}, Average: {averageDamagePerWaveGroup}, Max: {maxDamagePerWaveGroup}");
        }
    }
    
    /// <summary>
    /// 记录非wave造成的伤害（overhealDoDamage, hitTakeDamage, spawnAlly等）
    /// </summary>
    public void RecordNonWaveDamage(float damage)
    {
        if (damage > 0)
        {
            totalDamage += damage;
            Debug.Log($"[ColorStatistic] {color} - Recorded non-wave damage: {damage}, Total: {totalDamage}");
        }
    }
    
    /// <summary>
    /// 重置统计（用于新回合）
    /// </summary>
    public void Reset()
    {
        totalTilesGenerated = 0;
        totalWavesGenerated = 0;
        maxWaveSize = 0;
        averageDamagePerWaveGroup = 0f;
        maxDamagePerWaveGroup = 0f;
        totalDamage = 0f;
        waveGroupDamages.Clear();
    }
    
    /// <summary>
    /// 复制统计信息
    /// </summary>
    public ColorStatistic Clone()
    {
        ColorStatistic clone = new ColorStatistic();
        clone.color = this.color;
        clone.totalTilesGenerated = this.totalTilesGenerated;
        clone.totalWavesGenerated = this.totalWavesGenerated;
        clone.maxWaveSize = this.maxWaveSize;
        clone.averageDamagePerWaveGroup = this.averageDamagePerWaveGroup;
        clone.maxDamagePerWaveGroup = this.maxDamagePerWaveGroup;
        clone.totalDamage = this.totalDamage;
        clone.waveGroupDamages = new List<float>(this.waveGroupDamages);
        return clone;
    }
}

