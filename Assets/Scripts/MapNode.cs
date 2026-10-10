using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地图节点（UI按钮）
/// </summary>
public class MapNode : MonoBehaviour
{
    private const string MapNodeSpritePath = "mapNode/";
    private static readonly Color HoverTint = new Color(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color DisabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("节点配置")]
    [SerializeField] private string type = "battle";
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject consumableRewardIndicator;
    [SerializeField] private TextMeshProUGUI labelText;

    private Button nodeButton;
    private bool isBossNode;
    private bool used;
    private bool visited;
    private bool hasConsumableReward;
    private bool isPointerHovering;
    private Color normalIconColor = Color.white;
    private bool hasBattleDifficulty;
    private int battleDifficulty;
    private LevelInfo boundLevelInfo;
    private int boundLevelIndex = -1;

    public string Type => type;
    public bool IsBossNode => isBossNode;
    public bool IsUsed => used;
    public bool HasConsumableReward => hasConsumableReward;
    public bool HasBattleDifficulty => hasBattleDifficulty;
    public int BattleDifficulty => battleDifficulty;
    public bool HasBoundLevel => boundLevelInfo != null;
    public LevelInfo BoundLevelInfo => boundLevelInfo;
    public int BoundLevelIndex => boundLevelIndex;
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;
    public bool IsInteractable => nodeButton != null && nodeButton.interactable;

    public event Action<MapNode> OnNodeClicked;

    public void ClearClickListeners()
    {
        OnNodeClicked = null;
    }

    private void Awake()
    {
        nodeButton = GetComponentInChildren<Button>(true);
        if (nodeButton == null)
            nodeButton = gameObject.AddComponent<Button>();

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(HandleClick);

        // 关闭自动导航，避免选中态卡住导致“亮着却点不了”
        Navigation nav = nodeButton.navigation;
        nav.mode = Navigation.Mode.None;
        nodeButton.navigation = nav;

        // 用代码控制 hover 变暗；默认 ColorTint 几乎看不出变化
        nodeButton.transition = Selectable.Transition.None;

        if (iconImage == null && nodeButton.targetGraphic is Image targetImage)
            iconImage = targetImage;

        if (iconImage != null)
            normalIconColor = Color.white;

        EnsureLabelReference();
        SetupHoverTriggers(nodeButton.gameObject);
        EnsureConsumableIndicatorReference();
        SetConsumableIndicatorVisible(false);
        RefreshIcon();
        RefreshLabel();
    }

    private void SetupHoverTriggers(GameObject target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener(_ => OnHover(true));
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener(_ => OnHover(false));
        trigger.triggers.Add(entryExit);
    }

    private void OnHover(bool hovering)
    {
        if (hovering && (nodeButton == null || !nodeButton.interactable))
            return;

        isPointerHovering = hovering && nodeButton != null && nodeButton.interactable;
        RefreshIconColor();
    }

    private void EnsureConsumableIndicatorReference()
    {
        if (consumableRewardIndicator != null)
            return;

        Transform consumableTransform = transform.Find("consumable");
        if (consumableTransform != null)
            consumableRewardIndicator = consumableTransform.gameObject;
    }

    private void EnsureLabelReference()
    {
        if (labelText != null)
            return;

        labelText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void ResetBattleAssignment()
    {
        hasBattleDifficulty = false;
        battleDifficulty = 0;
        boundLevelInfo = null;
        boundLevelIndex = -1;
        RefreshLabel();
    }

    public void SetType(string newType)
    {
        type = newType;
        RefreshIcon();
        RefreshLabel();
    }

    public void SetIsBossNode(bool value)
    {
        isBossNode = value;
        RefreshIcon();
        RefreshLabel();
    }

    public void SetBattleDifficulty(int difficulty)
    {
        battleDifficulty = difficulty;
        hasBattleDifficulty = true;
        RefreshLabel();
    }

    public void BindLevel(LevelInfo levelInfo, int levelIndex)
    {
        boundLevelInfo = levelInfo;
        boundLevelIndex = levelIndex;
    }

    public void SetInteractable(bool interactable)
    {
        if (nodeButton != null)
            nodeButton.interactable = interactable;

        if (!interactable)
            isPointerHovering = false;

        RefreshIconColor();
    }

    public void SetMapVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible)
            isPointerHovering = false;
    }

    public void SetUsed(bool value)
    {
        used = value;
    }

    public void SetVisited(bool visited)
    {
        this.visited = visited;
        RefreshIconColor();
    }

    public void SetHasConsumableReward(bool value)
    {
        hasConsumableReward = value;
    }

    public void SetConsumableIndicatorVisible(bool visible)
    {
        EnsureConsumableIndicatorReference();
        if (consumableRewardIndicator != null)
            consumableRewardIndicator.SetActive(visible);
    }

    public void RefreshLabel()
    {
        EnsureLabelReference();
        if (labelText == null)
            return;

        labelText.text = BuildLabelText();
    }

    private string BuildLabelText()
    {
        if (isBossNode)
            return "BOSS";

        switch ((type ?? string.Empty).ToLowerInvariant())
        {
            case "elite":
                return hasBattleDifficulty ? $"Elite {battleDifficulty}" : "Elite";
            case "battle":
                return hasBattleDifficulty ? $"LV {battleDifficulty}" : string.Empty;
            case "shop":
                return "Shop";
            case "heal":
                return "Rest";
            case "event":
                return "Event";
            default:
                return string.Empty;
        }
    }

    private void HandleClick()
    {
        if (nodeButton != null && !nodeButton.interactable)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        OnNodeClicked?.Invoke(this);
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        iconImage.sprite = LoadMapNodeSprite(GetIconResourceName());
        RefreshIconColor();
    }

    private void RefreshIconColor()
    {
        if (iconImage == null)
            return;

        bool disabled = nodeButton == null || !nodeButton.interactable;
        if (disabled)
        {
            iconImage.color = normalIconColor * DisabledTint;
            return;
        }

        if (isPointerHovering)
        {
            iconImage.color = normalIconColor * HoverTint;
            return;
        }

        iconImage.color = normalIconColor;
    }

    private string GetIconResourceName()
    {
        if (isBossNode && string.Equals(type, "battle", StringComparison.OrdinalIgnoreCase))
            return "boss";

        switch ((type ?? string.Empty).ToLower())
        {
            case "battle":
                return "battle";
            case "elite":
                return "elite";
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
