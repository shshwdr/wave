using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 三选一技能选择界面
/// </summary>
public class SkillSelectMenu : MenuBase
{
    [Header("UI组件")]
    [SerializeField] private Transform buttonParent;
    [SerializeField] private TMP_Text titleText;

    private Button[] skillButtons;
    private TMP_Text[] skillTexts;
    private List<SkillInfo> selectedSkills = new List<SkillInfo>();
    private Action<SkillInfo> onSkillSelected;

    protected override void Awake()
    {
        base.Awake();
        
        // 从parent下查找所有Button
        if (buttonParent != null)
        {
            skillButtons = buttonParent.GetComponentsInChildren<Button>();
            
            // 为每个按钮查找TMP_Text并绑定事件
            skillTexts = new TMP_Text[skillButtons.Length];
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i; // 闭包变量
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index));
                
                // 从按钮内查找TMP_Text
                skillTexts[i] = skillButtons[i].GetComponentInChildren<TMP_Text>();
            }
            
            Debug.Log($"找到 {skillButtons.Length} 个技能按钮");
        }
        else
        {
            Debug.LogWarning("buttonParent未设置！");
        }
    }

    /// <summary>
    /// 显示技能选择界面
    /// </summary>
    public void ShowSkillSelection(Action<SkillInfo> onSelected)
    {
        onSkillSelected = onSelected;
        selectedSkills.Clear();

        // 获取可选择的技能列表（未拥有的和可升级的）
        List<SkillInfo> availableSkills = SkillManager.Instance.GetAvailableSkillsForSelection();

        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("没有可选择的技能！");
            return;
        }

        // 随机选择技能（最多选择按钮数量）
        List<SkillInfo> randomSkills = new List<SkillInfo>();
        List<SkillInfo> tempList = new List<SkillInfo>(availableSkills);

        int maxCount = skillButtons != null ? skillButtons.Length : 3;
        int count = Mathf.Min(maxCount, availableSkills.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            randomSkills.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        selectedSkills = randomSkills;

        // 更新UI显示
        UpdateSkillButtons();

        // 显示界面
        Show();
    }

    /// <summary>
    /// 更新技能按钮显示
    /// </summary>
    private void UpdateSkillButtons()
    {
        // 设置标题
        if (titleText != null)
        {
            titleText.text = "Skill Selection";
        }

        // 更新所有按钮
        if (skillButtons == null || skillTexts == null)
            return;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (i < selectedSkills.Count)
            {
                // 有技能数据，显示按钮和技能信息
                SkillInfo skill = selectedSkills[i];
                string description = SkillManager.Instance.GetSkillDescription(skill.identifier,true);
                
                if (skillTexts[i] != null)
                {
                    skillTexts[i].text = skill.name + "\n" + description;
                }
                
                skillButtons[i].gameObject.SetActive(true);
            }
            else
            {
                // 没有技能数据，隐藏按钮
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnSkillButtonClicked(int index)
    {
        if (index < 0 || index >= selectedSkills.Count)
            return;

        SkillInfo selectedSkill = selectedSkills[index];
        
        // 升级或获得技能
        SkillManager.Instance.UpgradeSkill(selectedSkill.identifier);
        
        Debug.Log($"选择了技能: {selectedSkill.identifier}");
        
        // 回调
        onSkillSelected?.Invoke(selectedSkill);
        
        // 隐藏界面
        Hide();
    }
}

