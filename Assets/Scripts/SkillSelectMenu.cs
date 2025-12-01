using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

/// <summary>
/// 技能选择界面 - 包含三选一、四个颜色区域、背包区域和拖拽功能
/// </summary>
public class SkillSelectMenu : MenuBase
{
    [Header("商店区域")]
    [SerializeField] private Transform shopParent; // 商店技能列表容器
    [SerializeField] private GameObject shopSkillItemPrefab; // 商店技能项Prefab（包含技能图标、价格、购买按钮）

    [Header("颜色区域")]
    [SerializeField] public ColorArea[] colorArea = new ColorArea[4]; // 0=红，1=黄，2=蓝，3=绿

    [Header("背包区域")]
    [SerializeField] public Transform backpackParent;

    [Header("技能图标Prefab")]
    [SerializeField] private GameObject skillIconPrefab;

    [Header("技能详情显示")]
    [SerializeField] private GameObject skillDetailPanel;
    [SerializeField] private TMP_Text skillDetailText;

    [Header("确认按钮")]
    [SerializeField] private Button confirmButton;
    
    [Header("刷新按钮")]
    [SerializeField] private Button refreshButton;
    
    [Header("锁定按钮")]
    [SerializeField] private Button lockButton;
    
    [Header("金币显示")]
    [SerializeField] private TMP_Text goldText;
    
    [Header("统计按钮")]
    [SerializeField] private Button statisticsButton;

    private Action onConfirm; // 确认按钮的回调
    private Dictionary<string, SkillIconUI> skillIconMap = new Dictionary<string, SkillIconUI>(); // 技能identifier -> UI实例
    private List<GameObject> shopSkillItems = new List<GameObject>(); // 商店技能项列表
    private int currentRefreshPrice = 1; // 当前刷新价格（每次进入商店重置为1）
    private bool isLocked = false; // 锁定状态
    private HashSet<string> lockedSkillIdentifiers = new HashSet<string>(); // 锁定的技能identifier列表

    // 拖拽相关
    private SkillIconUI draggingIcon = null;
    private SkillIconUI tempDragIcon = null; // 临时拖拽图标
    private Transform originalParent = null;
    private int originalSiblingIndex = -1;
    private int originalColorIndex = -1; // -1表示在背包中
    private PointerEventData currentDragEventData = null; // 当前拖拽事件数据
    
    [Header("拖拽设置")]
    [SerializeField] private int dragDropLayer = 0; // 拖拽目标检测的Layer

    private FMOD.Studio.EventInstance shopFilter;

    protected override void Awake()
    {
        base.Awake();
        dragDropLayer = LayerMask.NameToLayer("DropTarget");
        // 初始化刷新按钮
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        // 初始化锁定按钮
        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnLockClicked);
            UpdateLockButtonText();
        }

        // 初始化颜色区域的详情按钮和颜色图片
        for (int i = 0; i < 4; i++)
        {
            int colorIndex = i;
            if (colorArea[i] != null)
            {
                // 设置颜色图片
                if (colorArea[i].colorImage != null)
                {
                    TileColor tileColor = (TileColor)colorIndex;
                    Color waveColor = TileColorUtil.GetUnityColor(tileColor);
                    colorArea[i].colorImage.color = waveColor;
                }

                // 添加鼠标悬停事件
                if (colorArea[i].button != null)
                {
                    EventTrigger trigger = colorArea[i].button.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null)
                    {
                        trigger = colorArea[i].button.gameObject.AddComponent<EventTrigger>();
                    }

                    EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                    entryEnter.eventID = EventTriggerType.PointerEnter;
                    entryEnter.callback.AddListener((data) => OnColorAreaButtonHover(colorIndex, true));
                    trigger.triggers.Add(entryEnter);

                    EventTrigger.Entry entryExit = new EventTrigger.Entry();
                    entryExit.eventID = EventTriggerType.PointerExit;
                    entryExit.callback.AddListener((data) => OnColorAreaButtonHover(colorIndex, false));
                    trigger.triggers.Add(entryExit);
                }
            }
        }

        // 初始化技能详情面板
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
        }

        // 初始化确认按钮
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        // 初始化统计按钮
        if (statisticsButton != null)
        {
            statisticsButton.onClick.AddListener(OnStatisticsClicked);
        }
    }

    /// <summary>
    /// 显示技能商店界面
    /// </summary>
    public void ShowSkillSelection(Action onConfirmCallback = null)
    {
        onConfirm = onConfirmCallback;

        // 重置刷新价格为1
        currentRefreshPrice = 1;
        UpdateRefreshButton();

        // 如果有锁定的技能，保留它们；否则正常刷新
        bool hadLockedSkills = lockedSkillIdentifiers.Count > 0;
        if (hadLockedSkills)
        {
            // 保留锁定的技能，不刷新商店
            // 但清除锁定状态（锁会移除）
            isLocked = false;
            UpdateLockButtonText();
        }

        // 更新商店显示
        UpdateShop(hadLockedSkills);

        // 更新颜色区域和背包
        UpdateColorAreas();
        UpdateBackpack();

        // 显示界面
        Show();
        shopFilter = FMODUnity.RuntimeManager.CreateInstance("snapshot:/Shop");
        shopFilter.start();
        
        // 开始shop教程
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorial("shop");
        }
    }
    
    /// <summary>
    /// 更新商店显示
    /// </summary>
    private void UpdateShop(bool useLockedSkills = false)
    {
        if (shopParent == null)
            return;

        // 清除旧的商店技能项
        foreach (var item in shopSkillItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        shopSkillItems.Clear();

        List<SkillInfo> shopSkills = new List<SkillInfo>();

        // 如果有锁定的技能，先添加锁定的技能
        if (useLockedSkills && lockedSkillIdentifiers.Count > 0)
        {
            if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
            {
                foreach (var identifier in lockedSkillIdentifiers)
                {
                    if (CSVLoader.Instance.cardInfoMap.ContainsKey(identifier))
                    {
                        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                        // 检查技能是否仍然可以购买或升级
                        bool canBuy = !SkillManager.Instance.HasSkill(identifier) && skillInfo.buyPrice > 0;
                        bool canUpgrade = SkillManager.Instance.HasSkill(identifier) && 
                                         SkillManager.Instance.CanUpgradeSkill(identifier) && 
                                         skillInfo.upgradePrice > 0;
                        if (canBuy || canUpgrade)
                        {
                            shopSkills.Add(skillInfo);
                        }
                    }
                }
            }
            // 清除锁定状态（锁会移除）
            lockedSkillIdentifiers.Clear();
        }

        // 如果技能数量不足3个，补充新技能
        if (shopSkills.Count < 3)
        {
            int needCount = 3 - shopSkills.Count;
            List<SkillInfo> newSkills = GetShopSkills(needCount, shopSkills);
            shopSkills.AddRange(newSkills);
        }

        // 创建商店技能项
        foreach (var skillInfo in shopSkills)
        {
            CreateShopSkillItem(skillInfo);
        }
        
        // 更新金币显示
        UpdateGoldDisplay();
    }
    
    /// <summary>
    /// 获取商店技能列表（随机选择指定数量的技能）
    /// </summary>
    /// <param name="count">需要获取的技能数量，默认为3</param>
    /// <param name="excludeSkills">需要排除的技能列表（例如已锁定的技能）</param>
    private List<SkillInfo> GetShopSkills(int count = 3, List<SkillInfo> excludeSkills = null)
    {
        List<SkillInfo> allAvailableSkills = new List<SkillInfo>();
        
        if (CSVLoader.Instance == null || CSVLoader.Instance.cardInfoMap == null)
            return new List<SkillInfo>();

        // 获取需要排除的identifier集合
        HashSet<string> excludeIdentifiers = new HashSet<string>();
        if (excludeSkills != null)
        {
            foreach (var skill in excludeSkills)
            {
                excludeIdentifiers.Add(skill.identifier);
            }
        }

        // 获取当前关卡等级
        int currentLevel = 1;
        if (LevelManager.Instance != null)
        {
            currentLevel = LevelManager.Instance.CurrentLevel;
        }
        
        foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
        {
            // 排除已锁定的技能
            if (excludeIdentifiers.Contains(skillInfo.identifier))
                continue;

            if (!skillInfo.available)
                continue;
            
            // 检查unlockLevel条件：当前关卡等级必须 >= unlockLevel
            if (skillInfo.unlockLevel > 0 && currentLevel < skillInfo.unlockLevel)
            {
                continue; // 关卡等级不够，不显示此技能
            }
                
            // 检查unlock条件：如果unlock不为空，需要检查所有unlock中的技能是否都已获得
            if (skillInfo.unlock != null && skillInfo.unlock.Count > 0)
            {
                bool allUnlocked = true;
                foreach (var unlockIdentifier in skillInfo.unlock)
                {
                    if (string.IsNullOrEmpty(unlockIdentifier))
                        continue;
                        
                    if (!SkillManager.Instance.HasSkill(unlockIdentifier))
                    {
                        allUnlocked = false;
                        break;
                    }
                }
                
                // 如果有未解锁的前置技能，则不在商店中显示
                if (!allUnlocked)
                    continue;
            }
                
            // 检查是否可以购买或升级
            bool canBuy = !SkillManager.Instance.HasSkill(skillInfo.identifier) && skillInfo.buyPrice > 0;
            bool canUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier) && 
                             SkillManager.Instance.CanUpgradeSkill(skillInfo.identifier) && 
                             skillInfo.upgradePrice > 0;
            
            if (canBuy || canUpgrade)
            {
                allAvailableSkills.Add(skillInfo);
            }
        }

        // 随机选择指定数量的技能
        List<SkillInfo> result = new List<SkillInfo>();
        if (allAvailableSkills.Count == 0)
        {
            return result;
        }

        // 如果可用技能少于需要的数量，返回所有可用技能
        int actualCount = Mathf.Min(count, allAvailableSkills.Count);
        
        // 创建临时列表用于随机选择
        List<SkillInfo> tempList = new List<SkillInfo>(allAvailableSkills);
        
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            result.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return result;
    }
    
    /// <summary>
    /// 创建商店技能项
    /// </summary>
    private void CreateShopSkillItem(SkillInfo skillInfo)
    {
        if (shopSkillItemPrefab == null || shopParent == null)
            return;

        GameObject itemObj = Instantiate(shopSkillItemPrefab, shopParent);
        shopSkillItems.Add(itemObj);

        // 获取组件（假设Prefab包含：技能图标、价格文本、购买按钮）
        // 这里需要根据实际的Prefab结构调整
        // 假设结构：ShopSkillItem组件包含所有需要的引用
        ShopSkillItem shopItem = itemObj.GetComponent<ShopSkillItem>();
        if (shopItem == null)
        {
            shopItem = itemObj.AddComponent<ShopSkillItem>();
        }
        
        shopItem.Init(skillInfo, this);
        
        // 更新锁的显示状态
        shopItem.SetLockVisible(isLocked);
    }
    
    /// <summary>
    /// 购买技能
    /// </summary>
    public void BuySkill(SkillInfo skillInfo)
    {
        if (PlayerManager.Instance == null || SkillManager.Instance == null)
            return;

        bool isUpgrade = SkillManager.Instance.HasSkill(skillInfo.identifier);
        int price = isUpgrade ? skillInfo.upgradePrice : skillInfo.buyPrice;

        // 检查金币是否足够
        if (!PlayerManager.Instance.ConsumeGold(price))
        {
            Debug.LogWarning($"金币不足，无法购买技能: {skillInfo.identifier}");
            return;
        }

        // 升级或获得技能
        SkillManager.Instance.UpgradeSkill(skillInfo.identifier);

        // 如果是新技能，添加到背包
        if (!isUpgrade)
        {
            AddSkillToBackpack(skillInfo.identifier);
        }
        else
        {
            // 如果是升级，高亮对应技能
            HighlightSkill(skillInfo.identifier);
        }

        // 隐藏该技能项（从商店中移除，直到刷新后才会重新出现）
        RemoveShopSkillItem(skillInfo.identifier);
        
        // 更新其他商店技能项的状态（检查是否还能购买）
        UpdateShopSkillItemsState();
        
        UpdateColorAreas();
        UpdateBackpack();
        UpdateGoldDisplay();
        
        // 触发教程信号：第一次购买
        if (!isUpgrade && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SendSignal("purchase");
        }
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_buy_skill");
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
    }
    
    /// <summary>
    /// 更新所有商店技能项的状态（检查是否还能购买）
    /// </summary>
    private void UpdateShopSkillItemsState()
    {
        foreach (var item in shopSkillItems)
        {
            if (item != null)
            {
                ShopSkillItem shopItem = item.GetComponent<ShopSkillItem>();
                if (shopItem != null)
                {
                    shopItem.UpdateState();
                }
            }
        }
    }
    
    /// <summary>
    /// 从商店中移除指定的技能项
    /// </summary>
    private void RemoveShopSkillItem(string identifier)
    {
        // 查找对应的技能项
        GameObject itemToRemove = null;
        foreach (var item in shopSkillItems)
        {
            if (item != null)
            {
                ShopSkillItem shopItem = item.GetComponent<ShopSkillItem>();
                if (shopItem != null && shopItem.SkillIdentifier == identifier)
                {
                    itemToRemove = item;
                    break;
                }
            }
        }
        
        // 移除找到的项
        if (itemToRemove != null)
        {
            shopSkillItems.Remove(itemToRemove);
            Destroy(itemToRemove);
        }
    }
    
    /// <summary>
    /// 刷新商店
    /// </summary>
    private void OnRefreshClicked()
    {
        if (PlayerManager.Instance == null)
            return;

        // 消耗当前价格的金币
        if (!PlayerManager.Instance.ConsumeGold(currentRefreshPrice))
        {
            Debug.LogWarning("金币不足，无法刷新商店");
            return;
        }

        // 增加刷新价格（最高为5）
        if (currentRefreshPrice < 5)
        {
            currentRefreshPrice++;
        }

        // 刷新商店（保留锁定的技能，只刷新未锁定的技能）
        RefreshShop();
        UpdateGoldDisplay();
        UpdateRefreshButton();
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_hold_skill_color");
    }

    /// <summary>
    /// 刷新商店（保留锁定的技能，只刷新未锁定的技能）
    /// </summary>
    private void RefreshShop()
    {
        if (shopParent == null)
            return;

        // 获取锁定的技能信息（用于排除）
        List<SkillInfo> lockedSkills = new List<SkillInfo>();
        if (lockedSkillIdentifiers.Count > 0)
        {
            if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
            {
                foreach (var identifier in lockedSkillIdentifiers)
                {
                    if (CSVLoader.Instance.cardInfoMap.ContainsKey(identifier))
                    {
                        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                        // 检查技能是否仍然可以购买或升级
                        bool canBuy = !SkillManager.Instance.HasSkill(identifier) && skillInfo.buyPrice > 0;
                        bool canUpgrade = SkillManager.Instance.HasSkill(identifier) && 
                                         SkillManager.Instance.CanUpgradeSkill(identifier) && 
                                         skillInfo.upgradePrice > 0;
                        if (canBuy || canUpgrade)
                        {
                            lockedSkills.Add(skillInfo);
                        }
                    }
                }
            }
        }

        // 移除未锁定的技能项
        List<GameObject> itemsToRemove = new List<GameObject>();
        foreach (var item in shopSkillItems)
        {
            if (item != null)
            {
                ShopSkillItem shopItem = item.GetComponent<ShopSkillItem>();
                if (shopItem != null && !lockedSkillIdentifiers.Contains(shopItem.SkillIdentifier))
                {
                    itemsToRemove.Add(item);
                }
            }
        }
        foreach (var item in itemsToRemove)
        {
            shopSkillItems.Remove(item);
            Destroy(item);
        }

        // 计算需要补充的技能数量
        int lockedCount = shopSkillItems.Count;
        int needCount = 3 - lockedCount;

        // 如果技能数量不足3个，补充新技能
        if (needCount > 0)
        {
            List<SkillInfo> newSkills = GetShopSkills(needCount, lockedSkills);
            foreach (var skillInfo in newSkills)
            {
                CreateShopSkillItem(skillInfo);
            }
        }
        
        // 更新金币显示
        UpdateGoldDisplay();
    }

    /// <summary>
    /// 锁定按钮点击事件
    /// </summary>
    private void OnLockClicked()
    {
        isLocked = !isLocked;
        
        if (isLocked)
        {
            // 锁定：保存当前所有商店技能的identifier
            lockedSkillIdentifiers.Clear();
            foreach (var item in shopSkillItems)
            {
                if (item != null)
                {
                    ShopSkillItem shopItem = item.GetComponent<ShopSkillItem>();
                    if (shopItem != null)
                    {
                        lockedSkillIdentifiers.Add(shopItem.SkillIdentifier);
                    }
                }
            }
        }
        else
        {
            // 解锁：清除锁定的技能列表
            lockedSkillIdentifiers.Clear();
        }
        
        UpdateLockButtonText();
        UpdateLockIcons();
        UpdateRefreshButton(); // 更新刷新按钮状态
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
    }

    /// <summary>
    /// 更新锁定按钮文本
    /// </summary>
    private void UpdateLockButtonText()
    {
        if (lockButton != null)
        {
            TMP_Text buttonText = lockButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isLocked ? "Unlock" : "Lock";
            }
        }
    }

    /// <summary>
    /// 更新所有技能项的锁图标显示
    /// </summary>
    private void UpdateLockIcons()
    {
        foreach (var item in shopSkillItems)
        {
            if (item != null)
            {
                ShopSkillItem shopItem = item.GetComponent<ShopSkillItem>();
                if (shopItem != null)
                {
                    shopItem.SetLockVisible(isLocked);
                }
            }
        }
    }

    /// <summary>
    /// 更新刷新按钮显示
    /// </summary>
    private void UpdateRefreshButton()
    {
        if (refreshButton != null)
        {
            TMP_Text buttonText = refreshButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Refresh({currentRefreshPrice})";
            }

            // 更新按钮可交互状态（检查金币是否足够）
            // 如果有锁定的技能，仍然可以刷新（只刷新未锁定的技能）
            // 但如果所有技能都被锁定（3个），则禁用刷新按钮
            if (PlayerManager.Instance != null)
            {
                bool allLocked = lockedSkillIdentifiers.Count >= 3;
                bool canRefresh = PlayerManager.Instance.Gold >= currentRefreshPrice && !allLocked;
                refreshButton.interactable = canRefresh;
            }
        }
    }
    
    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null && PlayerManager.Instance != null)
        {
            goldText.text = $"Gold: {PlayerManager.Instance.Gold}";
        }
        
        // 同时更新刷新按钮状态
        UpdateRefreshButton();
    }

    /// <summary>
    /// 确认按钮点击事件
    /// </summary>
    private void OnConfirmClicked()
    {
        // 玩家等级+1，进入下一关
        MainGameManager mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager != null)
        {
            //mainGameManager.PlayerLevelUp();
            shopFilter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            shopFilter.release();
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
        }

        // 隐藏界面
        Hide();

        // 回调
        onConfirm?.Invoke();
    }
    
    /// <summary>
    /// 统计按钮点击事件
    /// </summary>
    private void OnStatisticsClicked()
    {
        StatisticsMenu statisticsMenu = FindObjectOfType<StatisticsMenu>();
        if (statisticsMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("StatisticsMenu");
            statisticsMenu = menuObj.AddComponent<StatisticsMenu>();
        }
        statisticsMenu.ShowLastRoundStatistics();
        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_place_skill_color");
    }


    /// <summary>
    /// 高亮技能图标
    /// </summary>
    private void HighlightSkill(string identifier)
    {
        // 查找技能图标（可能在背包或颜色区域）
        if (skillIconMap.ContainsKey(identifier))
        {
            SkillIconUI icon = skillIconMap[identifier];
            if (icon != null)
            {
                icon.StartHighlight();
            }
        }
    }

    /// <summary>
    /// 更新颜色区域显示
    /// </summary>
    private void UpdateColorAreas()
    {
        if (PlayerManager.Instance == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            if (colorArea[i] == null)
                continue;

            List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(i);
            int maxSlots = PlayerManager.Instance.GetWaveMaxSlotCount(i);
            
            // 更新slot数量显示
            if (colorArea[i].slotText != null)
            {
                colorArea[i].slotText.text = $"{skillIdentifiers.Count}/{maxSlots}";
            }

            // 更新技能图标显示
            if (colorArea[i].slotParent != null)
            {
                // 清除旧的图标
                foreach (Transform child in colorArea[i].slotParent)
                {
                    SkillIconUI icon = child.GetComponent<SkillIconUI>();
                    if (icon != null)
                    {
                        skillIconMap.Remove(icon.SkillIdentifier);
                        Destroy(child.gameObject);
                    }
                }

                // 创建新的图标
                foreach (var identifier in skillIdentifiers)
                {
                    CreateSkillIcon(identifier, colorArea[i].slotParent, i);
                }       
            }
        }
    }

    /// <summary>
    /// 更新背包显示
    /// </summary>
    private void UpdateBackpack()
    {
        if (backpackParent == null || SkillManager.Instance == null || PlayerManager.Instance == null)
            return;

        // 获取所有已拥有但未分配到颜色区域的技能
        List<string> unassignedSkills = GetUnassignedSkills();

        // 清除旧的图标（只清除在背包中的）
        List<SkillIconUI> iconsToRemove = new List<SkillIconUI>();
        foreach (Transform child in backpackParent)
        {
            Destroy(child.gameObject);
        }
        

        // 创建新的图标
        foreach (var identifier in unassignedSkills)
        {
            CreateSkillIcon(identifier, backpackParent, -1); // -1表示在背包中
        }
    }

    /// <summary>
    /// 获取所有未分配的技能
    /// </summary>
    private List<string> GetUnassignedSkills()
    {
        List<string> result = new List<string>();
        
        if (SkillManager.Instance == null || PlayerManager.Instance == null)
            return result;

        // 获取所有已拥有的技能
        HashSet<string> allOwnedSkills = new HashSet<string>();
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var kvp in CSVLoader.Instance.cardInfoMap)
            {
                if (SkillManager.Instance.HasSkill(kvp.Key))
                {
                    allOwnedSkills.Add(kvp.Key);
                }
            }
        }

        // 获取所有已分配的技能
        HashSet<string> assignedSkills = new HashSet<string>();
        for (int i = 0; i < 4; i++)
        {
            List<string> skills = PlayerManager.Instance.GetWaveSkills(i);
            foreach (var skill in skills)
            {
                assignedSkills.Add(skill);
            }
        }

        // 计算未分配的技能
        foreach (var skill in allOwnedSkills)
        {
            if (!assignedSkills.Contains(skill))
            {
                result.Add(skill);
            }
        }

        return result;
    }

    /// <summary>
    /// 创建技能图标
    /// </summary>
    private void CreateSkillIcon(string identifier, Transform parent, int colorIndex)
    {
        if (skillIconPrefab == null || parent == null)
            return;

        GameObject iconObj = Instantiate(skillIconPrefab, parent);
        SkillIconUI icon = iconObj.GetComponent<SkillIconUI>();
        if (icon == null)
        {
            icon = iconObj.AddComponent<SkillIconUI>();
        }

        icon.Init(identifier, colorIndex, this);
        skillIconMap[identifier] = icon;
    }

    /// <summary>
    /// 将技能添加到背包
    /// </summary>
    private void AddSkillToBackpack(string identifier)
    {
        if (backpackParent == null)
            return;

        // 如果技能已经在某个颜色区域，先移除
        RemoveSkillFromColorArea(identifier);

        // 添加到背包
        CreateSkillIcon(identifier, backpackParent, -1);
    }

    /// <summary>
    /// 从颜色区域移除技能
    /// </summary>
    private void RemoveSkillFromColorArea(string identifier)
    {
        if (PlayerManager.Instance == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            List<string> skills = PlayerManager.Instance.GetWaveSkills(i);
            if (skills.Contains(identifier))
            {
                skills.Remove(identifier);
                PlayerManager.Instance.SetWaveSkills(i, skills);
                break;
            }
        }
    }

    /// <summary>
    /// 颜色区域按钮悬停事件
    /// </summary>
    private void OnColorAreaButtonHover(int colorIndex, bool isEntering)
    {
        if (skillDetailPanel == null || skillDetailText == null)
            return;

        if (isEntering)
        {
            // 显示该颜色所有技能的详情
            if (PlayerManager.Instance != null && SkillManager.Instance != null)
            {
                List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(colorIndex);
                string detailText = "";
                foreach (var identifier in skillIdentifiers)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
                        detailText += description + "\n";
                    }
                }

                if (!string.IsNullOrEmpty(detailText))
                {
                    skillDetailText.text = detailText;
                    // 根据颜色索引设置panel背景色
                    SetSkillPanelColorByIndex(skillDetailPanel, colorIndex);
                    skillDetailPanel.SetActive(true);
                }
            }
        }
        else
        {
            skillDetailPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 开始拖拽技能图标
    /// </summary>
    public void StartDragSkill(SkillIconUI icon)
    {
        draggingIcon = icon;
        originalParent = icon.transform.parent;
        originalSiblingIndex = icon.transform.GetSiblingIndex();
        originalColorIndex = icon.ColorIndex;

        // 创建临时拖拽图标，放在Menu下
        if (skillIconPrefab != null && menu != null)
        {
            GameObject tempObj = Instantiate(skillIconPrefab, menu.transform);
            tempDragIcon = tempObj.GetComponent<SkillIconUI>();
            if (tempDragIcon == null)
            {
                tempDragIcon = tempObj.AddComponent<SkillIconUI>();
            }

            // 初始化临时图标（不设置parentMenu，避免触发拖拽逻辑）
            tempDragIcon.Init(icon.SkillIdentifier, icon.ColorIndex, null);
            
            // 设置临时图标的位置和大小
            RectTransform tempRect = tempObj.GetComponent<RectTransform>();
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            if (tempRect != null && iconRect != null)
            {
                tempRect.sizeDelta = iconRect.sizeDelta;
                tempRect.position = iconRect.position;
            }

            // 设置临时图标的CanvasGroup，使其可以拖拽
            CanvasGroup tempCanvasGroup = tempObj.GetComponent<CanvasGroup>();
            if (tempCanvasGroup == null)
            {
                tempCanvasGroup = tempObj.AddComponent<CanvasGroup>();
            }
            tempCanvasGroup.blocksRaycasts = false; // 不阻挡射线检测

            // 设置为顶层
            tempObj.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 更新拖拽中临时图标的位置
    /// </summary>
    public void UpdateDragPosition(PointerEventData eventData)
    {
        if (tempDragIcon == null)
            return;

        currentDragEventData = eventData;

        RectTransform tempRect = tempDragIcon.GetComponent<RectTransform>();
        Canvas canvas = tempDragIcon.GetComponentInParent<Canvas>();
        if (tempRect != null && canvas != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint);

            tempRect.position = canvas.transform.TransformPoint(localPoint);
        }
    }

    /// <summary>
    /// 拖拽技能图标到目标位置
    /// </summary>
    public void DropSkill(SkillIconUI icon, Transform targetParent, int targetColorIndex, int targetSlotIndex = -1)
    {
        if (draggingIcon == null)
            return;

        // 销毁临时拖拽图标
        if (tempDragIcon != null)
        {
            Destroy(tempDragIcon.gameObject);
            tempDragIcon = null;
        }

        string identifier = draggingIcon.SkillIdentifier;

        // 如果目标颜色区域已满，禁止放入
        if (targetColorIndex >= 0 && targetColorIndex < 4)
        {
            if (PlayerManager.Instance != null)
            {
                List<string> currentSkills = PlayerManager.Instance.GetWaveSkills(targetColorIndex);
                int maxSlots = PlayerManager.Instance.GetWaveMaxSlotCount(targetColorIndex);
                
                // 如果技能已经在目标颜色区域，不需要检查数量
                if (!currentSkills.Contains(identifier) && currentSkills.Count >= maxSlots)
                {
                    // 放回原位置
                    ReturnIconToOriginalPosition(icon);
                    return;
                }
                FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_hold_skill_color");
            }
        }

        // 如果目标和原位置相同，不处理
        if (targetParent == originalParent)
        {
            // 重置拖拽状态
            draggingIcon = null;
            originalParent = null;
            originalSiblingIndex = -1;
            originalColorIndex = -1;
            return;
        }

        // 从原位置移除
        if (originalColorIndex >= 0 && originalColorIndex < 4)
        {
            // 从颜色区域移除
            if (PlayerManager.Instance != null)
            {
                List<string> skills = PlayerManager.Instance.GetWaveSkills(originalColorIndex);
                skills.Remove(identifier);
                PlayerManager.Instance.SetWaveSkills(originalColorIndex, skills);
            }
        }
        // 如果原位置是背包（originalColorIndex == -1），不需要从PlayerManager移除，只需要更新UI即可

        // 添加到目标位置
        bool draggedToColor = false;
        if (targetColorIndex >= 0 && targetColorIndex < 4)
        {
            // 添加到颜色区域
            if (PlayerManager.Instance != null)
            {
                List<string> skills = PlayerManager.Instance.GetWaveSkills(targetColorIndex);
                if (!skills.Contains(identifier))
                {
                    if (targetSlotIndex >= 0 && targetSlotIndex < skills.Count)
                    {
                        skills.Insert(targetSlotIndex, identifier);
                    }
                    else
                    {
                        skills.Add(identifier);
                    }
                    PlayerManager.Instance.SetWaveSkills(targetColorIndex, skills);
                    draggedToColor = true; // 标记为拖动到颜色区域
                }
            }
        }
        else
        {
            
        }

        // 更新UI
        UpdateColorAreas();
        UpdateBackpack();

        // 触发教程信号：第一次拖动到颜色区域（从背包拖动到颜色区域）
        if (draggedToColor && originalColorIndex == -1 && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SendSignal("dragToColor");
        }

        // 重置拖拽状态
        draggingIcon = null;
        originalParent = null;
        originalSiblingIndex = -1;
        originalColorIndex = -1;
    }

    /// <summary>
    /// 将图标放回原位置
    /// </summary>
    private void ReturnIconToOriginalPosition(SkillIconUI icon)
    {
        // 销毁临时图标
        if (tempDragIcon != null)
        {
            Destroy(tempDragIcon.gameObject);
            tempDragIcon = null;
        }

        // 恢复原始图标的透明度
        if (draggingIcon != null && draggingIcon != icon)
        {
            CanvasGroup canvasGroup = draggingIcon.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        draggingIcon = null;
        originalParent = null;
        originalSiblingIndex = -1;
        originalColorIndex = -1;
    }

    /// <summary>
    /// 处理拖拽结束
    /// </summary>
    public void EndDragSkill(PointerEventData eventData)
    {
        if (draggingIcon == null)
            return;

        // 使用传入的事件数据，如果没有则使用保存的
        PointerEventData dragEventData = eventData != null ? eventData : currentDragEventData;
        if (dragEventData == null)
        {
            // 如果没有事件数据，创建默认的
            dragEventData = new PointerEventData(EventSystem.current);
            dragEventData.position = Input.mousePosition;
        }

        // 销毁临时拖拽图标
        if (tempDragIcon != null)
        {
            Destroy(tempDragIcon.gameObject);
            tempDragIcon = null;
        }

        // 恢复原始图标的透明度
        CanvasGroup canvasGroup = draggingIcon.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // 使用指定的Layer检测拖拽目标
        GameObject dropTarget = GetDropTarget(dragEventData);
        
        if (dropTarget != null)
        {
            DragDropTarget target = dropTarget.GetComponent<DragDropTarget>();
            if (target != null)
            {
                Transform targetParent = null;
                int targetColorIndex = -1;

                if (target.targetType == DragDropTarget.TargetType.ColorArea)
                {
                    // 颜色区域
                    if (target.colorIndex >= 0 && target.colorIndex < 4 && colorArea[target.colorIndex] != null)
                    {
                        targetParent = colorArea[target.colorIndex].slotParent;
                        targetColorIndex = target.colorIndex;
                    }
                }
                else if (target.targetType == DragDropTarget.TargetType.Backpack)
                {
                    // 背包
                    targetParent = backpackParent;
                    targetColorIndex = -1;
                }

                if (targetParent != null)
                {
                    // 计算slot索引
                    int slotIndex = GetSlotIndex(dropTarget.transform, targetParent);
                    DropSkill(draggingIcon, targetParent, targetColorIndex, slotIndex);
                    return;
                }
            }
        }

        // 如果没有有效目标，放回原位置
        if (originalParent != null)
        {
            DropSkill(draggingIcon, originalParent, originalColorIndex);
        }
        else
        {
            // 重置拖拽状态
            draggingIcon = null;
            originalParent = null;
            originalSiblingIndex = -1;
            originalColorIndex = -1;
        }
    }

    /// <summary>
    /// 获取拖拽目标（使用指定的Layer）
    /// </summary>
    private GameObject GetDropTarget(PointerEventData eventData)
    {
        // 使用Physics2D或Physics射线检测指定Layer的对象
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            int layerMask = 1 << dragDropLayer;
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, layerMask);
            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }
        }

        // 如果Physics检测失败，尝试使用EventSystem检测
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = eventData.position;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.layer == dragDropLayer)
                {
                    return result.gameObject;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 计算slot索引
    /// </summary>
    private int GetSlotIndex(Transform target, Transform parent)
    {
        // 如果目标就是parent，返回-1（添加到末尾）
        if (target == parent)
            return -1;

        // 查找目标在parent中的位置
        int index = 0;
        foreach (Transform child in parent)
        {
            if (child == target)
                return index;
            index++;
        }

        return -1;
    }

    /// <summary>
    /// 显示技能详情（用于技能图标悬停）
    /// </summary>
    public void ShowSkillDetail(string identifier, int colorIndex = -1)
    {
        if (skillDetailPanel == null || skillDetailText == null || SkillManager.Instance == null)
            return;

        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
        if (!string.IsNullOrEmpty(description))
        {
            skillDetailText.text = description;
            // 根据技能颜色或位置设置panel背景色
            SetSkillPanelColor(skillDetailPanel, identifier, colorIndex);
            skillDetailPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 根据技能颜色设置panel背景色
    /// </summary>
    private void SetSkillPanelColor(GameObject panel, string skillIdentifier, int colorIndex = -1)
    {
        if (panel == null || string.IsNullOrEmpty(skillIdentifier))
            return;
        
        // 获取panel的Image组件（背景）
        Image bgImage = panel.GetComponent<Image>();
        if (bgImage == null)
            return;
        
        // 如果技能在背包中（colorIndex < 0 或 >= 4），使用#FFF0A7颜色
        if (colorIndex < 0 || colorIndex >= 4)
        {
            bgImage.color = TileColorUtil.HexToColor("#FFF0A7");
            return;
        }
        
        // 优先使用colorIndex对应的颜色（技能实际在的颜色区域）
        if (colorIndex >= 0 && colorIndex < 4)
        {
            TileColor tileColor = (TileColor)colorIndex;
            Color colorValue = TileColorUtil.GetUnityColor(tileColor);
            bgImage.color = colorValue;
            return;
        }
        
        // 如果colorIndex无效，回退到使用CSV中的颜色
        if (CSVLoader.Instance == null || !CSVLoader.Instance.cardInfoMap.ContainsKey(skillIdentifier))
        {
            // 如果没有找到技能信息，使用默认颜色（白色）
            bgImage.color = Color.white;
            return;
        }
        
        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[skillIdentifier];
        
        // 如果技能有颜色，使用对应颜色；否则使用默认颜色（白色）
        if (!string.IsNullOrEmpty(skillInfo.color))
        {
            // 将颜色字符串转换为TileColor
            string colorLower = skillInfo.color.ToLower();
            if (colorLower == "red" || colorLower == "yellow" || colorLower == "blue" || colorLower == "green")
            {
                TileColor tileColor = GetTileColorFromString(skillInfo.color);
                Color colorValue = TileColorUtil.GetUnityColor(tileColor);
                bgImage.color = colorValue;
            }
            else
            {
                // 无效颜色，使用默认颜色（白色）
                bgImage.color = Color.white;
            }
        }
        else
        {
            // 没有颜色，使用默认颜色（白色）
            bgImage.color = Color.white;
        }
    }
    
    /// <summary>
    /// 根据颜色索引设置panel背景色（用于颜色区域悬停）
    /// </summary>
    private void SetSkillPanelColorByIndex(GameObject panel, int colorIndex)
    {
        if (panel == null)
            return;
        
        // 获取panel的Image组件（背景）
        Image bgImage = panel.GetComponent<Image>();
        if (bgImage == null)
            return;
        
        // 根据颜色索引设置颜色（0=红，1=黄，2=蓝，3=绿）
        if (colorIndex >= 0 && colorIndex < 4)
        {
            TileColor tileColor = (TileColor)colorIndex;
            Color colorValue = TileColorUtil.GetUnityColor(tileColor);
            bgImage.color = colorValue;
        }
        else
        {
            // 无效索引，使用默认颜色（白色）
            bgImage.color = Color.white;
        }
    }
    
    /// <summary>
    /// 将颜色字符串转换为TileColor
    /// </summary>
    private TileColor GetTileColorFromString(string colorStr)
    {
        switch (colorStr.ToLower())
        {
            case "red": return TileColor.Red;
            case "yellow": return TileColor.Yellow;
            case "blue": return TileColor.Blue;
            case "green": return TileColor.Green;
            default: return TileColor.Red;
        }
    }

    /// <summary>
    /// 隐藏技能详情
    /// </summary>
    public void HideSkillDetail()
    {
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 设置关闭按钮的启用状态（用于教程）
    /// </summary>
    public void SetCloseButtonEnabled(bool enabled)
    {
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(enabled);
        }
    }
    
    /// <summary>
    /// 设置刷新按钮的启用状态（用于教程）
    /// </summary>
    public void SetRefreshButtonEnabled(bool enabled)
    {
        if (refreshButton != null)
        {
            refreshButton.gameObject.SetActive(enabled);
        }
    }
}
