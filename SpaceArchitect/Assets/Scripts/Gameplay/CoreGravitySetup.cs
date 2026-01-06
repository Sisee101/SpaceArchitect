using UnityEngine;

/// <summary>
/// Core 引力设置辅助脚本
/// 确保 Core 通过 NBody 产生引力
/// </summary>
[RequireComponent(typeof(NBody))]
public class CoreGravitySetup : MonoBehaviour
{
    [Header("引力设置")]
    [Tooltip("Core 的引力强度（NBody.mass）\n推荐值：500-2000")]
    [SerializeField] private float gravityMass = 1000f;
    
    [Tooltip("是否固定（不会被其他引力移动）")]
    [SerializeField] private bool isFixed = true;
    
    [Header("调试")]
    [Tooltip("显示引力范围（Gizmos）")]
    [SerializeField] private bool showGravityRange = true;
    
    [Tooltip("显示的引力范围半径（仅用于可视化）")]
    [SerializeField] private float visualRange = 10f;
    
    private NBody nBody;
    private GravityEngine gravityEngine;
    
    void Awake()
    {
        // 获取或添加 NBody 组件
        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            nBody = gameObject.AddComponent<NBody>();
            Debug.Log($"[CoreGravitySetup] 已为 {gameObject.name} 添加 NBody 组件");
        }
        
        // 配置 NBody
        ConfigureNBody();
        
        // 确保 CoreDragger 的 produceGravity 为 true
        EnsureCoreDraggerProducesGravity();
    }
    
    void Start()
    {
        // 获取 GravityEngine
        gravityEngine = GravityEngine.Instance();
        if (gravityEngine == null)
        {
            Debug.LogError($"[CoreGravitySetup] {gameObject.name} 无法找到 GravityEngine！");
            return;
        }
        
        // 延迟验证和修复（确保 GravityEngine 完全初始化）
        Invoke(nameof(VerifyAndFixGravityRegistration), 0.5f);
    }
    
    /// <summary>
    /// 验证并修复 GravityEngine 注册
    /// </summary>
    private void VerifyAndFixGravityRegistration()
    {
        if (nBody == null || gravityEngine == null) return;
        
        // 检查是否已被添加到引擎
        if (nBody.engineRef != null)
        {
            Debug.Log($"<color=green>[CoreGravitySetup] ✓ {gameObject.name} 已被 GravityEngine 识别，正在产生引力</color>");
            Debug.Log($"<color=green>  → 引擎索引: {nBody.engineRef.index}, 质量: {nBody.mass}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[CoreGravitySetup] ⚠️ {gameObject.name} 未被 GravityEngine 识别！尝试手动添加...</color>");
            
            // 尝试手动添加到 GravityEngine
            TryManualAddToGravityEngine();
        }
    }
    
    /// <summary>
    /// 尝试手动添加到 GravityEngine
    /// </summary>
    private void TryManualAddToGravityEngine()
    {
        if (gravityEngine == null || nBody == null)
        {
            Debug.LogError($"[CoreGravitySetup] 无法手动添加：GravityEngine 或 NBody 为 null");
            return;
        }
        
        // 确保 NBody 组件启用
        if (!nBody.enabled)
        {
            nBody.enabled = true;
            Debug.Log($"[CoreGravitySetup] 已启用 {gameObject.name} 的 NBody 组件");
        }
        
        // 确保 GameObject 激活
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"[CoreGravitySetup] {gameObject.name} 未激活，无法添加到 GravityEngine");
            return;
        }
        
        try
        {
            // 手动添加到 GravityEngine
            gravityEngine.AddBody(gameObject);
            
            // 延迟验证
            Invoke(nameof(VerifyAfterManualAdd), 0.2f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CoreGravitySetup] 手动添加失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 手动添加后验证
    /// </summary>
    private void VerifyAfterManualAdd()
    {
        if (nBody.engineRef != null)
        {
            Debug.Log($"<color=green>[CoreGravitySetup] ✓ 手动添加成功！{gameObject.name} 现在正在产生引力</color>");
            Debug.Log($"<color=green>  → 引擎索引: {nBody.engineRef.index}, 质量: {nBody.mass}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[CoreGravitySetup] ✗ 手动添加失败！{gameObject.name} 仍未被识别</color>");
            Debug.LogError($"<color=red>请检查：</color>");
            Debug.LogError($"<color=red>  1. GravityEngine 是否正确配置？</color>");
            Debug.LogError($"<color=red>  2. Core GameObject 是否激活？</color>");
            Debug.LogError($"<color=red>  3. NBody 组件是否启用？</color>");
            Debug.LogError($"<color=red>  4. 是否有其他脚本干扰？</color>");
        }
    }
    
    /// <summary>
    /// 配置 NBody 组件
    /// </summary>
    private void ConfigureNBody()
    {
        if (nBody == null) return;
        
        // 设置质量（引力强度）
        nBody.mass = gravityMass;
        
        // 设置为静止
        nBody.vel = Vector3.zero;
        nBody.vel_phys = Vector3.zero;
        
        Debug.Log($"[CoreGravitySetup] {gameObject.name} NBody 配置完成 - mass: {gravityMass}");
    }
    
    /// <summary>
    /// 确保 CoreDragger 的 produceGravity 为 true
    /// </summary>
    private void EnsureCoreDraggerProducesGravity()
    {
        CoreDragger dragger = GetComponent<CoreDragger>();
        if (dragger != null)
        {
            if (!dragger.produceGravity)
            {
                Debug.LogWarning($"<color=yellow>[CoreGravitySetup] {gameObject.name} 的 CoreDragger.produceGravity 为 false，正在设置为 true...</color>");
                dragger.produceGravity = true;
            }
            Debug.Log($"[CoreGravitySetup] {gameObject.name} CoreDragger.produceGravity = true ✓");
        }
    }
    
    /// <summary>
    /// 在运行时强制重新注册到 GravityEngine
    /// </summary>
    [ContextMenu("强制重新注册到引擎")]
    public void ForceReregisterToEngine()
    {
        if (gravityEngine == null)
        {
            gravityEngine = GravityEngine.Instance();
        }
        
        if (gravityEngine == null)
        {
            Debug.LogError($"[CoreGravitySetup] 无法找到 GravityEngine");
            return;
        }
        
        // 如果已经注册，先移除
        if (nBody.engineRef != null)
        {
            Debug.Log($"[CoreGravitySetup] 先移除现有注册...");
            gravityEngine.RemoveBody(gameObject);
        }
        
        // 重新添加
        Debug.Log($"[CoreGravitySetup] 重新注册 {gameObject.name} 到 GravityEngine...");
        TryManualAddToGravityEngine();
    }
    
    /// <summary>
    /// 在 Inspector 中修改参数时更新
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying && nBody != null)
        {
            nBody.mass = gravityMass;
        }
    }
    
    /// <summary>
    /// 绘制引力范围（调试用）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGravityRange) return;
        
        // 绘制引力影响范围（可视化）
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f); // 半透明青绿色
        Gizmos.DrawWireSphere(transform.position, visualRange);
        
        // 绘制更强的引力区域
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, visualRange * 0.5f);
        
        // 绘制核心
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
    
    /// <summary>
    /// 设置引力强度
    /// </summary>
    public void SetGravityMass(float mass)
    {
        gravityMass = mass;
        if (nBody != null)
        {
            nBody.mass = mass;
        }
    }
    
    /// <summary>
    /// 获取引力强度
    /// </summary>
    public float GetGravityMass()
    {
        return nBody != null ? nBody.mass : gravityMass;
    }
    
    /// <summary>
    /// 在 Inspector 中显示信息
    /// </summary>
    [ContextMenu("显示引力信息")]
    void ShowGravityInfo()
    {
        if (gravityEngine == null)
        {
            gravityEngine = GravityEngine.Instance();
        }
        
        if (nBody == null)
        {
            Debug.LogWarning($"{gameObject.name} 没有 NBody 组件");
            return;
        }
        
        Debug.Log($"=== {gameObject.name} 引力信息 ===");
        Debug.Log($"GameObject 激活: {gameObject.activeInHierarchy}");
        Debug.Log($"NBody 启用: {nBody.enabled}");
        Debug.Log($"质量: {nBody.mass}");
        Debug.Log($"速度: {nBody.vel}");
        
        if (nBody.engineRef != null)
        {
            Debug.Log($"<color=green>已注册到引擎: 是 ✓</color>");
            Debug.Log($"  → 引擎索引: {nBody.engineRef.index}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>已注册到引擎: 否 ✗</color>");
            Debug.LogWarning($"  → Core 不会对飞船产生引力！");
        }
        
        if (gravityEngine != null)
        {
            Debug.Log($"GravityEngine 存在: 是");
            Debug.Log($"GravityEngine 质量缩放: {gravityEngine.massScale}");
            Debug.Log($"有效质量: {nBody.mass * gravityEngine.massScale}");
        }
        else
        {
            Debug.LogError($"GravityEngine 存在: 否 ✗");
        }
        
        Debug.Log($"=====================================");
    }
}

