using UnityEngine;
using SpaceArchitect.Core;
using SpaceArchitect.Gameplay;

namespace SpaceArchitect.CelestialObjects
{
    /// <summary>
    /// 行星控制器
    /// 管理行星的引力场、捕获判定和资源系统
    /// 实现IGravitySource接口，提供引力计算
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Planet : MonoBehaviour, IGravitySource
    {
        #region 引力参数
        
        [Header("引力参数")]
        [Tooltip("行星质量（用于引力计算）")]
        [SerializeField] private float _mass = 100f;
        
        [Tooltip("引力常数（游戏内自定义，与ShipController中的值保持一致）")]
        [SerializeField] private float _gravityConstant = 100f;
        
        [Tooltip("最大引力影响距离（超过此距离忽略引力）")]
        [SerializeField] private float _maxGravityDistance = 1000f;
        
        [Tooltip("引力影响的最小距离（防止除零错误）")]
        [SerializeField] private float _minGravityDistance = 0.1f;
        
        #endregion

        #region 捕获系统参数
        
        [Header("捕获系统参数")]
        [Tooltip("捕获范围半径（飞船进入此范围可被捕获）")]
        [SerializeField] private float _captureRadius = 50f;
        
        [Tooltip("捕获所需的最小速度")]
        [SerializeField] private float _minCaptureSpeed = 2f;
        
        [Tooltip("捕获所需的最大速度")]
        [SerializeField] private float _maxCaptureSpeed = 20f;
        
        [Tooltip("捕获角度阈值（飞船运动方向与径向的夹角，单位：度）")]
        [SerializeField] private float _captureAngleThreshold = 60f;
        
        [Tooltip("是否启用捕获功能")]
        [SerializeField] private bool _enableCapture = true;
        
        [Tooltip("当前已捕获的飞船（null表示未捕获）")]
        private ShipController _capturedShip;
        
        #endregion

        #region 碰撞体参数
        
        [Header("碰撞体设置")]
        [Tooltip("碰撞体半径（用于碰撞检测）")]
        [SerializeField] private float _collisionRadius = 10f;
        
        [Tooltip("捕获触发器标签（用于识别捕获触发器）")]
        [SerializeField] private string _captureTriggerTag = "PlanetCapture";
        
        #endregion

        #region 资源系统参数（后续扩展）
        
        [Header("资源系统（后续扩展）")]
        [Tooltip("资源类型（后续实现）")]
        [SerializeField] private string _resourceType = "None";
        
        [Tooltip("资源数量（后续实现）")]
        [SerializeField] private int _resourceAmount = 0;
        
        #endregion

        #region 组件引用
        
        private Transform _transform;
        private SphereCollider _collider;
        private SphereCollider _captureTrigger;
        private EventManager _eventManager;
        
        #endregion

        #region Unity生命周期
        
        private void Awake()
        {
            // 获取组件引用
            _transform = transform;
            _collider = GetComponent<SphereCollider>();
            
            // 获取EventManager实例
            _eventManager = EventManager.Instance;
            
            // 设置碰撞体
            SetupColliders();
        }
        
        private void Start()
        {
            // 确保捕获触发器已正确设置
            ValidateCaptureTrigger();
        }
        
        #endregion

        #region 碰撞体设置
        
        /// <summary>
        /// 设置碰撞体
        /// </summary>
        private void SetupColliders()
        {
            // 主碰撞体：用于碰撞检测（非触发器）
            if (_collider != null)
            {
                _collider.radius = _collisionRadius;
                _collider.isTrigger = false; // 实心碰撞体
            }
            
            // 创建捕获触发器（子对象）
            CreateCaptureTrigger();
        }
        
        /// <summary>
        /// 创建捕获触发器
        /// </summary>
        private void CreateCaptureTrigger()
        {
            // 查找是否已有捕获触发器
            Transform triggerTransform = _transform.Find("CaptureTrigger");
            
            if (triggerTransform == null)
            {
                // 创建新的GameObject作为捕获触发器
                GameObject triggerObject = new GameObject("CaptureTrigger");
                triggerObject.transform.SetParent(_transform);
                triggerObject.transform.localPosition = Vector3.zero;
                triggerObject.transform.localRotation = Quaternion.identity;
                triggerObject.transform.localScale = Vector3.one;
                
                // 添加SphereCollider组件
                _captureTrigger = triggerObject.AddComponent<SphereCollider>();
                _captureTrigger.isTrigger = true;
                _captureTrigger.radius = _captureRadius;
                
                // 设置标签（如果存在）
                if (!string.IsNullOrEmpty(_captureTriggerTag))
                {
                    // 注意：需要在Unity编辑器中手动创建标签
                    try
                    {
                        triggerObject.tag = _captureTriggerTag;
                    }
                    catch
                    {
                        Debug.LogWarning($"标签 '{_captureTriggerTag}' 不存在，请在Unity编辑器中创建该标签");
                    }
                }
            }
            else
            {
                _captureTrigger = triggerTransform.GetComponent<SphereCollider>();
                if (_captureTrigger == null)
                {
                    _captureTrigger = triggerTransform.gameObject.AddComponent<SphereCollider>();
                    _captureTrigger.isTrigger = true;
                }
                _captureTrigger.radius = _captureRadius;
            }
        }
        
        /// <summary>
        /// 验证捕获触发器
        /// </summary>
        private void ValidateCaptureTrigger()
        {
            if (_captureTrigger == null)
            {
                Debug.LogError($"行星 {gameObject.name} 的捕获触发器未正确设置！");
                return;
            }
            
            if (!_captureTrigger.isTrigger)
            {
                Debug.LogWarning($"行星 {gameObject.name} 的捕获触发器未设置为触发器，正在修正...");
                _captureTrigger.isTrigger = true;
            }
            
            // 更新捕获半径
            _captureTrigger.radius = _captureRadius;
            
            // 验证并设置标签
            if (!string.IsNullOrEmpty(_captureTriggerTag))
            {
                GameObject triggerObject = _captureTrigger.gameObject;
                if (triggerObject.tag != _captureTriggerTag)
                {
                    try
                    {
                        triggerObject.tag = _captureTriggerTag;
                    }
                    catch
                    {
                        Debug.LogWarning($"行星 {gameObject.name} 的捕获触发器标签 '{_captureTriggerTag}' 不存在，请在Unity编辑器中创建该标签");
                    }
                }
            }
        }
        
        #endregion

        #region IGravitySource接口实现
        
        /// <summary>
        /// 实现IGravitySource接口：计算给定位置的引力向量
        /// 使用万有引力公式：F = G * M / r²
        /// </summary>
        /// <param name="position">计算引力的位置（世界坐标）</param>
        /// <returns>引力向量（世界坐标，单位：m/s²，表示加速度）</returns>
        public Vector3 GetGravityForce(Vector3 position)
        {
            // 计算方向向量（从目标位置指向行星中心）
            Vector3 direction = (Position - position);
            
            // 计算距离
            float distance = direction.magnitude;
            
            // 检查距离范围
            if (distance < _minGravityDistance)
            {
                // 距离太近，返回零向量（避免除零错误）
                return Vector3.zero;
            }
            
            if (distance > _maxGravityDistance)
            {
                // 距离太远，忽略引力
                return Vector3.zero;
            }
            
            // 归一化方向向量
            direction.Normalize();
            
            // 计算引力大小：F = G * M / r²
            float forceMagnitude = (_gravityConstant * _mass) / (distance * distance);
            
            // 返回引力向量（投影到XZ平面，因为游戏在水平面运动）
            Vector3 gravityVector = direction * forceMagnitude;
            gravityVector.y = 0f; // 投影到XZ平面
            
            return gravityVector;
        }
        
        /// <summary>
        /// 实现IGravitySource接口：获取行星位置
        /// </summary>
        public Vector3 Position => _transform.position;
        
        #endregion

        #region 捕获判定系统
        
        /// <summary>
        /// 捕获触发器检测（Unity触发器回调）
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 检查是否启用捕获
            if (!_enableCapture)
            {
                return;
            }
            
            // 检查是否已有捕获的飞船
            if (_capturedShip != null)
            {
                // 如果已经捕获了飞船，忽略其他飞船
                return;
            }
            
            // 获取飞船控制器组件
            ShipController ship = other.GetComponent<ShipController>();
            if (ship == null)
            {
                return;
            }
            
            // 检查飞船是否仍然存在且有效
            if (ship == null || ship.gameObject == null)
            {
                return;
            }
            
            // 执行捕获判定
            TryCaptureShip(ship);
        }
        
        /// <summary>
        /// 触发器退出时检查是否需要释放飞船
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            // 如果退出的对象是当前捕获的飞船，释放它
            ShipController ship = other.GetComponent<ShipController>();
            if (ship != null && ship == _capturedShip)
            {
                // 注意：飞船可能因为Boost而脱离，这里只是清理引用
                // 实际的状态转换由ShipController的Boost逻辑处理
                _capturedShip = null;
            }
        }
        
        /// <summary>
        /// 尝试捕获飞船
        /// </summary>
        /// <param name="ship">飞船控制器</param>
        private void TryCaptureShip(ShipController ship)
        {
            // 检查飞船是否处于可捕获状态（必须是飞行状态）
            if (ship.CurrentState != ShipState.Flying)
            {
                return;
            }
            
            // 判定1：距离判定
            if (!CheckDistanceCondition(ship))
            {
                return;
            }
            
            // 判定2：速度判定
            if (!CheckVelocityCondition(ship))
            {
                return;
            }
            
            // 判定3：角度判定
            if (!CheckAngleCondition(ship))
            {
                return;
            }
            
            // 所有条件满足，执行捕获
            CaptureShip(ship);
        }
        
        /// <summary>
        /// 判定1：距离判定
        /// 检查飞船是否在捕获范围内
        /// </summary>
        private bool CheckDistanceCondition(ShipController ship)
        {
            Vector3 shipPosition = ship.transform.position;
            Vector3 planetPosition = Position;
            
            // 投影到同一水平面
            shipPosition.y = planetPosition.y;
            
            float distance = Vector3.Distance(shipPosition, planetPosition);
            
            // 检查是否在捕获范围内
            bool isWithinRange = distance <= _captureRadius;
            
            if (!isWithinRange)
            {
                Debug.Log($"[捕获判定] 距离不满足：{distance:F2} > {_captureRadius:F2}");
            }
            
            return isWithinRange;
        }
        
        /// <summary>
        /// 判定2：速度判定
        /// 检查飞船速度是否在合适范围内
        /// </summary>
        private bool CheckVelocityCondition(ShipController ship)
        {
            float speed = ship.Velocity.magnitude;
            
            // 检查速度是否在捕获范围内
            bool isValidSpeed = speed >= _minCaptureSpeed && speed <= _maxCaptureSpeed;
            
            if (!isValidSpeed)
            {
                Debug.Log($"[捕获判定] 速度不满足：{speed:F2} 不在 [{_minCaptureSpeed:F2}, {_maxCaptureSpeed:F2}] 范围内");
            }
            
            return isValidSpeed;
        }
        
        /// <summary>
        /// 判定3：角度判定
        /// 检查飞船运动方向与径向的夹角是否合适
        /// 角度越小（接近垂直），越容易捕获
        /// </summary>
        private bool CheckAngleCondition(ShipController ship)
        {
            Vector3 shipPosition = ship.transform.position;
            Vector3 planetPosition = Position;
            Vector3 shipVelocity = ship.Velocity;
            
            // 投影到同一水平面
            shipPosition.y = planetPosition.y;
            shipVelocity.y = 0f;
            
            // 计算径向向量（从行星指向飞船）
            Vector3 radialDirection = (shipPosition - planetPosition).normalized;
            
            // 检查速度是否为零
            if (shipVelocity.magnitude < 0.01f)
            {
                Debug.Log("[捕获判定] 速度为零，无法计算角度");
                return false;
            }
            
            // 计算速度方向
            Vector3 velocityDirection = shipVelocity.normalized;
            
            // 计算夹角（点积）
            float dotProduct = Vector3.Dot(radialDirection, velocityDirection);
            
            // 将点积转换为角度（0-180度）
            float angle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * Mathf.Rad2Deg;
            
            // 角度越小，速度方向越接近径向，越容易捕获
            // 但实际捕获通常需要一定的切向速度（角度接近90度）
            // 这里使用阈值：如果角度太大（接近180度或0度），说明速度方向不合适
            // 我们允许角度在 [90 - threshold, 90 + threshold] 范围内
            
            float optimalAngle = 90f; // 最优角度是90度（完全切向）
            float angleDifference = Mathf.Abs(angle - optimalAngle);
            
            bool isValidAngle = angleDifference <= _captureAngleThreshold;
            
            if (!isValidAngle)
            {
                Debug.Log($"[捕获判定] 角度不满足：{angle:F2}° 与最优角度 {optimalAngle:F2}° 的差值为 {angleDifference:F2}° > {_captureAngleThreshold:F2}°");
            }
            else
            {
                Debug.Log($"[捕获判定] 角度满足：{angle:F2}° (与最优角度差 {angleDifference:F2}°)");
            }
            
            return isValidAngle;
        }
        
        /// <summary>
        /// 执行捕获
        /// </summary>
        /// <param name="ship">被捕获的飞船</param>
        private void CaptureShip(ShipController ship)
        {
            // 设置捕获的飞船
            _capturedShip = ship;
            
            // 触发捕获事件
            if (_eventManager != null)
            {
                // 传递行星自身（MonoBehaviour）作为参数
                _eventManager.TriggerEvent<MonoBehaviour>(GameEventType.OnShipCaptured, this);
            }
            
            Debug.Log($"行星 {gameObject.name} 成功捕获飞船 {ship.gameObject.name}！");
        }
        
        /// <summary>
        /// 释放捕获的飞船（当飞船使用Boost脱离时调用，或手动释放）
        /// </summary>
        public void ReleaseShip()
        {
            if (_capturedShip != null)
            {
                Debug.Log($"行星 {gameObject.name} 释放飞船 {_capturedShip.gameObject.name}");
                _capturedShip = null;
            }
        }
        
        #endregion

        #region 公共属性和方法
        
        /// <summary>
        /// 获取行星质量
        /// </summary>
        public float Mass => _mass;
        
        /// <summary>
        /// 获取捕获范围半径
        /// </summary>
        public float CaptureRadius => _captureRadius;
        
        /// <summary>
        /// 获取当前是否捕获了飞船
        /// </summary>
        public bool HasCapturedShip => _capturedShip != null;
        
        /// <summary>
        /// 获取当前捕获的飞船
        /// </summary>
        public ShipController CapturedShip => _capturedShip;
        
        /// <summary>
        /// 设置捕获参数（运行时修改）
        /// </summary>
        public void SetCaptureParameters(float radius, float minSpeed, float maxSpeed, float angleThreshold)
        {
            _captureRadius = radius;
            _minCaptureSpeed = minSpeed;
            _maxCaptureSpeed = maxSpeed;
            _captureAngleThreshold = angleThreshold;
            
            // 更新触发器半径
            if (_captureTrigger != null)
            {
                _captureTrigger.radius = _captureRadius;
            }
        }
        
        #endregion

        #region 调试和可视化
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制引力影响范围
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _maxGravityDistance);
            
            // 绘制捕获范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _captureRadius);
            
            // 绘制碰撞范围
            Gizmos.color = Color.red;
            float collisionRadius = _collider != null ? _collider.radius : _collisionRadius;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
        
        /// <summary>
        /// 选中时绘制详细信息
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制更明显的引力影响范围
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _maxGravityDistance);
            
            // 绘制捕获范围（更明显）
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _captureRadius);
        }
        
        #endregion

        #region 编辑器辅助方法
        
        /// <summary>
        /// 在编辑器中验证参数
        /// </summary>
        private void OnValidate()
        {
            // 确保参数合理
            _mass = Mathf.Max(0.1f, _mass);
            _captureRadius = Mathf.Max(0.1f, _captureRadius);
            _minCaptureSpeed = Mathf.Max(0f, _minCaptureSpeed);
            _maxCaptureSpeed = Mathf.Max(_minCaptureSpeed, _maxCaptureSpeed);
            _captureAngleThreshold = Mathf.Clamp(_captureAngleThreshold, 0f, 180f);
            
            // 更新触发器半径（如果存在）
            if (_captureTrigger != null)
            {
                _captureTrigger.radius = _captureRadius;
            }
            
            // 更新碰撞体半径（如果存在）
            if (_collider != null)
            {
                _collider.radius = _collisionRadius;
            }
        }
        
        #endregion
    }
}