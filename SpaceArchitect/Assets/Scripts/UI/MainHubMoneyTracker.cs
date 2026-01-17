using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MainHub场景数据追踪器
/// 在进入MainHub场景时记录当前的Money值和解锁行星数量，并在点击特定按钮时计算差额
/// 同时显示当前已完成的订单总数（不计算差额）
/// </summary>
public class MainHubMoneyTracker : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("触发计算差额的按钮（如果为空，将自动查找）")]
    [SerializeField] private Button triggerButton;
    
    [Header("数据配置引用")]
    [Tooltip("订单数据配置引用（如果为空，将自动查找）")]
    [SerializeField] private SphereOrderDataConfig orderDataConfig;
    
    [Header("文本显示组件")]
    [Tooltip("显示订单金额差额的Text组件（Unity UI Text）")]
    [SerializeField] private Text moneyDifferenceText;
    
    [Tooltip("显示订单金额差额的TextMeshPro组件（TextMeshPro - Text (UI)）")]
    [SerializeField] private TextMeshProUGUI moneyDifferenceTextMeshPro;
    
    [Tooltip("显示累计完成订单总数的Text组件（Unity UI Text）")]
    [SerializeField] private Text orderCountText;
    
    [Tooltip("显示累计完成订单总数的TextMeshPro组件（TextMeshPro - Text (UI)）")]
    [SerializeField] private TextMeshProUGUI orderCountTextMeshPro;
    
    [Tooltip("显示新开辟航线数量的Text组件（Unity UI Text）")]
    [SerializeField] private Text unlockPlanetText;
    
    [Tooltip("显示新开辟航线数量的TextMeshPro组件（TextMeshPro - Text (UI)）")]
    [SerializeField] private TextMeshProUGUI unlockPlanetTextMeshPro;
    
    [Header("自动查找设置")]
    [Tooltip("是否在Start时自动查找丢失的引用（推荐保持为true）")]
    [SerializeField] private bool autoFindOnStart = true;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 进入场景时记录的Money值
    private int initialMoneyValue;
    
    // 进入场景时记录的行星解锁数量
    private int initialUnlockedPlanetCount;
    
    // 是否已经记录了初始值
    private bool hasRecordedInitialValue = false;
    
    void Start()
    {
        // 记录进入场景时的Money值和解锁行星数量
        RecordInitialValues();
        
        // 自动查找丢失的引用
        if (autoFindOnStart)
        {
            FindMissingReferences();
        }
        
        // 绑定按钮事件
        BindButtonEvent();
    }
    
    void OnEnable()
    {
        // 如果场景重新加载，重新记录初始值
        if (!hasRecordedInitialValue)
        {
            RecordInitialValues();
        }
        
        // 重新查找引用（场景重新加载后引用可能丢失）
        if (autoFindOnStart)
        {
            FindMissingReferences();
        }
        
        // 重新绑定按钮事件
        BindButtonEvent();
    }
    
    /// <summary>
    /// 获取当前已解锁的行星数量
    /// </summary>
    /// <returns>已解锁的行星数量</returns>
    private int GetUnlockedPlanetCount()
    {
        int count = 0;
        // 遍历13个行星，统计已解锁的数量
        for (int i = 0; i < 13; i++)
        {
            if (PlanetUnlockManager.IsPlanetUnlocked(i))
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 获取当前已完成的订单数量
    /// </summary>
    /// <returns>已完成的订单数量</returns>
    private int GetCompletedOrderCount()
    {
        // 如果订单数据配置未找到，尝试查找
        if (orderDataConfig == null)
        {
            FindOrderDataConfig();
        }
        
        if (orderDataConfig == null || orderDataConfig.orderDataList == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("MainHubMoneyTracker: 无法获取订单完成数量，orderDataConfig未配置！");
            }
            return 0;
        }
        
        int count = 0;
        // 遍历所有订单，统计已完成的数量
        foreach (var orderInfo in orderDataConfig.orderDataList)
        {
            if (orderInfo != null && orderInfo.CompleteOrder)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 查找文本组件（用于场景重新加载后重新查找）
    /// 自动查找场景中名为"Money"、"Order"、"Planet"的Text对象
    /// </summary>
    private void FindTextComponents()
    {
        // 查找名为"Money"的Text组件
        if (moneyDifferenceText == null && moneyDifferenceTextMeshPro == null)
        {
            GameObject moneyObj = GameObject.Find("Money");
            if (moneyObj != null)
            {
                // 优先查找TextMeshProUGUI
                moneyDifferenceTextMeshPro = moneyObj.GetComponent<TextMeshProUGUI>();
                if (moneyDifferenceTextMeshPro == null)
                {
                    // 如果找不到TextMeshPro，尝试查找Unity UI Text
                    moneyDifferenceText = moneyObj.GetComponent<Text>();
                }
                
                if ((moneyDifferenceText != null || moneyDifferenceTextMeshPro != null) && enableDebugLog)
                {
                    Debug.Log($"MainHubMoneyTracker: 自动找到名为'Money'的文本组件（{moneyObj.name}）");
                }
            }
        }
        
        // 查找名为"Order"的Text组件
        if (orderCountText == null && orderCountTextMeshPro == null)
        {
            GameObject orderObj = GameObject.Find("Order");
            if (orderObj != null)
            {
                // 优先查找TextMeshProUGUI
                orderCountTextMeshPro = orderObj.GetComponent<TextMeshProUGUI>();
                if (orderCountTextMeshPro == null)
                {
                    // 如果找不到TextMeshPro，尝试查找Unity UI Text
                    orderCountText = orderObj.GetComponent<Text>();
                }
                
                if ((orderCountText != null || orderCountTextMeshPro != null) && enableDebugLog)
                {
                    Debug.Log($"MainHubMoneyTracker: 自动找到名为'Order'的文本组件（{orderObj.name}）");
                }
            }
        }
        
        // 查找名为"Planet"的Text组件
        if (unlockPlanetText == null && unlockPlanetTextMeshPro == null)
        {
            GameObject planetObj = GameObject.Find("Planet");
            if (planetObj != null)
            {
                // 优先查找TextMeshProUGUI
                unlockPlanetTextMeshPro = planetObj.GetComponent<TextMeshProUGUI>();
                if (unlockPlanetTextMeshPro == null)
                {
                    // 如果找不到TextMeshPro，尝试查找Unity UI Text
                    unlockPlanetText = planetObj.GetComponent<Text>();
                }
                
                if ((unlockPlanetText != null || unlockPlanetTextMeshPro != null) && enableDebugLog)
                {
                    Debug.Log($"MainHubMoneyTracker: 自动找到名为'Planet'的文本组件（{planetObj.name}）");
                }
            }
        }
        
        // 如果通过GameObject.Find找不到，尝试在场景中所有Text组件中查找（包括未激活的）
        if ((moneyDifferenceText == null && moneyDifferenceTextMeshPro == null) ||
            (orderCountText == null && orderCountTextMeshPro == null) ||
            (unlockPlanetText == null && unlockPlanetTextMeshPro == null))
        {
            // 查找所有Text组件
            Text[] allTexts = Resources.FindObjectsOfTypeAll<Text>();
            TextMeshProUGUI[] allTextMeshPros = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            
            // 查找Money文本
            if (moneyDifferenceText == null && moneyDifferenceTextMeshPro == null)
            {
                foreach (Text txt in allTexts)
                {
                    if (txt.gameObject.scene.isLoaded && txt.gameObject.name == "Money")
                    {
                        moneyDifferenceText = txt;
                        if (enableDebugLog)
                        {
                            Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Money'的Text组件（{txt.gameObject.name}）");
                        }
                        break;
                    }
                }
                
                if (moneyDifferenceText == null)
                {
                    foreach (TextMeshProUGUI tmp in allTextMeshPros)
                    {
                        if (tmp.gameObject.scene.isLoaded && tmp.gameObject.name == "Money")
                        {
                            moneyDifferenceTextMeshPro = tmp;
                            if (enableDebugLog)
                            {
                                Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Money'的TextMeshPro组件（{tmp.gameObject.name}）");
                            }
                            break;
                        }
                    }
                }
            }
            
            // 查找Order文本
            if (orderCountText == null && orderCountTextMeshPro == null)
            {
                foreach (Text txt in allTexts)
                {
                    if (txt.gameObject.scene.isLoaded && txt.gameObject.name == "Order")
                    {
                        orderCountText = txt;
                        if (enableDebugLog)
                        {
                            Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Order'的Text组件（{txt.gameObject.name}）");
                        }
                        break;
                    }
                }
                
                if (orderCountText == null)
                {
                    foreach (TextMeshProUGUI tmp in allTextMeshPros)
                    {
                        if (tmp.gameObject.scene.isLoaded && tmp.gameObject.name == "Order")
                        {
                            orderCountTextMeshPro = tmp;
                            if (enableDebugLog)
                            {
                                Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Order'的TextMeshPro组件（{tmp.gameObject.name}）");
                            }
                            break;
                        }
                    }
                }
            }
            
            // 查找Planet文本
            if (unlockPlanetText == null && unlockPlanetTextMeshPro == null)
            {
                foreach (Text txt in allTexts)
                {
                    if (txt.gameObject.scene.isLoaded && txt.gameObject.name == "Planet")
                    {
                        unlockPlanetText = txt;
                        if (enableDebugLog)
                        {
                            Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Planet'的Text组件（{txt.gameObject.name}）");
                        }
                        break;
                    }
                }
                
                if (unlockPlanetText == null)
                {
                    foreach (TextMeshProUGUI tmp in allTextMeshPros)
                    {
                        if (tmp.gameObject.scene.isLoaded && tmp.gameObject.name == "Planet")
                        {
                            unlockPlanetTextMeshPro = tmp;
                            if (enableDebugLog)
                            {
                                Debug.Log($"MainHubMoneyTracker: 在场景中找到名为'Planet'的TextMeshPro组件（{tmp.gameObject.name}）");
                            }
                            break;
                        }
                    }
                }
            }
        }
        
        // 输出警告（如果某些文本组件未找到）
        if (moneyDifferenceText == null && moneyDifferenceTextMeshPro == null)
        {
            Debug.LogWarning("MainHubMoneyTracker: 未找到名为'Money'的文本组件，请在场景中创建名为'Money'的Text或TextMeshPro组件，或在Inspector中手动指定");
        }
        
        if (orderCountText == null && orderCountTextMeshPro == null)
        {
            Debug.LogWarning("MainHubMoneyTracker: 未找到名为'Order'的文本组件，请在场景中创建名为'Order'的Text或TextMeshPro组件，或在Inspector中手动指定");
        }
        
        if (unlockPlanetText == null && unlockPlanetTextMeshPro == null)
        {
            Debug.LogWarning("MainHubMoneyTracker: 未找到名为'Planet'的文本组件，请在场景中创建名为'Planet'的Text或TextMeshPro组件，或在Inspector中手动指定");
        }
    }
    
    /// <summary>
    /// 查找订单数据配置（用于场景重新加载后重新查找）
    /// </summary>
    private void FindOrderDataConfig()
    {
        if (orderDataConfig == null)
        {
            // 方法1：尝试从Resources文件夹加载
            orderDataConfig = Resources.Load<SphereOrderDataConfig>("SphereOrderDataConfig");
            if (orderDataConfig != null && enableDebugLog)
            {
                Debug.Log("MainHubMoneyTracker: 成功从Resources加载SphereOrderDataConfig");
            }
            
            // 方法2：如果Resources加载失败，尝试查找场景中使用该配置的对象
            if (orderDataConfig == null)
            {
                TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
                foreach (TaskManager tm in taskManagers)
                {
                    // 尝试通过反射获取orderDataConfig字段
                    var field = typeof(TaskManager).GetField("orderDataConfig", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var config = field.GetValue(tm) as SphereOrderDataConfig;
                        if (config != null)
                        {
                            orderDataConfig = config;
                            if (enableDebugLog)
                            {
                                Debug.Log($"MainHubMoneyTracker: 从TaskManager获取到SphereOrderDataConfig引用");
                            }
                            break;
                        }
                    }
                }
            }
            
            // 如果仍然找不到，输出警告
            if (orderDataConfig == null)
            {
                Debug.LogWarning("MainHubMoneyTracker: 未找到SphereOrderDataConfig，请确保Resources文件夹中有SphereOrderDataConfig资源，或手动指定引用");
            }
        }
    }
    
    /// <summary>
    /// 记录进入场景时的Money值和解锁行星数量
    /// </summary>
    private void RecordInitialValues()
    {
        initialMoneyValue = MoneyManager.money;
        initialUnlockedPlanetCount = GetUnlockedPlanetCount();
        hasRecordedInitialValue = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"MainHubMoneyTracker: 已记录进入场景时的数据");
            Debug.Log($"  - Money值: {initialMoneyValue}");
            Debug.Log($"  - 解锁行星数量: {initialUnlockedPlanetCount}/13");
        }
    }
    
    /// <summary>
    /// 查找丢失的引用（用于场景重新加载后重新查找）
    /// </summary>
    public void FindMissingReferences()
    {
        // 查找订单数据配置
        FindOrderDataConfig();
        
        // 查找文本组件（如果未手动指定）
        FindTextComponents();
        
        // 查找按钮
        if (triggerButton == null)
        {
            // 方法1：优先通过名称查找"Finish"按钮
            GameObject finishButtonObj = GameObject.Find("Finish");
            if (finishButtonObj != null)
            {
                triggerButton = finishButtonObj.GetComponent<Button>();
                if (triggerButton != null && enableDebugLog)
                {
                    Debug.Log($"MainHubMoneyTracker: 通过名称找到按钮（{finishButtonObj.name}）");
                }
            }
            
            // 方法2：如果通过名称找不到，查找场景中的所有Button（包括未激活的）
            if (triggerButton == null)
            {
                Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
                
                // 优先查找场景中名为"Finish"的Button（不是预制体的）
                foreach (Button btn in allButtons)
                {
                    // 检查是否是场景中的对象（不是预制体资源）且名称为"Finish"
                    if (btn.gameObject.scene.isLoaded && btn.gameObject.name == "Finish")
                    {
                        triggerButton = btn;
                        if (enableDebugLog)
                        {
                            Debug.Log($"MainHubMoneyTracker: 自动找到名为'Finish'的按钮（场景对象: {btn.gameObject.name}）");
                        }
                        break;
                    }
                }
            }
            
            // 如果仍然找不到，输出警告
            if (triggerButton == null)
            {
                Debug.LogWarning("MainHubMoneyTracker: 未找到名为'Finish'的按钮，请在Inspector中手动指定引用，或确保场景中存在名为'Finish'的按钮");
            }
        }
    }
    
    /// <summary>
    /// 绑定按钮点击事件
    /// </summary>
    private void BindButtonEvent()
    {
        if (triggerButton != null)
        {
            // 先移除所有监听器（避免重复绑定）
            triggerButton.onClick.RemoveAllListeners();
            
            // 添加监听器
            triggerButton.onClick.AddListener(OnTriggerButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log($"MainHubMoneyTracker: 已绑定按钮事件（按钮: {triggerButton.gameObject.name}）");
            }
        }
    }
    
    /// <summary>
    /// 触发按钮点击事件处理
    /// </summary>
    private void OnTriggerButtonClicked()
    {
        // 获取当前Money值
        int currentMoney = MoneyManager.money;
        
        // 获取当前解锁的行星数量
        int currentUnlockedCount = GetUnlockedPlanetCount();
        
        // 获取当前完成的订单数量
        int currentCompletedOrderCount = GetCompletedOrderCount();
        
        // 计算Money差额（当前Money - 进入场景时的Money）
        int moneyDifference = currentMoney - initialMoneyValue;
        
        // 计算解锁行星数量差额（当前解锁数量 - 进入场景时的解锁数量）
        int unlockDifference = currentUnlockedCount - initialUnlockedPlanetCount;
        
        if (enableDebugLog)
        {
            Debug.Log($"MainHubMoneyTracker: 按钮被点击，计算数据差额");
            Debug.Log($"  - Money: {initialMoneyValue} → {currentMoney} (差额: {moneyDifference} {(moneyDifference >= 0 ? "(增加)" : "(减少)")})");
            Debug.Log($"  - 解锁行星数量: {initialUnlockedPlanetCount}/13 → {currentUnlockedCount}/13 (差额: {unlockDifference} {(unlockDifference >= 0 ? "(增加)" : "(减少)")})");
            Debug.Log($"  - 订单完成总数: {currentCompletedOrderCount}");
        }
        
        // 触发差额计算事件（可以在这里添加其他逻辑）
        OnDataDifferenceCalculated(moneyDifference, initialMoneyValue, currentMoney, 
                                   unlockDifference, initialUnlockedPlanetCount, currentUnlockedCount,
                                   currentCompletedOrderCount);
    }
    
    /// <summary>
    /// 数据差额计算完成事件
    /// 可以在这里添加自定义逻辑，或者订阅此事件
    /// </summary>
    /// <param name="moneyDifference">Money差额</param>
    /// <param name="initialMoney">进入场景时的Money值</param>
    /// <param name="currentMoney">当前Money值</param>
    /// <param name="unlockDifference">解锁行星数量差额</param>
    /// <param name="initialUnlockCount">进入场景时的解锁数量</param>
    /// <param name="currentUnlockCount">当前解锁数量</param>
    /// <param name="currentOrderCount">当前订单完成总数</param>
    private void OnDataDifferenceCalculated(int moneyDifference, int initialMoney, int currentMoney,
                                            int unlockDifference, int initialUnlockCount, int currentUnlockCount,
                                            int currentOrderCount)
    {
        // 更新文本显示
        UpdateTextDisplays(moneyDifference, currentOrderCount, unlockDifference);
        
        // 这里可以添加自定义逻辑
        // 例如：保存到文件、触发其他事件等
        
        // Money差额处理
        if (moneyDifference > 0)
        {
            // 可以在这里添加Money增加的逻辑
        }
        else if (moneyDifference < 0)
        {
            // 可以在这里添加Money减少的逻辑
        }
        
        // 解锁行星数量差额处理
        if (unlockDifference > 0)
        {
            // 可以在这里添加解锁新行星的逻辑
            if (enableDebugLog)
            {
                Debug.Log($"MainHubMoneyTracker: 检测到解锁了 {unlockDifference} 个新行星！");
            }
        }
        else if (unlockDifference < 0)
        {
            // 理论上不应该发生，但可以处理（可能是重置操作）
            if (enableDebugLog)
            {
                Debug.LogWarning($"MainHubMoneyTracker: 检测到解锁数量减少 {Mathf.Abs(unlockDifference)} 个（可能是重置操作）");
            }
        }
        
        // 订单完成总数处理（只显示总数，不计算差额）
        if (enableDebugLog)
        {
            Debug.Log($"MainHubMoneyTracker: 当前已完成订单总数: {currentOrderCount}");
        }
    }
    
    /// <summary>
    /// 更新文本显示
    /// </summary>
    /// <param name="moneyDifference">Money差额</param>
    /// <param name="orderCount">订单完成总数</param>
    /// <param name="unlockDifference">解锁行星数量差额</param>
    private void UpdateTextDisplays(int moneyDifference, int orderCount, int unlockDifference)
    {
        // 更新订单金额差额文本
        string moneyText = $"本月收支为：{moneyDifference}";
        if (moneyDifferenceText != null)
        {
            moneyDifferenceText.text = moneyText;
        }
        if (moneyDifferenceTextMeshPro != null)
        {
            moneyDifferenceTextMeshPro.text = moneyText;
        }
        
        // 更新累计完成订单总数文本
        string orderText = $"累计完成订单总数：{orderCount}";
        if (orderCountText != null)
        {
            orderCountText.text = orderText;
        }
        if (orderCountTextMeshPro != null)
        {
            orderCountTextMeshPro.text = orderText;
        }
        
        // 更新新开辟航线数量文本
        string unlockText = $"新开辟航线：{unlockDifference}";
        if (unlockPlanetText != null)
        {
            unlockPlanetText.text = unlockText;
        }
        if (unlockPlanetTextMeshPro != null)
        {
            unlockPlanetTextMeshPro.text = unlockText;
        }
    }
    
    /// <summary>
    /// 手动触发计算差额（可以在外部调用）
    /// </summary>
    public void CalculateDifference()
    {
        OnTriggerButtonClicked();
    }
    
    /// <summary>
    /// 获取进入场景时的Money值
    /// </summary>
    public int GetInitialMoneyValue()
    {
        return initialMoneyValue;
    }
    
    /// <summary>
    /// 获取当前Money差额
    /// </summary>
    public int GetMoneyDifference()
    {
        return MoneyManager.money - initialMoneyValue;
    }
    
    /// <summary>
    /// 获取进入场景时的解锁行星数量
    /// </summary>
    public int GetInitialUnlockedPlanetCount()
    {
        return initialUnlockedPlanetCount;
    }
    
    /// <summary>
    /// 获取当前解锁行星数量差额
    /// </summary>
    public int GetUnlockedPlanetDifference()
    {
        return GetUnlockedPlanetCount() - initialUnlockedPlanetCount;
    }
    
    /// <summary>
    /// 获取当前解锁的行星数量
    /// </summary>
    public int GetCurrentUnlockedPlanetCount()
    {
        return GetUnlockedPlanetCount();
    }
    
    /// <summary>
    /// 获取当前已完成的订单总数
    /// </summary>
    public int GetCurrentCompletedOrderCount()
    {
        return GetCompletedOrderCount();
    }
    
    void OnDestroy()
    {
        // 取消按钮事件绑定，避免内存泄漏
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnTriggerButtonClicked);
        }
    }
}
