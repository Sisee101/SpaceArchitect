using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 胜利图片显示组件
/// 实现 IVictoryFeedbackDisplay 接口，管理胜利图片和印章的显示、隐藏和动画
/// 执行顺序：显示图片（淡入） → 0.5s后显示印章（缩放+淡入） → 2s后同时消失（淡出） → 触发完成回调
/// </summary>
public class VictoryImageDisplay : MonoBehaviour, IVictoryFeedbackDisplay
{
    [Header("UI引用")]
    [Tooltip("显示面板（包含图片和印章的父对象）")]
    [SerializeField] private GameObject displayPanel;
    
    [Tooltip("图片显示组件")]
    [SerializeField] private Image imageDisplay;
    
    [Tooltip("印章图片显示组件（叠在图片上方）")]
    [SerializeField] private Image stampImageDisplay;
    
    [Header("印章配置")]
    [Tooltip("印章图片（所有任务共用，统一配置）")]
    [SerializeField] private Sprite stampSprite;
    
    [Header("时间参数")]
    [Tooltip("印章出现延迟时间（秒，从图片显示后开始计算）")]
    [SerializeField] private float stampAppearDelay = 0.5f;
    
    [Tooltip("显示时长（秒，从印章出现后开始计算）")]
    [SerializeField] private float displayDuration = 2.0f;
    
    [Tooltip("图片淡入动画时长（秒）")]
    [SerializeField] private float fadeInDuration = 0.3f;
    
    [Tooltip("图片和印章淡出动画时长（秒）")]
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Tooltip("印章出现动画时长（秒）")]
    [SerializeField] private float stampAppearDuration = 0.5f;
    
    [Header("动画缓动类型")]
    [Tooltip("图片淡入缓动类型")]
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    
    [Tooltip("图片和印章淡出缓动类型")]
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;
    
    [Tooltip("印章出现缓动类型")]
    [SerializeField] private Ease stampAppearEase = Ease.OutBack;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 接口实现
    public event Action OnHidden;
    public bool IsShowing { get; private set; }
    
    // 当前显示的协程
    private Coroutine currentSequenceCoroutine;
    
    void Start()
    {
        // 验证引用
        if (displayPanel == null)
        {
            Debug.LogError("VictoryImageDisplay: displayPanel未配置！");
        }
        
        if (imageDisplay == null)
        {
            Debug.LogError("VictoryImageDisplay: imageDisplay未配置！");
        }
        
        if (stampImageDisplay == null)
        {
            Debug.LogWarning("VictoryImageDisplay: stampImageDisplay未配置！印章将无法显示。");
        }
        
        // 初始隐藏所有元素
        HideAll();
    }
    
    /// <summary>
    /// 显示胜利图片和印章
    /// </summary>
    /// <param name="image">胜利图片</param>
    /// <param name="onComplete">完成回调</param>
    public void ShowImage(Sprite image, Action onComplete = null)
    {
        if (image == null)
        {
            Debug.LogWarning("VictoryImageDisplay: 图片为空，跳过显示");
            onComplete?.Invoke();
            return;
        }
        
        if (imageDisplay == null || displayPanel == null)
        {
            Debug.LogError("VictoryImageDisplay: UI引用未配置完整！");
            onComplete?.Invoke();
            return;
        }
        
        // 如果正在显示，先停止当前显示
        if (IsShowing)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("VictoryImageDisplay: 正在显示中，停止当前显示");
            }
            StopAllCoroutines();
            HideAll();
        }
        
        // 确保面板和GameObject是激活的
        if (!displayPanel.activeSelf)
        {
            displayPanel.SetActive(true);
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // 开始显示序列
        currentSequenceCoroutine = StartCoroutine(ShowImageSequence(image, onComplete));
    }
    
    /// <summary>
    /// 显示图片序列（协程）
    /// </summary>
    private IEnumerator ShowImageSequence(Sprite image, Action onComplete)
    {
        IsShowing = true;
        
        if (enableDebugLog)
        {
            Debug.Log("VictoryImageDisplay: 开始显示图片序列");
        }
        
        // 步骤1：显示图片（淡入）
        ShowImageWithFadeIn(image);
        yield return new WaitForSeconds(fadeInDuration);
        
        if (enableDebugLog)
        {
            Debug.Log($"VictoryImageDisplay: 图片已显示，等待 {stampAppearDelay} 秒后显示印章");
        }
        
        // 步骤2：等待印章出现延迟
        yield return new WaitForSeconds(stampAppearDelay);
        
        // 步骤3：显示印章（缩放+淡入）
        if (stampSprite != null && stampImageDisplay != null)
        {
            ShowStampWithAnimation(stampSprite);
            yield return new WaitForSeconds(stampAppearDuration);
            
            if (enableDebugLog)
            {
                Debug.Log($"VictoryImageDisplay: 印章已显示，保持 {displayDuration} 秒");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("VictoryImageDisplay: 印章图片未配置，跳过显示印章");
            }
        }
        
        // 步骤4：保持显示
        yield return new WaitForSeconds(displayDuration);
        
        // 步骤5：图片和印章同时淡出
        if (enableDebugLog)
        {
            Debug.Log("VictoryImageDisplay: 开始隐藏图片和印章");
        }
        yield return StartCoroutine(HideAllWithFadeOut());
        
        IsShowing = false;
        currentSequenceCoroutine = null;
        
        if (enableDebugLog)
        {
            Debug.Log("VictoryImageDisplay: 图片序列已结束，准备调用完成回调");
        }
        
        // 步骤6：触发完成事件
        OnHidden?.Invoke();
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 显示图片（带淡入动画）
    /// </summary>
    private void ShowImageWithFadeIn(Sprite image)
    {
        if (imageDisplay == null || displayPanel == null)
        {
            return;
        }
        
        // 设置图片
        imageDisplay.sprite = image;
        
        // 显示面板和图片
        displayPanel.SetActive(true);
        imageDisplay.gameObject.SetActive(true);
        
        // 设置初始状态（透明）
        Color color = imageDisplay.color;
        color.a = 0f;
        imageDisplay.color = color;
        
        // 淡入动画
        if (fadeInDuration > 0f)
        {
            imageDisplay.DOFade(1f, fadeInDuration)
                .SetEase(fadeInEase);
        }
        else
        {
            // 无动画，直接显示
            color.a = 1f;
            imageDisplay.color = color;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"VictoryImageDisplay: 图片已显示: {image.name}");
        }
    }
    
    /// <summary>
    /// 显示印章（缩放+淡入动画）
    /// </summary>
    private void ShowStampWithAnimation(Sprite stamp)
    {
        if (stampImageDisplay == null || stamp == null)
        {
            return;
        }
        
        // 设置印章图片
        stampImageDisplay.sprite = stamp;
        
        // 显示印章
        stampImageDisplay.gameObject.SetActive(true);
        
        // 设置初始状态（缩放为0，透明）
        stampImageDisplay.transform.localScale = Vector3.zero;
        Color color = stampImageDisplay.color;
        color.a = 0f;
        stampImageDisplay.color = color;
        
        // 播放印章出现动画（缩放+淡入）
        if (stampAppearDuration > 0f)
        {
            Sequence sequence = DOTween.Sequence();
            
            // 缩放动画
            Tween scaleTween = stampImageDisplay.transform.DOScale(Vector3.one, stampAppearDuration)
                .SetEase(stampAppearEase);
            
            // 淡入动画
            Tween fadeTween = stampImageDisplay.DOFade(1f, stampAppearDuration)
                .SetEase(Ease.OutQuad);
            
            sequence.Append(scaleTween);
            sequence.Join(fadeTween);
        }
        else
        {
            // 无动画，直接显示
            stampImageDisplay.transform.localScale = Vector3.one;
            color.a = 1f;
            stampImageDisplay.color = color;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("VictoryImageDisplay: 印章已显示");
        }
    }
    
    /// <summary>
    /// 隐藏所有（图片和印章一起淡出）
    /// </summary>
    private IEnumerator HideAllWithFadeOut()
    {
        if (fadeOutDuration > 0f)
        {
            if (enableDebugLog)
            {
                Debug.Log($"VictoryImageDisplay: 开始淡出动画，时长={fadeOutDuration}秒");
            }
            
            // 使用DOTween同时淡出图片和印章
            Sequence sequence = DOTween.Sequence();
            
            if (imageDisplay != null && imageDisplay.gameObject.activeSelf)
            {
                Tween imageFade = imageDisplay.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase);
                sequence.Join(imageFade);
            }
            
            if (stampImageDisplay != null && stampImageDisplay.gameObject.activeSelf)
            {
                Tween stampFade = stampImageDisplay.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase);
                sequence.Join(stampFade);
            }
            
            // 等待动画完成
            yield return sequence.WaitForCompletion();
            
            if (enableDebugLog)
            {
                Debug.Log("VictoryImageDisplay: 淡出动画已完成");
            }
        }
        
        // 隐藏所有元素
        HideAll();
    }
    
    /// <summary>
    /// 隐藏所有（立即隐藏，无动画）
    /// </summary>
    private void HideAll()
    {
        // 先停止所有DOTween动画
        if (imageDisplay != null)
        {
            imageDisplay.DOKill();
        }
        if (stampImageDisplay != null)
        {
            stampImageDisplay.DOKill();
            stampImageDisplay.transform.DOKill();
        }
        
        // 确保透明度为0
        if (imageDisplay != null)
        {
            Color color = imageDisplay.color;
            color.a = 0f;
            imageDisplay.color = color;
            imageDisplay.gameObject.SetActive(false);
        }
        
        if (stampImageDisplay != null)
        {
            Color color = stampImageDisplay.color;
            color.a = 0f;
            stampImageDisplay.color = color;
            stampImageDisplay.transform.localScale = Vector3.one;
            stampImageDisplay.gameObject.SetActive(false);
        }
        
        // 隐藏面板
        if (displayPanel != null)
        {
            displayPanel.SetActive(false);
        }
        
        IsShowing = false;
    }
    
    /// <summary>
    /// 显示胜利视频（接口实现，但不支持视频）
    /// </summary>
    public void ShowVideo(VideoClip videoClip, Action onComplete = null)
    {
        Debug.LogWarning("VictoryImageDisplay: 不支持视频播放，请使用 VictoryVideoPlayer");
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 立即隐藏（接口实现）
    /// </summary>
    public void Hide()
    {
        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }
        HideAll();
    }
    
    void OnDestroy()
    {
        // 清理DOTween动画
        if (imageDisplay != null)
        {
            imageDisplay.DOKill();
        }
        if (stampImageDisplay != null)
        {
            stampImageDisplay.DOKill();
            stampImageDisplay.transform.DOKill();
        }
    }
}
