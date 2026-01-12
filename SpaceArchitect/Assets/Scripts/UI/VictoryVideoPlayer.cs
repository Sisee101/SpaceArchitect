using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 胜利视频播放器
/// 管理胜利结算视频的播放、显示和隐藏
/// </summary>
public class VictoryVideoPlayer : MonoBehaviour
{
    [Header("视频组件")]
    [SerializeField] private VideoPlayer videoPlayer;        // VideoPlayer组件
    [SerializeField] private RawImage videoDisplay;          // 显示视频的RawImage
    [SerializeField] private RenderTexture renderTexture;   // 渲染纹理（用于视频输出）
    
    [Header("UI面板")]
    [SerializeField] private GameObject videoPanel;          // 视频面板（包含RawImage的父对象）
    
    [Header("视频设置")]
    [Tooltip("视频播放完成后自动隐藏的延迟时间（秒）")]
    [SerializeField] private float hideDelayAfterVideo = 0.5f;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 事件：视频播放完成
    public event Action OnVideoFinished;
    
    // 事件：视频面板完全隐藏（视频消失）
    public event Action OnVideoHidden;
    
    // 当前是否正在播放
    private bool isPlaying = false;
    
    void Start()
    {
        // 验证引用
        if (videoPlayer == null)
        {
            Debug.LogError("VictoryVideoPlayer: videoPlayer未配置！");
            return;
        }
        
        if (videoDisplay == null)
        {
            Debug.LogError("VictoryVideoPlayer: videoDisplay未配置！");
            return;
        }
        
        if (renderTexture == null)
        {
            Debug.LogError("VictoryVideoPlayer: renderTexture未配置！");
            return;
        }
        
        if (videoPanel == null)
        {
            Debug.LogWarning("VictoryVideoPlayer: videoPanel未配置，将使用videoDisplay的父对象");
            if (videoDisplay != null)
            {
                videoPanel = videoDisplay.transform.parent?.gameObject;
            }
        }
        
        // 配置VideoPlayer
        SetupVideoPlayer();
        
        // 初始隐藏视频面板
        HideVideo();
    }
    
    /// <summary>
    /// 配置VideoPlayer组件
    /// </summary>
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;
        
        // 设置渲染目标为RenderTexture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        
        // 订阅视频播放完成事件
        videoPlayer.loopPointReached += OnVideoLoopPointReached;
        
        // 订阅视频准备完成事件
        videoPlayer.prepareCompleted += OnVideoPrepareCompleted;
    }
    
    /// <summary>
    /// 播放视频
    /// </summary>
    /// <param name="videoClip">要播放的视频剪辑</param>
    /// <param name="onFinished">播放完成回调</param>
    public void PlayVideo(VideoClip videoClip, Action onFinished = null)
    {
        if (videoClip == null)
        {
            Debug.LogWarning("VictoryVideoPlayer: videoClip为空，跳过播放");
            onFinished?.Invoke();
            return;
        }
        
        if (isPlaying)
        {
            Debug.LogWarning("VictoryVideoPlayer: 视频正在播放中，停止当前播放");
            StopVideo();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"VictoryVideoPlayer: 开始播放视频 {videoClip.name}");
        }
        
        // 设置视频剪辑
        videoPlayer.clip = videoClip;
        
        // 设置完成回调
        if (onFinished != null)
        {
            OnVideoFinished = onFinished;
        }
        
        // 显示视频面板
        ShowVideo();
        
        // 准备并播放视频
        videoPlayer.Prepare();
    }
    
    /// <summary>
    /// 视频准备完成回调
    /// </summary>
    private void OnVideoPrepareCompleted(VideoPlayer source)
    {
        if (enableDebugLog)
        {
            Debug.Log("VictoryVideoPlayer: 视频准备完成，开始播放");
        }
        
        // 开始播放
        isPlaying = true;
        videoPlayer.Play();
    }
    
    /// <summary>
    /// 视频播放完成回调（到达循环点）
    /// </summary>
    private void OnVideoLoopPointReached(VideoPlayer source)
    {
        if (enableDebugLog)
        {
            Debug.Log("VictoryVideoPlayer: 视频播放完成");
        }
        
        isPlaying = false;
        
        // 延迟后隐藏视频
        StartCoroutine(HideVideoAfterDelay());
        
        // 触发完成事件
        OnVideoFinished?.Invoke();
        OnVideoFinished = null; // 清空事件
    }
    
    /// <summary>
    /// 延迟后隐藏视频
    /// </summary>
    private IEnumerator HideVideoAfterDelay()
    {
        yield return new WaitForSeconds(hideDelayAfterVideo);
        HideVideo();
        
        // 触发视频隐藏完成事件
        OnVideoHidden?.Invoke();
        OnVideoHidden = null; // 清空事件
    }
    
    /// <summary>
    /// 停止视频播放
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        isPlaying = false;
        HideVideo();
    }
    
    /// <summary>
    /// 显示视频面板
    /// </summary>
    private void ShowVideo()
    {
        if (videoPanel != null)
        {
            videoPanel.SetActive(true);
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
        }
        
        // 确保RenderTexture已设置到RawImage
        if (videoDisplay != null && renderTexture != null)
        {
            videoDisplay.texture = renderTexture;
        }
    }
    
    /// <summary>
    /// 隐藏视频面板
    /// </summary>
    private void HideVideo()
    {
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }
    
    void OnDestroy()
    {
        // 清理事件订阅
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
            videoPlayer.prepareCompleted -= OnVideoPrepareCompleted;
        }
    }
}
