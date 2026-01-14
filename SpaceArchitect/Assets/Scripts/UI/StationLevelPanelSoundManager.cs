using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// StationLevelPanel 音效管理器
/// 低耦合的音效管理系统，通过配置方式管理升级按键和确认键的音效
/// </summary>
public class StationLevelPanelSoundManager : MonoBehaviour
{
    /// <summary>
    /// 音效类型枚举
    /// </summary>
    public enum SoundType
    {
        UpgradeButton,  // 升级按键音效
        ConfirmButton   // 确认键音效
    }
    
    /// <summary>
    /// 按钮-音效映射配置
    /// </summary>
    [System.Serializable]
    public class ButtonSoundMapping
    {
        [Tooltip("按钮引用（如果为空，会尝试通过按钮名称查找）")]
        public Button button;
        
        [Tooltip("按钮名称（用于自动查找，如果 button 引用为空）")]
        public string buttonName;
        
        [Tooltip("音效类型（升级按键或确认键）")]
        public SoundType soundType;
        
        [Tooltip("是否已绑定事件（运行时自动设置）")]
        public bool isBound = false;
    }
    
    [Header("音效配置")]
    [Tooltip("升级按键音效（所有升级按键共用）")]
    [SerializeField] private AudioClip upgradeButtonSound;
    
    [Tooltip("确认键音效")]
    [SerializeField] private AudioClip confirmButtonSound;
    
    [Header("按钮-音效映射")]
    [Tooltip("按钮和音效类型的映射列表（在 Inspector 中配置）")]
    [SerializeField] private List<ButtonSoundMapping> buttonMappings = new List<ButtonSoundMapping>();
    
    [Header("音频源设置")]
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("是否自动查找未配置的按钮（通过名称）")]
    [SerializeField] private bool autoFindButtons = true;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 已绑定的按钮集合（用于防止重复绑定）
    private HashSet<Button> boundButtons = new HashSet<Button>();
    
    void Awake()
    {
        // 初始化音频源
        InitializeAudioSource();
    }
    
    void OnEnable()
    {
        // 面板激活时绑定按钮事件
        BindButtonEvents();
    }
    
    void OnDisable()
    {
        // 面板禁用时解绑按钮事件（可选，防止内存泄漏）
        UnbindButtonEvents();
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
            
            // 如果还是没有，尝试从父对象（StationLevelPanel）获取
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
                    Debug.Log("StationLevelPanelSoundManager: 已自动创建 AudioSource 组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 绑定所有按钮的点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 先尝试自动查找未配置的按钮
        if (autoFindButtons)
        {
            AutoFindButtons();
        }
        
        // 绑定所有配置的按钮
        foreach (var mapping in buttonMappings)
        {
            if (mapping == null)
            {
                continue;
            }
            
            // 获取按钮引用
            Button button = GetButtonFromMapping(mapping);
            
            if (button == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"StationLevelPanelSoundManager: 无法找到按钮（名称: {mapping.buttonName}），跳过绑定");
                }
                continue;
            }
            
            // 防止重复绑定
            if (boundButtons.Contains(button))
            {
                continue;
            }
            
            // 绑定点击事件
            button.onClick.AddListener(() => OnButtonClicked(mapping.soundType));
            boundButtons.Add(button);
            mapping.isBound = true;
            
            if (enableDebugLog)
            {
                Debug.Log($"StationLevelPanelSoundManager: 已绑定按钮 {button.name}，音效类型: {mapping.soundType}");
            }
        }
    }
    
    /// <summary>
    /// 解绑所有按钮的点击事件
    /// </summary>
    private void UnbindButtonEvents()
    {
        foreach (var mapping in buttonMappings)
        {
            if (mapping == null || !mapping.isBound)
            {
                continue;
            }
            
            Button button = GetButtonFromMapping(mapping);
            if (button != null)
            {
                // 注意：由于使用了匿名函数，无法精确移除
                // 这里只是标记为未绑定，实际解绑会在按钮销毁时自动完成
                mapping.isBound = false;
            }
        }
        
        boundButtons.Clear();
    }
    
    /// <summary>
    /// 从映射配置中获取按钮引用
    /// </summary>
    private Button GetButtonFromMapping(ButtonSoundMapping mapping)
    {
        // 优先使用直接引用
        if (mapping.button != null)
        {
            return mapping.button;
        }
        
        // 如果引用为空，尝试通过名称查找
        if (!string.IsNullOrEmpty(mapping.buttonName))
        {
            // 在当前 GameObject 及其子对象中查找
            Button foundButton = FindButtonByName(mapping.buttonName);
            if (foundButton != null)
            {
                // 保存找到的按钮引用，避免下次再查找
                mapping.button = foundButton;
                return foundButton;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 通过名称查找按钮
    /// </summary>
    private Button FindButtonByName(string buttonName)
    {
        // 在当前 GameObject 及其子对象中查找
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        
        foreach (Button btn in allButtons)
        {
            if (btn.name == buttonName || btn.name.Contains(buttonName))
            {
                return btn;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 自动查找未配置的按钮
    /// </summary>
    private void AutoFindButtons()
    {
        // 获取所有按钮
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        
        // 检查每个按钮是否已在映射列表中
        foreach (Button btn in allButtons)
        {
            // 跳过返回按钮（通常由面板自己管理）
            if (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("return"))
            {
                continue;
            }
            
            // 检查是否已配置
            bool alreadyConfigured = false;
            foreach (var mapping in buttonMappings)
            {
                if (mapping != null && mapping.button == btn)
                {
                    alreadyConfigured = true;
                    break;
                }
            }
            
            // 如果未配置，尝试添加到映射列表
            if (!alreadyConfigured)
            {
                // 根据按钮名称判断音效类型（启发式判断）
                SoundType soundType = GuessSoundType(btn.name);
                
                var newMapping = new ButtonSoundMapping
                {
                    button = btn,
                    buttonName = btn.name,
                    soundType = soundType
                };
                
                buttonMappings.Add(newMapping);
                
                if (enableDebugLog)
                {
                    Debug.Log($"StationLevelPanelSoundManager: 自动找到按钮 {btn.name}，推测音效类型: {soundType}");
                }
            }
        }
    }
    
    /// <summary>
    /// 根据按钮名称推测音效类型（启发式方法）
    /// </summary>
    private SoundType GuessSoundType(string buttonName)
    {
        string lowerName = buttonName.ToLower();
        
        // 如果包含 confirm、ok、apply、submit 等关键词，认为是确认键
        if (lowerName.Contains("confirm") || lowerName.Contains("ok") || 
            lowerName.Contains("apply") || lowerName.Contains("submit") ||
            lowerName.Contains("确定") || lowerName.Contains("确认"))
        {
            return SoundType.ConfirmButton;
        }
        
        // 其他情况默认为升级按键
        return SoundType.UpgradeButton;
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClicked(SoundType soundType)
    {
        AudioClip clipToPlay = null;
        
        // 根据音效类型选择对应的音效
        switch (soundType)
        {
            case SoundType.UpgradeButton:
                clipToPlay = upgradeButtonSound;
                break;
            case SoundType.ConfirmButton:
                clipToPlay = confirmButtonSound;
                break;
        }
        
        // 播放音效
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
            
            if (enableDebugLog)
            {
                Debug.Log($"StationLevelPanelSoundManager: 播放音效 {clipToPlay.name}，类型: {soundType}");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                if (clipToPlay == null)
                {
                    Debug.LogWarning($"StationLevelPanelSoundManager: 音效类型 {soundType} 未配置音效文件");
                }
                if (audioSource == null)
                {
                    Debug.LogWarning("StationLevelPanelSoundManager: AudioSource 未配置");
                }
            }
        }
    }
    
    /// <summary>
    /// 手动添加按钮映射（运行时调用）
    /// </summary>
    public void AddButtonMapping(Button button, SoundType soundType)
    {
        if (button == null)
        {
            Debug.LogWarning("StationLevelPanelSoundManager: 按钮为空，无法添加映射");
            return;
        }
        
        // 检查是否已存在
        foreach (var mapping in buttonMappings)
        {
            if (mapping != null && mapping.button == button)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"StationLevelPanelSoundManager: 按钮 {button.name} 已存在于映射列表中");
                }
                return;
            }
        }
        
        // 添加新映射
        var newMapping = new ButtonSoundMapping
        {
            button = button,
            buttonName = button.name,
            soundType = soundType
        };
        
        buttonMappings.Add(newMapping);
        
        // 立即绑定事件
        if (gameObject.activeInHierarchy)
        {
            button.onClick.AddListener(() => OnButtonClicked(soundType));
            boundButtons.Add(button);
            newMapping.isBound = true;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"StationLevelPanelSoundManager: 已添加按钮映射 {button.name}，音效类型: {soundType}");
        }
    }
    
    /// <summary>
    /// 移除按钮映射（运行时调用）
    /// </summary>
    public void RemoveButtonMapping(Button button)
    {
        if (button == null)
        {
            return;
        }
        
        for (int i = buttonMappings.Count - 1; i >= 0; i--)
        {
            if (buttonMappings[i] != null && buttonMappings[i].button == button)
            {
                // 解绑事件（注意：由于使用匿名函数，无法精确移除）
                // 但按钮销毁时会自动清理
                boundButtons.Remove(button);
                buttonMappings.RemoveAt(i);
                
                if (enableDebugLog)
                {
                    Debug.Log($"StationLevelPanelSoundManager: 已移除按钮映射 {button.name}");
                }
                return;
            }
        }
    }
    
    /// <summary>
    /// 设置升级按键音效（运行时调用）
    /// </summary>
    public void SetUpgradeButtonSound(AudioClip clip)
    {
        upgradeButtonSound = clip;
    }
    
    /// <summary>
    /// 设置确认键音效（运行时调用）
    /// </summary>
    public void SetConfirmButtonSound(AudioClip clip)
    {
        confirmButtonSound = clip;
    }
    
    /// <summary>
    /// 测试播放升级按键音效（用于测试）
    /// </summary>
    [ContextMenu("测试播放升级按键音效")]
    public void TestPlayUpgradeSound()
    {
        if (upgradeButtonSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(upgradeButtonSound);
            Debug.Log("StationLevelPanelSoundManager: 测试播放升级按键音效");
        }
        else
        {
            Debug.LogWarning("StationLevelPanelSoundManager: 升级按键音效未配置");
        }
    }
    
    /// <summary>
    /// 测试播放确认键音效（用于测试）
    /// </summary>
    [ContextMenu("测试播放确认键音效")]
    public void TestPlayConfirmSound()
    {
        if (confirmButtonSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(confirmButtonSound);
            Debug.Log("StationLevelPanelSoundManager: 测试播放确认键音效");
        }
        else
        {
            Debug.LogWarning("StationLevelPanelSoundManager: 确认键音效未配置");
        }
    }
}
