using TMPro;
using UnityEngine;

/// <summary>
/// 「always for battle and ui」常驻 HUD：地图与战斗期间保持显示并刷新金币等。
/// </summary>
public class AlwaysBattleAndUiController : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private EnemyHealthBar playerHealthBar;

    private int lastGold = -1;
    private bool playerHealthBarInitialized;

    private void Awake()
    {
        if (goldText == null)
        {
            goldText = GetComponentInChildren<TMP_Text>(true);
        }

        if (playerHealthBar == null)
        {
            playerHealthBar = GetComponentInChildren<EnemyHealthBar>(true);
        }
    }

    private void Update()
    {
        if (PlayerManager.Instance == null)
        {
            return;
        }

        UpdateGoldDisplay();
        UpdateHealthBarDisplay();
    }

    public void RefreshDisplay()
    {
        if (PlayerManager.Instance == null)
        {
            return;
        }

        UpdateGoldDisplay();
        UpdateHealthBarDisplay();
    }

    private void UpdateGoldDisplay()
    {
        if (goldText == null)
        {
            return;
        }

        int currentGold = PlayerManager.Instance.Gold;
        goldText.text = $"x{currentGold}";
        lastGold = currentGold;
    }

    private void UpdateHealthBarDisplay()
    {
        if (playerHealthBar == null)
        {
            return;
        }

        int currentHealth = PlayerManager.Instance.CurrentHealth;
        int maxHealth = PlayerManager.Instance.MaxHealth;

        if (!playerHealthBarInitialized)
        {
            playerHealthBar.InitStandalone(currentHealth, maxHealth);
            playerHealthBarInitialized = true;
        }
        else
        {
            playerHealthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}
