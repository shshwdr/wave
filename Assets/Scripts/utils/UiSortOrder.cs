using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI draw order: menus (0) &lt; popup (100) &lt; collectable fly (200).
/// Uses canvas sorting only; does not change layout or sibling order.
/// </summary>
public static class UiSortOrder
{
    public const int Menu = 0;
    public const int Popup = 100;
    public const int Fly = 200;

    public static void ApplySorting(Transform root, int sortingOrder, bool enableRaycast = false)
    {
        if (root == null)
            return;

        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null)
            canvas = root.gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        if (enableRaycast)
        {
            if (raycaster == null)
                raycaster = root.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }
        else if (raycaster != null)
        {
            raycaster.enabled = false;
        }
    }

    public static void BringPopupToFront(Transform popupRoot)
    {
        if (popupRoot == null)
            return;

        ApplySorting(popupRoot, Popup, enableRaycast: true);
        CollectableFlyManager.BringLayerToFront();
    }

    public static void BringMenuToFront(Transform menuRoot)
    {
        if (menuRoot == null)
            return;

        DialogBase dialog = Object.FindObjectOfType<DialogBase>(true);
        if (dialog != null && dialog.IsActive)
            BringPopupToFront(dialog.transform);

        CollectableFlyManager.BringLayerToFront();
    }
}
