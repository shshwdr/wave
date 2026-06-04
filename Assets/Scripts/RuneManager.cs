using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 符文管理器 - 记录玩家拥有的符文，按 effect 驱动战斗逻辑
/// </summary>
public class RuneManager : Singleton<RuneManager>
{
    private readonly HashSet<string> ownedRunes = new HashSet<string>();

    public void Init()
    {
        ownedRunes.Clear();

        if (CSVLoader.Instance == null || CSVLoader.Instance.runeInfoMap == null)
            return;

        foreach (var runeInfo in CSVLoader.Instance.runeInfoMap.Values)
        {
            if (runeInfo.isStart)
            {
                ownedRunes.Add(runeInfo.identifier);
                Debug.Log($"初始符文: {runeInfo.identifier}");
            }
        }
    }

    public bool HasRune(string identifier)
    {
        return !string.IsNullOrEmpty(identifier) && ownedRunes.Contains(identifier);
    }

    public bool HasEffect(string effect)
    {
        if (string.IsNullOrEmpty(effect) || CSVLoader.Instance == null)
            return false;

        foreach (string identifier in ownedRunes)
        {
            if (CSVLoader.Instance.runeInfoMap.TryGetValue(identifier, out RuneInfo info)
                && info.effect == effect)
            {
                return true;
            }
        }

        return false;
    }

    public void AddRune(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return;

        ownedRunes.Add(identifier);
    }

    public int GetRuneValue(string identifier)
    {
        if (!HasRune(identifier) || CSVLoader.Instance == null)
            return 0;

        if (CSVLoader.Instance.runeInfoMap.TryGetValue(identifier, out RuneInfo info))
            return info.values;

        return 0;
    }

    public int GetValueByEffect(string effect)
    {
        if (string.IsNullOrEmpty(effect) || CSVLoader.Instance == null)
            return 0;

        foreach (string identifier in ownedRunes)
        {
            if (!CSVLoader.Instance.runeInfoMap.TryGetValue(identifier, out RuneInfo info))
                continue;

            if (info.effect == effect)
                return info.values;
        }

        return 0;
    }

    public IEnumerable<string> GetOwnedRuneIdentifiers()
    {
        return ownedRunes;
    }

    public string GetRuneName(string identifier)
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.runeInfoMap.TryGetValue(identifier, out RuneInfo info))
            return identifier ?? "";

        if (!string.IsNullOrEmpty(info.name))
            return info.name;

        return info.identifier;
    }

    public string GetRuneDescription(string identifier)
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.runeInfoMap.TryGetValue(identifier, out RuneInfo info))
            return "";

        int value = info.values;
        if (HasRune(identifier))
            value = GetRuneValue(identifier);

        if (string.IsNullOrEmpty(info.description))
            return GetRuneName(identifier);

        return info.description.Replace("{0}", value.ToString());
    }

    /// <summary>
    /// 玩家回合开始时是否跳过护盾减半
    /// </summary>
    public bool ShouldKeepShieldAtTurnStart()
    {
        return HasEffect("keepShieldValue");
    }

    /// <summary>
    /// 敌人攻击带护盾的玩家时，对敌人造成反伤
    /// </summary>
    public void TryApplyShieldDamageReflection(Enemy attacker, int attackDamage)
    {
        if (attacker == null || attacker.IsDead || attackDamage <= 0)
            return;

        if (!HasEffect("ShieldDamage") || PlayerManager.Instance == null)
            return;

        if (PlayerManager.Instance.CurrentShield <= 0)
            return;

        int percent = GetValueByEffect("ShieldDamage");
        if (percent <= 0)
            return;

        int reflectDamage = Mathf.RoundToInt(attackDamage * (percent / 100f));
        if (reflectDamage <= 0)
            return;

        attacker.TakeDamage(reflectDamage, Vector3.left, false, 0, 0f);
    }

    /// <summary>
    /// 击退碰撞时对碰撞双方造成波浪伤害（hitTakeDamage）
    /// </summary>
    public void TryApplyHitTakeDamageOnCollision(
        Enemy self,
        Enemy collidedEnemy,
        Ally collidedAlly,
        float redWaveDamage,
        bool hasCollision)
    {
        if (self == null || !hasCollision || redWaveDamage <= 0)
            return;

        if (!HasEffect("hitTakeDamage"))
            return;

        int percent = GetValueByEffect("hitTakeDamage");
        if (percent <= 0)
            return;

        float collisionDamage = redWaveDamage * (percent / 100f);
        if (collisionDamage <= 0)
            return;

        int damage = (int)collisionDamage;

        self.TakeDamage(damage, Vector3.right, false, 0, 0f);

        if (collidedEnemy != null && !collidedEnemy.IsDead)
            collidedEnemy.TakeDamage(damage, Vector3.left, false, 0, 0f);
    }

    /// <summary>
    /// 商店折扣后的价格（shopCheaper）
    /// </summary>
    public int GetDiscountedShopPrice(int originalPrice)
    {
        if (originalPrice <= 0 || !HasEffect("shopCheaper"))
            return originalPrice;

        int percent = GetValueByEffect("shopCheaper");
        if (percent <= 0)
            return originalPrice;

        return Mathf.Max(1, Mathf.RoundToInt(originalPrice * (1f - percent / 100f)));
    }

    /// <summary>
    /// 符文提供的全局波浪伤害倍率（damageIncreaseAll + SafeDistance）
    /// </summary>
    public float GetWaveDamageMultiplier()
    {
        float multiplier = 1f;

        if (HasEffect("damageIncreaseAll"))
        {
            int percent = GetValueByEffect("damageIncreaseAll");
            if (percent > 0)
                multiplier *= 1f + percent / 100f;
        }

        if (HasEffect("SafeDistance") && IsLeftmostColumnsClear(3))
        {
            int percent = GetValueByEffect("SafeDistance");
            if (percent > 0)
                multiplier *= 1f + percent / 100f;
        }

        return multiplier;
    }

    /// <summary>
    /// 最左侧若干列是否没有存活敌人
    /// </summary>
    public bool IsLeftmostColumnsClear(int columnCount)
    {
        EnemyManager enemyManager = Object.FindObjectOfType<EnemyManager>();
        if (enemyManager == null || columnCount <= 0)
            return false;

        foreach (var enemy in enemyManager.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            if (enemy.GridPosition.x < columnCount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 敌人位于最左侧若干列时的额外伤害倍率（closerMoreDamage）
    /// </summary>
    public float GetCloserMoreDamageMultiplier(int enemyGridX)
    {
        if (!HasEffect("closerMoreDamage") || enemyGridX >= 2)
            return 1f;

        int percent = GetValueByEffect("closerMoreDamage");
        if (percent <= 0)
            return 1f;

        return 1f + percent / 100f;
    }

    public bool ShouldHitKeepMove()
    {
        return HasEffect("hitKeepMove");
    }

    /// <summary>
    /// 敌人被击退到右边界时对 Boss 造成百分比伤害（PushToBoss）
    /// </summary>
    public void TryApplyPushToBoss(float referenceDamage)
    {
        if (!HasEffect("PushToBoss") || referenceDamage <= 0)
            return;

        Boss boss = MainGameManager.GetCurrentBoss();
        if (boss == null || boss.IsDead)
            return;

        int percent = GetValueByEffect("PushToBoss");
        if (percent <= 0)
            return;

        int bossDamage = Mathf.RoundToInt(referenceDamage * (percent / 100f));
        if (bossDamage <= 0)
            return;

        boss.TakeDamage(bossDamage, Vector3.left, false, 0, 0f);
    }

    /// <summary>
    /// 随从死亡时对四向相邻敌人造成固定伤害（ExplodeAlly）
    /// </summary>
    public void TryExplodeAllyOnDeath(Ally ally)
    {
        if (ally == null || !HasEffect("ExplodeAlly"))
            return;

        int damage = GetValueByEffect("ExplodeAlly");
        if (damage <= 0)
            return;

        EnemyManager enemyManager = Object.FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
            return;

        Vector2Int center = ally.GridPosition;
        Vector2Int[] adjacentPositions =
        {
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x, center.y - 1),
            new Vector2Int(center.x - 1, center.y),
            new Vector2Int(center.x + 1, center.y)
        };

        foreach (var pos in adjacentPositions)
        {
            foreach (var enemy in enemyManager.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead || enemy.GridPosition != pos)
                    continue;

                enemy.TakeDamage(damage, Vector3.zero, false, 0, 0f);
            }
        }
    }
}
