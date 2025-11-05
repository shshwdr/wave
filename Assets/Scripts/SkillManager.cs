using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能管理器 - 记录所有技能和等级
/// </summary>
public class SkillManager : Singleton<SkillManager>
{
    // 技能标识符 -> 技能等级（0表示未拥有，1表示1级，2表示2级...）
    private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

    /// <summary>
    /// 初始化技能管理器，加载isStart技能
    /// </summary>
    public void Init()
    {
        skillLevels.Clear();
        
        // 加载所有标记为isStart的技能
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
            {
                if (skillInfo.isStart)
                {
                    skillLevels[skillInfo.identifier] = 1; // isStart技能初始为1级
                    Debug.Log($"初始技能: {skillInfo.identifier} - 等级 1");
                }
            }
        }
    }

    /// <summary>
    /// 获取技能等级（0表示未拥有）
    /// </summary>
    public int GetSkillLevel(string identifier)
    {
        if (skillLevels.ContainsKey(identifier))
        {
            return skillLevels[identifier];
        }
        return 0;
    }

    /// <summary>
    /// 设置技能等级
    /// </summary>
    public void SetSkillLevel(string identifier, int level)
    {
        skillLevels[identifier] = level;
    }

    /// <summary>
    /// 升级技能（如果已拥有则升级，否则获得）
    /// </summary>
    public void UpgradeSkill(string identifier)
    {
        if (skillLevels.ContainsKey(identifier))
        {
            SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
            int currentLevel = skillLevels[identifier];
            if (currentLevel < skillInfo.maxLevel)
            {
                skillLevels[identifier] = currentLevel + 1;
                Debug.Log($"技能升级: {identifier} - 等级 {skillLevels[identifier]}");
            }
        }
        else
        {
            // 获得新技能
            skillLevels[identifier] = 1;
            Debug.Log($"获得新技能: {identifier} - 等级 1");
        }
    }

    /// <summary>
    /// 检查技能是否已拥有
    /// </summary>
    public bool HasSkill(string identifier)
    {
        return skillLevels.ContainsKey(identifier) && skillLevels[identifier] > 0;
    }

    /// <summary>
    /// 检查技能是否可以升级
    /// </summary>
    public bool CanUpgradeSkill(string identifier)
    {
        if (!skillLevels.ContainsKey(identifier) || skillLevels[identifier] == 0)
        {
            return true; // 未拥有，可以获取
        }
        
        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
        return skillLevels[identifier] < skillInfo.maxLevel;
    }

    /// <summary>
    /// 获取指定颜色的所有技能（包括未拥有的）
    /// </summary>
    public List<SkillInfo> GetSkillsByColor(string color)
    {
        List<SkillInfo> result = new List<SkillInfo>();
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
            {
                if (skillInfo.color == color)
                {
                    result.Add(skillInfo);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取指定颜色的所有已拥有技能
    /// </summary>
    public List<SkillInfo> GetOwnedSkillsByColor(string color)
    {
        List<SkillInfo> result = new List<SkillInfo>();
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
            {
                if (skillInfo.color == color && HasSkill(skillInfo.identifier))
                {
                    result.Add(skillInfo);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有可选择的技能（未拥有的和可升级的）
    /// </summary>
    public List<SkillInfo> GetAvailableSkillsForSelection()
    {
        List<SkillInfo> result = new List<SkillInfo>();
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
            foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
            {
                if (CanUpgradeSkill(skillInfo.identifier))
                {
                    result.Add(skillInfo);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取技能当前等级的值
    /// </summary>
    public int GetSkillValue(string identifier)
    {
        if (!HasSkill(identifier))
        {
            return 0;
        }

        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
        int level = skillLevels[identifier];
        
        if (skillInfo.values != null && skillInfo.values.Count > 0)
        {
            // 等级从1开始，数组索引从0开始
            int index = level - 1;
            if (index >= 0 && index < skillInfo.values.Count)
            {
                return skillInfo.values[index];
            }
        }
        
        return 0;
    }
    public int GetNextSkillValue(string identifier)
    {
        int level = 0;
        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier]; 
        if (HasSkill(identifier))
        {
            level = skillLevels[identifier];
        }

        level++;
        
        if (skillInfo.values != null && skillInfo.values.Count > 0)
        {
            // 等级从1开始，数组索引从0开始
            int index = level - 1;
            if (index >= 0 && index < skillInfo.values.Count)
            {
                return skillInfo.values[index];
            }
        }
        
        return 0;
    }

    /// <summary>
    /// 获取技能描述（替换{0}为当前等级的值）
    /// </summary>
    public string GetSkillDescription(string identifier, bool showNext)
    {
        if (!CSVLoader.Instance.cardInfoMap.ContainsKey(identifier))
        {
            return "";
        }

        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
        int currentValue = GetSkillValue(identifier);

        var valueString = currentValue.ToString();
        if (showNext)
        {
            
            int nextValue = GetNextSkillValue(identifier);
            valueString = nextValue.ToString();
            if (HasSkill(identifier))
            {
                valueString = $"{currentValue} -> {nextValue}";
            }
        }

        if (string.IsNullOrEmpty(skillInfo.description))
        {
            return skillInfo.name;
        }

        // 替换{0}为当前等级的值
        return skillInfo.description.Replace("{0}", valueString);
    }
}

