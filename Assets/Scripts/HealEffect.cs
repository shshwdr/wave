using UnityEngine;
using DG.Tweening;

/// <summary>
/// 回血效果辅助类
/// </summary>
public static class HealEffect
{
    /// <summary>
    /// 在被治疗的实体上创建回血效果（作为子对象）
    /// </summary>
    public static void CreateHealEffect(Transform parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("HealEffect: parent transform is null");
            return;
        }
        
        GameObject healEffectPrefab = Resources.Load<GameObject>("effect/healEffect");
        if (healEffectPrefab != null)
        {
            // 作为子对象添加到被治疗的实体上
            GameObject healEffect = Object.Instantiate(healEffectPrefab, parent);
            healEffect.transform.localPosition = Vector3.zero; // 相对于父对象的位置
            
            // 2秒后自动销毁
            Object.Destroy(healEffect, 2f);
        }
        else
        {
            Debug.LogWarning("无法加载回血效果prefab: effect/healEffect");
        }
    }
    
    /// <summary>
    /// 在指定位置创建回血效果（兼容旧代码）
    /// </summary>
    public static void CreateHealEffect(Vector3 position)
    {
        GameObject healEffectPrefab = Resources.Load<GameObject>("effect/healEffect");
        if (healEffectPrefab != null)
        {
            GameObject healEffect = Object.Instantiate(healEffectPrefab, position, Quaternion.identity);
            // 2秒后自动销毁
            Object.Destroy(healEffect, 2f);
        }
        else
        {
            Debug.LogWarning("无法加载回血效果prefab: effect/healEffect");
        }
    }
}

