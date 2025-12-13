using UnityEngine;

/// <summary>
/// 飞船状态管理器
/// 管理飞船的不同状态，控制引力引擎的影响
/// </summary>
public class ShipState : MonoBehaviour
{
    /// <summary>
    /// 飞船状态枚举
    /// </summary>
    public enum State
    {
        PreLaunch,  // 发射前状态（不受引力影响）
        Flying,     // 飞行状态（受引力影响）
        Captured,   // 被行星捕获状态（受引力影响，处于轨道引导中）
        Crashed,    // 碰撞状态（已坠毁，不受引力影响）
        Escaped     // 逃离状态（离开摄像机视野，不受引力影响）
    }

    [Header("当前状态")]
    [SerializeField] private State currentState = State.PreLaunch;

    [Header("逃离检测设置")]
    [Tooltip("用于检测视野的摄像机（如果为空，使用主摄像机）")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("检测间隔（秒），降低性能消耗")]
    [SerializeField] private float escapeCheckInterval = 0.5f;
    
    [Tooltip("视野边界扩展（0-1），值越大越容易触发逃离（例如0.1表示在视野外10%时触发）")]
    [SerializeField] private float viewportMargin = 0.1f;

    private NBody nBody;
    private GravityEngine gravityEngine;
    private bool hasInitialized = false;
    private Vector3 initialPosition;
    private Rigidbody rb;
    private float lastEscapeCheckTime = 0f;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public State CurrentState => currentState;

    /// <summary>
    /// 状态改变事件
    /// </summary>
    public event System.Action<State> OnStateChanged;

    void Awake()
    {
        // 获取组件
        nBody = GetComponent<NBody>();
        if (nBody == null)
        {
            Debug.LogError($"ShipState: {gameObject.name} 缺少 NBody 组件！");
            return;
        }

        rb = GetComponent<Rigidbody>();

        // 保存初始位置
        initialPosition = transform.position;

        // 尝试获取GravityEngine实例（可能还未初始化）
        gravityEngine = GravityEngine.instance;
        
        // 如果GravityEngine已存在，注册回调确保飞船不被自动添加
        // 这个回调会在GravityEngine初始化完成后执行，确保即使飞船被检测到也会被移除
        if (gravityEngine != null)
        {
            // 使用AddGEStartCallback确保在GravityEngine初始化完成后立即移除飞船
            gravityEngine.AddGEStartCallback(OnGravityEngineInitialized);
        }
    }

    void Start()
    {
        // 获取GravityEngine实例（如果之前在Awake中未获取到）
        if (gravityEngine == null)
        {
            gravityEngine = GravityEngine.instance;
            if (gravityEngine != null)
            {
                // AddGEStartCallback会自动处理：如果已初始化完成则立即执行回调，否则在初始化完成后执行
                gravityEngine.AddGEStartCallback(OnGravityEngineInitialized);
            }
        }

        if (gravityEngine == null)
        {
            Debug.LogError("ShipState: 场景中没有找到 GravityEngine！");
            return;
        }

        // 获取摄像机引用
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning("ShipState: 未找到主摄像机，逃离检测功能将不可用");
            }
        }

        // 初始化为PreLaunch状态（这会检查并移除如果已被添加）
        InitializePreLaunchState();
        hasInitialized = true;
    }

    /// <summary>
    /// GravityEngine初始化完成后的回调
    /// 确保飞船不会被自动添加到引力引擎中
    /// </summary>
    private void OnGravityEngineInitialized()
    {
        // 检查飞船是否被意外添加到GravityEngine
        if (nBody != null && nBody.engineRef != null && gravityEngine != null)
        {
            Debug.Log($"检测到飞船在GravityEngine初始化时被自动添加，正在移除...");
            gravityEngine.RemoveBody(gameObject);
            nBody.engineRef = null;
        }

        // 确保飞船处于PreLaunch状态
        if (currentState == State.PreLaunch)
        {
            InitializePreLaunchState();
        }
    }

    /// <summary>
    /// 初始化PreLaunch状态
    /// </summary>
    private void InitializePreLaunchState()
    {
        if (nBody == null) return;

        // 检查是否已经被GravityEngine添加
        if (nBody.engineRef != null)
        {
            // 如果已被添加，立即移除
            Debug.Log($"检测到飞船已被自动添加到引力引擎，正在移除...");
            gravityEngine.RemoveBody(gameObject);
            nBody.engineRef = null;
        }

        // 确保位置正确
        transform.position = initialPosition;

        Debug.Log($"飞船初始化完成 - PreLaunch状态，未添加到引力引擎");
    }

    void FixedUpdate()
    {
        // 在PreLaunch状态下，强制保持位置不变
        if (hasInitialized && currentState == State.PreLaunch)
        {
            // 确保飞船不会因为任何原因移动（包括重力）
            // 每帧都重置到初始位置，确保完全固定
            transform.position = initialPosition;
            
            // 如果有Rigidbody，重置其速度
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // 设置为运动学，完全由脚本控制
            }

            // 持续检查是否被意外添加到引擎
            if (nBody.engineRef != null)
            {
                Debug.LogWarning("飞船在PreLaunch状态下被意外添加到引力引擎，正在移除...");
                gravityEngine.RemoveBody(gameObject);
                nBody.engineRef = null;
            }
        }
        
        // 在Flying或Captured状态下，同步Rigidbody位置以便碰撞检测
        if (hasInitialized && (currentState == State.Flying || currentState == State.Captured))
        {
            // 同步Rigidbody位置（GravityEngine控制transform.position，但需要同步到Rigidbody才能检测碰撞）
            if (rb != null && !rb.isKinematic)
            {
                // 将transform的位置同步到Rigidbody，这样Unity物理引擎才能检测到碰撞
                // 使用MovePosition而不是直接设置position，这样Unity会进行碰撞检测
                rb.MovePosition(transform.position);
                
                // 同步旋转（如果需要）
                // rb.MoveRotation(transform.rotation);
            }
            
            // 检测飞船是否离开摄像机视野
            CheckForEscape();
        }
    }

    /// <summary>
    /// 设置飞船状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void SetState(State newState)
    {
        if (currentState == newState)
            return;

        State oldState = currentState;
        currentState = newState;

        // 根据状态处理引力影响
        HandleGravityForState(newState);

        // 触发本地状态改变事件（保持向后兼容）
        OnStateChanged?.Invoke(newState);

        // 通过EventManager触发全局事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipStateChanged(oldState, newState, gameObject);
            
            // 如果切换到Crashed或Escaped状态，触发失败事件
            if (newState == State.Crashed || newState == State.Escaped)
            {
                EventManager.Instance.TriggerShipFailed(newState, gameObject);
            }
        }

        Debug.Log($"飞船状态改变: {oldState} -> {newState}");
    }

    /// <summary>
    /// 根据状态处理引力影响
    /// </summary>
    /// <param name="state">当前状态</param>
    private void HandleGravityForState(State state)
    {
        if (gravityEngine == null || nBody == null)
            return;

        switch (state)
        {
            case State.PreLaunch:
                // PreLaunch状态：从引力引擎移除
                if (nBody.engineRef != null)
                {
                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                    Debug.Log("飞船已从引力引擎移除（PreLaunch状态）");
                }
                
                // 重置到初始位置
                transform.position = initialPosition;
                
                // 如果有Rigidbody，重置速度
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                break;

            case State.Flying:
                // Flying状态：确保NBody组件启用，然后添加到引力引擎
                if (nBody != null && !nBody.enabled)
                {
                    nBody.enabled = true;
                }

                if (nBody.engineRef == null)
                {
                    // 添加到引力引擎
                    gravityEngine.AddBody(gameObject);
                    Debug.Log($"飞船已添加到引力引擎（Flying状态），engineRef: {nBody.engineRef != null}");
                }
                else
                {
                    Debug.LogWarning("飞船已经在引力引擎中");
                }
                
                // 确保Rigidbody配置正确，以便碰撞检测
                if (rb != null)
                {
                    // 不是Kinematic，但也不使用Unity重力
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    // 注意：位置由GravityEngine控制，但Rigidbody需要非Kinematic才能检测碰撞
                }
                break;

            case State.Captured:
                // Captured状态：确保NBody组件启用，保持在引力引擎中（与Flying状态类似，但表示被行星捕获）
                if (nBody != null && !nBody.enabled)
                {
                    nBody.enabled = true;
                }

                if (nBody.engineRef == null)
                {
                    // 添加到引力引擎
                    gravityEngine.AddBody(gameObject);
                    Debug.Log($"飞船已添加到引力引擎（Captured状态），engineRef: {nBody.engineRef != null}");
                }
                
                // 确保Rigidbody配置正确，以便碰撞检测
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
                // Captured状态下，飞船仍然受引力影响，但会被PlanetGravityCapture引导
                break;

            case State.Crashed:
                // Crashed状态：从引力引擎移除，停止物理模拟
                if (nBody.engineRef != null)
                {
                    // 在移除前，将当前速度传递给Rigidbody（如果有）
                    if (rb != null && gravityEngine != null)
                    {
                        Vector3 currentVelocity = gravityEngine.GetVelocity(nBody);
                        rb.velocity = currentVelocity;
                        Debug.Log($"将GravityEngine速度传递给Rigidbody: {currentVelocity}");
                    }

                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                    Debug.Log("飞船已从引力引擎移除（Crashed状态）");
                }

                // 禁用NBody组件，停止GravityEngine控制
                if (nBody != null)
                {
                    nBody.enabled = false;
                }

                // 如果有Rigidbody，让它由Unity物理引擎控制
                if (rb != null)
                {
                    rb.isKinematic = false; // 允许物理碰撞
                    rb.useGravity = false; // 不使用Unity重力（飞船有自己的物理系统）
                    // 确保碰撞检测模式正确
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    Debug.Log("Rigidbody已切换为物理模式（Crashed状态）");
                }

                // 销毁飞船模型（禁用所有渲染器组件）
                DestroyShipModel();
                break;

            case State.Escaped:
                // Escaped状态：从引力引擎移除，停止物理模拟
                if (nBody.engineRef != null)
                {
                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                    Debug.Log("飞船已从引力引擎移除（Escaped状态）");
                }

                // 禁用NBody组件，停止GravityEngine控制
                if (nBody != null)
                {
                    nBody.enabled = false;
                }

                // 如果有Rigidbody，停止物理模拟
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Debug.Log("Rigidbody已停止（Escaped状态）");
                }
                break;
        }
    }

    /// <summary>
    /// 发射飞船（从PreLaunch切换到Flying）
    /// </summary>
    /// <param name="initialVelocity">初始速度</param>
    public void Launch(Vector3 initialVelocity)
    {
        if (currentState != State.PreLaunch)
        {
            Debug.LogWarning("飞船只能在PreLaunch状态下发射！");
            return;
        }

        if (nBody == null)
        {
            Debug.LogError($"飞船发射失败：缺少 NBody 组件！飞船: {gameObject.name}");
            return;
        }
        
        if (gravityEngine == null)
        {
            Debug.LogError($"飞船发射失败：GravityEngine 为 null！飞船: {gameObject.name}");
            // 尝试重新获取 GravityEngine
            gravityEngine = GravityEngine.instance;
            if (gravityEngine == null)
            {
                Debug.LogError("无法找到 GravityEngine 实例！");
                return;
            }
            Debug.Log("已重新获取 GravityEngine 实例");
        }

        // 先设置NBody的初始速度（在添加到引擎之前）
        nBody.vel = initialVelocity;
        Debug.Log($"飞船发射速度设置: {initialVelocity}");

        // 切换到Flying状态（这会自动添加到引力引擎）
        SetState(State.Flying);

        // 通过EventManager触发发射事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipLaunched(initialVelocity, gameObject);
        }

        // 确保速度正确应用到GravityEngine内部
        // 延迟一帧执行，确保AddBody完成
        StartCoroutine(ApplyVelocityAfterAddition(initialVelocity));
    }

    /// <summary>
    /// 在添加到引擎后应用速度
    /// </summary>
    private System.Collections.IEnumerator ApplyVelocityAfterAddition(Vector3 velocity)
    {
        yield return new WaitForFixedUpdate(); // 等待物理更新

        if (nBody.engineRef != null && gravityEngine != null && nBody != null)
        {
            // 使用GravityEngine的API设置速度
            gravityEngine.SetVelocity(nBody, velocity);
            Debug.Log($"飞船速度已应用到引力引擎: {velocity}, magnitude: {velocity.magnitude}");
        }
        else
        {
            Debug.LogError("无法设置速度：飞船未正确添加到引力引擎");
        }
    }

    /// <summary>
    /// 销毁飞船模型
    /// 禁用所有渲染器组件，使模型不可见
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

        // 也可以尝试查找并销毁名为 "Ship" 的子对象（如果存在）
        Transform shipModel = transform.Find("Ship");
        if (shipModel != null)
        {
            Destroy(shipModel.gameObject);
            Debug.Log("已销毁飞船模型子对象");
        }
        else if (renderers.Length > 0)
        {
            Debug.Log($"已禁用 {renderers.Length} 个渲染器组件，飞船模型已隐藏");
        }
        else
        {
            Debug.LogWarning("未找到飞船模型渲染器组件");
        }
    }

    /// <summary>
    /// 检查是否可以发射
    /// </summary>
    public bool CanLaunch()
    {
        return currentState == State.PreLaunch;
    }

    /// <summary>
    /// 检测飞船是否离开摄像机视野
    /// </summary>
    private void CheckForEscape()
    {
        // 检查时间间隔，降低性能消耗
        if (Time.time - lastEscapeCheckTime < escapeCheckInterval)
        {
            return;
        }
        lastEscapeCheckTime = Time.time;

        // 检查摄像机是否可用
        if (targetCamera == null)
        {
            return;
        }

        // 检查飞船是否在摄像机视野外
        if (!IsVisibleToCamera())
        {
            // 切换到Escaped状态
            if (currentState != State.Escaped)
            {
                SetState(State.Escaped);
                Debug.Log($"飞船已离开摄像机视野，切换到Escaped状态");
            }
        }
    }

    /// <summary>
    /// 检查飞船是否在摄像机视野内
    /// </summary>
    /// <returns>如果飞船在视野内返回true，否则返回false</returns>
    private bool IsVisibleToCamera()
    {
        if (targetCamera == null)
        {
            return true; // 如果没有摄像机，假设可见
        }

        // 将世界坐标转换为视口坐标
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(transform.position);

        // 检查是否在视口范围内（考虑边界扩展）
        // viewportPoint.x 和 viewportPoint.y 在 [0, 1] 范围内表示在视野内
        // viewportPoint.z > 0 表示在摄像机前方
        bool isInViewport = viewportPoint.z > 0 && 
                           viewportPoint.x >= -viewportMargin && 
                           viewportPoint.x <= 1 + viewportMargin &&
                           viewportPoint.y >= -viewportMargin && 
                           viewportPoint.y <= 1 + viewportMargin;

        return isInViewport;
    }

    /// <summary>
    /// 检查飞船是否在Escaped状态
    /// </summary>
    public bool IsEscaped()
    {
        return currentState == State.Escaped;
    }
}

