using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Post-battle reward screen (Slay the Spire style).
/// </summary>
public class BattleResultMenu : MenuBase
{
    public class RewardData
    {
        public int displayGold;
        public int goldToGrant;
        public string consumableId;
        public bool includeCardReward = true;
        public bool includeRelicReward = true;
    }

    [Header("Battle Result UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform cellsParent;
    [SerializeField] private BattleResultCell cellPrefab;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Generic Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite cardIcon;
    [SerializeField] private Sprite relicIcon;

    private readonly List<BattleResultCell> cells = new List<BattleResultCell>();
    private Action onFinished;
    private RewardData currentRewards;
    private int pendingGoldToGrant;
    private string pendingConsumableId;

    protected override void Awake()
    {
        EnsureUiBuilt();
        base.Awake();

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    public void ShowRewards(RewardData rewards, Action onComplete)
    {
        currentRewards = rewards;
        onFinished = onComplete;
        pendingGoldToGrant = rewards != null ? rewards.goldToGrant : 0;
        pendingConsumableId = rewards?.consumableId;

        RebuildCells();
        UpdateContinueButton();
        Show();
        transform.SetAsLastSibling();
    }

    public override void Show(bool immediate = false)
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
        }

        base.Show(immediate);
        transform.SetAsLastSibling();
    }

    private void RebuildCells()
    {
        ClearCells();

        if (currentRewards == null)
            return;

        if (currentRewards.displayGold > 0)
        {
            AddCell(
                BattleResultCell.RewardType.Gold,
                GetGoldIcon(),
                $"{currentRewards.displayGold} Gold",
                ClaimGold);
        }

        if (!string.IsNullOrEmpty(currentRewards.consumableId))
        {
            string consumableName = ConsumableManager.Instance != null
                ? ConsumableManager.Instance.GetName(currentRewards.consumableId)
                : currentRewards.consumableId;

            Sprite consumableSprite = ConsumableManager.Instance?.GetInfo(currentRewards.consumableId)?.icon;
            AddCell(
                BattleResultCell.RewardType.Consumable,
                consumableSprite,
                consumableName,
                ClaimConsumable);
        }

        if (currentRewards.includeCardReward && currentRewards.includeRelicReward)
        {
            AddCell(
                BattleResultCell.RewardType.Shop,
                GetRelicIcon(),
                "Choose a skill and relic",
                OpenCombinedRewardShop);
        }
        else if (currentRewards.includeCardReward)
        {
            AddCell(
                BattleResultCell.RewardType.Card,
                GetCardIcon(),
                "Choose a skill",
                OpenCardRewardShop);
        }
    }

    private void AddCell(BattleResultCell.RewardType type, Sprite icon, string label, Action handler)
    {
        BattleResultCell cell = CreateCellInstance();
        if (cell == null)
            return;

        cell.Setup(type, icon, label, handler);
        cells.Add(cell);
    }

    private BattleResultCell CreateCellInstance()
    {
        if (cellsParent == null)
            return null;

        if (cellPrefab != null)
            return Instantiate(cellPrefab, cellsParent);

        return BuildRuntimeCell(cellsParent);
    }

    private void ClaimGold()
    {
        if (pendingGoldToGrant <= 0)
            return;

        int grantAmount = pendingGoldToGrant;
        int flyCount = Mathf.Clamp(currentRewards != null ? currentRewards.displayGold : grantAmount, 1, 15);
        Sprite icon = GetGoldIcon();
        Vector3 startPosition = GetCellWorldPosition(BattleResultCell.RewardType.Gold);
        RectTransform target = GetGoldFlyTarget();

        pendingGoldToGrant = 0;
        MarkCellClaimed(BattleResultCell.RewardType.Gold);

        if (target != null && CollectableFlyManager.Instance != null)
        {
            CollectableFlyManager.Instance.FlyToTarget(icon, startPosition, target, flyCount, () =>
            {
                if (grantAmount > 0 && PlayerManager.Instance != null)
                    PlayerManager.Instance.AddGold(grantAmount);

                FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_buy_skill");
            });
            return;
        }

        if (grantAmount > 0 && PlayerManager.Instance != null)
            PlayerManager.Instance.AddGold(grantAmount);

        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_buy_skill");
    }

    private void ClaimConsumable()
    {
        if (string.IsNullOrEmpty(pendingConsumableId) || ConsumableManager.Instance == null)
            return;

        if (!ConsumableManager.Instance.CanObtainConsumable(pendingConsumableId))
        {
            ConfirmDialog.ShowConfirm(
                "Consumable Full",
                "Consumable inventory is full. Sell an existing consumable to obtain a new one.");
            return;
        }

        string consumableId = pendingConsumableId;
        Sprite consumableSprite = ConsumableManager.Instance.GetInfo(consumableId)?.icon;
        Vector3 startPosition = GetCellWorldPosition(BattleResultCell.RewardType.Consumable);
        ConsumableView view = FindObjectOfType<ConsumableView>(true);
        RectTransform target = view != null
            ? view.GetFlyTargetForConsumable(consumableId)
            : null;

        pendingConsumableId = null;
        MarkCellClaimed(BattleResultCell.RewardType.Consumable);

        if (target != null && consumableSprite != null && CollectableFlyManager.Instance != null)
        {
            CollectableFlyManager.Instance.FlyToTarget(consumableSprite, startPosition, target, 1, () =>
            {
                if (ConsumableManager.Instance.AddConsumable(consumableId, 1))
                    view?.Refresh();

                FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_buy_skill");
            });
            return;
        }

        if (ConsumableManager.Instance.AddConsumable(consumableId, 1))
            view?.Refresh();

        FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/sfx_buy_skill");
    }

    private void OpenCombinedRewardShop()
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>(true);
        if (skillMenu == null)
            return;

        MarkCellClaimed(BattleResultCell.RewardType.Shop);
        skillMenu.ShowBattleRewardOverlay(
            null,
            SkillSelectMenu.ShopMode.BattleReward,
            null,
            requireBothPicks: true);
    }

    private void OpenCardRewardShop()
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>(true);
        if (skillMenu == null)
            return;

        MarkCellClaimed(BattleResultCell.RewardType.Card);
        skillMenu.ShowBattleRewardOverlay(
            null,
            SkillSelectMenu.ShopMode.BattleRewardSkill);
    }

    private void OpenRelicRewardShop()
    {
        SkillSelectMenu skillMenu = FindObjectOfType<SkillSelectMenu>(true);
        if (skillMenu == null)
            return;

        MarkCellClaimed(BattleResultCell.RewardType.Relic);
        skillMenu.ShowBattleRewardOverlay(
            null,
            SkillSelectMenu.ShopMode.BattleRewardRune);
    }

    private Vector3 GetCellWorldPosition(BattleResultCell.RewardType type)
    {
        foreach (BattleResultCell cell in cells)
        {
            if (cell != null && cell.Type == type)
                return cell.GetIconWorldPosition();
        }

        return transform.position;
    }

    private RectTransform GetGoldFlyTarget()
    {
        BattleUI battleUI = FindObjectOfType<BattleUI>(true);
        if (battleUI != null && battleUI.GoldTextRect != null)
            return battleUI.GoldTextRect;

        AlwaysBattleAndUiController hud = FindObjectOfType<AlwaysBattleAndUiController>(true);
        if (hud != null)
        {
            TMP_Text hudGoldText = hud.GetComponentInChildren<TMP_Text>(true);
            if (hudGoldText != null)
                return hudGoldText.rectTransform;
        }

        return null;
    }

    private void MarkCellClaimed(BattleResultCell.RewardType type)
    {
        foreach (BattleResultCell cell in cells)
        {
            if (cell != null && cell.Type == type)
                cell.SetClaimed(true);
        }

        UpdateContinueButton();
    }

    private bool AllRewardsClaimed()
    {
        if (cells.Count == 0)
            return true;

        foreach (BattleResultCell cell in cells)
        {
            if (cell != null && !cell.IsClaimed)
                return false;
        }

        return true;
    }

    private void UpdateContinueButton()
    {
        if (continueButtonText == null)
            return;

        continueButtonText.text = AllRewardsClaimed() ? "Continue" : "Skip Rewards";
    }

    private void OnContinueClicked()
    {
        if (AllRewardsClaimed())
        {
            Finish();
            return;
        }

        ConfirmDialog.ShowConfirm(
            "Skip Rewards?",
            "You still have unclaimed rewards. Skip them and continue?",
            onYes: Finish,
            onNo: null);
    }

    private void Finish()
    {
        Hide();
        onFinished?.Invoke();
        onFinished = null;
        currentRewards = null;
        ClearCells();
    }

    private void ClearCells()
    {
        foreach (BattleResultCell cell in cells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }

        cells.Clear();
    }

    private Sprite GetGoldIcon()
    {
        if (goldIcon != null)
            return goldIcon;

        return Resources.Load<Sprite>("enemy/chest");
    }

    private Sprite GetCardIcon()
    {
        if (cardIcon != null)
            return cardIcon;

        return Resources.Load<Sprite>("mapNode/battle");
    }

    private Sprite GetRelicIcon()
    {
        if (relicIcon != null)
            return relicIcon;

        return Resources.Load<Sprite>("mapNode/shop");
    }

    public static BattleResultMenu GetOrCreate()
    {
        BattleResultMenu existing = FindObjectOfType<BattleResultMenu>(true);
        if (existing != null)
            return existing;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("BattleResultMenu: No Canvas found.");
            return null;
        }

        GameObject root = new GameObject("BattleResultMenu", typeof(RectTransform), typeof(BattleResultMenu));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsLastSibling();

        BattleResultMenu menu = root.GetComponent<BattleResultMenu>();
        menu.EnsureUiBuilt();
        return menu;
    }

    private void EnsureUiBuilt()
    {
        if (menu != null && cellsParent != null && continueButton != null)
            return;

        RectTransform selfRect = GetComponent<RectTransform>();
        if (selfRect == null)
            selfRect = gameObject.AddComponent<RectTransform>();

        selfRect.anchorMin = Vector2.zero;
        selfRect.anchorMax = Vector2.one;
        selfRect.offsetMin = Vector2.zero;
        selfRect.offsetMax = Vector2.zero;

        if (menu == null)
        {
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.85f);

            menu = panel;
            animatedRect = panelRect;
        }

        if (titleText == null)
        {
            titleText = CreateTmpText("Title", menu.transform, "Rewards", 36, new Vector2(0.5f, 0.82f), new Vector2(500f, 60f));
        }

        if (cellsParent == null)
        {
            GameObject listObj = new GameObject("Cells", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform listRect = listObj.GetComponent<RectTransform>();
            listRect.SetParent(menu.transform, false);
            listRect.anchorMin = new Vector2(0.5f, 0.35f);
            listRect.anchorMax = new Vector2(0.5f, 0.75f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.sizeDelta = new Vector2(520f, 360f);

            VerticalLayoutGroup layout = listObj.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            cellsParent = listRect;
        }

        if (continueButton == null)
        {
            continueButton = CreateButton("ContinueButton", menu.transform, new Vector2(0.5f, 0.12f), new Vector2(260f, 56f), out continueButtonText);
            continueButtonText.text = "Skip Rewards";
        }
    }

    private static BattleResultCell BuildRuntimeCell(Transform parent)
    {
        GameObject row = new GameObject("BattleResultCell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BattleResultCell));
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.SetParent(parent, false);
        rowRect.sizeDelta = new Vector2(500f, 72f);

        Image rowImage = row.GetComponent<Image>();
        rowImage.color = new Color(0.15f, 0.18f, 0.28f, 0.95f);

        Button button = row.GetComponent<Button>();

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.SetParent(row.transform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(48f, 48f);
        Image iconImage = iconObj.GetComponent<Image>();

        TMP_Text label = CreateTmpText("Label", row.transform, "", 24, new Vector2(0.55f, 0.5f), new Vector2(360f, 48f));

        GameObject overlayObj = new GameObject("ClaimedOverlay", typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.SetParent(row.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayObj.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        overlayObj.SetActive(false);

        BattleResultCell cell = row.GetComponent<BattleResultCell>();
        cell.BindReferences(button, iconImage, label, overlayImage);
        return cell;
    }

    private static TMP_Text CreateTmpText(string name, Transform parent, string text, int fontSize, Vector2 anchor, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        TMP_Text tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 size, out TMP_Text label)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.55f, 0.2f, 0.2f, 1f);

        label = CreateTmpText("Text", obj.transform, "", 24, new Vector2(0.5f, 0.5f), size);
        return obj.GetComponent<Button>();
    }
}
