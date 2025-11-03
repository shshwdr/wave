using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class ListExtension
{
    
    public static T PickItem<T>(this List<T> list)
    {
        if (list.Count == 0)
        {
            throw new System.InvalidOperationException("Cannot select a random item from an empty list.");
        }

        Random random = new Random();
        int index = random.Next(0, list.Count);
        var item = list[index];
        list.RemoveAt(index);
        return item;
    }
    public static List<T> MergeWith<T>(this List<T> listA, List<T> listB)
    {
        var merged = new List<T>(listA);
        merged.AddRange(listB);
        return merged;
    }
    public static T GetValueOrDefault<T>(this List<T> list, int index,T defaultValue = default(T))
    {
        if (index >= 0 && index < list.Count)
        {
            return list[index];
        }
        return defaultValue;  // 返回 null (对于引用类型) 或默认值 (对于值类型)
    }
    public static T RandomItem<T>(this List<T> list)
    {
        if (list.Count == 0)
        {
            Debug.LogError("Cannot select a random item from an empty list.");
            return default(T);
        }

        Random random = new Random();
        int index = random.Next(0, list.Count);
        return list[index];
    }
    
    /// <summary>
    /// 从列表中按权重随机选取一个元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="list">要选择的列表</param>
    /// <param name="weightSelector">用于获取每个元素的权重（必须 >= 0）</param>
    /// <returns>被选中的元素</returns>
    public static T RandomWeightedItem<T>(this List<T> list, Func<T, float> weightSelector)
    {
        Random random = new Random();
        if (list == null || list.Count == 0)
        {
            Debug.LogError("Cannot select from an empty list.");
            return default;
        }

        float totalWeight = 0f;
        foreach (var item in list)
        {
            float weight = weightSelector(item);
            if (weight < 0f)
            {
                Debug.LogWarning($"Item has negative weight ({weight}), treating as 0.");
                weight = 0f;
            }
            totalWeight += weight;
        }

        if (totalWeight == 0f)
        {
            Debug.LogWarning("All weights are zero, returning random item.");
            return list[random.Next(list.Count)];
        }

        float randomValue = (float)(random.NextDouble() * totalWeight);
        float cumulative = 0f;

        foreach (var item in list)
        {
            cumulative += weightSelector(item);
            if (randomValue <= cumulative)
                return item;
        }

        // Fallback, should not happen
        return list[list.Count - 1];
    }

    public static T LastItem<T>(this List<T> list)
    {
        if (list.Count == 0)
        {
            throw new System.InvalidOperationException("Cannot select the last item from an empty list.");
        }
        return list[list.Count - 1];
    }
    
    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static int GetRnadomIndexWithWeight(this IList<int> weights)
    {
        int totalWeight = 0;
        foreach (int weight in weights)
        {
            totalWeight += weight;
        }

        Random random = new Random();
        int randomNumber = random.Next(totalWeight); // 生成一个 0 到 totalWeight 之间的随机数

        int accumulatedWeight = 0;
        int selectedIndex = -1;
        for (int i = 0; i < weights.Count; i++)
        {
            accumulatedWeight += weights[i];
            if (randomNumber < accumulatedWeight)
            {
                selectedIndex = i;
                break;
            }
        }

        return selectedIndex;
    }
}