using UnityEngine;
using System.Collections;

/// <summary>
/// 相机BGM控制器
/// 根据相机的激活状态自动播放/停止背景音乐
/// 解决Additive模式加载场景时多个场景BGM同时播放的问题
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraBGM : MonoBehaviour
{
    [Header("BGM设置")]
    [Tooltip("背景音乐AudioClip（如果为空则不播放）")]
    [SerializeField] private AudioClip bgmClip;
    
    [Tooltip("AudioSource组件（如果为空会自动创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("启用时自动播放BGM")]
    [SerializeField] private bool autoPlayOnEnable = true;
    
    [Tooltip("循环播放")]
    [SerializeField] private bool loop = true;
    
    [Tooltip("音量（0-1）")]
    [SerializeField] private float volume = 1f;
    
    [Header("淡入淡出设置（可选）")]
    [Tooltip("是否启用淡入淡出效果")]
    [SerializeField] private bool useFade = false;
    
    [Tooltip("淡入淡出时间（秒）")]
    [SerializeField] private float fadeTime = 1f;
    
    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = false;
    
    // 私有变量
    private Camera cameraComponent;
    private bool wasCameraActive = false;
    private Coroutine fadeCoroutine;
    
    void Awake()
    {
        // 获取Camera组件
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError($"CameraBGM: {gameObject.name} 上未找到Camera组件！");
            return;
        }
        
        // 初始化AudioSource
        InitializeAudioSource();
        
        // 记录初始相机状态
        wasCameraActive = IsCameraActive();
    }
    
    void OnEnable()
    {
        // 延迟一帧检查，确保相机状态已更新
        StartCoroutine(CheckCameraStateDelayed());
    }
    
    void OnDisable()
    {
        // 停止BGM
        StopBGM();
    }
    
    void Update()
    {
        // 持续检测相机状态变化
        CheckCameraStateChange();
    }
    
    /// <summary>
    /// 延迟检查相机状态（确保OnEnable时相机状态已正确设置）
    /// </summary>
    private IEnumerator CheckCameraStateDelayed()
    {
        yield return null; // 等待一帧
        
        if (IsCameraActive())
        {
            wasCameraActive = true;
            if (autoPlayOnEnable && bgmClip != null)
            {
                PlayBGM();
            }
        }
    }
    
    /// <summary>
    /// 检查相机状态是否变化
    /// </summary>
    private void CheckCameraStateChange()
    {
        bool isActive = IsCameraActive();
        
        // 如果状态发生变化
        if (isActive != wasCameraActive)
        {
            wasCameraActive = isActive;
            
            if (isActive)
            {
                // 相机激活：播放BGM
                if (bgmClip != null && autoPlayOnEnable)
                {
                    PlayBGM();
                }
                
                if (showDebugLog)
                {
                    Debug.Log($"CameraBGM: {gameObject.name} 相机已激活，开始播放BGM: {bgmClip?.name ?? "None"}");
                }
            }
            else
            {
                // 相机禁用：停止BGM
                StopBGM();
                
                if (showDebugLog)
                {
                    Debug.Log($"CameraBGM: {gameObject.name} 相机已禁用，停止BGM");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查相机是否激活
    /// </summary>
    private bool IsCameraActive()
    {
        if (cameraComponent == null)
        {
            return false;
        }
        
        // 相机必须同时满足：GameObject激活、Camera组件启用
        return gameObject.activeInHierarchy && cameraComponent.enabled;
    }
    
    /// <summary>
    /// 初始化AudioSource组件
    /// </summary>
    private void InitializeAudioSource()
    {
        // 如果AudioSource为空，尝试获取或创建
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            
            // 如果还是没有，创建一个新的
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                
                if (showDebugLog)
                {
                    Debug.Log($"CameraBGM: {gameObject.name} 已自动创建AudioSource组件");
                }
            }
        }
        
        // 配置AudioSource
        if (audioSource != null)
        {
            audioSource.playOnAwake = false; // 不自动播放，由脚本控制
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.priority = 128; // 中等优先级
        }
    }
    
    /// <summary>
    /// 播放BGM
    /// </summary>
    public void PlayBGM()
    {
        if (bgmClip == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"CameraBGM: {gameObject.name} BGM Clip为空，无法播放");
            }
            return;
        }
        
        if (audioSource == null)
        {
            InitializeAudioSource();
        }
        
        // 如果相机未激活，不播放
        if (!IsCameraActive())
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"CameraBGM: {gameObject.name} 相机未激活，不播放BGM");
            }
            return;
        }
        
        // 如果正在播放相同的BGM，跳过
        if (audioSource.isPlaying && audioSource.clip == bgmClip)
        {
            if (showDebugLog)
            {
                Debug.Log($"CameraBGM: {gameObject.name} BGM {bgmClip.name} 已在播放中，跳过");
            }
            return;
        }
        
        // 停止淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 设置BGM
        audioSource.clip = bgmClip;
        audioSource.loop = loop;
        audioSource.volume = volume;
        
        // 播放BGM
        if (useFade)
        {
            fadeCoroutine = StartCoroutine(FadeInBGM());
        }
        else
        {
            audioSource.Play();
            
            if (showDebugLog)
            {
                Debug.Log($"CameraBGM: {gameObject.name} 开始播放BGM: {bgmClip.name}");
            }
        }
    }
    
    /// <summary>
    /// 停止BGM
    /// </summary>
    public void StopBGM()
    {
        if (audioSource == null || !audioSource.isPlaying)
        {
            return;
        }
        
        // 停止淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // 停止BGM
        if (useFade)
        {
            fadeCoroutine = StartCoroutine(FadeOutBGM());
        }
        else
        {
            audioSource.Stop();
            
            if (showDebugLog)
            {
                Debug.Log($"CameraBGM: {gameObject.name} 已停止BGM");
            }
        }
    }
    
    /// <summary>
    /// 淡入BGM
    /// </summary>
    private IEnumerator FadeInBGM()
    {
        audioSource.volume = 0f;
        audioSource.Play();
        
        float elapsed = 0f;
        while (elapsed < fadeTime && audioSource.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            audioSource.volume = Mathf.Lerp(0f, volume, t);
            yield return null;
        }
        
        audioSource.volume = volume;
        
        if (showDebugLog)
        {
            Debug.Log($"CameraBGM: {gameObject.name} BGM淡入完成: {bgmClip.name}");
        }
    }
    
    /// <summary>
    /// 淡出BGM
    /// </summary>
    private IEnumerator FadeOutBGM()
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeTime && audioSource.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        audioSource.Stop();
        audioSource.volume = volume; // 恢复音量设置
        
        if (showDebugLog)
        {
            Debug.Log($"CameraBGM: {gameObject.name} BGM淡出完成");
        }
    }
    
    /// <summary>
    /// 设置BGM音量（公开方法，可供外部调用）
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// 获取当前BGM音量
    /// </summary>
    public float GetVolume()
    {
        return volume;
    }
}
