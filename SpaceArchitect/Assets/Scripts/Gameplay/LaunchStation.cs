using UnityEngine;
using SpaceArchitect.Core;

namespace SpaceArchitect.Gameplay
{
    /// <summary>
    /// 发射台控制器
    /// 管理飞船发射操作，包括鼠标拖拽、方向指示、速度计算和发射
    /// </summary>
    public class LaunchStation : MonoBehaviour
    {
        #region 飞船引用
        
        [Header("飞船引用")]
        [Tooltip("要发射的飞船（如果为空，会自动查找场景中的ShipController）")]
        [SerializeField] private ShipController _ship;
        
        #endregion

        #region 拖拽发射参数
        
        [Header("拖拽发射参数")]
        [Tooltip("是否启用拖拽发射功能")]
        [SerializeField] private bool _enableDragLaunch = true;
        
        [Tooltip("鼠标拖拽的速度倍数（拖拽距离 * 此值 = 发射速度）")]
        [SerializeField] private float _dragToVelocityMultiplier = 1f;
        
        [Tooltip("最小发射速度")]
        [SerializeField] private float _minLaunchVelocity = 1f;
        
        [Tooltip("最大发射速度")]
        [SerializeField] private float _maxLaunchVelocity = 50f;
        
        [Tooltip("最小拖拽距离（低于此距离不发射）")]
        [SerializeField] private float _minDragDistance = 0.5f;
        
        [Tooltip("是否锁定在水平面（XZ平面）")]
        [SerializeField] private bool _lockToHorizontalPlane = true;
        
        [Tooltip("水平面高度（世界坐标Y值）")]
        [SerializeField] private float _horizontalPlaneHeight = 0f;
        
        #endregion

        #region 方向指示器参数
        
        [Header("方向指示器")]
        [Tooltip("是否显示方向指示器")]
        [SerializeField] private bool _showDirectionIndicator = true;
        
        [Tooltip("方向指示器对象（可选，如果为空则动态创建）")]
        [SerializeField] private GameObject _directionIndicatorObject;
        
        [Tooltip("方向指示器颜色")]
        [SerializeField] private Color _directionIndicatorColor = Color.yellow;
        
        [Tooltip("方向指示器宽度")]
        [SerializeField] private float _directionIndicatorWidth = 0.1f;
        
        [Tooltip("方向指示器最大长度")]
        [SerializeField] private float _directionIndicatorMaxLength = 20f;
        
        [Tooltip("使用LineRenderer还是自定义绘制")]
        [SerializeField] private bool _useLineRenderer = true;
        
        #endregion

        #region 摄像机引用
        
        [Header("摄像机设置")]
        [Tooltip("用于射线投射的摄像机（如果为空，自动查找主摄像机）")]
        [SerializeField] private Camera _camera;
        
        #endregion

        #region 内部变量
        
        /// <summary>
        /// 拖拽状态
        /// </summary>
        private bool _isDragging = false;
        
        /// <summary>
        /// 拖拽起始位置（世界坐标）
        /// </summary>
        private Vector3 _dragStartPosition;
        
        /// <summary>
        /// 拖拽当前位置（世界坐标）
        /// </summary>
        private Vector3 _dragCurrentPosition;
        
        /// <summary>
        /// 发射方向
        /// </summary>
        private Vector3 _launchDirection;
        
        /// <summary>
        /// 发射速度大小
        /// </summary>
        private float _launchVelocityMagnitude;
        
        /// <summary>
        /// 是否准备好发射
        /// </summary>
        private bool _isReadyToLaunch = false;
        
        /// <summary>
        /// 方向指示器LineRenderer组件
        /// </summary>
        private LineRenderer _lineRenderer;
        
        /// <summary>
        /// Transform组件缓存
        /// </summary>
        private Transform _transform;
        
        /// <summary>
        /// 事件管理器引用
        /// </summary>
        private EventManager _eventManager;
        
        #endregion

        #region Unity生命周期
        
        private void Awake()
        {
            // 获取Transform组件
            _transform = transform;
            
            // 获取EventManager实例
            _eventManager = EventManager.Instance;
            
            // 查找摄像机
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning("LaunchStation: 未找到主摄像机，请手动指定摄像机");
                }
            }
            
            // 查找飞船
            if (_ship == null)
            {
                _ship = FindObjectOfType<ShipController>();
                if (_ship == null)
                {
                    Debug.LogWarning("LaunchStation: 场景中未找到ShipController，请手动指定飞船");
                }
            }
            
            // 初始化方向指示器
            InitializeDirectionIndicator();
        }
        
        private void Start()
        {
            // 设置水平面高度（如果使用飞船位置）
            if (_lockToHorizontalPlane && _ship != null)
            {
                _horizontalPlaneHeight = _ship.transform.position.y;
            }
            
            // 确保初始状态正确
            UpdateReadyState();
        }
        
        private void Update()
        {
            // 处理鼠标输入
            HandleMouseInput();
            
            // 更新方向指示器
            if (_showDirectionIndicator)
            {
                UpdateDirectionIndicator();
            }
        }
        
        #endregion

        #region 鼠标输入处理
        
        /// <summary>
        /// 处理鼠标输入
        /// </summary>
        private void HandleMouseInput()
        {
            if (!_enableDragLaunch || _ship == null)
            {
                return;
            }
            
            // 检查飞船是否处于可发射状态
            if (_ship.CurrentState != ShipState.PreLaunch)
            {
                _isReadyToLaunch = false;
                return;
            }
            
            // 鼠标按下：开始拖拽
            if (Input.GetMouseButtonDown(0))
            {
                StartDragging();
            }
            // 鼠标拖拽中：更新拖拽位置
            else if (_isDragging && Input.GetMouseButton(0))
            {
                UpdateDragging();
            }
            // 鼠标松开：发射飞船
            else if (_isDragging && Input.GetMouseButtonUp(0))
            {
                EndDraggingAndLaunch();
            }
        }
        
        /// <summary>
        /// 开始拖拽
        /// </summary>
        private void StartDragging()
        {
            // 获取鼠标在世界坐标的位置
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            if (mouseWorldPos != Vector3.zero)
            {
                _isDragging = true;
                _dragStartPosition = mouseWorldPos;
                _dragCurrentPosition = mouseWorldPos;
                
                Debug.Log($"开始拖拽发射，起始位置：{_dragStartPosition}");
            }
        }
        
        /// <summary>
        /// 更新拖拽
        /// </summary>
        private void UpdateDragging()
        {
            // 获取当前鼠标在世界坐标的位置
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            if (mouseWorldPos != Vector3.zero)
            {
                _dragCurrentPosition = mouseWorldPos;
                
                // 计算发射方向和速度
                CalculateLaunchParameters();
                
                // 更新准备状态
                UpdateReadyState();
            }
        }
        
        /// <summary>
        /// 结束拖拽并发射
        /// </summary>
        private void EndDraggingAndLaunch()
        {
            if (!_isDragging)
            {
                return;
            }
            
            _isDragging = false;
            
            // 计算最终发射参数
            CalculateLaunchParameters();
            
            // 检查是否满足发射条件
            if (_isReadyToLaunch && _launchVelocityMagnitude >= _minLaunchVelocity)
            {
                // 发射飞船
                LaunchShip();
            }
            else
            {
                Debug.Log("拖拽距离太短，无法发射");
            }
            
            // 重置拖拽状态
            ResetDragging();
        }
        
        /// <summary>
        /// 重置拖拽状态
        /// </summary>
        private void ResetDragging()
        {
            _isDragging = false;
            _dragStartPosition = Vector3.zero;
            _dragCurrentPosition = Vector3.zero;
            _launchDirection = Vector3.zero;
            _launchVelocityMagnitude = 0f;
            _isReadyToLaunch = false;
            
            // 隐藏方向指示器
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }
        
        #endregion

        #region 鼠标位置获取
        
        /// <summary>
        /// 获取鼠标在世界坐标的位置（投影到水平面）
        /// </summary>
        /// <returns>世界坐标位置，如果获取失败返回Vector3.zero</returns>
        private Vector3 GetMouseWorldPosition()
        {
            if (_camera == null)
            {
                return Vector3.zero;
            }
            
            // 从鼠标屏幕位置创建射线
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            
            if (_lockToHorizontalPlane)
            {
                // 计算射线与水平面的交点
                return GetRayPlaneIntersection(ray, Vector3.up, new Vector3(0, _horizontalPlaneHeight, 0));
            }
            else
            {
                // 使用射线投射（默认与Y=0的平面相交）
                Plane plane = new Plane(Vector3.up, new Vector3(0, _horizontalPlaneHeight, 0));
                float distance;
                if (plane.Raycast(ray, out distance))
                {
                    return ray.GetPoint(distance);
                }
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// 计算射线与平面的交点
        /// </summary>
        private Vector3 GetRayPlaneIntersection(Ray ray, Vector3 planeNormal, Vector3 planePoint)
        {
            float denominator = Vector3.Dot(planeNormal, ray.direction);
            
            // 如果分母为0，射线与平面平行
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return Vector3.zero;
            }
            
            float t = Vector3.Dot(planeNormal, planePoint - ray.origin) / denominator;
            return ray.GetPoint(t);
        }
        
        #endregion

        #region 发射参数计算
        
        /// <summary>
        /// 计算发射参数（方向和速度）
        /// </summary>
        private void CalculateLaunchParameters()
        {
            if (_ship == null)
            {
                return;
            }
            
            // 获取飞船位置
            Vector3 shipPosition = _ship.transform.position;
            
            // 计算拖拽方向（从拖拽起始位置指向当前鼠标位置）
            Vector3 dragDirection = (_dragCurrentPosition - _dragStartPosition);
            
            // 投影到水平面（如果启用）
            if (_lockToHorizontalPlane)
            {
                dragDirection.y = 0f;
            }
            
            // 计算拖拽距离
            float dragDistance = dragDirection.magnitude;
            
            // 检查最小拖拽距离
            if (dragDistance < _minDragDistance)
            {
                _isReadyToLaunch = false;
                return;
            }
            
            // 归一化方向向量
            _launchDirection = dragDirection.normalized;
            
            // 计算速度大小：拖拽距离 * 速度倍数
            _launchVelocityMagnitude = dragDistance * _dragToVelocityMultiplier;
            
            // 限制速度范围
            _launchVelocityMagnitude = Mathf.Clamp(_launchVelocityMagnitude, _minLaunchVelocity, _maxLaunchVelocity);
            
            _isReadyToLaunch = true;
        }
        
        /// <summary>
        /// 更新准备状态
        /// </summary>
        private void UpdateReadyState()
        {
            // 检查是否满足发射条件
            if (_ship == null)
            {
                _isReadyToLaunch = false;
                return;
            }
            
            if (_ship.CurrentState != ShipState.PreLaunch)
            {
                _isReadyToLaunch = false;
                return;
            }
            
            // 其他条件检查...
            // 目前只要有拖拽且满足最小距离即可
        }
        
        #endregion

        #region 方向指示器
        
        /// <summary>
        /// 初始化方向指示器
        /// </summary>
        private void InitializeDirectionIndicator()
        {
            if (!_showDirectionIndicator || !_useLineRenderer)
            {
                return;
            }
            
            // 如果没有指定方向指示器对象，创建一个
            if (_directionIndicatorObject == null)
            {
                _directionIndicatorObject = new GameObject("DirectionIndicator");
                _directionIndicatorObject.transform.SetParent(_transform);
                _directionIndicatorObject.transform.localPosition = Vector3.zero;
            }
            
            // 获取或添加LineRenderer组件
            _lineRenderer = _directionIndicatorObject.GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = _directionIndicatorObject.AddComponent<LineRenderer>();
            }
            
            // 配置LineRenderer
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _directionIndicatorColor;
            _lineRenderer.endColor = _directionIndicatorColor;
            _lineRenderer.startWidth = _directionIndicatorWidth;
            _lineRenderer.endWidth = _directionIndicatorWidth;
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.enabled = false;
        }
        
        /// <summary>
        /// 更新方向指示器
        /// </summary>
        private void UpdateDirectionIndicator()
        {
            if (!_isDragging || !_isReadyToLaunch || _ship == null)
            {
                // 隐藏指示器
                if (_lineRenderer != null)
                {
                    _lineRenderer.enabled = false;
                }
                return;
            }
            
            // 显示指示器
            if (_useLineRenderer && _lineRenderer != null)
            {
                UpdateLineRenderer();
            }
            // 可以在这里添加其他可视化方式（如自定义绘制）
        }
        
        /// <summary>
        /// 更新LineRenderer
        /// </summary>
        private void UpdateLineRenderer()
        {
            if (_lineRenderer == null || _ship == null)
            {
                return;
            }
            
            Vector3 shipPosition = _ship.transform.position;
            
            // 计算指示器长度（限制最大长度）
            float indicatorLength = Mathf.Min(_launchVelocityMagnitude / _dragToVelocityMultiplier, _directionIndicatorMaxLength);
            
            // 设置起点和终点
            Vector3 startPoint = shipPosition;
            Vector3 endPoint = shipPosition + _launchDirection * indicatorLength;
            
            // 更新LineRenderer
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, startPoint);
            _lineRenderer.SetPosition(1, endPoint);
            
            // 根据速度调整颜色（可选：速度越大，颜色越红）
            float speedRatio = _launchVelocityMagnitude / _maxLaunchVelocity;
            Color currentColor = Color.Lerp(_directionIndicatorColor, Color.red, speedRatio * 0.5f);
            _lineRenderer.startColor = currentColor;
            _lineRenderer.endColor = currentColor;
        }
        
        #endregion

        #region 飞船发射
        
        /// <summary>
        /// 发射飞船
        /// </summary>
        private void LaunchShip()
        {
            if (_ship == null)
            {
                Debug.LogError("LaunchStation: 无法发射，飞船引用为空");
                return;
            }
            
            // 计算最终发射速度向量
            Vector3 launchVelocity = _launchDirection * _launchVelocityMagnitude;
            
            // 确保速度在XZ平面
            launchVelocity.y = 0f;
            
            // 调用飞船的Launch方法
            _ship.Launch(launchVelocity);
            
            // 注意：ShipController.Launch() 已经会触发 OnShipLaunch 事件
            // 这里可以选择性地额外触发事件或执行其他逻辑
            
            Debug.Log($"发射台发射飞船！速度：{launchVelocity.magnitude:F2}，方向：{_launchDirection}");
            
            // 重置拖拽状态
            ResetDragging();
        }
        
        /// <summary>
        /// 手动发射飞船（用于测试或脚本调用）
        /// </summary>
        /// <param name="direction">发射方向（世界坐标，会被归一化）</param>
        /// <param name="speed">发射速度</param>
        public void ManualLaunch(Vector3 direction, float speed)
        {
            if (_ship == null)
            {
                Debug.LogError("LaunchStation: 无法发射，飞船引用为空");
                return;
            }
            
            // 归一化方向
            direction.Normalize();
            
            // 投影到水平面
            direction.y = 0f;
            
            // 限制速度
            speed = Mathf.Clamp(speed, _minLaunchVelocity, _maxLaunchVelocity);
            
            // 计算速度向量
            Vector3 launchVelocity = direction * speed;
            
            // 发射
            _ship.Launch(launchVelocity);
        }
        
        #endregion

        #region 公共方法和属性
        
        /// <summary>
        /// 获取当前飞船引用
        /// </summary>
        public ShipController Ship => _ship;
        
        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging => _isDragging;
        
        /// <summary>
        /// 是否准备好发射
        /// </summary>
        public bool IsReadyToLaunch => _isReadyToLaunch;
        
        /// <summary>
        /// 当前计算的发射速度
        /// </summary>
        public float CurrentLaunchVelocity => _launchVelocityMagnitude;
        
        /// <summary>
        /// 当前计算的发射方向
        /// </summary>
        public Vector3 CurrentLaunchDirection => _launchDirection;
        
        /// <summary>
        /// 设置飞船引用
        /// </summary>
        public void SetShip(ShipController ship)
        {
            _ship = ship;
            
            // 更新水平面高度
            if (_lockToHorizontalPlane && _ship != null)
            {
                _horizontalPlaneHeight = _ship.transform.position.y;
            }
        }
        
        /// <summary>
        /// 设置拖拽速度倍数
        /// </summary>
        public void SetDragToVelocityMultiplier(float multiplier)
        {
            _dragToVelocityMultiplier = Mathf.Max(0.1f, multiplier);
        }
        
        /// <summary>
        /// 启用/禁用拖拽发射
        /// </summary>
        public void SetDragLaunchEnabled(bool enabled)
        {
            _enableDragLaunch = enabled;
            
            if (!enabled)
            {
                ResetDragging();
            }
        }
        
        #endregion

        #region 调试和可视化
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制水平面
            if (_lockToHorizontalPlane)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(
                    new Vector3(transform.position.x, _horizontalPlaneHeight, transform.position.z),
                    new Vector3(10f, 0.1f, 10f)
                );
            }
            
            // 绘制拖拽信息
            if (Application.isPlaying && _isDragging)
            {
                // 绘制拖拽起始位置
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_dragStartPosition, 0.5f);
                
                // 绘制拖拽当前位置
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_dragCurrentPosition, 0.5f);
                
                // 绘制拖拽方向线
                if (_isReadyToLaunch)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(_dragStartPosition, _dragCurrentPosition);
                    
                    // 绘制发射方向箭头
                    if (_ship != null)
                    {
                        Vector3 shipPos = _ship.transform.position;
                        Vector3 arrowEnd = shipPos + _launchDirection * (_launchVelocityMagnitude / _dragToVelocityMultiplier);
                        Gizmos.DrawLine(shipPos, arrowEnd);
                    }
                }
            }
        }
        
        #endregion
    }
}