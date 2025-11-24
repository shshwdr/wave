using System.Collections;
using UnityEngine;

/// <summary>
/// 物体切换组件：每switchTime在初始状态和设定状态之间切换位置、大小、旋转
/// </summary>
public class TransformSwitcher : MonoBehaviour
{
    [Header("切换设置")]
    [SerializeField] private float switchTime = 0.25f; // 切换时间间隔
    
    [Header("切换状态（Vector3.zero, 1, 0 表示完全不变）")]
    [SerializeField] private Vector3 switchPosition = Vector3.zero; // 切换位置
    [SerializeField] private float switchScale = 1f; // 切换大小倍数（1表示不变，在originalScale*1和originalScale*switchScale间切换）
    [SerializeField] private float switchRotation = 0f; // 切换旋转角度（0表示不变，360度）
    
    private RectTransform rectTransform; // UI Transform
    private Transform normalTransform; // 普通 Transform
    
    private Vector3 initialPosition; // 初始位置
    private Vector3 initialScale; // 初始大小（Vector3）
    private float initialRotation; // 初始旋转
    
    private bool isInSwitchState = false; // 当前是否在切换状态
    private Coroutine switchCoroutine; // 切换协程
    
    private void Awake()
    {
        // 尝试获取 RectTransform（UI）
        rectTransform = GetComponent<RectTransform>();
        // 如果没有 RectTransform，获取普通 Transform
        if (rectTransform == null)
        {
            normalTransform = transform;
        }
    }
    
    private void OnEnable()
    {
        // 保存初始状态
        SaveInitialState();
        // 启动切换协程
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }
        switchCoroutine = StartCoroutine(SwitchCoroutine());
    }
    
    private void OnDisable()
    {
        // 停止切换协程
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
        // 恢复到初始状态
        RestoreInitialState();
    }
    
    /// <summary>
    /// 保存初始状态
    /// </summary>
    private void SaveInitialState()
    {
        if (rectTransform != null)
        {
            initialPosition = rectTransform.anchoredPosition3D;
            initialScale = rectTransform.localScale; // 记录完整的Vector3 scale
            initialRotation = rectTransform.localEulerAngles.z;
        }
        else if (normalTransform != null)
        {
            initialPosition = normalTransform.localPosition;
            initialScale = normalTransform.localScale; // 记录完整的Vector3 scale
            initialRotation = normalTransform.localEulerAngles.z;
        }
    }
    
    /// <summary>
    /// 恢复到初始状态
    /// </summary>
    private void RestoreInitialState()
    {
        ApplyState(initialPosition, initialScale * 1f, initialRotation);
        isInSwitchState = false;
    }
    
    /// <summary>
    /// 切换协程
    /// </summary>
    private IEnumerator SwitchCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchTime);
            
            // 切换状态
            if (isInSwitchState)
            {
                // 切换到初始状态：originalScale * 1
                ApplyState(initialPosition, initialScale * 1f, initialRotation);
                isInSwitchState = false;
            }
            else
            {
                // 切换到设定状态
                // 如果 switchPosition 是 Vector3.zero，则位置不变（使用初始位置）
                Vector3 targetPosition = switchPosition != Vector3.zero 
                    ? initialPosition + switchPosition 
                    : initialPosition;
                // 在 originalScale * 1 和 originalScale * switchScale 间切换
                Vector3 targetScale = initialScale * switchScale;
                // 如果 switchRotation 是 0，则旋转不变（使用初始旋转）
                float targetRotation = switchRotation != 0f 
                    ? switchRotation 
                    : initialRotation;
                
                ApplyState(targetPosition, targetScale, targetRotation);
                isInSwitchState = true;
            }
        }
    }
    
    /// <summary>
    /// 应用状态（位置、大小、旋转）
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="scale">目标大小（Vector3）</param>
    /// <param name="rotation">目标旋转</param>
    private void ApplyState(Vector3 position, Vector3 scale, float rotation)
    {
        if (rectTransform != null)
        {
            // UI Transform
            rectTransform.anchoredPosition3D = position;
            rectTransform.localScale = scale;
            Vector3 eulerAngles = rectTransform.localEulerAngles;
            eulerAngles.z = rotation;
            rectTransform.localEulerAngles = eulerAngles;
        }
        else if (normalTransform != null)
        {
            // 普通 Transform
            normalTransform.localPosition = position;
            normalTransform.localScale = scale;
            Vector3 eulerAngles = normalTransform.localEulerAngles;
            eulerAngles.z = rotation;
            normalTransform.localEulerAngles = eulerAngles;
        }
    }
}

