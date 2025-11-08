using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// 敌人血条UI - 使用Fill Image实现
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image fillImage;  // 血条填充图片（Image Type设为Filled）
    [SerializeField] private TextMeshProUGUI healthText; // 血量文字显示

    [Header("设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private bool followEnemy = true;

    private Enemy enemy;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private float currentFillAmount = 1f;
    private int currentHealth = 0;
    private int maxHealth = 0;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 初始化血条
    /// </summary>
    public void Init(Enemy targetEnemy, int maxHealth)
    {
        enemy = targetEnemy;
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
        
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            currentFillAmount = 1f;
        }
        
        // 如果没有healthText，创建一个
        if (healthText == null)
        {
            CreateHealthText();
        }

        UpdateHealthBar(maxHealth, maxHealth);
    }
    
    /// <summary>
    /// 创建血量文字
    /// </summary>
    private void CreateHealthText()
    {
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        healthText = textObj.AddComponent<TextMeshProUGUI>();
        healthText.text = $"{currentHealth}/{maxHealth}";
        healthText.fontSize = 12;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// 更新血条
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
        
        if (fillImage != null)
        {
            float targetFill = (float)currentHealth / maxHealth;
            targetFill = Mathf.Clamp01(targetFill);
            
            // 使用DOTween平滑更新fillAmount
            DOTween.To(() => currentFillAmount, x => 
            {
                currentFillAmount = x;
                fillImage.fillAmount = x;
            }, targetFill, 0.3f).SetEase(Ease.OutQuad);

            // 根据血量改变颜色
            // float healthPercent = (float)currentHealth / maxHealth;
            // if (healthPercent > 0.6f)
            // {
            //     fillImage.color = Color.green;
            // }
            // else if (healthPercent > 0.3f)
            // {
            //     fillImage.color = Color.yellow;
            // }
            // else
            // {
            //     fillImage.color = Color.red;
            // }
        }
        
        // 更新血量文字
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    /// <summary>
    /// 设置血条显示/隐藏
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}

