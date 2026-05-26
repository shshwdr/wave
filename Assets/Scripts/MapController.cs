using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图总控制（纯UI）
/// </summary>
public class MapController : MonoBehaviour
{
    [Header("地图引用")]
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private Transform islandsParent;
    [SerializeField] private RectTransform playerSprite;

    private readonly List<IslandController> islandControllers = new List<IslandController>();
    private IslandController currentIsland;
    private readonly HashSet<MapNode> usedShopNodes = new HashSet<MapNode>();
    private bool revealAllNodesCheat;

    private void Update()
    {
        if (mapRoot != null && !mapRoot.activeInHierarchy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.M) && MainGameManager.Instance != null && MainGameManager.Instance.useCheat)
        {
            revealAllNodesCheat = !revealAllNodesCheat;
            if (currentIsland != null)
            {
                currentIsland.SetRevealAllNodesCheat(revealAllNodesCheat);
            }
        }
    }

    public void OpenMap()
    {
        InitMap();
        SetMapVisible(true);
        if (currentIsland != null)
        {
            currentIsland.RefreshNodeInteraction();
        }
        MovePlayerToCurrentIsland();
    }

    public void CloseMap()
    {
        SetMapVisible(false);
    }

    public void InitMap()
    {
        CollectIslands();
        if (islandControllers.Count == 0)
        {
            Debug.LogWarning("MapController: 未找到IslandController");
            return;
        }

        int targetIslandId = GetCurrentLevelIslandId();
        currentIsland = FindIslandController(targetIslandId);
        if (currentIsland == null)
        {
            currentIsland = islandControllers[0];
        }

        for (int i = 0; i < islandControllers.Count; i++)
        {
            bool isActive = islandControllers[i] == currentIsland;
            islandControllers[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                islandControllers[i].SetRevealAllNodesCheat(revealAllNodesCheat);
                islandControllers[i].Init(OnNodeClicked);
            }
        }

        MovePlayerToCurrentIsland();
    }

    private int GetCurrentLevelIslandId()
    {
        if (MainGameManager.Instance == null || LevelManager.Instance == null)
        {
            return 0;
        }

        LevelInfo levelInfo = LevelManager.Instance.GetLevelByIndex(MainGameManager.Instance.NextBattleLevelIndex);
        if (levelInfo != null)
        {
            return levelInfo.island;
        }

        return 0;
    }

    private IslandController FindIslandController(int islandId)
    {
        foreach (IslandController island in islandControllers)
        {
            if (island.IslandId == islandId)
            {
                return island;
            }
        }

        if (islandId >= 0 && islandId < islandControllers.Count)
        {
            return islandControllers[islandId];
        }

        return null;
    }

    private void OnNodeClicked(MapNode node)
    {
        if (node == null || currentIsland == null)
        {
            return;
        }

        string nodeType = (node.Type ?? string.Empty).ToLower();

        if (nodeType == "shop" && usedShopNodes.Contains(node))
        {
            return;
        }

        CloseMap();

        switch (nodeType)
        {
            case "event":
                StartCoroutine(HandleEventNode(node));
                return;
            case "shop":
                HandleShopNode(node);
                return;
            case "heal":
                HandleHealNode(node);
                return;
            default:
                currentIsland.MarkNodeCompleted(node);
                MainGameManager.Instance.StartBattleFromMap(node);
                return;
        }
    }

    private void HandleShopNode(MapNode node)
    {
        usedShopNodes.Add(node);
        MainGameManager.Instance.ShowMapShop(() =>
        {
            currentIsland.MarkNodeCompleted(node);
            MainGameManager.Instance.OpenMap();
        });
    }

    private void HandleHealNode(MapNode node)
    {
        currentIsland.MarkNodeCompleted(node);

        if (PlayerManager.Instance != null)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(PlayerManager.Instance.MaxHealth * 0.3f));
            PlayerManager.Instance.Heal(healAmount);
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowToast($"恢复了 {healAmount} 点生命（30%）");
            }
        }

        MainGameManager.Instance.OpenMap();
    }

    private IEnumerator HandleEventNode(MapNode node)
    {
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu == null)
        {
            Debug.LogWarning("MapController: 未找到EventMenu，事件节点结束后直接回地图");
            currentIsland.MarkNodeCompleted(node);
            MainGameManager.Instance.OpenMap();
            yield break;
        }

        bool eventCompleted = false;
        eventMenu.ShowEvent(() => { eventCompleted = true; });

        while (!eventCompleted)
        {
            yield return null;
        }

        currentIsland.MarkNodeCompleted(node);
        MainGameManager.Instance.OpenMap();
    }

    private void MovePlayerToCurrentIsland()
    {
        if (currentIsland == null)
        {
            return;
        }

        if (playerSprite != null)
        {
            currentIsland.MovePlayerSpriteToCurrentNode(playerSprite);
            if (currentIsland.CurrentNode == null && currentIsland.CharacterPos != null)
            {
                playerSprite.anchoredPosition = currentIsland.CharacterPos.anchoredPosition;
            }

            Vector3 playerScale = playerSprite.localScale;
            float targetDirection = currentIsland.CharacterPos != null && currentIsland.CharacterPos.localScale.x < 0f ? -1f : 1f;
            playerScale.x = Mathf.Abs(playerScale.x) * targetDirection;
            playerSprite.localScale = playerScale;
        }
    }

    private void CollectIslands()
    {
        islandControllers.Clear();
        Transform parent = islandsParent != null ? islandsParent : transform;
        islandControllers.AddRange(parent.GetComponentsInChildren<IslandController>(true));
    }

    private void SetMapVisible(bool visible)
    {
        if (mapRoot != null)
        {
            mapRoot.SetActive(visible);
            return;
        }

        gameObject.SetActive(visible);
    }
}
