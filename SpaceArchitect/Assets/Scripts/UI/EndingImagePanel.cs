using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 结局图片面板控制器
/// 支持点击按钮弹出图片面板、顺序切换图片、在最后一张图片时显示退出游戏按钮
/// </summary>
public class EndingImagePanel : MonoBehaviour
{
    [Header("图片配置")]
    [Tooltip("结局图片列表（按顺序显示）")]
    [SerializeField] private List<Sprite> endingImages = new List<Sprite>();
    
    [Header("UI引用")]
    [Tooltip("显示当前图片的Image组件")]
    [SerializeField] private Image currentImageDisplay;
    
    [Tooltip("切换下一张图片的按钮（独立的按钮）")]
    [SerializeField] private Button nextImageButton;
    
    [Tooltip("退出游戏的按钮（独立的按钮，最后一张图片时显示）")]
    [SerializeField] private Button exitGameButton;
    
    [Header("音效（可选）")]
    [Tooltip("音频源组件")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("按钮点击音效")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Tooltip("图片切换音效")]
    [SerializeField] private AudioClip imageSwitchSound;
    
    [Tooltip("点击音效播放后的延迟时间（秒）")]
    [SerializeField] private float clickSoundDelay = 0.15f;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 内部状态
    private int currentImageIndex = 0;
    
    void Start()
    {
        // 初始化音频源
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 绑定按钮事件
        if (nextImageButton != null)
        {
            nextImageButton.onClick.AddListener(OnNextImageClicked);
        }
        else
        {
            Debug.LogWarning("EndingImagePanel: nextImageButton 未分配！请在Inspector中分配按钮引用。");
        }
        
        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }
        else
        {
            Debug.LogWarning("EndingImagePanel: exitGameButton 未分配！请在Inspector中分配按钮引用。");
        }
        
        // 验证图片列表
        if (endingImages == null || endingImages.Count == 0)
        {
            Debug.LogWarning("EndingImagePanel: endingImages 列表为空！请在Inspector中添加结局图片。");
        }
        
        // 验证Image组件
        if (currentImageDisplay == null)
        {
            Debug.LogError("EndingImagePanel: currentImageDisplay 未分配！请在Inspector中分配Image组件。");
        }
        
        // 默认隐藏面板
        Hide();
    }
    
    /// <summary>
    /// 显示图片面板
    /// </summary>
    public void Show()
    {
        // 重置到第一张图片
        currentImageIndex = 0;
        
        // 显示面板
        gameObject.SetActive(true);
        
        // 更新图片显示
        UpdateImageDisplay();
        
        // 更新按钮显示状态
        UpdateButtonVisibility();
        
        if (enableDebugLog)
        {
            Debug.Log($"EndingImagePanel: 显示面板，共 {endingImages.Count} 张图片，当前显示第 {currentImageIndex + 1} 张");
        }
    }
    
    /// <summary>
    /// 隐藏图片面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (enableDebugLog)
        {
            Debug.Log("EndingImagePanel: 隐藏面板");
        }
    }
    
    /// <summary>
    /// 切换到下一张图片按钮点击事件
    /// </summary>
    private void OnNextImageClicked()
    {
        // 播放点击音效
        PlayClickSound();
        
        // 延迟执行切换（确保音效播放）
        StartCoroutine(PlaySoundAndSwitchImage());
    }
    
    /// <summary>
    /// 播放音效并切换图片（协程）
    /// </summary>
    private IEnumerator PlaySoundAndSwitchImage()
    {
        // 等待音效播放时间
        if (clickSoundDelay > 0f)
        {
            yield return new WaitForSeconds(clickSoundDelay);
        }
        
        // 切换到下一张图片
        if (currentImageIndex < endingImages.Count - 1)
        {
            currentImageIndex++;
            UpdateImageDisplay();
            UpdateButtonVisibility();
            
            if (enableDebugLog)
            {
                Debug.Log($"EndingImagePanel: 切换到第 {currentImageIndex + 1} 张图片（共 {endingImages.Count} 张）");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("EndingImagePanel: 已经是最后一张图片，无法继续切换");
            }
        }
    }
    
    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    private void OnExitGameClicked()
    {
        // 播放点击音效
        PlayClickSound();
        
        // 延迟执行退出（确保音效播放）
        StartCoroutine(PlaySoundAndExitGame());
    }
    
    /// <summary>
    /// 播放音效并退出游戏（协程）
    /// </summary>
    private IEnumerator PlaySoundAndExitGame()
    {
        // 等待音效播放时间
        if (clickSoundDelay > 0f)
        {
            yield return new WaitForSeconds(clickSoundDelay);
        }
        
        if (enableDebugLog)
        {
            Debug.Log("EndingImagePanel: 退出游戏");
        }
        
        // 调用退出游戏方法
        if (SceneTransitionManager.Instance != null)
        {
            // 如果SceneTransitionManager有QuitGame方法，使用它
            var quitMethod = typeof(SceneTransitionManager).GetMethod("QuitGame");
            if (quitMethod != null)
            {
                quitMethod.Invoke(SceneTransitionManager.Instance, null);
            }
            else
            {
                // 如果没有QuitGame方法，直接调用Application.Quit
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        else
        {
            // 如果没有SceneTransitionManager，直接退出
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    /// <summary>
    /// 更新图片显示
    /// </summary>
    private void UpdateImageDisplay()
    {
        if (currentImageDisplay == null)
        {
            return;
        }
        
        // 检查索引是否有效
        if (currentImageIndex >= 0 && currentImageIndex < endingImages.Count)
        {
            if (endingImages[currentImageIndex] != null)
            {
                currentImageDisplay.sprite = endingImages[currentImageIndex];
            }
            else
            {
                Debug.LogWarning($"EndingImagePanel: 索引 {currentImageIndex} 对应的图片为null");
            }
        }
        else
        {
            Debug.LogError($"EndingImagePanel: 图片索引 {currentImageIndex} 超出范围（有效范围: 0-{endingImages.Count - 1}）");
        }
    }
    
    /// <summary>
    /// 更新按钮显示状态
    /// </summary>
    private void UpdateButtonVisibility()
    {
        // 判断是否是最后一张图片
        bool isLastImage = currentImageIndex >= endingImages.Count - 1;
        
        // 控制切换按钮的显示/隐藏
        if (nextImageButton != null)
        {
            nextImageButton.gameObject.SetActive(!isLastImage);
        }
        
        // 控制退出按钮的显示/隐藏
        if (exitGameButton != null)
        {
            exitGameButton.gameObject.SetActive(isLastImage);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"EndingImagePanel: 按钮状态更新 - 切换按钮: {!isLastImage}, 退出按钮: {isLastImage}");
        }
    }
    
    /// <summary>
    /// 播放点击音效
    /// </summary>
    private void PlayClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// 播放图片切换音效
    /// </summary>
    private void PlaySwitchSound()
    {
        if (audioSource != null && imageSwitchSound != null)
        {
            audioSource.PlayOneShot(imageSwitchSound);
        }
    }
    
    /// <summary>
    /// 获取当前图片索引（用于调试）
    /// </summary>
    public int GetCurrentImageIndex()
    {
        return currentImageIndex;
    }
    
    /// <summary>
    /// 获取图片总数（用于调试）
    /// </summary>
    public int GetImageCount()
    {
        return endingImages != null ? endingImages.Count : 0;
    }
}
