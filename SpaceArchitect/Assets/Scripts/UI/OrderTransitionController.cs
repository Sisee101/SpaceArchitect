using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 订单切换控制器
/// 处理订单完成后的盖章、移出、移入动画
/// 管理订单按钮和面板的组合切换
/// </summary>
public class OrderTransitionController : MonoBehaviour
{
    [Header("订单组合引用（按钮+面板的组合）")]
    [SerializeField] private OrderSet currentOrderSet;  // 当前显示的订单组合（按钮+面板）
    [SerializeField] private OrderSet newOrderSet;      // 待移入的新订单组合（按钮+面板）
    
    [Header("动画参数")]
    [SerializeField] private float stampDisplayDuration = 1.5f;  // 盖章显示时间（秒）
    [SerializeField] private float transitionDuration = 0.6f;    // 切换动画时长（秒）
    [SerializeField] private float transitionDelay = 0.1f;       // 新订单移入延迟（秒）
    
    private bool isTransitioning = false;  // 是否正在切换中
    
    // 保存面板状态，用于场景切换后恢复
    private static bool wasPanelOpenBeforeSceneChange = false;
    
    void Start()
    {
        // 确保OrderManager已初始化
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderTransitionController: OrderManager未找到！");
        }
        
        // 初始化第一个订单（显示在主界面上）
        InitializeFirstOrder();
        
        // 初始化新订单组合位置（屏幕左侧外）
        if (newOrderSet != null)
        {
            float screenLeft = -Screen.width - 200f;
            Vector3 currentPos = currentOrderSet != null ? currentOrderSet.transform.localPosition : Vector3.zero;
            newOrderSet.transform.localPosition = new Vector3(screenLeft, currentPos.y, currentPos.z);
            
            // 设置初始透明度和隐藏状态
            if (newOrderSet.CanvasGroupComponent != null)
            {
                newOrderSet.CanvasGroupComponent.alpha = 0f;
            }
            
            // 新订单初始状态：按钮显示，面板隐藏（等待用户点击按钮）
            if (newOrderSet.Button != null)
            {
                newOrderSet.Button.SetActive(false);  // 初始隐藏，等待移入后再显示
            }
            if (newOrderSet.Panel != null)
            {
                newOrderSet.Panel.Hide();  // 面板默认隐藏
            }
        }
        
        // 如果之前面板是打开的，恢复面板状态
        if (wasPanelOpenBeforeSceneChange && currentOrderSet != null && currentOrderSet.Panel != null)
        {
            // 延迟一帧再显示，确保场景完全加载
            StartCoroutine(RestorePanelStateDelayed());
        }
    }
    
    /// <summary>
    /// 延迟恢复面板状态（确保场景完全加载）
    /// </summary>
    private IEnumerator RestorePanelStateDelayed()
    {
        yield return null; // 等待一帧
        
        if (currentOrderSet != null && currentOrderSet.Panel != null)
        {
            currentOrderSet.Panel.Show();
            Debug.Log("OrderTransitionController: 已恢复面板打开状态");
        }
        
        wasPanelOpenBeforeSceneChange = false; // 重置标志
    }
    
    /// <summary>
    /// 初始化第一个订单（场景加载时调用）
    /// </summary>
    private void InitializeFirstOrder()
    {
        if (currentOrderSet != null && OrderManager.Instance != null)
        {
            OrderData firstOrder = OrderManager.Instance.GetCurrentOrder();
            if (firstOrder != null && firstOrder.orderImage != null)
            {
                currentOrderSet.SetOrderImage(firstOrder.orderImage);
                currentOrderSet.HideStamp();  // 确保盖章图层是隐藏的
                
                // 第一个订单：按钮显示，面板隐藏（等待用户点击）
                if (currentOrderSet.Button != null)
                {
                    currentOrderSet.Button.SetActive(true);
                }
                if (currentOrderSet.Panel != null)
                {
                    currentOrderSet.Panel.Hide();
                }
                
                Debug.Log($"OrderTransitionController: 已初始化第一个订单 (ID: {firstOrder.orderId})");
            }
            else
            {
                Debug.LogWarning("OrderTransitionController: 无法获取第一个订单或订单图片为空");
            }
        }
    }
    
    /// <summary>
    /// 完成当前订单并切换到下一个订单
    /// </summary>
    public void CompleteCurrentOrderAndSwitch()
    {
        // 防止重复触发
        if (isTransitioning)
        {
            Debug.LogWarning("OrderTransitionController: 正在切换中，请稍候...");
            return;
        }
        
        // 检查 OrderManager 是否存在
        if (OrderManager.Instance == null)
        {
            Debug.LogError("OrderTransitionController: OrderManager未找到，无法切换订单！");
            return;
        }
        
        // 检查是否有下一个订单
        if (!OrderManager.Instance.HasMoreOrders())
        {
            Debug.Log("所有订单已完成！循环回到第一个订单。");
            // 循环回到第一个订单
            OrderManager.Instance.ResetToFirstOrder();
            InitializeFirstOrder();
            // 可选：显示完成提示UI
            return;
        }
        
        Debug.Log("OrderTransitionController: 开始订单切换动画");
        // 启动切换协程
        StartCoroutine(AnimateOrderTransition());
    }
    
    /// <summary>
    /// 执行订单切换动画
    /// </summary>
    private IEnumerator AnimateOrderTransition()
    {
        isTransitioning = true;
        
        // 步骤1：显示盖章效果（无论面板是否展开都显示）
        if (currentOrderSet != null && currentOrderSet.Panel != null)
        {
            // 如果面板当前是隐藏的，先显示面板
            if (!currentOrderSet.Panel.gameObject.activeSelf)
            {
                currentOrderSet.Panel.Show();
            }
            // 显示盖章
            currentOrderSet.ShowStamp();
        }
        
        // 步骤2：等待一段时间，让用户看到盖章效果
        yield return new WaitForSeconds(stampDisplayDuration);
        
        // 步骤3：检查是否还有下一个订单
        if (!OrderManager.Instance.HasMoreOrders())
        {
            Debug.Log("所有订单已完成！");
            // 可选：如果订单完成，可以选择循环回到第一个，或显示完成提示
            // 这里选择循环回到第一个订单
            OrderManager.Instance.ResetToFirstOrder();
            // 重新初始化第一个订单
            InitializeFirstOrder();
            isTransitioning = false;
            yield break;
        }
        
        // 步骤4：准备新订单
        OrderData nextOrder = OrderManager.Instance.GetNextPendingOrder();
        if (nextOrder == null || nextOrder.orderImage == null)
        {
            Debug.LogError("OrderTransitionController: 无法获取下一个订单或订单图片为空！");
            isTransitioning = false;
            yield break;
        }
        
        // 更新新订单组合内容
        if (newOrderSet != null)
        {
            newOrderSet.SetOrderImage(nextOrder.orderImage);
            newOrderSet.HideStamp();  // 确保新订单的盖章是隐藏的
            
            // 新订单初始状态：按钮显示，面板隐藏
            if (newOrderSet.Button != null)
            {
                newOrderSet.Button.SetActive(true);  // 准备显示按钮
            }
            if (newOrderSet.Panel != null)
            {
                newOrderSet.Panel.Hide();  // 面板默认隐藏，等待用户点击按钮
            }
        }
        
        // 步骤5：执行切换动画（按钮和面板一起移动）
        yield return StartCoroutine(PlayTransitionAnimation());
        
        // 步骤6：切换引用和状态更新
        SwapOrderSets();
        
        // 步骤7：标记当前订单为已完成
        OrderManager.Instance.MarkCurrentOrderAsCompleted();
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// 播放切换动画（按钮和面板一起移动）
    /// 旧订单向右滑出，新订单从左侧滑入
    /// </summary>
    private IEnumerator PlayTransitionAnimation()
    {
        if (currentOrderSet == null || newOrderSet == null)
        {
            Debug.LogError("OrderTransitionController: 订单组合引用为空！");
            yield break;
        }
        
        // 获取当前位置和屏幕尺寸
        float originalX = currentOrderSet.GetCurrentX();  // 当前订单的位置（也就是新订单的目标位置）
        float screenLeft = -Screen.width - 200f;  // 屏幕左侧外（新订单初始位置，负值）
        float screenRight = Screen.width + 200f;  // 屏幕右侧外（旧订单移出位置，正值）
        float yPos = currentOrderSet.transform.localPosition.y;
        float zPos = currentOrderSet.transform.localPosition.z;
        
        Debug.Log($"OrderTransitionController: 准备动画 - originalX={originalX}, screenLeft={screenLeft}, screenRight={screenRight}");
        Debug.Log($"OrderTransitionController: 旧订单当前位置: {currentOrderSet.transform.localPosition.x}, 新订单当前位置: {newOrderSet.transform.localPosition.x}");
        
        // 确保新订单 GameObject 是激活的（这样才能看到动画）
        if (!newOrderSet.gameObject.activeSelf)
        {
            newOrderSet.gameObject.SetActive(true);
        }
        
        // 强制设置新订单组合初始位置（屏幕左侧外，确保在动画开始前）
        Vector3 newOrderStartPos = new Vector3(screenLeft, yPos, zPos);
        newOrderSet.transform.localPosition = newOrderStartPos;
        
        // 确保新订单的 CanvasGroup alpha 为 0（初始不可见，动画时淡入）
        if (newOrderSet.CanvasGroupComponent != null)
        {
            newOrderSet.CanvasGroupComponent.alpha = 0f;
        }
        
        Debug.Log($"OrderTransitionController: 已设置新订单初始位置到左侧: {newOrderSet.transform.localPosition.x}");
        
        // 确保新订单按钮在动画开始时是激活的（准备移入）
        if (newOrderSet.Button != null)
        {
            newOrderSet.Button.SetActive(true);
        }
        
        // 确保新订单面板是隐藏的（等待用户点击）
        if (newOrderSet.Panel != null)
        {
            newOrderSet.Panel.Hide();
        }
        
        Debug.Log($"OrderTransitionController: 开始动画 - 旧订单从 {currentOrderSet.GetCurrentX()} 向右移到 {screenRight}, 新订单从 {screenLeft} 移到 {originalX}");
        
        // 使用DOTween Sequence创建动画序列
        Sequence transitionSequence = DOTween.Sequence();
        
        // 移出旧订单组合（按钮和面板一起向右移出屏幕）
        transitionSequence.Join(currentOrderSet.transform.DOLocalMoveX(screenRight, transitionDuration).SetEase(Ease.InCubic));
        if (currentOrderSet.CanvasGroupComponent != null)
        {
            transitionSequence.Join(currentOrderSet.CanvasGroupComponent.DOFade(0, transitionDuration));
        }
        
        // 移入新订单组合（按钮和面板一起从左侧移入，稍微延迟让移出先开始）
        transitionSequence.Join(newOrderSet.transform.DOLocalMoveX(originalX, transitionDuration).SetEase(Ease.OutCubic).SetDelay(transitionDelay));
        if (newOrderSet.CanvasGroupComponent != null)
        {
            transitionSequence.Join(newOrderSet.CanvasGroupComponent.DOFade(1, transitionDuration).SetDelay(transitionDelay));
        }
        
        // 等待动画完成
        yield return transitionSequence.WaitForCompletion();
        
        Debug.Log("OrderTransitionController: 动画完成");
    }
    
    /// <summary>
    /// 交换订单组合引用
    /// </summary>
    private void SwapOrderSets()
    {
        // 交换引用
        OrderSet temp = currentOrderSet;
        currentOrderSet = newOrderSet;
        newOrderSet = temp;
        
        // 重置旧组合（准备下次使用）
        // 注意：此时 newOrderSet 已经是之前的 currentOrderSet（刚移出的那个）
        if (newOrderSet != null)
        {
            newOrderSet.HideStamp();
            
            float screenLeft = -Screen.width - 200f;  // 屏幕左侧外（新订单初始位置）
            float yPos = currentOrderSet != null ? currentOrderSet.transform.localPosition.y : 0f;
            float zPos = currentOrderSet != null ? currentOrderSet.transform.localPosition.z : 0f;
            
            // 直接设置位置到左侧外（不通过 ResetPosition，因为那会设置 alpha）
            newOrderSet.transform.localPosition = new Vector3(screenLeft, yPos, zPos);
            
            // 设置 CanvasGroup alpha 为 0
            if (newOrderSet.CanvasGroupComponent != null)
            {
                newOrderSet.CanvasGroupComponent.alpha = 0f;
            }
            
            // 重置状态：按钮显示，面板隐藏
            if (newOrderSet.Button != null)
            {
                newOrderSet.Button.SetActive(false);  // 准备下次移入时再显示
            }
            if (newOrderSet.Panel != null)
            {
                newOrderSet.Panel.Hide();
            }
            
            // 确保 GameObject 保持激活状态（否则下次动画看不到）
            if (!newOrderSet.gameObject.activeSelf)
            {
                newOrderSet.gameObject.SetActive(true);
            }
            
            Debug.Log($"OrderTransitionController: 已重置 newOrderSet 到左侧外位置: {screenLeft}");
        }
    }
    
    /// <summary>
    /// 获取当前订单组合（供外部访问，如UIManager）
    /// </summary>
    public OrderSet GetCurrentOrderSet()
    {
        return currentOrderSet;
    }
    
    /// <summary>
    /// 获取新订单组合（供外部访问）
    /// </summary>
    public OrderSet GetNewOrderSet()
    {
        return newOrderSet;
    }
    
    /// <summary>
    /// 保存面板状态（在场景切换前调用）
    /// </summary>
    public void SavePanelState()
    {
        if (currentOrderSet != null && currentOrderSet.Panel != null)
        {
            wasPanelOpenBeforeSceneChange = currentOrderSet.Panel.gameObject.activeSelf;
            Debug.Log($"OrderTransitionController: 保存面板状态 - 打开: {wasPanelOpenBeforeSceneChange}");
        }
    }
    
    /// <summary>
    /// 检查当前面板是否打开
    /// </summary>
    public bool IsPanelOpen()
    {
        if (currentOrderSet != null && currentOrderSet.Panel != null)
        {
            return currentOrderSet.Panel.gameObject.activeSelf;
        }
        return false;
    }
}

