using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用角色状态：stun / mark。Enemy、Ally、Player 都可以挂。
/// 状态格子从 Resources/prefab/statusCell 生成，挂到 status 节点下。
/// </summary>
public class CharacterStatus : MonoBehaviour
{
    public const string StunId = "stun";
    public const string MarkId = "mark";
    private const string StatusCellPrefabPath = "prefab/statusCell";

    [SerializeField] private Transform statusParent;

    private static GameObject statusCellPrefab;

    private int markStacks;
    private int markPercentPerStack;
    private bool stunned;
    private int stunTurns;
    private bool stunIconPendingHide;
    private readonly Dictionary<string, StatusCell> cells = new Dictionary<string, StatusCell>(StringComparer.OrdinalIgnoreCase);

    public bool IsStunned => stunned;
    public int MarkStacks => markStacks;

    public float MarkDamageMultiplier
    {
        get
        {
            if (markStacks <= 0 || markPercentPerStack <= 0)
                return 1f;
            return 1f + markStacks * markPercentPerStack / 100f;
        }
    }

    private void Awake()
    {
        ResolveStatusParent();
        ClearSpawnedCells();
    }

    public void ResetAll()
    {
        markStacks = 0;
        markPercentPerStack = 0;
        stunned = false;
        stunTurns = 0;
        stunIconPendingHide = false;
        ClearSpawnedCells();
    }

    public void AddMark(int percentPerStack)
    {
        if (percentPerStack < 0)
            percentPerStack = 0;

        markStacks++;
        markPercentPerStack = percentPerStack;
        RefreshCell(MarkId, markStacks, showCount: true);
    }

    public void ApplyStun()
    {
        stunned = true;
        stunTurns = 1;
        stunIconPendingHide = false;
        ShowStatusCell(StunId, stunTurns, showCount: true);
    }

    /// <summary>
    /// 因眩晕跳过本回合行动：数字减到 0，图标先留着。
    /// </summary>
    public void ConsumeStun()
    {
        if (!stunned)
            return;

        stunTurns = Mathf.Max(0, stunTurns - 1);
        stunned = stunTurns > 0;
        ShowStatusCell(StunId, stunTurns, showCount: true);
        if (!stunned)
            stunIconPendingHide = true;
    }

    /// <summary>
    /// 下一次行动前：若眩晕已结束（数字为 0），才隐藏图标。
    /// </summary>
    public void HideExpiredStun()
    {
        if (stunned || !stunIconPendingHide)
            return;

        stunIconPendingHide = false;
        RemoveCell(StunId);
    }

    public void BindStatusParent(Transform parent)
    {
        if (parent != null)
            statusParent = parent;
        ResolveStatusParent();
    }

    private void ResolveStatusParent()
    {
        if (statusParent != null)
            return;

        Transform found = transform.Find("status");
        if (found == null)
            found = transform.Find("statusParent");
        statusParent = found != null ? found : transform;
    }

    private void RefreshCell(string id, int count, bool showCount)
    {
        if (count <= 0)
        {
            RemoveCell(id);
            return;
        }

        StatusCell cell = GetOrCreateCell(id);
        if (cell == null)
            return;

        Sprite sprite = Resources.Load<Sprite>("status/" + id);
        if (sprite != null)
            cell.SetIcon(sprite);

        cell.SetVisible(true);
        cell.SetCount(count, showCount);
    }

    private void ShowStatusCell(string id, int count, bool showCount)
    {
        StatusCell cell = GetOrCreateCell(id);
        if (cell == null)
            return;

        Sprite sprite = Resources.Load<Sprite>("status/" + id);
        if (sprite != null)
            cell.SetIcon(sprite);

        cell.SetVisible(true);
        cell.SetCount(count, showCount);
    }

    private StatusCell GetOrCreateCell(string id)
    {
        if (cells.TryGetValue(id, out StatusCell existing) && existing != null)
            return existing;

        ResolveStatusParent();
        if (statusParent == null)
            return null;

        GameObject prefab = LoadStatusCellPrefab();
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, statusParent);
        StatusCell cell = instance.GetComponent<StatusCell>();
        if (cell == null)
            cell = instance.AddComponent<StatusCell>();

        cell.SetId(id);
        cells[id] = cell;
        return cell;
    }

    private void RemoveCell(string id)
    {
        if (!cells.TryGetValue(id, out StatusCell cell))
            return;

        cells.Remove(id);
        if (cell != null)
            Destroy(cell.gameObject);
    }

    private void ClearSpawnedCells()
    {
        foreach (var cell in cells.Values)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        cells.Clear();

        ResolveStatusParent();
        if (statusParent == null)
            return;

        foreach (var leftover in statusParent.GetComponentsInChildren<StatusCell>(true))
        {
            if (leftover != null)
                Destroy(leftover.gameObject);
        }
    }

    private static GameObject LoadStatusCellPrefab()
    {
        if (statusCellPrefab != null)
            return statusCellPrefab;

        statusCellPrefab = Resources.Load<GameObject>(StatusCellPrefabPath);
        if (statusCellPrefab == null)
            statusCellPrefab = Resources.Load<GameObject>("prefab/StatusCell");
        return statusCellPrefab;
    }
}
