using UnityEngine;
using SpaceArchitect.Core;

namespace SpaceArchitect.Gameplay
{
    /// <summary>
    /// 飞船状态枚举
    /// 定义飞船在一局游戏中可能的所有状态
    /// </summary>
    public enum ShipState
    {
        PreLaunch,      // 发射前：位置锁定在发射台
        Flying,         // 正常飞行：受引力作用运动
        Captured,       // 被捕获：围绕行星做匀速圆周运动，忽略引力
        Crashed,        // 坠毁：与行星或障碍物碰撞
        Arrived,        // 成功抵达：到达目的地
        Escaped         // 逃逸：飞出游戏边界
    }

    /// <summary>
    /// 飞船控制器
    /// 管理飞船的状态机、物理运动、输入响应和事件交互
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour, IGravitySource
    {
        #region 状态机变量
        
        /// <summary>
        /// 当前飞船状态
        /// </summary>
        private ShipState _currentState = ShipState.PreLaunch;
        
        /// <summary>
        /// 当前状态属性（公开只读）
        /// </summary>
        public ShipState CurrentState => _currentState;
        
        /// <summary>
        /// 是否允许状态转换
        /// </summary>
        private bool _canChangeState = true;
        
        #endregion

        #region 物理运动变量
        
        [Header("物理参数")]
        [Tooltip("飞船质量（用于物理计算）")]
        [SerializeField] private float _mass = 1f;
        
        [Tooltip("当前速度（世界坐标，XZ平面）")]
        private Vector3 _velocity;
        
        /// <summary>
        /// 当前速度属性（公开只读）
        /// </summary>
        public Vector3 Velocity => _velocity;
        
        [Tooltip("锁定的Y坐标（水平面，默认0）")]
        [SerializeField] private float _horizontalPlaneHeight = 0f;
        
        /// <summary>
        /// 锁定的Y坐标（内部使用）
        /// </summary>
        private float _lockedYPosition;
        
        [Tooltip("固定时间步长（用于物理计算）")]
        private const float FIXED_TIMESTEP = 0.02f;
        
        #endregion

        #region 引力系统变量
        
        [Header("引力参数")]
        [Tooltip("引力常数（游戏内自定义，非真实值）")]
        [SerializeField] private float _gravityConstant = 100f;
        
        [Tooltip("最大引力影响距离（超过此距离忽略引力）")]
        [SerializeField] private float _maxGravityDistance = 1000f;
        
        /// <summary>
        /// 场景中所有引力源的缓存
        /// </summary>
        private IGravitySource[] _gravitySources;
        
        /// <summary>
        /// 是否忽略引力作用（被捕获时设为true）
        /// </summary>
        private bool _ignoreGravity = false;
        
        #endregion

        #region 捕获系统变量
        
        [Header("捕获参数")]
        [Tooltip("当前捕获飞船的行星组件（null表示未被捕获）")]
        private MonoBehaviour _capturedByPlanet;
        
        [Tooltip("捕获时的轨道半径")]
        private float _captureOrbitRadius;
        
        [Tooltip("捕获时的角速度（弧度/秒）")]
        private float _captureAngularVelocity;
        
        [Tooltip("捕获时的初始角度（相对于行星）")]
        private float _captureInitialAngle;
        
        [Tooltip("捕获时的初始时间")]
        private float _captureStartTime;
        
        #endregion

        #region Boost技能变量
        
        [Header("Boost技能参数")]
        [Tooltip("Boost技能冷却时间（秒）")]
        [SerializeField] private float _boostCooldown = 2f;
        
        [Tooltip("Boost技能持续时间（秒）")]
        [SerializeField] private float _boostDuration = 0.5f;
        
        [Tooltip("Boost加速倍数")]
        [SerializeField] private float _boostForce = 50f;
        
        [Tooltip("Boost冷却计时器")]
        private float _boostCooldownTimer = 0f;
        
        [Tooltip("Boost持续时间计时器")]
        private float _boostDurationTimer = 0f;
        
        [Tooltip("是否正在使用Boost")]
        private bool _isBoosting = false;
        
        #endregion

        #region 碰撞检测变量
        
        [Header("碰撞检测")]
        [Tooltip("碰撞标签（障碍物和行星使用此标签触发碰撞）")]
        [SerializeField] private string _collisionTag = "Obstacle";
        
        [Tooltip("目的地标签")]
        [SerializeField] private string _destinationTag = "Destination";
        
        [Tooltip("捕获触发器标签")]
        [SerializeField] private string _captureTriggerTag = "PlanetCapture";
        
        #endregion

        #region 边界检测变量
        
        [Header("边界检测")]
        [Tooltip("游戏边界大小（世界坐标单位）")]
        [SerializeField] private float _boundarySize = 1000f;
        
        [Tooltip("边界中心点（通常为原点）")]
        [SerializeField] private Vector3 _boundaryCenter = Vector3.zero;
        
        #endregion

        #region 组件引用
        
        private Rigidbody _rigidbody;
        private Transform _transform;
        
        #endregion

        #region 事件系统
        
        private EventManager _eventManager;
        
        #endregion

        #region Unity生命周期
        
        private void Awake()
        {
            // 获取组件引用
            _rigidbody = GetComponent<Rigidbody>();
            _transform = transform;
            
            // 获取EventManager实例
            _eventManager = EventManager.Instance;
            
            // 初始化状态
            InitializeState();
        }
        
        private void Start()
        {
            // 设置锁定Y坐标为水平面高度（默认0）
            _lockedYPosition = _horizontalPlaneHeight;
            
            // 强制设置飞船位置到水平面
            Vector3 currentPos = _transform.position;
            currentPos.y = _lockedYPosition;
            _transform.position = currentPos;
            
            // 查找场景中所有引力源
            RefreshGravitySources();
            
            // 订阅捕获事件（使用MonoBehaviour作为参数类型，因为Planet可能还未定义）
            if (_eventManager != null)
            {
                _eventManager.Subscribe<MonoBehaviour>(GameEventType.OnShipCaptured, OnCapturedByPlanet);
            }
            
            // 禁用Unity物理引擎的重力（使用自定义引力系统）
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true; // 使用自定义物理，不需要Unity物理引擎
            }
        }
        
        private void Update()
        {
            // 处理输入（在Update中检测，确保响应及时）
            HandleInput();
            
            // 更新Boost计时器
            UpdateBoostTimers();
            
            // 检查边界（逃逸判定）
            CheckBoundaries();
            
            // 持续锁定Y坐标（防止其他因素影响位置）
            EnsureYPositionLocked();
        }
        
        private void FixedUpdate()
        {
            // 在FixedUpdate中更新物理运动（固定时间步长）
            UpdatePhysics(Time.fixedDeltaTime);
        }
        
        private void OnDestroy()
        {
            // 取消事件订阅
            if (_eventManager != null)
            {
                _eventManager.Unsubscribe<MonoBehaviour>(GameEventType.OnShipCaptured, OnCapturedByPlanet);
            }
        }
        
        #endregion

        #region 状态机系统
        
        /// <summary>
        /// 初始化状态
        /// </summary>
        private void InitializeState()
        {
            _currentState = ShipState.PreLaunch;
            _canChangeState = true;
            OnStateEnter(_currentState);
        }
        
        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="newState">新状态</param>
        private void ChangeState(ShipState newState)
        {
            if (!_canChangeState)
            {
                Debug.LogWarning($"无法切换状态：当前状态 {_currentState} 已锁定");
                return;
            }
            
            if (_currentState == newState)
            {
                return; // 状态相同，无需切换
            }
            
            // 执行状态退出逻辑
            OnStateExit(_currentState);
            
            // 切换状态
            ShipState oldState = _currentState;
            _currentState = newState;
            
            // 执行状态进入逻辑
            OnStateEnter(_currentState);
            
            Debug.Log($"飞船状态切换：{oldState} → {_currentState}");
        }
        
        /// <summary>
        /// 状态进入时的逻辑
        /// </summary>
        private void OnStateEnter(ShipState state)
        {
            switch (state)
            {
                case ShipState.PreLaunch:
                    // 发射前：锁定位置
                    LockPosition();
                    _velocity = Vector3.zero;
                    // 确保Y坐标锁定
                    EnsureYPositionLocked();
                    break;
                    
                case ShipState.Flying:
                    // 正常飞行：启用引力作用
                    _ignoreGravity = false;
                    _capturedByPlanet = null;
                    // 确保Y坐标锁定
                    EnsureYPositionLocked();
                    break;
                    
                case ShipState.Captured:
                    // 被捕获：忽略引力，开始圆周运动
                    _ignoreGravity = true;
                    InitializeCaptureOrbit();
                    // 确保Y坐标锁定
                    EnsureYPositionLocked();
                    break;
                    
                case ShipState.Crashed:
                    // 坠毁：停止所有运动
                    _velocity = Vector3.zero;
                    _canChangeState = false; // 锁定状态，无法再切换
                    break;
                    
                case ShipState.Arrived:
                    // 抵达：停止运动
                    _velocity = Vector3.zero;
                    _canChangeState = false;
                    break;
                    
                case ShipState.Escaped:
                    // 逃逸：停止运动
                    _velocity = Vector3.zero;
                    _canChangeState = false;
                    break;
            }
        }
        
        /// <summary>
        /// 状态退出时的逻辑
        /// </summary>
        private void OnStateExit(ShipState state)
        {
            switch (state)
            {
                case ShipState.Captured:
                    // 离开捕获状态：恢复引力作用
                    _ignoreGravity = false;
                    _capturedByPlanet = null;
                    break;
            }
        }
        
        #endregion

        #region 发射系统
        
        /// <summary>
        /// 发射飞船（由LaunchStation调用）
        /// </summary>
        /// <param name="initialVelocity">初始速度向量（世界坐标，XZ平面）</param>
        public void Launch(Vector3 initialVelocity)
        {
            if (_currentState != ShipState.PreLaunch)
            {
                Debug.LogWarning($"无法发射：当前状态为 {_currentState}，只能从PreLaunch状态发射");
                return;
            }
            
            // 确保速度在XZ平面（Y分量为0）
            initialVelocity.y = 0f;
            
            // 设置初始速度
            _velocity = initialVelocity;
            
            // 确保飞船位置在水平面上
            Vector3 currentPos = _transform.position;
            currentPos.y = _lockedYPosition;
            _transform.position = currentPos;
            
            // 切换到飞行状态
            ChangeState(ShipState.Flying);
            
            // 触发发射事件
            if (_eventManager != null)
            {
                _eventManager.TriggerEvent<Vector3>(GameEventType.OnShipLaunch, initialVelocity);
            }
            
            Debug.Log($"飞船发射！初始速度：{initialVelocity}");
        }
        
        /// <summary>
        /// 锁定位置（发射前）
        /// </summary>
        private void LockPosition()
        {
            Vector3 pos = _transform.position;
            pos.y = _lockedYPosition;
            _transform.position = pos;
        }
        
        #endregion

        #region 物理运动系统
        
        /// <summary>
        /// 更新物理运动（在FixedUpdate中调用）
        /// </summary>
        private void UpdatePhysics(float deltaTime)
        {
            // 根据当前状态执行不同的物理更新
            switch (_currentState)
            {
                case ShipState.PreLaunch:
                    // 发射前：保持位置锁定
                    LockPosition();
                    break;
                    
                case ShipState.Flying:
                    // 正常飞行：应用引力和速度
                    UpdateFlyingPhysics(deltaTime);
                    break;
                    
                case ShipState.Captured:
                    // 被捕获：做匀速圆周运动
                    UpdateCapturedPhysics(deltaTime);
                    break;
                    
                case ShipState.Crashed:
                case ShipState.Arrived:
                case ShipState.Escaped:
                    // 终态：不更新物理
                    break;
            }
        }
        
        /// <summary>
        /// 更新飞行状态的物理
        /// </summary>
        private void UpdateFlyingPhysics(float deltaTime)
        {
            // 计算并应用引力（如果不是忽略引力）
            if (!_ignoreGravity)
            {
                Vector3 totalGravity = CalculateTotalGravity();
                
                // 应用引力加速度：v = v0 + a*t
                _velocity += totalGravity * deltaTime;
                
                // 应用Boost加速
                if (_isBoosting)
                {
                    Vector3 boostDirection = _velocity.normalized;
                    if (boostDirection == Vector3.zero)
                    {
                        boostDirection = _transform.forward;
                    }
                    _velocity += boostDirection * _boostForce * deltaTime;
                }
            }
            
            // 更新位置：p = p0 + v*t
            Vector3 newPosition = _transform.position + _velocity * deltaTime;
            
            // 锁定Y坐标（水平面运动）
            newPosition.y = _lockedYPosition;
            
            _transform.position = newPosition;
            
            // 确保速度的Y分量为0（防止累积误差）
            _velocity.y = 0f;
            
            // 更新旋转（朝向运动方向）
            if (_velocity.magnitude > 0.1f)
            {
                Vector3 lookDirection = _velocity.normalized;
                lookDirection.y = 0; // 只在XZ平面旋转
                _transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }
        
        /// <summary>
        /// 更新捕获状态的物理（匀速圆周运动）
        /// </summary>
        private void UpdateCapturedPhysics(float deltaTime)
        {
            if (_capturedByPlanet == null)
            {
                // 如果没有捕获行星，切换到飞行状态
                ChangeState(ShipState.Flying);
                return;
            }
            
            // 计算当前时间相对于捕获开始的时间
            float elapsedTime = Time.time - _captureStartTime;
            
            // 计算当前角度：θ = θ0 + ω*t
            float currentAngle = _captureInitialAngle + _captureAngularVelocity * elapsedTime;
            
            // 计算新位置（围绕行星的圆周运动）
            Vector3 planetPosition;
            if (_capturedByPlanet is IGravitySource gravitySource)
            {
                planetPosition = gravitySource.Position;
            }
            else
            {
                planetPosition = _capturedByPlanet.transform.position;
            }
            Vector3 offset = new Vector3(
                Mathf.Cos(currentAngle) * _captureOrbitRadius,
                0f,
                Mathf.Sin(currentAngle) * _captureOrbitRadius
            );
            
            Vector3 newPosition = planetPosition + offset;
            newPosition.y = _lockedYPosition; // 锁定Y坐标到水平面
            
            // 更新位置
            _transform.position = newPosition;
            
            // 确保Y坐标锁定（双重保险）
            EnsureYPositionLocked();
            
            // 计算切线速度（用于显示）
            float tangentialSpeed = _captureAngularVelocity * _captureOrbitRadius;
            Vector3 tangentDirection = new Vector3(-Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle));
            _velocity = tangentDirection * tangentialSpeed;
            
            // 朝向运动方向
            _transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
        }
        
        /// <summary>
        /// 初始化捕获轨道参数
        /// </summary>
        private void InitializeCaptureOrbit()
        {
            if (_capturedByPlanet == null)
            {
                return;
            }
            
            // 计算轨道半径（飞船到行星的距离）
            Vector3 planetPosition;
            if (_capturedByPlanet is IGravitySource gravitySourceForCapture)
            {
                planetPosition = gravitySourceForCapture.Position;
            }
            else
            {
                planetPosition = _capturedByPlanet.transform.position;
            }
            Vector3 shipPosition = _transform.position;
            shipPosition.y = planetPosition.y; // 投影到同一水平面
            _captureOrbitRadius = Vector3.Distance(shipPosition, planetPosition);
            
            // 计算初始角度
            Vector3 direction = (shipPosition - planetPosition).normalized;
            _captureInitialAngle = Mathf.Atan2(direction.z, direction.x);
            
            // 计算角速度（使用捕获时的速度）
            float speed = _velocity.magnitude;
            if (speed > 0 && _captureOrbitRadius > 0)
            {
                _captureAngularVelocity = speed / _captureOrbitRadius;
            }
            else
            {
                // 默认角速度
                _captureAngularVelocity = 1f / _captureOrbitRadius;
            }
            
            // 记录捕获开始时间
            _captureStartTime = Time.time;
            
            Debug.Log($"飞船被捕获，轨道半径：{_captureOrbitRadius}，角速度：{_captureAngularVelocity}");
        }
        
        #endregion

        #region 引力系统
        
        /// <summary>
        /// 刷新场景中所有引力源
        /// </summary>
        public void RefreshGravitySources()
        {
            // 查找所有实现IGravitySource接口的对象
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            System.Collections.Generic.List<IGravitySource> gravitySources = new System.Collections.Generic.List<IGravitySource>();
            
            foreach (var mb in allMonoBehaviours)
            {
                // 跳过自己
                if (mb == this)
                    continue;
                
                // 检查是否实现IGravitySource接口
                if (mb is IGravitySource gravitySource)
                {
                    gravitySources.Add(gravitySource);
                }
            }
            
            _gravitySources = gravitySources.ToArray();
            
            Debug.Log($"找到 {_gravitySources.Length} 个引力源");
        }
        
        /// <summary>
        /// 计算所有引力源对飞船的总引力
        /// </summary>
        /// <returns>总引力向量（世界坐标）</returns>
        private Vector3 CalculateTotalGravity()
        {
            Vector3 totalGravity = Vector3.zero;
            Vector3 shipPosition = _transform.position;
            
            // 遍历所有引力源，累加引力
            if (_gravitySources != null)
            {
                foreach (var source in _gravitySources)
                {
                    if (source == null || source == (IGravitySource)this)
                        continue;
                    
                    Vector3 gravity = source.GetGravityForce(shipPosition);
                    totalGravity += gravity;
                }
            }
            
            // 投影到XZ平面（水平面运动）
            totalGravity.y = 0f;
            
            return totalGravity;
        }
        
        /// <summary>
        /// 实现IGravitySource接口（飞船本身也可以作为引力源，但通常质量很小）
        /// 这里返回零向量，飞船通常不作为引力源
        /// </summary>
        public Vector3 GetGravityForce(Vector3 position)
        {
            // 飞船通常不产生引力，返回零向量
            // 如果需要，可以在这里实现飞船的引力
            return Vector3.zero;
        }
        
        /// <summary>
        /// 实现IGravitySource接口的位置属性
        /// </summary>
        public Vector3 Position => _transform.position;
        
        #endregion

        #region 输入响应系统
        
        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 只在飞行状态或被捕获状态允许使用技能
            if (_currentState != ShipState.Flying && _currentState != ShipState.Captured)
            {
                return;
            }
            
            // Boost技能（E键）
            if (Input.GetKeyDown(KeyCode.E))
            {
                UseBoost();
            }
            
            // 轨迹预测技能（Q键）- 占位，后续实现
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // TODO: 实现轨迹预测
                Debug.Log("轨迹预测技能（待实现）");
            }
        }
        
        /// <summary>
        /// 使用Boost技能
        /// </summary>
        private void UseBoost()
        {
            // 检查冷却时间
            if (_boostCooldownTimer > 0f)
            {
                Debug.Log($"Boost技能冷却中：{_boostCooldownTimer:F2}秒");
                return;
            }
            
            // 如果当前处于捕获状态，使用Boost会脱离捕获
            if (_currentState == ShipState.Captured)
            {
                // 脱离捕获状态，恢复飞行状态
                ChangeState(ShipState.Flying);
                
                // 给飞船一个脱离速度（使用当前速度方向）
                if (_velocity.magnitude > 0)
                {
                    _velocity *= 1.5f; // 增加速度
                }
                else
                {
                    // 如果没有速度，给一个默认方向的速度
                    Vector3 planetPos = _capturedByPlanet is IGravitySource gs ? gs.Position : _capturedByPlanet.transform.position;
                    Vector3 escapeDirection = (_transform.position - planetPos).normalized;
                    escapeDirection.y = 0;
                    _velocity = escapeDirection * 10f;
                }
                
                Debug.Log("Boost技能：脱离捕获状态");
            }
            
            // 启动Boost
            _isBoosting = true;
            _boostDurationTimer = _boostDuration;
            _boostCooldownTimer = _boostCooldown;
            
            Debug.Log("Boost技能激活！");
        }
        
        /// <summary>
        /// 更新Boost计时器
        /// </summary>
        private void UpdateBoostTimers()
        {
            // 更新Boost持续时间
            if (_boostDurationTimer > 0f)
            {
                _boostDurationTimer -= Time.deltaTime;
                if (_boostDurationTimer <= 0f)
                {
                    _isBoosting = false;
                }
            }
            
            // 更新Boost冷却时间
            if (_boostCooldownTimer > 0f)
            {
                _boostCooldownTimer -= Time.deltaTime;
            }
        }
        
        #endregion

        #region 碰撞检测系统
        
        /// <summary>
        /// Unity碰撞检测（与障碍物或行星碰撞体）
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // 只在飞行状态检测碰撞
            if (_currentState != ShipState.Flying)
            {
                return;
            }
            
            // 检查碰撞标签
            if (collision.gameObject.CompareTag(_collisionTag))
            {
                // 与障碍物或行星碰撞，触发坠毁
                Crash();
            }
        }
        
        /// <summary>
        /// Unity触发器检测（用于目的地检测）
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 检查是否抵达目的地
            if (other.CompareTag(_destinationTag))
            {
                if (_currentState == ShipState.Flying)
                {
                    ArriveAtDestination();
                }
            }
        }
        
        /// <summary>
        /// 坠毁处理
        /// </summary>
        private void Crash()
        {
            if (_currentState == ShipState.Crashed || !_canChangeState)
            {
                return;
            }
            
            ChangeState(ShipState.Crashed);
            
            // 触发坠毁事件
            if (_eventManager != null)
            {
                _eventManager.TriggerEvent(GameEventType.OnShipCrashed);
            }
            
            Debug.Log("飞船坠毁！");
        }
        
        /// <summary>
        /// 抵达目的地处理
        /// </summary>
        private void ArriveAtDestination()
        {
            if (_currentState == ShipState.Arrived || !_canChangeState)
            {
                return;
            }
            
            ChangeState(ShipState.Arrived);
            
            // 触发抵达事件
            if (_eventManager != null)
            {
                _eventManager.TriggerEvent(GameEventType.OnShipArrived);
            }
            
            Debug.Log("飞船成功抵达目的地！");
        }
        
        #endregion

        #region 捕获交互系统
        
        /// <summary>
        /// 响应捕获事件（由EventManager调用）
        /// </summary>
        private void OnCapturedByPlanet(MonoBehaviour planetBehaviour)
        {
            // 检查是否应该响应这个捕获事件
            // 注意：这里假设捕获事件会传递给所有飞船，可能需要添加飞船ID检查
            
            // 只在飞行状态才能被捕获
            if (_currentState != ShipState.Flying)
            {
                return;
            }
            
            // 设置捕获的行星组件
            _capturedByPlanet = planetBehaviour;
            
            // 切换到捕获状态
            ChangeState(ShipState.Captured);
            
            if (planetBehaviour != null)
            {
                Debug.Log($"飞船被 {planetBehaviour.name} 捕获");
            }
        }
        
        /// <summary>
        /// 检查是否在行星的捕获范围内（供Planet脚本调用）
        /// </summary>
        public bool IsWithinCaptureRange(Vector3 planetPosition, float captureRadius)
        {
            Vector3 shipPos = _transform.position;
            // 投影到同一水平面（使用飞船的锁定Y坐标）
            Vector3 planetPosOnPlane = planetPosition;
            planetPosOnPlane.y = _lockedYPosition;
            shipPos.y = _lockedYPosition;
            
            float distance = Vector3.Distance(shipPos, planetPosOnPlane);
            return distance <= captureRadius;
        }
        
        /// <summary>
        /// 检查速度是否在捕获范围内（供Planet脚本调用）
        /// </summary>
        public bool HasValidCaptureVelocity(float minSpeed, float maxSpeed)
        {
            float speed = _velocity.magnitude;
            return speed >= minSpeed && speed <= maxSpeed;
        }
        
        #endregion

        #region 边界检测系统
        
        /// <summary>
        /// 检查是否超出游戏边界
        /// </summary>
        private void CheckBoundaries()
        {
            // 只在飞行状态检查边界
            if (_currentState != ShipState.Flying)
            {
                return;
            }
            
            Vector3 position = _transform.position;
            Vector3 offset = position - _boundaryCenter;
            
            // 检查是否超出边界（XZ平面）
            if (Mathf.Abs(offset.x) > _boundarySize || 
                Mathf.Abs(offset.z) > _boundarySize)
            {
                Escape();
            }
        }
        
        /// <summary>
        /// 逃逸处理
        /// </summary>
        private void Escape()
        {
            if (_currentState == ShipState.Escaped || !_canChangeState)
            {
                return;
            }
            
            ChangeState(ShipState.Escaped);
            
            // 触发逃逸事件
            if (_eventManager != null)
            {
                _eventManager.TriggerEvent(GameEventType.OnShipEscaped);
            }
            
            Debug.Log("飞船逃逸出游戏边界！");
        }
        
        #endregion

        #region Y坐标锁定系统
        
        /// <summary>
        /// 确保Y坐标锁定到水平面
        /// 在Update中调用，防止任何因素导致Y坐标偏移
        /// </summary>
        private void EnsureYPositionLocked()
        {
            // 检查当前Y坐标是否偏离锁定位置
            if (Mathf.Abs(_transform.position.y - _lockedYPosition) > 0.01f)
            {
                Vector3 correctedPos = _transform.position;
                correctedPos.y = _lockedYPosition;
                _transform.position = correctedPos;
                
                // 确保速度的Y分量为0
                _velocity.y = 0f;
            }
        }
        
        #endregion

        #region 调试和可视化
        
        /// <summary>
        /// 绘制调试信息（在Scene视图中）
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制速度向量
            if (Application.isPlaying && _velocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(_transform.position, _velocity.normalized * 5f);
            }
            
            // 绘制捕获轨道
            if (_currentState == ShipState.Captured && _capturedByPlanet != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = _capturedByPlanet is IGravitySource gs ? gs.Position : _capturedByPlanet.transform.position;
                center.y = _transform.position.y;
                
                // 绘制轨道圆（近似）
                float angleStep = 10f * Mathf.Deg2Rad;
                Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * _captureOrbitRadius, 0, Mathf.Sin(0) * _captureOrbitRadius);
                for (float angle = angleStep; angle <= 360f * Mathf.Deg2Rad; angle += angleStep)
                {
                    Vector3 point = center + new Vector3(Mathf.Cos(angle) * _captureOrbitRadius, 0, Mathf.Sin(angle) * _captureOrbitRadius);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }
        }
        
        #endregion
    }
}