using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店符文项组件 - 显示符文信息、价格和购买按钮
/// </summary>
public class ShopRuneItem : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text actionText;

    private RuneInfo runeInfo;
    private SkillSelectMenu parentMenu;

    public string RuneIdentifier => runeInfo != null ? runeInfo.identifier : "";

    public void Init(RuneInfo info, SkillSelectMenu menu)
    {
        runeInfo = info;
        parentMenu = menu;
        UpdateDisplay();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
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

        if (nameText != null)
            nameText.text = RuneManager.Instance.GetRuneName(runeInfo.identifier);

        if (descriptionText != null)
            descriptionText.text = RuneManager.Instance.GetRuneDescription(runeInfo.identifier);

        bool alreadyOwned = RuneManager.Instance.HasRune(runeInfo.identifier);
        int price = RuneManager.Instance.GetDiscountedShopPrice(runeInfo.buyPrice);
        bool canAfford = !alreadyOwned
            && PlayerManager.Instance != null
            && PlayerManager.Instance.Gold >= price;

        if (priceText != null)
            priceText.text = price.ToString();

        if (buyButton != null)
            buyButton.interactable = canAfford;

        if (actionText != null)
            actionText.text = alreadyOwned ? "OWNED" : "BUY";

        bool insufficientGold = !alreadyOwned && !canAfford;
        ApplyTextColors(insufficientGold);
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
}
