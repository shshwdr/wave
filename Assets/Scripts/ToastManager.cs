using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Toast管理器 - 管理toast的显示和消失
/// </summary>
public class ToastManager : Singleton<ToastManager>
{
    [Header("Toast设置")]
    [SerializeField] private GameObject toastPrefab; // Toast预制体
    [SerializeField] private Transform toastParent; // Toast父对象
    [SerializeField] private float toastDuration = 3f; // Toast显示时长
    [SerializeField] private float toastSpacing = 80f; // Toast之间的间距
    
    private List<Toast> activeToasts = new List<Toast>(); // 当前活跃的toast列表
    
    private void Awake()
    {
        // 如果没有指定父对象，创建一个
        if (toastParent == null)
        {
            GameObject parentObj = new GameObject("ToastParent");
            parentObj.transform.SetParent(transform);
            toastParent = parentObj.transform;
        }
    }
    
    /// <summary>
    /// 显示toast
    /// </summary>
    public void ShowToast(string message)
    {
        if (toastPrefab == null)
        {
            Debug.LogWarning("Toast prefab is not set!");
            return;
        }
        
        // 创建toast实例
        GameObject toastObj = Instantiate(toastPrefab, toastParent);
        Toast toast = toastObj.GetComponent<Toast>();
        
        if (toast == null)
        {
            toast = toastObj.AddComponent<Toast>();
        }
        
        // 初始化toast
        toast.Init(message, toastDuration, this);
        
        // 添加到活跃列表
        activeToasts.Add(toast);
        
        // 更新所有toast的位置
        UpdateToastPositions();
    }
    
    /// <summary>
    /// 移除toast
    /// </summary>
    public void RemoveToast(Toast toast)
    {
        if (activeToasts.Contains(toast))
        {
            activeToasts.Remove(toast);
            UpdateToastPositions();
        }
    }
    
    /// <summary>
    /// 更新所有toast的位置（从下往上排列）
    /// </summary>
    private void UpdateToastPositions()
    {
        for (int i = 0; i < activeToasts.Count; i++)
        {
            if (activeToasts[i] != null)
            {
                float targetY = i * toastSpacing;
                activeToasts[i].SetTargetPosition(targetY);
            }
        }
    }
}

