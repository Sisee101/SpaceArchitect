using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 面板按钮音效管理器（可复用组件）
/// 可以附加到任何面板上，管理该面板下所有按钮的音效播放
/// 支持在Inspector中配置按钮和音效，自动绑定点击事件
/// </summary>
public class PanelButtonSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonSoundEntry
    {
        [Tooltip("按钮引用（拖拽按钮到此处）")]
        public Button button;
        
        [Tooltip("点击音效（如果为空，使用默认点击音效）")]
        public AudioClip clickSound;
        
        [Tooltip("悬停音效（如果为空，使用默认悬停音效，可选）")]
        public AudioClip hoverSound;
    }
    
    [Header("音频源配置")]
    [Tooltip("音频源组件（如果为空，自动创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("默认音效")]
    [Tooltip("默认点击音效（所有按钮共用，除非单独配置）")]
    [SerializeField] private AudioClip defaultClickSound;
    
    [Tooltip("默认悬停音效（所有按钮共用，除非单独配置，可选）")]
    [SerializeField] private AudioClip defaultHoverSound;
    
    [Header("按钮音效配置")]
    [Tooltip("配置该面板下的按钮和对应音效")]
    [SerializeField] private List<ButtonSoundEntry> buttonSounds = new List<ButtonSoundEntry>();
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 按钮到音效配置的快速查找字典（运行时构建）
    private Dictionary<Button, ButtonSoundEntry> buttonSoundMap = new Dictionary<Button, ButtonSoundEntry>();
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 构建按钮音效查找字典
        BuildButtonSoundMap();
        
        // 绑定按钮点击事件
        BindButtonEvents();
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            // 尝试获取组件上的AudioSource
            audioSource = GetComponent<AudioSource>();
            
            // 如果还是没有，自动创建一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                if (enableDebugLog)
                {
                    Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 自动创建AudioSource组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 构建按钮音效查找字典
    /// </summary>
    private void BuildButtonSoundMap()
    {
        buttonSoundMap.Clear();
        
        foreach (var entry in buttonSounds)
        {
            if (entry != null && entry.button != null)
            {
                buttonSoundMap[entry.button] = entry;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 已构建按钮音效字典，共 {buttonSoundMap.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 为所有配置的按钮绑定点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        foreach (var entry in buttonSounds)
        {
            if (entry != null && entry.button != null)
            {
                // 移除可能存在的旧监听器（避免重复）
                entry.button.onClick.RemoveListener(() => OnButtonClicked(entry.button));
                
                // 添加新的监听器
                entry.button.onClick.AddListener(() => OnButtonClicked(entry.button));
                
                if (enableDebugLog)
                {
                    Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 已为按钮 {entry.button.name} 绑定点击音效");
                }
            }
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    /// <param name="button">被点击的按钮</param>
    private void OnButtonClicked(Button button)
    {
        PlayButtonClick(button);
    }
    
    /// <summary>
    /// 播放按钮点击音效（供外部调用）
    /// </summary>
    /// <param name="button">按钮引用</param>
    public void PlayButtonClick(Button button)
    {
        if (button == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"PanelButtonSoundManager ({gameObject.name}): 按钮引用为空，无法播放音效");
            }
            return;
        }
        
        // 查找按钮配置
        ButtonSoundEntry entry = null;
        if (buttonSoundMap.ContainsKey(button))
        {
            entry = buttonSoundMap[button];
        }
        
        // 确定使用的音效
        AudioClip soundToPlay = null;
        if (entry != null && entry.clickSound != null)
        {
            soundToPlay = entry.clickSound;
        }
        else if (defaultClickSound != null)
        {
            soundToPlay = defaultClickSound;
        }
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
            if (enableDebugLog)
            {
                Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 播放按钮点击音效 - 按钮: {button.name}, 音效: {soundToPlay.name}");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"PanelButtonSoundManager ({gameObject.name}): 按钮 {button.name} 没有配置音效，且默认音效也未配置");
        }
    }
    
    /// <summary>
    /// 播放按钮悬停音效（供外部调用，需要配合EventTrigger使用）
    /// </summary>
    /// <param name="button">按钮引用</param>
    public void PlayButtonHover(Button button)
    {
        if (button == null)
        {
            return;
        }
        
        // 查找按钮配置
        ButtonSoundEntry entry = null;
        if (buttonSoundMap.ContainsKey(button))
        {
            entry = buttonSoundMap[button];
        }
        
        // 确定使用的音效
        AudioClip soundToPlay = null;
        if (entry != null && entry.hoverSound != null)
        {
            soundToPlay = entry.hoverSound;
        }
        else if (defaultHoverSound != null)
        {
            soundToPlay = defaultHoverSound;
        }
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
            if (enableDebugLog)
            {
                Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 播放按钮悬停音效 - 按钮: {button.name}, 音效: {soundToPlay.name}");
            }
        }
    }
    
    /// <summary>
    /// 手动添加按钮配置（供外部调用）
    /// </summary>
    /// <param name="button">按钮引用</param>
    /// <param name="clickSound">点击音效（可选）</param>
    /// <param name="hoverSound">悬停音效（可选）</param>
    public void AddButton(Button button, AudioClip clickSound = null, AudioClip hoverSound = null)
    {
        if (button == null)
        {
            Debug.LogWarning($"PanelButtonSoundManager ({gameObject.name}): 尝试添加空按钮引用");
            return;
        }
        
        // 检查按钮是否已存在
        ButtonSoundEntry existingEntry = buttonSounds.Find(e => e != null && e.button == button);
        if (existingEntry != null)
        {
            // 更新现有配置
            existingEntry.clickSound = clickSound;
            existingEntry.hoverSound = hoverSound;
            if (enableDebugLog)
            {
                Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 更新按钮 {button.name} 的音效配置");
            }
        }
        else
        {
            // 添加新配置
            ButtonSoundEntry newEntry = new ButtonSoundEntry
            {
                button = button,
                clickSound = clickSound,
                hoverSound = hoverSound
            };
            buttonSounds.Add(newEntry);
            
            // 绑定事件
            button.onClick.AddListener(() => OnButtonClicked(button));
            
            if (enableDebugLog)
            {
                Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 添加按钮 {button.name} 的音效配置");
            }
        }
        
        // 重新构建查找字典
        BuildButtonSoundMap();
    }
    
    /// <summary>
    /// 移除按钮配置（供外部调用）
    /// </summary>
    /// <param name="button">按钮引用</param>
    public void RemoveButton(Button button)
    {
        if (button == null)
        {
            return;
        }
        
        // 移除事件监听
        button.onClick.RemoveListener(() => OnButtonClicked(button));
        
        // 从列表中移除
        buttonSounds.RemoveAll(e => e != null && e.button == button);
        
        // 重新构建查找字典
        BuildButtonSoundMap();
        
        if (enableDebugLog)
        {
            Debug.Log($"PanelButtonSoundManager ({gameObject.name}): 移除按钮 {button.name} 的音效配置");
        }
    }
    
    void OnDestroy()
    {
        // 清理所有按钮的事件监听
        foreach (var entry in buttonSounds)
        {
            if (entry != null && entry.button != null)
            {
                entry.button.onClick.RemoveListener(() => OnButtonClicked(entry.button));
            }
        }
    }
}
