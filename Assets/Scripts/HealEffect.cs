using UnityEngine;

/// <summary>
/// 回血效果辅助类
/// </summary>
public static class HealEffect
{
    /// <summary>
    /// 在指定位置创建回血效果
    /// </summary>
    public static void CreateHealEffect(Vector3 position)
    {
        GameObject healEffectPrefab = Resources.Load<GameObject>("effect/healEffect");
        if (healEffectPrefab != null)
        {
            GameObject healEffect = Object.Instantiate(healEffectPrefab, position, Quaternion.identity);
            // prefab应该自己处理动画和销毁逻辑
        }
        else
        {
            Debug.LogWarning("无法加载回血效果prefab: effect/healEffect");
        }
    }
}

