using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sprite动画播放器，用于播放sprite序列动画
/// </summary>
public class SpriteRenderAnim : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private string identifier = ""; // 敌人标识符
    [SerializeField] private float switchTime = 0.1f; // 切换sprite的时间间隔

    [Header("Renderer引用")]
    [SerializeField] private SpriteRenderer mainRender; // 主渲染器
    [SerializeField] private SpriteRenderer flashRender; // 闪烁渲染器
    [SerializeField] private SpriteRenderer backColorRender; // 背景颜色渲染器

    private SpriteRenderer spriteRenderer; // 保留向后兼容
    private Coroutine currentAnimCoroutine;
    private bool isPlaying = false;
    

    private void Awake()
    {
        // 如果没有手动设置mainRender，尝试自动获取（向后兼容）
        if (mainRender == null)
        {
            mainRender = GetComponent<SpriteRenderer>();
            if (mainRender == null)
            {
                mainRender = GetComponentInParent<SpriteRenderer>();
            }
        }
        
        // 保留spriteRenderer引用用于向后兼容
        spriteRenderer = mainRender;
        
        if (mainRender == null)
        {
            Debug.LogError($"SpriteRenderAnim: 无法找到mainRender组件在 {gameObject.name}");
        }
    }

    /// <summary>
    /// 设置identifier
    /// </summary>
    public void SetIdentifier(string id)
    {
        identifier = id;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="sprites">sprite数组</param>
    /// <param name="loop">是否循环</param>
    public void PlayAnim(Sprite[] sprites, bool loop)
    {
        if (mainRender == null || sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"SpriteRenderAnim: 无法播放动画 - mainRender或sprites为空");
            return;
        }

        // 停止当前动画
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }

        currentAnimCoroutine = StartCoroutine(PlayAnimationCoroutine(sprites, loop));
    }

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    public void PlayAtk()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            Debug.LogError($"SpriteRenderAnim: identifier为空，无法播放攻击动画");
            return;
        }

        string folderPath = $"enemy/{identifier}";
        // 播放攻击动画后切换到idle
        PlayAnimThenFollow("atk", "idle");
    }

    /// <summary>
    /// 播放待机动画
    /// </summary>
    public void PlayIdle()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            Debug.LogError($"SpriteRenderAnim: identifier为空，无法播放待机动画");
            return;
        }

        string folderPath = $"enemy/{identifier}";
        Sprite[] sprites = LoadSpritesBySuffix(folderPath, "idle");
        
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"SpriteRenderAnim: 无法找到待机动画 - {folderPath}/*idle");
            return;
        }

        PlayAnim(sprites, true);
    }

    /// <summary>
    /// 播放受伤动画
    /// </summary>
    public void PlayHurt()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        string folderPath = $"enemy/{identifier}";
        // 播放受伤动画后切换到idle
        PlayAnimThenFollow("hurt", "idle");
    }

    /// <summary>
    /// 播放移动动画
    /// </summary>
    public void PlayMove()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        string folderPath = $"enemy/{identifier}";
        Sprite[] sprites = LoadSpritesBySuffix(folderPath, "move");
        
        if (sprites != null && sprites.Length > 0)
        {
            PlayAnim(sprites, true);
        }
    }

    /// <summary>
    /// 播放特殊技能动画
    /// </summary>
    public void PlaySpecial()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        string folderPath = $"enemy/{identifier}";
        Sprite[] sprites = LoadSpritesBySuffix(folderPath, "special");
        
        if (sprites != null && sprites.Length > 0)
        {
            
            PlayAnimThenFollow("special", "idle");
            //PlayAnim(sprites, false);
        }
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDead()
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        string folderPath = $"enemy/{identifier}";
        Sprite[] sprites = LoadSpritesBySuffix(folderPath, "dead");
        
        if (sprites != null && sprites.Length > 0)
        {
            PlayAnim(sprites, false);
        }
    }

    /// <summary>
    /// 播放一个动画后循环另一个动画
    /// </summary>
    /// <param name="anim">第一个动画名称（如 "atk", "hurt"）</param>
    /// <param name="loopAnim">循环播放的动画名称（如 "idle", "move"）</param>
    public void PlayAnimThenFollow(string anim, string loopAnim)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            Debug.LogWarning($"SpriteRenderAnim: identifier为空，无法播放动画序列");
            return;
        }

        string folderPath = $"enemy/{identifier}";
        Sprite[] firstSprites = LoadSpritesBySuffix(folderPath, anim);
        Sprite[] loopSprites = LoadSpritesBySuffix(folderPath, loopAnim);

        if (firstSprites == null || firstSprites.Length == 0)
        {
            Debug.LogWarning($"SpriteRenderAnim: 无法找到动画 - {folderPath}/*{anim}");
            // 如果第一个动画不存在，直接播放循环动画
            if (loopSprites != null && loopSprites.Length > 0)
            {
                PlayAnim(loopSprites, true);
            }
            return;
        }

        if (loopSprites == null || loopSprites.Length == 0)
        {
            Debug.LogWarning($"SpriteRenderAnim: 无法找到循环动画 - {folderPath}/*{loopAnim}");
            // 如果循环动画不存在，只播放第一个动画
            PlayAnim(firstSprites, false);
            return;
        }

        // 停止当前动画
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }

        currentAnimCoroutine = StartCoroutine(PlayAnimThenFollowCoroutine(firstSprites, loopSprites));
    }

    /// <summary>
    /// 根据后缀加载sprites
    /// </summary>
    private Sprite[] LoadSpritesBySuffix(string folderPath, string suffix)
    {
        // 加载文件夹中的所有sprite（包括sprite sheet中的sprite）
        Sprite[] allSprites = Resources.LoadAll<Sprite>(folderPath);
        List<Sprite> sprites = new List<Sprite>();

        string lowerSuffix = suffix.ToLower();
        
        foreach (Sprite sprite in allSprites)
        {
            string spriteName = sprite.name.ToLower();
            
            // 检查sprite名称中是否包含suffix（例如：normal_atk_0_0 包含 "atk"）
            // 使用下划线分隔，确保匹配的是完整的动画类型（如_atk_而不是atk作为其他单词的一部分）
            if (spriteName.Contains($"_{lowerSuffix}_") || spriteName.EndsWith($"_{lowerSuffix}"))
            {
                sprites.Add(sprite);
            }
        }

        // 按名称排序（确保动画顺序正确）
        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        return sprites.Count > 0 ? sprites.ToArray() : null;
    }

    /// <summary>
    /// 动画协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(Sprite[] sprites, bool loop)
    {
        isPlaying = true;

        do
        {
            foreach (Sprite sprite in sprites)
            {
                // 同时设置三个render的sprite
                if (mainRender != null)
                {
                    mainRender.sprite = sprite;
                }
                if (flashRender != null)
                {
                    flashRender.sprite = sprite;
                }
                if (backColorRender != null)
                {
                    backColorRender.sprite = sprite;
                }
                yield return new WaitForSeconds(switchTime);
            }
        } while (loop);

        isPlaying = false;
        currentAnimCoroutine = null;
    }

    /// <summary>
    /// 播放一个动画后循环另一个动画的协程
    /// </summary>
    private IEnumerator PlayAnimThenFollowCoroutine(Sprite[] firstSprites, Sprite[] loopSprites)
    {
        isPlaying = true;

        // 播放第一个动画（不循环）
        foreach (Sprite sprite in firstSprites)
        {
            // 同时设置三个render的sprite
            if (mainRender != null)
            {
                mainRender.sprite = sprite;
            }
            if (flashRender != null)
            {
                flashRender.sprite = sprite;
            }
            if (backColorRender != null)
            {
                backColorRender.sprite = sprite;
            }
            yield return new WaitForSeconds(switchTime);
        }

        // 然后循环播放第二个动画
        while (true)
        {
            foreach (Sprite sprite in loopSprites)
            {
                // 同时设置三个render的sprite
                if (mainRender != null)
                {
                    mainRender.sprite = sprite;
                }
                if (flashRender != null)
                {
                    flashRender.sprite = sprite;
                }
                if (backColorRender != null)
                {
                    backColorRender.sprite = sprite;
                }
                yield return new WaitForSeconds(switchTime);
            }
        }
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
    /// 检查是否有对应的动画文件夹
    /// </summary>
    public static bool HasAnimationFolder(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        // 首字母大写
        string folderName = char.ToUpper(identifier[0]) + identifier.Substring(1);
        string folderPath = $"enemy/{folderName}";
        
        Object[] objects = Resources.LoadAll(folderPath);
        return objects != null && objects.Length > 2;
    }
}

