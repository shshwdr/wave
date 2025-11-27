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
    [SerializeField] private RectTransform healthBarContainer; // HP bar容器（用于抖动和缩放）

    [Header("设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private bool followEnemy = true;

    [Header("HP Bar特效设置")]
    [SerializeField] private float damageShakeDuration = 0.3f; // 扣血抖动持续时间
    [SerializeField] private float damageShakeStrength = 20f; // 扣血抖动强度
    [SerializeField] private int damageShakeVibrato = 20; // 扣血抖动震动次数
    [SerializeField] private float healPulseDuration = 0.3f; // 回血脉冲持续时间
    [SerializeField] private float healPulseScale = 1.2f; // 回血脉冲缩放倍数
    [SerializeField] private int healPulseCount = 2; // 回血脉冲次数
    [SerializeField] private float healthBarAnimationDuration = 0.5f; // 血条动画持续时间
    [SerializeField] private float healthNumberAnimationDuration = 0.5f; // 数字动画持续时间

    private Enemy enemy;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private int currentHealth = 0;
    private int maxHealth = 0;
    
    // HP bar动画相关
    private int lastHealth = -1; // 上一次的血量
    private float currentDisplayHealth = 0f; // 当前显示的血量（用于平滑过渡）
    private int currentDisplayHealthInt = 0; // 当前显示的血量整数（用于数字跳动）
    private int targetHealth = 0; // 目标血量
    private Tween healthBarTween; // 血条动画
    private Tween healthNumberTween; // 数字动画
    private Tween shakeTween; // 抖动动画
    private Tween pulseTween; // 脉冲动画
    private Vector3 originalHealthBarScale = Vector3.one; // HP bar原始缩放
    private Vector3 originalHealthBarPosition = Vector3.zero; // HP bar原始位置

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 初始化HP bar容器
        if (healthBarContainer == null)
        {
            // 如果没有指定容器，使用当前RectTransform
            healthBarContainer = rectTransform;
        }
        
        if (healthBarContainer != null)
        {
            originalHealthBarScale = healthBarContainer.localScale;
            originalHealthBarPosition = healthBarContainer.localPosition;
        }
    }

    /// <summary>
    /// 初始化血条
    /// </summary>
    public void Init(Enemy targetEnemy, int maxHealth)
    {
        enemy = targetEnemy;
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
        
        // 初始化动画相关变量
        lastHealth = maxHealth;
        currentDisplayHealth = maxHealth;
        currentDisplayHealthInt = maxHealth;
        targetHealth = maxHealth;
        
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
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
        // 从CSVLoader获取font
        if (CSVLoader.Instance != null && CSVLoader.Instance.font != null)
        {
            healthText.font = CSVLoader.Instance.font;
        }
    }

    /// <summary>
    /// 更新血条
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        int oldMaxHealth = this.maxHealth;
        int oldHealth = this.currentHealth;
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
        
        // 如果最大血量改变，更新显示值
        if (oldMaxHealth != maxHealth && oldMaxHealth > 0)
        {
            // 按比例调整显示值
            float healthPercent = currentDisplayHealth / oldMaxHealth;
            currentDisplayHealth = maxHealth * healthPercent;
            currentDisplayHealthInt = Mathf.RoundToInt(currentDisplayHealth);
        }
        
        // 初始化（第一次调用）
        if (lastHealth == -1)
        {
            lastHealth = currentHealth;
            currentDisplayHealth = currentHealth;
            currentDisplayHealthInt = currentHealth;
            targetHealth = currentHealth;
        }
        
        // 如果血量改变，触发特效
        if (currentHealth != lastHealth)
        {
            bool isDamage = currentHealth < lastHealth;
            bool isHeal = currentHealth > lastHealth;
            
            if (isDamage)
            {
                // 扣血：抖动效果
                PlayDamageShake();
            }
            else if (isHeal)
            {
                // 回血：脉冲效果
                PlayHealPulse();
            }
            
            // 更新目标血量
            targetHealth = currentHealth;
            lastHealth = currentHealth;
            
            // 开始血条和数字的平滑过渡动画
            StartHealthBarAnimation();
        }
        
        // 更新血条显示
        UpdateHealthBarDisplay();
    }
    
    /// <summary>
    /// 更新血条显示（每帧调用）
    /// </summary>
    private void UpdateHealthBarDisplay()
    {
        if (fillImage != null)
        {
            float healthPercent = currentDisplayHealth / maxHealth;
            healthPercent = Mathf.Clamp01(healthPercent);
            fillImage.fillAmount = healthPercent;
        }
        
        // 更新血量文字显示（使用整数跳动）
        if (healthText != null)
        {
            currentDisplayHealthInt = Mathf.Clamp(currentDisplayHealthInt, 0, maxHealth);
            healthText.text = $"{currentDisplayHealthInt}/{maxHealth}";
        }
    }
    
    /// <summary>
    /// 播放扣血抖动效果
    /// </summary>
    private void PlayDamageShake()
    {
        if (healthBarContainer == null)
            return;
        
        // 如果前一个抖动动画未完成，先恢复初始状态
        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
            healthBarContainer.localPosition = originalHealthBarPosition;
        }
        
        // 播放抖动动画
        shakeTween = healthBarContainer.DOShakePosition(
            damageShakeDuration,
            damageShakeStrength,
            damageShakeVibrato,
            90f,
            false,
            true
        ).OnComplete(() =>
        {
            // 动画完成后恢复原始位置
            healthBarContainer.localPosition = originalHealthBarPosition;
        });
    }
    
    /// <summary>
    /// 播放回血脉冲效果
    /// </summary>
    private void PlayHealPulse()
    {
        if (healthBarContainer == null)
            return;
        
        // 如果前一个脉冲动画未完成，先恢复初始状态
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
            healthBarContainer.localScale = originalHealthBarScale;
        }
        
        // 播放脉冲动画（放大缩小）
        Sequence pulseSequence = DOTween.Sequence();
        float scalePerPulse = healPulseDuration / healPulseCount;
        
        for (int i = 0; i < healPulseCount; i++)
        {
            pulseSequence.Append(healthBarContainer.DOScale(originalHealthBarScale * healPulseScale, scalePerPulse * 0.5f).SetEase(Ease.OutQuad));
            pulseSequence.Append(healthBarContainer.DOScale(originalHealthBarScale, scalePerPulse * 0.5f).SetEase(Ease.InQuad));
        }
        
        pulseTween = pulseSequence.OnComplete(() =>
        {
            // 动画完成后确保恢复原始缩放
            healthBarContainer.localScale = originalHealthBarScale;
        });
    }
    
    /// <summary>
    /// 开始血条平滑过渡动画
    /// </summary>
    private void StartHealthBarAnimation()
    {
        // 停止之前的血条动画
        if (healthBarTween != null && healthBarTween.IsActive())
        {
            healthBarTween.Kill();
        }
        
        // 停止之前的数字动画
        if (healthNumberTween != null && healthNumberTween.IsActive())
        {
            healthNumberTween.Kill();
        }
        
        float startHealth = currentDisplayHealth;
        float endHealth = targetHealth;
        int startHealthInt = currentDisplayHealthInt;
        int endHealthInt = targetHealth;
        
        // 血条平滑过渡动画
        healthBarTween = DOTween.To(
            () => currentDisplayHealth,
            x => 
            {
                currentDisplayHealth = x;
                UpdateHealthBarDisplay();
            },
            endHealth,
            healthBarAnimationDuration
        ).SetEase(Ease.OutQuad);
        
        // 数字逐数字跳动动画
        int healthDiff = endHealthInt - startHealthInt;
        if (healthDiff != 0)
        {
            // 计算每个数字之间的时间间隔
            float timePerNumber = healthNumberAnimationDuration / Mathf.Abs(healthDiff);
            timePerNumber = Mathf.Max(0.01f, timePerNumber); // 最小间隔0.01秒
            
            // 创建序列动画，让数字一个接一个跳动
            Sequence numberSequence = DOTween.Sequence();
            int step = healthDiff > 0 ? 1 : -1;
            int absDiff = Mathf.Abs(healthDiff);
            
            // 创建一个数组来存储所有要显示的值，避免闭包问题
            int[] healthValues = new int[absDiff];
            for (int i = 0; i < absDiff; i++)
            {
                healthValues[i] = startHealthInt + (step * (i + 1));
            }
            
            for (int i = 0; i < absDiff; i++)
            {
                int index = i; // 捕获索引值
                numberSequence.AppendCallback(() =>
                {
                    currentDisplayHealthInt = healthValues[index];
                    UpdateHealthBarDisplay();
                });
                
                if (i < absDiff - 1)
                {
                    numberSequence.AppendInterval(timePerNumber);
                }
            }
            
            // 确保最后的值是正确的
            numberSequence.AppendCallback(() =>
            {
                currentDisplayHealthInt = endHealthInt;
                UpdateHealthBarDisplay();
            });
            
            healthNumberTween = numberSequence;
        }
        else
        {
            // 如果血量没有变化，直接设置
            currentDisplayHealthInt = endHealthInt;
            UpdateHealthBarDisplay();
        }
    }

    /// <summary>
    /// 设置血条显示/隐藏
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void Update()
    {
        // 每帧更新血条显示（确保动画过程中血条和数字正确显示）
        if (healthBarTween != null && healthBarTween.IsActive())
        {
            UpdateHealthBarDisplay();
        }
    }
    
    private void OnDestroy()
    {
        // 清理所有动画
        if (healthBarTween != null)
            healthBarTween.Kill();
        if (healthNumberTween != null)
            healthNumberTween.Kill();
        if (shakeTween != null)
            shakeTween.Kill();
        if (pulseTween != null)
            pulseTween.Kill();
        DOTween.Kill(this);
    }
}

