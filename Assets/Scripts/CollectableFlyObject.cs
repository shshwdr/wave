using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single UI icon that flies from one screen position to another, then returns to the pool.
/// </summary>
public class CollectableFlyObject : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform rectTransform;

    private Tween flyTween;
    private Action onComplete;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public Image IconImage
    {
        get
        {
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);
            return iconImage;
        }
    }

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
    }

    public void Play(
        Sprite sprite,
        Vector3 worldStart,
        Vector3 worldEnd,
        float duration,
        float delay,
        float angleDegrees,
        Action completeCallback)
    {
        onComplete = completeCallback;
        KillTween();

        gameObject.SetActive(true);

        if (IconImage != null)
        {
            IconImage.sprite = sprite;
            IconImage.enabled = sprite != null;
            IconImage.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.05f);
        }

        Vector3 start = worldStart + (Vector3)(UnityEngine.Random.insideUnitCircle * 12f);
        Vector3 end = worldEnd;
        Vector3 direction = (end - start).sqrMagnitude > 0.01f ? (end - start).normalized : Vector3.right;
        Vector3 skewedDirection = Quaternion.Euler(0f, 0f, angleDegrees) * direction;
        start += (Vector3)(skewedDirection * UnityEngine.Random.Range(4f, 14f));

        transform.position = start;
        transform.localScale = Vector3.one;

        flyTween = transform
            .DOMove(end, duration)
            .SetDelay(delay)
            .SetEase(Ease.InBack)
            .OnComplete(HandleComplete);
    }

    public void StopAndRelease()
    {
        KillTween();
        onComplete = null;
        gameObject.SetActive(false);
    }

    private void HandleComplete()
    {
        Action callback = onComplete;
        onComplete = null;
        flyTween = null;
        callback?.Invoke();
    }

    private void KillTween()
    {
        if (flyTween != null && flyTween.IsActive())
            flyTween.Kill();
        flyTween = null;
    }

    private void OnDisable()
    {
        KillTween();
    }
}
