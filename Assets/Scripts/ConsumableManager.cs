using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消耗品管理器 - 记录持有数量与获得顺序
/// </summary>
public class ConsumableManager : Singleton<ConsumableManager>
{
    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    private readonly List<string> slotOrder = new List<string>();

    public void Init()
    {
        counts.Clear();
        slotOrder.Clear();

        if (CSVLoader.Instance == null || CSVLoader.Instance.consumableInfoMap == null)
            return;

        foreach (var info in CSVLoader.Instance.consumableInfoMap.Values)
        {
            if (info.start > 0)
            {
                AddConsumable(info.identifier, info.start, recordOrder: true);
            }
        }
    }

    public int GetCount(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return 0;

        return counts.TryGetValue(identifier, out int count) ? count : 0;
    }

    public bool HasConsumable(string identifier)
    {
        return GetCount(identifier) > 0;
    }

    public ConsumableInfo GetInfo(string identifier)
    {
        if (CSVLoader.Instance == null || string.IsNullOrEmpty(identifier))
            return null;

        CSVLoader.Instance.consumableInfoMap.TryGetValue(identifier, out ConsumableInfo info);
        return info;
    }

    public int GetValue(string identifier)
    {
        ConsumableInfo info = GetInfo(identifier);
        if (info?.values == null || info.values.Count == 0)
            return 0;

        return info.values[0];
    }

    public string GetName(string identifier)
    {
        ConsumableInfo info = GetInfo(identifier);
        if (info == null || string.IsNullOrEmpty(info.name))
            return identifier ?? "";

        return info.name;
    }

    public string GetDescription(string identifier)
    {
        ConsumableInfo info = GetInfo(identifier);
        if (info == null)
            return "";

        string description = info.description ?? "";
        int value = GetValue(identifier);
        if (description.Contains("{0}"))
            description = description.Replace("{0}", value.ToString());

        return description;
    }

    public bool CanAdd(string identifier, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(identifier))
            return false;

        ConsumableInfo info = GetInfo(identifier);
        if (info == null || !info.available)
            return false;

        return GetCount(identifier) + amount <= info.maxCount;
    }

    public bool AddConsumable(string identifier, int amount, bool recordOrder = true)
    {
        if (amount <= 0 || string.IsNullOrEmpty(identifier))
            return false;

        ConsumableInfo info = GetInfo(identifier);
        if (info == null || !info.available)
            return false;

        int current = GetCount(identifier);
        int newCount = Mathf.Min(current + amount, info.maxCount);
        int added = newCount - current;
        if (added <= 0)
            return false;

        counts[identifier] = newCount;

        if (recordOrder && current == 0 && !slotOrder.Contains(identifier))
            slotOrder.Add(identifier);

        return true;
    }

    public bool RemoveConsumable(string identifier, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(identifier))
            return false;

        int current = GetCount(identifier);
        if (current < amount)
            return false;

        int newCount = current - amount;
        if (newCount <= 0)
        {
            counts.Remove(identifier);
            slotOrder.Remove(identifier);
        }
        else
        {
            counts[identifier] = newCount;
        }

        return true;
    }

    public List<string> GetOwnedTypes()
    {
        var owned = new List<string>();
        foreach (string identifier in slotOrder)
        {
            if (GetCount(identifier) > 0)
                owned.Add(identifier);
        }

        return owned;
    }

    public int SellConsumable(string identifier, int amount)
    {
        if (amount <= 0)
            return 0;

        ConsumableInfo info = GetInfo(identifier);
        if (info == null)
            return 0;

        int sellAmount = Mathf.Min(amount, GetCount(identifier));
        if (sellAmount <= 0)
            return 0;

        if (!RemoveConsumable(identifier, sellAmount))
            return 0;

        int goldGained = sellAmount * info.price;
        if (goldGained > 0 && PlayerManager.Instance != null)
            PlayerManager.Instance.AddGold(goldGained);

        return goldGained;
    }

    /// <summary>
    /// 使用即时生效的消耗品（heal / shield）。swapMagic 由 MainGameManager 处理。
    /// </summary>
    public bool TryUseImmediate(string identifier)
    {
        ConsumableInfo info = GetInfo(identifier);
        if (info == null || GetCount(identifier) <= 0)
            return false;

        if (info.effect == "swapMagic")
            return false;

        if (PlayerManager.Instance == null)
            return false;

        switch (info.effect)
        {
            case "heal":
                PlayerManager.Instance.Heal(GetValue(identifier));
                break;
            case "shield":
                PlayerManager.Instance.AddShield(GetValue(identifier));
                break;
            default:
                Debug.LogWarning($"Unknown consumable effect: {info.effect}");
                return false;
        }

        return RemoveConsumable(identifier, 1);
    }
}
