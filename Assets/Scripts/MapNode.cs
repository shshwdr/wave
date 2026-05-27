using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图节点（UI按钮）
/// </summary>
public class MapNode : MonoBehaviour
{
    private const string MapNodeSpritePath = "mapNode/";

    [Header("节点配置")]
    [SerializeField] private string type = "battle";
    [SerializeField] private Image iconImage;

    private Button nodeButton;
    private bool isBossNode;
    private bool used;

    public string Type => type;
    public bool IsBossNode => isBossNode;
    public bool IsUsed => used;
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;
    public bool IsInteractable => nodeButton != null && nodeButton.interactable;

    public event Action<MapNode> OnNodeClicked;

    private void Awake()
    {
        nodeButton = GetComponentInChildren<Button>();
        if (nodeButton == null)
        {
            nodeButton = gameObject.AddComponent<Button>();
        }

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(HandleClick);
        RefreshIcon();
    }

    public void SetType(string newType)
    {
        type = newType;
        RefreshIcon();
    }

    public void SetIsBossNode(bool value)
    {
        isBossNode = value;
        RefreshIcon();
    }

    public void SetInteractable(bool interactable)
    {
        if (nodeButton != null)
        {
            nodeButton.interactable = interactable;
        }
    }

    public void SetMapVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetUsed(bool value)
    {
        used = value;
    }

    public void SetVisited(bool visited)
    {
        // 预留：可按 visited 调整节点外观
    }

    private void HandleClick()
    {
        OnNodeClicked?.Invoke(this);
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = LoadMapNodeSprite(GetIconResourceName());
    }

    private string GetIconResourceName()
    {
        if (isBossNode && string.Equals(type, "battle", StringComparison.OrdinalIgnoreCase))
        {
            return "boss";
        }

        switch ((type ?? string.Empty).ToLower())
        {
            case "battle":
                return "battle";
            case "event":
                return "event";
            case "shop":
                return "shop";
            case "heal":
                return "heal";
            default:
                return "battle";
        }
    }

    private static Sprite LoadMapNodeSprite(string resourceName)
    {
        return Resources.Load<Sprite>(MapNodeSpritePath + resourceName);
    }
}
