using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrolls a tiled UI Image under a Mask by moving it and wrapping one tile.
/// Extra size padding keeps the mask fully covered so the loop looks seamless.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class UITiledImageScroller : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = new Vector2(40f, -40f);
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rect;
    private Image image;
    private Vector2 tileSize;
    private Vector2 offset;
    private Vector2 baseSizeDelta;
    private Vector2 baseAnchoredPosition;
    private bool basesCached;

    private void Awake()
    {
        rect = (RectTransform)transform;
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (rect == null)
            rect = (RectTransform)transform;
        if (image == null)
            image = GetComponent<Image>();

        // Clear any leftover custom material from the previous UV approach.
        image.material = null;
        image.type = Image.Type.Tiled;

        if (!basesCached)
        {
            baseSizeDelta = rect.sizeDelta;
            baseAnchoredPosition = rect.anchoredPosition;
            basesCached = true;
        }

        RefreshTileSize();
        // One full tile of margin on every side so wrap never exposes empty area.
        rect.sizeDelta = baseSizeDelta + tileSize * 2f;
        offset = Vector2.zero;
        rect.anchoredPosition = baseAnchoredPosition;
    }

    private void OnDisable()
    {
        if (rect == null || !basesCached)
            return;

        offset = Vector2.zero;
        rect.anchoredPosition = baseAnchoredPosition;
        rect.sizeDelta = baseSizeDelta;
    }

    private void Update()
    {
        if (tileSize.x < 0.01f || tileSize.y < 0.01f)
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        offset += scrollSpeed * dt;

        float x = Mathf.Repeat(offset.x, tileSize.x);
        float y = Mathf.Repeat(offset.y, tileSize.y);
        rect.anchoredPosition = baseAnchoredPosition + new Vector2(x, y);
    }

    public void SetScrollSpeed(Vector2 speed)
    {
        scrollSpeed = speed;
    }

    private void RefreshTileSize()
    {
        if (image == null || image.sprite == null)
        {
            tileSize = new Vector2(64f, 64f);
            return;
        }

        float mul = image.pixelsPerUnitMultiplier;
        if (mul <= 0f)
            mul = 1f;

        Rect spriteRect = image.sprite.rect;
        tileSize = new Vector2(spriteRect.width / mul, spriteRect.height / mul);
    }
}
