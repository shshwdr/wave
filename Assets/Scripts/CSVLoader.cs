using System.Collections;
using System.Collections.Generic;
using Sinbad;
using TMPro;
using UnityEngine;

public class SkillInfo
{
    public string identifier;
    public string name;
    public string description;
    public string color; //should  delete this and related code
    public bool isStart;
    public string effect;
    public List<int> values; //count equals level
    public int maxLevel => (values != null && values.Count > 0) ? values.Count : 1;
    public bool available;
    public int buyPrice;
    public int upgradePrice;
    public List<string> unlock;
    public int unlockLevel;

}

public class EnemyInfo
{
    
    public string identifier;
    public string name;
    public string description;
    public int hp;
    public int attack;
    public int speed;
    public int range;
    public List<string> skill;
    public int skillValue;
    public int skillCD;
    public int attackIncrease;
    public int hpIncrease;
    public int gold;
    public Sprite icon=> Resources.Load<Sprite>("enemy/"+identifier);

}

public class LevelInfo
{
    public string enemies;
    public int level;
    public int gold;
    public int startEnemyCount;
    public int difficulty;
    public string bossIdentifier;
    public bool hasEvent;
    public int turns;
    public string type;
    public string typeIdentifier;
    public string eventType;
    public int island;
}

public class IslandInfo
{
    public int island;
    public int battleCount;
    public int eventCount;
    public int shopCount;
    public int healCount;

    public int TotalNodeCount => battleCount + eventCount + shopCount + healCount;
}

public class EventInfo
{
    public string identifier;
    public string name;
    public string description;
    public string option0;
    public string option1;
    public string option2;
    public List<string> result0;
    public List<string> result1;
    public List<string> result2;
    public string desc0;
    public string desc1;
    public string desc2;
    public bool isAvailable;
    public string type;
}

public class TutorialInfo
{
    public string identifier;
    public string text;
    public string dialoguePosition;
    public List<string> actions;
    public string wait;
    public string highlight;
    public bool isEnding;
    public bool isBlocking;
    public TutorialInfo nextInfo; // 下一个教程的引用（从CSV顺序推断，只有不是isEnding的才有next）
}

public class StartAnimInfo
{
    public string identifier;
    public string text;
    public List<string> actions;
    public bool isEnding;
    public StartAnimInfo nextInfo; // 下一个动画的引用（从CSV顺序推断，只有不是isEnding的才有next）
}

public class RuneInfo
{
    public string identifier;
    public string name;
    public string description;
    public string descriptionMore;
    public bool isStart;
    public string effect;
    public int values;
    public bool available;
    public int buyPrice;
    public List<string> unlock;
    public int unlockLevel;
    public string synergy;
    public Sprite icon => Resources.Load<Sprite>("rune/" + identifier);
}

public class ConsumableInfo
{
    public string identifier;
    public string name;
    public string description;
    public string effect;
    public List<int> values;
    public int start;
    public bool available;
    public int maxCount;
    public int unlockLevel;
    public int price;
    public bool isBattleOnly;
    public Sprite icon => Resources.Load<Sprite>("Consumables/" + identifier);
}

public class CSVLoader : Singleton<CSVLoader>
{
    public TMP_FontAsset font;
    public Dictionary<string, SkillInfo> cardInfoMap = new Dictionary<string, SkillInfo>();
    public Dictionary<string, EnemyInfo> enemyInfoMap = new Dictionary<string, EnemyInfo>();
    public Dictionary<int, LevelInfo> levelInfoMap = new Dictionary<int, LevelInfo>();
    public Dictionary<int, IslandInfo> islandInfoMap = new Dictionary<int, IslandInfo>();
    public Dictionary<string, EventInfo> eventInfoMap = new Dictionary<string, EventInfo>();
    public Dictionary<string, TutorialInfo> tutorialInfoMap = new Dictionary<string, TutorialInfo>();
    public List<TutorialInfo> tutorialInfoList = new List<TutorialInfo>(); // 保持CSV顺序的列表
    public Dictionary<string, StartAnimInfo> startAnimInfoMap = new Dictionary<string, StartAnimInfo>();
    public List<StartAnimInfo> startAnimInfoList = new List<StartAnimInfo>(); // 保持CSV顺序的列表
    public Dictionary<string, RuneInfo> runeInfoMap = new Dictionary<string, RuneInfo>();
    public List<RuneInfo> runeInfoList = new List<RuneInfo>();
    public Dictionary<string, ConsumableInfo> consumableInfoMap = new Dictionary<string, ConsumableInfo>();
    // Start is called before the first frame update
    public void Init()
    {
        var cardInfos = CsvUtil.LoadObjects<SkillInfo>("skill");
        foreach (var cardInfo in cardInfos)
        {
            cardInfoMap[cardInfo.identifier] = cardInfo;
        }
        var enemyInfos = CsvUtil.LoadObjects<EnemyInfo>("enemy");
        foreach (var enemyInfo in enemyInfos)
        {
            enemyInfoMap.Add(enemyInfo.identifier, enemyInfo);
        }
        var levelInfos = CsvUtil.LoadObjects<LevelInfo>("level");
        int i = 0;
        foreach (var levelInfo in levelInfos)
        {
            levelInfo.level = i;
            i++;
            levelInfoMap.Add(levelInfo.level, levelInfo);
        }
        var islandInfos = CsvUtil.LoadObjects<IslandInfo>("island");
        foreach (var islandInfo in islandInfos)
        {
            islandInfoMap[islandInfo.island] = islandInfo;
        }
        var eventInfos = CsvUtil.LoadObjects<EventInfo>("event");
        foreach (var eventInfo in eventInfos)
        {
            eventInfoMap.Add(eventInfo.identifier, eventInfo);
        }
        var tutorialInfos = CsvUtil.LoadObjects<TutorialInfo>("tutorial");
        tutorialInfoList = new List<TutorialInfo>(tutorialInfos);
        
        // 设置nextInfo链接，并添加到字典（只有有identifier的才添加到字典）
        for (i = 0; i < tutorialInfoList.Count; i++)
        {
            var tutorialInfo = tutorialInfoList[i];
            
            // 解析actions字段（CSV中用|分隔，CsvUtil应该能自动解析为List<string>）
            // 如果actions为null，初始化为空列表
            if (tutorialInfo.actions == null)
            {
                tutorialInfo.actions = new List<string>();
            }
            
            // 设置下一个教程的引用（只有不是isEnding的才有next）
            if (!tutorialInfo.isEnding && i < tutorialInfoList.Count - 1)
            {
                tutorialInfo.nextInfo = tutorialInfoList[i + 1];
            }
            else
            {
                tutorialInfo.nextInfo = null;
            }
            
            // 只有有identifier的才添加到字典
            if (!string.IsNullOrEmpty(tutorialInfo.identifier))
            {
                tutorialInfoMap.Add(tutorialInfo.identifier, tutorialInfo);
            }
        }
        
        // 加载开场动画数据
        var startAnimInfos = CsvUtil.LoadObjects<StartAnimInfo>("startAnim");
        startAnimInfoList = new List<StartAnimInfo>(startAnimInfos);
        
        // 设置nextInfo链接，并添加到字典（只有有identifier的才添加到字典）
        for (i = 0; i < startAnimInfoList.Count; i++)
        {
            var startAnimInfo = startAnimInfoList[i];
            
            // 解析actions字段（CSV中用|分隔，CsvUtil应该能自动解析为List<string>）
            // 如果actions为null，初始化为空列表
            if (startAnimInfo.actions == null)
            {
                startAnimInfo.actions = new List<string>();
            }
            
            // 设置下一个动画的引用（只有不是isEnding的才有next）
            if (!startAnimInfo.isEnding && i < startAnimInfoList.Count - 1)
            {
                startAnimInfo.nextInfo = startAnimInfoList[i + 1];
            }
            else
            {
                startAnimInfo.nextInfo = null;
            }
            
            // 只有有identifier的才添加到字典
            if (!string.IsNullOrEmpty(startAnimInfo.identifier))
            {
                startAnimInfoMap.Add(startAnimInfo.identifier, startAnimInfo);
            }
        }

        var runeInfos = CsvUtil.LoadObjects<RuneInfo>("rune");
        runeInfoList = new List<RuneInfo>(runeInfos);
        runeInfoMap.Clear();
        foreach (var runeInfo in runeInfoList)
        {
            if (string.IsNullOrEmpty(runeInfo.identifier))
                continue;

            runeInfoMap[runeInfo.identifier] = runeInfo;
        }

        var consumableInfos = CsvUtil.LoadObjects<ConsumableInfo>("consumable");
        consumableInfoMap.Clear();
        foreach (var consumableInfo in consumableInfos)
        {
            if (string.IsNullOrEmpty(consumableInfo.identifier))
                continue;

            consumableInfoMap[consumableInfo.identifier] = consumableInfo;
        }
    }

    public IslandInfo GetIslandInfo(int islandId)
    {
        if (islandInfoMap != null && islandInfoMap.TryGetValue(islandId, out IslandInfo info))
        {
            return info;
        }

        return null;
    }
}
