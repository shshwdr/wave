using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 随从管理器
/// </summary>
public class AllyManager : MonoBehaviour
{
    private List<Ally> activeAllies = new List<Ally>();

    public List<Ally> ActiveAllies => activeAllies;

    /// <summary>
    /// 添加随从
    /// </summary>
    public void AddAlly(Ally ally)
    {
        if (ally != null && !activeAllies.Contains(ally))
        {
            activeAllies.Add(ally);
        }
    }

    /// <summary>
    /// 移除随从
    /// </summary>
    public void RemoveAlly(Ally ally)
    {
        if (ally != null)
        {
            activeAllies.Remove(ally);
        }
    }

    /// <summary>
    /// 检查指定位置是否有随从
    /// </summary>
    public bool HasAllyAtPosition(Vector2Int gridPos)
    {
        foreach (var ally in activeAllies)
        {
            if (ally != null && !ally.IsDead && ally.GridPosition == gridPos)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定位置的随从
    /// </summary>
    public Ally GetAllyAtPosition(Vector2Int gridPos)
    {
        foreach (var ally in activeAllies)
        {
            if (ally != null && !ally.IsDead && ally.GridPosition == gridPos)
            {
                return ally;
            }
        }
        return null;
    }
}

