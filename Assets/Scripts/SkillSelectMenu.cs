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
    [Header("三选一区域")]
    [SerializeField] private Transform threeChoiceParent;
    private Button[] threeChoiceButtons;
    private TMP_Text[] threeChoiceTexts;

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

    private List<SkillInfo> selectedSkills = new List<SkillInfo>(); // 三选一的技能列表
    private Action<SkillInfo> onSkillSelected; // 三选一选择后的回调
    private Action onConfirm; // 确认按钮的回调
    private Dictionary<string, SkillIconUI> skillIconMap = new Dictionary<string, SkillIconUI>(); // 技能identifier -> UI实例
    private bool threeChoiceCompleted = false; // 三选一是否已完成

    // 拖拽相关
    private SkillIconUI draggingIcon = null;
    private SkillIconUI tempDragIcon = null; // 临时拖拽图标
    private Transform originalParent = null;
    private int originalSiblingIndex = -1;
    private int originalColorIndex = -1; // -1表示在背包中
    private PointerEventData currentDragEventData = null; // 当前拖拽事件数据
    
    [Header("拖拽设置")]
    [SerializeField] private int dragDropLayer = 0; // 拖拽目标检测的Layer

    protected override void Awake()
    {
        base.Awake();
        dragDropLayer = LayerMask.NameToLayer("DropTarget");
        // 初始化三选一按钮
        if (threeChoiceParent != null)
        {
            threeChoiceButtons = threeChoiceParent.GetComponentsInChildren<Button>();
            threeChoiceTexts = new TMP_Text[threeChoiceButtons.Length];
            for (int i = 0; i < threeChoiceButtons.Length; i++)
            {
                int index = i;
                threeChoiceButtons[i].onClick.AddListener(() => OnThreeChoiceClicked(index));
                threeChoiceTexts[i] = threeChoiceButtons[i].GetComponentInChildren<TMP_Text>();
            }
        }

        // 初始化颜色区域的详情按钮
        for (int i = 0; i < 4; i++)
        {
            int colorIndex = i;
            if (colorArea[i] != null && colorArea[i].button != null)
            {
                // 添加鼠标悬停事件
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
    }

    /// <summary>
    /// 显示技能选择界面
    /// </summary>
    public void ShowSkillSelection(Action<SkillInfo> onSelected, Action onConfirmCallback = null)
    {
        onSkillSelected = onSelected;
        onConfirm = onConfirmCallback;
        selectedSkills.Clear();
        threeChoiceCompleted = false;

        // 获取可选择的技能列表（未拥有的和可升级的）
        List<SkillInfo> availableSkills = SkillManager.Instance.GetAvailableSkillsForSelection();

        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("没有可选择的技能！");
            // 如果没有可选择的技能，直接隐藏三选一区域
            if (threeChoiceParent != null)
            {
                threeChoiceParent.gameObject.SetActive(false);
            }
            threeChoiceCompleted = true;
        }
        else
        {
            // 随机选择技能（最多选择按钮数量）
            List<SkillInfo> randomSkills = new List<SkillInfo>();
            List<SkillInfo> tempList = new List<SkillInfo>(availableSkills);

            int maxCount = threeChoiceButtons != null ? threeChoiceButtons.Length : 3;
            int count = Mathf.Min(maxCount, availableSkills.Count);
            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, tempList.Count);
                randomSkills.Add(tempList[randomIndex]);
                tempList.RemoveAt(randomIndex);
            }

            selectedSkills = randomSkills;
            UpdateThreeChoiceButtons();
            threeChoiceParent.gameObject.SetActive(true);
        }

        // 更新颜色区域和背包
        UpdateColorAreas();
        UpdateBackpack();

        // 显示界面
        Show();
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
            mainGameManager.PlayerLevelUp();
        }

        // 隐藏界面
        Hide();

        // 回调
        onConfirm?.Invoke();
    }

    /// <summary>
    /// 更新三选一按钮显示
    /// </summary>
    private void UpdateThreeChoiceButtons()
    {
        if (threeChoiceButtons == null || threeChoiceTexts == null)
            return;

        for (int i = 0; i < threeChoiceButtons.Length; i++)
        {
            if (i < selectedSkills.Count)
            {
                SkillInfo skill = selectedSkills[i];
                string description = SkillManager.Instance.GetSkillDescription(skill.identifier, true);
                
                if (threeChoiceTexts[i] != null)
                {
                    threeChoiceTexts[i].text = description;
                }
                
                threeChoiceButtons[i].gameObject.SetActive(true);
            }
            else
            {
                threeChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 三选一按钮点击事件
    /// </summary>
    private void OnThreeChoiceClicked(int index)
    {
        if (index < 0 || index >= selectedSkills.Count)
            return;

        SkillInfo selectedSkill = selectedSkills[index];
        
        // 检查是否是升级（已拥有该技能）
        bool isUpgrade = SkillManager.Instance.HasSkill(selectedSkill.identifier);
        
        // 升级或获得技能
        SkillManager.Instance.UpgradeSkill(selectedSkill.identifier);
        
        Debug.Log($"选择了技能: {selectedSkill.identifier}, 是否升级: {isUpgrade}");
        
        if (isUpgrade)
        {
            // 如果是升级，高亮对应技能（无论在背包还是颜色区域）
            HighlightSkill(selectedSkill.identifier);
        }
        else
        {
            // 如果是新技能，添加到背包
            AddSkillToBackpack(selectedSkill.identifier);
        }
        
        // 隐藏三选一区域
        if (threeChoiceParent != null)
        {
            threeChoiceParent.gameObject.SetActive(false);
        }
        threeChoiceCompleted = true;
        
        // 回调
        onSkillSelected?.Invoke(selectedSkill);
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
                }
            }
        }
        else
        {
            
        }

        // 更新UI
        UpdateColorAreas();
        UpdateBackpack();

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
    public void ShowSkillDetail(string identifier)
    {
        if (skillDetailPanel == null || skillDetailText == null || SkillManager.Instance == null)
            return;

        string description = SkillManager.Instance.GetSkillDescription(identifier, false);
        if (!string.IsNullOrEmpty(description))
        {
            skillDetailText.text = description;
            skillDetailPanel.SetActive(true);
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
}
