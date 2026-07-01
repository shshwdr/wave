using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 符文图标 - 显示图标，悬停显示描述
/// </summary>
public class RuneIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject description;

    private string runeIdentifier;
    private RuneMenu parentMenu;

    private void Awake()
    {
        if (description != null)
            description.gameObject.SetActive(false);

        if (nameText != null)
            nameText.gameObject.SetActive(false);
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

        UpdateIconImage();

        if (descriptionText != null)
            descriptionText.text = RuneManager.Instance.GetRuneDescription(runeIdentifier);
    }

    private void UpdateIconImage()
    {
        if (iconImage == null || string.IsNullOrEmpty(runeIdentifier))
            return;

        Sprite sprite = null;
        if (CSVLoader.Instance != null
            && CSVLoader.Instance.runeInfoMap.TryGetValue(runeIdentifier, out RuneInfo info))
        {
            sprite = info.icon;
        }

        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
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
