using UnityEngine;
using UnityEngine.SceneManagement;

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
        Setup,      // 检查/摆放阶段（摆放引力枢纽，不受引力影响）
        PreLaunch,  // 发射前状态（不受引力影响，可以发射）
        Flying,     // 飞行状态（受引力影响）
        Captured,   // 被行星捕获状态（受引力影响，处于轨道引导中）
        Crashed,    // 碰撞状态（已坠毁，不受引力影响）
        Escaped     // 逃离状态（离开摄像机视野，不受引力影响）
    }

    [Header("当前状态")]
    [SerializeField] private State currentState = State.Setup;

    [Header("逃离检测设置")]
    [Tooltip("用于检测视野的摄像机（如果为空，使用主摄像机）")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("检测间隔（秒），降低性能消耗")]
    [SerializeField] private float escapeCheckInterval = 0.5f;
    
    [Tooltip("视野边界扩展（0-1），值越大越容易触发逃离（例如0.2表示在视野外20%时触发）")]
    [SerializeField] private float viewportMargin = 0.2f; // 增大默认值，避免相机跟随延迟导致的误判
    
    [Tooltip("发射后延迟检查逃离的时间（秒），避免刚发射时误判")]
    [SerializeField] private float escapeCheckDelayAfterLaunch = 1.0f;

    // 速度缩放功能已移除（简化预测系统，提高准确性）
    // [Header("速度调整")]
    // [Tooltip("飞行速度缩放系数（例如：1.5 = 速度提升50%，2.0 = 速度提升100%）")]
    // [SerializeField] private float speedMultiplier = 1.0f;

    private NBody nBody;
    private GravityEngine gravityEngine;
    private bool hasInitialized = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation; // 保存初始旋转
    private Rigidbody rb;
    private float lastEscapeCheckTime = 0f;
    private float launchTime = -1f; // 发射时间（-1表示未发射）
    
    // 保存飞船模型子对象的引用，用于重置时恢复
    private Transform shipModelTransform;
    
    // 速度缩放功能已移除
    // private Vector3 lastScaledVelocity = Vector3.zero;

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

        // 保存初始位置和旋转
        initialPosition = transform.position;
        initialRotation = transform.rotation;

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

        // 获取摄像机引用（优先使用当前场景中的相机，避免MainHub相机冲突）
        if (targetCamera == null)
        {
            InitializeTargetCamera();
        }

        // 初始化状态（Setup或PreLaunch状态，这会检查并移除如果已被添加到引力引擎）
        InitializeStationaryState();
        hasInitialized = true;
    }
    
    /// <summary>
    /// 初始化目标相机（优先使用CameraFollowShip的相机，确保与相机跟随脚本使用同一个相机）
    /// </summary>
    private void InitializeTargetCamera()
    {
        Debug.Log($"[ShipState] InitializeTargetCamera: 开始查找相机，飞船场景: {gameObject.scene.name}, 当前状态: {currentState}");
        
        // 列出所有相机（用于调试）
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"[ShipState] InitializeTargetCamera: 找到 {allCameras.Length} 个相机:");
        foreach (Camera cam in allCameras)
        {
            Debug.Log($"  - {cam.name} (场景: {cam.gameObject.scene.name}, Tag: {cam.tag}, 启用: {cam.enabled}, Active: {cam.gameObject.activeInHierarchy})");
        }
        
        // 优先级1：查找CameraFollowShip组件所在的相机（这是实际控制视野的相机）
        // 关键修复：确保只查找当前场景中的CameraFollowShip（避免在叠加场景中找到错误场景的对象）
        CameraFollowShip[] allCameraFollowShips = FindObjectsOfType<CameraFollowShip>();
        CameraFollowShip cameraFollowShip = null;
        foreach (CameraFollowShip cfs in allCameraFollowShips)
        {
            if (cfs.gameObject.scene == gameObject.scene)
            {
                cameraFollowShip = cfs;
                break;
            }
        }
        
        if (cameraFollowShip != null)
        {
            Debug.Log($"[ShipState] InitializeTargetCamera: 找到CameraFollowShip: {cameraFollowShip.name} (场景: {cameraFollowShip.gameObject.scene.name})");
            Camera followCamera = cameraFollowShip.GetComponent<Camera>();
            if (followCamera != null)
            {
                Debug.Log($"[ShipState] InitializeTargetCamera: CameraFollowShip的相机: {followCamera.name} (场景: {followCamera.gameObject.scene.name}, 启用: {followCamera.enabled}, Tag: {followCamera.tag})");
                if (followCamera.enabled)
                {
                    // 验证相机是否属于当前场景（双重检查）
                    if (followCamera.gameObject.scene == gameObject.scene)
                    {
                        targetCamera = followCamera;
                        Debug.Log($"[ShipState] ✅ 找到CameraFollowShip的相机: {followCamera.name} (场景: {followCamera.gameObject.scene.name})");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[ShipState] CameraFollowShip的相机不属于当前场景！相机场景: {followCamera.gameObject.scene.name}, 飞船场景: {gameObject.scene.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ShipState] CameraFollowShip的相机未启用: {followCamera.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[ShipState] CameraFollowShip没有Camera组件");
            }
        }
        else
        {
            if (allCameraFollowShips.Length > 0)
            {
                Debug.LogWarning($"[ShipState] InitializeTargetCamera: 找到 {allCameraFollowShips.Length} 个CameraFollowShip，但都不在当前场景 ({gameObject.scene.name})");
            }
            else
            {
                Debug.LogWarning($"[ShipState] InitializeTargetCamera: 未找到CameraFollowShip组件");
            }
        }
        
        // 优先级2：查找当前物体所在场景中的相机（关卡场景）
        Scene currentScene = gameObject.scene;
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        // 首先尝试查找当前场景中已启用的MainCamera
        foreach (Camera cam in cameras)
        {
            // 优先使用当前场景中的相机，且标签为MainCamera，且已启用
            if (cam.gameObject.scene == currentScene && 
                cam.CompareTag("MainCamera") && 
                cam.enabled)
            {
                targetCamera = cam;
                Debug.Log($"[ShipState] 找到目标相机: {cam.name} (场景: {currentScene.name})");
                return;
            }
        }
        
        // 如果当前场景中没有找到MainCamera，尝试查找当前场景中的任何已启用的相机
        foreach (Camera cam in cameras)
        {
            if (cam.gameObject.scene == currentScene && cam.enabled)
            {
                targetCamera = cam;
                Debug.LogWarning($"[ShipState] 当前场景中没有MainCamera，使用备用相机: {cam.name} (场景: {currentScene.name})");
                return;
            }
        }
        
        // 如果当前场景中完全没有相机，尝试使用Camera.main（但需要验证它不属于MainHub场景）
        if (Camera.main != null && Camera.main.enabled)
        {
            string mainCamSceneName = Camera.main.gameObject.scene.name;
            bool isMainHubScene = mainCamSceneName.StartsWith("0") && mainCamSceneName.Contains("_MainHub");
            
            if (!isMainHubScene)
            {
                targetCamera = Camera.main;
                Debug.LogWarning($"[ShipState] 使用Camera.main作为备用相机: {Camera.main.name} (场景: {mainCamSceneName})");
            }
            else
            {
                Debug.LogWarning($"[ShipState] Camera.main属于MainHub场景 ({mainCamSceneName})，跳过使用");
            }
        }
        
        if (targetCamera == null)
        {
            Debug.LogWarning($"[ShipState] 未找到可用的相机！当前场景: {currentScene.name}，逃离检测功能将不可用");
        }
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

        // 确保飞船处于Setup或PreLaunch状态（静止状态）
        if (currentState == State.Setup || currentState == State.PreLaunch)
        {
            InitializeStationaryState();
        }
    }

    /// <summary>
    /// 初始化静止状态（Setup或PreLaunch状态）
    /// 确保飞船不在引力引擎中，位置固定
    /// </summary>
    private void InitializeStationaryState()
    {
        if (nBody == null) return;

        // 检查是否已经被GravityEngine添加
        if (nBody.engineRef != null)
        {
            // 如果已被添加，立即移除
            Debug.Log($"检测到飞船已被自动添加到引力引擎，正在移除...");
            if (gravityEngine != null)
            {
                gravityEngine.RemoveBody(gameObject);
                nBody.engineRef = null;
            }
        }

        // 确保位置正确
        transform.position = initialPosition;

        Debug.Log($"飞船初始化完成 - {currentState}状态，未添加到引力引擎");
    }

    void FixedUpdate()
    {
        // 在Setup或PreLaunch状态下，强制保持位置不变
        if (hasInitialized && (currentState == State.Setup || currentState == State.PreLaunch))
        {
            // 确保飞船不会因为任何原因移动（包括重力）
            // 每帧都重置到初始位置和旋转，确保完全固定
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            
            // 如果有Rigidbody，重置其速度
            if (rb != null)
            {
                // 先设置为 kinematic 之前重置速度
                if (!rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true; // 设置为运动学，完全由脚本控制
            }

            // 持续检查是否被意外添加到引擎
            if (nBody != null && nBody.engineRef != null)
            {
                string stateName = currentState == State.Setup ? "Setup" : "PreLaunch";
                Debug.LogWarning($"飞船在{stateName}状态下被意外添加到引力引擎，正在移除...");
                if (gravityEngine != null)
                {
                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                }
            }
        }
        
        // 在Flying或Captured状态下，同步Rigidbody位置以便碰撞检测
        // 速度缩放功能已移除（简化系统，提高预测准确性）
        if (hasInitialized && (currentState == State.Flying || currentState == State.Captured))
        {
            // 速度缩放逻辑已移除
            // 如果需要调整游戏速度，可以使用 GravityEngine 的 timeZoom 或 massScale 参数

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
            case State.Setup:
                // Setup状态：检查/摆放阶段（不受引力影响，位置固定）
                if (nBody.engineRef != null)
                {
                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                    Debug.Log("飞船已从引力引擎移除（Setup状态）");
                }
                
                // 确保位置和旋转正确
                transform.position = initialPosition;
                transform.rotation = initialRotation;
                
                // 如果有Rigidbody，重置速度
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // 设置为运动学，完全由脚本控制
                }
                break;

            case State.PreLaunch:
                // PreLaunch状态：发射前状态（不受引力影响，位置固定）
                if (nBody.engineRef != null)
                {
                    gravityEngine.RemoveBody(gameObject);
                    nBody.engineRef = null;
                    Debug.Log("飞船已从引力引擎移除（PreLaunch状态）");
                }
                
                // 确保位置和旋转正确
                transform.position = initialPosition;
                transform.rotation = initialRotation;
                
                // 如果有Rigidbody，重置速度
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // 设置为运动学，完全由脚本控制
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
        
        // 速度缩放功能已移除
        // lastScaledVelocity = Vector3.zero;

        // 记录发射时间（用于延迟逃离检查）
        launchTime = Time.time;

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
    /// 禁用所有渲染器组件和子对象，使模型不可见（但不真正销毁，以便重置时恢复）
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
            // 保存引用以便重置时恢复
            shipModelTransform = shipModel;
            // 禁用子对象而不是销毁
            shipModel.gameObject.SetActive(false);
            Debug.Log("已禁用飞船模型子对象（保留以便重置）");
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
        // 在Setup或PreLaunch状态下不检查逃离（相机可能还在移动）
        if (currentState == State.Setup || currentState == State.PreLaunch)
        {
            return;
        }
        
        // 如果刚发射，延迟检查（避免误判）
        if (launchTime > 0 && Time.time - launchTime < escapeCheckDelayAfterLaunch)
        {
            return;
        }
        
        // 检查时间间隔，降低性能消耗
        if (Time.time - lastEscapeCheckTime < escapeCheckInterval)
        {
            return;
        }
        lastEscapeCheckTime = Time.time;

        // 确保目标相机有效
        if (targetCamera == null || !targetCamera.enabled)
        {
            InitializeTargetCamera();
            if (targetCamera == null || !targetCamera.enabled)
            {
                return;
            }
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
        // 确保目标相机有效
        if (targetCamera == null || !targetCamera.enabled)
        {
            Debug.Log($"[ShipState] IsVisibleToCamera: 目标相机无效，重新初始化。当前状态: {currentState}, 飞船场景: {gameObject.scene.name}");
            InitializeTargetCamera();
            if (targetCamera == null || !targetCamera.enabled)
            {
                Debug.LogWarning($"[ShipState] IsVisibleToCamera: 目标相机无效，假设飞船可见。当前场景: {gameObject.scene.name}");
                return true; // 如果没有摄像机，假设可见
            }
        }

        // 验证相机是否属于当前场景（防止使用MainHub的相机）
        if (targetCamera.gameObject.scene != gameObject.scene)
        {
            Debug.LogWarning($"[ShipState] IsVisibleToCamera: 目标相机不属于当前场景！相机场景: {targetCamera.gameObject.scene.name}, 飞船场景: {gameObject.scene.name}，重新初始化相机");
            InitializeTargetCamera();
            if (targetCamera == null || !targetCamera.enabled || targetCamera.gameObject.scene != gameObject.scene)
            {
                Debug.LogWarning($"[ShipState] IsVisibleToCamera: 无法找到正确的相机，假设飞船可见");
                return true;
            }
        }

        // 将世界坐标转换为视口坐标
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(transform.position);
        
        // 详细调试信息
        Debug.Log($"[ShipState] IsVisibleToCamera 详细检查 - 飞船位置: {transform.position}, 相机位置: {targetCamera.transform.position}, 视口坐标: {viewportPoint}, 相机: {targetCamera.name} (场景: {targetCamera.gameObject.scene.name}), 相机启用: {targetCamera.enabled}, 相机Tag: {targetCamera.tag}");

        // 检查是否在视口范围内（考虑边界扩展）
        // viewportPoint.x 和 viewportPoint.y 在 [0, 1] 范围内表示在视野内
        // viewportPoint.z > 0 表示在摄像机前方
        
        // 如果飞船刚发射，使用更大的容差（避免相机跟随延迟导致的误判）
        float currentMargin = viewportMargin;
        if (launchTime > 0 && Time.time - launchTime < escapeCheckDelayAfterLaunch)
        {
            // 发射后短时间内使用更大的容差（2倍）
            currentMargin = viewportMargin * 2f;
        }
        
        bool isInViewport = viewportPoint.z > 0 && 
                           viewportPoint.x >= -currentMargin && 
                           viewportPoint.x <= 1 + currentMargin &&
                           viewportPoint.y >= -currentMargin && 
                           viewportPoint.y <= 1 + currentMargin;

        // 调试信息（详细输出）
        if (!isInViewport)
        {
            Debug.LogWarning($"[ShipState] IsVisibleToCamera: 飞船不在视野内！视口坐标: {viewportPoint}, 飞船位置: {transform.position}, 相机: {targetCamera.name} (场景: {targetCamera.gameObject.scene.name}), viewportMargin: {viewportMargin}, 当前容差: {currentMargin}, 检查结果: z>0={viewportPoint.z > 0}, x范围={viewportPoint.x >= -currentMargin && viewportPoint.x <= 1 + currentMargin}, y范围={viewportPoint.y >= -currentMargin && viewportPoint.y <= 1 + currentMargin}");
        }
        else
        {
            // 减少日志输出，避免刷屏
            // Debug.Log($"[ShipState] IsVisibleToCamera: 飞船在视野内 ✓");
        }

        return isInViewport;
    }

    /// <summary>
    /// 检查飞船是否在Escaped状态
    /// </summary>
    public bool IsEscaped()
    {
        return currentState == State.Escaped;
    }

    /// <summary>
    /// 重置飞船到初始状态（Setup状态）
    /// 用于游戏重启功能
    /// </summary>
    public void ResetShip()
    {
        // 如果已经在Setup状态，直接返回
        if (currentState == State.Setup)
        {
            return;
        }

        Debug.Log("重置飞船到初始状态（Setup状态）...");

        // 如果之前是Crashed状态，需要恢复飞船模型的渲染
        if (currentState == State.Crashed)
        {
            RestoreShipModel();
        }

        // 切换到Setup状态（初始状态，这会处理所有必要的清理工作）
        SetState(State.Setup);

        // 重置发射时间
        launchTime = -1f;

        // 确保位置和旋转正确
        transform.position = initialPosition;
        transform.rotation = initialRotation; // 恢复到初始旋转，而不是重置为identity

        // 重置Rigidbody
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 确保NBody组件启用（如果之前被禁用了）
        if (nBody != null && !nBody.enabled)
        {
            nBody.enabled = true;
        }

        // 重置NBody的速度
        if (nBody != null)
        {
            nBody.vel = Vector3.zero;
            nBody.vel_phys = Vector3.zero;
        }
        
        // 速度缩放功能已移除
        // lastScaledVelocity = Vector3.zero;

        // 重置Crash脚本的状态（重要：否则碰撞检测会被忽略）
        Crash crashScript = GetComponent<Crash>();
        if (crashScript != null)
        {
            crashScript.ResetCrashState();
        }

        Debug.Log("飞船已重置到初始状态（Setup）");
    }

    /// <summary>
    /// 从Setup状态转换到PreLaunch状态
    /// 当点击READY按钮时调用此方法，表示检查/摆放阶段完成，进入准备发射阶段
    /// </summary>
    public void ReadyToPreLaunch()
    {
        // 只能在Setup状态下调用
        if (currentState != State.Setup)
        {
            Debug.LogWarning($"飞船当前状态为 {currentState}，无法转换到PreLaunch状态。只能在Setup状态下调用ReadyToPreLaunch()。");
            return;
        }

        Debug.Log("飞船从Setup状态转换到PreLaunch状态（检查阶段完成，进入准备发射阶段）");

        // 切换到PreLaunch状态
        SetState(State.PreLaunch);

        // 重置发射时间（PreLaunch状态时还未发射）
        launchTime = -1f;

        // 确保位置和旋转正确
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // 确保Rigidbody配置正确
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 确保NBody组件启用
        if (nBody != null && !nBody.enabled)
        {
            nBody.enabled = true;
        }

        // 重置NBody的速度
        if (nBody != null)
        {
            nBody.vel = Vector3.zero;
            nBody.vel_phys = Vector3.zero;
        }

        // 通过EventManager触发状态转换事件（可选）
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipStateChanged(State.Setup, State.PreLaunch, gameObject);
        }

        Debug.Log("飞船已进入PreLaunch状态，可以发射");
    }

    /// <summary>
    /// 恢复飞船模型（恢复所有被禁用的渲染器和子对象）
    /// </summary>
    private void RestoreShipModel()
    {
        // 先恢复被禁用的子对象（如果存在）
        if (shipModelTransform != null && !shipModelTransform.gameObject.activeSelf)
        {
            shipModelTransform.gameObject.SetActive(true);
            Debug.Log("已恢复飞船模型子对象");
        }

        // 获取所有渲染器组件（包括被禁用的子对象中的）
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                // 启用渲染器，使模型可见
                renderer.enabled = true;
            }
        }

        if (renderers.Length > 0)
        {
            Debug.Log($"已恢复 {renderers.Length} 个渲染器组件，飞船模型已重新显示");
        }
        else
        {
            Debug.LogWarning("未找到飞船模型渲染器组件");
        }
    }
}

