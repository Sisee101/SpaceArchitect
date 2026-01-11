using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 主菜单面板控制器
/// 处理主菜单的所有按钮点击事件和鼠标悬停高亮效果
/// </summary>
public class MainMenuPanel : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button planetEncyclopediaButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("高亮控制器")]
    [SerializeField] private MenuHighlightController highlightController; // 高亮跟随控制器
    
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;          // 音频源组件
    [SerializeField] private AudioClip buttonHoverSound;       // 按钮悬停音效
    [SerializeField] private AudioClip buttonClickSound;       // 按钮点击音效
    [SerializeField] private float clickSoundDelay = 0.15f;    // 点击音效播放后的延迟时间（秒），用于确保音效播放完成再执行后续操作
    
    void Start()
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
        
        // 绑定按钮点击事件
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(startGameButton);
        }
        
        if (planetEncyclopediaButton != null)
        {
            planetEncyclopediaButton.onClick.AddListener(OnPlanetEncyclopediaClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(planetEncyclopediaButton);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(settingsButton);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            // 添加鼠标悬停高亮效果
            SetupButtonHoverHighlight(quitButton);
        }
    }
    
    /// <summary>
    /// 为按钮设置鼠标悬停高亮效果
    /// </summary>
    /// <param name="button">目标按钮</param>
    private void SetupButtonHoverHighlight(Button button)
    {
        if (button == null) return;
        
        // 获取或添加 EventTrigger 组件
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 检查是否已经存在 PointerEnter 事件（避免重复添加）
        bool alreadyExists = false;
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                alreadyExists = true;
                break;
            }
        }
        
        // 如果不存在，则添加
        if (!alreadyExists)
        {
            // 创建 PointerEnter 事件（鼠标进入）
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((eventData) => OnButtonHovered(button.GetComponent<RectTransform>()));
            
            // 添加到 EventTrigger
            eventTrigger.triggers.Add(entryEnter);
        }
    }
    
    /// <summary>
    /// 按钮鼠标悬停事件（通过按钮引用）
    /// </summary>
    /// <param name="buttonRect">按钮的 RectTransform</param>
    private void OnButtonHovered(RectTransform buttonRect)
    {
        if (highlightController != null && buttonRect != null)
        {
            highlightController.MoveToButton(buttonRect);
        }
        
        // 播放悬停音效
        PlayButtonHoverSound();
    }
    
    /// <summary>
    /// 播放按钮悬停音效
    /// </summary>
    private void PlayButtonHoverSound()
    {
        if (audioSource != null && buttonHoverSound != null)
        {
            audioSource.PlayOneShot(buttonHoverSound);
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
    /// 显示主菜单面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏主菜单面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    private void OnStartGameClicked()
    {
        // 播放点击音效并延迟执行场景切换，确保音效能够播放
        StartCoroutine(PlayClickSoundAndLoadScene());
    }
    
    /// <summary>
    /// 播放点击音效并延迟加载场景（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndLoadScene()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        // 等待一小段时间，让音效有时间播放
        yield return new WaitForSeconds(clickSoundDelay);
        
        Debug.Log("开始游戏 - 跳转到主界面");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainHubScene();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
    
    /// <summary>
    /// 行星图鉴按钮点击事件
    /// </summary>
    private void OnPlanetEncyclopediaClicked()
    {
        // 播放点击音效并延迟打开面板，确保音效能够播放
        StartCoroutine(PlayClickSoundAndShowPlanetEncyclopedia());
    }
    
    /// <summary>
    /// 播放点击音效并延迟显示行星图鉴（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndShowPlanetEncyclopedia()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        // 等待一小段时间，让音效有时间播放
        yield return new WaitForSeconds(clickSoundDelay);
        
        Debug.Log("打开行星图鉴");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPlanetEncyclopedia();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
        }
    }
    
    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void OnSettingsClicked()
    {
        // 播放点击音效并延迟打开面板，确保音效能够播放
        StartCoroutine(PlayClickSoundAndShowSettings());
    }
    
    /// <summary>
    /// 播放点击音效并延迟显示设置（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndShowSettings()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        // 等待一小段时间，让音效有时间播放
        yield return new WaitForSeconds(clickSoundDelay);
        
        Debug.Log("打开设置");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
        else
        {
            Debug.LogError("UIManager未找到！");
        }
    }
    
    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    private void OnQuitClicked()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        Debug.Log("退出游戏");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.QuitGame();
        }
        else
        {
            Debug.LogError("SceneTransitionManager未找到！");
        }
    }
}

