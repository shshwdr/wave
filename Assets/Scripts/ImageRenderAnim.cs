using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image动画播放器，用于在UI Image中播放sprite序列动画
/// </summary>
public class ImageRenderAnim : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private List<Sprite> sprites = new List<Sprite>(); // Sprite列表
    private float switchTime = 0.25f; // 切换sprite的时间间隔
    [SerializeField] private bool playOnStart = false; // 是否在Start时自动播放
    [SerializeField] private bool loop = false; // 是否循环播放
    
    private Image image;
    private Coroutine currentAnimCoroutine;
    private bool isPlaying = false;
    
    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;
    
    private void Awake()
    {
        // 自动获取Image组件
        image = GetComponent<Image>();
        if (image == null)
        {
            image = GetComponentInParent<Image>();
        }
        if (image == null)
        {
            Debug.LogError($"ImageRenderAnim: 无法找到Image组件在 {gameObject.name}");
        }
        
    }
    
    private void Start()
    {
        if (playOnStart && sprites != null && sprites.Count > 0)
        {
            PlayAnim(sprites, loop);
        }
    }
    
    /// <summary>
    /// 设置Sprite列表
    /// </summary>
    public void SetSprites(List<Sprite> spriteList)
    {
        sprites = spriteList != null ? new List<Sprite>(spriteList) : new List<Sprite>();
    }
    
    /// <summary>
    /// 设置Sprite数组
    /// </summary>
    public void SetSprites(Sprite[] spriteArray)
    {
        sprites = spriteArray != null ? new List<Sprite>(spriteArray) : new List<Sprite>();
    }
    
    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="spriteList">sprite列表</param>
    /// <param name="shouldLoop">是否循环</param>
    public void PlayAnim(List<Sprite> spriteList, bool shouldLoop = false)
    {
        if (spriteList == null || spriteList.Count == 0)
        {
            spriteList = sprites;
        }
        
        if (image == null || spriteList == null || spriteList.Count == 0)
        {
            Debug.LogWarning($"ImageRenderAnim: 无法播放动画 - image或sprites为空");
            return;
        }
        
        // 停止当前动画
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }
        
        loop = shouldLoop;
        currentAnimCoroutine = StartCoroutine(PlayAnimationCoroutine(spriteList, shouldLoop));
    }
    
    /// <summary>
    /// 播放动画（使用数组）
    /// </summary>
    /// <param name="spriteArray">sprite数组</param>
    /// <param name="shouldLoop">是否循环</param>
    public void PlayAnim(Sprite[] spriteArray, bool shouldLoop = false)
    {
        if (spriteArray == null || spriteArray.Length == 0)
        {
            PlayAnim(sprites, shouldLoop);
            return;
        }
        
        List<Sprite> spriteList = new List<Sprite>(spriteArray);
        PlayAnim(spriteList, shouldLoop);
    }
    
    /// <summary>
    /// 使用当前设置的sprites播放动画
    /// </summary>
    /// <param name="shouldLoop">是否循环</param>
    public void PlayAnim(bool shouldLoop = false)
    {
        PlayAnim(sprites, shouldLoop);
    }
    
    /// <summary>
    /// 动画协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(List<Sprite> spriteList, bool shouldLoop)
    {
        isPlaying = true;
        
        do
        {
            foreach (Sprite sprite in spriteList)
            {
                if (image != null && sprite != null)
                {
                    image.sprite = sprite;
                }
                yield return new WaitForSeconds(switchTime);
            }
        } while (shouldLoop);
        
        isPlaying = false;
        currentAnimCoroutine = null;
    }
    
    /// <summary>
    /// 停止当前动画
    /// </summary>
    public void Stop()
    {
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
            currentAnimCoroutine = null;
        }
        isPlaying = false;
    }
    
    /// <summary>
    /// 设置切换时间
    /// </summary>
    public void SetSwitchTime(float time)
    {
        switchTime = time;
    }
    
    /// <summary>
    /// 设置循环
    /// </summary>
    public void SetLoop(bool shouldLoop)
    {
        loop = shouldLoop;
        // 如果正在播放，需要重启动画以应用新的循环设置
        if (isPlaying)
        {
            Stop();
            PlayAnim(sprites, shouldLoop);
        }
    }
    
    private void OnDestroy()
    {
        Stop();
    }
}

