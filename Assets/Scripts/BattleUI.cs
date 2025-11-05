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
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image swapCountBarFill;

    private PlayerManager playerManager;
    private MainGameManager mainGameManager;

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
            levelText.text = $"Level: {mainGameManager.PlayerLevel}";
        }
    }
}

