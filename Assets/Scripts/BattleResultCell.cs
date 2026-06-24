using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single reward row on the battle result screen (icon + label + button).
/// </summary>
public class BattleResultCell : MonoBehaviour
{
    public enum RewardType
    {
        Gold,
        Consumable,
        Card,
        Relic,
        Shop
    }

    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image claimedOverlay;

    private RewardType rewardType;
    private Action onClick;
    private bool claimed;

    public RewardType Type => rewardType;
    public bool IsClaimed => claimed;

    private void Awake()
    {
        EnsureReferences();
        EnsureClickHandler();

        if (claimedOverlay != null)
            claimedOverlay.gameObject.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);

        if (claimedOverlay == null)
        {
            Transform overlayTransform = transform.Find("overlay");
            if (overlayTransform != null)
                claimedOverlay = overlayTransform.GetComponent<Image>();
        }
    }

    private void EnsureClickHandler()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    public Vector3 GetIconWorldPosition()
    {
        if (iconImage != null)
            return iconImage.transform.position;

        return transform.position;
    }

    public void BindReferences(Button btn, Image icon, TMP_Text label, Image overlay = null)
    {
        button = btn;
        iconImage = icon;
        labelText = label;
        claimedOverlay = overlay;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(RewardType type, Sprite icon, string label, Action clickHandler)
    {
        EnsureReferences();
        EnsureClickHandler();

        rewardType = type;
        onClick = clickHandler;
        claimed = false;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (labelText != null)
            labelText.text = label;

        UpdateClaimedVisual();
    }

    public void SetClaimed(bool value)
    {
        claimed = value;
        UpdateClaimedVisual();
    }

    private void HandleClick()
    {
        if (claimed)
            return;

        onClick?.Invoke();
    }

    private void UpdateClaimedVisual()
    {
        if (button != null)
            button.interactable = !claimed;

        if (claimedOverlay != null)
        {
            claimedOverlay.raycastTarget = claimed;
            claimedOverlay.gameObject.SetActive(claimed);
        }
    }
}
