using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个岛屿控制器
/// </summary>
public class IslandController : MonoBehaviour
{
    [Header("岛屿配置")]
    [SerializeField] private Transform mapNodeParent;
    [SerializeField] private RectTransform characterPos;
    [SerializeField] private int nodeCount = 9;
    [SerializeField] private Image linePrefab;
    [SerializeField] private RectTransform lineParent;

    private readonly List<MapNode> allNodes = new List<MapNode>();
    private readonly List<MapNode> activeNodes = new List<MapNode>();
    private readonly List<Image> spawnedLines = new List<Image>();
    private readonly Dictionary<MapNode, HashSet<MapNode>> graph = new Dictionary<MapNode, HashSet<MapNode>>();
    private readonly HashSet<MapNode> completedNodes = new HashSet<MapNode>();
    private readonly List<MapNode> unlockedNodes = new List<MapNode>();

    public RectTransform CharacterPos => characterPos;
    public IReadOnlyList<MapNode> ActiveNodes => activeNodes;

    private void Awake()
    {
        CollectNodes();
    }

    public void Init(System.Action<MapNode> onNodeClicked)
    {
        CollectNodes();
        ClearLines();
        completedNodes.Clear();
        unlockedNodes.Clear();

        SelectActiveNodes();
        BuildNodeTypes();
        BuildConnections();

        foreach (MapNode node in allNodes)
        {
            bool active = activeNodes.Contains(node);
            node.gameObject.SetActive(active);
            node.OnNodeClicked -= onNodeClicked;
            if (active)
            {
                node.OnNodeClicked += onNodeClicked;
                node.SetInteractable(false);
            }
        }

        MapNode startNode = GetNearestNode(characterPos != null ? characterPos.anchoredPosition : Vector2.zero, activeNodes);
        SetOnlyStartNodeInteractable(startNode);
    }

    public void MarkNodeCompleted(MapNode node)
    {
        if (node == null)
        {
            return;
        }

        completedNodes.Add(node);
        UnlockConnectedNodes(node);
        node.SetInteractable(false);
    }

    public void DisableAllNodeInteraction()
    {
        foreach (MapNode node in activeNodes)
        {
            node.SetInteractable(false);
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

        int targetCount = Mathf.Clamp(nodeCount, 1, allNodes.Count);
        List<MapNode> pool = new List<MapNode>(allNodes);

        while (activeNodes.Count < targetCount && pool.Count > 0)
        {
            int randomIndex = Random.Range(0, pool.Count);
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

        MapNode startNode = GetNearestNode(characterPos != null ? characterPos.anchoredPosition : Vector2.zero, activeNodes);
        List<MapNode> candidates = new List<MapNode>(activeNodes);
        if (startNode != null)
        {
            startNode.SetType("battle");
            candidates.Remove(startNode);
        }

        int battleTotal = Mathf.Min(7, activeNodes.Count);
        int eventTotal = Mathf.Min(2, Mathf.Max(0, activeNodes.Count - battleTotal));
        int assignedBattle = startNode != null ? 1 : 0;

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

            node.SetType("battle");
        }
    }

    private void BuildConnections()
    {
        graph.Clear();
        foreach (MapNode node in activeNodes)
        {
            graph[node] = new HashSet<MapNode>();
        }

        // 先按“最近距离+1.5倍阈值”连线
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

        // 再把不连通的组拼起来
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

    private void UnlockConnectedNodes(MapNode node)
    {
        if (!graph.TryGetValue(node, out HashSet<MapNode> connected))
        {
            return;
        }

        foreach (MapNode next in connected)
        {
            if (completedNodes.Contains(next))
            {
                continue;
            }

            if (!unlockedNodes.Contains(next))
            {
                unlockedNodes.Add(next);
            }

            next.SetInteractable(false);
        }
    }

    private void SetOnlyStartNodeInteractable(MapNode startNode)
    {
        foreach (MapNode node in activeNodes)
        {
            bool isStartNode = node == startNode;
            node.SetInteractable(isStartNode);
            if (isStartNode && !unlockedNodes.Contains(node))
            {
                unlockedNodes.Add(node);
            }
        }
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
                        // 节点删到剩两条边时：30%继续检查，70%直接到下一个节点
                        if (Random.value > 0.3f)
                        {
                            break;
                        }
                    }

                    MapNode other = neighbours[j];

                    // 无向边仅处理一次
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
                // 无向边仅生成一次
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

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
