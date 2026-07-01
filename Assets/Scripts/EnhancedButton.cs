using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EnhancedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform controlledGraphic;

    [Header("Hover")]
    [SerializeField] private float hoverRotate;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverTime = 0.3f;
    [SerializeField] private Ease hoverEase = Ease.OutElastic;

    [Header("Click")]
    [SerializeField] private float clickRotate;
    [SerializeField] private float clickScale = 1f;
    [SerializeField] private float clickTime = 0.15f;
    [SerializeField] private Ease clickEase = Ease.OutElastic;

    [Header("Settings")]
    [SerializeField] private bool buttonAnim = true;

    private Vector3 baseScale;
    private Vector3 baseEuler;
    private bool isHovered;
    private bool isPressed;
    private Tweener rotateTween;
    private Tweener scaleTween;

    private void Awake()
    {
        CacheBaseTransform();
    }

    private void OnDisable()
    {
        KillTweens();
        ResetToBase();
        isHovered = false;
        isPressed = false;
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisualState();
    }

    private void CacheBaseTransform()
    {
        if (controlledGraphic == null)
            return;

        baseScale = controlledGraphic.localScale;
        baseEuler = controlledGraphic.localEulerAngles;
    }

    private void ResetToBase()
    {
        if (controlledGraphic == null)
            return;

        controlledGraphic.localScale = baseScale;
        controlledGraphic.localEulerAngles = baseEuler;
    }

    private void UpdateVisualState()
    {
        if (!buttonAnim || controlledGraphic == null)
            return;

        float targetZ;
        Vector3 targetScale;
        float duration;
        Ease ease;

        if (isPressed)
        {
            targetZ = baseEuler.z + clickRotate;
            targetScale = baseScale * clickScale;
            duration = clickTime;
            ease = clickEase;
        }
        else if (isHovered)
        {
            targetZ = baseEuler.z + hoverRotate;
            targetScale = baseScale * hoverScale;
            duration = hoverTime;
            ease = hoverEase;
        }
        else
        {
            targetZ = baseEuler.z;
            targetScale = baseScale;
            duration = hoverTime;
            ease = hoverEase;
        }

        ApplyTransform(targetZ, targetScale, duration, ease);
    }

    private void ApplyTransform(float targetZ, Vector3 targetScale, float duration, Ease ease)
    {
        KillTweens();

        Vector3 targetEuler = new Vector3(baseEuler.x, baseEuler.y, targetZ);

        if (duration <= 0f)
        {
            controlledGraphic.localEulerAngles = targetEuler;
            controlledGraphic.localScale = targetScale;
            return;
        }

        rotateTween = controlledGraphic
            .DOLocalRotate(targetEuler, duration)
            .SetEase(ease);

        scaleTween = controlledGraphic
            .DOScale(targetScale, duration)
            .SetEase(ease);
    }

    private void KillTweens()
    {
        if (rotateTween != null && rotateTween.IsActive())
            rotateTween.Kill();

        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();

        rotateTween = null;
        scaleTween = null;
    }
}
