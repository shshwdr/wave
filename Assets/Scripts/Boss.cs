using UnityEngine;
using DG.Tweening;

/// <summary>
/// Boss类，继承Enemy
/// </summary>
public class Boss : Enemy
{
    [Header("Boss设置")]
    [SerializeField] private int bossHeight = 3; // boss高度（格子数）
    
    private Vector2Int startGridPos; // 起始网格位置
    private Vector3 startWorldPos; // 起始世界位置
    private bool movingDown = true; // 是否向下移动
    private int moveCount = 0; // 移动计数
    private BoardManager boardManager;
    private bool isMoving = false; // 是否正在移动
    
    /// <summary>
    /// 初始化Boss
    /// </summary>
    public void InitBoss(Vector2Int gridPos, int health, EnemyInfo info, BoardManager board)
    {
        boardManager = board;
        startGridPos = gridPos;
        startWorldPos = boardManager != null ? boardManager.GridToWorldPosition(gridPos) : Vector3.zero;
        
        // 调用父类的Init方法
        Init(gridPos, health, info);
        
        // 设置初始位置
        if (boardManager != null)
        {
            transform.position = startWorldPos;
        }
    }
    
    /// <summary>
    /// 开始Boss移动（每回合调用）
    /// </summary>
    public void StartMove()
    {
        if (isMoving || IsDead || boardManager == null)
            return;
            
        isMoving = true;
        
        // 计算目标位置（每次移动1格）
        Vector2Int targetGridPos;
        if (movingDown)
        {
            // 向下移动1格
            targetGridPos = new Vector2Int(startGridPos.x, startGridPos.y - 1);
            moveCount++;
            
            // 如果已经向下移动了bossHeight次，改为向上移动
            if (moveCount >= bossHeight)
            {
                movingDown = false;
                moveCount = 0;
            }
        }
        else
        {
            // 向上移动1格
            targetGridPos = new Vector2Int(startGridPos.x, startGridPos.y + 1);
            moveCount++;
            
            // 如果已经向上移动了bossHeight次，改为向下移动
            if (moveCount >= bossHeight)
            {
                movingDown = true;
                moveCount = 0;
            }
        }
        
        // 计算目标世界位置
        Vector3 targetWorldPos = boardManager.GridToWorldPosition(targetGridPos);
        
        // 执行移动动画（移动1格的时间，使用父类的moveSpeed）
        float moveDuration = 1f / base.moveSpeed;
        transform.DOMove(targetWorldPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;
                // 更新网格位置（用于碰撞检测）
                UpdateGridPosition(targetGridPos);
                startGridPos = targetGridPos; // 更新起始位置
            });
    }
    
    /// <summary>
    /// 更新Boss的网格位置（用于移动后更新）
    /// </summary>
    private void UpdateGridPosition(Vector2Int newPos)
    {
        // 使用反射来设置private字段gridPosition
        var field = typeof(Enemy).GetField("gridPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, newPos);
        }
    }
    
    /// <summary>
    /// 重写TakeAction方法，Boss不执行攻击和技能，只移动（移动由MainGameManager控制）
    /// </summary>
    public override void TakeAction()
    {
        // Boss不执行攻击和技能，移动由MainGameManager的StartMove()控制
        // 所以这里什么都不做
        return;
    }
    
    /// <summary>
    /// 重写Die方法，Boss死亡时立即结束战斗
    /// </summary>
    public override void Die()
    {
        // 先调用父类的Die方法
        base.Die();
        
        // Boss死亡时立即结束战斗（胜利）
        MainGameManager instance = UnityEngine.Object.FindObjectOfType<MainGameManager>();
        if (instance != null)
        {
            instance.CompleteLevel();
        }
    }
}

