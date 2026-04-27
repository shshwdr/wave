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
    private int currentIslandIndex = 0;

    private void Awake()
    {
        CollectIslands();
        if (mapRoot != null)
        {
            mapRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OpenMap()
    {
        InitMap();

        SetMapVisible(true);
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

        currentIslandIndex = Mathf.Clamp(currentIslandIndex, 0, islandControllers.Count - 1);
        currentIsland = islandControllers[currentIslandIndex];

        for (int i = 0; i < islandControllers.Count; i++)
        {
            islandControllers[i].gameObject.SetActive(i == currentIslandIndex);
            if (i == currentIslandIndex)
            {
                islandControllers[i].Init(OnNodeClicked);
            }
        }

        MovePlayerToCurrentIsland();
    }

    private void OnNodeClicked(MapNode node)
    {
        if (node == null || currentIsland == null)
        {
            return;
        }

        currentIsland.DisableAllNodeInteraction();
        currentIsland.MarkNodeCompleted(node);
        CloseMap();

        string nodeType = (node.Type ?? string.Empty).ToLower();
        if (nodeType == "event")
        {
            StartCoroutine(HandleEventNode());
            return;
        }

        //MainGameManager.Instance.StartBattleFromMap();
    }

    private IEnumerator HandleEventNode()
    {
        EventMenu eventMenu = FindObjectOfType<EventMenu>();
        if (eventMenu == null)
        {
            Debug.LogWarning("MapController: 未找到EventMenu，事件节点结束后直接回地图");
            OpenMap();
            yield break;
        }

        bool eventCompleted = false;
        eventMenu.ShowEvent(() => { eventCompleted = true; });

        while (!eventCompleted)
        {
            yield return null;
        }

        OpenMap();
    }

    private void MovePlayerToCurrentIsland()
    {
        if (playerSprite == null || currentIsland == null || currentIsland.CharacterPos == null)
        {
            return;
        }

        playerSprite.anchoredPosition = currentIsland.CharacterPos.anchoredPosition;

        // 同步角色朝向（仅匹配X方向的正负）
        Vector3 playerScale = playerSprite.localScale;
        float targetDirection = currentIsland.CharacterPos.localScale.x < 0f ? -1f : 1f;
        playerScale.x = Mathf.Abs(playerScale.x) * targetDirection;
        playerSprite.localScale = playerScale;
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
