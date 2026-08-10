using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 「always for battle and ui」常驻 HUD：地图与战斗期间保持显示并刷新金币等。
/// </summary>
public class AlwaysBattleAndUiController : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private EnemyHealthBar playerHealthBar;
    [SerializeField] private EnemyHealthBar playerShieldHealthBar;
    [SerializeField] private float shieldBarOffsetY = -35f;

    private int lastGold = -1;
    private bool playerHealthBarInitialized;
    private bool playerShieldBarInitialized;
    private int shieldBarDisplayMax;

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

        EnsureShieldHealthBar();
        if (playerShieldHealthBar != null)
            playerShieldHealthBar.SetVisible(false);
    }

    private void Update()
    {
        if (PlayerManager.Instance == null)
        {
            return;
        }

        UpdateGoldDisplay();
        UpdateHealthBarDisplay();
        UpdateShieldBarDisplay();
    }

    public void RefreshDisplay()
    {
        if (PlayerManager.Instance == null)
        {
            return;
        }

        UpdateGoldDisplay();
        UpdateHealthBarDisplay();
        UpdateShieldBarDisplay();
    }

    private void EnsureShieldHealthBar()
    {
        if (playerShieldHealthBar != null)
            return;

        if (playerHealthBar == null)
            return;

        GameObject shieldBarObject = Instantiate(playerHealthBar.gameObject, playerHealthBar.transform.parent);
        shieldBarObject.name = "ShieldBar";

        RectTransform shieldRect = shieldBarObject.GetComponent<RectTransform>();
        RectTransform healthRect = playerHealthBar.GetComponent<RectTransform>();
        if (shieldRect != null && healthRect != null)
        {
            shieldRect.anchorMin = healthRect.anchorMin;
            shieldRect.anchorMax = healthRect.anchorMax;
            shieldRect.pivot = healthRect.pivot;
            shieldRect.sizeDelta = healthRect.sizeDelta;
            shieldRect.anchoredPosition = healthRect.anchoredPosition + new Vector2(0f, shieldBarOffsetY);
        }

        playerShieldHealthBar = shieldBarObject.GetComponent<EnemyHealthBar>();
        shieldBarObject.SetActive(false);
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

    private void UpdateShieldBarDisplay()
    {
        EnsureShieldHealthBar();

        if (playerShieldHealthBar == null)
        {
            return;
        }

        int currentShield = PlayerManager.Instance.CurrentShield;
        int maxHealth = PlayerManager.Instance.MaxHealth;
        bool showShield = currentShield > 0;

        GameObject shieldGo = playerShieldHealthBar.gameObject;
        if (shieldGo.name.Equals("shield", StringComparison.OrdinalIgnoreCase) == false)
        {
            Transform namedShield = FindChildByName(transform, "shield");
            if (namedShield != null)
                namedShield.gameObject.SetActive(showShield);
        }

        if (!showShield)
        {
            if (shieldGo.activeSelf)
                playerShieldHealthBar.SetVisible(false);

            playerShieldBarInitialized = false;
            shieldBarDisplayMax = maxHealth;
            return;
        }

        if (!shieldGo.activeSelf)
            playerShieldHealthBar.SetVisible(true);

        shieldBarDisplayMax = Mathf.Max(shieldBarDisplayMax, currentShield, maxHealth, 1);
        int shieldMax = shieldBarDisplayMax;

        if (!playerShieldBarInitialized)
        {
            playerShieldHealthBar.InitStandalone(currentShield, shieldMax);
            playerShieldBarInitialized = true;
        }
        else
        {
            playerShieldHealthBar.UpdateHealthBar(currentShield, shieldMax);
        }
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildByName(root.GetChild(i), name);
            if (child != null)
                return child;
        }

        return null;
    }
}
