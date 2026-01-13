using System;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 胜利反馈显示接口
/// 统一管理胜利图片/视频的显示，实现解耦设计
/// </summary>
public interface IVictoryFeedbackDisplay
{
    /// <summary>
    /// 显示胜利图片和印章
    /// </summary>
    /// <param name="image">胜利图片（使用orderImage）</param>
    /// <param name="onComplete">显示完成回调（图片和印章都消失后调用）</param>
    void ShowImage(Sprite image, Action onComplete = null);
    
    /// <summary>
    /// 显示胜利视频（可选，用于向后兼容）
    /// </summary>
    /// <param name="videoClip">视频剪辑</param>
    /// <param name="onComplete">播放完成回调</param>
    void ShowVideo(VideoClip videoClip, Action onComplete = null);
    
    /// <summary>
    /// 立即隐藏
    /// </summary>
    void Hide();
    
    /// <summary>
    /// 图片/视频隐藏完成事件
    /// </summary>
    event Action OnHidden;
    
    /// <summary>
    /// 是否正在显示
    /// </summary>
    bool IsShowing { get; }
}
