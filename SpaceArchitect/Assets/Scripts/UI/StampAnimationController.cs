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
    [SerializeField] private float animationDuration = 1.0f; // 动画时长
    [SerializeField] private Ease scaleEase = Ease.OutBack; // 缩放缓动类型
    [SerializeField] private Ease fadeEase = Ease.OutQuad; // 淡入缓动类型
    
    [Header("初始状态")]
    [SerializeField] private Vector3 initialScale = Vector3.zero; // 初始缩放（0表示从0开始）
    [SerializeField] private float initialAlpha = 0f;            // 初始透明度（0表示完全透明）
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool isAnimating = false;
    
    void Start()
    {
        // 确保面板初始隐藏
        if (stampPanel != null)
        {
            stampPanel.SetActive(false);
        }
        
        // 设置初始状态
        if (stampImage != null)
        {
            stampImage.transform.localScale = initialScale;
            Color color = stampImage.color;
            color.a = initialAlpha;
            stampImage.color = color;
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
        if (stampPanel == null || stampImage == null)
        {
            Debug.LogWarning("StampAnimationController: stampPanel或stampImage未配置！");
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
        
        // 显示面板
        stampPanel.SetActive(true);
        
        // 重置状态
        stampImage.transform.localScale = initialScale;
        Color color = stampImage.color;
        color.a = initialAlpha;
        stampImage.color = color;
        
        // 开始动画
        StartCoroutine(PlayAnimationCoroutine(onComplete));
    }
    
    /// <summary>
    /// 播放动画协程
    /// </summary>
    private IEnumerator PlayAnimationCoroutine(Action onComplete)
    {
        isAnimating = true;
        
        // 使用DOTween同时播放缩放和淡入动画
        Sequence sequence = DOTween.Sequence();
        
        // 缩放动画：从初始缩放缩放到1
        sequence.Append(stampImage.transform.DOScale(Vector3.one, animationDuration)
            .SetEase(scaleEase));
        
        // 淡入动画：从初始透明度淡入到1
        sequence.Join(stampImage.DOFade(1f, animationDuration)
            .SetEase(fadeEase));
        
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
