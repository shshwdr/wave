using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 符文菜单 - 仅展示玩家已拥有的符文
/// </summary>
public class RuneMenu : MonoBehaviour
{
    [SerializeField] private Transform runeParent;
    [SerializeField] private GameObject runePrefab;

    [Header("可选：集中显示悬停描述")]
    [SerializeField] private TMP_Text sharedDescriptionText;

    private readonly List<GameObject> spawnedIcons = new List<GameObject>();

    private void Start()
    {
        RefreshRunes();
    }

    public void RefreshRunes()
    {
        ClearIcons();

        if (runeParent == null || runePrefab == null || CSVLoader.Instance == null || RuneManager.Instance == null)
            return;

        List<RuneInfo> runes = CSVLoader.Instance.runeInfoList;
        if (runes == null)
            return;

        foreach (RuneInfo runeInfo in runes)
        {
            if (runeInfo == null || string.IsNullOrEmpty(runeInfo.identifier))
                continue;

            if (!RuneManager.Instance.HasRune(runeInfo.identifier))
                continue;

            GameObject iconObj = Instantiate(runePrefab, runeParent);
            RuneIcon icon = iconObj.GetComponent<RuneIcon>();
            if (icon != null)
                icon.Init(runeInfo.identifier, this);

            spawnedIcons.Add(iconObj);
        }
    }

    public void ShowRuneDescription(string identifier)
    {
        if (sharedDescriptionText == null || RuneManager.Instance == null)
            return;

        sharedDescriptionText.gameObject.SetActive(true);
        sharedDescriptionText.text = RuneManager.Instance.GetRuneDescription(identifier);
    }

    public void HideRuneDescription()
    {
        if (sharedDescriptionText == null)
            return;

        sharedDescriptionText.gameObject.SetActive(false);
    }

    private void ClearIcons()
    {
        foreach (GameObject icon in spawnedIcons)
        {
            if (icon != null)
                Destroy(icon);
        }

        spawnedIcons.Clear();
    }
}
