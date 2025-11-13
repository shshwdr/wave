using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 棋盘管理器 - 管理6x8（可调节）的战斗棋盘
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("棋盘设置")]
    [SerializeField] private int boardWidth = 8;
    [SerializeField] private int boardHeight = 6;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector2 boardOffset = Vector2.zero;

    [Header("预制体")]
    [SerializeField] private GameObject tileCellPrefab;

    [Header("颜色设置")]
    [SerializeField] private List<TileColor> availableColors = new List<TileColor> 
    { 
        TileColor.Red, 
        TileColor.Yellow, 
        TileColor.Blue, 
        TileColor.Green 
    };

    private TileCell[,] board;
    private Transform boardParent;
    
    // 颜色权重（用于more/less技能），默认都是1.0
    private float[] colorWeights = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };

    public int Width => boardWidth;
    public int Height => boardHeight;
    public TileCell[,] Board => board;

    private void Awake()
    {
        // 创建棋盘父对象
        boardParent = new GameObject("Board").transform;
        boardParent.SetParent(transform);
        boardParent.localPosition = Vector3.zero;
        
        // 计算居中偏移
        CalculateCenterOffset();
    }

    /// <summary>
    /// 计算棋盘居中偏移
    /// </summary>
    private void CalculateCenterOffset()
    {
        // 计算棋盘总宽度和高度
        float totalWidth = (boardWidth - 1) * tileSize;
        float totalHeight = (boardHeight - 1) * tileSize;
        
        // 居中偏移（负的一半）
        boardOffset = new Vector2(-totalWidth * 0.5f, -totalHeight * 0.5f);
    }

    /// <summary>
    /// 初始化棋盘
    /// </summary>
    public void InitializeBoard(int width = -1, int height = -1)
    {
        if (width > 0) boardWidth = width;
        if (height > 0) boardHeight = height;

        // 重新计算居中偏移
        CalculateCenterOffset();

        // 清空现有棋盘
        ClearBoard();

        // 创建新棋盘
        board = new TileCell[boardWidth, boardHeight];
    }

    /// <summary>
    /// 清空棋盘
    /// </summary>
    public void ClearBoard()
    {
        if (board != null)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    if (board[x, y] != null)
                    {
                        Destroy(board[x, y].gameObject);
                        board[x, y] = null;
                    }
                }
            }
        }

        // 销毁所有子对象
        if (boardParent != null)
        {
            Utils.destroyAllChildren(boardParent);
        }
    }

    /// <summary>
    /// 随机生成棋盘颜色
    /// </summary>
    public void GenerateRandomColors()
    {
        if (board == null || tileCellPrefab == null)
        {
            Debug.LogError("Board not initialized or tileCellPrefab not set!");
            return;
        }

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (board[x, y] == null)
                {
                    // 创建格子
                    GameObject tileObj = Instantiate(tileCellPrefab, boardParent);
                    TileCell tile = tileObj.GetComponent<TileCell>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileCell>();
                    }

                    // 根据权重随机颜色
                    TileColor randomColor = GetWeightedRandomColor();
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    // 设置位置
                    Vector3 worldPos = GridToWorldPosition(gridPos);
                    tileObj.transform.position = worldPos;

                    // 初始化
                    tile.Init(randomColor, gridPos);
                    board[x, y] = tile;
                }
                else
                {
                    // 更新现有格子颜色（根据权重）
                    TileColor randomColor = GetWeightedRandomColor();
                    board[x, y].SetColor(randomColor);
                }
            }
        }
    }

    /// <summary>
    /// 网格坐标转世界坐标
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float x = gridPos.x * tileSize + boardOffset.x;
        float y = gridPos.y * tileSize + boardOffset.y;
        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - boardOffset.x) / tileSize);
        int y = Mathf.RoundToInt((worldPos.y - boardOffset.y) / tileSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 获取格子（检查边界）
    /// </summary>
    public TileCell GetTile(Vector2Int gridPos)
    {
        if (IsValidPosition(gridPos))
        {
            return board[gridPos.x, gridPos.y];
        }
        return null;
    }

    /// <summary>
    /// 检查位置是否有效
    /// </summary>
    public bool IsValidPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < boardWidth && 
               gridPos.y >= 0 && gridPos.y < boardHeight;
    }

    /// <summary>
    /// 交换两个格子
    /// </summary>
    public void SwapTiles(Vector2Int pos1, Vector2Int pos2)
    {
        if (!IsValidPosition(pos1) || !IsValidPosition(pos2))
        {
            Debug.LogWarning("Invalid positions for swap!");
            return;
        }

        TileCell tile1 = board[pos1.x, pos1.y];
        TileCell tile2 = board[pos2.x, pos2.y];

        if (tile1 == null || tile2 == null)
        {
            return;
        }
        
        // 检查是否有dirty或disabled的tile，这些tile不能参与交换
        if (tile1.IsDirty || tile2.IsDirty || tile1.IsDisabled || tile2.IsDisabled)
        {
            Debug.LogWarning("Cannot swap dirty or disabled tiles!");
            return;
        }

        // 交换数据
        board[pos1.x, pos1.y] = tile2;
        board[pos2.x, pos2.y] = tile1;

        tile1.SetGridPosition(pos2);
        tile2.SetGridPosition(pos1);

        // 动画
        Vector3 pos1World = GridToWorldPosition(pos1);
        Vector3 pos2World = GridToWorldPosition(pos2);

        tile1.SwapAnimation(pos2World);
        tile2.SwapAnimation(pos1World);
    }

    /// <summary>
    /// 获取连通的所有同色格子（上下左右）
    /// </summary>
    public List<Vector2Int> GetConnectedSameColorTiles(Vector2Int startPos)
    {
        List<Vector2Int> connected = new List<Vector2Int>();
        TileCell startTile = GetTile(startPos);
        
        if (startTile == null)
        {
            return connected;
        }
        
        // 如果起始tile是dirty或disabled，不能参与wave生成
        if (startTile.IsDirty || startTile.IsDisabled)
        {
            return connected;
        }

        TileColor targetColor = startTile.Color;
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // 上
            new Vector2Int(0, -1), // 下
            new Vector2Int(-1, 0), // 左
            new Vector2Int(1, 0)   // 右
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            connected.Add(current);

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (visited.Contains(next))
                    continue;

                TileCell tile = GetTile(next);
                // dirty或disabled的tile不能参与wave生成
                if (tile != null && tile.Color == targetColor && !tile.IsDirty && !tile.IsDisabled)
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return connected;
    }

    /// <summary>
    /// 移除格子
    /// </summary>
    public void RemoveTile(Vector2Int gridPos)
    {
        if (!IsValidPosition(gridPos))
            return;

        TileCell tile = board[gridPos.x, gridPos.y];
        if (tile != null)
        {
            tile.DestroyAnimation().OnComplete(() =>
            {
                Destroy(tile.gameObject);
            });
            board[gridPos.x, gridPos.y] = null;
        }
    }

    /// <summary>
    /// 让所有格子从右往左掉落填补空缺
    /// </summary>
    public void ApplyGravity()
    {
        // 按行处理，每行从右往左移动填补空位
        for (int y = 0; y < boardHeight; y++)
        {
            int writeIndex = 0; // 下一个要写入的位置（从左往右）

            // 从左往右遍历，收集所有非空格子，把它们移到左边
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[x, y] != null)
                {
                    if (writeIndex != x)
                    {
                        // 移动格子到新位置（向左移动）
                        board[writeIndex, y] = board[x, y];
                        board[x, y] = null;
                        board[writeIndex, y].SetGridPosition(new Vector2Int(writeIndex, y));
                        
                        // 掉落动画（向左移动）
                        Vector3 targetPos = GridToWorldPosition(new Vector2Int(writeIndex, y));
                        board[writeIndex, y].FallAnimation(targetPos);
                    }
                    writeIndex++;
                }
            }

            // 填充空位（在右侧生成新格子）
            for (int x = writeIndex; x < boardWidth; x++)
            {
                if (board[x, y] == null && tileCellPrefab != null)
                {
                    GameObject tileObj = Instantiate(tileCellPrefab, boardParent);
                    TileCell tile = tileObj.GetComponent<TileCell>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileCell>();
                    }

                    // 根据权重随机颜色
                    TileColor randomColor = GetWeightedRandomColor();
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    // 从右侧生成（动画效果）
                    Vector3 startPos = GridToWorldPosition(new Vector2Int(boardWidth, y));
                    Vector3 targetPos = GridToWorldPosition(gridPos);
                    tileObj.transform.position = startPos;
                    
                    tile.Init(randomColor, gridPos);
                    board[x, y] = tile;
                    
                    // 掉落动画（向左移动）
                    tile.FallAnimation(targetPos);
                }
            }
        }
    }
    
        /// <summary>
    /// 让所有格子从右往左掉落填补空缺
    /// </summary>
    public void ApplyGravityWrong()
    {
        // 按行处理，每行从右往左移动填补空位
        for (int y = 0; y < boardHeight; y++)
        {
            int writeIndex = boardWidth - 1; // 下一个要写入的位置（从右往左）

            // 从左往右遍历，收集所有非空格子
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[x, y] != null)
                {
                    if (writeIndex != x)
                    {
                        // 移动格子到新位置
                        board[writeIndex, y] = board[x, y];
                        board[x, y] = null;
                        board[writeIndex, y].SetGridPosition(new Vector2Int(writeIndex, y));
                        
                        // 掉落动画（向左移动）
                        Vector3 targetPos = GridToWorldPosition(new Vector2Int(writeIndex, y));
                        board[writeIndex, y].FallAnimation(targetPos);
                    }
                    writeIndex--;
                }
            }

            // 填充空位（在右侧生成新格子）
            for (int x = writeIndex; x >= 0; x--)
            {
                if (board[x, y] == null && tileCellPrefab != null)
                {
                    GameObject tileObj = Instantiate(tileCellPrefab, boardParent);
                    TileCell tile = tileObj.GetComponent<TileCell>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileCell>();
                    }

                    // 根据权重随机颜色
                    TileColor randomColor = GetWeightedRandomColor();
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    // 从右侧生成（动画效果）
                    Vector3 startPos = GridToWorldPosition(new Vector2Int(boardWidth, y));
                    Vector3 targetPos = GridToWorldPosition(gridPos);
                    tileObj.transform.position = startPos;
                    
                    tile.Init(randomColor, gridPos);
                    board[x, y] = tile;
                    
                    // 掉落动画（向左移动）
                    tile.FallAnimation(targetPos);
                }
            }
        }
    }

    /// <summary>
    /// 检查是否在棋盘右半部分
    /// </summary>
    public bool IsInRightHalf(Vector2Int gridPos)
    {
        return gridPos.x >= boardWidth / 2;
    }
    
    /// <summary>
    /// 根据权重随机获取颜色（动态检查more/less技能）
    /// </summary>
    private TileColor GetWeightedRandomColor()
    {
        // 动态计算每个颜色的权重（检查more/less技能）
        float[] dynamicWeights = new float[4]; // 0=红，1=黄，2=蓝，3=绿
        
        for (int i = 0; i < availableColors.Count && i < 4; i++)
        {
            // 基础权重为1.0
            float weight = 1.0f;
            
            // 检查该颜色是否有more/less技能
            if (PlayerManager.Instance != null && SkillManager.Instance != null)
            {
                List<string> skillIdentifiers = PlayerManager.Instance.GetWaveSkills(i);
                foreach (var identifier in skillIdentifiers)
                {
                    if (SkillManager.Instance.HasSkill(identifier))
                    {
                        SkillInfo skillInfo = CSVLoader.Instance.cardInfoMap[identifier];
                        if (skillInfo != null)
                        {
                            if (skillInfo.effect == "more")
                            {
                                int value = SkillManager.Instance.GetSkillValue(identifier);
                                weight += value / 100f; // 增加百分比
                            }
                            else if (skillInfo.effect == "less")
                            {
                                int value = SkillManager.Instance.GetSkillValue(identifier);
                                weight -= value / 100f; // 减少百分比
                            }
                        }
                    }
                }
            }
            
            dynamicWeights[i] = Mathf.Max(0f, weight); // 确保权重不为负
        }
        
        // 计算总权重
        float totalWeight = 0f;
        for (int i = 0; i < availableColors.Count && i < 4; i++)
        {
            totalWeight += dynamicWeights[i];
        }
        
        if (totalWeight <= 0f)
        {
            // 如果总权重为0或负数，使用均匀随机
            return availableColors[Random.Range(0, availableColors.Count)];
        }
        
        // 随机一个0到totalWeight之间的值
        float randomValue = Random.Range(0f, totalWeight);
        
        // 根据权重选择颜色
        float currentWeight = 0f;
        for (int i = 0; i < availableColors.Count && i < 4; i++)
        {
            currentWeight += dynamicWeights[i];
            if (randomValue <= currentWeight)
            {
                return availableColors[i];
            }
        }
        
        // 如果没找到（理论上不应该发生），返回第一个颜色
        return availableColors[0];
    }
    
    /// <summary>
    /// 修改颜色权重（用于more/less技能）
    /// </summary>
    public void ModifyColorWeight(int colorIndex, float delta)
    {
        if (colorIndex >= 0 && colorIndex < colorWeights.Length)
        {
            colorWeights[colorIndex] = Mathf.Max(0f, colorWeights[colorIndex] + delta);
        }
    }
    
    /// <summary>
    /// 重置所有颜色权重为1.0
    /// </summary>
    public void ResetColorWeights()
    {
        for (int i = 0; i < colorWeights.Length; i++)
        {
            colorWeights[i] = 1.0f;
        }
    }
}

