using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个岛屿控制器
/// </summary>
public class IslandController : MonoBehaviour
{
    [Header("岛屿配置")]
    [SerializeField] private int islandId;
    [SerializeField] private Transform mapNodeParent;
    [SerializeField] private RectTransform characterPos;
    [SerializeField] private Image linePrefab;
    [SerializeField] private RectTransform lineParent;

    private readonly List<MapNode> allNodes = new List<MapNode>();
    private readonly List<MapNode> activeNodes = new List<MapNode>();
    private readonly List<Image> spawnedLines = new List<Image>();
    private readonly Dictionary<MapNode, HashSet<MapNode>> graph = new Dictionary<MapNode, HashSet<MapNode>>();
    private readonly HashSet<MapNode> unlockedNodes = new HashSet<MapNode>();
    private readonly HashSet<MapNode> visitedNodes = new HashSet<MapNode>();

    private MapNode entryNode;
    private MapNode currentNode;
    private MapNode bossNode;
    private bool mapInitialized;
    private bool revealAllNodesCheat;
    private Action<MapNode> nodeClickHandler;

    public int IslandId => islandId;
    public RectTransform CharacterPos => characterPos;
    public MapNode CurrentNode => currentNode;
    public MapNode BossNode => bossNode;
    public IReadOnlyList<MapNode> ActiveNodes => activeNodes;

    private void Awake()
    {
        CollectNodes();
    }

    public void Init(Action<MapNode> onNodeClicked, bool forceReset = false)
    {
        nodeClickHandler = onNodeClicked;
        CollectNodes();

        if (mapInitialized && !forceReset)
        {
            RefreshMapState();
            return;
        }

        mapInitialized = false;
        revealAllNodesCheat = false;
        ClearLines();
        unlockedNodes.Clear();
        visitedNodes.Clear();
        foreach (MapNode node in allNodes)
        {
            node.SetUsed(false);
        }
        entryNode = null;
        currentNode = null;
        bossNode = null;

        SelectActiveNodes();
        BuildConnections();
        BuildNodeTypes();

        currentNode = null;
        if (entryNode != null)
        {
            unlockedNodes.Add(entryNode);
            visitedNodes.Add(entryNode);
        }

        mapInitialized = true;
        BindNodes();
        RefreshMapState();
    }

    public void SetRevealAllNodesCheat(bool reveal)
    {
        revealAllNodesCheat = reveal;
        RefreshMapState();
    }

    public void MarkNodeCompleted(MapNode node)
    {
        if (node == null)
        {
            return;
        }

        currentNode = node;
        visitedNodes.Add(node);
        unlockedNodes.Add(node);

        if (!IsShopNode(node))
        {
            node.SetUsed(true);
        }

        UnlockAdjacentNodes(node);
        RefreshMapState();
    }

    public void RefreshNodeInteraction()
    {
        if (mapInitialized)
        {
            RefreshMapState();
        }
    }

    public void MovePlayerSpriteToCurrentNode(RectTransform playerSprite)
    {
        if (playerSprite == null || currentNode == null)
        {
            return;
        }

        playerSprite.anchoredPosition = currentNode.Position;
    }

    private void BindNodes()
    {
        foreach (MapNode node in allNodes)
        {
            bool active = activeNodes.Contains(node);
            node.OnNodeClicked -= nodeClickHandler;
            if (active)
            {
                node.OnNodeClicked += nodeClickHandler;
            }
        }
    }

    private void RefreshMapState()
    {
        HashSet<MapNode> visibleNodes = BuildVisibleNodeSet();

        foreach (MapNode node in allNodes)
        {
            bool active = activeNodes.Contains(node);
            if (!active)
            {
                node.SetMapVisible(false);
                continue;
            }

            bool visible = visibleNodes.Contains(node);
            node.SetMapVisible(visible);
            if (!visible)
            {
                node.SetInteractable(false);
                continue;
            }

            // Cheat 模式下：允许点击所有可见节点（不受“已解锁/已使用”限制）。
            bool canClick = revealAllNodesCheat
                ? true
                : (unlockedNodes.Contains(node) && !node.IsUsed);
            node.SetInteractable(canClick);
            node.SetVisited(visitedNodes.Contains(node));
        }

        RefreshLineVisibility(visibleNodes);
    }

    private HashSet<MapNode> BuildVisibleNodeSet()
    {
        HashSet<MapNode> visible = new HashSet<MapNode>();
        if (revealAllNodesCheat)
        {
            foreach (MapNode node in activeNodes)
            {
                visible.Add(node);
            }
            return visible;
        }

        foreach (MapNode node in visitedNodes)
        {
            if (activeNodes.Contains(node))
            {
                visible.Add(node);
            }
        }

        return visible;
    }

    private void RefreshLineVisibility(HashSet<MapNode> visibleNodes)
    {
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            Image line = spawnedLines[i];
            if (line == null)
            {
                continue;
            }

            MapNodeLineBinding binding = line.GetComponent<MapNodeLineBinding>();
            bool show = binding == null || (visibleNodes.Contains(binding.NodeA) && visibleNodes.Contains(binding.NodeB));
            line.gameObject.SetActive(show);
        }
    }

    private void CollectNodes()
    {
        allNodes.Clear();
        Transform parent = mapNodeParent != null ? mapNodeParent : transform;
        allNodes.AddRange(parent.GetComponentsInChildren<MapNode>(true));
    }

    private void SelectActiveNodes()
    {
        activeNodes.Clear();
        if (allNodes.Count == 0)
        {
            return;
        }

        IslandInfo islandInfo = CSVLoader.Instance != null ? CSVLoader.Instance.GetIslandInfo(islandId) : null;
        int targetCount = islandInfo != null ? islandInfo.TotalNodeCount : 9;
        targetCount = Mathf.Clamp(targetCount, 1, allNodes.Count);

        List<MapNode> pool = new List<MapNode>(allNodes);
        while (activeNodes.Count < targetCount && pool.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            activeNodes.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }
    }

    private void BuildNodeTypes()
    {
        if (activeNodes.Count == 0)
        {
            return;
        }

        IslandInfo islandInfo = CSVLoader.Instance != null ? CSVLoader.Instance.GetIslandInfo(islandId) : null;
        int battleTotal = islandInfo != null ? islandInfo.battleCount : Mathf.Min(7, activeNodes.Count);
        int eventTotal = islandInfo != null ? islandInfo.eventCount : Mathf.Min(2, Mathf.Max(0, activeNodes.Count - battleTotal));
        int shopTotal = islandInfo != null ? islandInfo.shopCount : 0;
        int healTotal = islandInfo != null ? islandInfo.healCount : 0;

        int expectedTotal = battleTotal + eventTotal + shopTotal + healTotal;
        if (expectedTotal != activeNodes.Count)
        {
            Debug.LogWarning($"Island {islandId}: island.csv 节点总数({expectedTotal})与选中节点数({activeNodes.Count})不一致，将按 CSV 配额分配并截断/补齐为 battle");
            while (battleTotal + eventTotal + shopTotal + healTotal < activeNodes.Count)
            {
                battleTotal++;
            }
            while (battleTotal + eventTotal + shopTotal + healTotal > activeNodes.Count && battleTotal > 0)
            {
                battleTotal--;
            }
        }

        Vector2 playerPos = characterPos != null ? characterPos.anchoredPosition : Vector2.zero;
        entryNode = GetNearestNode(playerPos, activeNodes);
        bossNode = GetFarthestNode(playerPos, activeNodes);

        foreach (MapNode node in activeNodes)
        {
            node.SetIsBossNode(false);
        }

        List<MapNode> candidates = new List<MapNode>(activeNodes);
        int assignedBattle = 0;

        if (entryNode != null)
        {
            entryNode.SetType("battle");
            candidates.Remove(entryNode);
            assignedBattle++;
        }

        if (bossNode != null && bossNode != entryNode && battleTotal > assignedBattle)
        {
            bossNode.SetType("battle");
            bossNode.SetIsBossNode(true);
            candidates.Remove(bossNode);
            assignedBattle++;
        }
        else if (bossNode != null && bossNode == entryNode)
        {
            bossNode.SetIsBossNode(true);
        }

        AssignBattleToInitialNeighbors(entryNode, candidates, ref assignedBattle);

        // 起点邻接战斗节点可能超出 CSV 的 battleCount，以实际分配为准
        battleTotal = Mathf.Max(battleTotal, assignedBattle);

        Shuffle(candidates);
        foreach (MapNode node in candidates)
        {
            if (assignedBattle < battleTotal)
            {
                node.SetType("battle");
                assignedBattle++;
                continue;
            }

            if (eventTotal > 0)
            {
                node.SetType("event");
                eventTotal--;
                continue;
            }

            if (shopTotal > 0)
            {
                node.SetType("shop");
                shopTotal--;
                continue;
            }

            if (healTotal > 0)
            {
                node.SetType("heal");
                healTotal--;
                continue;
            }

            node.SetType("battle");
        }
    }

    /// <summary>
    /// 开局与起点相连、玩家最初能走到的节点一律为战斗
    /// </summary>
    private void AssignBattleToInitialNeighbors(MapNode startNode, List<MapNode> candidates, ref int assignedBattle)
    {
        if (startNode == null || !graph.TryGetValue(startNode, out HashSet<MapNode> neighbors))
        {
            return;
        }

        foreach (MapNode neighbor in neighbors)
        {
            if (!candidates.Contains(neighbor))
            {
                continue;
            }

            neighbor.SetType("battle");
            candidates.Remove(neighbor);
            assignedBattle++;
        }
    }

    private Vector2 GetPlayerReferencePosition()
    {
        if (currentNode != null)
        {
            return currentNode.Position;
        }

        return characterPos != null ? characterPos.anchoredPosition : Vector2.zero;
    }

    private void BuildConnections()
    {
        graph.Clear();
        foreach (MapNode node in activeNodes)
        {
            graph[node] = new HashSet<MapNode>();
        }

        foreach (MapNode node in activeNodes)
        {
            float nearestDistance = float.MaxValue;
            MapNode nearestNode = null;
            foreach (MapNode other in activeNodes)
            {
                if (other == node)
                {
                    continue;
                }

                float distance = Vector2.Distance(node.Position, other.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNode = other;
                }
            }

            if (nearestNode == null)
            {
                continue;
            }

            Connect(node, nearestNode);
            float threshold = nearestDistance * 1.5f;
            foreach (MapNode other in activeNodes)
            {
                if (other == node)
                {
                    continue;
                }

                float distance = Vector2.Distance(node.Position, other.Position);
                if (distance <= threshold)
                {
                    Connect(node, other);
                }
            }
        }

        while (true)
        {
            List<List<MapNode>> groups = GetConnectedGroups();
            if (groups.Count <= 1)
            {
                break;
            }

            float bestDistance = float.MaxValue;
            MapNode bestA = null;
            MapNode bestB = null;

            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    foreach (MapNode a in groups[i])
                    {
                        foreach (MapNode b in groups[j])
                        {
                            float distance = Vector2.Distance(a.Position, b.Position);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestA = a;
                                bestB = b;
                            }
                        }
                    }
                }
            }

            if (bestA == null || bestB == null)
            {
                break;
            }

            Connect(bestA, bestB);
        }

        PruneRedundantEdges();
        RebuildLinesFromGraph();
    }

    private List<List<MapNode>> GetConnectedGroups()
    {
        List<List<MapNode>> groups = new List<List<MapNode>>();
        HashSet<MapNode> visited = new HashSet<MapNode>();

        foreach (MapNode node in activeNodes)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            List<MapNode> group = new List<MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();
            queue.Enqueue(node);
            visited.Add(node);

            while (queue.Count > 0)
            {
                MapNode current = queue.Dequeue();
                group.Add(current);

                foreach (MapNode next in graph[current])
                {
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    private void UnlockAdjacentNodes(MapNode node)
    {
        if (!graph.TryGetValue(node, out HashSet<MapNode> neighbors))
        {
            return;
        }

        foreach (MapNode next in neighbors)
        {
            unlockedNodes.Add(next);
            visitedNodes.Add(next);
        }
    }

    private static bool IsShopNode(MapNode node)
    {
        return string.Equals(node?.Type, "shop", StringComparison.OrdinalIgnoreCase);
    }

    private void Connect(MapNode a, MapNode b)
    {
        if (a == null || b == null || a == b)
        {
            return;
        }

        if (graph[a].Contains(b))
        {
            return;
        }

        graph[a].Add(b);
        graph[b].Add(a);
    }

    private void PruneRedundantEdges()
    {
        bool removedAny;
        do
        {
            removedAny = false;
            for (int i = 0; i < activeNodes.Count; i++)
            {
                MapNode node = activeNodes[i];
                List<MapNode> neighbours = new List<MapNode>(graph[node]);
                for (int j = 0; j < neighbours.Count; j++)
                {
                    if (graph[node].Count == 2)
                    {
                        if (UnityEngine.Random.value > 0.3f)
                        {
                            break;
                        }
                    }

                    MapNode other = neighbours[j];
                    if (node.GetInstanceID() > other.GetInstanceID())
                    {
                        continue;
                    }

                    graph[node].Remove(other);
                    graph[other].Remove(node);

                    if (IsGraphConnected())
                    {
                        removedAny = true;
                    }
                    else
                    {
                        graph[node].Add(other);
                        graph[other].Add(node);
                    }
                }
            }
        } while (removedAny);
    }

    private bool IsGraphConnected()
    {
        if (activeNodes.Count <= 1)
        {
            return true;
        }

        HashSet<MapNode> visited = new HashSet<MapNode>();
        Queue<MapNode> queue = new Queue<MapNode>();
        queue.Enqueue(activeNodes[0]);
        visited.Add(activeNodes[0]);

        while (queue.Count > 0)
        {
            MapNode current = queue.Dequeue();
            foreach (MapNode next in graph[current])
            {
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == activeNodes.Count;
    }

    private void RebuildLinesFromGraph()
    {
        ClearLines();
        for (int i = 0; i < activeNodes.Count; i++)
        {
            MapNode node = activeNodes[i];
            foreach (MapNode other in graph[node])
            {
                if (node.GetInstanceID() < other.GetInstanceID())
                {
                    SpawnLine(node, other);
                }
            }
        }
    }

    private void SpawnLine(MapNode a, MapNode b)
    {
        if (linePrefab == null)
        {
            return;
        }

        RectTransform parent = lineParent != null ? lineParent : (RectTransform)(mapNodeParent != null ? mapNodeParent : transform);
        Image line = Instantiate(linePrefab, parent);
        line.gameObject.SetActive(true);
        spawnedLines.Add(line);

        MapNodeLineBinding binding = line.gameObject.GetComponent<MapNodeLineBinding>();
        if (binding == null)
        {
            binding = line.gameObject.AddComponent<MapNodeLineBinding>();
        }
        binding.NodeA = a;
        binding.NodeB = b;

        RectTransform lineRect = line.rectTransform;
        Vector2 from = a.Position;
        Vector2 to = b.Position;
        Vector2 dir = to - from;
        float length = dir.magnitude;

        lineRect.anchoredPosition = (from + to) * 0.5f;
        lineRect.sizeDelta = new Vector2(length, lineRect.sizeDelta.y);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void ClearLines()
    {
        for (int i = spawnedLines.Count - 1; i >= 0; i--)
        {
            if (spawnedLines[i] != null)
            {
                Destroy(spawnedLines[i].gameObject);
            }
        }
        spawnedLines.Clear();
    }

    private static MapNode GetNearestNode(Vector2 position, List<MapNode> candidates)
    {
        MapNode nearest = null;
        float best = float.MaxValue;
        foreach (MapNode node in candidates)
        {
            float distance = Vector2.Distance(position, node.Position);
            if (distance < best)
            {
                best = distance;
                nearest = node;
            }
        }

        return nearest;
    }

    private static MapNode GetFarthestNode(Vector2 position, List<MapNode> candidates)
    {
        MapNode farthest = null;
        float best = float.MinValue;
        foreach (MapNode node in candidates)
        {
            float distance = Vector2.Distance(position, node.Position);
            if (distance > best)
            {
                best = distance;
                farthest = node;
            }
        }

        return farthest;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}

/// <summary>
/// 地图连线与节点的绑定，用于按可见性隐藏连线
/// </summary>
public class MapNodeLineBinding : MonoBehaviour
{
    public MapNode NodeA;
    public MapNode NodeB;
}
