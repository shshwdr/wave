using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店符文项组件 - 显示符文信息、价格和购买按钮
/// </summary>
public class ShopRuneItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text actionText;

    private RuneInfo runeInfo;
    private SkillSelectMenu parentMenu;
    private bool hidePrice;

    public string RuneIdentifier => runeInfo != null ? runeInfo.identifier : "";

    private void Awake()
    {
        if (nameText != null)
            nameText.gameObject.SetActive(false);
    }

    public void Init(RuneInfo info, SkillSelectMenu menu, bool freePurchase = false)
    {
        runeInfo = info;
        parentMenu = menu;
        hidePrice = freePurchase;
        UpdateDisplay();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
            SetupDetailHover(buyButton.gameObject);
        }
    }

    public void UpdateState()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (runeInfo == null || RuneManager.Instance == null)
            return;

        UpdateIconImage();

        if (descriptionText != null)
            descriptionText.text = RuneManager.Instance.GetRuneDescription(runeInfo.identifier);

        bool alreadyOwned = RuneManager.Instance.HasRune(runeInfo.identifier);
        int price = RuneManager.Instance.GetDiscountedShopPrice(runeInfo.buyPrice);
        bool canAfford = hidePrice
            || (!alreadyOwned
                && PlayerManager.Instance != null
                && PlayerManager.Instance.Gold >= price);

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = hidePrice ? "0" : price.ToString();
        }

        if (buyButton != null)
            buyButton.interactable = canAfford;

        if (actionText != null)
            actionText.text = hidePrice ? "GET" : (alreadyOwned ? "OWNED" : "BUY");

        bool insufficientGold = !hidePrice && !alreadyOwned && !canAfford;
        ApplyTextColors(insufficientGold);
    }

    private void UpdateIconImage()
    {
        if (iconImage == null || runeInfo == null)
            return;

        Sprite sprite = runeInfo.icon;
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = true;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void ApplyTextColors(bool insufficientGold)
    {
        if (priceText == null)
            return;

        priceText.color = insufficientGold ? TileColorUtil.GetUnaffordableTextColor() : Color.white;
    }

    private void OnBuyClicked()
    {
        if (parentMenu != null && runeInfo != null)
            parentMenu.BuyRune(runeInfo);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowDetail();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDetail();
    }

    private void ShowDetail()
    {
        if (parentMenu != null && runeInfo != null)
            parentMenu.ShowRuneDetail(runeInfo.identifier);
    }

    private void HideDetail()
    {
        if (parentMenu != null)
            parentMenu.HideSkillDetail();
    }

    private void SetupDetailHover(GameObject target)
    {
        if (target == null)
            return;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener(_ => ShowDetail());
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener(_ => HideDetail());
        trigger.triggers.Add(entryExit);
    }
}
