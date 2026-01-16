using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 导入面板控制器
/// 管理游戏导入内容的显示和切换（8张图片，每张图片上有按钮点击进入下一页）
/// </summary>
public class IntroductionPanel : MonoBehaviour
{
    [Header("导入图片列表")]
    [Tooltip("导入图片列表（按顺序排列，共8张）")]
    [SerializeField] private Sprite[] introductionImages = new Sprite[8];
    
    [Header("UI引用")]
    [Tooltip("图片显示组件（显示当前导入图片）")]
    [SerializeField] private Image imageDisplay;
    
    [Tooltip("图片上的按钮（点击切换到下一张，最后一张点击后完成导入）")]
    [SerializeField] private Button imageButton;
    
    [Header("动画参数")]
    [Tooltip("图片切换动画时长（秒）")]
    [SerializeField] private float imageSwitchDuration = 0.3f;
    
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("按钮点击音效")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 当前显示的图片索引
    private int currentImageIndex = 0;
    
    // 导入完成事件
    public System.Action OnIntroductionCompleted;
    
    void Awake()
    {
        // 自动获取Image组件（如果未配置）
        if (imageDisplay == null)
        {
            imageDisplay = GetComponentInChildren<Image>();
            if (imageDisplay == null)
            {
                Debug.LogError("IntroductionPanel: 未找到Image组件！请在Inspector中配置Image Display字段，或确保子对象中有Image组件。");
            }
        }
        
        // 自动获取Button组件（如果未配置）
        if (imageButton == null)
        {
            imageButton = GetComponentInChildren<Button>();
            if (imageButton == null)
            {
                Debug.LogError("IntroductionPanel: 未找到Button组件！请在Inspector中配置Image Button字段，或确保子对象中有Button组件。");
            }
        }
        
        // 自动获取或添加AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    void Start()
    {
        // 初始化按钮事件
        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(OnImageButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log("IntroductionPanel: 图片按钮事件已绑定");
            }
        }
        
        // 默认隐藏
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示导入面板（从第一张图片开始）
    /// </summary>
    public void Show()
    {
        if (introductionImages == null || introductionImages.Length == 0)
        {
            Debug.LogWarning("IntroductionPanel: 导入图片列表为空，无法显示导入面板！");
            return;
        }
        
        gameObject.SetActive(true);
        currentImageIndex = 0;
        ShowImage(0);
        
        if (enableDebugLog)
        {
            Debug.Log($"IntroductionPanel: 显示导入面板，共 {introductionImages.Length} 张图片");
        }
    }
    
    /// <summary>
    /// 隐藏导入面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        
        if (enableDebugLog)
        {
            Debug.Log("IntroductionPanel: 隐藏导入面板");
        }
    }
    
    /// <summary>
    /// 显示指定索引的图片
    /// </summary>
    private void ShowImage(int index)
    {
        if (introductionImages == null || index < 0 || index >= introductionImages.Length)
        {
            Debug.LogWarning($"IntroductionPanel: 无法显示图片，索引 {index} 超出范围（总数: {introductionImages?.Length ?? 0}）");
            return;
        }
        
        if (imageDisplay == null)
        {
            Debug.LogError("IntroductionPanel: Image Display 未配置！无法显示图片。");
            return;
        }
        
        if (introductionImages[index] == null)
        {
            Debug.LogWarning($"IntroductionPanel: 图片索引 {index} 的Sprite为空！");
            return;
        }
        
        currentImageIndex = index;
        
        // 使用淡入淡出动画切换图片
        if (imageSwitchDuration > 0)
        {
            imageDisplay.DOFade(0f, imageSwitchDuration * 0.5f).OnComplete(() =>
            {
                if (imageDisplay != null)
                {
                    imageDisplay.sprite = introductionImages[index];
                    imageDisplay.DOFade(1f, imageSwitchDuration * 0.5f);
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"IntroductionPanel: 已切换到图片 {index + 1}/{introductionImages.Length}, Sprite: {introductionImages[index].name}");
                    }
                }
            });
        }
        else
        {
            // 无动画，直接切换
            imageDisplay.sprite = introductionImages[index];
            Color color = imageDisplay.color;
            color.a = 1f;
            imageDisplay.color = color;
            
            if (enableDebugLog)
            {
                Debug.Log($"IntroductionPanel: 已切换到图片 {index + 1}/{introductionImages.Length}, Sprite: {introductionImages[index].name}");
            }
        }
    }
    
    /// <summary>
    /// 图片按钮点击事件（切换到下一张或完成导入）
    /// </summary>
    private void OnImageButtonClicked()
    {
        // 播放点击音效
        PlayButtonClickSound();
        
        if (introductionImages == null || introductionImages.Length == 0)
        {
            Debug.LogWarning("IntroductionPanel: 图片列表为空，无法切换");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"IntroductionPanel: 图片按钮被点击，当前索引: {currentImageIndex}/{introductionImages.Length - 1}");
        }
        
        // 检查是否是最后一张图片
        if (currentImageIndex < introductionImages.Length - 1)
        {
            // 切换到下一张图片
            ShowImage(currentImageIndex + 1);
        }
        else
        {
            // 最后一张图片，完成导入
            if (enableDebugLog)
            {
                Debug.Log("IntroductionPanel: 已显示最后一张图片，完成导入");
            }
            
            // 触发完成事件
            OnIntroductionCompleted?.Invoke();
            
            // 隐藏面板
            Hide();
        }
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    void OnDestroy()
    {
        // 清理DOTween动画
        if (imageDisplay != null)
        {
            imageDisplay.DOKill();
        }
    }
}
