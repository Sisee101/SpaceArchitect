using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 文字图片控制器
/// 管理右上文字图片的显示和A键切换
/// </summary>
public class TextImageController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image textImageDisplay;
    
    [Header("文字图片列表（8张图片）")]
    [Tooltip("8张文字图片，在Inspector中配置。当前使用占位图片")]
    [SerializeField] private List<Sprite> textImageList = new List<Sprite>();
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前显示的图片索引（0-7）
    private int currentImageIndex = 0;
    
    // 是否启用A键切换（只有在结算界面显示时才启用）
    private bool aKeyEnabled = false;
    
    void Start()
    {
        // 初始化：显示第一张图片
        if (textImageList != null && textImageList.Count > 0 && textImageDisplay != null)
        {
            if (textImageList[0] != null)
            {
                textImageDisplay.sprite = textImageList[0];
                currentImageIndex = 0;
            }
        }
        else
        {
            Debug.LogWarning("TextImageController: 文字图片列表为空或textImageDisplay未配置！");
        }
        
        // 初始禁用A键切换（结算界面未显示时）
        aKeyEnabled = false;
    }
    
    void Update()
    {
        // 只有在结算界面显示时才监听A键切换
        if (aKeyEnabled && Input.GetKeyDown(KeyCode.A))
        {
            NextTextImage();
        }
    }
    
    /// <summary>
    /// 设置A键是否启用（供OrderDetailPanel调用）
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetAKeyEnabled(bool enabled)
    {
        aKeyEnabled = enabled;
    }
    
    /// <summary>
    /// 切换到下一张文字图片（循环）
    /// </summary>
    private void NextTextImage()
    {
        if (textImageList == null || textImageList.Count == 0)
        {
            Debug.LogWarning("TextImageController: 文字图片列表为空！");
            return;
        }
        
        if (textImageDisplay == null)
        {
            Debug.LogWarning("TextImageController: textImageDisplay未配置！");
            return;
        }
        
        // 循环切换：索引+1，到达末尾后回到0
        currentImageIndex = (currentImageIndex + 1) % textImageList.Count;
        
        // 设置图片
        if (textImageList[currentImageIndex] != null)
        {
            textImageDisplay.sprite = textImageList[currentImageIndex];
            
            if (enableDebugLog)
            {
                Debug.Log($"TextImageController: 切换到第 {currentImageIndex + 1} 张图片（共 {textImageList.Count} 张）");
            }
        }
    }
    
    /// <summary>
    /// 设置指定索引的文字图片（供后续代码调用）
    /// </summary>
    /// <param name="index">图片索引（0-7）</param>
    public void SetTextImage(int index)
    {
        if (textImageList == null || textImageList.Count == 0)
        {
            Debug.LogWarning("TextImageController: 文字图片列表为空！");
            return;
        }
        
        if (index < 0 || index >= textImageList.Count)
        {
            Debug.LogWarning($"TextImageController: 索引 {index} 超出范围（0-{textImageList.Count - 1}）！");
            return;
        }
        
        if (textImageDisplay == null)
        {
            Debug.LogWarning("TextImageController: textImageDisplay未配置！");
            return;
        }
        
        currentImageIndex = index;
        
        if (textImageList[currentImageIndex] != null)
        {
            textImageDisplay.sprite = textImageList[currentImageIndex];
        }
    }
    
    /// <summary>
    /// 设置文字图片列表（供后续代码调用）
    /// </summary>
    /// <param name="images">文字图片列表（8张）</param>
    public void SetTextImages(List<Sprite> images)
    {
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning("TextImageController: 图片列表为空！");
            return;
        }
        
        textImageList = images;
        
        // 如果列表有图片，显示第一张
        if (textImageList.Count > 0)
        {
            SetTextImage(0);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"TextImageController: 已设置 {textImageList.Count} 张文字图片");
        }
    }
    
    /// <summary>
    /// 获取当前图片索引
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentImageIndex;
    }
}
