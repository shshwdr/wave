using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色状态栏里的一格。Prefab 上配置好图片和文字，代码只负责显示/隐藏和更新层数。
/// </summary>
public class StatusCell : MonoBehaviour
{
    [SerializeField] private string statusId;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public string Id
    {
        get
        {
            if (!string.IsNullOrEmpty(statusId))
                return statusId;
            return gameObject.name;
        }
    }

    private void Awake()
    {
        if (icon == null)
            icon = GetComponent<Image>();
        if (icon == null)
            icon = GetComponentInChildren<Image>(true);
        if (countText == null)
            countText = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetId(string id)
    {
        statusId = id;
        gameObject.name = id;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetCount(int count, bool showCount)
    {
        if (countText == null)
            return;

        countText.gameObject.SetActive(showCount);
        if (showCount)
            countText.text = count.ToString();
    }

    public void SetIcon(Sprite sprite)
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }
}
