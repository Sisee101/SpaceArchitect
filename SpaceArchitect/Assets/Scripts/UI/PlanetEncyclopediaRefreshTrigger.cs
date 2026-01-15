using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 行星图鉴刷新触发器
/// 低耦合脚本，可挂载在打开图鉴的按钮上
/// 每次按钮点击时，自动刷新所有行星图鉴卡片的解锁状态
/// </summary>
[RequireComponent(typeof(Button))]
public class PlanetEncyclopediaRefreshTrigger : MonoBehaviour
{
    [Header("刷新设置")]
    [Tooltip("是否在按钮点击时自动刷新图鉴状态")]
    [SerializeField] private bool autoRefreshOnClick = true;
    
    [Tooltip("刷新延迟时间（秒），用于确保PlanetUnlockManager已初始化")]
    [SerializeField] private float refreshDelay = 0.1f;
    
    [Header("调试选项")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("PlanetEncyclopediaRefreshTrigger: 未找到Button组件！");
            return;
        }
    }
    
    void Start()
    {
        if (button != null && autoRefreshOnClick)
        {
            // 订阅按钮点击事件
            button.onClick.AddListener(OnButtonClicked);
            
            if (showDebugLogs)
            {
                Debug.Log("PlanetEncyclopediaRefreshTrigger: 已订阅按钮点击事件，将自动刷新图鉴状态");
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClicked()
    {
        if (autoRefreshOnClick)
        {
            // 使用协程延迟刷新，确保PlanetUnlockManager已初始化
            StartCoroutine(RefreshEncyclopediaDelayed());
        }
    }
    
    /// <summary>
    /// 延迟刷新图鉴的协程
    /// </summary>
    private IEnumerator RefreshEncyclopediaDelayed()
    {
        // 等待指定时间，确保PlanetUnlockManager已经正确初始化
        yield return new WaitForSeconds(refreshDelay);
        
        // 执行刷新
        RefreshAllPlanetCards();
    }
    
    /// <summary>
    /// 刷新所有行星图鉴卡片的状态
    /// 从PlanetUnlockManager读取最新解锁状态并更新显示
    /// </summary>
    public void RefreshAllPlanetCards()
    {
        if (showDebugLogs)
        {
            Debug.Log("PlanetEncyclopediaRefreshTrigger: 开始刷新所有行星图鉴卡片状态");
        }
        
        // 查找场景中所有的PlanetCard组件
        PlanetCard[] allCards = FindObjectsOfType<PlanetCard>(true); // true表示包括未激活的对象
        
        if (allCards == null || allCards.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("PlanetEncyclopediaRefreshTrigger: 未找到任何PlanetCard组件");
            }
            return;
        }
        
        int refreshedCount = 0;
        
        // 遍历所有卡片，更新解锁状态
        foreach (PlanetCard card in allCards)
        {
            if (card != null)
            {
                int cardIndex = card.GetCardIndex();
                
                if (cardIndex >= 0 && cardIndex < 13)
                {
                    // 从PlanetUnlockManager获取最新的解锁状态
                    bool currentUnlockState = PlanetUnlockManager.IsPlanetUnlocked(cardIndex);
                    
                    // 更新卡片状态
                    card.SetUnlockState(currentUnlockState);
                    
                    refreshedCount++;
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"PlanetEncyclopediaRefreshTrigger: 刷新卡片 {cardIndex} 的解锁状态为 {(currentUnlockState ? "已解锁" : "未解锁")}");
                    }
                }
                else
                {
                    if (showDebugLogs)
                    {
                        Debug.LogWarning($"PlanetEncyclopediaRefreshTrigger: 卡片索引 {cardIndex} 无效，跳过刷新");
                    }
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PlanetEncyclopediaRefreshTrigger: 刷新完成，共刷新 {refreshedCount} 张卡片");
        }
    }
    
    /// <summary>
    /// 手动触发刷新（供外部调用）
    /// </summary>
    public void TriggerRefresh()
    {
        StartCoroutine(RefreshEncyclopediaDelayed());
    }
}
