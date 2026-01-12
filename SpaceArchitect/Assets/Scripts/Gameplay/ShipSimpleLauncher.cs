using UnityEngine;

/// <summary>
/// 简单飞船发射器
/// 鼠标移动控制发射方向，点击鼠标左键发射
/// </summary>
public class ShipSimpleLauncher : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("发射速度大小")]
    [SerializeField] private float launchSpeed = 5f;

    [Tooltip("箭头可视化长度")]
    [SerializeField] private float arrowLength = 3f;

    [Tooltip("箭头分段数量（建议5个）")]
    [SerializeField] private int arrowSegmentCount = 5;

    [Header("箭头可视化设置（使用图片）")]
    [Tooltip("箭头图片（Sprite），如果为空则不显示箭头")]
    [SerializeField] private Sprite arrowSprite;

    [Tooltip("箭头大小（缩放）")]
    [SerializeField] private Vector2 arrowSize = new Vector2(0.3f, 0.3f);

    [Tooltip("箭头之间的间距（相对于箭头长度）")]
    [SerializeField] private float arrowSpacing = 0.6f;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private ShipState shipState;
    private Camera mainCamera;
    private GameObject arrowContainerObject;
    private SpriteRenderer[] arrowSpriteRenderers;
    private Vector3 currentLaunchDirection = new Vector3(1f, 0f, 0f); // 默认向右（在XY平面）

    void Awake()
    {
        // 获取ShipState组件
        shipState = GetComponent<ShipState>();
        if (shipState == null)
        {
            Debug.LogError("ShipSimpleLauncher: 缺少 ShipState 组件！");
        }

        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("ShipSimpleLauncher: 未找到摄像机！");
            }
        }
    }

    void Start()
    {
        // 创建箭头可视化对象
        CreateArrowVisualization();
    }

    void Update()
    {
        // 只在PreLaunch状态下显示箭头并允许发射
        if (shipState != null && shipState.CanLaunch())
        {
            // 更新发射方向（基于鼠标位置）
            UpdateLaunchDirection();

            // 更新箭头可视化
            UpdateArrowVisualization();

            // 检查是否点击鼠标左键进行发射
            if (Input.GetMouseButtonDown(0))
            {
                LaunchShip();
            }
        }
        else
        {
            // 如果不是PreLaunch状态，隐藏箭头
            HideArrows();
        }
    }

    /// <summary>
    /// 创建箭头可视化对象
    /// </summary>
    private void CreateArrowVisualization()
    {
        // 创建箭头容器对象
        arrowContainerObject = new GameObject("LaunchArrowContainer");
        arrowContainerObject.transform.SetParent(transform);
        arrowContainerObject.transform.localPosition = Vector3.zero;

        // 如果没有设置箭头图片，直接返回
        if (arrowSprite == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipSimpleLauncher: 未设置箭头图片（Arrow Sprite），箭头将不显示");
            }
            arrowSpriteRenderers = new SpriteRenderer[0];
            return;
        }

        // 创建多个箭头实例
        arrowSpriteRenderers = new SpriteRenderer[arrowSegmentCount];
        for (int i = 0; i < arrowSegmentCount; i++)
        {
            GameObject arrowObj = new GameObject($"Arrow_{i}");
            arrowObj.transform.SetParent(arrowContainerObject.transform);
            arrowObj.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.color = Color.white; // 使用白色，保持图片原始颜色
            sr.sortingOrder = 100; // 确保箭头显示在其他对象前面
            sr.enabled = false; // 初始隐藏

            arrowSpriteRenderers[i] = sr;
        }
    }

    /// <summary>
    /// 更新发射方向（基于鼠标位置）
    /// </summary>
    private void UpdateLaunchDirection()
    {
        if (mainCamera == null) return;

        // 获取鼠标在世界空间的位置（在Z=0平面上）
        Vector3 mouseScreenPos = Input.mousePosition;
        
        // 使用Raycast方式获取鼠标在Z=0平面的位置，更准确
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Plane xyPlane = new Plane(Vector3.forward, Vector3.zero); // Z=0平面
        
        if (xyPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            
            // 计算从飞船到鼠标的方向
            Vector3 shipPos = transform.position;
            shipPos.z = 0f; // 确保飞船位置也在Z=0平面
            
            Vector3 direction = mouseWorldPos - shipPos;
            direction.z = 0f; // 确保在XY平面

            // 如果方向有效，更新当前发射方向
            if (direction.magnitude > 0.01f)
            {
                currentLaunchDirection = direction.normalized;
            }
        }
    }

    /// <summary>
    /// 更新箭头可视化（使用Sprite图片）
    /// </summary>
    private void UpdateArrowVisualization()
    {
        // 如果没有箭头图片或渲染器，直接返回
        if (arrowSprite == null || arrowSpriteRenderers == null || arrowSpriteRenderers.Length == 0)
        {
            return;
        }

        if (shipState == null || !shipState.CanLaunch())
        {
            HideArrows();
            return;
        }

        Vector3 shipPos = transform.position;
        shipPos.z = 0f; // 确保飞船位置在Z=0平面
        
        Vector3 arrowDir = currentLaunchDirection;
        arrowDir.z = 0f; // 确保箭头方向在XY平面
        arrowDir = arrowDir.normalized; // 重新归一化
        
        // 计算箭头方向的角度（用于旋转Sprite）
        float angle = Mathf.Atan2(arrowDir.y, arrowDir.x) * Mathf.Rad2Deg;
        Quaternion arrowRotation = Quaternion.Euler(0, 0, angle);

        // 更新每个箭头的位置、旋转和颜色
        for (int i = 0; i < arrowSpriteRenderers.Length; i++)
        {
            if (arrowSpriteRenderers[i] == null) continue;

            float t = (float)(i + 1) / (arrowSegmentCount + 1); // 0到1的进度

            // 计算箭头位置（沿着箭头方向，考虑间距）
            // 从飞船位置稍微向前一点开始，然后按间距排列
            float startOffset = arrowLength * 0.1f; // 从飞船稍微向前一点开始
            float arrowDistance = startOffset + (t * arrowLength * arrowSpacing);
            Vector3 arrowPos = shipPos + arrowDir * arrowDistance;
            arrowPos.z = 0f; // 确保在Z=0平面

            // 设置箭头位置和旋转
            arrowSpriteRenderers[i].transform.position = arrowPos;
            arrowSpriteRenderers[i].transform.rotation = arrowRotation;
            arrowSpriteRenderers[i].transform.localScale = new Vector3(arrowSize.x, arrowSize.y, 1f);

            // 保持图片原始颜色（白色，不进行颜色叠加）
            arrowSpriteRenderers[i].color = Color.white;

            // 显示箭头
            arrowSpriteRenderers[i].enabled = true;
        }
    }

    /// <summary>
    /// 隐藏所有箭头
    /// </summary>
    private void HideArrows()
    {
        if (arrowSpriteRenderers == null) return;

        foreach (var sr in arrowSpriteRenderers)
        {
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }

    /// <summary>
    /// 发射飞船
    /// </summary>
    private void LaunchShip()
    {
        // 检查ShipState组件
        if (shipState == null)
        {
            Debug.LogError("ShipSimpleLauncher: ShipState 组件未找到，无法发射！");
            return;
        }

        // 检查是否可以发射
        if (!shipState.CanLaunch())
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipSimpleLauncher: 飞船当前状态不允许发射（当前状态: {shipState.CurrentState}）");
            }
            return;
        }

        // 计算发射速度（使用当前鼠标方向，确保在XY平面）
        Vector3 velocity = currentLaunchDirection * launchSpeed;
        velocity.z = 0f; // 确保速度在XY平面

        // 调用ShipState.Launch()发射飞船
        // 这会自动执行以下步骤：
        // 1. 设置 NBody.vel = velocity
        // 2. 切换到 Flying 状态
        // 3. 将飞船添加到 GravityEngine
        // 4. 延迟一帧后通过 GravityEngine.SetVelocity() 确保速度正确应用
        shipState.Launch(velocity);

        // 隐藏箭头
        HideArrows();

        if (showDebugLog)
        {
            Debug.Log($"ShipSimpleLauncher: 飞船已发射！速度: {velocity}, 方向: {currentLaunchDirection}, 大小: {velocity.magnitude}");
        }
    }

    void OnDisable()
    {
        // 禁用时隐藏箭头
        HideArrows();
    }

    void OnDestroy()
    {
        // 清理箭头可视化对象
        if (arrowContainerObject != null)
        {
            Destroy(arrowContainerObject);
        }
    }
}



