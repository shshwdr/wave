using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 一场战斗的游戏控制 - 回合制战斗系统
/// </summary>
public class MainGameManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Camera mainCamera;

    [Header("波浪设置")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform waveParent;

    [Header("游戏设置")]
    [SerializeField] private float tileMoveDistance = 1f;
    [SerializeField] private float enemyMoveDistance = 1f;
    [SerializeField] private float enemyMoveDuration = 0.5f;

    private enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Processing,        // 处理中（波浪攻击、敌人移动等）
        GameOver
    }

    private GameState currentState = GameState.PlayerTurn;
    private bool isProcessing = false;  // 是否正在处理（波浪、移动等）
    private Vector2Int selectedTilePos = new Vector2Int(-1, -1);
    private Vector2Int dragStartPos = new Vector2Int(-1, -1);
    private bool isDragging = false;
    
    // 任意位置交换相关
    private Vector2Int firstSwapTilePos = new Vector2Int(-1, -1);
    private bool waitingForSecondSwap = false;
    
    // 高亮显示相关
    private Vector2Int lastHighlightPos = new Vector2Int(-1, -1);
    private List<Vector2Int> highlightedTiles = new List<Vector2Int>();

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
        if (enemyManager == null)
            enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager != null)
            enemyManager.Init(boardManager);

        if (waveParent == null)
        {
            GameObject waveParentObj = new GameObject("Waves");
            waveParentObj.transform.SetParent(transform);
            waveParent = waveParentObj.transform;
        }

        StartBattle();
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        // 清空棋盘
        if (boardManager != null)
        {
            boardManager.ClearBoard();
            boardManager.InitializeBoard();
            boardManager.GenerateRandomColors();
        }

        // 清空敌人
        if (enemyManager != null)
        {
            enemyManager.ClearAllEnemies();
            enemyManager.SpawnEnemiesRandomly();
        }

        currentState = GameState.PlayerTurn;
    }

    private void Update()
    {
        if (currentState == GameState.GameOver)
            return;

        // 检查游戏结束条件
        if (enemyManager != null && enemyManager.HasEnemyAtLeftEdge())
        {
            GameOver();
            return;
        }

        // 玩家回合 - 处理输入和鼠标悬停（只有在没有处理中时）
        if (currentState == GameState.PlayerTurn && !isProcessing)
        {
            HandlePlayerInput();
            HandleMouseHover();
        }
        else if (isProcessing)
        {
            // 处理中时清除高亮
            ClearHighlights();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandlePlayerInput()
    {
        // 处理中时不允许操作
        if (isProcessing)
            return;

        // Shift + 鼠标左键 - 任意位置交换
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    if (!waitingForSecondSwap)
                    {
                        // 选择第一个格子
                        firstSwapTilePos = gridPos;
                        waitingForSecondSwap = true;
                        HighlightTile(gridPos, Color.yellow);
                    }
                    else
                    {
                        // 选择第二个格子并交换
                        if (gridPos != firstSwapTilePos)
                        {
                            ClearHighlights();
                            
                            // 标记为处理中
                            isProcessing = true;
                            currentState = GameState.Processing;
                            
                            boardManager.SwapTiles(firstSwapTilePos, gridPos);
                            
                            // 等待动画完成后进入敌人回合
                            DOVirtual.DelayedCall(0.5f, () =>
                            {
                                isProcessing = false;
                                EndPlayerTurn();
                            });
                        }
                        waitingForSecondSwap = false;
                        firstSwapTilePos = new Vector2Int(-1, -1);
                    }
                }
            }
        }
        else
        {
            // 取消任意位置交换选择
            if (waitingForSecondSwap)
            {
                ClearHighlights();
                waitingForSecondSwap = false;
                firstSwapTilePos = new Vector2Int(-1, -1);
            }

            // 鼠标左键 - 拖动交换（相邻）
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && boardManager.IsValidPosition(gridPos))
                {
                    dragStartPos = gridPos;
                    isDragging = true;
                    selectedTilePos = gridPos;
                }
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector2Int gridPos = GetMouseGridPosition();
                if (boardManager != null && 
                    boardManager.IsValidPosition(gridPos) && 
                    gridPos != dragStartPos &&
                    IsAdjacent(gridPos, dragStartPos))
                {
                // 标记为处理中
                isProcessing = true;
                currentState = GameState.Processing;

                // 交换格子
                boardManager.SwapTiles(dragStartPos, gridPos);
                
                // 等待动画完成后进入敌人回合
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    isProcessing = false;
                    EndPlayerTurn();
                });
                }
                isDragging = false;
            }
        }

        // 鼠标右键 - 消除同色格子
        if (Input.GetMouseButtonDown(1))
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (boardManager != null && boardManager.IsValidPosition(gridPos))
            {
                ClearHighlights();
                EliminateConnectedTiles(gridPos);
            }
        }
    }

    /// <summary>
    /// 处理鼠标悬停
    /// </summary>
    private void HandleMouseHover()
    {
        Vector2Int gridPos = GetMouseGridPosition();
        if (boardManager != null && boardManager.IsValidPosition(gridPos))
        {
            // 如果鼠标位置改变了，更新高亮
            if (gridPos != lastHighlightPos && !waitingForSecondSwap)
            {
                UpdateHighlightTiles(gridPos);
                lastHighlightPos = gridPos;
            }
        }
    }

    /// <summary>
    /// 更新高亮显示的格子
    /// </summary>
    private void UpdateHighlightTiles(Vector2Int mousePos)
    {
        ClearHighlights();

        if (boardManager == null)
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(mousePos);

        // 高亮所有连通的格子
        foreach (var pos in connectedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            if (tile != null)
            {
                tile.SetHighlight(true);
                //tile.SetHighlightColor(Color.cyan); // 使用青色高亮
                highlightedTiles.Add(pos);
            }
        }
    }

    /// <summary>
    /// 高亮单个格子
    /// </summary>
    private void HighlightTile(Vector2Int pos, Color color)
    {
        if (boardManager == null)
            return;

        TileCell tile = boardManager.GetTile(pos);
        if (tile != null)
        {
            tile.SetHighlight(true);
            tile.SetHighlightColor(color);
            highlightedTiles.Add(pos);
        }
    }

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    private void ClearHighlights()
    {
        if (boardManager == null)
            return;

        foreach (var pos in highlightedTiles)
        {
            TileCell tile = boardManager.GetTile(pos);
            if (tile != null)
            {
                tile.SetHighlight(false);
            }
        }
        highlightedTiles.Clear();
    }

    /// <summary>
    /// 获取鼠标所在的网格位置
    /// </summary>
    private Vector2Int GetMouseGridPosition()
    {
        if (mainCamera == null)
            return Vector2Int.zero;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (boardManager != null)
        {
            return boardManager.WorldToGridPosition(mouseWorldPos);
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// 检查两个位置是否相邻
    /// </summary>
    private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    /// <summary>
    /// 消除连通同色格子
    /// </summary>
    private void EliminateConnectedTiles(Vector2Int startPos)
    {
        if (boardManager == null || isProcessing)
            return;

        // 获取所有连通的同色格子
        List<Vector2Int> connectedTiles = boardManager.GetConnectedSameColorTiles(startPos);

        if (connectedTiles.Count == 0) // 如果没有连通格子，不执行消除
            return;

        // 标记为处理中
        isProcessing = true;
        currentState = GameState.Processing;

        // 消除所有连通的格子并创建波浪
        float waveDuration = 2f; // 波浪移动持续时间
        
        foreach (var pos in connectedTiles)
        {
            Vector3 worldPos = boardManager.GridToWorldPosition(pos);
            CreateWave(worldPos);

            boardManager.RemoveTile(pos);
        }

        // 立即应用重力（与波浪移动同时进行）
        // 等待一小段时间让消除动画完成，然后开始重力
        DOVirtual.DelayedCall(0.3f, () =>
        {
            boardManager.ApplyGravity();
        });

        // 等待重力动画和波浪移动都完成后，进入敌人回合
        float maxDuration = Mathf.Max(waveDuration, 0.8f); // 重力动画大约0.8秒
        DOVirtual.DelayedCall(maxDuration + 0.2f, () =>
        {
            isProcessing = false;
            EndPlayerTurn();
        });
    }

    /// <summary>
    /// 创建波浪攻击
    /// </summary>
    private void CreateWave(Vector3 spawnPosition)
    {
        if (wavePrefab == null)
            return;

        GameObject waveObj = Instantiate(wavePrefab, spawnPosition, Quaternion.identity, waveParent);
        Wave wave = waveObj.GetComponent<Wave>();
        if (wave == null)
        {
            wave = waveObj.AddComponent<Wave>();
        }

        wave.Init(spawnPosition);
    }

    /// <summary>
    /// 结束玩家回合
    /// </summary>
    private void EndPlayerTurn()
    {
        currentState = GameState.EnemyTurn;
        isProcessing = true; // 敌人移动时也禁止操作

        // 敌人向左移动
        if (enemyManager != null)
        {
            enemyManager.MoveAllEnemiesLeft(enemyMoveDistance, enemyMoveDuration);

            // 刷新新敌人
            DOVirtual.DelayedCall(enemyMoveDuration + 0.1f, () =>
            {
                enemyManager.SpawnNewEnemy();

                // 进入下一回合
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    isProcessing = false;
                    currentState = GameState.PlayerTurn;
                });
            });
        }
        else
        {
            isProcessing = false;
            currentState = GameState.PlayerTurn;
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    private void GameOver()
    {
        currentState = GameState.GameOver;
        isProcessing = true; // 游戏结束时禁止操作
        
        // 等待敌人移动动画完成后显示弹窗
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GameOverDialog.ShowGameOver(
                onRestart: () =>
                {
                    // 重新开始游戏
                    StartBattle();
                },
                onQuit: () =>
                {
                    // 退出游戏
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }
            );
        });
    }
}

