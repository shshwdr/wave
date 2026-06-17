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
    /// 获取所有可选择的技能（未拥有的和可升级的，且available为true）
    /// </summary>
    public List<SkillInfo> GetAvailableSkillsForSelection()
    {
        List<SkillInfo> result = new List<SkillInfo>();
        if (CSVLoader.Instance != null && CSVLoader.Instance.cardInfoMap != null)
        {
        // 获取当前关卡等级
        int currentLevel = 1;
        if (LevelManager.Instance != null)
        {
            currentLevel = LevelManager.Instance.CurrentLevel;
        }

        foreach (var skillInfo in CSVLoader.Instance.cardInfoMap.Values)
        {
            // 只有available为true且可以升级的技能才能被选择
            if (!skillInfo.available || !CanUpgradeSkill(skillInfo.identifier))
                continue;
            
            // 检查unlockLevel条件：当前关卡等级必须 >= unlockLevel
            if (skillInfo.unlockLevel > 0 && currentLevel < skillInfo.unlockLevel)
            {
                continue; // 关卡等级不够，不显示此技能
            }
            
            result.Add(skillInfo);
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
    /// 获取技能名字（根据等级格式化）
    /// 1级：正常显示，2级：名字+，3级及以上：名字+2、名字+3等
    /// </summary>
    /// <param name="identifier">技能标识符</param>
    /// <param name="useNextLevel">是否使用下一个等级（用于购买界面）</param>
    public string GetSkillName(string identifier, bool useNextLevel = false)
    {
        if (!CSVLoader.Instance.cardInfoMap.ContainsKey(identifier))
        {
            return "";
        }

        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
        int level = GetSkillLevel(identifier);
        
        // 如果使用下一个等级（购买界面）
        if (useNextLevel)
        {
            if (level == 0)
            {
                level = 1; // 未拥有，显示1级
            }
            else
            {
                level = level + 1; // 已拥有，显示下一个等级
            }
        }
        
        // 如果等级为0（未拥有且不使用下一个等级），显示1级
        if (level == 0)
        {
            level = 1;
        }
        
        // 根据等级格式化名字
        if (level == 1)
        {
            return skillInfo.name; // 1级：正常显示
        }
        else if (level == 2)
        {
            return skillInfo.name + "+"; // 2级：名字+
        }
        else
        {
            return skillInfo.name + "+" + (level - 1).ToString(); // 3级及以上：名字+2、名字+3等
        }
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
            return GetSkillName(identifier, false);
        }

        // 替换{0}为当前等级的值
        return skillInfo.description.Replace("{0}", valueString);
    }

    /// <summary>
    /// 构建颜色区域所有技能的描述文本（每条前加列表分隔圆点）
    /// </summary>
    public string BuildColorAreaSkillDescriptions(IList<string> skillIdentifiers, bool showNext = false)
    {
        if (skillIdentifiers == null || skillIdentifiers.Count == 0)
            return "";

        var lines = new List<string>();
        foreach (var identifier in skillIdentifiers)
        {
            if (!HasSkill(identifier))
                continue;

            string description = GetSkillDescription(identifier, showNext);
            if (!string.IsNullOrEmpty(description))
                lines.Add("• " + description);
        }

        return string.Join("\n", lines);
    }
}

