using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店技能项组件 - 显示技能信息、价格和购买按钮
/// </summary>
public class ShopSkillItem : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject lockIcon; // 锁的图标GameObject
    [SerializeField] private TMP_Text actionText; // 锁的图标GameObject
    
    

    private SkillInfo skillInfo;
    private SkillSelectMenu parentMenu;
    private bool hidePrice;
    
    /// <summary>
    /// 获取技能identifier（用于外部访问）
    /// </summary>
    public string SkillIdentifier => skillInfo != null ? skillInfo.identifier : "";

    /// <summary>
    /// 初始化商店技能项
    /// </summary>
    public void Init(SkillInfo info, SkillSelectMenu menu, bool freePurchase = false)
    {
        skillInfo = info;
        parentMenu = menu;
        hidePrice = freePurchase;

        // 更新显示
        UpdateDisplay();

        // 设置购买按钮
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    /// <summary>
    /// 更新状态（检查是否还能购买）
    /// </summary>
    public void UpdateState()
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (skillInfo == null)
            return;

        // 更新技能名称（购买界面显示下一个等级）
        if (skillNameText != null)
        {
            skillNameText.text = SkillManager.Instance.GetSkillName(skillInfo.identifier, true);
        }

        // 更新技能描述
        if (skillDescriptionText != null)
        {
            string description = SkillManager.Instance.GetSkillDescription(skillInfo.identifier, true);
            skillDescriptionText.text = description;
        }

        // 更新价格
        // if (priceText != null)
        // {
        //     bool isUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier);
        //     int price = isUpgrade ? skillInfo.upgradePrice : skillInfo.buyPrice;
        //     //priceText.text = $"Price: {price}";
        //     priceText.text = $"{price}";
        // }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(!hidePrice);
        }

        // 更新购买按钮状态
        if (buyButton != null)
        {
            bool isUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier);
            int price = isUpgrade ? skillInfo.upgradePrice : skillInfo.buyPrice;
            if (RuneManager.Instance != null)
                price = RuneManager.Instance.GetDiscountedShopPrice(price);
            
            bool canAfford = hidePrice || (PlayerManager.Instance != null && PlayerManager.Instance.Gold >= price);
            buyButton.interactable = canAfford;
            
            if (priceText != null && !hidePrice)
            {
                priceText.text = price.ToString();
            }

            if (actionText != null)
            {
                actionText.text = hidePrice ? "GET" : (isUpgrade ? "UPGRADE" : "BUY");
            }
            // TMP_Text buttonText = buyButton.GetComponentInChildren<TMP_Text>();
            // if (buttonText != null)
            // {
            //     buttonText.text = (isUpgrade ? "Upgrade" : "Buy") +$"({price})";
            // }
        }
    }

    /// <summary>
    /// 设置锁图标的显示/隐藏
    /// </summary>
    public void SetLockVisible(bool visible)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(visible);
        }
    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    private void OnBuyClicked()
    {
        if (parentMenu != null && skillInfo != null)
        {
            parentMenu.BuySkill(skillInfo);
        }
    }
}


