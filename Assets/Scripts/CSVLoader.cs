using System.Collections;
using System.Collections.Generic;
using Sinbad;
using UnityEngine;

public class SkillInfo
{
    public string identifier;
    public string name;
    public string description;
    public string color;
    public bool isStart;
    public string effect;
    public List<int> values; //count equals level
    public int maxLevel;

}

public class EnemyInfo
{
    
    public string identifier;
    public string name;
    public string description;
    public int hp;
    public int attack;
    public int speed;
    public List<string> skill;

}

public class LevelInfo
{
    public string enemies;
    public int level;
}
public class CSVLoader : Singleton<CSVLoader>
{
    public Dictionary<string, SkillInfo> cardInfoMap = new Dictionary<string, SkillInfo>();
    public Dictionary<string, EnemyInfo> enemyInfoMap = new Dictionary<string, EnemyInfo>();
    public Dictionary<int, LevelInfo> levelInfoMap = new Dictionary<int, LevelInfo>();
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
        foreach (var levelInfo in levelInfos)
        {
            levelInfoMap.Add(levelInfo.level, levelInfo);
        }
    }
}
