using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 订单完成显示组件
/// 管理任务完成时订单图片和"已送达"印章的显示、隐藏和动画
/// 执行顺序：显示订单图片 → 延迟1秒 → 显示印章 → 保持1秒 → 一起消失
/// </summary>
public class OrderCompletionDisplay : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("显示面板（包含订单图片和印章的父对象）")]
    [SerializeField] private GameObject displayPanel;
    
    [Tooltip("订单图片显示组件")]
    [SerializeField] private Image orderImageDisplay;
    
    [Tooltip("印章图片显示组件（叠在订单图片上方）")]
    [SerializeField] private Image stampImageDisplay;
    
    [Header("印章图片")]
    [Tooltip("已送达印章图片（可配置）")]
    [SerializeField] private Sprite stampSprite;
    
    [Header("动画参数")]
    [Tooltip("订单图片出现动画时长（秒）")]
    [SerializeField] private float orderImageAppearDuration = 0.3f;
    
    [Tooltip("印章出现延迟时间（秒，从订单图片显示后开始计算）")]
    [SerializeField] private float stampAppearDelay = 1.0f;
    
    [Tooltip("印章出现动画时长（秒）")]
    [SerializeField] private float stampAppearDuration = 0.5f;
    
    [Tooltip("保持显示时长（秒，印章出现后保持的时间）")]
    [SerializeField] private float displayDuration = 1.0f;
    
    [Tooltip("消失动画时长（秒，订单和印章一起消失的动画时长）")]
    [SerializeField] private float hideDuration = 0.3f;
    
    [Header("动画缓动类型")]
    [Tooltip("订单图片淡入缓动类型")]
    [SerializeField] private Ease orderImageFadeEase = Ease.OutQuad;
    
    [Tooltip("印章出现缓动类型")]
    [SerializeField] private Ease stampAppearEase = Ease.OutBack;
    
    [Tooltip("消失动画缓动类型")]
    [SerializeField] private Ease hideEase = Ease.InQuad;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前是否正在显示
    private bool isDisplaying = false;
    
    void Start()
    {
        // 验证引用
        if (displayPanel == null)
        {
            Debug.LogError("OrderCompletionDisplay: displayPanel未配置！");
        }
        
        if (orderImageDisplay == null)
        {
            Debug.LogError("OrderCompletionDisplay: orderImageDisplay未配置！");
        }
        
        if (stampImageDisplay == null)
        {
            Debug.LogError("OrderCompletionDisplay: stampImageDisplay未配置！");
        }
        
        // 初始隐藏所有元素
        HideAll();
    }
    
    /// <summary>
    /// 显示订单完成序列
    /// </summary>
    /// <param name="orderImage">订单图片（Sprite）</param>
    /// <param name="onComplete">完成回调（订单和印章都消失后调用）</param>
    public void ShowOrderCompletion(Sprite orderImage, Action onComplete = null)
    {
        if (isDisplaying)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderCompletionDisplay: 正在显示中，停止当前显示");
            }
            StopAllCoroutines();
            HideAll();
        }
        
        if (orderImage == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderCompletionDisplay: 订单图片为空，跳过显示");
            }
            onComplete?.Invoke();
            return;
        }
        
        if (orderImageDisplay == null || stampImageDisplay == null || displayPanel == null)
        {
            Debug.LogError("OrderCompletionDisplay: UI引用未配置完整！");
            onComplete?.Invoke();
            return;
        }
        
        // 确保面板是激活的（协程需要在激活的GameObject上运行）
        if (!displayPanel.activeSelf)
        {
            displayPanel.SetActive(true);
            if (enableDebugLog)
            {
                Debug.Log("OrderCompletionDisplay: 面板未激活，已自动激活");
            }
        }
        
        // 确保当前GameObject是激活的（协程需要在激活的GameObject上运行）
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (enableDebugLog)
            {
                Debug.Log("OrderCompletionDisplay: GameObject未激活，已自动激活");
            }
        }
        
        // 设置印章图片
        if (stampImageDisplay != null && stampSprite != null)
        {
            stampImageDisplay.sprite = stampSprite;
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning("OrderCompletionDisplay: 印章图片未配置，印章将不显示");
        }
        
        // 开始显示序列
        StartCoroutine(ShowOrderCompletionSequence(orderImage, onComplete));
    }
    
    /// <summary>
    /// 显示订单完成序列（协程）
    /// </summary>
    private IEnumerator ShowOrderCompletionSequence(Sprite orderImage, Action onComplete)
    {
        isDisplaying = true;
        
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: 开始显示订单完成序列");
        }
        
        // 步骤1：显示订单图片（淡入动画）
        ShowOrderImage(orderImage);
        yield return new WaitForSeconds(orderImageAppearDuration);
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderCompletionDisplay: 订单图片已显示，等待 {stampAppearDelay} 秒后显示印章");
        }
        
        // 步骤2：等待印章出现延迟
        yield return new WaitForSeconds(stampAppearDelay);
        
        // 步骤3：显示印章（叠在订单图片上方）
        ShowStamp();
        yield return new WaitForSeconds(stampAppearDuration);
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderCompletionDisplay: 印章已显示，保持 {displayDuration} 秒");
        }
        
        // 步骤4：保持显示
        yield return new WaitForSeconds(displayDuration);
        
        // 步骤5：隐藏所有（订单和印章一起消失）
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: 开始隐藏订单和印章");
        }
        yield return StartCoroutine(HideAllWithAnimationCoroutine());
        
        isDisplaying = false;
        
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: 订单完成序列已结束，准备调用完成回调");
            Debug.Log($"OrderCompletionDisplay: onComplete是否为null={onComplete == null}");
        }
        
        // 调用完成回调（在协程中直接调用，确保在主线程执行）
        if (onComplete != null)
        {
            if (enableDebugLog)
            {
                Debug.Log("OrderCompletionDisplay: 正在调用完成回调");
            }
            try
            {
                onComplete.Invoke();
                if (enableDebugLog)
                {
                    Debug.Log("OrderCompletionDisplay: 完成回调已成功调用");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OrderCompletionDisplay: 调用完成回调时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning("OrderCompletionDisplay: 完成回调为空，跳过调用");
        }
    }
    
    /// <summary>
    /// 显示订单图片（带淡入动画）
    /// </summary>
    private void ShowOrderImage(Sprite orderImage)
    {
        if (orderImageDisplay == null || displayPanel == null)
        {
            return;
        }
        
        // 设置订单图片
        orderImageDisplay.sprite = orderImage;
        
        // 显示面板
        displayPanel.SetActive(true);
        orderImageDisplay.gameObject.SetActive(true);
        
        // 设置初始状态（透明）
        Color color = orderImageDisplay.color;
        color.a = 0f;
        orderImageDisplay.color = color;
        
        // 淡入动画
        if (orderImageAppearDuration > 0f)
        {
            orderImageDisplay.DOFade(1f, orderImageAppearDuration)
                .SetEase(orderImageFadeEase);
        }
        else
        {
            // 无动画，直接显示
            color.a = 1f;
            orderImageDisplay.color = color;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderCompletionDisplay: 订单图片已显示: {orderImage.name}");
        }
    }
    
    /// <summary>
    /// 显示印章（叠在订单图片上方，带动画）
    /// </summary>
    private void ShowStamp()
    {
        if (stampImageDisplay == null || stampSprite == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("OrderCompletionDisplay: 印章图片未配置，跳过显示印章");
            }
            return;
        }
        
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
            Debug.Log("OrderCompletionDisplay: 印章已显示");
        }
    }
    
    /// <summary>
    /// 隐藏所有（订单和印章一起消失，带淡出动画）- 协程版本
    /// </summary>
    private IEnumerator HideAllWithAnimationCoroutine()
    {
        if (hideDuration > 0f)
        {
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompletionDisplay: 开始淡出动画，时长={hideDuration}秒");
                Debug.Log($"OrderCompletionDisplay: orderImageDisplay.activeSelf={orderImageDisplay?.gameObject.activeSelf}, stampImageDisplay.activeSelf={stampImageDisplay?.gameObject.activeSelf}");
            }
            
            // 使用DOTween同时淡出订单图片和印章
            Sequence sequence = DOTween.Sequence();
            
            if (orderImageDisplay != null && orderImageDisplay.gameObject.activeSelf)
            {
                Tween orderFade = orderImageDisplay.DOFade(0f, hideDuration)
                    .SetEase(hideEase);
                sequence.Join(orderFade);
                
                if (enableDebugLog)
                {
                    Debug.Log($"OrderCompletionDisplay: 订单图片当前Alpha={orderImageDisplay.color.a}");
                }
            }
            
            if (stampImageDisplay != null && stampImageDisplay.gameObject.activeSelf)
            {
                Tween stampFade = stampImageDisplay.DOFade(0f, hideDuration)
                    .SetEase(hideEase);
                sequence.Join(stampFade);
                
                if (enableDebugLog)
                {
                    Debug.Log($"OrderCompletionDisplay: 印章图片当前Alpha={stampImageDisplay.color.a}");
                }
            }
            
            // 等待动画完成
            yield return sequence.WaitForCompletion();
            
            if (enableDebugLog)
            {
                Debug.Log("OrderCompletionDisplay: 淡出动画已完成");
                if (orderImageDisplay != null)
                {
                    Debug.Log($"OrderCompletionDisplay: 动画后订单图片Alpha={orderImageDisplay.color.a}, activeSelf={orderImageDisplay.gameObject.activeSelf}");
                }
                if (stampImageDisplay != null)
                {
                    Debug.Log($"OrderCompletionDisplay: 动画后印章图片Alpha={stampImageDisplay.color.a}, activeSelf={stampImageDisplay.gameObject.activeSelf}");
                }
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("OrderCompletionDisplay: hideDuration为0，跳过动画，直接隐藏");
            }
        }
        
        // 隐藏所有元素
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: 调用HideAll()隐藏所有元素");
        }
        HideAll();
        
        if (enableDebugLog)
        {
            Debug.Log($"OrderCompletionDisplay: HideAll()执行完成，displayPanel.activeSelf={displayPanel?.activeSelf}");
        }
    }
    
    /// <summary>
    /// 隐藏所有（订单和印章一起消失，带淡出动画）- 非协程版本（保留用于其他调用）
    /// </summary>
    private void HideAllWithAnimation()
    {
        if (hideDuration > 0f)
        {
            // 使用DOTween同时淡出订单图片和印章
            Sequence sequence = DOTween.Sequence();
            
            if (orderImageDisplay != null && orderImageDisplay.gameObject.activeSelf)
            {
                Tween orderFade = orderImageDisplay.DOFade(0f, hideDuration)
                    .SetEase(hideEase);
                sequence.Join(orderFade);
            }
            
            if (stampImageDisplay != null && stampImageDisplay.gameObject.activeSelf)
            {
                Tween stampFade = stampImageDisplay.DOFade(0f, hideDuration)
                    .SetEase(hideEase);
                sequence.Join(stampFade);
            }
            
            sequence.OnComplete(() => {
                HideAll();
            });
        }
        else
        {
            // 无动画，直接隐藏
            HideAll();
        }
    }
    
    /// <summary>
    /// 隐藏所有（立即隐藏，无动画）
    /// </summary>
    public void HideAll()
    {
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: HideAll() 开始执行");
        }
        
        // 先停止所有DOTween动画，避免动画干扰
        if (orderImageDisplay != null)
        {
            orderImageDisplay.DOKill();
        }
        if (stampImageDisplay != null)
        {
            stampImageDisplay.DOKill();
        }
        
        // 确保透明度为0（即使动画已经完成，也要确保）
        if (orderImageDisplay != null)
        {
            Color color = orderImageDisplay.color;
            color.a = 0f; // 先设置为0，确保不可见
            orderImageDisplay.color = color;
            orderImageDisplay.gameObject.SetActive(false);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompletionDisplay: 订单图片已隐藏，Alpha={orderImageDisplay.color.a}, activeSelf={orderImageDisplay.gameObject.activeSelf}");
            }
        }
        
        if (stampImageDisplay != null)
        {
            Color color = stampImageDisplay.color;
            color.a = 0f; // 先设置为0，确保不可见
            stampImageDisplay.color = color;
            stampImageDisplay.transform.localScale = Vector3.one;
            stampImageDisplay.gameObject.SetActive(false);
            
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompletionDisplay: 印章图片已隐藏，Alpha={stampImageDisplay.color.a}, activeSelf={stampImageDisplay.gameObject.activeSelf}");
            }
        }
        
        // 最后隐藏整个面板
        if (displayPanel != null)
        {
            displayPanel.SetActive(false);
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompletionDisplay: 面板已隐藏，activeSelf={displayPanel.activeSelf}");
            }
        }
        
        // 也隐藏当前GameObject（如果它是面板本身）
        if (gameObject != displayPanel)
        {
            gameObject.SetActive(false);
            if (enableDebugLog)
            {
                Debug.Log($"OrderCompletionDisplay: GameObject已隐藏，activeSelf={gameObject.activeSelf}");
            }
        }
        
        isDisplaying = false;
        
        if (enableDebugLog)
        {
            Debug.Log("OrderCompletionDisplay: HideAll() 执行完成");
        }
    }
    
    /// <summary>
    /// 检查是否正在显示
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }
    
    void OnDestroy()
    {
        // 清理DOTween动画
        if (orderImageDisplay != null)
        {
            orderImageDisplay.DOKill();
        }
        
        if (stampImageDisplay != null)
        {
            stampImageDisplay.DOKill();
            stampImageDisplay.transform.DOKill();
        }
    }
}
