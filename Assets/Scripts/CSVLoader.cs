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
    public int maxLevel;
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
}
public class CSVLoader : Singleton<CSVLoader>
{
    public TMP_FontAsset font;
    public Dictionary<string, SkillInfo> cardInfoMap = new Dictionary<string, SkillInfo>();
    public Dictionary<string, EnemyInfo> enemyInfoMap = new Dictionary<string, EnemyInfo>();
    public Dictionary<int, LevelInfo> levelInfoMap = new Dictionary<int, LevelInfo>();
    public Dictionary<string, EventInfo> eventInfoMap = new Dictionary<string, EventInfo>();
    // Start is called before the first frame update
    public void Init()
    {
        var cardInfos = CsvUtil.LoadObjects<SkillInfo>("skill");
        foreach (var cardInfo in cardInfos)
        {
            cardInfoMap.Add(cardInfo.identifier, cardInfo);
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
        var eventInfos = CsvUtil.LoadObjects<EventInfo>("event");
        foreach (var eventInfo in eventInfos)
        {
            eventInfoMap.Add(eventInfo.identifier, eventInfo);
        }
    }
}
