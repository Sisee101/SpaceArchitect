using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 邮箱面板控制器
/// 管理邮箱面板的显示和交互，包括按钮列表生成、M键事件响应、图片显示
/// </summary>
public class MailPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect scrollRect;              // 滚动视图
    [SerializeField] private RectTransform content;              // Content容器
    [SerializeField] private Image imageDisplay;                 // 右侧图片显示区域
    
    [Header("数据配置")]
    [SerializeField] private MailDataConfig mailDataConfig;      // 邮件数据配置
    
    [Header("布局参数")]
    [SerializeField] private float buttonSpacing = 10f;         // 按钮间距（已废弃，使用Content的VerticalLayoutGroup的Spacing）
    
    [Header("左侧按钮显示设置")]
    [Tooltip("是否使用自定义按钮尺寸（如果为true，使用下面的width和height；如果为false，使用buttonIcon的原始尺寸）")]
    [SerializeField] private bool useCustomButtonSize = true;
    [Tooltip("左侧按钮的显示宽度（如果useCustomButtonSize为true）")]
    [SerializeField] private float buttonDisplayWidth = 250f;
    [Tooltip("左侧按钮的显示高度（如果useCustomButtonSize为true）")]
    [SerializeField] private float buttonDisplayHeight = 100f;
    [Tooltip("是否保持按钮图片比例（如果为true，按钮会按比例缩放以适应width和height）")]
    [SerializeField] private bool preserveButtonAspect = true;
    
    [Header("动画参数")]
    [SerializeField] private float insertAnimationDuration = 0.3f;  // 插入动画时长
    [SerializeField] private bool enableInsertAnimation = true;    // 是否启用插入动画
    
    [Header("音效")]
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("邮件按钮点击音效")]
    [SerializeField] private AudioClip mailButtonClickSound;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // PlayerPrefs键名
    private const string MAIL_SHOWN_IDS_KEY = "MailShownIds";
    
    [Header("运行时数据（仅用于调试查看）")]
    [Tooltip("已显示的邮件ID列表（按显示顺序，最前面的是最新插入的）")]
    [SerializeField] private List<int> shownMailIds = new List<int>();
    
    [Tooltip("当前创建的按钮对象列表")]
    [SerializeField] private List<GameObject> mailButtonObjects = new List<GameObject>();
    
    [Tooltip("当前选中的按钮")]
    [SerializeField] private GameObject currentSelectedButton = null;
    
    void Start()
    {
        // 初始化音频源
        InitializeAudioSource();
        
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        // 加载已显示的邮件ID列表
        LoadShownMailIds();
        
        // 如果面板已激活，刷新按钮列表
        if (gameObject.activeSelf)
        {
            RefreshButtonList();
        }
    }
    
    void OnDestroy()
    {
        // 清理资源
    }
    
    /// <summary>
    /// 显示邮箱面板
    /// </summary>
    public void Show()
    {
        // 只设置激活状态，不重复刷新（Start已经刷新过了）
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            RefreshButtonList();
        }
    }
    
    /// <summary>
    /// 隐藏邮箱面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 插入新邮件按钮
    /// </summary>
    public void InsertNewMail()
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null || mailDataConfig.mailDataList.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MailPanel: mailDataConfig未配置或数据为空！");
            }
            return;
        }
        
        // 查找第一个未显示的邮件（按配置顺序）
        MailDataConfig.MailInfo newMail = null;
        foreach (var mailInfo in mailDataConfig.mailDataList)
        {
            if (mailInfo != null && !shownMailIds.Contains(mailInfo.mailId))
            {
                newMail = mailInfo;
                break;
            }
        }
        
        if (newMail == null)
        {
            if (enableDebugLog)
            {
                Debug.Log("MailPanel: 所有邮件都已显示，无法插入新邮件");
            }
            return;
        }
        
        // 防止重复添加（双重检查）
        if (shownMailIds.Contains(newMail.mailId))
        {
            Debug.LogWarning($"MailPanel: 邮件ID {newMail.mailId} 已存在，跳过添加");
            return;
        }
        
        // 将新邮件ID添加到列表最前面
        shownMailIds.Insert(0, newMail.mailId);
        
        // 保存到PlayerPrefs
        SaveShownMailIds();
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 插入新邮件 mailId={newMail.mailId}，当前已显示 {shownMailIds.Count} 个邮件");
        }
        
        // 如果面板已打开，立即创建UI并刷新显示
        if (gameObject.activeSelf)
        {
            RefreshButtonList();
            
            // 滚动到顶部显示新插入的按钮
            StartCoroutine(ScrollToTopAfterFrame());
            
            // 播放插入动画（可选）
            if (enableInsertAnimation && mailButtonObjects.Count > 0)
            {
                PlayInsertAnimation(mailButtonObjects[0]);
            }
        }
    }
    
    /// <summary>
    /// 按指定邮件ID插入新邮件（用于订单完成后解锁邮件）
    /// </summary>
    /// <param name="mailId">要插入的邮件ID</param>
    /// <returns>是否成功插入</returns>
    public bool InsertMailById(int mailId)
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MailPanel: mailDataConfig未配置！");
            }
            return false;
        }
        
        // 检查是否已经显示
        if (shownMailIds.Contains(mailId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailPanel: 邮件ID {mailId} 已存在，跳过添加");
            }
            return false;
        }
        
        // 查找指定ID的邮件
        var mailInfo = mailDataConfig.GetMailInfoById(mailId);
        if (mailInfo == null)
        {
            Debug.LogWarning($"MailPanel: 未找到mailId={mailId}的邮件数据！");
            return false;
        }
        
        // 将新邮件ID添加到列表最前面
        shownMailIds.Insert(0, mailId);
        
        // 保存到PlayerPrefs
        SaveShownMailIds();
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 插入指定邮件 mailId={mailId}，当前已显示 {shownMailIds.Count} 个邮件");
        }
        
        // 如果面板已打开，立即创建UI并刷新显示
        if (gameObject.activeSelf)
        {
            RefreshButtonList();
            
            // 滚动到顶部显示新插入的按钮
            StartCoroutine(ScrollToTopAfterFrame());
            
            // 播放插入动画（可选）
            if (enableInsertAnimation && mailButtonObjects.Count > 0)
            {
                PlayInsertAnimation(mailButtonObjects[0]);
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 刷新按钮列表（根据shownMailIds重建所有按钮）
    /// </summary>
    private void RefreshButtonList()
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            Debug.LogWarning("MailPanel: mailDataConfig未配置！");
            return;
        }
        
        // 清除现有按钮
        ClearAllButtons();
        
        // 根据shownMailIds创建按钮（按ID顺序，最前面的ID对应最上面的按钮）
        foreach (int mailId in shownMailIds)
        {
            var mailInfo = mailDataConfig.GetMailInfoById(mailId);
            if (mailInfo != null)
            {
                CreateMailButton(mailInfo);
            }
            else
            {
                Debug.LogWarning($"MailPanel: 未找到mailId={mailId}的邮件数据，跳过创建按钮");
            }
        }
    }
    
    /// <summary>
    /// 创建单个邮件按钮（直接创建Image GameObject，不需要预制体）
    /// </summary>
    private void CreateMailButton(MailDataConfig.MailInfo mailInfo)
    {
        if (content == null)
        {
            Debug.LogError("MailPanel: content未配置！");
            return;
        }
        
        if (mailInfo == null || mailInfo.buttonIcon == null)
        {
            Debug.LogWarning($"MailPanel: mailInfo或buttonIcon为空，跳过创建按钮");
            return;
        }
        
        // 创建新的GameObject
        GameObject buttonObj = new GameObject($"MailButton_{mailInfo.mailId}");
        buttonObj.transform.SetParent(content, false);
        buttonObj.SetActive(true);
        
        // 添加RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        
        // 设置RectTransform尺寸
        if (useCustomButtonSize)
        {
            // 使用自定义尺寸
            float targetWidth = buttonDisplayWidth;
            float targetHeight = buttonDisplayHeight;
            
            // 如果保持比例，根据buttonIcon的原始尺寸计算合适的尺寸
            if (preserveButtonAspect && mailInfo.buttonIcon != null && mailInfo.buttonIcon.texture != null)
            {
                float originalWidth = mailInfo.buttonIcon.texture.width;
                float originalHeight = mailInfo.buttonIcon.texture.height;
                float aspectRatio = originalWidth / originalHeight;
                
                // 计算合适的尺寸，确保图片完全显示在限制范围内
                float widthRatio = buttonDisplayWidth / originalWidth;
                float heightRatio = buttonDisplayHeight / originalHeight;
                float scale = Mathf.Min(widthRatio, heightRatio); // 选择较小的缩放比例
                
                targetWidth = originalWidth * scale;
                targetHeight = originalHeight * scale;
            }
            
            rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
        }
        else
        {
            // 使用buttonIcon的原始尺寸
            if (mailInfo.buttonIcon != null && mailInfo.buttonIcon.texture != null)
            {
                rectTransform.sizeDelta = new Vector2(mailInfo.buttonIcon.texture.width, mailInfo.buttonIcon.texture.height);
            }
            else
            {
                // 如果无法获取尺寸，使用默认值
                rectTransform.sizeDelta = new Vector2(100, 100);
            }
        }
        
        // 添加Image组件并设置buttonIcon
        Image iconImage = buttonObj.AddComponent<Image>();
        iconImage.sprite = mailInfo.buttonIcon;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true; // 保持图片比例
        iconImage.raycastTarget = true; // 必须为true，才能接收点击事件
        
        // 添加MailButtonItem组件并初始化
        MailButtonItem buttonItem = buttonObj.AddComponent<MailButtonItem>();
        buttonItem.Initialize(mailInfo.mailId, mailInfo.buttonIcon, mailInfo.contentImage, this);
        
        // 添加到列表
        mailButtonObjects.Add(buttonObj);
    }
    
    /// <summary>
    /// 清除所有按钮
    /// </summary>
    private void ClearAllButtons()
    {
        foreach (var buttonObj in mailButtonObjects)
        {
            if (buttonObj != null)
            {
                Destroy(buttonObj);
            }
        }
        mailButtonObjects.Clear();
        currentSelectedButton = null;
    }
    
    /// <summary>
    /// 处理邮件按钮点击
    /// </summary>
    public void OnMailButtonClicked(int mailId, Sprite contentImage)
    {
        if (imageDisplay == null)
        {
            Debug.LogWarning("MailPanel: imageDisplay未配置！");
            return;
        }
        
        // 更新右侧图片显示
        imageDisplay.sprite = contentImage;
        
        // 更新选中状态
        foreach (var buttonObj in mailButtonObjects)
        {
            if (buttonObj != null)
            {
                MailButtonItem buttonItem = buttonObj.GetComponent<MailButtonItem>();
                if (buttonItem != null)
                {
                    bool isSelected = buttonItem.GetMailId() == mailId;
                    buttonItem.SetSelected(isSelected);
                    
                    if (isSelected)
                    {
                        currentSelectedButton = buttonObj;
                    }
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 邮件按钮被点击，mailId={mailId}");
        }
    }
    
    /// <summary>
    /// 播放插入动画
    /// </summary>
    private void PlayInsertAnimation(GameObject buttonObj)
    {
        if (buttonObj == null) return;
        
        // 初始状态：缩放为0，透明度为0
        buttonObj.transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = buttonObj.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        
        // 动画：缩放和淡入
        buttonObj.transform.DOScale(Vector3.one, insertAnimationDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, insertAnimationDuration).SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 延迟一帧后滚动到顶部（确保布局已更新）
    /// </summary>
    private IEnumerator ScrollToTopAfterFrame()
    {
        yield return null; // 等待一帧，确保布局已更新
        Canvas.ForceUpdateCanvases();
        
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    /// <summary>
    /// 从PlayerPrefs加载已显示的邮件ID列表
    /// </summary>
    private void LoadShownMailIds()
    {
        string json = PlayerPrefs.GetString(MAIL_SHOWN_IDS_KEY, "");
        
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                shownMailIds = JsonUtility.FromJson<SerializableList<int>>(json).list;
                
                // 去重处理（防止PlayerPrefs中有重复数据）
                List<int> uniqueIds = new List<int>();
                foreach (int id in shownMailIds)
                {
                    if (!uniqueIds.Contains(id))
                    {
                        uniqueIds.Add(id);
                    }
                }
                
                if (uniqueIds.Count != shownMailIds.Count)
                {
                    Debug.LogWarning($"MailPanel: 检测到重复的邮件ID，已自动去重。原有 {shownMailIds.Count} 个，去重后 {uniqueIds.Count} 个");
                    shownMailIds = uniqueIds;
                    // 保存去重后的数据
                    SaveShownMailIds();
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"MailPanel: 从PlayerPrefs加载了 {shownMailIds.Count} 个已显示的邮件ID");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MailPanel: 加载PlayerPrefs数据失败，使用默认值。错误：{e.Message}");
                InitializeDefaultMailIds();
            }
        }
        else
        {
            // 首次运行，初始化邮件
            InitializeDefaultMailIds();
        }
    }
    
    /// <summary>
    /// 初始化默认邮件ID列表（只显示id为22的邮件）
    /// </summary>
    private void InitializeDefaultMailIds()
    {
        shownMailIds.Clear();
        
        if (mailDataConfig != null && mailDataConfig.mailDataList != null)
        {
            // 查找id为22的邮件
            var mail22 = mailDataConfig.GetMailInfoById(22);
            if (mail22 != null)
            {
                shownMailIds.Add(22);
                
                if (enableDebugLog)
                {
                    Debug.Log("MailPanel: 首次运行，初始化邮件ID=22");
                }
            }
            else
            {
                Debug.LogWarning("MailPanel: 未找到ID为22的邮件！");
            }
            
            // 保存到PlayerPrefs
            SaveShownMailIds();
        }
    }
    
    /// <summary>
    /// 重置邮箱到初始状态（清空PlayerPrefs并重新初始化）
    /// </summary>
    [ContextMenu("重置邮箱数据")]
    public void ResetMailToInitialState()
    {
        if (enableDebugLog)
        {
            Debug.Log("MailPanel: 重置邮箱到初始状态");
        }
        
        // 删除PlayerPrefs中的数据
        PlayerPrefs.DeleteKey(MAIL_SHOWN_IDS_KEY);
        PlayerPrefs.Save();
        
        // 清空当前显示的邮件ID列表
        shownMailIds.Clear();
        
        // 重新初始化（只显示id为22的邮件）
        InitializeDefaultMailIds();
        
        // 如果面板已打开，刷新按钮列表
        if (gameObject.activeSelf)
        {
            RefreshButtonList();
            
            // 滚动到顶部
            StartCoroutine(ScrollToTopAfterFrame());
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 邮箱已重置，当前显示 {shownMailIds.Count} 个初始邮件");
        }
    }
    
    /// <summary>
    /// 保存已显示的邮件ID列表到PlayerPrefs
    /// </summary>
    private void SaveShownMailIds()
    {
        try
        {
            SerializableList<int> serializableList = new SerializableList<int>(shownMailIds);
            string json = JsonUtility.ToJson(serializableList);
            PlayerPrefs.SetString(MAIL_SHOWN_IDS_KEY, json);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MailPanel: 保存PlayerPrefs数据失败！错误：{e.Message}");
        }
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                
                if (enableDebugLog)
                {
                    Debug.Log("MailPanel: 已自动创建 AudioSource 组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 播放邮件按钮点击音效（供 MailButtonItem 调用）
    /// </summary>
    public void PlayMailButtonClickSound()
    {
        if (mailButtonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(mailButtonClickSound);
            
            if (enableDebugLog)
            {
                Debug.Log("MailPanel: 播放邮件按钮点击音效");
            }
        }
    }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainHub();
        }
    }
    
    /// <summary>
    /// 可序列化的List包装类（用于JsonUtility）
    /// </summary>
    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> list;
        
        public SerializableList(List<T> list)
        {
            this.list = list;
        }
    }
}
