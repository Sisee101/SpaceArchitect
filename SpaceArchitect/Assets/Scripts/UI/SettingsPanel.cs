using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板控制器（基础版）
/// 处理设置相关的UI交互
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    
    [Header("音量控制")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("音量显示文本")]
    [SerializeField] private Text bgmVolumeText;
    [SerializeField] private Text sfxVolumeText;
    
    // 音量键名（用于PlayerPrefs）
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    void Start()
    {
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        // 加载保存的设置
        LoadSettings();
        
        // 绑定音量滑块事件
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        LoadSettings(); // 显示时重新加载设置
    }
    
    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("返回主菜单");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainMenu();
        }
    }
    
    /// <summary>
    /// BGM音量改变事件
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        // 保存设置
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // 更新显示文本
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }
        
        // TODO: 实际应用音量到音频系统
        // AudioManager.Instance.SetBGMVolume(value);
    }
    
    /// <summary>
    /// SFX音量改变事件
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // 保存设置
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // 更新显示文本
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }
        
        // TODO: 实际应用音量到音频系统
        // AudioManager.Instance.SetSFXVolume(value);
    }
    
    /// <summary>
    /// 加载保存的设置
    /// </summary>
    private void LoadSettings()
    {
        // 加载BGM音量（默认0.7）
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.7f);
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = bgmVolume;
        }
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = Mathf.RoundToInt(bgmVolume * 100).ToString() + "%";
        }
        
        // 加载SFX音量（默认0.7）
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.7f);
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVolume;
        }
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = Mathf.RoundToInt(sfxVolume * 100).ToString() + "%";
        }
    }
}

