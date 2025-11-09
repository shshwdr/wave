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

    private SkillInfo skillInfo;
    private SkillSelectMenu parentMenu;
    
    /// <summary>
    /// 获取技能identifier（用于外部访问）
    /// </summary>
    public string SkillIdentifier => skillInfo != null ? skillInfo.identifier : "";

    /// <summary>
    /// 初始化商店技能项
    /// </summary>
    public void Init(SkillInfo info, SkillSelectMenu menu)
    {
        skillInfo = info;
        parentMenu = menu;

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

        // 更新技能名称
        if (skillNameText != null)
        {
            skillNameText.text = skillInfo.name;
        }

        // 更新技能描述
        if (skillDescriptionText != null)
        {
            string description = SkillManager.Instance.GetSkillDescription(skillInfo.identifier, true);
            skillDescriptionText.text = description;
        }

        // 更新价格
        if (priceText != null)
        {
            bool isUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier);
            int price = isUpgrade ? skillInfo.upgradePrice : skillInfo.buyPrice;
            priceText.text = $"Price: {price}";
        }

        // 更新购买按钮状态
        if (buyButton != null)
        {
            bool isUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier);
            int price = isUpgrade ? skillInfo.upgradePrice : skillInfo.buyPrice;
            
            // 检查金币是否足够
            bool canAfford = PlayerManager.Instance != null && PlayerManager.Instance.Gold >= price;
            buyButton.interactable = canAfford;
            
            // 更新按钮文本
            TMP_Text buttonText = buyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isUpgrade ? "Upgrade" : "Buy";
            }
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


