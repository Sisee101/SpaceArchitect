using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 飞船过热处理脚本
/// 处理飞船过热失败的视觉效果：变红 -> 爆炸特效 -> 模型消失
/// </summary>
[RequireComponent(typeof(ShipState))]
public class ShipOverheat : MonoBehaviour
{
    [Header("视觉效果设置")]
    [Tooltip("变红效果的持续时间（秒）")]
    [SerializeField] private float redEffectDuration = 1f;
    
    [Tooltip("爆炸特效Prefab（可选）")]
    [SerializeField] private GameObject explosionParticlesPrefab;
    
    [Tooltip("过热时的粒子效果Prefab（可选，用于变红阶段）")]
    [SerializeField] private GameObject overheatParticlesPrefab;
    
    [Header("渲染器设置")]
    [Tooltip("飞船模型的渲染器（如果为空，将自动查找）")]
    [SerializeField] private Renderer[] shipRenderers;
    
    [Header("变红效果方式")]
    [Tooltip("变红效果模式：使用纹理（更明显）或使用颜色修改（动态）")]
    [SerializeField] private OverheatMode overheatMode = OverheatMode.UseTexture;
    
    [Tooltip("变红时的红色纹理（Texture2D，如果设置了则使用纹理替换，效果更明显）")]
    [SerializeField] private Texture2D overheatTexture;
    
    [Tooltip("变红时的颜色（仅在使用颜色修改模式时有效）")]
    [SerializeField] private Color overheatColor = new Color(1f, 0.1f, 0.1f, 1f); // 更鲜艳的红色
    
    [Tooltip("Emission强度倍数（值越大越亮）")]
    [SerializeField] private float emissionIntensity = 8f; // 增加发光强度
    
    [Tooltip("是否添加闪烁效果（让红色更动态明显）")]
    [SerializeField] private bool enableFlicker = true;
    
    [Tooltip("闪烁频率（每秒闪烁次数，初始值）")]
    [SerializeField] private float flickerFrequency = 5f;
    
    [Tooltip("闪烁频率最大倍数（到达爆炸时闪烁速度的倍数）")]
    [SerializeField] private float maxFlickerMultiplier = 10f; // 最终闪烁频率会是初始值的10倍
    
    [Header("炫光效果设置")]
    [Tooltip("是否启用炫光效果（Bloom风格的发光）")]
    [SerializeField] private bool enableBloomGlow = true;
    
    [Tooltip("炫光大小（相对于飞船的缩放倍数）")]
    [SerializeField] private float glowSize = 2f;
    
    [Tooltip("炫光材质（如果为空，将自动创建）")]
    [SerializeField] private Material glowMaterial;
    
    [Tooltip("炫光颜色")]
    [SerializeField] private Color glowColor = new Color(1f, 0.2f, 0.1f, 1f); // 红色炫光
    
    [Tooltip("炫光最大强度（Emission强度）")]
    [SerializeField] private float glowMaxIntensity = 15f;
    
    [Header("UI警告设置")]
    [Tooltip("UI警告文本（如果为空则自动创建）")]
    [SerializeField] private UnityEngine.UI.Text warningText;
    
    [Tooltip("警告文本内容")]
    [SerializeField] private string warningMessage = "警告！飞船过热！";
    
    [Tooltip("警告文本显示持续时间（秒，0表示一直显示直到离开加热范围）")]
    [SerializeField] private float warningDisplayDuration = 0f;
    
    [Tooltip("警告文本字体大小")]
    [SerializeField] private int warningFontSize = 36;
    
    [Tooltip("警告文本颜色")]
    [SerializeField] private Color warningTextColor = Color.red;
    
    [Tooltip("原始材质（用于恢复，如果为空则自动保存）")]
    [SerializeField] private Material[] originalMaterials;
    
    [Tooltip("变红材质（可选，如果为空则动态修改颜色或纹理）")]
    [SerializeField] private Material[] redMaterials;
    
    /// <summary>
    /// 变红效果模式枚举
    /// </summary>
    public enum OverheatMode
    {
        UseTexture,      // 使用纹理替换（效果更明显）
        UseColorModify   // 使用颜色修改（动态渐变）
    }
    
    private Texture2D[] originalMainTextures; // 保存原始主纹理
    private Texture2D[] originalEmissionTextures; // 保存原始Emission纹理
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLog = true;
    
    private ShipState shipState;
    private bool hasOverheated = false;
    private bool isOverheating = false; // 是否正在过热（变红闪烁状态）
    private Coroutine overheatEffectCoroutine;
    private Coroutine flickerCoroutine; // 闪烁协程（持续闪烁到爆炸）
    private MaterialPropertyBlock[] materialPropertyBlocks; // 用于修改材质属性而不创建实例
    private GameObject warningTextObject; // UI警告文本对象
    private float totalOverheatTime = 3f; // 总过热时间（从开始过热到爆炸）
    private float overheatStartTime = 0f; // 开始过热的时间（用于计算闪烁进度）
    private GameObject glowObject; // 炫光GameObject
    private Renderer glowRenderer; // 炫光渲染器
    private Material glowMaterialInstance; // 炫光材质实例
    private GameObject overheatParticlesInstance; // 过热粒子系统实例（需要保存引用，避免爆炸时被销毁）
    private ParticleSystem overheatParticleSystem; // 过热粒子系统组件引用
    
    void Awake()
    {
        // 获取ShipState组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError($"ShipOverheat: {gameObject.name} 缺少 ShipState 组件！");
        }
        
        // 如果没有指定渲染器，自动查找（只查找飞船自己的渲染器，排除不相关的对象）
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            // 获取飞船及其直接子对象中的渲染器
            // 注意：只获取属于飞船的渲染器，不获取其他GameObject的渲染器
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();
            
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer != null)
                {
                    // 验证渲染器是否真的属于飞船（渲染器的transform应该是飞船本身或其子对象）
                    Transform rendererTransform = renderer.transform;
                    if (rendererTransform == transform || rendererTransform.IsChildOf(transform))
                    {
                        validRenderers.Add(renderer);
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"ShipOverheat: 找到飞船渲染器: {renderer.gameObject.name}");
                        }
                    }
                    else if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 跳过不属于飞船的渲染器: {renderer.gameObject.name} (不属于 {gameObject.name})");
                    }
                }
            }
            
            shipRenderers = validRenderers.ToArray();
            
            if (showDebugLog)
            {
                Debug.Log($"ShipOverheat: 自动查找完成，找到 {shipRenderers.Length} 个飞船渲染器");
            }
        }
        else
        {
            // 如果手动指定了渲染器，验证它们是否真的属于飞船
            System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in shipRenderers)
            {
                if (renderer != null)
                {
                    Transform rendererTransform = renderer.transform;
                    if (rendererTransform == transform || rendererTransform.IsChildOf(transform))
                    {
                        validRenderers.Add(renderer);
                    }
                    else if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 警告！手动指定的渲染器 {renderer.gameObject.name} 不属于飞船 {gameObject.name}，将被忽略");
                    }
                }
            }
            
            if (validRenderers.Count != shipRenderers.Length)
            {
                shipRenderers = validRenderers.ToArray();
                if (showDebugLog)
                {
                    Debug.LogWarning($"ShipOverheat: 已过滤掉不属于飞船的渲染器，有效渲染器数量: {shipRenderers.Length}");
                }
            }
        }
        
        // 初始化 MaterialPropertyBlock 数组
        if (shipRenderers != null && shipRenderers.Length > 0)
        {
            if (materialPropertyBlocks == null || materialPropertyBlocks.Length != shipRenderers.Length)
            {
                materialPropertyBlocks = new MaterialPropertyBlock[shipRenderers.Length];
                for (int j = 0; j < materialPropertyBlocks.Length; j++)
                {
                    materialPropertyBlocks[j] = new MaterialPropertyBlock();
                    // 获取当前 PropertyBlock（如果已有）
                    if (shipRenderers[j] != null)
                    {
                        shipRenderers[j].GetPropertyBlock(materialPropertyBlocks[j]);
                    }
                }
            }
            
            // 保存原始材质状态（从 sharedMaterial 保存，确保是原始值）
            SaveOriginalMaterialState();
        }
        
        // ✅ 创建炫光效果（Bloom风格的发光）
        if (enableBloomGlow)
        {
            CreateBloomGlow();
        }
    }
    
    void Start()
    {
        // 延迟订阅事件，确保EventManager已经初始化
        SubscribeToEvents();
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: Start() 完成 - 飞船: {gameObject.name}, 渲染器数量: {shipRenderers?.Length ?? 0}, EventManager: {EventManager.Instance != null}");
        }
    }
    
    void OnEnable()
    {
        // 订阅开始过热和过热完成事件
        SubscribeToEvents();
        
        // 重新保存原始材质状态（防止之前修改过的材质实例影响恢复）
        // 这确保每次启用时，我们都有正确的原始材质参考
        if (shipRenderers != null && shipRenderers.Length > 0)
        {
            SaveOriginalMaterialState();
        }
    }
    
    /// <summary>
    /// 创建炫光效果（Bloom风格的发光）
    /// </summary>
    private void CreateBloomGlow()
    {
        // 如果已经存在，先销毁
        if (glowObject != null)
        {
            Destroy(glowObject);
        }
        
        // 创建一个Quad作为炫光载体
        glowObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glowObject.name = "ShipOverheatGlow";
        glowObject.transform.SetParent(transform);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one * glowSize;
        
        // 移除Collider（不需要碰撞）
        Collider collider = glowObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // 获取渲染器
        glowRenderer = glowObject.GetComponent<Renderer>();
        if (glowRenderer == null)
        {
            Debug.LogError("ShipOverheat: 无法创建炫光渲染器！");
            return;
        }
        
        // 创建或使用指定的材质
        if (glowMaterial != null)
        {
            glowMaterialInstance = new Material(glowMaterial);
        }
        else
        {
            // ✅ 创建一个默认的自发光材质（使用 Unlit Shader，效果更明显）
            // 先尝试 URP Unlit
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
            {
                // 如果找不到 URP Unlit，尝试 Standard Unlit
                unlitShader = Shader.Find("Unlit/Transparent");
            }
            if (unlitShader == null)
            {
                // 最后尝试 Standard
                unlitShader = Shader.Find("Standard");
            }
            
            if (unlitShader != null)
            {
                glowMaterialInstance = new Material(unlitShader);
                
                // 设置颜色（半透明，让发光效果更明显）
                glowMaterialInstance.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
                
                // 如果是 URP Unlit，可能不支持 Emission，使用颜色直接发光
                // 如果是 Standard，设置 Emission
                if (glowMaterialInstance.HasProperty("_EmissionColor"))
                {
                    glowMaterialInstance.SetColor("_EmissionColor", glowColor * glowMaxIntensity);
                    glowMaterialInstance.EnableKeyword("_EMISSION");
                    if (glowMaterialInstance.HasProperty("_EmissionEnabled"))
                    {
                        glowMaterialInstance.SetFloat("_EmissionEnabled", 1f);
                    }
                }
                else
                {
                    // 如果没有 Emission，使用颜色本身来发光（提高颜色强度）
                    glowMaterialInstance.color = glowColor * glowMaxIntensity * 0.5f;
                    glowMaterialInstance.color = new Color(glowMaterialInstance.color.r, glowMaterialInstance.color.g, glowMaterialInstance.color.b, 0.8f);
                }
            }
            else
            {
                Debug.LogError("ShipOverheat: 无法找到合适的Shader来创建炫光材质！");
                return;
            }
        }
        
        // 应用材质
        glowRenderer.material = glowMaterialInstance;
        
        // 初始隐藏（只有在过热时才显示）
        glowObject.SetActive(false);
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: ✅ 炫光效果已创建 - 大小: {glowSize}, 颜色: {glowColor}, 最大强度: {glowMaxIntensity}");
        }
    }
    
    /// <summary>
    /// 保存原始材质状态（从 sharedMaterial 保存，确保是原始值）
    /// </summary>
    private void SaveOriginalMaterialState()
    {
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            return;
        }
        
        // 确保数组已初始化
        if (originalMaterials == null || originalMaterials.Length != shipRenderers.Length)
        {
            originalMaterials = new Material[shipRenderers.Length];
        }
        if (originalMainTextures == null || originalMainTextures.Length != shipRenderers.Length)
        {
            originalMainTextures = new Texture2D[shipRenderers.Length];
        }
        if (originalEmissionTextures == null || originalEmissionTextures.Length != shipRenderers.Length)
        {
            originalEmissionTextures = new Texture2D[shipRenderers.Length];
        }
        
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null)
            {
                // 验证渲染器是否属于飞船
                Transform rendererTransform = shipRenderers[i].transform;
                if (rendererTransform != transform && !rendererTransform.IsChildOf(transform))
                {
                    continue;
                }
                
                // 保存 sharedMaterial（共享材质，不会被修改，这是真正的原始材质）
                Material sharedMat = shipRenderers[i].sharedMaterial;
                if (sharedMat != null)
                {
                    originalMaterials[i] = sharedMat;
                    
                    // 从 sharedMaterial 保存原始纹理（确保是原始值，不是被修改过的实例值）
                    if (sharedMat.HasProperty("_MainTex"))
                    {
                        originalMainTextures[i] = sharedMat.GetTexture("_MainTex") as Texture2D;
                    }
                    else if (sharedMat.HasProperty("_BaseMap"))
                    {
                        originalMainTextures[i] = sharedMat.GetTexture("_BaseMap") as Texture2D;
                    }
                    
                    // 保存原始 Emission 纹理
                    if (sharedMat.HasProperty("_EmissionMap"))
                    {
                        originalEmissionTextures[i] = sharedMat.GetTexture("_EmissionMap") as Texture2D;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 订阅事件（统一管理）
    /// </summary>
    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            // 先取消订阅，避免重复订阅
            EventManager.Instance.OnShipStartOverheating -= HandleShipStartOverheating;
            EventManager.Instance.OnShipOverheated -= HandleShipOverheated;
            
            // 重新订阅
            EventManager.Instance.OnShipStartOverheating += HandleShipStartOverheating;
            EventManager.Instance.OnShipOverheated += HandleShipOverheated;
            EventManager.Instance.OnGameReset += HandleGameReset; // 订阅游戏重置事件
            
            if (showDebugLog)
            {
                Debug.Log($"ShipOverheat: 事件订阅成功 - 飞船: {gameObject.name}");
            }
        }
        else if (showDebugLog)
        {
            Debug.LogWarning($"ShipOverheat: EventManager 实例为空，事件订阅失败 - 飞船: {gameObject.name}");
        }
    }
    
    void OnDisable()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShipStartOverheating -= HandleShipStartOverheating;
            EventManager.Instance.OnShipOverheated -= HandleShipOverheated;
            EventManager.Instance.OnGameReset -= HandleGameReset; // 取消订阅游戏重置事件
        }
        
        // ✅ 隐藏炫光效果
        if (glowObject != null)
        {
            glowObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // ✅ 清理炫光对象
        if (glowObject != null)
        {
            Destroy(glowObject);
            glowObject = null;
        }
        
        // ✅ 清理炫光材质实例
        if (glowMaterialInstance != null)
        {
            Destroy(glowMaterialInstance);
            glowMaterialInstance = null;
        }
        
        // ✅ 清理UI警告对象
        if (warningTextObject != null)
        {
            Destroy(warningTextObject);
            warningTextObject = null;
        }
    }
    
    /// <summary>
    /// 处理飞船开始过热事件（进入加热范围，立即开始变红效果）
    /// </summary>
    /// <param name="firePlanet">火行星GameObject</param>
    /// <param name="ship">飞船GameObject</param>
    private void HandleShipStartOverheating(GameObject firePlanet, GameObject ship)
    {
        // 调试信息：检查事件是否被接收
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 收到开始过热事件 - 目标飞船: {ship?.name}, 当前飞船: {gameObject.name}, 匹配: {ship == gameObject}, 正在过热: {isOverheating}, 已过热: {hasOverheated}");
        }
        
        // 只处理自己的飞船事件
        if (ship != gameObject)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipOverheat: 事件目标飞船({ship?.name})与当前飞船({gameObject.name})不匹配，忽略事件");
            }
            return;
        }
        
        if (isOverheating || hasOverheated)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipOverheat: 飞船已经在过热状态（isOverheating={isOverheating}, hasOverheated={hasOverheated}），忽略重复事件");
            }
            return;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 飞船 {gameObject.name} 进入加热范围，立即开始变红效果！火行星: {firePlanet?.name}");
        }
        
        // 标记为正在过热（变红）
        isOverheating = true;
        
        // 记录开始过热的时间（用于计算闪烁进度）
        overheatStartTime = Time.unscaledTime;
        
        // 停止之前的协程（如果有）
        if (overheatEffectCoroutine != null)
        {
            StopCoroutine(overheatEffectCoroutine);
        }
        
        // 立即开始变红效果（不等待）
        overheatEffectCoroutine = StartCoroutine(StartOverheatingEffect());
    }
    
    /// <summary>
    /// 处理飞船过热完成事件（3秒后，触发爆炸）
    /// </summary>
    /// <param name="firePlanet">火行星GameObject</param>
    /// <param name="ship">飞船GameObject</param>
    private void HandleShipOverheated(GameObject firePlanet, GameObject ship)
    {
        // 只处理自己的飞船过热完成事件
        if (ship != gameObject || hasOverheated)
        {
            return;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 飞船 {gameObject.name} 过热完成，触发爆炸！火行星: {firePlanet.name}");
        }
        
        // 标记为已过热完成（但不要停止闪烁，让闪烁持续到爆炸特效播放）
        hasOverheated = true;
        // 注意：不要在这里设置 isOverheating = false，让闪烁继续到爆炸特效开始
        
        // 停止之前的协程（如果有）
        if (overheatEffectCoroutine != null)
        {
            StopCoroutine(overheatEffectCoroutine);
        }
        
        // 触发爆炸效果序列（在爆炸特效序列中才会停止闪烁）
        overheatEffectCoroutine = StartCoroutine(ExplosionEffectSequence());
    }
    
    /// <summary>
    /// 处理游戏重置事件（重置材质状态）
    /// </summary>
    private void HandleGameReset()
    {
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 收到游戏重置事件，重置材质状态 - 飞船: {gameObject.name}");
        }
        
        // ✅ 先重置状态标志（防止协程继续执行）
        isOverheating = false;
        hasOverheated = false;
        overheatStartTime = 0f;
        
        // ✅ 停止所有协程（必须在重置状态标志之后，确保协程能检测到状态变化）
        if (overheatEffectCoroutine != null)
        {
            StopCoroutine(overheatEffectCoroutine);
            overheatEffectCoroutine = null;
        }
        
        // ✅ 停止闪烁协程
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        
        // ✅ 隐藏UI警告
        HideWarningUI();
        
        // ✅ 隐藏炫光效果
        if (glowObject != null)
        {
            glowObject.SetActive(false);
            if (glowMaterialInstance != null)
            {
                // 重置炫光强度
                glowMaterialInstance.SetColor("_EmissionColor", glowColor * 0f);
            }
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 重置时隐藏炫光效果");
            }
        }
        
        // ✅ 停止并销毁过热粒子系统
        if (overheatParticleSystem != null && overheatParticleSystem.isPlaying)
        {
            overheatParticleSystem.Stop();
        }
        if (overheatParticlesInstance != null)
        {
            Destroy(overheatParticlesInstance);
            overheatParticlesInstance = null;
            overheatParticleSystem = null;
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 重置时已销毁过热粒子系统");
            }
        }
        
        // ✅ 立即恢复原始材质状态（清除PropertyBlock，恢复原始纹理和颜色）
        // 不需要等待协程完成，直接重置
        ResetOverheatState();
        
        // ✅ 重新保存原始材质状态（确保下次过热时使用正确的原始值）
        SaveOriginalMaterialState();
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: ✅ 游戏重置完成 - 状态已重置，所有协程已停止，材质已恢复，炫光已隐藏，粒子系统已清理，原始状态已重新保存");
        }
    }
    
    /// <summary>
    /// 开始过热效果（立即变红，发红光）
    /// </summary>
    private IEnumerator StartOverheatingEffect()
    {
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 开始变红效果（texture变红，发红光） - 模式: {overheatMode}, 有纹理: {overheatTexture != null}, 渲染器数量: {shipRenderers?.Length ?? 0}");
        }
        
        // 检查渲染器
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipOverheat: 没有找到渲染器组件，无法应用变红效果！请确保飞船有MeshRenderer或SkinnedMeshRenderer组件。");
            }
            yield break;
        }
        
        // 显示UI警告
        ShowWarningUI();
        
        // ✅ 启用炫光效果（与闪烁同步）
        if (enableBloomGlow && glowObject != null)
        {
            glowObject.SetActive(true);
            // 让炫光始终面向摄像机（Billboard效果）
            if (glowRenderer != null && glowMaterialInstance != null)
            {
                // 初始设置炫光强度为0，让闪烁协程来控制
                glowMaterialInstance.SetColor("_EmissionColor", glowColor * 0f);
            }
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 炫光效果已启用");
            }
        }
        
        // ✅ 播放过热粒子效果（可选）- 保存引用，避免爆炸时被销毁
        if (overheatParticlesPrefab != null)
        {
            // 如果已经存在，先停止并销毁旧的
            if (overheatParticlesInstance != null)
            {
                if (overheatParticleSystem != null)
                {
                    overheatParticleSystem.Stop();
                }
                Destroy(overheatParticlesInstance);
            }
            
            // 创建新的粒子系统实例
            overheatParticlesInstance = Instantiate(overheatParticlesPrefab, transform.position, Quaternion.identity);
            overheatParticlesInstance.transform.SetParent(transform);
            
            // 获取粒子系统组件
            overheatParticleSystem = overheatParticlesInstance.GetComponent<ParticleSystem>();
            if (overheatParticleSystem == null)
            {
                overheatParticleSystem = overheatParticlesInstance.GetComponentInChildren<ParticleSystem>();
            }
            
            if (overheatParticleSystem != null)
            {
                // ✅ 配置粒子系统使用未缩放时间（确保时停时也能正常播放）
                var main = overheatParticleSystem.main;
                main.useUnscaledTime = true;
                
                // 播放粒子系统
                overheatParticleSystem.Play();
                
                if (showDebugLog)
                {
                    Debug.Log($"ShipOverheat: ✅ 过热粒子效果已播放 - 使用未缩放时间: {main.useUnscaledTime}");
                }
            }
            else if (showDebugLog)
            {
                Debug.LogWarning("ShipOverheat: 过热粒子特效Prefab中没有找到ParticleSystem组件！");
            }
        }
        else if (showDebugLog)
        {
            Debug.LogWarning("ShipOverheat: 未设置过热粒子特效Prefab，粒子效果将不显示");
        }
        
        // ✅ 立即应用红色效果并启动闪烁（不等待渐变完成）
        if (overheatMode == OverheatMode.UseTexture && overheatTexture != null)
        {
            // 立即应用红色纹理
            ApplyRedTextureImmediate();
        }
        else
        {
            // 立即应用红色颜色
            ApplyRedColorImmediate();
        }
        
        // ✅ 立即启动持续闪烁协程（在变红效果开始时就启动，不等待redEffectDuration）
        // 这样闪烁会立即开始，并在整个过热过程中持续闪烁到爆炸
        if (isOverheating)
        {
            // 确保闪烁协程没有被启动过，或者先停止旧的
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            
            // 立即启动持续闪烁协程
            flickerCoroutine = StartCoroutine(ContinuousFlickerCoroutine());
            
            if (showDebugLog)
            {
                Debug.Log($"ShipOverheat: ✅ 持续闪烁协程已立即启动！overheatStartTime={overheatStartTime}, totalOverheatTime={totalOverheatTime}, 将一直闪烁到爆炸");
            }
        }
        else if (showDebugLog)
        {
            Debug.LogWarning($"ShipOverheat: ❌ 未启动闪烁协程 - isOverheating={isOverheating}（应该为 true）");
        }
        
        // ❌ 移除渐变协程，让闪烁协程成为主要效果
        // 闪烁协程会直接更新 PropertyBlock，效果更明显
        // 如果保留渐变协程，它可能会干扰闪烁效果，并且会导致重置时材质状态不一致
        
        // 闪烁协程已经在上面启动了，它会持续运行直到爆炸或重置
        // 不需要额外的渐变协程
    }
    
    /// <summary>
    /// 持续闪烁协程（频率逐渐加快，一直闪烁到爆炸）
    /// </summary>
    private IEnumerator ContinuousFlickerCoroutine()
    {
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 🔥 开始持续闪烁协程（频率从 {flickerFrequency}Hz 逐渐加快到 {flickerFrequency * maxFlickerMultiplier}Hz）");
        }
        
        // 持续闪烁，直到爆炸特效开始或取消过热
        // 注意：isOverheating 在 HandleShipOverheated 中不会被设置为 false，
        // 只有在 ExplosionEffectSequence 中才会被设置为 false，这样闪烁会一直持续到爆炸特效开始
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 🔥 持续闪烁协程开始运行，isOverheating={isOverheating}, overheatStartTime={overheatStartTime}, totalOverheatTime={totalOverheatTime}");
        }
        
        int frameCount = 0;
        while (isOverheating)
        {
            // 如果还没有开始过热（overheatStartTime 为 0），等待
            if (overheatStartTime <= 0f)
            {
                if (showDebugLog)
                {
                    Debug.LogWarning("ShipOverheat: overheatStartTime 为 0，等待中...");
                }
                yield return null;
                continue;
            }
            
            // 从开始过热到现在的时间
            float elapsed = Time.unscaledTime - overheatStartTime;
            float t = Mathf.Clamp01(elapsed / totalOverheatTime); // 0到1，表示过热进度
            
            // 如果已经过热完成，使用最大闪烁频率（最快的闪烁）
            if (hasOverheated)
            {
                t = 1f; // 强制使用最大进度，最快的闪烁
            }
            
            // 计算当前闪烁频率（逐渐加快）
            // 从初始频率逐渐增加到 maxFlickerMultiplier 倍
            float currentFlickerFrequency = flickerFrequency * (1f + t * (maxFlickerMultiplier - 1f));
            
            // ✅ 计算闪烁值（正弦波，0到1之间）- 增强闪烁幅度，让效果更明显
            float flickerValue = Mathf.Sin(Time.unscaledTime * currentFlickerFrequency * Mathf.PI * 2f) * 0.5f + 0.5f; // 0 到 1
            // ✅ 调整闪烁范围，让效果更明显（0.0 到 1.0，从完全不亮到最亮）
            float flickerStrength = flickerValue; // 直接使用 0 到 1 的范围
            
            // ✅ 增强闪烁对比度：让暗的部分更暗（接近0），亮的部分更亮（接近1）
            // 使用平方函数增强对比度：flickerValue^2 会让低值更低，高值保持不变
            // 然后再映射到更宽的范围内，让闪烁更明显
            float flickerStrengthEnhanced = Mathf.Pow(flickerValue, 1.5f); // 使用1.5次幂，让低值更低，高值稍微降低但保持较高
            
            // 应用到所有渲染器
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null && i < materialPropertyBlocks.Length && materialPropertyBlocks[i] != null)
                {
                    MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                    Material sharedMat = shipRenderers[i].sharedMaterial;
                    
                    if (sharedMat != null)
                    {
                        // ✅ 计算闪烁颜色（用于颜色模式）- 使用增强的闪烁强度
                        Color flickerColor = Color.white;
                        if (overheatMode == OverheatMode.UseColorModify)
                        {
                            // 在暗红色和亮红色之间闪烁，让效果更明显
                            Color darkRed = new Color(overheatColor.r * 0.3f, overheatColor.g * 0.3f, overheatColor.b * 0.3f, overheatColor.a);
                            flickerColor = Color.Lerp(darkRed, overheatColor, flickerStrengthEnhanced);
                        }
                        
                        // ✅ 获取材质实例，直接修改材质实例（让闪烁在Inspector中可见）
                        Material instanceMat = shipRenderers[i].material;
                        if (instanceMat != null)
                        {
                            // ✅ 直接修改材质实例的纹理或颜色（让闪烁在Inspector中可见）
                            if (overheatMode == OverheatMode.UseTexture && overheatTexture != null)
                            {
                                // 纹理模式：设置纹理，并让纹理的颜色 tint 闪烁（让闪烁更明显）
                                if (instanceMat.HasProperty("_MainTex"))
                                {
                                    instanceMat.SetTexture("_MainTex", overheatTexture);
                                }
                                if (instanceMat.HasProperty("_BaseMap"))
                                {
                                    instanceMat.SetTexture("_BaseMap", overheatTexture);
                                }
                                
                                // ✅ 让纹理的颜色 tint 也闪烁（如果材质支持），让闪烁在Inspector中可见
                                // 在深红色和白色之间闪烁，让效果非常明显
                                Color darkRed = new Color(0.2f, 0f, 0f, 1f); // 很暗的红色
                                Color brightRed = Color.white; // 白色（让纹理完全显示）
                                Color textureTint = Color.Lerp(darkRed, brightRed, flickerStrength);
                                
                                if (instanceMat.HasProperty("_Color"))
                                {
                                    instanceMat.color = textureTint;
                                }
                                if (instanceMat.HasProperty("_BaseColor"))
                                {
                                    instanceMat.SetColor("_BaseColor", textureTint);
                                }
                            }
                            else
                            {
                                // 颜色模式：直接设置颜色到材质实例（闪烁效果）
                                if (instanceMat.HasProperty("_Color"))
                                {
                                    instanceMat.color = flickerColor;
                                }
                                if (instanceMat.HasProperty("_BaseColor"))
                                {
                                    instanceMat.SetColor("_BaseColor", flickerColor);
                                }
                            }
                            
                            // ✅ 应用闪烁效果到Emission（直接修改材质实例，让闪烁在Inspector中可见）
                            // ✅ 使用增强的闪烁强度，让闪烁更明显
                            if (instanceMat.HasProperty("_EmissionColor"))
                            {
                                Color emissionColor;
                                
                                if (overheatMode == OverheatMode.UseTexture && overheatTexture != null)
                                {
                                    // 纹理模式：白色Emission闪烁，从完全关闭到最亮（让闪烁非常明显）
                                    // ✅ 使用更大的闪烁范围：从完全黑色（0.0）到非常亮（emissionIntensity * 2），让闪烁非常明显
                                    float minEmission = 0.0f; // 完全关闭（黑色），让闪烁更明显
                                    float maxEmission = emissionIntensity * 1.5f; // 更亮的最大值，让闪烁对比更明显
                                    float currentEmissionIntensity = Mathf.Lerp(minEmission, maxEmission, flickerStrength);
                                    emissionColor = Color.white * currentEmissionIntensity;
                                }
                                else
                                {
                                    // 颜色模式：红色Emission闪烁，从完全关闭到最亮
                                    float minEmission = 0.0f; // 完全关闭（黑色）
                                    float maxEmission = emissionIntensity * 1.5f; // 更亮的最大值
                                    float currentEmissionIntensity = Mathf.Lerp(minEmission, maxEmission, flickerStrength);
                                    emissionColor = overheatColor * currentEmissionIntensity;
                                }
                                
                                // ✅ 直接设置到材质实例（这样在Inspector中也能看到闪烁）
                                instanceMat.SetColor("_EmissionColor", emissionColor);
                                
                                // ✅ 确保Emission关键字已启用
                                if (!instanceMat.IsKeywordEnabled("_EMISSION"))
                                {
                                    instanceMat.EnableKeyword("_EMISSION");
                                }
                                if (instanceMat.HasProperty("_EmissionEnabled"))
                                {
                                    instanceMat.SetFloat("_EmissionEnabled", 1f);
                                }
                                
                                // 设置Emission纹理（仅纹理模式）
                                if (overheatMode == OverheatMode.UseTexture && overheatTexture != null && instanceMat.HasProperty("_EmissionMap"))
                                {
                                    instanceMat.SetTexture("_EmissionMap", overheatTexture);
                                }
                                
                                // ✅ 添加调试日志（仅在特定条件下）- 帮助调试闪烁效果
                                // 只在第一个渲染器上输出，避免重复日志
                                if (i == 0 && showDebugLog && frameCount % 60 == 0) // 每60次迭代输出一次，避免日志过多
                                {
                                    Debug.Log($"ShipOverheat: 🔥 闪烁效果 - flickerValue={flickerValue:F3}, flickerStrength={flickerStrength:F3}, Emission强度={emissionColor.maxColorComponent:F1}, 模式={overheatMode}, 频率={currentFlickerFrequency:F1}Hz");
                                }
                            }
                        }
                        
                        // ✅ 同时也更新PropertyBlock（作为备份，确保效果覆盖所有情况）
                        if (overheatMode == OverheatMode.UseTexture && overheatTexture != null)
                        {
                            if (sharedMat.HasProperty("_MainTex"))
                            {
                                propBlock.SetTexture("_MainTex", overheatTexture);
                            }
                            if (sharedMat.HasProperty("_BaseMap"))
                            {
                                propBlock.SetTexture("_BaseMap", overheatTexture);
                            }
                        }
                        else
                        {
                            if (sharedMat.HasProperty("_Color"))
                            {
                                propBlock.SetColor("_Color", flickerColor);
                            }
                            if (sharedMat.HasProperty("_BaseColor"))
                            {
                                propBlock.SetColor("_BaseColor", flickerColor);
                            }
                        }
                        
                        // ✅ 同时也更新PropertyBlock的Emission（保持一致性）
                        if (sharedMat.HasProperty("_EmissionColor"))
                        {
                            Color emissionColor;
                            
                            if (overheatMode == OverheatMode.UseTexture && overheatTexture != null)
                            {
                                emissionColor = Color.white * (emissionIntensity * flickerStrength);
                            }
                            else
                            {
                                emissionColor = overheatColor * (emissionIntensity * flickerStrength);
                            }
                            
                            propBlock.SetColor("_EmissionColor", emissionColor);
                            
                            if (overheatMode == OverheatMode.UseTexture && overheatTexture != null && sharedMat.HasProperty("_EmissionMap"))
                            {
                                propBlock.SetTexture("_EmissionMap", overheatTexture);
                            }
                        }
                        
                        // ✅ 应用PropertyBlock（确保效果覆盖）
                        shipRenderers[i].SetPropertyBlock(propBlock);
                    }
                }
            }
            
            // ✅ 同步更新炫光效果（与飞船闪烁同步）
            if (enableBloomGlow && glowObject != null && glowObject.activeSelf && glowMaterialInstance != null)
            {
                // 让炫光跟随飞船位置（确保始终在飞船中心）
                Vector3 shipPos = transform.position;
                shipPos.z = 0f; // 保持在XY平面
                glowObject.transform.position = shipPos;
                
                // 让炫光面向摄像机（Billboard效果）- 在XY平面（Z=0）
                Camera mainCam = Camera.main;
                if (mainCam == null)
                {
                    mainCam = FindObjectOfType<Camera>();
                }
                
                if (mainCam != null)
                {
                    Vector3 camPos = mainCam.transform.position;
                    camPos.z = 0f; // 确保摄像机位置也在XY平面
                    Vector3 lookDir = camPos - shipPos;
                    lookDir.z = 0f; // 保持在XY平面
                    if (lookDir.magnitude > 0.01f)
                    {
                        glowObject.transform.rotation = Quaternion.LookRotation(-lookDir.normalized, Vector3.forward);
                    }
                }
                
                // ✅ 同步闪烁强度到炫光（使用相同的flickerStrength）
                // 从完全关闭到最大强度之间闪烁
                float glowIntensity = Mathf.Lerp(0f, glowMaxIntensity, flickerStrength);
                
                // 根据材质类型更新炫光
                if (glowMaterialInstance.HasProperty("_EmissionColor"))
                {
                    // 使用 Emission 属性
                    Color glowEmissionColor = glowColor * glowIntensity;
                    glowMaterialInstance.SetColor("_EmissionColor", glowEmissionColor);
                    
                    // 确保Emission关键字已启用
                    if (!glowMaterialInstance.IsKeywordEnabled("_EMISSION"))
                    {
                        glowMaterialInstance.EnableKeyword("_EMISSION");
                    }
                    if (glowMaterialInstance.HasProperty("_EmissionEnabled"))
                    {
                        glowMaterialInstance.SetFloat("_EmissionEnabled", 1f);
                    }
                }
                else if (glowMaterialInstance.HasProperty("_Color") || glowMaterialInstance.HasProperty("_BaseColor"))
                {
                    // 如果没有 Emission，直接修改颜色（让颜色本身发光）
                    Color glowColorFlicker = glowColor * glowIntensity * 0.5f;
                    glowColorFlicker.a = 0.6f + (flickerStrength * 0.4f); // Alpha 也在闪烁，让效果更明显
                    
                    if (glowMaterialInstance.HasProperty("_Color"))
                    {
                        glowMaterialInstance.color = glowColorFlicker;
                    }
                    if (glowMaterialInstance.HasProperty("_BaseColor"))
                    {
                        glowMaterialInstance.SetColor("_BaseColor", glowColorFlicker);
                    }
                }
            }
            
            // ✅ 在每次循环结束时递增帧计数器
            frameCount++;
            
            yield return null;
        }
        
        // ✅ 闪烁协程结束时隐藏炫光
        if (glowObject != null && glowObject.activeSelf)
        {
            glowObject.SetActive(false);
            if (glowMaterialInstance != null)
            {
                glowMaterialInstance.SetColor("_EmissionColor", glowColor * 0f);
            }
        }
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 🔥 持续闪烁协程结束（已爆炸或取消过热），总循环次数: {frameCount}");
        }
    }
    
    /// <summary>
    /// 显示UI警告文本
    /// </summary>
    private void ShowWarningUI()
    {
        // 如果警告文本为空，自动创建
        if (warningText == null)
        {
            // 查找Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // 如果没有Canvas，创建一个
                GameObject canvasObj = new GameObject("WarningCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                if (showDebugLog)
                {
                    Debug.Log("ShipOverheat: 自动创建了Canvas用于显示警告");
                }
            }
            
            // 创建警告文本对象
            GameObject warningObj = new GameObject("OverheatWarningText");
            warningObj.transform.SetParent(canvas.transform, false);
            
            warningText = warningObj.AddComponent<Text>();
            warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningText.fontSize = warningFontSize;
            warningText.color = warningTextColor;
            warningText.alignment = TextAnchor.MiddleCenter;
            warningText.text = warningMessage;
            warningText.enabled = false;
            
            // 设置位置（屏幕中央上方）
            RectTransform rectTransform = warningObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(800, 100);
            }
            
            warningTextObject = warningObj;
            
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: 自动创建了警告文本UI");
            }
        }
        
        // 显示警告文本
        if (warningText != null)
        {
            warningText.enabled = true;
            
            // 如果设置了显示持续时间，在时间后自动隐藏
            if (warningDisplayDuration > 0.01f)
            {
                StartCoroutine(HideWarningUICoroutine());
            }
            
            if (showDebugLog)
            {
                Debug.Log($"ShipOverheat: 显示UI警告: {warningMessage}");
            }
        }
    }
    
    /// <summary>
    /// 隐藏UI警告文本的协程
    /// </summary>
    private IEnumerator HideWarningUICoroutine()
    {
        yield return new WaitForSecondsRealtime(warningDisplayDuration);
        HideWarningUI();
    }
    
    /// <summary>
    /// 隐藏UI警告文本
    /// </summary>
    private void HideWarningUI()
    {
        if (warningText != null)
        {
            warningText.enabled = false;
            
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: 隐藏UI警告");
            }
        }
    }
    
    /// <summary>
    /// 爆炸效果序列（3秒后触发）
    /// </summary>
    private IEnumerator ExplosionEffectSequence()
    {
        if (showDebugLog)
        {
            Debug.Log("ShipOverheat: 开始爆炸效果序列");
        }
        
        // 现在停止闪烁（闪烁一直持续到这里）
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        
        // 标记为不再过热（停止闪烁循环）
        isOverheating = false;
        
        // 隐藏UI警告
        HideWarningUI();
        
        // ✅ 隐藏炫光效果（爆炸时停止炫光）
        if (glowObject != null)
        {
            glowObject.SetActive(false);
            if (glowMaterialInstance != null)
            {
                glowMaterialInstance.SetColor("_EmissionColor", glowColor * 0f);
            }
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 爆炸时隐藏炫光效果");
            }
        }
        
        // ✅ 停止过热粒子系统（但不立即销毁，让粒子自然消失）
        // 注意：使用 Stop(false) 停止发射新粒子，但让已存在的粒子继续播放直到自然消失
        if (overheatParticleSystem != null && overheatParticleSystem.isPlaying)
        {
            // false = 不立即清除已存在的粒子，让它们自然消失
            overheatParticleSystem.Stop(false);
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 已停止过热粒子系统发射（让已存在的粒子自然消失）");
            }
        }
        
        // ✅ 延迟销毁过热粒子系统（让粒子自然消失，而不是立即消失）
        // 启动协程来延迟销毁，让粒子系统播放完成
        if (overheatParticlesInstance != null)
        {
            StartCoroutine(DestroyOverheatParticlesDelayed());
        }
        
        // 第一步：爆炸特效
        PlayExplosionEffect();
        
        // 第二步：延迟一点，让爆炸特效先显示
        yield return new WaitForSecondsRealtime(0.2f);
        
        // 第三步：销毁飞船模型
        DestroyShipModel();
        
        // 第四步：切换到Crashed状态
        if (shipState != null)
        {
            shipState.SetState(ShipState.State.Crashed);
        }
        
        if (showDebugLog)
        {
            Debug.Log("ShipOverheat: 爆炸效果序列完成，飞船已切换到Crashed状态");
        }
    }
    
    /// <summary>
    /// 变红效果协程（使用纹理替换模式 - 效果更明显）
    /// </summary>
    private IEnumerator RedEffectTextureCoroutine()
    {
        if (shipRenderers == null || shipRenderers.Length == 0 || overheatTexture == null)
        {
            yield break;
        }
        
        if (showDebugLog)
        {
            Debug.Log("ShipOverheat: 使用纹理替换模式，替换为红色纹理");
        }
        
        // 保存原始纹理和材质状态
        Color[] originalEmissionColors = new Color[shipRenderers.Length];
        bool[] hasEmissionProperty = new bool[shipRenderers.Length];
        
        // 替换所有渲染器的主纹理为红色纹理（使用MaterialPropertyBlock，确保效果正确显示）
        // 注意：确保只修改飞船自己的渲染器，不修改其他对象
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null)
            {
                // 验证渲染器是否真的属于飞船（安全检查）
                Transform rendererTransform = shipRenderers[i].transform;
                bool belongsToShip = rendererTransform == transform || rendererTransform.IsChildOf(transform);
                
                if (!belongsToShip)
                {
                    if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 警告！渲染器 {shipRenderers[i].gameObject.name} 不属于飞船 {gameObject.name}，跳过修改");
                    }
                    continue;
                }
                
                // 确保PropertyBlock存在
                if (i >= materialPropertyBlocks.Length || materialPropertyBlocks[i] == null)
                {
                    if (materialPropertyBlocks == null || materialPropertyBlocks.Length != shipRenderers.Length)
                    {
                        materialPropertyBlocks = new MaterialPropertyBlock[shipRenderers.Length];
                    }
                    materialPropertyBlocks[i] = new MaterialPropertyBlock();
                    shipRenderers[i].GetPropertyBlock(materialPropertyBlocks[i]);
                }
                
                MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                
                // 获取材质信息（用于检查属性是否存在）
                Material sharedMat = shipRenderers[i].sharedMaterial;
                if (sharedMat != null)
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: 正在修改渲染器 {shipRenderers[i].gameObject.name} 的材质（使用PropertyBlock）");
                    }
                    
                    // 替换主纹理（使用PropertyBlock，优先级高于材质本身）
                    if (sharedMat.HasProperty("_MainTex") || sharedMat.HasProperty("_BaseMap"))
                    {
                        if (sharedMat.HasProperty("_MainTex"))
                        {
                            propBlock.SetTexture("_MainTex", overheatTexture);
                        }
                        if (sharedMat.HasProperty("_BaseMap")) // URP
                        {
                            propBlock.SetTexture("_BaseMap", overheatTexture);
                        }
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"ShipOverheat: 已通过PropertyBlock将 {shipRenderers[i].gameObject.name} 的主纹理替换为红色纹理");
                        }
                    }
                    else if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 材质 {sharedMat.name} 没有 _MainTex 或 _BaseMap 属性，无法替换纹理");
                    }
                    
                    // 设置Emission纹理（让红色纹理也发光）
                    if (sharedMat.HasProperty("_EmissionMap"))
                    {
                        propBlock.SetTexture("_EmissionMap", overheatTexture);
                    }
                    
                    // 设置Emission颜色和强度（让纹理发光）
                    if (sharedMat.HasProperty("_EmissionColor"))
                    {
                        originalEmissionColors[i] = propBlock.GetColor("_EmissionColor");
                        if (originalEmissionColors[i] == Color.clear || originalEmissionColors[i] == Color.black)
                        {
                            // 如果PropertyBlock中没有，尝试从材质获取
                            originalEmissionColors[i] = sharedMat.GetColor("_EmissionColor");
                        }
                        hasEmissionProperty[i] = true;
                        
                        Color emissionColor = Color.white * emissionIntensity;
                        propBlock.SetColor("_EmissionColor", emissionColor);
                        
                        // 启用Emission关键字（需要同时设置到材质和PropertyBlock）
                        // 注意：PropertyBlock不能设置关键字，需要在材质上设置
                        Material mat = shipRenderers[i].material; // 获取实例材质（如果没有会创建）
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionEnabled"))
                        {
                            mat.SetFloat("_EmissionEnabled", 1f);
                        }
                    }
                    
                    // 设置颜色tint（轻微红色，让效果更明显）
                    if (sharedMat.HasProperty("_Color"))
                    {
                        Color originalColor = propBlock.GetColor("_Color");
                        if (originalColor == Color.clear)
                        {
                            originalColor = sharedMat.color;
                        }
                        Color tintColor = Color.Lerp(originalColor, overheatColor, 0.3f);
                        propBlock.SetColor("_Color", tintColor);
                    }
                    if (sharedMat.HasProperty("_BaseColor")) // URP
                    {
                        Color originalColor = propBlock.GetColor("_BaseColor");
                        if (originalColor == Color.clear)
                        {
                            originalColor = sharedMat.GetColor("_BaseColor");
                        }
                        Color tintColor = Color.Lerp(originalColor, overheatColor, 0.3f);
                        propBlock.SetColor("_BaseColor", tintColor);
                    }
                    
                    // 应用PropertyBlock到渲染器（这一步很重要！）
                    shipRenderers[i].SetPropertyBlock(propBlock);
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: PropertyBlock已应用到渲染器 {shipRenderers[i].gameObject.name}");
                    }
                }
                else if (showDebugLog)
                {
                    Debug.LogWarning($"ShipOverheat: 渲染器 {shipRenderers[i].gameObject.name} 的材质为空，无法修改");
                }
            }
        }
        
        // 逐渐增强效果（只是逐渐增强Emission，不闪烁）- 使用MaterialPropertyBlock更新
        float elapsed = 0f;
        while (elapsed < redEffectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / redEffectDuration;
            
            // 在每一帧中，确保纹理和Emission都正确应用
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null && i < materialPropertyBlocks.Length && materialPropertyBlocks[i] != null)
                {
                    MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                    Material sharedMat = shipRenderers[i].sharedMaterial;
                    
                    if (sharedMat != null)
                    {
                        // 确保纹理始终设置（覆盖之前的）
                        if (overheatTexture != null)
                        {
                            if (sharedMat.HasProperty("_MainTex"))
                            {
                                propBlock.SetTexture("_MainTex", overheatTexture);
                            }
                            if (sharedMat.HasProperty("_BaseMap")) // URP
                            {
                                propBlock.SetTexture("_BaseMap", overheatTexture);
                            }
                        }
                        
                        // 更新Emission颜色（只是逐渐增强，不闪烁）
                        if (hasEmissionProperty[i] && sharedMat.HasProperty("_EmissionColor"))
                        {
                            // 逐渐增强Emission（不闪烁）
                            Color emissionColor = Color.white * (emissionIntensity * t);
                            propBlock.SetColor("_EmissionColor", emissionColor);
                            
                            // 设置Emission纹理（如果需要）
                            if (overheatTexture != null && sharedMat.HasProperty("_EmissionMap"))
                            {
                                propBlock.SetTexture("_EmissionMap", overheatTexture);
                            }
                        }
                        
                        // 应用PropertyBlock到渲染器（重要！必须每帧应用，确保效果覆盖）
                        shipRenderers[i].SetPropertyBlock(propBlock);
                    }
                }
            }
            
            yield return null;
        }
        
        // 确保最终效果（最大强度）- 使用PropertyBlock，确保纹理和Emission都正确应用
        // 但只有在仍然过热时才应用（如果已重置，不应该应用）
        if (isOverheating)
        {
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null && i < materialPropertyBlocks.Length && materialPropertyBlocks[i] != null)
                {
                    MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                    Material sharedMat = shipRenderers[i].sharedMaterial;
                    
                    if (sharedMat != null)
                    {
                        // 确保纹理已设置（最终确认）
                        if (overheatTexture != null)
                        {
                            if (sharedMat.HasProperty("_MainTex"))
                            {
                                propBlock.SetTexture("_MainTex", overheatTexture);
                            }
                            if (sharedMat.HasProperty("_BaseMap")) // URP
                            {
                                propBlock.SetTexture("_BaseMap", overheatTexture);
                            }
                            if (sharedMat.HasProperty("_EmissionMap"))
                            {
                                propBlock.SetTexture("_EmissionMap", overheatTexture);
                            }
                        }
                        
                        // 最终最大强度Emission
                        if (hasEmissionProperty[i] && sharedMat.HasProperty("_EmissionColor"))
                        {
                            Color finalEmissionColor = Color.white * emissionIntensity;
                            propBlock.SetColor("_EmissionColor", finalEmissionColor);
                        }
                        
                        // 应用PropertyBlock到渲染器（最终确认）
                        shipRenderers[i].SetPropertyBlock(propBlock);
                        
                        // 确保材质上的Emission关键字已启用（PropertyBlock不能设置关键字）
                        if (shipRenderers[i].material != null)
                        {
                            Material mat = shipRenderers[i].material;
                            mat.EnableKeyword("_EMISSION");
                            if (mat.HasProperty("_EmissionEnabled"))
                            {
                                mat.SetFloat("_EmissionEnabled", 1f);
                            }
                        }
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"ShipOverheat: 最终效果已应用到渲染器 {shipRenderers[i].gameObject.name} - 纹理: {overheatTexture != null}, Emission强度: {emissionIntensity}");
                        }
                    }
                }
            }
        }
        // 如果已重置，跳过最终效果应用（这是正常的，不需要日志）
    }
    
    /// <summary>
    /// 变红效果协程（使用颜色修改模式 - 动态渐变）
    /// </summary>
    private IEnumerator RedEffectColorCoroutine()
    {
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            yield break;
        }
        
        // 保存原始颜色和Emission状态
        Color[] originalColors = new Color[shipRenderers.Length];
        Color[] originalEmissionColors = new Color[shipRenderers.Length];
        bool[] hasColorProperty = new bool[shipRenderers.Length];
        bool[] hasEmissionProperty = new bool[shipRenderers.Length];
        bool[] emissionWasEnabled = new bool[shipRenderers.Length];
        
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null && shipRenderers[i].material != null)
            {
                Material mat = shipRenderers[i].material;
                
                // 保存原始颜色
                if (mat.HasProperty("_Color"))
                {
                    originalColors[i] = mat.color;
                    hasColorProperty[i] = true;
                }
                
                // 保存原始Emission并启用Emission
                if (mat.HasProperty("_EmissionColor"))
                {
                    originalEmissionColors[i] = mat.GetColor("_EmissionColor");
                    hasEmissionProperty[i] = true;
                    
                    // 启用Emission（使用Standard Shader的关键字）
                    mat.EnableKeyword("_EMISSION");
                    emissionWasEnabled[i] = true;
                    
                    // 对于URP/Lit，可能需要不同的设置
                    if (mat.HasProperty("_EmissionEnabled"))
                    {
                        mat.SetFloat("_EmissionEnabled", 1f);
                    }
                }
            }
        }
        
        // 逐渐变红（从原始颜色渐变到鲜艳红色）
        float elapsed = 0f;
        while (elapsed < redEffectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / redEffectDuration;
            
            // 计算闪烁效果（如果启用）
            float flickerT = t;
            if (enableFlicker)
            {
                // 使用正弦波创建闪烁效果
                float flicker = Mathf.Sin(elapsed * flickerFrequency * Mathf.PI * 2f) * 0.3f + 0.7f; // 0.4 到 1.0 之间
                flickerT = t * flicker; // 闪烁叠加到渐变上
            }
            
            // 使用更强烈的红色插值（使用乘法混合让红色更明显）
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null && hasColorProperty[i])
                {
                    // 验证渲染器是否真的属于飞船（安全检查）
                    Transform rendererTransform = shipRenderers[i].transform;
                    if (rendererTransform != transform && !rendererTransform.IsChildOf(transform))
                    {
                        continue; // 跳过不属于飞船的渲染器
                    }
                    
                    Material mat = shipRenderers[i].material;
                    
                    // 方法1：直接颜色插值（基础方法）
                    Color targetColor = Color.Lerp(originalColors[i], overheatColor, t);
                    
                    // 方法2：使用乘法混合让红色更强烈（将红色叠加到原始颜色上）
                    Color multipliedColor = Color.Lerp(Color.white, overheatColor, t);
                    Color finalColor = new Color(
                        originalColors[i].r * multipliedColor.r,
                        originalColors[i].g * multipliedColor.g,
                        originalColors[i].b * multipliedColor.b,
                        originalColors[i].a
                    );
                    
                    // 使用更强烈的混合方式
                    finalColor = Color.Lerp(originalColors[i], finalColor, t);
                    
                    // 应用颜色
                    mat.color = finalColor;
                    
                    // 应用强烈的Emission发光效果
                    if (hasEmissionProperty[i])
                    {
                        // 计算强烈的红色Emission
                        Color emissionColor = overheatColor * (emissionIntensity * t);
                        
                        // 添加闪烁效果到Emission
                        if (enableFlicker)
                        {
                            float flickerEmission = Mathf.Sin(elapsed * flickerFrequency * Mathf.PI * 2f) * 0.5f + 1.0f;
                            emissionColor *= flickerEmission;
                        }
                        
                        mat.SetColor("_EmissionColor", emissionColor);
                        
                        // 确保Emission启用
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
            
            yield return null;
        }
        
        // 确保到达最终鲜艳红色和强烈发光（但只有在仍然过热时才应用）
        if (isOverheating)
        {
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null && hasColorProperty[i])
                {
                    Material mat = shipRenderers[i].material;
                    
                    // 最终红色（使用乘法混合让红色最强烈）
                    Color multipliedColor = new Color(
                        originalColors[i].r * overheatColor.r,
                        originalColors[i].g * overheatColor.g,
                        originalColors[i].b * overheatColor.b,
                        originalColors[i].a
                    );
                    mat.color = multipliedColor;
                    
                    // 最终强烈Emission发光
                    if (hasEmissionProperty[i])
                    {
                        Color finalEmissionColor = overheatColor * emissionIntensity;
                        mat.SetColor("_EmissionColor", finalEmissionColor);
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
        // 如果已重置，跳过最终效果应用（这是正常的，不需要日志）
    }
    
    /// <summary>
    /// 立即应用红色纹理效果（用于快速切换）
    /// </summary>
    private void ApplyRedTextureImmediate()
    {
        if (shipRenderers == null || shipRenderers.Length == 0 || overheatTexture == null)
        {
            return;
        }
        
        // 确保PropertyBlock数组已初始化
        if (materialPropertyBlocks == null || materialPropertyBlocks.Length != shipRenderers.Length)
        {
            materialPropertyBlocks = new MaterialPropertyBlock[shipRenderers.Length];
            for (int j = 0; j < materialPropertyBlocks.Length; j++)
            {
                materialPropertyBlocks[j] = new MaterialPropertyBlock();
            }
        }
        
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null)
            {
                // 验证渲染器是否真的属于飞船（安全检查）
                Transform rendererTransform = shipRenderers[i].transform;
                bool belongsToShip = rendererTransform == transform || rendererTransform.IsChildOf(transform);
                
                if (!belongsToShip)
                {
                    if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 警告！渲染器 {shipRenderers[i].gameObject.name} 不属于飞船 {gameObject.name}，跳过修改");
                    }
                    continue;
                }
                
                Material sharedMat = shipRenderers[i].sharedMaterial;
                if (sharedMat != null)
                {
                    // 获取或创建PropertyBlock
                    if (materialPropertyBlocks[i] == null)
                    {
                        materialPropertyBlocks[i] = new MaterialPropertyBlock();
                    }
                    
                    MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                    shipRenderers[i].GetPropertyBlock(propBlock); // 获取当前PropertyBlock（如果有）
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: 立即应用红色纹理到渲染器 {shipRenderers[i].gameObject.name}（使用PropertyBlock）");
                    }
                    
                    // 使用PropertyBlock替换主纹理（确保覆盖之前的）
                    if (sharedMat.HasProperty("_MainTex"))
                    {
                        propBlock.SetTexture("_MainTex", overheatTexture);
                    }
                    if (sharedMat.HasProperty("_BaseMap")) // URP
                    {
                        propBlock.SetTexture("_BaseMap", overheatTexture);
                    }
                    
                    // 设置Emission纹理
                    if (sharedMat.HasProperty("_EmissionMap"))
                    {
                        propBlock.SetTexture("_EmissionMap", overheatTexture);
                    }
                    
                    // 设置Emission颜色和强度
                    if (sharedMat.HasProperty("_EmissionColor"))
                    {
                        propBlock.SetColor("_EmissionColor", Color.white * emissionIntensity);
                        
                        // 启用Emission关键字（PropertyBlock不能设置关键字，需要在材质上设置）
                        Material mat = shipRenderers[i].material;
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionEnabled"))
                        {
                            mat.SetFloat("_EmissionEnabled", 1f);
                        }
                    }
                    
                    // 应用PropertyBlock到渲染器（重要！确保效果覆盖）
                    shipRenderers[i].SetPropertyBlock(propBlock);
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: PropertyBlock已应用到渲染器 {shipRenderers[i].gameObject.name}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 立即应用红色颜色效果（用于快速切换）
    /// </summary>
    private void ApplyRedColorImmediate()
    {
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            return;
        }
        
        // 确保PropertyBlock数组已初始化
        if (materialPropertyBlocks == null || materialPropertyBlocks.Length != shipRenderers.Length)
        {
            materialPropertyBlocks = new MaterialPropertyBlock[shipRenderers.Length];
            for (int j = 0; j < materialPropertyBlocks.Length; j++)
            {
                materialPropertyBlocks[j] = new MaterialPropertyBlock();
            }
        }
        
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null)
            {
                // 验证渲染器是否真的属于飞船（安全检查）
                Transform rendererTransform = shipRenderers[i].transform;
                bool belongsToShip = rendererTransform == transform || rendererTransform.IsChildOf(transform);
                
                if (!belongsToShip)
                {
                    if (showDebugLog)
                    {
                        Debug.LogWarning($"ShipOverheat: 警告！渲染器 {shipRenderers[i].gameObject.name} 不属于飞船 {gameObject.name}，跳过修改");
                    }
                    continue;
                }
                
                Material sharedMat = shipRenderers[i].sharedMaterial;
                if (sharedMat != null)
                {
                    // 获取或创建PropertyBlock
                    if (materialPropertyBlocks[i] == null)
                    {
                        materialPropertyBlocks[i] = new MaterialPropertyBlock();
                    }
                    
                    MaterialPropertyBlock propBlock = materialPropertyBlocks[i];
                    shipRenderers[i].GetPropertyBlock(propBlock); // 获取当前PropertyBlock（如果有）
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: 立即应用红色颜色到渲染器 {shipRenderers[i].gameObject.name}（使用PropertyBlock）");
                    }
                    
                    // 使用PropertyBlock应用红色（确保覆盖之前的）
                    if (sharedMat.HasProperty("_Color"))
                    {
                        propBlock.SetColor("_Color", overheatColor);
                    }
                    if (sharedMat.HasProperty("_BaseColor")) // URP
                    {
                        propBlock.SetColor("_BaseColor", overheatColor);
                    }
                    
                    // 设置Emission颜色和强度
                    if (sharedMat.HasProperty("_EmissionColor"))
                    {
                        propBlock.SetColor("_EmissionColor", overheatColor * emissionIntensity);
                        
                        // 启用Emission关键字（PropertyBlock不能设置关键字，需要在材质上设置）
                        Material mat = shipRenderers[i].material;
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionEnabled"))
                        {
                            mat.SetFloat("_EmissionEnabled", 1f);
                        }
                    }
                    
                    // 应用PropertyBlock到渲染器（重要！确保效果覆盖）
                    shipRenderers[i].SetPropertyBlock(propBlock);
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: PropertyBlock已应用到渲染器 {shipRenderers[i].gameObject.name}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 取消过热效果（停止变红效果并恢复原始状态）
    /// 当飞船激活隔热技能或离开加热范围时调用
    /// </summary>
    public void CancelOverheating()
    {
        if (!isOverheating && !hasOverheated)
        {
            return; // 如果没有在过热，直接返回
        }
        
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 取消过热效果，恢复飞船原始状态");
        }
        
        // 停止所有协程
        if (overheatEffectCoroutine != null)
        {
            StopCoroutine(overheatEffectCoroutine);
            overheatEffectCoroutine = null;
        }
        
        // 停止闪烁协程
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        
        // 隐藏UI警告
        HideWarningUI();
        
        // ✅ 隐藏炫光效果
        if (glowObject != null)
        {
            glowObject.SetActive(false);
            if (glowMaterialInstance != null)
            {
                // 重置炫光强度
                glowMaterialInstance.SetColor("_EmissionColor", glowColor * 0f);
            }
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 取消过热时隐藏炫光效果");
            }
        }
        
        // ✅ 停止并销毁过热粒子系统（取消过热时立即清理）
        if (overheatParticleSystem != null && overheatParticleSystem.isPlaying)
        {
            overheatParticleSystem.Stop();
        }
        if (overheatParticlesInstance != null)
        {
            // 取消过热时，立即销毁粒子系统（不需要延迟）
            Destroy(overheatParticlesInstance);
            overheatParticlesInstance = null;
            overheatParticleSystem = null;
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 取消过热时已销毁过热粒子系统");
            }
        }
        
        // 恢复原始状态
        RestoreOriginalState();
        
        // 重置状态标志
        isOverheating = false;
        hasOverheated = false;
        overheatStartTime = 0f;
    }
    
    /// <summary>
    /// 恢复原始状态（材质、纹理、颜色等）
    /// 使用 ResetOverheatState 来恢复，因为它使用 PropertyBlock 系统
    /// </summary>
    private void RestoreOriginalState()
    {
        // 直接调用 ResetOverheatState，它已经实现了正确的恢复逻辑（使用 PropertyBlock）
        ResetOverheatState();
    }
    
    /// <summary>
    /// 延迟销毁过热粒子系统（让粒子自然消失，而不是立即消失）
    /// </summary>
    private IEnumerator DestroyOverheatParticlesDelayed()
    {
        if (overheatParticleSystem != null)
        {
            // 等待粒子系统停止并让粒子自然消失
            // 等待粒子系统的最大生命周期
            float maxLifetime = 0f;
            var main = overheatParticleSystem.main;
            
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                maxLifetime = main.startLifetime.constant;
            }
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                maxLifetime = main.startLifetime.constantMax;
            }
            else
            {
                // 如果是曲线模式，使用一个合理的默认值
                maxLifetime = 2f;
            }
            
            // ✅ 等待粒子系统停止后，再等待最大生命周期，让所有粒子自然消失
            // 使用未缩放时间，确保时停时也能正确等待
            // 增加额外的缓冲时间，确保所有粒子都消失
            float waitTime = maxLifetime + 1f; // 额外1秒缓冲
            if (showDebugLog)
            {
                Debug.Log($"ShipOverheat: 等待 {waitTime:F1} 秒后销毁过热粒子系统（让粒子自然消失，最大生命周期: {maxLifetime:F1}秒）");
            }
            yield return new WaitForSecondsRealtime(waitTime);
        }
        else
        {
            // 如果没有粒子系统，等待一小段时间后销毁
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // 销毁粒子系统实例
        if (overheatParticlesInstance != null)
        {
            Destroy(overheatParticlesInstance);
            overheatParticlesInstance = null;
            overheatParticleSystem = null;
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: ✅ 延迟销毁过热粒子系统完成（粒子已自然消失）");
            }
        }
    }
    
    /// <summary>
    /// 播放爆炸特效
    /// </summary>
    private void PlayExplosionEffect()
    {
        if (explosionParticlesPrefab == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipOverheat: 未设置爆炸特效Prefab，爆炸效果将不显示");
            }
            return;
        }
        
        // 在飞船位置实例化爆炸特效
        Vector3 explosionPosition = transform.position;
        GameObject explosionInstance = Instantiate(explosionParticlesPrefab, explosionPosition, Quaternion.identity);
        
        // 获取粒子系统组件并播放
        ParticleSystem particleSystem = explosionInstance.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = explosionInstance.GetComponentInChildren<ParticleSystem>();
        }
        
        if (particleSystem != null)
        {
            particleSystem.Play();
            
            // 如果粒子系统播放完成后会自动销毁，则不需要手动销毁
            if (!particleSystem.main.loop)
            {
                Destroy(explosionInstance, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
            }
            
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: 爆炸特效已播放");
            }
        }
        else
        {
            Debug.LogWarning($"ShipOverheat: 爆炸特效Prefab {explosionParticlesPrefab.name} 中没有找到ParticleSystem组件！");
            Destroy(explosionInstance);
        }
    }
    
    /// <summary>
    /// 销毁飞船模型（参考Crash脚本）
    /// </summary>
    private void DestroyShipModel()
    {
        // 获取所有渲染器组件（包括 MeshRenderer 和 SkinnedMeshRenderer）
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                // 禁用渲染器，使模型不可见
                renderer.enabled = false;
            }
        }
        
        // 查找名为 "Ship" 的子对象（如果存在），禁用而不是销毁，以便重置时恢复
        Transform shipModel = transform.Find("Ship");
        if (shipModel != null)
        {
            // 禁用子对象而不是销毁
            shipModel.gameObject.SetActive(false);
            Debug.Log("已禁用飞船模型子对象（过热失败）");
        }
        else if (renderers.Length > 0)
        {
            Debug.Log($"已禁用 {renderers.Length} 个渲染器组件，飞船模型已隐藏（过热失败）");
        }
        else
        {
            Debug.LogWarning("未找到飞船模型渲染器组件（过热失败）");
        }
    }
    
    /// <summary>
    /// 重置过热状态（用于游戏重启）
    /// </summary>
    public void ResetOverheatState()
    {
        if (showDebugLog)
        {
            Debug.Log($"ShipOverheat: 开始重置过热状态 - isOverheating={isOverheating}, hasOverheated={hasOverheated}");
        }
        
        // 首先标记状态，防止协程继续执行
        hasOverheated = false;
        isOverheating = false;
        overheatStartTime = 0f;
        
        // 停止所有协程（在清除效果之前，防止协程在重置后继续执行）
        if (overheatEffectCoroutine != null)
        {
            StopCoroutine(overheatEffectCoroutine);
            overheatEffectCoroutine = null;
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: 已停止变红效果协程");
            }
        }
        
        // 停止闪烁协程
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
            if (showDebugLog)
            {
                Debug.Log("ShipOverheat: 已停止闪烁协程");
            }
        }
        
        // 等待一帧，确保所有协程都已停止（避免协程在重置后继续执行）
        // 注意：这里不能使用 yield，所以我们在下一帧的 LateUpdate 或 Update 中处理
        // 但更简单的方法是：立即清除所有效果
        
        // 清除PropertyBlock并恢复原始材质属性
        if (shipRenderers != null)
        {
            for (int i = 0; i < shipRenderers.Length; i++)
            {
                if (shipRenderers[i] != null)
                {
                    // 验证渲染器是否属于飞船（安全检查）
                    Transform rendererTransform = shipRenderers[i].transform;
                    if (rendererTransform != transform && !rendererTransform.IsChildOf(transform))
                    {
                        continue; // 跳过不属于飞船的渲染器
                    }
                    
                    // 首先清除PropertyBlock（设置为null）
                    shipRenderers[i].SetPropertyBlock(null);
                    
                    // 然后完全重置材质实例，从 sharedMaterial 恢复所有属性
                    // 方法：获取 sharedMaterial，然后创建一个新的材质实例，完全复制所有属性
                    Material sharedMat = shipRenderers[i].sharedMaterial;
                    if (sharedMat == null)
                    {
                        if (showDebugLog)
                        {
                            Debug.LogWarning($"ShipOverheat: 渲染器 {shipRenderers[i].gameObject.name} 的 sharedMaterial 为空，无法恢复");
                        }
                        continue;
                    }
                    
                    // 强制重新创建材质实例（确保完全恢复到原始状态）
                    // 方法：将材质实例设置为 null，然后重新获取，这样会从 sharedMaterial 创建一个新的实例
                    Material oldInstanceMat = shipRenderers[i].material;
                    if (oldInstanceMat != null && oldInstanceMat != sharedMat)
                    {
                        // 如果材质实例存在且不是共享材质，销毁它（Unity会自动管理）
                        // 注意：Unity会在需要时自动创建新的实例，我们不需要手动销毁
                    }
                    
                    // 重新获取材质实例（如果之前不存在，Unity会从sharedMaterial创建新的）
                    Material instanceMat = shipRenderers[i].material;
                    if (instanceMat == null)
                    {
                        if (showDebugLog)
                        {
                            Debug.LogWarning($"ShipOverheat: 渲染器 {shipRenderers[i].gameObject.name} 的材质实例为空，无法恢复");
                        }
                        continue;
                    }
                    
                    // 完全从 sharedMaterial 恢复材质实例的所有属性
                    // 方法1：复制所有主要纹理属性（强制覆盖）
                    if (instanceMat.HasProperty("_MainTex") && sharedMat.HasProperty("_MainTex"))
                    {
                        Texture originalTex = sharedMat.GetTexture("_MainTex");
                        instanceMat.SetTexture("_MainTex", originalTex);
                    }
                    if (instanceMat.HasProperty("_BaseMap") && sharedMat.HasProperty("_BaseMap"))
                    {
                        Texture originalTex = sharedMat.GetTexture("_BaseMap");
                        instanceMat.SetTexture("_BaseMap", originalTex);
                    }
                    if (instanceMat.HasProperty("_EmissionMap") && sharedMat.HasProperty("_EmissionMap"))
                    {
                        Texture originalTex = sharedMat.GetTexture("_EmissionMap");
                        instanceMat.SetTexture("_EmissionMap", originalTex);
                    }
                    
                    // 确保所有可能的纹理属性都被恢复（防止有遗漏）
                    if (instanceMat.HasProperty("_DetailAlbedoMap") && sharedMat.HasProperty("_DetailAlbedoMap"))
                    {
                        instanceMat.SetTexture("_DetailAlbedoMap", sharedMat.GetTexture("_DetailAlbedoMap"));
                    }
                    if (instanceMat.HasProperty("_DetailNormalMap") && sharedMat.HasProperty("_DetailNormalMap"))
                    {
                        instanceMat.SetTexture("_DetailNormalMap", sharedMat.GetTexture("_DetailNormalMap"));
                    }
                    
                    // 恢复颜色
                    if (instanceMat.HasProperty("_Color") && sharedMat.HasProperty("_Color"))
                    {
                        instanceMat.color = sharedMat.color;
                    }
                    if (instanceMat.HasProperty("_BaseColor") && sharedMat.HasProperty("_BaseColor"))
                    {
                        instanceMat.SetColor("_BaseColor", sharedMat.GetColor("_BaseColor"));
                    }
                    
                    // ✅ 恢复Emission颜色和关键字（关键！）
                    if (instanceMat.HasProperty("_EmissionColor") && sharedMat.HasProperty("_EmissionColor"))
                    {
                        Color sharedEmission = sharedMat.GetColor("_EmissionColor");
                        instanceMat.SetColor("_EmissionColor", sharedEmission);
                        
                        // 检查 sharedMaterial 的 Emission 是否有效（非黑色/透明）
                        bool hasSharedEmission = sharedEmission.r > 0.01f || sharedEmission.g > 0.01f || sharedEmission.b > 0.01f;
                        
                        // 根据 sharedMaterial 的状态设置 Emission 关键字
                        if (!hasSharedEmission || sharedEmission == Color.black || sharedEmission == Color.clear || sharedEmission.a < 0.01f)
                        {
                            // ✅ sharedMaterial 没有 Emission，强制禁用关键字和Emission
                            if (instanceMat.IsKeywordEnabled("_EMISSION"))
                            {
                                instanceMat.DisableKeyword("_EMISSION");
                            }
                            if (instanceMat.HasProperty("_EmissionEnabled"))
                            {
                                instanceMat.SetFloat("_EmissionEnabled", 0f);
                            }
                            // 确保Emission颜色为黑色（完全关闭）
                            instanceMat.SetColor("_EmissionColor", Color.black);
                            
                            if (showDebugLog)
                            {
                                Debug.Log($"ShipOverheat: ✅ 已禁用渲染器 {shipRenderers[i].gameObject.name} 的Emission（sharedMaterial 无 Emission）");
                            }
                        }
                        else
                        {
                            // sharedMaterial 有 Emission，恢复原始状态
                            instanceMat.EnableKeyword("_EMISSION");
                            if (instanceMat.HasProperty("_EmissionEnabled"))
                            {
                                instanceMat.SetFloat("_EmissionEnabled", 1f);
                            }
                            if (showDebugLog)
                            {
                                Debug.Log($"ShipOverheat: ✅ 已恢复渲染器 {shipRenderers[i].gameObject.name} 的原始Emission颜色: {sharedEmission}");
                            }
                        }
                    }
                    
                    // ✅ 确保 _EmissionMap 也被恢复（如果 sharedMaterial 没有，设置为 null）
                    if (instanceMat.HasProperty("_EmissionMap"))
                    {
                        Texture originalEmissionTex = null;
                        if (sharedMat.HasProperty("_EmissionMap"))
                        {
                            originalEmissionTex = sharedMat.GetTexture("_EmissionMap");
                        }
                        instanceMat.SetTexture("_EmissionMap", originalEmissionTex);
                        if (showDebugLog)
                        {
                            Debug.Log($"ShipOverheat: ✅ 已恢复 _EmissionMap: {originalEmissionTex?.name ?? "null"}");
                        }
                    }
                    
                    // 恢复其他可能被修改的属性
                    if (instanceMat.HasProperty("_Metallic") && sharedMat.HasProperty("_Metallic"))
                    {
                        instanceMat.SetFloat("_Metallic", sharedMat.GetFloat("_Metallic"));
                    }
                    if (instanceMat.HasProperty("_Smoothness") && sharedMat.HasProperty("_Smoothness"))
                    {
                        instanceMat.SetFloat("_Smoothness", sharedMat.GetFloat("_Smoothness"));
                    }
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipOverheat: ✅ 已完全恢复渲染器 {shipRenderers[i].gameObject.name} 的材质到原始状态（从 sharedMaterial 恢复所有属性）");
                    }
                }
            }
        }
        
        // 清理PropertyBlock数组（可选，保留以便下次使用）
        // materialPropertyBlocks = null;
        
        // 恢复飞船模型（通过ShipState的RestoreShipModel方法）
        // 这里只重置状态，模型恢复由ShipState处理
    }
}

