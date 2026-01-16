using UnityEngine;
using System.Collections;

/// <summary>
/// 音频管理器
/// 负责管理背景音乐（BGM）和音效（SFX）的播放、音量控制等
/// </summary>
public class AudioManager : MonoBehaviour
{
    
    [Header("背景音乐设置")]
    [Tooltip("背景音乐AudioSource组件（如果为空会自动创建）")]
    [SerializeField] private AudioSource bgmSource;
    
    [Tooltip("当前播放的背景音乐")]
    [SerializeField] private AudioClip currentBGM;
    
    [Tooltip("背景音乐淡入淡出时间（秒）")]
    [SerializeField] private float fadeTime = 1f;
    
    [Header("音效设置")]
    [Tooltip("音效AudioSource组件（用于播放SFX，如果为空会自动创建）")]
    [SerializeField] private AudioSource sfxSource;
    
    [Header("音量设置")]
    [Tooltip("BGM音量（0-1）")]
    [SerializeField] private float bgmVolume = 0.7f;
    
    [Tooltip("SFX音量（0-1）")]
    [SerializeField] private float sfxVolume = 0.7f;
    
    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;
    
    // PlayerPrefs键名
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    // 淡入淡出协程
    private Coroutine fadeCoroutine;
    
    [Header("自动播放设置")]
    [Tooltip("场景加载时自动播放背景音乐")]
    [SerializeField] private bool playOnStart = true;
    
    void Awake()
    {
        // 初始化AudioSource
        InitializeAudioSources();
        
        // 从PlayerPrefs加载音量设置
        LoadVolumeSettings();
    }
    
    void Start()
    {
        // 如果设置了自动播放，且已配置了背景音乐，则自动播放
        if (playOnStart && currentBGM != null)
        {
            PlayBGM(currentBGM, fadeIn: true);
        }
    }
    
    /// <summary>
    /// 初始化AudioSource组件
    /// </summary>
    private void InitializeAudioSources()
    {
        // 初始化BGM AudioSource
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true; // 背景音乐循环播放
            bgmSource.volume = bgmVolume;
        }
        
        // 初始化SFX AudioSource（可选，用于播放音效）
        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFXSource");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
        
        if (showDebugLog)
        {
            Debug.Log("AudioManager: AudioSource组件已初始化");
        }
    }
    
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="bgmClip">背景音乐AudioClip</param>
    /// <param name="fadeIn">是否淡入（默认true）</param>
    public void PlayBGM(AudioClip bgmClip, bool fadeIn = true)
    {
        if (bgmClip == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("AudioManager: 尝试播放空的背景音乐");
            }
            return;
        }
        
        // 如果正在播放相同的音乐，不重复播放
        if (currentBGM == bgmClip && bgmSource.isPlaying)
        {
            if (showDebugLog)
            {
                Debug.Log($"AudioManager: 背景音乐 {bgmClip.name} 已在播放中，跳过");
            }
            return;
        }
        
        currentBGM = bgmClip;
        
        // 停止淡入淡出协程（如果有）
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        if (fadeIn && bgmSource.isPlaying)
        {
            // 如果正在播放其他音乐，先淡出再淡入新音乐
            fadeCoroutine = StartCoroutine(FadeOutAndPlayBGM(bgmClip));
        }
        else
        {
            // 直接播放或淡入
            if (fadeIn)
            {
                fadeCoroutine = StartCoroutine(FadeInBGM(bgmClip));
            }
            else
            {
                bgmSource.clip = bgmClip;
                bgmSource.volume = bgmVolume;
                bgmSource.Play();
                
                if (showDebugLog)
                {
                    Debug.Log($"AudioManager: 开始播放背景音乐: {bgmClip.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    /// <param name="fadeOut">是否淡出（默认true）</param>
    public void StopBGM(bool fadeOut = true)
    {
        if (!bgmSource.isPlaying)
        {
            return;
        }
        
        // 停止淡入淡出协程（如果有）
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        if (fadeOut)
        {
            fadeCoroutine = StartCoroutine(FadeOutBGM());
        }
        else
        {
            bgmSource.Stop();
            currentBGM = null;
            
            if (showDebugLog)
            {
                Debug.Log("AudioManager: 背景音乐已停止");
            }
        }
    }
    
    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Pause();
            if (showDebugLog)
            {
                Debug.Log("AudioManager: 背景音乐已暂停");
            }
        }
    }
    
    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
            if (showDebugLog)
            {
                Debug.Log("AudioManager: 背景音乐已恢复");
            }
        }
    }
    
    /// <summary>
    /// 设置BGM音量
    /// </summary>
    /// <param name="volume">音量（0-1）</param>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
        
        // 保存到PlayerPrefs
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
        
        if (showDebugLog)
        {
            Debug.Log($"AudioManager: BGM音量已设置为 {bgmVolume:F2}");
        }
    }
    
    /// <summary>
    /// 设置SFX音量
    /// </summary>
    /// <param name="volume">音量（0-1）</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        
        // 保存到PlayerPrefs
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
        
        if (showDebugLog)
        {
            Debug.Log($"AudioManager: SFX音量已设置为 {sfxVolume:F2}");
        }
    }
    
    /// <summary>
    /// 获取BGM音量
    /// </summary>
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    
    /// <summary>
    /// 获取SFX音量
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
    
    /// <summary>
    /// 播放音效（使用PlayOneShot，可以同时播放多个音效）
    /// </summary>
    /// <param name="sfxClip">音效AudioClip</param>
    /// <param name="volumeScale">音量缩放（0-1，默认使用全局SFX音量）</param>
    public void PlaySFX(AudioClip sfxClip, float volumeScale = 1f)
    {
        if (sfxClip == null || sfxSource == null)
        {
            return;
        }
        
        float finalVolume = sfxVolume * Mathf.Clamp01(volumeScale);
        sfxSource.PlayOneShot(sfxClip, finalVolume);
    }
    
    /// <summary>
    /// 从PlayerPrefs加载音量设置
    /// </summary>
    private void LoadVolumeSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.7f);
        
        // 应用音量设置
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"AudioManager: 已加载音量设置 - BGM: {bgmVolume:F2}, SFX: {sfxVolume:F2}");
        }
    }
    
    /// <summary>
    /// 淡入背景音乐
    /// </summary>
    private IEnumerator FadeInBGM(AudioClip bgmClip)
    {
        bgmSource.clip = bgmClip;
        bgmSource.volume = 0f;
        bgmSource.Play();
        
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t);
            yield return null;
        }
        
        bgmSource.volume = bgmVolume;
        
        if (showDebugLog)
        {
            Debug.Log($"AudioManager: 背景音乐 {bgmClip.name} 淡入完成");
        }
    }
    
    /// <summary>
    /// 淡出背景音乐
    /// </summary>
    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        bgmSource.Stop();
        bgmSource.volume = bgmVolume; // 恢复音量设置
        currentBGM = null;
        
        if (showDebugLog)
        {
            Debug.Log("AudioManager: 背景音乐淡出完成");
        }
    }
    
    /// <summary>
    /// 淡出当前音乐并播放新音乐
    /// </summary>
    private IEnumerator FadeOutAndPlayBGM(AudioClip newBGM)
    {
        // 先淡出当前音乐
        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        // 切换音乐并淡入
        bgmSource.clip = newBGM;
        bgmSource.volume = 0f;
        bgmSource.Play();
        
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t);
            yield return null;
        }
        
        bgmSource.volume = bgmVolume;
        
        if (showDebugLog)
        {
            Debug.Log($"AudioManager: 背景音乐已切换为 {newBGM.name}");
        }
    }
    
}

