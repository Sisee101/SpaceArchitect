using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 员工手册图片浏览控制器
/// 管理图片浏览功能：图片显示、左右箭头切换、返回按钮
/// </summary>
public class HandbookImageViewer : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("图片显示区域（可以直接拖入GameObject，会自动获取Image组件）")]
    [SerializeField] private GameObject imageDisplayObject;  // 图片显示GameObject（备选方案）
    [Tooltip("图片显示组件（如果直接拖入Image组件，使用此字段）")]
    [SerializeField] private Image currentImageDisplay;  // 当前显示的图片组件
    [SerializeField] private Button leftArrowButton;     // 左箭头按钮
    [SerializeField] private Button rightArrowButton;    // 右箭头按钮
    [SerializeField] private Button backButton;          // 返回按钮
    
    [Header("页码指示器（可选）")]
    [Tooltip("显示当前页码的文本组件（如：1/15）")]
    [SerializeField] private Text pageIndicator;
    
    [Header("动画参数")]
    [Tooltip("图片切换动画时长（秒）")]
    [SerializeField] private float imageSwitchDuration = 0.3f;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 当前状态
    private int currentSectionIndex = -1;  // 当前浏览的选项索引
    private int currentImageIndex = 0;     // 当前图片索引
    private List<Sprite> currentImageList = new List<Sprite>();  // 当前选项的图片列表
    
    // 返回按钮点击事件
    public System.Action OnBackClicked;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        // 自动获取Image组件（如果通过GameObject引用）
        if (currentImageDisplay == null && imageDisplayObject != null)
        {
            currentImageDisplay = imageDisplayObject.GetComponent<Image>();
            if (currentImageDisplay == null)
            {
                // 尝试从子对象获取
                currentImageDisplay = imageDisplayObject.GetComponentInChildren<Image>();
                if (currentImageDisplay == null)
                {
                    Debug.LogError($"HandbookImageViewer: {imageDisplayObject.name} 及其子对象都没有找到Image组件！\n" +
                        "请执行以下步骤：\n" +
                        "1. 选中 ImageDisplay GameObject\n" +
                        "2. 在 Inspector 中点击 'Add Component'\n" +
                        "3. 搜索并添加 'Image' 组件（UnityEngine.UI.Image）");
                }
                else
                {
                    if (enableDebugLog)
                    {
                        Debug.Log($"HandbookImageViewer: 从子对象获取到Image组件: {currentImageDisplay.gameObject.name}");
                    }
                }
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.Log($"HandbookImageViewer: 自动获取到Image组件: {imageDisplayObject.name}");
                }
            }
        }
        
        // 如果两个字段都没有配置，输出错误
        if (currentImageDisplay == null)
        {
            Debug.LogError("HandbookImageViewer: Current Image Display 未配置！\n" +
                "请执行以下步骤之一：\n" +
                "方法1（推荐）：在 Inspector 中配置 'Image Display Object' 字段，将 ImageDisplay GameObject 拖入\n" +
                "方法2：在 Inspector 中配置 'Current Image Display' 字段，将 ImageDisplay GameObject 的 Image 组件拖入");
        }
    }
    
    void Start()
    {
        if (!isInitialized)
        {
            InitializeButtons();
            isInitialized = true;
        }
    }
    
    /// <summary>
    /// 初始化按钮事件
    /// </summary>
    private void InitializeButtons()
    {
        // 绑定左箭头按钮
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
            if (enableDebugLog)
            {
                Debug.Log($"HandbookImageViewer: 左箭头按钮已绑定: {leftArrowButton.name}");
            }
        }
        else
        {
            Debug.LogWarning("HandbookImageViewer: 左箭头按钮未配置！");
        }
        
        // 绑定右箭头按钮
        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(OnRightArrowClicked);
            if (enableDebugLog)
            {
                Debug.Log($"HandbookImageViewer: 右箭头按钮已绑定: {rightArrowButton.name}");
            }
        }
        else
        {
            Debug.LogWarning("HandbookImageViewer: 右箭头按钮未配置！");
        }
        
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
            if (enableDebugLog)
            {
                Debug.Log($"HandbookImageViewer: 返回按钮已绑定: {backButton.name}");
            }
        }
        else
        {
            Debug.LogWarning("HandbookImageViewer: 返回按钮未配置！");
        }
        
        if (enableDebugLog)
        {
            Debug.Log("HandbookImageViewer: 按钮事件已初始化");
        }
    }
    
    /// <summary>
    /// 显示指定选项的图片
    /// </summary>
    public void ShowSection(int sectionIndex, List<Sprite> images)
    {
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning($"HandbookImageViewer: 选项 {sectionIndex} 的图片列表为空");
            return;
        }
        
        currentSectionIndex = sectionIndex;
        currentImageList = images;
        currentImageIndex = 0;
        
        // 显示第一张图片
        ShowImage(0);
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookImageViewer: 显示选项 {sectionIndex}，共 {images.Count} 张图片");
        }
    }
    
    /// <summary>
    /// 显示指定索引的图片
    /// </summary>
    private void ShowImage(int index)
    {
        if (currentImageList == null || index < 0 || index >= currentImageList.Count)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"HandbookImageViewer: 无法显示图片，索引 {index} 超出范围（总数: {currentImageList?.Count ?? 0}）");
            }
            return;
        }
        
        currentImageIndex = index;
        
        // 检查Image组件是否已获取
        if (currentImageDisplay == null)
        {
            Debug.LogError("HandbookImageViewer: currentImageDisplay 为空！无法显示图片。");
            return;
        }
        
        // 检查图片是否为空
        if (currentImageList[index] == null)
        {
            Debug.LogWarning($"HandbookImageViewer: 图片索引 {index} 的Sprite为空！");
            return;
        }
        
        // 更新图片显示
        // 使用淡入淡出动画
        if (imageSwitchDuration > 0)
        {
            currentImageDisplay.DOFade(0f, imageSwitchDuration * 0.5f).OnComplete(() =>
            {
                if (currentImageDisplay != null)
                {
                    currentImageDisplay.sprite = currentImageList[index];
                    currentImageDisplay.DOFade(1f, imageSwitchDuration * 0.5f);
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"HandbookImageViewer: 已设置图片 Sprite: {currentImageList[index].name}");
                    }
                }
            });
        }
        else
        {
            currentImageDisplay.sprite = currentImageList[index];
            // 确保图片可见（设置Alpha为1）
            Color color = currentImageDisplay.color;
            color.a = 1f;
            currentImageDisplay.color = color;
            
            if (enableDebugLog)
            {
                Debug.Log($"HandbookImageViewer: 已设置图片 Sprite: {currentImageList[index].name}");
            }
        }
        
        // 更新箭头可见性
        UpdateArrowVisibility();
        
        // 更新页码指示器
        UpdatePageIndicator();
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookImageViewer: 显示图片 {index + 1}/{currentImageList.Count}, Sprite: {currentImageList[index].name}");
        }
    }
    
    /// <summary>
    /// 左箭头点击事件（上一张图片）
    /// </summary>
    private void OnLeftArrowClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log($"HandbookImageViewer: 左箭头被点击，当前索引: {currentImageIndex}, 总图片数: {currentImageList?.Count ?? 0}");
        }
        
        if (currentImageList == null || currentImageList.Count == 0)
        {
            Debug.LogWarning("HandbookImageViewer: 图片列表为空，无法切换");
            return;
        }
        
        if (currentImageIndex > 0)
        {
            ShowImage(currentImageIndex - 1);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("HandbookImageViewer: 已经是第一张图片，无法向左切换");
            }
        }
    }
    
    /// <summary>
    /// 右箭头点击事件（下一张图片）
    /// </summary>
    private void OnRightArrowClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log($"HandbookImageViewer: 右箭头被点击，当前索引: {currentImageIndex}, 总图片数: {currentImageList?.Count ?? 0}");
        }
        
        if (currentImageList == null || currentImageList.Count == 0)
        {
            Debug.LogWarning("HandbookImageViewer: 图片列表为空，无法切换");
            return;
        }
        
        if (currentImageIndex < currentImageList.Count - 1)
        {
            ShowImage(currentImageIndex + 1);
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("HandbookImageViewer: 已经是最后一张图片，无法向右切换");
            }
        }
    }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (enableDebugLog)
        {
            Debug.Log("HandbookImageViewer: 返回按钮被点击");
        }
        
        OnBackClicked?.Invoke();
    }
    
    /// <summary>
    /// 根据当前位置更新箭头可见性
    /// </summary>
    private void UpdateArrowVisibility()
    {
        if (currentImageList == null || currentImageList.Count == 0)
        {
            // 没有图片时，隐藏两个箭头
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(false);
            }
            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(false);
            }
            return;
        }
        
        // 只有一张图片时，隐藏两个箭头
        if (currentImageList.Count == 1)
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(false);
            }
            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(false);
            }
            return;
        }
        
        // 第一张图片（索引0）：只显示右箭头，隐藏左箭头
        if (currentImageIndex == 0)
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(false);
            }
            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(true);
            }
        }
        // 最后一张图片（索引为最后）：只显示左箭头，隐藏右箭头
        else if (currentImageIndex >= currentImageList.Count - 1)
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(true);
            }
            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(false);
            }
        }
        // 中间图片：显示两个箭头
        else
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(true);
            }
            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(true);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"HandbookImageViewer: 更新箭头可见性 - 当前索引: {currentImageIndex}/{currentImageList.Count - 1}, " +
                $"左箭头: {(leftArrowButton != null && leftArrowButton.gameObject.activeSelf ? "显示" : "隐藏")}, " +
                $"右箭头: {(rightArrowButton != null && rightArrowButton.gameObject.activeSelf ? "显示" : "隐藏")}");
        }
    }
    
    /// <summary>
    /// 更新页码指示器
    /// </summary>
    private void UpdatePageIndicator()
    {
        if (pageIndicator != null && currentImageList != null && currentImageList.Count > 0)
        {
            pageIndicator.text = $"{currentImageIndex + 1}/{currentImageList.Count}";
        }
    }
    
    /// <summary>
    /// 显示图片浏览视图
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏图片浏览视图
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // 清理DOTween动画
        if (currentImageDisplay != null)
        {
            currentImageDisplay.DOKill();
        }
    }
}
