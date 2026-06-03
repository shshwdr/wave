using UnityEngine;

/// <summary>
/// 集中管理所有 KeyCode 作弊快捷键
/// </summary>
public class CheatManager : Singleton<CheatManager>
{
    public bool useCheat;

    public bool IsEnabled => useCheat;

    public bool IsShiftSwapActive =>
        useCheat && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

    private void Start()
    {
#if !UNITY_EDITOR
        useCheat = false;
#endif
    }

    private void Update()
    {
        if (!useCheat)
            return;

        HandleGlobalCheats();
        HandleEditModeCheats();
        HandlePuzzleEditCheats();
    }

    /// <summary>R 重启场景，H 清除全局进度标识，I/O 玩家伤害/死亡，P 秒杀敌人，M 地图全开，C 加 10 金币</summary>
    private void HandleGlobalCheats()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            MainGameManager.Instance?.Restart();
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetHasWonGame(false);
                GameDataManager.Instance.SetIsInHardMode(false);
                Debug.Log("已清除全局标识：hasWonGame 和 isInHardMode");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            PlayerManager.Instance?.TakeDamage(40);
            return;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            PlayerManager.Instance?.CheatKill();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            MainGameManager mainGameManager = MainGameManager.Instance;
            if (mainGameManager != null && mainGameManager.IsInPuzzleEditMode)
            {
                mainGameManager.CheatEnterPuzzlePlayMode();
            }
            else
            {
                EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
                enemyManager?.CheatKillAllEnemies();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            MapController mapController = FindObjectOfType<MapController>(true);
            mapController?.ToggleRevealAllNodesCheat();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            PlayerManager.Instance?.AddGold(10);
            Debug.Log("Cheat: +10 gold");
        }
    }

    /// <summary>X 进入 Puzzle 编辑，S/L/P 在编辑模式下保存/加载/试玩</summary>
    private void HandleEditModeCheats()
    {
        MainGameManager mainGameManager = MainGameManager.Instance;
        if (mainGameManager == null || mainGameManager.IsPublishMode)
            return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            mainGameManager.CheatEnterPuzzleEditMode();
            return;
        }

        if (!mainGameManager.IsInPuzzleEditMode)
            return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            mainGameManager.CheatSaveCurrentPuzzle();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            mainGameManager.CheatLoadFirstPuzzle();
        }
    }

    /// <summary>1-4 + 鼠标左/右键编辑 Puzzle 格子</summary>
    private void HandlePuzzleEditCheats()
    {
        MainGameManager mainGameManager = MainGameManager.Instance;
        if (mainGameManager == null || !mainGameManager.IsInPuzzleEditMode)
            return;

        mainGameManager.CheatProcessPuzzleEditInput();
    }
}
