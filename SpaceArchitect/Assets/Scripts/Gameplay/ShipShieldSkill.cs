using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 飞船防撞技能脚本
/// 点击C键触发防撞技能，飞船可以穿越小行星带
/// </summary>
public class ShipShieldSkill : MonoBehaviour
{
    [Header("技能设置")]
    [Tooltip("触发技能按键（默认C键）")]
    [SerializeField] private KeyCode shieldKey = KeyCode.C;

    [Tooltip("技能持续时间（秒）")]
    [SerializeField] private float shieldDuration = 5f;

    [Tooltip("技能冷却时间（秒）")]
    [SerializeField] private float cooldownDuration = 10f;

    [Header("保护罩特效设置")]
    [Tooltip("保护罩特效GameObject（场景中已存在的特效，直接控制其active状态）")]
    [SerializeField] private GameObject shieldVFX;
    
    [Tooltip("是否让特效跟随飞船旋转（跟随飞船速度方向）")]
    [SerializeField] private bool followShipRotation = true;

    [Header("小行星带设置")]
    [Tooltip("小行星带的Tag（飞船可以穿越的对象）")]
    [SerializeField] private string asteroidBeltTag = "Obstacles";

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private bool isShieldActive = false; // 技能是否激活
    private bool isOnCooldown = false; // 是否在冷却中
    private float shieldTimer = 0f; // 技能持续时间计时器
    private float cooldownTimer = 0f; // 冷却时间计时器
    private NBody shipNBody; // 飞船NBody组件（用于获取速度）
    private GravityEngine gravityEngine; // 引力引擎（用于获取速度）

    /// <summary>
    /// 获取技能是否激活
    /// </summary>
    public bool IsShieldActive => isShieldActive;

    /// <summary>
    /// 获取是否在冷却中
    /// </summary>
    public bool IsOnCooldown => isOnCooldown;

    /// <summary>
    /// 获取冷却剩余时间
    /// </summary>
    public float CooldownRemainingTime => cooldownTimer;

    void Awake()
    {
        // 初始化时确保特效是隐藏的
        if (shieldVFX != null)
        {
            shieldVFX.SetActive(false);
        }
        
        // 获取飞船NBody组件
        shipNBody = GetComponent<NBody>();
        if (shipNBody == null)
        {
            Debug.LogWarning("ShipShieldSkill: 未找到NBody组件，无法获取飞船速度方向");
        }
        
        // 获取引力引擎
        gravityEngine = GravityEngine.instance;
        if (gravityEngine == null)
        {
            gravityEngine = FindObjectOfType<GravityEngine>();
        }
    }

    void Update()
    {
        // 更新技能持续时间（使用未缩放时间，确保时停时也能正常计时）
        if (isShieldActive)
        {
            shieldTimer -= Time.unscaledDeltaTime;
            if (shieldTimer <= 0f)
            {
                DeactivateShield();
            }
        }

        // 更新冷却时间（使用未缩放时间）
        if (isOnCooldown)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
                if (showDebugLog)
                {
                    Debug.Log("ShipShieldSkill: 技能冷却完成，可以再次使用");
                }
            }
        }

        // 检查按键输入
        if (Input.GetKeyDown(shieldKey))
        {
            TryActivateShield();
        }
        
        // 如果特效激活且需要跟随飞船旋转，更新特效旋转
        if (isShieldActive && shieldVFX != null && shieldVFX.activeSelf && followShipRotation)
        {
            UpdateShieldVFXRotation();
        }
    }

    /// <summary>
    /// 尝试激活防撞技能
    /// </summary>
       private void TryActivateShield()
 {
        // 如果技能已经激活，不重复激活
        if (isShieldActive)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipShieldSkill: 技能已经在激活状态");
            }
            return;
        }

        // 如果技能在冷却中，不能使用
        if (isOnCooldown)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipShieldSkill: 技能还在冷却中，剩余时间: {cooldownTimer:F2}秒");
            }
            return;
        }

        // 激活技能
        ActivateShield();
    }

    /// <summary>
    /// 激活防撞技能
    /// </summary>
    private void ActivateShield()
    {
        isShieldActive = true;
        shieldTimer = shieldDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipShieldSkill: 防撞技能已激活，持续时间: {shieldDuration}秒");
        }

        // 显示保护罩特效
        ShowShieldVFX();

        // 触发技能激活事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 停用防撞技能
    /// </summary>
    private void DeactivateShield()
    {
        if (!isShieldActive) return;

        isShieldActive = false;
        shieldTimer = 0f;

        // 开始冷却
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipShieldSkill: 防撞技能已停用，开始冷却，冷却时间: {cooldownDuration}秒");
        }

        // 隐藏保护罩特效
        HideShieldVFX();

        // 触发技能停用事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 显示保护罩特效（直接控制已存在的特效GameObject的active状态）
    /// </summary>
    private void ShowShieldVFX()
    {
        if (shieldVFX == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipShieldSkill: 未设置保护罩特效（Shield VFX），特效将不显示");
            }
            return;
        }

        // 直接激活特效GameObject
        shieldVFX.SetActive(true);
        
        // 如果跟随旋转，配置粒子系统设置并立即更新一次旋转
        if (followShipRotation)
        {
            // 配置所有粒子系统使用Local空间（这样它们会跟随父对象旋转）
            ConfigureParticleSystemsForRotation(shieldVFX.transform);
            
            // 强制刷新所有Material，确保Shader更改生效
            RefreshAllMaterials(shieldVFX.transform);
            
            UpdateShieldVFXRotation();
        }

        if (showDebugLog)
        {
            Debug.Log($"ShipShieldSkill: 保护罩特效已显示 - GameObject: {shieldVFX.name}, 位置: {shieldVFX.transform.position}, 跟随旋转: {followShipRotation}");
        }
    }
    
    /// <summary>
    /// 更新保护罩特效的旋转，使其跟随飞船速度方向
    /// 递归设置特效及其所有子对象的旋转，并修改粒子系统设置使其能正确旋转
    /// </summary>
    private void UpdateShieldVFXRotation()
    {
        if (shieldVFX == null || !shieldVFX.activeSelf) return;
        
        // 获取目标旋转（飞船的旋转）
        Quaternion targetRotation = transform.rotation;
        
        // 设置特效本身的旋转
        shieldVFX.transform.rotation = targetRotation;
        
        // 递归设置所有子对象的旋转
        SetRotationRecursive(shieldVFX.transform, targetRotation);
        
        // 尝试通过Material属性控制旋转（如果Shader支持）
        ApplyParticleRotation(shieldVFX.transform, targetRotation);
        
        // 调试：检查旋转是否生效（每60帧打印一次，避免日志过多）
        if (showDebugLog && Time.frameCount % 60 == 0)
        {
            Debug.Log($"ShipShieldSkill: 更新旋转 - 飞船旋转: {transform.rotation.eulerAngles}, 特效旋转: {shieldVFX.transform.rotation.eulerAngles}");
            
            // 检查所有粒子系统的Shader信息
            ParticleSystem[] allPS = shieldVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allPS)
            {
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    Debug.Log($"ShipShieldSkill: [Shader检查] 粒子系统: {ps.name}, Shader: {renderer.material.shader?.name ?? "null"}, RenderMode: {renderer.renderMode}");
                }
            }
        }
    }
    
    /// <summary>
    /// 配置粒子系统使用Local空间，使其能正确跟随父对象旋转
    /// 同时修改Renderer设置和Material属性（如果Shader支持）
    /// </summary>
    private void ConfigureParticleSystemsForRotation(Transform targetTransform)
    {
        if (targetTransform == null) return;
        
        // 检查当前对象是否有粒子系统
        ParticleSystem ps = targetTransform.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            
            // 记录原始设置（用于调试）
            ParticleSystemSimulationSpace originalSpace = main.simulationSpace;
            
            // 将Simulation Space设置为Local，这样粒子会跟随父对象旋转
            bool spaceChanged = (main.simulationSpace != ParticleSystemSimulationSpace.Local);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            if (showDebugLog)
            {
                Debug.Log($"ShipShieldSkill: 粒子系统 {targetTransform.name} - Simulation Space: {originalSpace} -> Local (改变: {spaceChanged})");
            }
            
            // 修改Renderer设置
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                ParticleSystemRenderMode originalMode = renderer.renderMode;
                bool modeChanged = false;
                
                // 如果使用Billboard相关模式，改为HorizontalBillboard或Mesh
                // Billboard模式会让粒子始终面向摄像机，无法旋转
                if (originalMode == ParticleSystemRenderMode.Billboard)
                {
                    // 尝试使用HorizontalBillboard（在XY平面内可以旋转）
                    renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    modeChanged = true;
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipShieldSkill: 粒子系统 {targetTransform.name} - RenderMode: {originalMode} -> HorizontalBillboard");
                    }
                }
                else if (originalMode == ParticleSystemRenderMode.Mesh)
                {
                    // 如果已经是Mesh模式，确保mesh能正确旋转
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipShieldSkill: 粒子系统 {targetTransform.name} - RenderMode: Mesh (已支持旋转)");
                    }
                }
                else
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipShieldSkill: 粒子系统 {targetTransform.name} - RenderMode: {originalMode} (保持不变)");
                    }
                }
                
                // 尝试通过Material属性控制旋转（如果Shader支持）
                // 某些自定义Shader可能暴露了旋转相关的属性
                if (renderer.material != null)
                {
                    Material mat = renderer.material;
                    
                    // 检查常见的旋转相关属性名称
                    string[] rotationPropertyNames = {
                        "_Rotation", "_RotationAngle", "_Angle", 
                        "_ObjectRotation", "_WorldRotation",
                        "_BaseRotation", "_RotationOffset"
                    };
                    
                    foreach (string propName in rotationPropertyNames)
                    {
                        if (mat.HasProperty(propName))
                        {
                            if (showDebugLog)
                            {
                                // 尝试获取属性值来推断类型
                                try
                                {
                                    float floatValue = mat.GetFloat(propName);
                                    Debug.Log($"ShipShieldSkill: 发现Material属性: {propName} (Float类型，当前值: {floatValue})");
                                }
                                catch
                                {
                                    try
                                    {
                                        Vector4 vectorValue = mat.GetVector(propName);
                                        Debug.Log($"ShipShieldSkill: 发现Material属性: {propName} (Vector类型，当前值: {vectorValue})");
                                    }
                                    catch
                                    {
                                        Debug.Log($"ShipShieldSkill: 发现Material属性: {propName} (类型未知)");
                                    }
                                }
                            }
                        }
                    }
                }
                
                // 如果修改了设置，重新启动粒子系统以应用更改
                if (spaceChanged || modeChanged)
                {
                    ps.Stop();
                    ps.Clear();
                    ps.Play();
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"ShipShieldSkill: 粒子系统 {targetTransform.name} 已重新启动以应用新设置");
                    }
                }
            }
            else
            {
                if (showDebugLog)
                {
                    Debug.LogWarning($"ShipShieldSkill: 粒子系统 {targetTransform.name} 没有Renderer组件！");
                }
            }
        }
        
        // 递归配置所有子对象的粒子系统
        foreach (Transform child in targetTransform)
        {
            ConfigureParticleSystemsForRotation(child);
        }
    }
    
    /// <summary>
    /// 递归设置GameObject及其所有子对象的旋转
    /// </summary>
    private void SetRotationRecursive(Transform targetTransform, Quaternion rotation)
    {
        if (targetTransform == null) return;
        
        // 设置当前对象的旋转
        targetTransform.rotation = rotation;
        
        // 递归设置所有子对象的旋转
        foreach (Transform child in targetTransform)
        {
            SetRotationRecursive(child, rotation);
        }
    }
    
    /// <summary>
    /// 通过Material属性控制旋转（如果Shader支持）
    /// 某些自定义Shader Graph可能暴露了旋转相关的Material属性
    /// </summary>
    private void ApplyParticleRotation(Transform targetTransform, Quaternion rotation)
    {
        if (targetTransform == null) return;
        
        ParticleSystem ps = targetTransform.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                
                if (showDebugLog && Time.frameCount % 300 == 0) // 每5秒打印一次调试信息
                {
                    Debug.Log($"ShipShieldSkill: [Material检查] 粒子系统: {targetTransform.name}, Material: {mat.name}, Shader: {mat.shader?.name ?? "null"}");
                }
                
                // 获取旋转角度（Z轴旋转，因为游戏在XY平面）
                float zRotation = rotation.eulerAngles.z;
                
                // 尝试设置常见的旋转相关Material属性
                // 这些属性名称取决于Shader Graph的具体实现
                // 注意：_MainTex_Rotation 是纹理旋转，不是整个粒子系统的旋转
                string[] rotationPropertyNames = {
                    "_Rotation", "_RotationAngle", "_Angle", 
                    "_ObjectRotation", "_WorldRotation",
                    "_BaseRotation", "_RotationOffset", "_ZRotation",
                    "_MainTex_Rotation" // Particle系统有这个属性，但它是纹理旋转
                };
                
                bool foundRotationProperty = false;
                foreach (string propName in rotationPropertyNames)
                {
                    if (mat.HasProperty(propName))
                    {
                        // 尝试设置Float类型（最常见）
                        try
                        {
                            // 先尝试获取当前值，确认是Float类型
                            float currentValue = mat.GetFloat(propName);
                            // 设置角度值（可能需要转换为弧度或使用其他单位）
                            mat.SetFloat(propName, zRotation);
                            foundRotationProperty = true;
                            
                            if (showDebugLog)
                            {
                                Debug.Log($"ShipShieldSkill: 通过Material属性 {propName} (Float) 设置旋转角度: {zRotation}° (原值: {currentValue})");
                            }
                            break; // 找到第一个可用的属性就停止
                        }
                        catch
                        {
                            // 如果不是Float，尝试Vector类型
                            try
                            {
                                Vector4 currentValue = mat.GetVector(propName);
                                // 如果是Vector类型，设置旋转向量
                                Vector3 rotationVector = rotation.eulerAngles;
                                mat.SetVector(propName, rotationVector);
                                foundRotationProperty = true;
                                
                                if (showDebugLog)
                                {
                                    Debug.Log($"ShipShieldSkill: 通过Material属性 {propName} (Vector) 设置旋转向量: {rotationVector} (原值: {currentValue})");
                                }
                                break;
                            }
                            catch
                            {
                                // 既不是Float也不是Vector，跳过
                                if (showDebugLog)
                                {
                                    Debug.LogWarning($"ShipShieldSkill: Material属性 {propName} 存在但类型不支持（不是Float或Vector）");
                                }
                            }
                        }
                    }
                }
                
                if (!foundRotationProperty)
                {
                    // 如果没有找到旋转属性，记录所有可用的Material属性（用于调试）
                    // 只在第一次检查时打印，避免日志过多
                    if (showDebugLog)
                    {
                        Shader shader = mat.shader;
                        if (shader != null)
                        {
                            int propertyCount = shader.GetPropertyCount();
                            System.Text.StringBuilder propList = new System.Text.StringBuilder();
                            propList.Append($"ShipShieldSkill: [Material属性列表] 粒子系统 {targetTransform.name} (共{propertyCount}个属性): ");
                            
                            for (int i = 0; i < propertyCount; i++)
                            {
                                string propName = shader.GetPropertyName(i);
                                propList.Append(propName);
                                if (i < propertyCount - 1) propList.Append(", ");
                            }
                            
                            Debug.Log(propList.ToString());
                            Debug.LogWarning($"ShipShieldSkill: [Material检查] 粒子系统 {targetTransform.name} 没有找到旋转相关属性。Shader: {shader.name}。可能需要修改Shader Graph以支持旋转。");
                        }
                        else
                        {
                            Debug.LogWarning($"ShipShieldSkill: [Material检查] 粒子系统 {targetTransform.name} 的Material没有Shader！");
                        }
                    }
                }
            }
        }
        
        // 递归处理所有子对象
        foreach (Transform child in targetTransform)
        {
            ApplyParticleRotation(child, rotation);
        }
    }
    
    /// <summary>
    /// 隐藏保护罩特效（直接控制已存在的特效GameObject的active状态）
    /// </summary>
    private void HideShieldVFX()
    {
        if (shieldVFX != null)
        {
            shieldVFX.SetActive(false);

            if (showDebugLog)
            {
                Debug.Log($"ShipShieldSkill: 保护罩特效已隐藏 - GameObject: {shieldVFX.name}");
            }
        }
    }

    /// <summary>
    /// 检查是否可以忽略碰撞（用于Crash脚本调用）
    /// </summary>
    /// <param name="collisionTag">碰撞对象的Tag</param>
    /// <returns>如果可以忽略碰撞返回true，否则返回false</returns>
    public bool CanIgnoreCollision(string collisionTag)
    {
        // 只有当技能激活且碰撞对象是小行星带时，才能忽略碰撞
        if (isShieldActive && !string.IsNullOrEmpty(collisionTag))
        {
            if (collisionTag == asteroidBeltTag)
            {
                if (showDebugLog)
                {
                    Debug.Log($"ShipShieldSkill: 防撞技能激活，忽略小行星带碰撞（Tag: {collisionTag}）");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 手动激活技能（用于外部调用，如UI按钮）
    /// </summary>
    public void ActivateShieldManually()
    {
        TryActivateShield();
    }

    /// <summary>
    /// 手动停用技能（用于外部调用）
    /// </summary>
    public void DeactivateShieldManually()
    {
        DeactivateShield();
    }

    void OnDisable()
    {
        // 禁用时隐藏特效
        if (shieldVFX != null)
        {
            shieldVFX.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // 销毁时隐藏特效（但不销毁，因为它是场景中的对象）
        if (shieldVFX != null)
        {
            shieldVFX.SetActive(false);
        }
    }
    
    /// <summary>
    /// 延迟一帧后更新旋转，确保粒子系统已完全激活
    /// </summary>
    private IEnumerator DelayedRotationUpdate()
    {
        yield return null; // 等待一帧
        UpdateShieldVFXRotation();
    }
    
    /// <summary>
    /// 强制刷新所有Material，确保Shader更改生效
    /// 递归处理所有子对象
    /// </summary>
    private void RefreshAllMaterials(Transform targetTransform)
    {
        if (targetTransform == null) return;
        
        // 检查当前对象是否有粒子系统
        ParticleSystem ps = targetTransform.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.material != null)
            {
                // 强制Material更新（通过设置一个属性再恢复）
                Material mat = renderer.material;
                if (mat.HasProperty("_Color"))
                {
                    Color originalColor = mat.GetColor("_Color");
                    mat.SetColor("_Color", originalColor); // 强制更新
                }
                
                if (showDebugLog)
                {
                    Debug.Log($"ShipShieldSkill: [Material刷新] 粒子系统: {targetTransform.name}, Shader: {mat.shader?.name ?? "null"}");
                }
            }
        }
        
        // 递归处理所有子对象
        foreach (Transform child in targetTransform)
        {
            RefreshAllMaterials(child);
        }
    }
}

