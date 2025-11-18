using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗界面UI - 显示玩家血量和交换次数
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text swapCountText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text damageText;
    
    [SerializeField] private TMP_Text enemyCountText; // 剩余敌人数显示
    [SerializeField] private TMP_Text goldText; // 金币显示
    [SerializeField] private TMP_Text turnsRemainingText; // 剩余回合数显示
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image swapCountBarFill;

    private PlayerManager playerManager;
    private MainGameManager mainGameManager;
    private EnemyManager enemyManager;

    private void Start()
    {
        playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            Debug.LogWarning("PlayerManager未找到！");
        }
        
        mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager == null)
        {
            Debug.LogWarning("MainGameManager未找到！");
        }
        
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogWarning("EnemyManager未找到！");
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (playerManager == null)
            return;

        // 更新血量显示
        if (healthText != null)
        {
            healthText.text = $"HP: {playerManager.CurrentHealth}/{playerManager.MaxHealth}";
        }

        if (healthBarFill != null)
        {
            float healthPercent = (float)playerManager.CurrentHealth / playerManager.MaxHealth;
            healthBarFill.fillAmount = healthPercent;
        }

        // 更新交换次数显示
        if (swapCountText != null)
        {
            swapCountText.text = $"Swap: {playerManager.CurrentSwapCount}/{playerManager.MaxSwapCount}";
        }

        if (swapCountBarFill != null)
        {
            float swapPercent = (float)playerManager.CurrentSwapCount / playerManager.MaxSwapCount;
            swapCountBarFill.fillAmount = swapPercent;
        }

        // 更新等级显示
        if (levelText != null && mainGameManager != null)
        {
            levelText.text = $"Level: {mainGameManager.PlayerLevel+1}";
        }
        
        // 更新wave伤害显示
        if (damageText != null && playerManager != null)
        {
            float baseDamage = playerManager.GetCurrentBattleBaseDamage();
            damageText.text = $"Wave Damage: {baseDamage:F0}";
        }

        // 更新剩余敌人显示（只在normal模式下显示）
        if (enemyCountText != null && enemyManager != null && mainGameManager != null)
        {
            // 获取当前关卡类型
            LevelInfo currentLevelInfo = mainGameManager.GetCurrentLevelInfo();
            string levelType = currentLevelInfo != null && currentLevelInfo.type != null ? currentLevelInfo.type.ToLower() : "normal";
            
            // 只在normal模式下显示
            if (levelType == "normal")
            {
                int remaining = enemyManager.GetRemainingEnemyCount();
                int total = enemyManager.GetTotalEnemyCount();
                enemyCountText.text = $"Enemies: {remaining}/{total}";
                enemyCountText.gameObject.SetActive(true);
            }
            else
            {
                enemyCountText.gameObject.SetActive(false);
            }
        }

        // 更新金币显示
        if (goldText != null && playerManager != null)
        {
            goldText.text = $"Gold: {playerManager.Gold}";
        }

        // 更新剩余回合数显示（只在turns不为0时显示）
        if (turnsRemainingText != null && mainGameManager != null)
        {
            int remainingTurns = mainGameManager.GetRemainingTurns();
            if (remainingTurns >= 0)
            {
                turnsRemainingText.text = $"Turns: {remainingTurns}";
                turnsRemainingText.gameObject.SetActive(true);
            }
            else
            {
                turnsRemainingText.gameObject.SetActive(false);
            }
        }
    }
}

