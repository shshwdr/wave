using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

/// <summary>
/// Pools collectable fly icons and animates them from a source to a UI target.
/// </summary>
public class CollectableFlyManager : Singleton<CollectableFlyManager>
{
    [Header("Pool")]
    [SerializeField] private CollectableFlyObject flyPrefab;
    [SerializeField] private Transform flyParent;
    [SerializeField] private int initialPoolSize = 12;
    [SerializeField] private int maxPoolSize = 32;

    [Header("Flight")]
    [SerializeField] private float flyDuration = 0.55f;
    [SerializeField] private float spawnInterval = 0.04f;
    [SerializeField] private int maxFlyCount = 15;

    private IObjectPool<CollectableFlyObject> pool;
    private readonly List<CollectableFlyObject> activeObjects = new List<CollectableFlyObject>();

    protected override void Awake()
    {
        base.Awake();
        EnsurePool();
    }

    public static void EnsureInstance()
    {
        if (Instance != null)
            Instance.EnsurePool();
    }

    public static void BringLayerToFront()
    {
        if (Instance == null)
            return;

        Instance.EnsurePool();
    }

    public void FlyToTarget(Sprite sprite, Vector3 worldStart, RectTransform target, int count, Action onAllComplete = null)
    {
        if (sprite == null || target == null || count <= 0)
            return;

        EnsurePool();
        BringLayerToFront();

        int flyCount = Mathf.Clamp(count, 1, maxFlyCount);
        Vector3 worldEnd = target.position;
        int remaining = flyCount;

        for (int i = 0; i < flyCount; i++)
        {
            float delay = i * spawnInterval + UnityEngine.Random.Range(0f, spawnInterval * 0.75f);
            float duration = flyDuration * UnityEngine.Random.Range(0.92f, 1.08f);
            float angle = UnityEngine.Random.Range(-22f, 22f);

            CollectableFlyObject flyObject = pool.Get();
            activeObjects.Add(flyObject);
            flyObject.Play(sprite, worldStart, worldEnd, duration, delay, angle, () =>
            {
                ReleaseFlyObject(flyObject);
                remaining--;
                if (remaining <= 0)
                    onAllComplete?.Invoke();
            });
        }
    }

    private void EnsurePool()
    {
        if (pool != null)
            return;

        if (flyParent == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject parentObj = new GameObject("CollectableFlyLayer", typeof(RectTransform));
            flyParent = parentObj.transform;
            if (canvas != null)
            {
                flyParent.SetParent(canvas.transform, false);
                RectTransform layerRect = parentObj.GetComponent<RectTransform>();
                layerRect.anchorMin = Vector2.zero;
                layerRect.anchorMax = Vector2.one;
                layerRect.offsetMin = Vector2.zero;
                layerRect.offsetMax = Vector2.zero;
            }

            UiSortOrder.ApplySorting(flyParent, UiSortOrder.Fly);
        }

        if (flyPrefab == null)
            flyPrefab = CreateRuntimePrefab();

        pool = new ObjectPool<CollectableFlyObject>(
            CreatePooledItem,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: initialPoolSize,
            maxSize: maxPoolSize);
    }

    private CollectableFlyObject CreateRuntimePrefab()
    {
        GameObject root = new GameObject("CollectableFlyObject", typeof(RectTransform), typeof(CollectableFlyObject));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(48f, 48f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.SetParent(root.transform, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image image = iconObj.GetComponent<Image>();
        image.raycastTarget = false;

        CollectableFlyObject flyObject = root.GetComponent<CollectableFlyObject>();
        return flyObject;
    }

    private CollectableFlyObject CreatePooledItem()
    {
        CollectableFlyObject instance = Instantiate(flyPrefab, flyParent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private static void OnTakeFromPool(CollectableFlyObject flyObject)
    {
        if (flyObject != null)
        {
            flyObject.gameObject.SetActive(true);
            BringLayerToFront();
        }
    }

    private void OnReturnedToPool(CollectableFlyObject flyObject)
    {
        if (flyObject == null)
            return;

        flyObject.StopAndRelease();
        activeObjects.Remove(flyObject);
    }

    private static void OnDestroyPoolObject(CollectableFlyObject flyObject)
    {
        if (flyObject != null)
            Destroy(flyObject.gameObject);
    }

    private void ReleaseFlyObject(CollectableFlyObject flyObject)
    {
        if (flyObject == null || pool == null)
            return;

        pool.Release(flyObject);
    }
}
