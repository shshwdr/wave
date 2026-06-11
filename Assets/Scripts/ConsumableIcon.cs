using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 单个消耗品槽位：槽位背景常驻，有道具时显示内容；右键打开本 Icon 内的操作面板。
/// </summary>
public class ConsumableIcon : MonoBehaviour, IPointerClickHandler
{
    [Header("槽位")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private GameObject contentRoot;

    [Header("内容")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countText;

    [Header("操作面板")]
    [SerializeField] private GameObject interactivePanel;
    [SerializeField] private TMP_Text panelTitleText;
    [SerializeField] private TMP_Text panelDescriptionText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button sellAllButton;

    private ConsumableView parentView;
    private string identifier;

    public string Identifier => identifier;
    public bool HasConsumable => !string.IsNullOrEmpty(identifier);
    public bool IsPanelOpen => interactivePanel != null && interactivePanel.activeSelf;

    private void Awake()
    {
        parentView = GetComponentInParent<ConsumableView>();

        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(OnSellAllClicked);

        HidePanel();
        Clear();
    }

    public void SetConsumable(string consumableIdentifier)
    {
        identifier = consumableIdentifier;

        if (string.IsNullOrEmpty(identifier))
        {
            Clear();
            return;
        }

        if (contentRoot != null)
            contentRoot.SetActive(true);

        if (ConsumableManager.Instance == null)
            return;

        if (titleText != null)
            titleText.text = ConsumableManager.Instance.GetName(identifier);

        if (countText != null)
            countText.text = ConsumableManager.Instance.GetCount(identifier).ToString();

        UpdateIconImage();
    }

    public void Clear()
    {
        identifier = null;
        HidePanel();

        if (contentRoot != null)
            contentRoot.SetActive(false);

        if (titleText != null)
            titleText.text = string.Empty;

        if (countText != null)
            countText.text = string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void UpdateIconImage()
    {
        if (iconImage == null || ConsumableManager.Instance == null)
            return;

        ConsumableInfo info = ConsumableManager.Instance.GetInfo(identifier);
        Sprite sprite = info?.icon;
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasConsumable)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
            TogglePanel();
    }

    public void TogglePanel()
    {
        if (!HasConsumable || interactivePanel == null)
            return;

        if (IsPanelOpen)
        {
            HidePanel();
            return;
        }

        parentView?.NotifyPanelOpened(this);
        interactivePanel.SetActive(true);
        UpdatePanelContent();
    }

    public void HidePanel()
    {
        if (interactivePanel != null)
            interactivePanel.SetActive(false);
    }

    public void RefreshPanelIfOpen()
    {
        if (IsPanelOpen)
            UpdatePanelContent();
    }

    public bool IsPointerOver()
    {
        RectTransform slotRect = slotBackground != null
            ? slotBackground.transform as RectTransform
            : transform as RectTransform;

        if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, Input.mousePosition, null))
            return true;

        if (IsPanelOpen && interactivePanel != null)
        {
            RectTransform panelRect = interactivePanel.transform as RectTransform;
            if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, null))
                return true;
        }

        return false;
    }

    private void UpdatePanelContent()
    {
        if (!HasConsumable || ConsumableManager.Instance == null)
            return;

        ConsumableInfo info = ConsumableManager.Instance.GetInfo(identifier);
        int count = ConsumableManager.Instance.GetCount(identifier);

        if (panelTitleText != null)
            panelTitleText.text = ConsumableManager.Instance.GetName(identifier);

        if (panelDescriptionText != null)
            panelDescriptionText.text = ConsumableManager.Instance.GetDescription(identifier);

        bool inBattle = MainGameManager.Instance != null && MainGameManager.Instance.IsInActiveBattle;
        bool canUse = count > 0 && info != null && (!info.isBattleOnly || inBattle);

        if (useButton != null)
            useButton.gameObject.SetActive(canUse);

        if (sellButton != null)
            sellButton.gameObject.SetActive(count > 0);

        if (sellAllButton != null)
            sellAllButton.gameObject.SetActive(count > 1);
    }

    private void OnUseClicked()
    {
        if (!HasConsumable || ConsumableManager.Instance == null)
            return;

        ConsumableInfo info = ConsumableManager.Instance.GetInfo(identifier);
        if (info == null)
            return;

        HidePanel();

        if (info.effect == "swapMagic")
        {
            MainGameManager.Instance?.EnterConsumableSwapMode(identifier);
        }
        else
        {
            ConsumableManager.Instance.TryUseImmediate(identifier);
        }

        parentView?.Refresh();
    }

    private void OnSellClicked()
    {
        if (!HasConsumable || ConsumableManager.Instance == null)
            return;

        ConsumableManager.Instance.SellConsumable(identifier, 1);
        HidePanel();
        parentView?.Refresh();
    }

    private void OnSellAllClicked()
    {
        if (!HasConsumable || ConsumableManager.Instance == null)
            return;

        int count = ConsumableManager.Instance.GetCount(identifier);
        ConsumableManager.Instance.SellConsumable(identifier, count);
        HidePanel();
        parentView?.Refresh();
    }
}
