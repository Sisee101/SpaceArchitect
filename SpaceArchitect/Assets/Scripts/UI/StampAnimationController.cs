using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 印章动画控制器
/// 管理印章逐渐出现的动画效果
/// </summary>
public class StampAnimationController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject stampPanel;      // 印章面板（初始隐藏）
    [SerializeField] private Image stampImage;           // 印章图片
    
    [Header("动画参数")]
    [Tooltip("动画时长（秒）。设置为0表示突然出现，无动画")]
    [SerializeField] private float animationDuration = 0f; // 动画时长（0表示突然出现）
    
    [Tooltip("是否使用突然出现效果（true=突然出现，false=逐渐出现）")]
    [SerializeField] private bool useInstantAppear = true; // 是否突然出现
    
    [Header("逐渐出现动画参数（useInstantAppear=false时使用）")]
    [SerializeField] private Ease scaleEase = Ease.OutBack; // 缩放缓动类型
    [SerializeField] private Ease fadeEase = Ease.OutQuad; // 淡入缓动类型
    [SerializeField] private Vector3 initialScale = Vector3.zero; // 初始缩放（0表示从0开始）
    [SerializeField] private float initialAlpha = 0f;            // 初始透明度（0表示完全透明）
    
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;              // 音频源组件
    [SerializeField] private AudioClip stampAppearSound;           // 印章出现音效
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool isAnimating = false;
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 确保面板初始隐藏
        if (stampPanel != null)
        {
            stampPanel.SetActive(false);
        }
        
        // 设置初始状态（根据是否使用突然出现效果）
        if (stampImage != null)
        {
            if (useInstantAppear)
            {
                // 突然出现：初始状态就是最终状态（缩放1，透明度1）
                stampImage.transform.localScale = Vector3.one;
                Color color = stampImage.color;
                color.a = 1f;
                stampImage.color = color;
            }
            else
            {
                // 逐渐出现：初始状态是隐藏的
                stampImage.transform.localScale = initialScale;
                Color color = stampImage.color;
                color.a = initialAlpha;
                stampImage.color = color;
            }
        }
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        // 如果未手动指定 AudioSource，尝试自动获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // 如果还是没有，自动添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// 播放印章出现音效
    /// </summary>
    private void PlayStampSound()
    {
        if (audioSource != null && stampAppearSound != null)
        {
            audioSource.PlayOneShot(stampAppearSound);
            
            if (enableDebugLog)
            {
                Debug.Log("StampAnimationController: 播放印章出现音效");
            }
        }
        else if (enableDebugLog)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("StampAnimationController: AudioSource未配置，无法播放音效");
            }
            if (stampAppearSound == null)
            {
                Debug.LogWarning("StampAnimationController: 印章出现音效未配置");
            }
        }
    }
    
    /// <summary>
    /// 播放印章出现动画（供后续代码调用）
    /// </summary>
    public void PlayStampAnimation()
    {
        PlayStampAnimation(null);
    }
    
    /// <summary>
    /// 播放印章出现动画（带完成回调）
    /// </summary>
    /// <param name="onComplete">动画完成回调</param>
    public void PlayStampAnimation(Action onComplete)
    {
        if (enableDebugLog)
        {
            Debug.Log("StampAnimationController: 开始播放印章动画");
        }
        
        if (stampPanel == null)
        {
            Debug.LogError("StampAnimationController: stampPanel未配置！请在Inspector中配置Stamp Panel引用。");
            onComplete?.Invoke();
            return;
        }
        
        if (stampImage == null)
        {
            Debug.LogError("StampAnimationController: stampImage未配置！请在Inspector中配置Stamp Image引用。");
            onComplete?.Invoke();
            return;
        }
        
        if (isAnimating)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("StampAnimationController: 动画正在播放中，跳过");
            }
            return;
        }
        
        // 确保结算界面（父对象）是激活的，否则印章面板无法显示
        if (!stampPanel.transform.root.gameObject.activeSelf)
        {
            Debug.LogWarning("StampAnimationController: 结算界面未激活，无法显示印章动画！");
            onComplete?.Invoke();
            return;
        }
        
        // 显示面板
        stampPanel.SetActive(true);
        
        // 播放音效
        PlayStampSound();
        
        if (enableDebugLog)
        {
            Debug.Log($"StampAnimationController: 印章面板已激活，使用突然出现: {useInstantAppear}");
        }
        
        // 根据动画类型执行不同的逻辑
        if (useInstantAppear || animationDuration <= 0f)
        {
            // 突然出现：直接设置为最终状态
            if (stampImage != null)
            {
                stampImage.transform.localScale = Vector3.one;
                Color color = stampImage.color;
                color.a = 1f;
                stampImage.color = color;
            }
            
            isAnimating = false;
            
            if (enableDebugLog)
            {
                Debug.Log("StampAnimationController: 印章突然出现完成");
            }
            
            // 调用完成回调
            onComplete?.Invoke();
        }
        else
        {
            // 逐渐出现：重置状态并播放动画
            stampImage.transform.localScale = initialScale;
            Color color = stampImage.color;
            color.a = initialAlpha;
            stampImage.color = color;
            
            // 开始动画
            StartCoroutine(PlayAnimationCoroutine(onComplete));
        }
    }
    
    /// <summary>
    /// 播放动画协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(Action onComplete)
    {
        isAnimating = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"StampAnimationController: 开始动画协程，动画时长: {animationDuration}秒");
        }
        
        // 确保stampImage仍然有效
        if (stampImage == null)
        {
            Debug.LogError("StampAnimationController: stampImage在动画过程中变为null！");
            isAnimating = false;
            onComplete?.Invoke();
            yield break;
        }
        
        // 使用DOTween同时播放缩放和淡入动画
        Sequence sequence = DOTween.Sequence();
        
        // 缩放动画：从初始缩放缩放到1
        Tween scaleTween = stampImage.transform.DOScale(Vector3.one, animationDuration)
            .SetEase(scaleEase);
        
        // 淡入动画：从初始透明度淡入到1
        Tween fadeTween = stampImage.DOFade(1f, animationDuration)
            .SetEase(fadeEase);
        
        sequence.Append(scaleTween);
        sequence.Join(fadeTween);
        
        if (enableDebugLog)
        {
            Debug.Log($"StampAnimationController: DOTween序列已创建，开始播放动画");
        }
        
        // 等待动画完成
        yield return sequence.WaitForCompletion();
        
        isAnimating = false;
        
        if (enableDebugLog)
        {
            Debug.Log("StampAnimationController: 印章动画播放完成");
        }
        
        // 调用完成回调
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 隐藏印章（供后续代码调用）
    /// </summary>
    public void HideStamp()
    {
        if (stampPanel != null)
        {
            stampPanel.SetActive(false);
        }
        
        // 重置状态
        if (stampImage != null)
        {
            stampImage.transform.localScale = initialScale;
            Color color = stampImage.color;
            color.a = initialAlpha;
            stampImage.color = color;
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 检查是否正在播放动画
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
}
