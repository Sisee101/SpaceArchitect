using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏信息显示控制器
/// 显示Core技能数量和Boost技能剩余次数
/// </summary>
public class GameInfoDisplay : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("显示Core技能数量（Core1+Core2+Core3）的Text组件")]
    [SerializeField] private Text coreCountText;
    
    [Tooltip("显示Boost技能剩余次数的Text组件")]
    [SerializeField] private Text boostRemainingText;
    
    [Tooltip("与Boost显示Text同步显隐的额外Text组件（可选）")]
    [SerializeField] private Text additionalBoostText;

    [Header("Debug设置")]
    [Tooltip("是否在Debug窗口输出变量值")]
    [SerializeField] private bool enableDebugOutput = true;
    
    [Tooltip("Debug输出间隔（帧数），0表示每帧都输出")]
    [SerializeField] private int debugOutputInterval = 60; // 每60帧输出一次，减少日志刷屏

    [Header("查找设置")]
    [Tooltip("是否自动查找场景中的飞船（用于获取ShipBoostAim组件）")]
    [SerializeField] private bool autoFindSpaceship = true;
    
    [Tooltip("飞船的标签（用于自动查找）")]
    [SerializeField] private string spaceshipTag = "Spaceship";
    
    [Tooltip("手动指定飞船GameObject（如果autoFindSpaceship为false，则使用此引用）")]
    [SerializeField] private GameObject spaceshipObject;

    // 内部引用
    private ShipBoostAim shipBoostAim;
    
    // 缓存的变量值（用于Debug输出和减少不必要的更新）
    private int lastCoreCount = -1;
    private int lastBoostValue = -1;

    void Start()
    {
        // 验证UI引用
        if (coreCountText == null)
        {
            Debug.LogError("GameInfoDisplay: coreCountText 未赋值！请在Inspector中分配Text组件。");
        }
        
        if (boostRemainingText == null)
        {
            Debug.LogError("GameInfoDisplay: boostRemainingText 未赋值！请在Inspector中分配Text组件。");
        }

        // 查找或获取ShipBoostAim组件
        InitializeShipBoostAim();
    }

    void Update()
    {
        // 每帧更新显示文本
        UpdateDisplay();
    }

    /// <summary>
    /// 初始化ShipBoostAim组件引用
    /// </summary>
    private void InitializeShipBoostAim()
    {
        if (autoFindSpaceship)
        {
            // 自动查找飞船
            GameObject spaceship = GameObject.FindGameObjectWithTag(spaceshipTag);
            
            if (spaceship == null)
            {
                // 如果通过标签找不到，尝试通过组件查找
                ShipBoostAim foundBoostAim = FindObjectOfType<ShipBoostAim>();
                if (foundBoostAim != null)
                {
                    spaceship = foundBoostAim.gameObject;
                }
            }
            
            if (spaceship != null)
            {
                spaceshipObject = spaceship;
            }
            else
            {
                Debug.LogWarning("GameInfoDisplay: 未找到飞船对象，Boost剩余次数将无法显示。请检查飞船标签或手动指定spaceshipObject。");
            }
        }

        // 从飞船对象获取ShipBoostAim组件
        if (spaceshipObject != null)
        {
            shipBoostAim = spaceshipObject.GetComponent<ShipBoostAim>();
            
            if (shipBoostAim == null)
            {
                Debug.LogWarning($"GameInfoDisplay: {spaceshipObject.name} 未找到 ShipBoostAim 组件，Boost剩余次数将无法显示。");
            }
            else
            {
                Debug.Log($"GameInfoDisplay: 已找到 ShipBoostAim 组件（来自 {spaceshipObject.name}）");
            }
        }
    }

    /// <summary>
    /// 更新显示文本
    /// </summary>
    private void UpdateDisplay()
    {
        // 更新Core数量显示
        UpdateCoreCountDisplay();
        
        // 更新Boost剩余次数显示
        UpdateBoostRemainingDisplay();
    }

    /// <summary>
    /// 更新Core数量显示
    /// </summary>
    private void UpdateCoreCountDisplay()
    {
        if (coreCountText == null)
        {
            return;
        }

        // 计算Core1+Core2+Core3的和（解锁数量）
        int coreCount = 0;
        if (SkillManager.Core1) coreCount++;
        if (SkillManager.Core2) coreCount++;
        if (SkillManager.Core3) coreCount++;

        // 直接输出变量值（不格式化）
        coreCountText.text = coreCount.ToString();

        // Debug窗口输出（仅在值改变或达到输出间隔时输出）
        if (enableDebugOutput && (coreCount != lastCoreCount || Time.frameCount % debugOutputInterval == 0))
        {
            Debug.Log($"GameInfoDisplay - Core数量: {coreCount} (Core1={SkillManager.Core1}, Core2={SkillManager.Core2}, Core3={SkillManager.Core3})");
            lastCoreCount = coreCount;
        }
    }

    /// <summary>
    /// 更新Boost剩余次数显示
    /// </summary>
    private void UpdateBoostRemainingDisplay()
    {
        if (boostRemainingText == null)
        {
            return;
        }

        // 根据Boost1的值控制Text的启用/禁用
        // Boost1=false (0) 时禁用Text，Boost1=true (1) 时启用Text
        bool shouldEnable = SkillManager.Boost1;
        
        // 控制Boost显示Text的启用/禁用
        if (boostRemainingText.gameObject.activeSelf != shouldEnable)
        {
            boostRemainingText.gameObject.SetActive(shouldEnable);
            
            if (enableDebugOutput)
            {
                Debug.Log($"GameInfoDisplay - Boost显示Text已{(shouldEnable ? "启用" : "禁用")} (Boost1={SkillManager.Boost1})");
            }
        }
        
        // 同时控制额外Text的显隐（如果已分配）
        if (additionalBoostText != null && additionalBoostText.gameObject.activeSelf != shouldEnable)
        {
            additionalBoostText.gameObject.SetActive(shouldEnable);
            
            if (enableDebugOutput)
            {
                Debug.Log($"GameInfoDisplay - 额外Boost Text已{(shouldEnable ? "启用" : "禁用")} (Boost1={SkillManager.Boost1})");
            }
        }

        // 如果Boost1为false，直接返回，不更新文本
        if (!shouldEnable)
        {
            return;
        }

        // 如果ShipBoostAim组件无效，尝试重新查找
        if (shipBoostAim == null)
        {
            // 只在运行时尝试重新查找（避免每帧都查找）
            if (Application.isPlaying && Time.frameCount % 60 == 0) // 每60帧尝试一次
            {
                InitializeShipBoostAim();
            }
            
            // 如果仍然找不到，显示默认文本
            if (shipBoostAim == null)
            {
                boostRemainingText.text = "N/A";
                return;
            }
        }

        // 计算Boost1+Boost2+Boost3的和（解锁数量）
        int boostCount = 0;
        if (SkillManager.Boost1) boostCount++;
        if (SkillManager.Boost2) boostCount++;
        if (SkillManager.Boost3) boostCount++;

        // 获取当前已使用次数
        int currentUses = shipBoostAim.GetCurrentUses();

        // 计算 Boost1+Boost2+Boost3-currentUses
        int boostValue = boostCount - currentUses;
        
        // 直接输出变量值（不格式化）
        boostRemainingText.text = boostValue.ToString();

        // Debug窗口输出（仅在值改变或达到输出间隔时输出）
        if (enableDebugOutput && (boostValue != lastBoostValue || Time.frameCount % debugOutputInterval == 0))
        {
            Debug.Log($"GameInfoDisplay - Boost值: {boostValue} (Boost1+Boost2+Boost3={boostCount}, currentUses={currentUses}, Boost1={SkillManager.Boost1}, Boost2={SkillManager.Boost2}, Boost3={SkillManager.Boost3})");
            lastBoostValue = boostValue;
        }
    }

    /// <summary>
    /// 手动设置飞船对象引用（可在运行时调用）
    /// </summary>
    /// <param name="spaceship">飞船GameObject</param>
    public void SetSpaceshipObject(GameObject spaceship)
    {
        spaceshipObject = spaceship;
        InitializeShipBoostAim();
    }

    /// <summary>
    /// 手动设置ShipBoostAim组件引用（可在运行时调用）
    /// </summary>
    /// <param name="boostAim">ShipBoostAim组件</param>
    public void SetShipBoostAim(ShipBoostAim boostAim)
    {
        shipBoostAim = boostAim;
        if (boostAim != null)
        {
            spaceshipObject = boostAim.gameObject;
        }
    }
}
