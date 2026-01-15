using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 行星卡片控制器
/// 管理单个行星卡片的显示和点击事件
/// </summary>
public class PlanetCard : MonoBehaviour, IPointerClickHandler
{
    [Header("UI引用")]
    [SerializeField] private Image planetImage;      // 行星图片
    
    [Header("数据")]
    private int cardIndex = -1;                      // 卡片索引（0-12）
    private Sprite unlockedSprite;                    // 解锁后的卡片图片
    private Sprite lockedSprite;                      // 未解锁时的卡片图片
    private bool isUnlocked = false;                  // 当前解锁状态
    
    /// <summary>
    /// 卡片点击事件
    /// </summary>
    public event Action<PlanetCard> OnCardClicked;
    
    /// <summary>
    /// 初始化卡片
    /// </summary>
    /// <param name="unlockedSprite">解锁后的行星卡片图片</param>
    /// <param name="lockedSprite">未解锁时的卡片图片</param>
    /// <param name="index">卡片索引（0-12）</param>
    /// <param name="isUnlocked">初始解锁状态</param>
    public void Initialize(Sprite unlockedSprite, Sprite lockedSprite, int index, bool isUnlocked)
    {
        this.unlockedSprite = unlockedSprite;
        this.lockedSprite = lockedSprite;
        this.cardIndex = index;
        this.isUnlocked = isUnlocked;
        
        // 根据解锁状态设置图片
        UpdateDisplay();
    }
    
    /// <summary>
    /// 更新显示（根据解锁状态设置图片）
    /// </summary>
    private void UpdateDisplay()
    {
        if (planetImage == null)
        {
            Debug.LogWarning($"PlanetCard: planetImage引用为空，无法设置图片！");
            return;
        }
        
        // 根据解锁状态选择图片
        Sprite targetSprite = isUnlocked ? unlockedSprite : lockedSprite;
        
        if (targetSprite != null)
        {
            planetImage.sprite = targetSprite;
        }
        else
        {
            Debug.LogWarning($"PlanetCard: 索引 {cardIndex} 的{(isUnlocked ? "解锁" : "锁定")}图片为空！");
        }
    }
    
    /// <summary>
    /// 设置解锁状态
    /// </summary>
    /// <param name="unlocked">是否解锁</param>
    public void SetUnlockState(bool unlocked)
    {
        if (isUnlocked == unlocked)
        {
            return; // 状态未改变，无需更新
        }
        
        isUnlocked = unlocked;
        UpdateDisplay();
        
        Debug.Log($"PlanetCard: 索引 {cardIndex} 解锁状态已更新为 {(unlocked ? "已解锁" : "未解锁")}");
    }
    
    /// <summary>
    /// 获取当前解锁状态
    /// </summary>
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
    
    /// <summary>
    /// 获取卡片索引
    /// </summary>
    public int GetCardIndex()
    {
        return cardIndex;
    }
    
    /// <summary>
    /// 实现IPointerClickHandler接口，处理点击事件
    /// 这样不需要Button组件，卡片本身任何部位都可以点击
    /// 未解锁的卡片点击不会触发事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有解锁的卡片才能点击展开详情
        if (!isUnlocked)
        {
            Debug.Log($"PlanetCard: 索引 {cardIndex} 未解锁，无法点击展开");
            return;
        }
        
        if (OnCardClicked != null)
        {
            OnCardClicked.Invoke(this);
        }
    }
    
    void OnEnable()
    {
        // 订阅解锁事件和重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlanetUnlocked += OnPlanetUnlocked;
            EventManager.Instance.OnAllPlanetsReset += OnAllPlanetsReset;
        }
    }
    
    void OnDisable()
    {
        // 取消订阅解锁事件和重置事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlanetUnlocked -= OnPlanetUnlocked;
            EventManager.Instance.OnAllPlanetsReset -= OnAllPlanetsReset;
        }
    }
    
    /// <summary>
    /// 处理行星解锁事件
    /// </summary>
    private void OnPlanetUnlocked(int planetIndex)
    {
        // 如果解锁的是当前卡片对应的行星，更新状态
        if (planetIndex == cardIndex)
        {
            SetUnlockState(true);
        }
    }
    
    /// <summary>
    /// 处理所有行星重置事件
    /// </summary>
    private void OnAllPlanetsReset()
    {
        // 重新查询自己的解锁状态并更新显示
        bool currentUnlockState = PlanetUnlockManager.IsPlanetUnlocked(cardIndex);
        SetUnlockState(currentUnlockState);
        
        Debug.Log($"PlanetCard: 索引 {cardIndex} 已响应重置事件，当前状态: {(currentUnlockState ? "已解锁" : "未解锁")}");
    }
}
