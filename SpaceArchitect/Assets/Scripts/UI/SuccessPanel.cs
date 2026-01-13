using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 成功面板控制器
/// 处理游戏成功后的UI显示和返回主界面功能
/// </summary>
public class SuccessPanel : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("返回主界面按钮")]
    [SerializeField] private Button returnToMainHubButton;
    
    [Header("音效（可选）")]
    [Tooltip("音频源组件（如果为空，会自动获取或添加）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("按钮点击音效")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Tooltip("点击音效播放后的延迟时间（秒），用于确保音效播放完成再执行场景切换")]
    [SerializeField] private float clickSoundDelay = 0.15f;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        // 在 Awake 中初始化，确保在 Start 之前完成
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
    }
    
    void Start()
    {
        // 如果面板在场景中默认是激活的，且没有外部控制，则隐藏
        // 但只在第一次启动时执行，避免覆盖 GameRestartManager 的激活
        // 注意：如果面板在 Inspector 中默认是未激活的，这里不会执行
        if (gameObject.activeSelf)
        {
            // 延迟一帧检查，避免与 GameRestartManager 的激活冲突
            StartCoroutine(CheckAndHideOnStart());
        }
    }
    
    /// <summary>
    /// 检查并隐藏（在 Start 中调用，避免与外部激活冲突）
    /// </summary>
    private IEnumerator CheckAndHideOnStart()
    {
        yield return null; // 等待一帧，让 GameRestartManager 有机会激活面板
        
        // 如果面板仍然激活，说明可能是场景中默认激活的，需要隐藏
        // 但如果 GameRestartManager 已经激活了面板，这里不应该隐藏
        // 由于无法直接判断，我们假设如果面板在场景中默认应该是隐藏的
        // 用户应该在 Inspector 中将面板设置为未激活状态
        // 这里不做自动隐藏，避免覆盖 GameRestartManager 的激活
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    private void InitializePanel()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 绑定返回按钮
        if (returnToMainHubButton != null)
        {
            returnToMainHubButton.onClick.RemoveAllListeners();
            returnToMainHubButton.onClick.AddListener(OnReturnButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log("SuccessPanel: 返回主界面按钮已绑定");
            }
        }
        else
        {
            Debug.LogWarning("SuccessPanel: 返回主界面按钮未配置！请在Inspector中配置 Return To Main Hub Button 字段。");
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
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// 显示成功面板
    /// </summary>
    public void Show()
    {
        // 确保初始化完成
        if (!isInitialized)
        {
            InitializePanel();
            isInitialized = true;
        }
        
        // 检查 GameObject 是否有效
        if (gameObject == null)
        {
            Debug.LogError("SuccessPanel: GameObject 为空！无法显示面板。");
            return;
        }
        
        // 先确保父对象和 Canvas 激活
        EnsureParentAndCanvasActive();
        
        // 激活 GameObject
        gameObject.SetActive(true);
        
        // 再次检查并修复可能的问题
        EnsurePanelVisible();
        
        if (enableDebugLog)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Debug.Log($"SuccessPanel: 成功面板已显示 - GameObject: {gameObject.name}, Active: {gameObject.activeSelf}, " +
                $"Parent Active: {(transform.parent != null ? transform.parent.gameObject.activeSelf.ToString() : "null")}, " +
                $"Canvas Active: {(canvas != null ? canvas.gameObject.activeSelf.ToString() : "null")}");
        }
    }
    
    /// <summary>
    /// 确保父对象和 Canvas 激活
    /// </summary>
    private void EnsureParentAndCanvasActive()
    {
        // 检查并激活所有父对象
        Transform current = transform;
        while (current.parent != null)
        {
            current = current.parent;
            if (!current.gameObject.activeSelf)
            {
                Debug.LogWarning($"SuccessPanel: 父对象 {current.name} 未激活，正在激活...");
                current.gameObject.SetActive(true);
            }
        }
        
        // 检查 Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (!canvas.gameObject.activeSelf)
            {
                Debug.LogWarning($"SuccessPanel: Canvas {canvas.name} 未激活，正在激活...");
                canvas.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("SuccessPanel: 未找到 Canvas 组件！");
        }
    }
    
    /// <summary>
    /// 确保面板可见
    /// </summary>
    private void EnsurePanelVisible()
    {
        // 检查 Canvas Group（如果有）
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // 确保 RectTransform 位置正确
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 检查是否在屏幕外（位置异常大）
            if (Mathf.Abs(rectTransform.anchoredPosition.x) > 10000 || 
                Mathf.Abs(rectTransform.anchoredPosition.y) > 10000)
            {
                Debug.LogWarning($"SuccessPanel: RectTransform 位置异常 ({rectTransform.anchoredPosition})，面板可能在屏幕外！");
            }
        }
    }
    
    /// <summary>
    /// 隐藏成功面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (enableDebugLog)
        {
            Debug.Log("SuccessPanel: 成功面板已隐藏");
        }
    }
    
    /// <summary>
    /// 返回主界面按钮点击事件
    /// </summary>
    private void OnReturnButtonClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log("SuccessPanel: 返回主界面按钮被点击");
        }
        
        // 播放点击音效并延迟执行场景切换，确保音效能够播放
        StartCoroutine(PlayClickSoundAndReturnToMainHub());
    }
    
    /// <summary>
    /// 播放点击音效并延迟返回主界面（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndReturnToMainHub()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        // 恢复游戏时间（因为游戏成功时会暂停）
        Time.timeScale = 1f;
        
        // 等待音效播放完成
        if (clickSoundDelay > 0)
        {
            yield return new WaitForSecondsRealtime(clickSoundDelay); // 使用 WaitForSecondsRealtime 因为时间可能被暂停
        }
        
        // 返回主界面
        if (enableDebugLog)
        {
            Debug.Log("SuccessPanel: 返回主界面场景");
        }
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainHubScene();
        }
        else
        {
            Debug.LogError("SuccessPanel: SceneTransitionManager未找到！无法返回主界面。");
        }
    }
}
