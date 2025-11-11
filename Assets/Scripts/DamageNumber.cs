using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 伤害数字显示
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private static bool damageNumberMoveLeft = true;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float jumpHeight = 0.8f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;

    private void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
            if (damageText == null)
            {
                // 创建TextMeshPro组件
                GameObject textObj = new GameObject("DamageText");
                textObj.transform.SetParent(transform);
                textObj.transform.localPosition = Vector3.zero;
                
                RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                
                //rectTransform.sizeDelta = new Vector2(100, 50);
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                damageText = textObj.AddComponent<TextMeshProUGUI>();
                damageText.fontSize = 30;
                damageText.enableWordWrapping = false;
                damageText.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    public void ShowDamage(int damage, Vector3 worldPosition, bool isHeal = false)
    {
        if (damageText == null)
            return;

        // 设置位置
        transform.position = worldPosition;
        
        // 设置文本和颜色
        damageText.text = isHeal ? $"+{damage}" : $"-{damage}";
        damageText.color = isHeal ? healColor : damageColor;
        
        // 重置初始状态
        transform.localScale = Vector3.one;
        damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, 1f);
        
        // 向上跳跃并淡出
        Vector3 targetPosition = worldPosition + Vector3.up * jumpHeight;

        targetPosition += Vector3.left * (damageNumberMoveLeft ? 1f : -1f) * Random.Range(0.4f,0.8f);
        damageNumberMoveLeft = !damageNumberMoveLeft;
        
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOScale(Vector3.one * 1.5f, duration * 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() => transform.DOScale(Vector3.one, duration * 0.7f)));
        sequence.Join(damageText.DOFade(0f, duration).SetEase(Ease.InQuad));
        
        sequence.OnComplete(() =>
        {
            // 销毁或回收对象
            Destroy(gameObject);
        });
    }

    /// <summary>
    /// 创建伤害数字
    /// </summary>
    public static void CreateDamageNumber(int damage, Vector3 worldPosition, bool isHeal = false)
    {
        //GameObject damageObj = new GameObject("DamageNumber");
        //DamageNumber damageNumber = damageObj.AddComponent<DamageNumber>();
        
        // 如果是世界坐标，需要转换为屏幕坐标显示
        Camera mainCamera = Camera.main;
       // if (mainCamera != null)
       // {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
            
            // 创建Canvas用于显示
            //if (canvas == null)
            //{
                GameObject canvasObj = Instantiate(Resources.Load<GameObject>("damageNumber"));
            //}
            
       // }
        
       canvasObj.GetComponentInChildren<RectTransform>().position = screenPos;
        canvasObj.GetComponent<DamageNumber>().ShowDamage(damage, worldPosition, isHeal);
    }
}

