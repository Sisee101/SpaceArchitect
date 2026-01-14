using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 通用按钮音效管理器
/// 低耦合的音效管理系统，支持多种音效类型，每种类型可配置多个按钮
/// 支持点击音效和悬停音效（可选）
/// </summary>
public class UniversalButtonSoundManager : MonoBehaviour
{
    /// <summary>
    /// 音效类型配置
    /// </summary>
    [System.Serializable]
    public class SoundTypeConfig
    {
        [Tooltip("音效类型名称（用于标识，如：升级按键、确认键等）")]
        public string soundTypeName = "音效类型";
        
        [Tooltip("点击音效（必需）")]
        public AudioClip clickSound;
        
        [Tooltip("悬停音效（可选，如果为空则此音效类型的按钮不播放悬停音效）")]
        public AudioClip hoverSound;
        
        [Tooltip("使用此音效的按钮列表")]
        public List<Button> buttons = new List<Button>();
        
        [Tooltip("是否已绑定事件（运行时自动设置）")]
        public bool isBound = false;
    }
    
    [Header("音效类型配置")]
    [Tooltip("音效类型配置列表（每种类型可配置多个按钮）")]
    [SerializeField] private List<SoundTypeConfig> soundTypeConfigs = new List<SoundTypeConfig>();
    
    [Header("全局悬停音效（可选，已废弃）")]
    [Tooltip("全局悬停音效（已废弃，请在每个音效类型配置中单独设置悬停音效）")]
    [SerializeField] private AudioClip globalHoverSound;
    
    [Header("音频源设置")]
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("自动查找设置")]
    [Tooltip("是否自动查找子对象中的按钮（用于快速配置）")]
    [SerializeField] private bool autoFindButtons = false;
    
    [Tooltip("自动查找时，是否排除名称包含以下关键词的按钮（用逗号分隔，如：back,return,close）")]
    [SerializeField] private string excludeButtonKeywords = "back,return,close,取消,返回";
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 已绑定的按钮集合（用于防止重复绑定）
    private HashSet<Button> boundButtons = new HashSet<Button>();
    
    // 悬停音效事件触发器映射（用于悬停音效）
    private Dictionary<Button, EventTrigger> buttonEventTriggers = new Dictionary<Button, EventTrigger>();
    
    void Awake()
    {
        // 初始化音频源
        InitializeAudioSource();
    }
    
    void OnEnable()
    {
        // 面板激活时绑定按钮事件
        BindAllButtonEvents();
    }
    
    void OnDisable()
    {
        // 面板禁用时解绑按钮事件（可选，防止内存泄漏）
        UnbindAllButtonEvents();
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            // 尝试从当前 GameObject 获取
            audioSource = GetComponent<AudioSource>();
            
            // 如果还是没有，尝试从父对象获取
            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
            
            // 如果还是没有，自动创建一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                
                if (enableDebugLog)
                {
                    Debug.Log("UniversalButtonSoundManager: 已自动创建 AudioSource 组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 绑定所有按钮的事件
    /// </summary>
    private void BindAllButtonEvents()
    {
        // 先尝试自动查找按钮（如果启用）
        if (autoFindButtons)
        {
            AutoFindButtons();
        }
        
        // 绑定所有配置的按钮
        foreach (var config in soundTypeConfigs)
        {
            if (config == null || config.clickSound == null)
            {
                continue;
            }
            
            // 验证配置
            if (string.IsNullOrEmpty(config.soundTypeName))
            {
                config.soundTypeName = "未命名音效类型";
            }
            
            // 绑定该配置中的所有按钮
            foreach (Button button in config.buttons)
            {
                if (button == null)
                {
                    continue;
                }
                
                // 防止重复绑定
                if (boundButtons.Contains(button))
                {
                    if (enableDebugLog)
                    {
                        Debug.LogWarning($"UniversalButtonSoundManager: 按钮 {button.name} 已在其他音效类型中绑定，跳过");
                    }
                    continue;
                }
                
                // 绑定点击事件
                button.onClick.AddListener(() => OnButtonClicked(config.clickSound, config.soundTypeName));
                boundButtons.Add(button);
                
                // 绑定悬停事件（如果该音效类型配置了悬停音效）
                if (config.hoverSound != null)
                {
                    BindHoverEvent(button, config.hoverSound, config.soundTypeName);
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"UniversalButtonSoundManager: 已绑定按钮 {button.name}，音效类型: {config.soundTypeName}");
                }
            }
            
            config.isBound = true;
        }
    }
    
    /// <summary>
    /// 解绑所有按钮的事件
    /// </summary>
    private void UnbindAllButtonEvents()
    {
        // 注意：由于使用了匿名函数，无法精确移除
        // 但按钮销毁时会自动清理，这里只是标记为未绑定
        foreach (var config in soundTypeConfigs)
        {
            if (config != null)
            {
                config.isBound = false;
            }
        }
        
        boundButtons.Clear();
        buttonEventTriggers.Clear();
    }
    
    /// <summary>
    /// 绑定悬停事件
    /// </summary>
    private void BindHoverEvent(Button button, AudioClip hoverSoundClip, string soundTypeName)
    {
        if (button == null || hoverSoundClip == null)
        {
            return;
        }
        
        // 获取或添加 EventTrigger 组件
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 检查是否已添加悬停事件
        bool hasHoverEntry = false;
        foreach (var entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
            {
                hasHoverEntry = true;
                break;
            }
        }
        
        // 如果没有悬停事件，添加一个
        if (!hasHoverEntry)
        {
            EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
            hoverEntry.eventID = EventTriggerType.PointerEnter;
            // 使用闭包捕获音效文件和类型名称
            AudioClip clipToPlay = hoverSoundClip;
            string typeName = soundTypeName;
            hoverEntry.callback.AddListener((data) => { OnButtonHovered(clipToPlay, typeName); });
            eventTrigger.triggers.Add(hoverEntry);
            
            buttonEventTriggers[button] = eventTrigger;
            
            if (enableDebugLog)
            {
                Debug.Log($"UniversalButtonSoundManager: 已为按钮 {button.name} 绑定悬停音效（类型: {soundTypeName}）");
            }
        }
    }
    
    /// <summary>
    /// 自动查找按钮
    /// </summary>
    private void AutoFindButtons()
    {
        // 获取所有按钮
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        
        // 解析排除关键词
        string[] excludeKeywords = excludeButtonKeywords.Split(',');
        for (int i = 0; i < excludeKeywords.Length; i++)
        {
            excludeKeywords[i] = excludeKeywords[i].Trim().ToLower();
        }
        
        // 检查每个按钮是否已在配置中
        foreach (Button btn in allButtons)
        {
            if (btn == null)
            {
                continue;
            }
            
            // 检查是否在排除列表中
            string btnNameLower = btn.name.ToLower();
            bool shouldExclude = false;
            foreach (string keyword in excludeKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) && btnNameLower.Contains(keyword))
                {
                    shouldExclude = true;
                    break;
                }
            }
            
            if (shouldExclude)
            {
                continue;
            }
            
            // 检查是否已在配置中
            bool alreadyConfigured = false;
            foreach (var config in soundTypeConfigs)
            {
                if (config != null && config.buttons.Contains(btn))
                {
                    alreadyConfigured = true;
                    break;
                }
            }
            
            // 如果未配置，添加到第一个音效类型配置（或创建新配置）
            if (!alreadyConfigured)
            {
                // 如果没有任何配置，创建一个默认配置
                if (soundTypeConfigs.Count == 0)
                {
                    soundTypeConfigs.Add(new SoundTypeConfig
                    {
                        soundTypeName = "默认音效类型",
                        clickSound = null,
                        buttons = new List<Button>()
                    });
                }
                
                // 添加到第一个配置（用户可以在 Inspector 中调整）
                soundTypeConfigs[0].buttons.Add(btn);
                
                if (enableDebugLog)
                {
                    Debug.Log($"UniversalButtonSoundManager: 自动找到按钮 {btn.name}，已添加到音效类型: {soundTypeConfigs[0].soundTypeName}");
                }
            }
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClicked(AudioClip clip, string soundTypeName)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            
            if (enableDebugLog)
            {
                Debug.Log($"UniversalButtonSoundManager: 播放点击音效 {clip.name}，类型: {soundTypeName}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                if (clip == null)
                {
                    Debug.LogWarning($"UniversalButtonSoundManager: 音效类型 {soundTypeName} 未配置音效文件");
                }
                if (audioSource == null)
                {
                    Debug.LogWarning("UniversalButtonSoundManager: AudioSource 未配置");
                }
            }
        }
    }
    
    /// <summary>
    /// 按钮悬停事件处理
    /// </summary>
    private void OnButtonHovered(AudioClip hoverSoundClip, string soundTypeName)
    {
        if (hoverSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSoundClip);
            
            if (enableDebugLog)
            {
                Debug.Log($"UniversalButtonSoundManager: 播放悬停音效 {hoverSoundClip.name}，类型: {soundTypeName}");
            }
        }
    }
    
    #region 运行时 API
    
    /// <summary>
    /// 添加按钮到指定音效类型（运行时调用）
    /// </summary>
    /// <param name="button">要添加的按钮</param>
    /// <param name="soundTypeName">音效类型名称（如果不存在则创建）</param>
    /// <param name="clickSound">点击音效（如果音效类型不存在，需要提供）</param>
    public void AddButtonToSoundType(Button button, string soundTypeName, AudioClip clickSound = null)
    {
        if (button == null)
        {
            Debug.LogWarning("UniversalButtonSoundManager: 按钮为空，无法添加");
            return;
        }
        
        if (string.IsNullOrEmpty(soundTypeName))
        {
            Debug.LogWarning("UniversalButtonSoundManager: 音效类型名称为空");
            return;
        }
        
        // 查找或创建音效类型配置
        SoundTypeConfig config = FindOrCreateSoundTypeConfig(soundTypeName, clickSound);
        
        // 检查按钮是否已在列表中
        if (config.buttons.Contains(button))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"UniversalButtonSoundManager: 按钮 {button.name} 已在音效类型 {soundTypeName} 中");
            }
            return;
        }
        
        // 添加到列表
        config.buttons.Add(button);
        
        // 如果面板已激活，立即绑定事件
        if (gameObject.activeInHierarchy)
        {
            if (!boundButtons.Contains(button))
            {
                button.onClick.AddListener(() => OnButtonClicked(config.clickSound, config.soundTypeName));
                boundButtons.Add(button);
                
                // 如果该音效类型配置了悬停音效，绑定悬停事件
                if (config.hoverSound != null)
                {
                    BindHoverEvent(button, config.hoverSound, config.soundTypeName);
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"UniversalButtonSoundManager: 已添加按钮 {button.name} 到音效类型 {soundTypeName}");
        }
    }
    
    /// <summary>
    /// 从音效类型中移除按钮（运行时调用）
    /// </summary>
    public void RemoveButtonFromSoundType(Button button, string soundTypeName)
    {
        if (button == null || string.IsNullOrEmpty(soundTypeName))
        {
            return;
        }
        
        SoundTypeConfig config = FindSoundTypeConfig(soundTypeName);
        if (config != null && config.buttons.Contains(button))
        {
            config.buttons.Remove(button);
            boundButtons.Remove(button);
            
            if (enableDebugLog)
            {
                Debug.Log($"UniversalButtonSoundManager: 已从音效类型 {soundTypeName} 移除按钮 {button.name}");
            }
        }
    }
    
    /// <summary>
    /// 创建新的音效类型配置（运行时调用）
    /// </summary>
    public void CreateSoundTypeConfig(string soundTypeName, AudioClip clickSound)
    {
        if (string.IsNullOrEmpty(soundTypeName))
        {
            Debug.LogWarning("UniversalButtonSoundManager: 音效类型名称为空");
            return;
        }
        
        // 检查是否已存在
        if (FindSoundTypeConfig(soundTypeName) != null)
        {
            Debug.LogWarning($"UniversalButtonSoundManager: 音效类型 {soundTypeName} 已存在");
            return;
        }
        
        // 创建新配置
        soundTypeConfigs.Add(new SoundTypeConfig
        {
            soundTypeName = soundTypeName,
            clickSound = clickSound,
            buttons = new List<Button>()
        });
        
        if (enableDebugLog)
        {
            Debug.Log($"UniversalButtonSoundManager: 已创建音效类型配置 {soundTypeName}");
        }
    }
    
    /// <summary>
    /// 为指定音效类型设置悬停音效（运行时调用）
    /// </summary>
    public void SetHoverSoundForType(string soundTypeName, AudioClip clip)
    {
        SoundTypeConfig config = FindSoundTypeConfig(soundTypeName);
        if (config != null)
        {
            config.hoverSound = clip;
            
            // 如果已绑定按钮，需要重新绑定悬停事件
            if (gameObject.activeInHierarchy)
            {
                foreach (Button button in config.buttons)
                {
                    if (button != null && boundButtons.Contains(button))
                    {
                        if (clip != null)
                        {
                            BindHoverEvent(button, clip, soundTypeName);
                        }
                    }
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"UniversalButtonSoundManager: 已为音效类型 {soundTypeName} 设置悬停音效");
            }
        }
        else
        {
            Debug.LogWarning($"UniversalButtonSoundManager: 未找到音效类型 {soundTypeName}");
        }
    }
    
    /// <summary>
    /// 查找按钮所属的音效类型配置
    /// </summary>
    private SoundTypeConfig FindConfigForButton(Button button)
    {
        foreach (var config in soundTypeConfigs)
        {
            if (config != null && config.buttons.Contains(button))
            {
                return config;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 查找音效类型配置
    /// </summary>
    private SoundTypeConfig FindSoundTypeConfig(string soundTypeName)
    {
        foreach (var config in soundTypeConfigs)
        {
            if (config != null && config.soundTypeName == soundTypeName)
            {
                return config;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 查找或创建音效类型配置
    /// </summary>
    private SoundTypeConfig FindOrCreateSoundTypeConfig(string soundTypeName, AudioClip clickSound)
    {
        SoundTypeConfig config = FindSoundTypeConfig(soundTypeName);
        
        if (config == null)
        {
            // 创建新配置
            config = new SoundTypeConfig
            {
                soundTypeName = soundTypeName,
                clickSound = clickSound,
                buttons = new List<Button>()
            };
            soundTypeConfigs.Add(config);
        }
        else if (clickSound != null && config.clickSound == null)
        {
            // 如果配置存在但没有音效，更新音效
            config.clickSound = clickSound;
        }
        
        return config;
    }
    
    #endregion
    
    #region 测试方法
    
    /// <summary>
    /// 测试播放指定音效类型的点击音效（用于测试）
    /// </summary>
    [ContextMenu("测试播放第一个音效类型的点击音效")]
    public void TestPlayFirstSoundType()
    {
        if (soundTypeConfigs.Count > 0 && soundTypeConfigs[0] != null)
        {
            if (soundTypeConfigs[0].clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundTypeConfigs[0].clickSound);
                Debug.Log($"UniversalButtonSoundManager: 测试播放音效类型 {soundTypeConfigs[0].soundTypeName}");
            }
            else
            {
                Debug.LogWarning("UniversalButtonSoundManager: 第一个音效类型未配置音效文件");
            }
        }
        else
        {
            Debug.LogWarning("UniversalButtonSoundManager: 没有配置任何音效类型");
        }
    }
    
    /// <summary>
    /// 测试播放第一个音效类型的悬停音效（用于测试）
    /// </summary>
    [ContextMenu("测试播放第一个音效类型的悬停音效")]
    public void TestPlayFirstHoverSound()
    {
        if (soundTypeConfigs.Count > 0 && soundTypeConfigs[0] != null)
        {
            if (soundTypeConfigs[0].hoverSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundTypeConfigs[0].hoverSound);
                Debug.Log($"UniversalButtonSoundManager: 测试播放音效类型 {soundTypeConfigs[0].soundTypeName} 的悬停音效");
            }
            else
            {
                Debug.LogWarning($"UniversalButtonSoundManager: 音效类型 {soundTypeConfigs[0].soundTypeName} 未配置悬停音效");
            }
        }
        else
        {
            Debug.LogWarning("UniversalButtonSoundManager: 没有配置任何音效类型");
        }
    }
    
    #endregion
}
