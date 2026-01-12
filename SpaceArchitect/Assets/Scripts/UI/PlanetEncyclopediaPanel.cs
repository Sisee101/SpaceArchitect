using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 行星图鉴面板控制器
/// 处理行星图鉴的显示和交互，包括轮播切换和介绍卡片插入
/// </summary>
public class PlanetEncyclopediaPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect scrollRect;              // 滚动视图
    [SerializeField] private RectTransform content;              // Content容器
    [SerializeField] private Button leftArrow;                   // 左箭头
    [SerializeField] private Button rightArrow;                  // 右箭头
    
    [Header("预制体")]
    [SerializeField] private GameObject planetCardPrefab;        // 行星卡片预制体
    [SerializeField] private GameObject detailCardPrefab;        // 介绍卡片预制体
    
    [Header("数据配置")]
    [SerializeField] private List<Sprite> planetCardImages = new List<Sprite>();    // 13张行星卡片图片
    [SerializeField] private List<Sprite> detailCardImages = new List<Sprite>();    // 13张介绍卡片图片（索引对应）
    
    [Header("布局参数")]
    [SerializeField] private float cardSpacing = 20f;            // 卡片间距
    [SerializeField] private float planetCardWidth = 300f;       // 行星卡片宽度
    [SerializeField] private float planetCardHeight = 500f;      // 行星卡片高度
    [SerializeField] private float detailCardWidth = 600f;       // 介绍卡片宽度
    [SerializeField] private float detailCardHeight = 700f;      // 介绍卡片高度
    
    [Header("滚动位置调整")]
    [Tooltip("滚动到卡片中心时的水平偏移量（像素）。正值向右偏移，负值向左偏移")]
    [SerializeField] private float scrollCenterOffset = 0f;       // 滚动中心偏移量
    
    [Tooltip("点击左右箭头时，每次滚动的距离（像素）。如果为0，则自动使用（行星卡片宽度 + 卡片间距）")]
    [SerializeField] private float arrowScrollDistance = 0f;     // 箭头滚动距离（0表示自动计算）
    
    [Header("动画参数")]
    [SerializeField] private float scrollDuration = 0.3f;        // 滚动动画时长
    [SerializeField] private float insertAnimationDuration = 0.3f;  // 插入动画时长
    
    private List<PlanetCard> planetCards = new List<PlanetCard>();  // 所有行星卡片列表
    private DetailCard currentDetailCard = null;                     // 当前显示的介绍卡片
    private PlanetCard currentSelectedCard = null;                   // 当前选中的行星卡片
    private int totalPlanetCount = 13;                               // 行星总数
    
    void Start()
    {
        // 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        // 绑定箭头按钮
        if (leftArrow != null)
        {
            leftArrow.onClick.AddListener(OnLeftArrowClicked);
        }
        else
        {
            Debug.LogError("PlanetEncyclopediaPanel: leftArrow未配置！");
        }
        
        if (rightArrow != null)
        {
            rightArrow.onClick.AddListener(OnRightArrowClicked);
        }
        else
        {
            Debug.LogError("PlanetEncyclopediaPanel: rightArrow未配置！");
        }
        
        // 不在Start中初始化卡片，而是在Show()中初始化（确保面板显示时才创建）
    }
    
    void Update()
    {
        // 处理鼠标滚轮
        HandleMouseWheel();
    }
    
    /// <summary>
    /// 初始化所有卡片
    /// </summary>
    private void InitializeCards()
    {
        if (planetCardPrefab == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: planetCardPrefab未配置！");
            return;
        }
        
        if (content == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: content未配置！");
            return;
        }
        
        // 检查图片列表
        if (planetCardImages == null || planetCardImages.Count == 0)
        {
            Debug.LogError("PlanetEncyclopediaPanel: planetCardImages列表为空或未配置！请配置13张行星卡片图片。");
            return;
        }
        
        // 确保有足够的图片
        int imageCount = Mathf.Min(planetCardImages.Count, totalPlanetCount);
        Debug.Log($"PlanetEncyclopediaPanel: 开始初始化 {imageCount} 张卡片");
        
        // 创建所有行星卡片
        for (int i = 0; i < imageCount; i++)
        {
            CreatePlanetCard(i);
        }
        
        Debug.Log($"PlanetEncyclopediaPanel: 成功创建 {planetCards.Count} 张卡片");
        
        // 强制刷新布局（让Horizontal Layout Group重新计算位置）
        if (content != null)
        {
            // 立即刷新布局
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Debug.Log($"PlanetEncyclopediaPanel: Content当前宽度: {content.rect.width}, 高度: {content.rect.height}");
        }
    }
    
    /// <summary>
    /// 创建单个行星卡片
    /// </summary>
    private void CreatePlanetCard(int index)
    {
        if (index < 0 || index >= planetCardImages.Count)
        {
            Debug.LogWarning($"PlanetEncyclopediaPanel: 索引 {index} 超出图片列表范围！");
            return;
        }
        
        // 实例化预制体到Content下
        if (content == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: content引用为空，无法创建卡片！");
            return;
        }
        
        // 确保实例化到Content下（content应该是Content GameObject的RectTransform）
        GameObject cardObj = Instantiate(planetCardPrefab, content.transform);
        
        // 验证父对象是否正确
        if (cardObj.transform.parent != content.transform)
        {
            Debug.LogWarning($"PlanetEncyclopediaPanel: 卡片实例化位置不正确！父对象: {cardObj.transform.parent?.name}, 期望: {content.name}");
            // 强制设置父对象
            cardObj.transform.SetParent(content.transform, false);
        }
        
        PlanetCard planetCard = cardObj.GetComponent<PlanetCard>();
        
        if (planetCard == null)
        {
            Debug.LogError($"PlanetEncyclopediaPanel: 预制体 {planetCardPrefab.name} 没有 PlanetCard 脚本组件！");
            Destroy(cardObj);
            return;
        }
        
        // 初始化卡片
        Sprite cardSprite = planetCardImages[index];
        if (cardSprite == null)
        {
            Debug.LogWarning($"PlanetEncyclopediaPanel: 索引 {index} 的图片为空！");
        }
        planetCard.Initialize(cardSprite, index);
        
        // 订阅点击事件
        planetCard.OnCardClicked += OnCardClicked;
        
        // 添加到列表
        planetCards.Add(planetCard);
        
        Debug.Log($"PlanetEncyclopediaPanel: 创建卡片 {index}, 图片: {(cardSprite != null ? cardSprite.name : "null")}");
    }
    
    /// <summary>
    /// 处理行星卡片点击
    /// </summary>
    private void OnCardClicked(PlanetCard card)
    {
        Debug.Log($"PlanetEncyclopediaPanel: 卡片 {card.GetCardIndex()} 被点击");
        
        // 如果点击的是当前已选中的卡片，移除介绍卡片
        if (currentSelectedCard == card && currentDetailCard != null)
        {
            Debug.Log("PlanetEncyclopediaPanel: 收起介绍卡片");
            RemoveDetailCard();
            return;
        }
        
        // 移除旧的介绍卡片
        if (currentDetailCard != null)
        {
            RemoveDetailCard();
        }
        
        // 插入新的介绍卡片
        Debug.Log($"PlanetEncyclopediaPanel: 展开卡片 {card.GetCardIndex()} 的介绍");
        InsertDetailCardAfter(card);
    }
    
    /// <summary>
    /// 在指定卡片后插入介绍卡片
    /// </summary>
    private void InsertDetailCardAfter(PlanetCard card)
    {
        if (detailCardPrefab == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: detailCardPrefab未配置！");
            return;
        }
        
        if (content == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: content未配置！");
            return;
        }
        
        int cardIndex = card.GetCardIndex();
        if (cardIndex < 0 || cardIndex >= detailCardImages.Count)
        {
            Debug.LogWarning($"PlanetEncyclopediaPanel: 索引 {cardIndex} 超出介绍图片列表范围！");
            return;
        }
        
        // 实例化介绍卡片（parent应该是Transform，如果content是RectTransform需要使用content.transform）
        GameObject detailCardObj = Instantiate(detailCardPrefab, content != null ? content.transform : null);
        DetailCard detailCard = detailCardObj.GetComponent<DetailCard>();
        
        if (detailCard == null)
        {
            Debug.LogError($"PlanetEncyclopediaPanel: 预制体 {detailCardPrefab.name} 没有 DetailCard 脚本组件！");
            Destroy(detailCardObj);
            return;
        }
        
        // 设置介绍卡片的位置（在被点击的卡片后面）
        int targetIndex = card.transform.GetSiblingIndex() + 1;
        detailCardObj.transform.SetSiblingIndex(targetIndex);
        
        // 初始化介绍卡片
        Sprite detailSprite = detailCardImages[cardIndex];
        detailCard.Initialize(detailSprite);
        
        // 设置LayoutElement（较大尺寸）
        LayoutElement layoutElement = detailCardObj.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = detailCardWidth;
            layoutElement.preferredHeight = detailCardHeight;
        }
        
        // 强制刷新布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        
        // 保存引用
        currentDetailCard = detailCard;
        currentSelectedCard = card;
        
        // 滚动到被点击的卡片和详情卡片的中心位置（让两者同时在画面中央显示）
        ScrollToCardAndDetail(card, detailCardObj);
    }
    
    /// <summary>
    /// 移除当前介绍卡片
    /// </summary>
    private void RemoveDetailCard()
    {
        if (currentDetailCard != null)
        {
            Destroy(currentDetailCard.gameObject);
            currentDetailCard = null;
            currentSelectedCard = null;
            
            // 强制刷新布局
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }
    
    /// <summary>
    /// 左箭头点击
    /// </summary>
    private void OnLeftArrowClicked()
    {
        Debug.Log("PlanetEncyclopediaPanel: 左箭头被点击");
        
        if (scrollRect == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: scrollRect未配置！");
            return;
        }
        
        if (content == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: content未配置！");
            return;
        }
        
        // 计算滚动步长
        // 如果arrowScrollDistance为0，则自动使用（行星卡片宽度 + 间距）
        // 否则使用用户配置的滚动距离
        float scrollStep = arrowScrollDistance > 0 ? arrowScrollDistance : (planetCardWidth + cardSpacing);
        float contentWidth = content.rect.width;
        
        if (contentWidth <= 0)
        {
            Debug.LogWarning("PlanetEncyclopediaPanel: Content宽度为0，无法滚动！");
            return;
        }
        
        float targetPosition = scrollRect.horizontalNormalizedPosition - (scrollStep / contentWidth);
        targetPosition = Mathf.Clamp01(targetPosition);
        
        Debug.Log($"PlanetEncyclopediaPanel: 向左滚动，当前位置: {scrollRect.horizontalNormalizedPosition}, 目标位置: {targetPosition}");
        
        // 执行滚动动画
        DOTween.To(() => scrollRect.horizontalNormalizedPosition, 
                   x => scrollRect.horizontalNormalizedPosition = x, 
                   targetPosition, scrollDuration)
               .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 右箭头点击
    /// </summary>
    private void OnRightArrowClicked()
    {
        Debug.Log("PlanetEncyclopediaPanel: 右箭头被点击");
        
        if (scrollRect == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: scrollRect未配置！");
            return;
        }
        
        if (content == null)
        {
            Debug.LogError("PlanetEncyclopediaPanel: content未配置！");
            return;
        }
        
        // 计算滚动步长
        // 如果arrowScrollDistance为0，则自动使用（行星卡片宽度 + 间距）
        // 否则使用用户配置的滚动距离
        float scrollStep = arrowScrollDistance > 0 ? arrowScrollDistance : (planetCardWidth + cardSpacing);
        float contentWidth = content.rect.width;
        
        if (contentWidth <= 0)
        {
            Debug.LogWarning("PlanetEncyclopediaPanel: Content宽度为0，无法滚动！");
            return;
        }
        
        float targetPosition = scrollRect.horizontalNormalizedPosition + (scrollStep / contentWidth);
        targetPosition = Mathf.Clamp01(targetPosition);
        
        Debug.Log($"PlanetEncyclopediaPanel: 向右滚动，当前位置: {scrollRect.horizontalNormalizedPosition}, 目标位置: {targetPosition}");
        
        // 执行滚动动画
        DOTween.To(() => scrollRect.horizontalNormalizedPosition, 
                   x => scrollRect.horizontalNormalizedPosition = x, 
                   targetPosition, scrollDuration)
               .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 处理鼠标滚轮
    /// </summary>
    private void HandleMouseWheel()
    {
        if (scrollRect == null) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 计算滚动步长
            // 如果arrowScrollDistance为0，则自动使用（行星卡片宽度 + 间距）
            // 否则使用用户配置的滚动距离
            float baseScrollStep = arrowScrollDistance > 0 ? arrowScrollDistance : (planetCardWidth + cardSpacing);
            float scrollStep = baseScrollStep * scroll * 2f; // 乘以2增加灵敏度
            float targetPosition = scrollRect.horizontalNormalizedPosition + (scrollStep / content.rect.width);
            targetPosition = Mathf.Clamp01(targetPosition);
            
            // 执行滚动
            scrollRect.horizontalNormalizedPosition = targetPosition;
        }
    }
    
    /// <summary>
    /// 滚动到指定卡片
    /// </summary>
    private void ScrollToCard(PlanetCard card)
    {
        if (scrollRect == null || content == null) return;
        
        // 计算卡片在Content中的位置
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect == null) return;
        
        // 获取卡片相对于Content的位置
        float cardPositionX = cardRect.anchoredPosition.x;
        float normalizedPosition = cardPositionX / (content.rect.width - scrollRect.viewport.rect.width);
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        
        // 执行滚动动画
        DOTween.To(() => scrollRect.horizontalNormalizedPosition, 
                   x => scrollRect.horizontalNormalizedPosition = x, 
                   normalizedPosition, scrollDuration)
               .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 滚动到行星卡片和详情卡片的中心位置（让两者同时在画面中央显示）
    /// </summary>
    private void ScrollToCardAndDetail(PlanetCard card, GameObject detailCardObj)
    {
        if (scrollRect == null || content == null) return;
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform detailRect = detailCardObj.GetComponent<RectTransform>();
        
        if (cardRect == null || detailRect == null) return;
        
        // 等待一帧，确保布局已更新
        StartCoroutine(ScrollToCardAndDetailCoroutine(cardRect, detailRect));
    }
    
    /// <summary>
    /// 滚动到行星卡片和详情卡片中心的协程
    /// </summary>
    private IEnumerator ScrollToCardAndDetailCoroutine(RectTransform cardRect, RectTransform detailRect)
    {
        // 等待布局更新完成
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // 获取两个卡片在Content中的位置（相对于Content的左边缘）
        // 在Horizontal Layout Group中，anchoredPosition.x 是卡片中心相对于Content左边缘的偏移
        float cardCenterX = cardRect.anchoredPosition.x;
        float detailCenterX = detailRect.anchoredPosition.x;
        
        // 计算两个卡片组合的中心点位置
        float combinedCenterX = (cardCenterX + detailCenterX) / 2f;
        
        // 计算视口宽度
        float viewportWidth = scrollRect.viewport.rect.width;
        
        // 计算Content的总宽度和可滚动宽度
        float contentWidth = content.rect.width;
        float scrollableWidth = contentWidth - viewportWidth;
        
        if (scrollableWidth <= 0)
        {
            // 如果不需要滚动，直接返回
            yield break;
        }
        
        // 计算目标滚动位置：让组合中心点在视口中央
        // 视口中央在Content中的位置 = combinedCenterX - viewportWidth / 2
        // 由于Content的pivot通常在左上角(0,1)，所以直接计算即可
        // 应用用户配置的偏移量
        float targetContentPosition = combinedCenterX - viewportWidth / 2f + scrollCenterOffset;
        
        // 转换为归一化位置（0-1）
        // normalizedPosition = 0 表示Content左边缘对齐视口左边缘
        // normalizedPosition = 1 表示Content右边缘对齐视口右边缘
        float normalizedPosition = targetContentPosition / scrollableWidth;
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        
        Debug.Log($"PlanetEncyclopediaPanel: 滚动到卡片中心，combinedCenterX={combinedCenterX}, viewportWidth={viewportWidth}, normalizedPosition={normalizedPosition}");
        
        // 执行滚动动画
        DOTween.To(() => scrollRect.horizontalNormalizedPosition, 
                   x => scrollRect.horizontalNormalizedPosition = x, 
                   normalizedPosition, scrollDuration)
               .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// 显示行星图鉴面板
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 如果卡片列表为空，初始化卡片（只在第一次显示时初始化）
        if (planetCards.Count == 0)
        {
            InitializeCards();
        }
    }
    
    /// <summary>
    /// 隐藏行星图鉴面板
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
        Debug.Log("返回主界面");
        // 根据当前场景决定返回哪里
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (UIManager.Instance != null)
        {
            if (currentSceneName == "01_MainHub")
            {
                // 在主界面场景中，返回主界面
                UIManager.Instance.ReturnToMainHub();
            }
            else
            {
                // 在主菜单场景中，返回主菜单
                UIManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
