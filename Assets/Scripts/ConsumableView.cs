using UnityEngine;

/// <summary>
/// 消耗品栏视图：管理 3 个 ConsumableIcon 槽位，刷新显示。
/// </summary>
public class ConsumableView : MonoBehaviour
{
    [SerializeField] private ConsumableIcon[] slotIcons = new ConsumableIcon[3];

    public bool IsAnyPanelOpen
    {
        get
        {
            foreach (ConsumableIcon icon in slotIcons)
            {
                if (icon != null && icon.IsPanelOpen)
                    return true;
            }

            return false;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (!IsAnyPanelOpen)
            return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (!IsPointerOverConsumableUI())
                HideAllPanels();
        }
    }

    public void Refresh()
    {
        if (ConsumableManager.Instance == null)
            return;

        var owned = ConsumableManager.Instance.GetOwnedTypes();
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null)
                continue;

            if (i < owned.Count)
                slotIcons[i].SetConsumable(owned[i]);
            else
                slotIcons[i].Clear();
        }

        foreach (ConsumableIcon icon in slotIcons)
        {
            if (icon == null)
                continue;

            if (icon.IsPanelOpen && !icon.HasConsumable)
                icon.HidePanel();
            else
                icon.RefreshPanelIfOpen();
        }
    }

    public void NotifyPanelOpened(ConsumableIcon source)
    {
        foreach (ConsumableIcon icon in slotIcons)
        {
            if (icon != null && icon != source)
                icon.HidePanel();
        }
    }

    public void HideAllPanels()
    {
        foreach (ConsumableIcon icon in slotIcons)
        {
            icon?.HidePanel();
        }
    }

    public RectTransform GetFlyTargetForConsumable(string consumableId)
    {
        if (slotIcons == null || slotIcons.Length == 0)
            return transform as RectTransform;

        foreach (ConsumableIcon icon in slotIcons)
        {
            if (icon != null && icon.Identifier == consumableId)
                return icon.FlyTargetRect;
        }

        foreach (ConsumableIcon icon in slotIcons)
        {
            if (icon != null && !icon.HasConsumable)
                return icon.FlyTargetRect;
        }

        ConsumableIcon firstIcon = slotIcons[0];
        return firstIcon != null ? firstIcon.FlyTargetRect : transform as RectTransform;
    }

    public static bool IsPointerOverConsumableUI()
    {
        ConsumableView view = FindObjectOfType<ConsumableView>(true);
        if (view == null)
            return false;

        return view.IsPointerOverIconArea();
    }

    private bool IsPointerOverIconArea()
    {
        foreach (ConsumableIcon icon in slotIcons)
        {
            if (icon != null && icon.IsPointerOver())
                return true;
        }

        return false;
    }
}
