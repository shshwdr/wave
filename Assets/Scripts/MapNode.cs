using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图节点（UI按钮）
/// </summary>
public class MapNode : MonoBehaviour
{
    [Header("节点配置")]
    [SerializeField] private string type = "battle";
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite battleSprite;
    [SerializeField] private Sprite eventSprite;
    [SerializeField] private Sprite defaultSprite;

    private Button nodeButton;

    public string Type => type;
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

    public void SetInteractable(bool interactable)
    {
        if (nodeButton != null)
        {
            nodeButton.interactable = interactable;
        }
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

        switch ((type ?? string.Empty).ToLower())
        {
            case "battle":
                iconImage.sprite = battleSprite != null ? battleSprite : defaultSprite;
                break;
            case "event":
                iconImage.sprite = eventSprite != null ? eventSprite : defaultSprite;
                break;
            default:
                iconImage.sprite = defaultSprite;
                break;
        }
    }
}
