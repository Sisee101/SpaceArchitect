using UnityEngine;

/// <summary>
/// Core物体拖拽脚本（支持引力）
/// 只对tag为Core的物体有效，可以在XY平面自由拖拽
/// 物体保持在GravityEngine中产生引力，但位置由拖拽控制
/// </summary>
public class CoreDragger : MonoBehaviour
{
    [Header("引力设置")]
    [Tooltip("是否产生引力（如果为false，物体不会添加到GravityEngine）")]
    public bool produceGravity = true;

    private bool isDragging = false;
    private Rigidbody coreRb;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private FixedObject fixedObject;
    private Vector3 offset;
    private float mouseZCoord;

    void Start()
    {
        coreRb = GetComponent<Rigidbody>();
        nBody = GetComponent<NBody>();
        gravityEngine = GravityEngine.instance;

        // 诊断信息
        Debug.Log($"[CoreDragger] Start: {gameObject.name}");
        Debug.Log($"[CoreDragger] Tag: {gameObject.tag}, 应该是 'Core'");
        Debug.Log($"[CoreDragger] Collider: {GetComponent<Collider>() != null}");
        Debug.Log($"[CoreDragger] NBody: {nBody != null}");
        Debug.Log($"[CoreDragger] Rigidbody: {coreRb != null}");
        Debug.Log($"[CoreDragger] ProduceGravity: {produceGravity}");

        // 检查是否有FixedObject组件（用于固定位置但产生引力）
        fixedObject = GetComponent<FixedObject>();

        // 如果启用引力，确保物体在GravityEngine中
        if (produceGravity)
        {
            EnsureInGravityEngine();
        }
        else
        {
            // 如果不需要引力，移除
            RemoveFromGravityEngine();
        }

        // 确保Rigidbody始终是运动学的，这样位置完全由拖拽控制
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
        }
    }

    void Update()
    {
        // 使用射线检测来处理拖拽（不依赖 OnMouseDown 事件）
        HandleMouseInput();
        
        // 如果正在拖拽，更新位置
        if (isDragging)
        {
            HandleDrag();
        }
    }
    
    /// <summary>
    /// 处理鼠标输入（使用射线检测，更可靠）
    /// </summary>
    private void HandleMouseInput()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning($"[CoreDragger] Camera.main 为 null，无法检测鼠标输入");
            return;
        }
            
        // 检查鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            Debug.Log($"[CoreDragger] 鼠标按下，进行射线检测: {gameObject.name}");
            
            // 射线检测（使用所有碰撞层）
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Debug.Log($"[CoreDragger] 射线击中物体: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}");
                
                // 检查是否击中当前物体或其子物体
                if (hit.collider != null && 
                    (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                {
                    Debug.Log($"[CoreDragger] 击中当前物体或其子物体");
                    
                    // 检查 Tag
                    if (gameObject.CompareTag("Core"))
                    {
                        Debug.Log($"[CoreDragger] Tag 检查通过，开始拖拽");
                        StartDragInternal();
                    }
                    else
                    {
                        Debug.LogWarning($"[CoreDragger] Tag 检查失败，当前 Tag: {gameObject.tag}");
                    }
                }
                else
                {
                    Debug.Log($"[CoreDragger] 射线击中的不是当前物体: {hit.collider?.gameObject?.name ?? "null"}");
                }
            }
            else
            {
                Debug.Log($"[CoreDragger] 射线没有击中任何物体");
                
                // 备用方法：检查鼠标是否在物体附近（屏幕坐标）
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
                Vector2 mousePos = Input.mousePosition;
                float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), mousePos);
                
                Debug.Log($"[CoreDragger] 屏幕距离检测: 物体屏幕位置={screenPos}, 鼠标位置={mousePos}, 距离={distance}");
                
                // 如果鼠标在物体附近（100像素内），也允许拖拽
                if (distance < 100f && gameObject.CompareTag("Core"))
                {
                    Debug.Log($"[CoreDragger] 使用屏幕距离检测，开始拖拽");
                    StartDragInternal();
                }
            }
        }
        
        // 检查鼠标松开
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDragInternal();
        }
    }
    
    /// <summary>
    /// 内部开始拖拽方法
    /// </summary>
    private void StartDragInternal()
    {
        Debug.Log($"[CoreDragger] 开始拖拽（射线检测）: {gameObject.name}");
        
        // 如果启用引力，确保物体在引力引擎中
        if (produceGravity)
        {
            EnsureInGravityEngine();
        }
        else
        {
            RemoveFromGravityEngine();
        }

        isDragging = true;
        
        // 计算偏移量
        mouseZCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
        
        Debug.Log($"[CoreDragger] 拖拽参数 - mouseZCoord: {mouseZCoord}, offset: {offset}");
        Debug.Log($"[CoreDragger] 当前位置: {transform.position}, 鼠标世界位置: {mousePos}");

        // 确保是运动学模式
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }

        // 通过EventManager触发拖拽开始事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerCoreDragStart(gameObject);
        }
    }
    
    /// <summary>
    /// 内部结束拖拽方法
    /// </summary>
    private void EndDragInternal()
    {
        Debug.Log($"[CoreDragger] 结束拖拽: {gameObject.name}");
        isDragging = false;

        // 保持运动学模式
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }

        // 如果产生引力，更新最终位置
        if (produceGravity && nBody != null && nBody.engineRef != null)
        {
            UpdateGravityEnginePosition();
        }
        else if (!produceGravity)
        {
            RemoveFromGravityEngine();
        }

        // 通过EventManager触发拖拽结束事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerCoreDragEnd(gameObject, transform.position);
        }
    }

    void FixedUpdate()
    {
        // 检查 GravityEngine 是否可用且已初始化
        if (gravityEngine == null || !gravityEngine.IsSetup())
        {
            return; // GravityEngine 还没准备好，跳过
        }
        
        if (produceGravity)
        {
            // 只在物体不在引擎中时才添加（避免重复添加导致错误）
            if (nBody != null && nBody.engineRef == null)
            {
                EnsureInGravityEngine();
            }
            
            // 如果位置被拖拽改变，更新GravityEngine中的位置
            if (nBody != null && nBody.engineRef != null && gravityEngine != null)
            {
                UpdateGravityEnginePosition();
            }
        }
        else
        {
            // 如果不需要引力，持续确保物体不在引力引擎中
            if (nBody != null && nBody.engineRef != null)
            {
                RemoveFromGravityEngine();
            }
        }

        // 确保Rigidbody始终是运动学的
        if (coreRb != null && !coreRb.isKinematic)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
        }
    }
    
    /// <summary>
    /// 处理拖拽（在 Update 中调用，更可靠）
    /// </summary>
    private void HandleDrag()
    {
        if (!Input.GetMouseButton(0))
        {
            // 鼠标已松开，结束拖拽
            EndDragInternal();
            return;
        }

        if (!gameObject.CompareTag("Core"))
        {
            Debug.LogWarning($"[CoreDragger] HandleDrag: Tag 不是 'Core'");
            return;
        }

        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPos();
        
        // 计算新位置（鼠标位置 + 偏移量）
        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = transform.position.z; // 保持原有Z轴深度，只在XY平面移动
        
        // 更新物体位置
        transform.position = newPosition;
        
        Debug.Log($"[CoreDragger] 拖拽中 - 鼠标世界位置: {mouseWorldPos}, 新位置: {newPosition}");

        // 如果产生引力，立即更新GravityEngine中的位置
        if (produceGravity && nBody != null && nBody.engineRef != null)
        {
            UpdateGravityEnginePosition();
        }

        // 通过EventManager触发拖拽中事件（每帧触发，但可以限制频率）
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerCoreDrag(gameObject, newPosition);
        }
    }

    void OnMouseDown()
    {
        Debug.Log($"[CoreDragger] OnMouseDown 被调用: {gameObject.name}");
        
        // 只对tag为Core的物体有效，并且必须有Collider
        if (!gameObject.CompareTag("Core"))
        {
            Debug.LogWarning($"[CoreDragger] Tag 不是 'Core'，当前 Tag: {gameObject.tag}");
            return;
        }

        // 检查是否有Collider（OnMouseDown事件需要Collider）
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[CoreDragger] {gameObject.name} 没有 Collider 组件！OnMouseDown 需要 Collider。");
            return;
        }

        Debug.Log($"[CoreDragger] 开始拖拽: {gameObject.name}");

        // 如果启用引力，确保物体在引力引擎中
        if (produceGravity)
        {
            EnsureInGravityEngine();
        }
        else
        {
            // 如果不需要引力，确保物体不在引力引擎中
            RemoveFromGravityEngine();
        }

        isDragging = true;
        
        // 检查 Camera.main 是否可用
        if (Camera.main == null)
        {
            Debug.LogError($"[CoreDragger] Camera.main 为 null！无法进行拖拽。");
            isDragging = false;
            return;
        }
        
        mouseZCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
        
        Debug.Log($"[CoreDragger] 拖拽参数 - mouseZCoord: {mouseZCoord}, offset: {offset}");
        Debug.Log($"[CoreDragger] 当前位置: {transform.position}, 鼠标世界位置: {mousePos}");
        Debug.Log($"[CoreDragger] isDragging 已设置为: {isDragging}");

        // 确保是运动学模式，位置完全由拖拽控制
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }
    }

    void OnMouseDrag()
    {
        Debug.Log($"[CoreDragger] OnMouseDrag 被调用: {gameObject.name}, isDragging: {isDragging}, Tag: {gameObject.tag}");
        
        // 只对tag为Core的物体有效
        if (!isDragging)
        {
            Debug.LogWarning($"[CoreDragger] OnMouseDrag 被调用，但 isDragging = false！可能 OnMouseDown 没有正确设置。");
            return;
        }
        
        if (!gameObject.CompareTag("Core"))
        {
            Debug.LogWarning($"[CoreDragger] OnMouseDrag 被调用，但 Tag 不是 'Core': {gameObject.tag}");
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = transform.position.z; // 保持原有Z轴深度，只在XY平面移动
        
        Debug.Log($"[CoreDragger] 更新位置: {transform.position} -> {newPosition}");
        transform.position = newPosition;

        // 如果产生引力，立即更新GravityEngine中的位置
        if (produceGravity && nBody != null && nBody.engineRef != null)
        {
            UpdateGravityEnginePosition();
        }
        else
        {
            Debug.LogWarning($"[CoreDragger] 无法更新 GravityEngine 位置: produceGravity={produceGravity}, nBody={nBody != null}, engineRef={nBody != null && nBody.engineRef != null}");
        }
    }

    void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        Debug.Log($"[CoreDragger] OnMouseUp 被调用: {gameObject.name}");
        isDragging = false;

        // 保持运动学模式，物体留在拖拽结束的位置
        // 不恢复物理模拟，确保位置完全由拖拽决定
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }

        // 如果产生引力，更新最终位置
        if (produceGravity && nBody != null && nBody.engineRef != null)
        {
            UpdateGravityEnginePosition();
        }
        else if (!produceGravity)
        {
            // 如果不需要引力，确保物体不在引力引擎中
            RemoveFromGravityEngine();
        }
    }

    Vector3 GetMouseWorldPos()
    {
        if (Camera.main == null)
        {
            Debug.LogError($"[CoreDragger] Camera.main 为 null！无法获取鼠标世界坐标。");
            return transform.position; // 返回当前位置作为备用
        }
        
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mouseZCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
    
    /// <summary>
    /// 诊断方法：检查拖拽功能是否正常
    /// </summary>
    [ContextMenu("诊断拖拽功能")]
    public void DiagnoseDrag()
    {
        Debug.Log("=== CoreDragger 诊断 ===");
        Debug.Log($"物体名称: {gameObject.name}");
        Debug.Log($"Tag: {gameObject.tag} (应该是 'Core')");
        Debug.Log($"ProduceGravity: {produceGravity}");
        Debug.Log($"IsDragging: {isDragging}");
        
        // 检查组件
        Collider col = GetComponent<Collider>();
        Debug.Log($"Collider: {(col != null ? col.GetType().Name : "无")}");
        if (col != null)
        {
            Debug.Log($"  - Enabled: {col.enabled}");
            Debug.Log($"  - IsTrigger: {col.isTrigger}");
        }
        
        Debug.Log($"NBody: {(nBody != null ? "有" : "无")}");
        Debug.Log($"Rigidbody: {(coreRb != null ? "有" : "无")}");
        if (coreRb != null)
        {
            Debug.Log($"  - IsKinematic: {coreRb.isKinematic}");
            Debug.Log($"  - UseGravity: {coreRb.useGravity}");
        }
        
        Debug.Log($"GravityEngine: {(gravityEngine != null ? "有" : "无")}");
        Debug.Log($"Camera.main: {(Camera.main != null ? "有" : "无")}");
        
        // 检查 Tag
        if (!gameObject.CompareTag("Core"))
        {
            Debug.LogError($"❌ Tag 错误！当前 Tag: '{gameObject.tag}'，应该是 'Core'");
        }
        else
        {
            Debug.Log("✅ Tag 正确");
        }
        
        // 检查 Collider
        if (col == null)
        {
            Debug.LogError("❌ 缺少 Collider 组件！OnMouseDown 需要 Collider。");
        }
        else if (!col.enabled)
        {
            Debug.LogError("❌ Collider 被禁用！");
        }
        else
        {
            Debug.Log("✅ Collider 正常");
        }
        
        // 检查 Camera
        if (Camera.main == null)
        {
            Debug.LogError("❌ Camera.main 为 null！");
        }
        else
        {
            Debug.Log("✅ Camera.main 正常");
        }
        
        Debug.Log("=== 诊断完成 ===");
    }

    /// <summary>
    /// 确保物体在引力引擎中（用于产生引力）
    /// </summary>
    private void EnsureInGravityEngine()
    {
        if (nBody == null || gravityEngine == null)
        {
            Debug.LogWarning($"[CoreDragger] EnsureInGravityEngine: nBody 或 gravityEngine 为 null");
            return;
        }

        // 如果还没有添加到引擎，添加它
        if (nBody.engineRef == null)
        {
            // 确保有FixedObject组件（用于固定位置但产生引力）
            if (fixedObject == null)
            {
                fixedObject = gameObject.AddComponent<FixedObject>();
            }

            // 添加到引力引擎（使用 try-catch 防止空引用异常）
            try
            {
                gravityEngine.AddBody(gameObject);
                Debug.Log($"Gravity Core {gameObject.name} 已添加到引力引擎，质量: {nBody.mass}");
            }
            catch (System.NullReferenceException e)
            {
                Debug.LogWarning($"[CoreDragger] GravityEngine 可能还未完全初始化，延迟添加。错误: {e.Message}");
                // 不抛出异常，等待下一帧再试
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CoreDragger] 添加物体到 GravityEngine 失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 更新GravityEngine中的位置（当物体被拖拽时）
    /// </summary>
    private void UpdateGravityEnginePosition()
    {
        if (nBody == null || gravityEngine == null || nBody.engineRef == null)
            return;

        // 获取当前世界位置，转换为物理空间位置
        Vector3 worldPos = transform.position;
        Vector3 physPos = worldPos / gravityEngine.physToWorldFactor;
        
        // 更新NBody的初始位置（FixedObject会在PreEvolve时读取这个值）
        nBody.initialPhysPosition = physPos;
        
        // 如果使用FixedObject，也更新其内部位置
        if (fixedObject != null)
        {
            fixedObject.SetPositionDouble(new Vector3d(physPos));
        }
        
        // 使用GravityEngine的API更新位置（确保立即生效）
        Vector3d physPos3d = new Vector3d(physPos);
        gravityEngine.SetPositionDoubleV3(nBody, physPos3d);
    }

    /// <summary>
    /// 从引力引擎中移除物体
    /// </summary>
    private void RemoveFromGravityEngine()
    {
        if (nBody != null && gravityEngine != null)
        {
            if (nBody.engineRef != null)
            {
                gravityEngine.RemoveBody(gameObject);
                nBody.engineRef = null;
            }
        }
    }
}
