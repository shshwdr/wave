using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 敌人血条UI - 使用Fill Image实现
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image fillImage;  // 血条填充图片（Image Type设为Filled）

    [Header("设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private bool followEnemy = true;

    private Enemy enemy;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private float currentFillAmount = 1f;

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
        
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            currentFillAmount = 1f;
        }

        UpdateHealthBar(maxHealth, maxHealth);
    }

    /// <summary>
    /// 更新血条
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
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
            float healthPercent = (float)currentHealth / maxHealth;
            if (healthPercent > 0.6f)
            {
                fillImage.color = Color.green;
            }
            else if (healthPercent > 0.3f)
            {
                fillImage.color = Color.yellow;
            }
            else
            {
                fillImage.color = Color.red;
            }
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

