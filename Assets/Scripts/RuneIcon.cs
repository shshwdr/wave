using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 符文图标 - 常驻显示名称，悬停显示描述
/// </summary>
public class RuneIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject description;

    private string runeIdentifier;
    private RuneMenu parentMenu;

    private void Awake()
    {
        if (description != null)
            description.gameObject.SetActive(false);
    }

    public void Init(string identifier, RuneMenu menu)
    {
        runeIdentifier = identifier;
        parentMenu = menu;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (RuneManager.Instance == null)
            return;

        if (nameText != null)
            nameText.text = RuneManager.Instance.GetRuneName(runeIdentifier);

        if (descriptionText != null)
            descriptionText.text = RuneManager.Instance.GetRuneDescription(runeIdentifier);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (description != null)
            description.gameObject.SetActive(true);

        if (parentMenu != null)
            parentMenu.ShowRuneDescription(runeIdentifier);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (description != null)
            description.gameObject.SetActive(false);

        if (parentMenu != null)
            parentMenu.HideRuneDescription();
    }
}
