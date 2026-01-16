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
    
    [Header("小红点设置")]
    [Tooltip("小红点的尺寸（相对于按钮尺寸的比例，例如0.2表示按钮宽度的20%）")]
    [SerializeField] private float redDotSizeRatio = 0.2f;
    [Tooltip("小红点的颜色")]
    [SerializeField] private Color redDotColor = Color.red;
    [Tooltip("小红点距离右上角的偏移量（相对于按钮尺寸的比例）")]
    [SerializeField] private Vector2 redDotOffsetRatio = new Vector2(0.05f, 0.05f);
    
    [Header("音效")]
    [Tooltip("音频源组件（如果为空，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("邮件按钮点击音效")]
    [SerializeField] private AudioClip mailButtonClickSound;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // PlayerPrefs键名
    private const string MAIL_SHOWN_IDS_KEY = "MailShownIds"; // 已解锁且可以被添加到邮箱的邮件
    private const string MAIL_UNLOCKED_PENDING_KEY = "MailUnlockedPending"; // 已解锁但还不能添加进邮箱的邮件（暂存）
    private const string MAIL_CLICKED_IDS_KEY = "MailClickedIds"; // 已被点击过的邮件ID列表
    
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
        
        // 检查邮件是否被点击过，如果没被点击过，创建小红点
        if (!IsMailClicked(mailInfo.mailId))
        {
            CreateRedDot(buttonObj, rectTransform);
        }
        
        // 添加到列表
        mailButtonObjects.Add(buttonObj);
    }
    
    /// <summary>
    /// 创建小红点（在按钮右上角）
    /// </summary>
    /// <param name="buttonObj">按钮GameObject</param>
    /// <param name="buttonRectTransform">按钮的RectTransform</param>
    private void CreateRedDot(GameObject buttonObj, RectTransform buttonRectTransform)
    {
        if (buttonObj == null || buttonRectTransform == null)
        {
            return;
        }
        
        // 创建小红点GameObject
        GameObject redDotObj = new GameObject("RedDot");
        redDotObj.transform.SetParent(buttonObj.transform, false);
        redDotObj.SetActive(true);
        
        // 添加RectTransform
        RectTransform redDotRect = redDotObj.AddComponent<RectTransform>();
        
        // 设置锚点到右上角
        redDotRect.anchorMin = new Vector2(1f, 1f);
        redDotRect.anchorMax = new Vector2(1f, 1f);
        redDotRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 计算小红点尺寸（基于按钮尺寸）
        float buttonWidth = buttonRectTransform.rect.width;
        float buttonHeight = buttonRectTransform.rect.height;
        float dotSize = Mathf.Min(buttonWidth, buttonHeight) * redDotSizeRatio;
        
        // 设置尺寸
        redDotRect.sizeDelta = new Vector2(dotSize, dotSize);
        
        // 设置位置（右上角，带偏移）
        float offsetX = buttonWidth * redDotOffsetRatio.x;
        float offsetY = buttonHeight * redDotOffsetRatio.y;
        redDotRect.anchoredPosition = new Vector2(-offsetX, -offsetY);
        
        // 添加Image组件并设置为红色圆形
        Image redDotImage = redDotObj.AddComponent<Image>();
        redDotImage.color = redDotColor;
        
        // 创建圆形Sprite
        Sprite circleSprite = CreateCircleSprite((int)dotSize);
        if (circleSprite != null)
        {
            redDotImage.sprite = circleSprite;
        }
        
        redDotImage.type = Image.Type.Simple;
        redDotImage.preserveAspect = true;
        
        // 设置层级，确保小红点在按钮上方
        redDotObj.transform.SetAsLastSibling();
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 为邮件按钮 mailId={buttonObj.name} 创建小红点");
        }
    }
    
    /// <summary>
    /// 创建圆形Sprite
    /// </summary>
    /// <param name="size">圆形尺寸（像素）</param>
    /// <returns>圆形Sprite</returns>
    private Sprite CreateCircleSprite(int size)
    {
        if (size <= 0)
        {
            size = 32; // 默认尺寸
        }
        
        // 创建纹理
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        // 计算圆心和半径
        float centerX = size / 2f;
        float centerY = size / 2f;
        float radius = size / 2f - 1f; // 留1像素边距，避免锯齿
        
        // 填充圆形
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (distance <= radius)
                {
                    pixels[y * size + x] = Color.white; // 使用白色，通过Image的color属性控制最终颜色
                }
                else
                {
                    pixels[y * size + x] = Color.clear; // 透明
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // 创建Sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        
        return sprite;
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
        
        // 标记邮件为已点击
        MarkMailAsClicked(mailId);
        
        // 移除该邮件按钮上的小红点
        RemoveRedDotFromButton(mailId);
        
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
    /// 检查邮件是否被点击过
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    /// <returns>是否被点击过</returns>
    private bool IsMailClicked(int mailId)
    {
        List<int> clickedMailIds = GetClickedMailIds();
        return clickedMailIds.Contains(mailId);
    }
    
    /// <summary>
    /// 标记邮件为已点击
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    private void MarkMailAsClicked(int mailId)
    {
        List<int> clickedMailIds = GetClickedMailIds();
        
        if (!clickedMailIds.Contains(mailId))
        {
            clickedMailIds.Add(mailId);
            SaveClickedMailIds(clickedMailIds);
            
            if (enableDebugLog)
            {
                Debug.Log($"MailPanel: 标记邮件 mailId={mailId} 为已点击");
            }
        }
    }
    
    /// <summary>
    /// 获取已点击的邮件ID列表
    /// </summary>
    /// <returns>已点击的邮件ID列表</returns>
    private List<int> GetClickedMailIds()
    {
        string json = PlayerPrefs.GetString(MAIL_CLICKED_IDS_KEY, "");
        List<int> clickedMailIds = new List<int>();
        
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                SerializableList<int> serializableList = JsonUtility.FromJson<SerializableList<int>>(json);
                if (serializableList != null && serializableList.list != null)
                {
                    clickedMailIds = serializableList.list;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MailPanel: 解析已点击邮件数据失败: {e.Message}");
            }
        }
        
        return clickedMailIds;
    }
    
    /// <summary>
    /// 保存已点击的邮件ID列表到PlayerPrefs
    /// </summary>
    /// <param name="clickedMailIds">已点击的邮件ID列表</param>
    private void SaveClickedMailIds(List<int> clickedMailIds)
    {
        try
        {
            SerializableList<int> serializableList = new SerializableList<int>(clickedMailIds);
            string json = JsonUtility.ToJson(serializableList);
            PlayerPrefs.SetString(MAIL_CLICKED_IDS_KEY, json);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MailPanel: 保存已点击邮件数据失败！错误：{e.Message}");
        }
    }
    
    /// <summary>
    /// 移除指定邮件按钮上的小红点
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    private void RemoveRedDotFromButton(int mailId)
    {
        foreach (var buttonObj in mailButtonObjects)
        {
            if (buttonObj != null)
            {
                MailButtonItem buttonItem = buttonObj.GetComponent<MailButtonItem>();
                if (buttonItem != null && buttonItem.GetMailId() == mailId)
                {
                    // 查找小红点子对象
                    Transform redDotTransform = buttonObj.transform.Find("RedDot");
                    if (redDotTransform != null)
                    {
                        Destroy(redDotTransform.gameObject);
                        
                        if (enableDebugLog)
                        {
                            Debug.Log($"MailPanel: 已移除邮件 mailId={mailId} 按钮上的小红点");
                        }
                    }
                    break;
                }
            }
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
                
                // 确保ID为22的邮件始终在列表中（如果配置中存在）
                EnsureMail22Exists();
                
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
    /// 确保ID为22的邮件始终在列表中（如果配置中存在且不在列表中，则添加到列表末尾）
    /// </summary>
    private void EnsureMail22Exists()
    {
        if (mailDataConfig == null || mailDataConfig.mailDataList == null)
        {
            return;
        }
        
        // 检查ID为22的邮件是否存在
        var mail22 = mailDataConfig.GetMailInfoById(22);
        if (mail22 == null)
        {
            // 如果配置中不存在ID为22的邮件，不需要添加
            return;
        }
        
        // 如果ID为22的邮件不在列表中，添加到列表末尾（保持原有邮件顺序，新邮件在顶部）
        if (!shownMailIds.Contains(22))
        {
            shownMailIds.Add(22);
            SaveShownMailIds();
            
            if (enableDebugLog)
            {
                Debug.Log("MailPanel: 检测到ID为22的邮件不在列表中，已自动添加到列表末尾");
            }
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
    /// 重置邮箱到初始状态（清空PlayerPrefs并清空所有邮件）
    /// </summary>
    [ContextMenu("重置邮箱数据")]
    public void ResetMailToInitialState()
    {
        if (enableDebugLog)
        {
            Debug.Log("MailPanel: 重置邮箱到初始状态");
        }
        
        // 删除PlayerPrefs中的数据（正式列表、暂存列表和已点击记录）
        PlayerPrefs.DeleteKey(MAIL_SHOWN_IDS_KEY);
        PlayerPrefs.DeleteKey(MAIL_UNLOCKED_PENDING_KEY);
        PlayerPrefs.DeleteKey(MAIL_CLICKED_IDS_KEY);
        PlayerPrefs.Save();
        
        // 清空当前显示的邮件ID列表
        shownMailIds.Clear();
        
        // 如果面板已打开，清空按钮列表（不创建新按钮）
        if (gameObject.activeSelf)
        {
            ClearAllButtons();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 邮箱已重置，已清空所有邮件数据（包括正式列表和暂存列表）");
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
    /// 将邮件添加到暂存列表（已解锁但还不能添加进邮箱）
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    /// <returns>是否成功添加</returns>
    public bool AddMailToPending(int mailId)
    {
        // 检查是否已经在正式列表中
        if (shownMailIds.Contains(mailId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailPanel: 邮件 mailId={mailId} 已在正式列表中，跳过添加到暂存");
            }
            return false;
        }
        
        // 从PlayerPrefs加载暂存列表
        List<int> pendingMailIds = GetPendingMailIds();
        
        // 检查是否已在暂存列表中
        if (pendingMailIds.Contains(mailId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailPanel: 邮件 mailId={mailId} 已在暂存列表中，跳过重复添加");
            }
            return false;
        }
        
        // 添加到暂存列表
        pendingMailIds.Add(mailId);
        SavePendingMailIds(pendingMailIds);
        
        if (enableDebugLog)
        {
            Debug.Log($"MailPanel: 已将邮件 mailId={mailId} 添加到暂存列表");
        }
        
        return true;
    }
    
    /// <summary>
    /// 从暂存列表移除邮件并添加到正式列表
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    /// <returns>是否成功移动</returns>
    public bool MoveMailFromPendingToShown(int mailId)
    {
        // 从PlayerPrefs加载暂存列表
        List<int> pendingMailIds = GetPendingMailIds();
        
        // 检查是否在暂存列表中
        if (!pendingMailIds.Contains(mailId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailPanel: 邮件 mailId={mailId} 不在暂存列表中，无法移动");
            }
            return false;
        }
        
        // 从暂存列表移除
        pendingMailIds.Remove(mailId);
        SavePendingMailIds(pendingMailIds);
        
        // 添加到正式列表（如果尚未存在）
        if (!shownMailIds.Contains(mailId))
        {
            // 确保ID为22的邮件在列表中（如果配置中存在）
            EnsureMail22Exists();
            
            // 将新邮件插入到列表最前面（索引0）
            shownMailIds.Insert(0, mailId);
            SaveShownMailIds();
            
            // 如果面板已打开，刷新按钮列表
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
            
            if (enableDebugLog)
            {
                Debug.Log($"MailPanel: 已将邮件 mailId={mailId} 从暂存列表移动到正式列表并添加到邮箱（列表顶部）");
            }
            
            return true;
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"MailPanel: 邮件 mailId={mailId} 已在正式列表中，仅从暂存列表移除");
            }
            return false;
        }
    }
    
    /// <summary>
    /// 获取暂存邮件ID列表
    /// </summary>
    /// <returns>暂存邮件ID列表</returns>
    public List<int> GetPendingMailIds()
    {
        string json = PlayerPrefs.GetString(MAIL_UNLOCKED_PENDING_KEY, "");
        List<int> pendingMailIds = new List<int>();
        
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                SerializableList<int> serializableList = JsonUtility.FromJson<SerializableList<int>>(json);
                if (serializableList != null && serializableList.list != null)
                {
                    pendingMailIds = serializableList.list;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MailPanel: 解析暂存邮件数据失败: {e.Message}");
            }
        }
        
        return pendingMailIds;
    }
    
    /// <summary>
    /// 保存暂存邮件ID列表到PlayerPrefs
    /// </summary>
    /// <param name="pendingMailIds">暂存邮件ID列表</param>
    private void SavePendingMailIds(List<int> pendingMailIds)
    {
        try
        {
            SerializableList<int> serializableList = new SerializableList<int>(pendingMailIds);
            string json = JsonUtility.ToJson(serializableList);
            PlayerPrefs.SetString(MAIL_UNLOCKED_PENDING_KEY, json);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MailPanel: 保存暂存邮件数据失败！错误：{e.Message}");
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
