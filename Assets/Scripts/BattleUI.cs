using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 战斗界面UI - 显示玩家血量和交换次数
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text swapCountText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text damageText;
    
    [SerializeField] private TMP_Text enemyCountText; // 剩余敌人数显示
    [SerializeField] private GameObject enemyCountOB; // 剩余敌人数显示
    
    [SerializeField] private TMP_Text goldText; // 金币显示
    [SerializeField] private TMP_Text goldIncreaseText; // 金币增加提示文本
    [SerializeField] private TMP_Text turnsRemainingText; // 剩余回合数显示
    [SerializeField] private TMP_Text levelDescText; // 关卡描述显示
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image swapCountBarFill;
    [SerializeField] private RectTransform healthBarContainer; // HP bar容器（用于抖动和缩放）
    [SerializeField] private Button restartButton; // Restart按钮
    [SerializeField] private Button settingButton; // Setting按钮
    [SerializeField] private TMP_Text hardModeText; // HARD MODE字样

    [Header("HP Bar特效设置")]
    [SerializeField] private float damageShakeDuration = 0.3f; // 扣血抖动持续时间
    [SerializeField] private float damageShakeStrength = 10f; // 扣血抖动强度
    [SerializeField] private int damageShakeVibrato = 10; // 扣血抖动震动次数
    [SerializeField] private float healPulseDuration = 0.5f; // 回血脉冲持续时间
    [SerializeField] private float healPulseScale = 1.1f; // 回血脉冲缩放倍数
    [SerializeField] private int healPulseCount = 2; // 回血脉冲次数
    [SerializeField] private float healthBarAnimationDuration = 0.5f; // 血条动画持续时间
    [SerializeField] private float healthNumberAnimationDuration = 0.5f; // 数字动画持续时间

    private PlayerManager playerManager;
    private MainGameManager mainGameManager;
    private EnemyManager enemyManager;
    
    // HP bar动画相关
    private int lastHealth = -1; // 上一次的血量
    private float currentDisplayHealth = 0f; // 当前显示的血量（用于平滑过渡）
    private int currentDisplayHealthInt = 0; // 当前显示的血量整数（用于数字跳动）
    private int targetHealth = 0; // 目标血量
    private int maxHealth = 0; // 最大血量
    private Tween healthBarTween; // 血条动画
    private Tween healthNumberTween; // 数字动画
    private Tween shakeTween; // 抖动动画
    private Tween pulseTween; // 脉冲动画
    private Vector3 originalHealthBarScale = Vector3.one; // HP bar原始缩放
    private Vector3 originalHealthBarPosition = Vector3.zero; // HP bar原始位置
    
    // 金币相关
    private int lastGold = -1; // 上一次的金币数量
    private Tween goldIncreaseTween; // 金币增加动画
    private Tween goldTextPulseTween; // 金币文本脉冲动画
    private Vector3 originalGoldIncreasePosition = Vector3.zero; // 金币增加文本原始位置
    private Vector3 originalGoldTextScale = Vector3.one; // 金币文本原始缩放
    
    // Swap相关
    private Tween swapTextPulseTween; // Swap文本脉冲动画
    private Vector3 originalSwapTextScale = Vector3.one; // Swap文本原始缩放

    private void Start()
    {
        playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            Debug.LogWarning("PlayerManager未找到！");
        }
        
        mainGameManager = FindObjectOfType<MainGameManager>();
        if (mainGameManager == null)
        {
            Debug.LogWarning("MainGameManager未找到！");
        }
        
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogWarning("EnemyManager未找到！");
        }
        
        // 初始化HP bar容器
        if (healthBarContainer == null && healthBarFill != null)
        {
            // 如果没有指定容器，尝试找到healthBarFill的父对象
            healthBarContainer = healthBarFill.transform.parent as RectTransform;
        }
        
        if (healthBarContainer != null)
        {
            originalHealthBarScale = healthBarContainer.localScale;
            originalHealthBarPosition = healthBarContainer.localPosition;
        }
        
        // 初始化血量显示
        if (playerManager != null)
        {
            lastHealth = playerManager.CurrentHealth;
            currentDisplayHealth = playerManager.CurrentHealth;
            currentDisplayHealthInt = playerManager.CurrentHealth;
            targetHealth = playerManager.CurrentHealth;
            maxHealth = playerManager.MaxHealth;
            
            // 初始化金币显示
            lastGold = playerManager.Gold;
        }
        
        // 初始化金币增加文本
        if (goldIncreaseText != null)
        {
            goldIncreaseText.gameObject.SetActive(false);
            if (goldIncreaseText.transform is RectTransform goldIncreaseRect)
            {
                originalGoldIncreasePosition = goldIncreaseRect.anchoredPosition;
            }
        }
        
        // 初始化金币文本原始缩放
        if (goldText != null && goldText.transform is RectTransform goldTextRect)
        {
            originalGoldTextScale = goldTextRect.localScale;
        }
        
        // 初始化Swap文本原始缩放
        if (swapCountText != null && swapCountText.transform is RectTransform swapTextRect)
        {
            originalSwapTextScale = swapTextRect.localScale;
        }
        
        // 初始化Restart按钮
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
            
            // 困难模式下隐藏retry按钮
            bool isInHardMode = GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode();
            restartButton.gameObject.SetActive(!isInHardMode);
        }
        
        // 初始化Setting按钮
        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners();
            settingButton.onClick.AddListener(OnSettingButtonClicked);
        }
        
        // 初始化HARD MODE字样
        if (hardModeText != null)
        {
            bool isInHardMode = GameDataManager.Instance != null && GameDataManager.Instance.IsInHardMode();
            hardModeText.gameObject.SetActive(isInHardMode);
            if (isInHardMode)
            {
                hardModeText.text = "HARD MODE";
            }
        }
    }
    
    /// <summary>
    /// Restart按钮点击事件
    /// </summary>
    private void OnRestartButtonClicked()
    {
        // 显示确认对话框
        ConfirmDialog.ShowConfirm(
            title: "Restart Level",
            content: "Do you want to restart this level?",
            onYes: () =>
            {
                // 选择Yes，执行restart逻辑（和失败界面的restart一样）
                if (mainGameManager != null)
                {
                    mainGameManager.RetryLevel();
                }
            },
            onNo: () =>
            {
                // 选择No，关闭对话框（DialogBase会自动处理）
            }
        );
    }
    
    /// <summary>
    /// Setting按钮点击事件
    /// </summary>
    private void OnSettingButtonClicked()
    {
        // 显示设置菜单
        SettingMenu settingMenu = FindObjectOfType<SettingMenu>();
        if (settingMenu == null)
        {
            // 如果没有找到，创建一个新的
            GameObject menuObj = new GameObject("SettingMenu");
            settingMenu = menuObj.AddComponent<SettingMenu>();
        }
        
        if (settingMenu != null)
        {
            settingMenu.Show();
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (playerManager == null)
            return;

        // 更新血量显示（带特效）
        UpdateHealthBarWithEffects();

        // 更新交换次数显示
        if (swapCountText != null)
        {
            swapCountText.text = $"{playerManager.CurrentSwapCount}/{playerManager.MaxSwapCount}";
        }

        if (swapCountBarFill != null)
        {
            float swapPercent = (float)playerManager.CurrentSwapCount / playerManager.MaxSwapCount;
            swapCountBarFill.fillAmount = swapPercent;
        }

        // 更新等级显示
        if (levelText != null && mainGameManager != null)
        {
            levelText.text = $"LEVEL {mainGameManager.PlayerLevel+1}";
        }
        
        // 更新wave伤害显示
        if (damageText != null && playerManager != null)
        {
            float baseDamage = playerManager.GetCurrentBattleBaseDamage();
            damageText.text = $"{baseDamage:F0}";
        }

        // 更新剩余敌人显示（只在normal模式下显示）
        if (enemyCountText != null && enemyManager != null && mainGameManager != null)
        {
            // 获取当前关卡类型
            LevelInfo currentLevelInfo = mainGameManager.GetCurrentLevelInfo();
            string levelType = currentLevelInfo != null && currentLevelInfo.type != null ? currentLevelInfo.type.ToLower() : "normal";
            
            // 只在normal模式下显示
            if (levelType == "normal")
            {
                int remaining = enemyManager.GetRemainingEnemyCount();
                int total = enemyManager.GetTotalEnemyCount();
                enemyCountText.text = $"{remaining}/{total}";
                enemyCountOB.gameObject.SetActive(true);
            }
            else
            {
                enemyCountOB.gameObject.SetActive(false);
            }
        }

        // 更新金币显示
        if (goldText != null && playerManager != null)
        {
            int currentGold = playerManager.Gold;
            
            // 检测金币变化
            if (lastGold != -1 && currentGold > lastGold)
            {
                // 金币增加，显示增加提示并播放脉冲效果
                ShowGoldIncrease(currentGold - lastGold);
                PlayGoldTextPulse();
            }
            
            goldText.text = $"x{currentGold}";
            lastGold = currentGold;
        }

        // 更新剩余回合数显示（只在turns不为0时显示）
        if (turnsRemainingText != null && mainGameManager != null)
        {
            int remainingTurns = mainGameManager.GetRemainingTurns();
            if (remainingTurns >= 0)
            {
                turnsRemainingText.text = $"Turns: {remainingTurns}";
                turnsRemainingText.gameObject.SetActive(true);
            }
            else
            {
                turnsRemainingText.gameObject.SetActive(false);
            }
        }

        // 更新关卡描述显示
        if (levelDescText != null && mainGameManager != null)
        {
            LevelInfo currentLevelInfo = mainGameManager.GetCurrentLevelInfo();
            if (currentLevelInfo != null)
            {
                string message = "";
                string type = currentLevelInfo.type != null ? currentLevelInfo.type.ToLower() : "";
                
                switch (type)
                {
                    case "gold":
                        int turns = currentLevelInfo.turns > 0 ? currentLevelInfo.turns : 0;
                        message = $"Destroy chests to collect gold in {turns} turns!";
                        break;
                    case "boss":
                        message = "Defeat the boss to win!";
                        break;
                    case "puzzle":
                        int puzzleTurns = currentLevelInfo.turns > 0 ? currentLevelInfo.turns : 0;
                        message = $"Clear all tiles in {puzzleTurns} turns!";
                        break;
                    case "normal":
                    default:
                        message = "Eliminate all enemies to win!";
                        break;
                }
                
                levelDescText.text = message;
                levelDescText.gameObject.SetActive(true);
            }
            else
            {
                levelDescText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 更新HP bar（带特效）
    /// </summary>
    private void UpdateHealthBarWithEffects()
    {
        int currentHealth = playerManager.CurrentHealth;
        int currentMaxHealth = playerManager.MaxHealth;
        
        // 如果最大血量改变，更新
        if (maxHealth != currentMaxHealth)
        {
            maxHealth = currentMaxHealth;
            currentDisplayHealth = currentHealth;
            currentDisplayHealthInt = currentHealth;
            targetHealth = currentHealth;
            lastHealth = currentHealth;
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
        if (healthBarFill != null)
        {
            float healthPercent = currentDisplayHealth / maxHealth;
            healthPercent = Mathf.Clamp01(healthPercent);
            healthBarFill.fillAmount = healthPercent;
        }
        
        // 更新血量文字显示（使用整数跳动）
        if (healthText != null)
        {
            currentDisplayHealthInt = Mathf.Clamp(currentDisplayHealthInt, 0, maxHealth);
            healthText.text = $"HP: {currentDisplayHealthInt}/{maxHealth}";
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
            x => currentDisplayHealth = x,
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
            });
            
            healthNumberTween = numberSequence;
        }
        else
        {
            // 如果血量没有变化，直接设置
            currentDisplayHealthInt = endHealthInt;
        }
    }
    
    /// <summary>
    /// 播放Swap文本脉冲效果（类似回血脉冲）
    /// </summary>
    public void PlaySwapTextPulse()
    {
        if (swapCountText == null)
            return;
        
        RectTransform swapTextRect = swapCountText.transform as RectTransform;
        if (swapTextRect == null)
            return;
        
        // 如果前一个脉冲动画未完成，先恢复初始状态
        if (swapTextPulseTween != null && swapTextPulseTween.IsActive())
        {
            swapTextPulseTween.Kill();
            swapTextRect.localScale = originalSwapTextScale;
        }
        
        // 播放脉冲动画（放大缩小）
        Sequence pulseSequence = DOTween.Sequence();
        float scalePerPulse = healPulseDuration / healPulseCount;
        
        for (int i = 0; i < healPulseCount; i++)
        {
            pulseSequence.Append(swapTextRect.DOScale(originalSwapTextScale * healPulseScale, scalePerPulse * 0.5f).SetEase(Ease.OutQuad));
            pulseSequence.Append(swapTextRect.DOScale(originalSwapTextScale, scalePerPulse * 0.5f).SetEase(Ease.InQuad));
        }
        
        swapTextPulseTween = pulseSequence.OnComplete(() =>
        {
            // 动画完成后确保恢复原始缩放
            swapTextRect.localScale = originalSwapTextScale;
        });
    }
    
    /// <summary>
    /// 播放金币文本脉冲效果（类似回血脉冲）
    /// </summary>
    private void PlayGoldTextPulse()
    {
        if (goldText == null)
            return;
        
        RectTransform goldTextRect = goldText.transform as RectTransform;
        if (goldTextRect == null)
            return;
        
        // 如果前一个脉冲动画未完成，先恢复初始状态
        if (goldTextPulseTween != null && goldTextPulseTween.IsActive())
        {
            goldTextPulseTween.Kill();
            goldTextRect.localScale = originalGoldTextScale;
        }
        
        // 播放脉冲动画（放大缩小）
        Sequence pulseSequence = DOTween.Sequence();
        float scalePerPulse = healPulseDuration / healPulseCount;
        
        for (int i = 0; i < healPulseCount; i++)
        {
            pulseSequence.Append(goldTextRect.DOScale(originalGoldTextScale * healPulseScale, scalePerPulse * 0.5f).SetEase(Ease.OutQuad));
            pulseSequence.Append(goldTextRect.DOScale(originalGoldTextScale, scalePerPulse * 0.5f).SetEase(Ease.InQuad));
        }
        
        goldTextPulseTween = pulseSequence.OnComplete(() =>
        {
            // 动画完成后确保恢复原始缩放
            goldTextRect.localScale = originalGoldTextScale;
        });
    }
    
    /// <summary>
    /// 显示金币增加提示
    /// </summary>
    private void ShowGoldIncrease(int amount)
    {
        if (goldIncreaseText == null)
            return;
        
        // 停止之前的动画
        if (goldIncreaseTween != null && goldIncreaseTween.IsActive())
        {
            goldIncreaseTween.Kill();
        }
        
        // 设置文本内容
        goldIncreaseText.text = $"+{amount}";
        goldIncreaseText.gameObject.SetActive(true);
        
        // 获取RectTransform
        RectTransform goldIncreaseRect = goldIncreaseText.transform as RectTransform;
        if (goldIncreaseRect == null)
            return;
        
        // 重置位置
        goldIncreaseRect.anchoredPosition = originalGoldIncreasePosition;
        
        // 重置透明度
        Color originalColor = goldIncreaseText.color;
        goldIncreaseText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        
        // 创建上升动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 向上移动并淡出
        Vector2 endPosition = originalGoldIncreasePosition + new Vector3(0, 50f,0); // 向上移动50像素
        sequence.Append(goldIncreaseRect.DOAnchorPos(endPosition, 1f).SetEase(Ease.OutQuad));
        sequence.Join(goldIncreaseText.DOFade(0f, 1f).SetEase(Ease.OutQuad));
        
        // 动画完成后隐藏
        sequence.OnComplete(() =>
        {
            goldIncreaseText.gameObject.SetActive(false);
            goldIncreaseRect.anchoredPosition = originalGoldIncreasePosition;
            goldIncreaseText.color = originalColor;
        });
        
        goldIncreaseTween = sequence;
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
        if (goldIncreaseTween != null)
            goldIncreaseTween.Kill();
        if (goldTextPulseTween != null)
            goldTextPulseTween.Kill();
        if (swapTextPulseTween != null)
            swapTextPulseTween.Kill();
    }
}

