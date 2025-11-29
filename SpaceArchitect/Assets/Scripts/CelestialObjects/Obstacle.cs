using UnityEngine;
using SpaceArchitect.Core;
using SpaceArchitect.Gameplay;

namespace SpaceArchitect.CelestialObjects
{
    /// <summary>
    /// 障碍物控制器
    /// 管理障碍物的碰撞体配置和碰撞检测
    /// 当飞船碰撞障碍物时，触发坠毁事件
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        #region 碰撞体配置参数
        
        [Header("碰撞体配置")]
        [Tooltip("碰撞体类型：使用触发器（Trigger）还是实心碰撞体（Collider）")]
        [SerializeField] private CollisionType _collisionType = CollisionType.Collider;
        
        [Tooltip("障碍物标签（用于识别障碍物，默认：Obstacle）")]
        [SerializeField] private string _obstacleTag = "Obstacle";
        
        [Tooltip("是否自动添加碰撞体组件（如果GameObject上还没有碰撞体）")]
        [SerializeField] private bool _autoAddCollider = true;
        
        [Tooltip("碰撞体形状类型（仅在自动添加时使用）")]
        [SerializeField] private ColliderShape _colliderShape = ColliderShape.Box;
        
        [Tooltip("球形碰撞体半径（仅在ColliderShape为Sphere时使用）")]
        [SerializeField] private float _sphereRadius = 1f;
        
        [Tooltip("盒形碰撞体尺寸（仅在ColliderShape为Box时使用）")]
        [SerializeField] private Vector3 _boxSize = Vector3.one;
        
        #endregion

        #region 碰撞检测参数
        
        [Header("碰撞检测")]
        [Tooltip("是否启用碰撞检测")]
        [SerializeField] private bool _enableCollisionDetection = true;
        
        [Tooltip("是否只检测飞船碰撞（true：只检测ShipController，false：检测所有碰撞）")]
        [SerializeField] private bool _onlyDetectShip = true;
        
        [Tooltip("碰撞后是否立即触发事件（true：立即触发，false：由ShipController处理）")]
        [SerializeField] private bool _triggerEventOnCollision = false;
        
        #endregion

        #region 组件引用
        
        private Collider _collider;
        private EventManager _eventManager;
        
        #endregion

        #region 枚举定义
        
        /// <summary>
        /// 碰撞体类型枚举
        /// </summary>
        public enum CollisionType
        {
            Collider,   // 实心碰撞体（需要飞船有Rigidbody）
            Trigger     // 触发器（不需要Rigidbody，但需要OnTriggerEnter回调）
        }
        
        /// <summary>
        /// 碰撞体形状枚举
        /// </summary>
        public enum ColliderShape
        {
            Box,        // 盒形碰撞体
            Sphere,     // 球形碰撞体
            Capsule     // 胶囊形碰撞体
        }
        
        #endregion

        #region Unity生命周期
        
        private void Awake()
        {
            // 获取EventManager实例
            _eventManager = EventManager.Instance;
            
            // 配置碰撞体
            SetupCollider();
            
            // 设置标签
            SetupTag();
        }
        
        private void Start()
        {
            // 验证碰撞体配置
            ValidateCollider();
        }
        
        #endregion

        #region 碰撞体配置
        
        /// <summary>
        /// 设置碰撞体
        /// </summary>
        private void SetupCollider()
        {
            // 获取现有的碰撞体组件
            _collider = GetComponent<Collider>();
            
            // 如果没有碰撞体且需要自动添加
            if (_collider == null && _autoAddCollider)
            {
                // 根据形状类型添加相应的碰撞体
                switch (_colliderShape)
                {
                    case ColliderShape.Box:
                        _collider = gameObject.AddComponent<BoxCollider>();
                        if (_collider is BoxCollider boxCollider)
                        {
                            boxCollider.size = _boxSize;
                        }
                        break;
                        
                    case ColliderShape.Sphere:
                        _collider = gameObject.AddComponent<SphereCollider>();
                        if (_collider is SphereCollider sphereCollider)
                        {
                            sphereCollider.radius = _sphereRadius;
                        }
                        break;
                        
                    case ColliderShape.Capsule:
                        _collider = gameObject.AddComponent<CapsuleCollider>();
                        break;
                }
                
                Debug.Log($"障碍物 {gameObject.name} 自动添加了 {_colliderShape} 碰撞体");
            }
            
            // 配置碰撞体属性
            if (_collider != null)
            {
                // 根据碰撞类型设置isTrigger
                _collider.isTrigger = (_collisionType == CollisionType.Trigger);
            }
        }
        
        /// <summary>
        /// 设置标签
        /// </summary>
        private void SetupTag()
        {
            // 尝试设置标签
            try
            {
                if (!string.IsNullOrEmpty(_obstacleTag))
                {
                    // 检查标签是否存在
                    if (!IsTagDefined(_obstacleTag))
                    {
                        Debug.LogWarning($"标签 '{_obstacleTag}' 不存在！请在Unity编辑器中创建该标签，或使用已存在的标签。");
                        return;
                    }
                    
                    gameObject.tag = _obstacleTag;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"设置障碍物标签失败：{e.Message}。请在Unity编辑器中手动设置标签为 '{_obstacleTag}'");
            }
        }
        
        /// <summary>
        /// 检查标签是否已定义
        /// </summary>
        private bool IsTagDefined(string tag)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 验证碰撞体配置
        /// </summary>
        private void ValidateCollider()
        {
            if (_collider == null)
            {
                Debug.LogError($"障碍物 {gameObject.name} 没有碰撞体组件！碰撞检测将无法工作。");
                return;
            }
            
            // 检查触发器设置是否正确
            bool shouldBeTrigger = (_collisionType == CollisionType.Trigger);
            if (_collider.isTrigger != shouldBeTrigger)
            {
                Debug.LogWarning($"障碍物 {gameObject.name} 的碰撞体触发器设置不正确，正在修正...");
                _collider.isTrigger = shouldBeTrigger;
            }
            
            Debug.Log($"障碍物 {gameObject.name} 碰撞体配置完成 - 类型：{_collisionType}，形状：{_colliderShape}，标签：{_obstacleTag}");
        }
        
        #endregion

        #region 碰撞检测（实心碰撞体）
        
        /// <summary>
        /// Unity碰撞检测回调（实心碰撞体）
        /// 当飞船使用Rigidbody与障碍物发生物理碰撞时触发
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // 只处理实心碰撞体模式
            if (_collisionType != CollisionType.Collider)
            {
                return;
            }
            
            // 检查是否启用碰撞检测
            if (!_enableCollisionDetection)
            {
                return;
            }
            
            // 处理碰撞
            HandleCollision(collision.gameObject);
        }
        
        /// <summary>
        /// Unity碰撞检测回调（碰撞持续中）
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            // 可以在这里处理持续碰撞的逻辑（如果需要）
        }
        
        /// <summary>
        /// Unity碰撞检测回调（碰撞结束）
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            // 可以在这里处理碰撞结束的逻辑（如果需要）
        }
        
        #endregion

        #region 触发器检测
        
        /// <summary>
        /// Unity触发器检测回调（触发器模式）
        /// 当飞船进入触发器范围时触发
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 只处理触发器模式
            if (_collisionType != CollisionType.Trigger)
            {
                return;
            }
            
            // 检查是否启用碰撞检测
            if (!_enableCollisionDetection)
            {
                return;
            }
            
            // 处理碰撞
            HandleCollision(other.gameObject);
        }
        
        /// <summary>
        /// Unity触发器检测回调（触发器内）
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            // 可以在这里处理持续触发的逻辑（如果需要）
        }
        
        /// <summary>
        /// Unity触发器检测回调（离开触发器）
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            // 可以在这里处理离开触发器的逻辑（如果需要）
        }
        
        #endregion

        #region 碰撞处理逻辑
        
        /// <summary>
        /// 处理碰撞逻辑
        /// </summary>
        /// <param name="collidingObject">碰撞的对象</param>
        private void HandleCollision(GameObject collidingObject)
        {
            // 如果只检测飞船，检查是否是飞船
            if (_onlyDetectShip)
            {
                var shipController = collidingObject.GetComponent<ShipController>();
                if (shipController == null)
                {
                    // 不是飞船，忽略
                    return;
                }
                
                // 检查飞船是否处于可碰撞状态（必须在飞行状态）
                if (shipController.CurrentState != ShipState.Flying)
                {
                    return;
                }
            }
            
            // 记录碰撞信息
            Debug.Log($"障碍物 {gameObject.name} 与 {collidingObject.name} 发生碰撞");
            
            // 根据设置决定是否触发事件
            if (_triggerEventOnCollision)
            {
                // 直接触发坠毁事件
                TriggerCrashEvent();
            }
            // 否则，让ShipController的OnCollisionEnter自己处理
            // ShipController会检查标签并调用Crash()方法
        }
        
        /// <summary>
        /// 触发坠毁事件
        /// </summary>
        private void TriggerCrashEvent()
        {
            if (_eventManager != null)
            {
                _eventManager.TriggerEvent(GameEventType.OnShipCrashed);
                Debug.Log($"障碍物 {gameObject.name} 触发坠毁事件");
            }
            else
            {
                Debug.LogWarning($"障碍物 {gameObject.name} 无法触发坠毁事件：EventManager未找到");
            }
        }
        
        #endregion

        #region 公共方法和属性
        
        /// <summary>
        /// 获取碰撞体组件
        /// </summary>
        public Collider Collider => _collider;
        
        /// <summary>
        /// 获取障碍物标签
        /// </summary>
        public string ObstacleTag => _obstacleTag;
        
        /// <summary>
        /// 设置碰撞检测启用状态
        /// </summary>
        public void SetCollisionDetectionEnabled(bool enabled)
        {
            _enableCollisionDetection = enabled;
        }
        
        /// <summary>
        /// 设置碰撞体为触发器或非触发器
        /// </summary>
        public void SetTriggerMode(bool isTrigger)
        {
            if (_collider != null)
            {
                _collider.isTrigger = isTrigger;
                _collisionType = isTrigger ? CollisionType.Trigger : CollisionType.Collider;
            }
        }
        
        /// <summary>
        /// 手动触发坠毁事件（用于测试或特殊场景）
        /// </summary>
        public void ManualTriggerCrash()
        {
            TriggerCrashEvent();
        }
        
        #endregion

        #region 调试和可视化
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制碰撞体范围
            Gizmos.color = Color.red;
            
            Collider col = _collider != null ? _collider : GetComponent<Collider>();
            
            if (col != null)
            {
                if (col is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (col is SphereCollider sphereCollider)
                {
                    Gizmos.DrawWireSphere(
                        transform.TransformPoint(sphereCollider.center),
                        sphereCollider.radius * transform.lossyScale.magnitude
                    );
                }
                else if (col is CapsuleCollider capsuleCollider)
                {
                    // 绘制胶囊形碰撞体的近似形状（使用两个球体和圆柱体）
                    Vector3 center = transform.TransformPoint(capsuleCollider.center);
                    float radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                    float height = capsuleCollider.height * transform.lossyScale.y;
                    
                    // 绘制上下两个半球
                    Gizmos.DrawWireSphere(center + Vector3.up * (height / 2 - radius), radius);
                    Gizmos.DrawWireSphere(center - Vector3.up * (height / 2 - radius), radius);
                }
                else
                {
                    // 其他类型的碰撞体，绘制包围盒
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }
            else
            {
                // 没有碰撞体，绘制默认的立方体
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }
        
        /// <summary>
        /// 选中时绘制详细信息
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 选中时绘制更明显的碰撞体
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            
            Collider col = _collider != null ? _collider : GetComponent<Collider>();
            
            if (col != null)
            {
                if (col is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                }
                else if (col is SphereCollider sphereCollider)
                {
                    Gizmos.DrawSphere(
                        transform.TransformPoint(sphereCollider.center),
                        sphereCollider.radius * transform.lossyScale.magnitude
                    );
                }
            }
        }
        
        #endregion

        #region 编辑器辅助方法
        
        /// <summary>
        /// 在编辑器中验证参数
        /// </summary>
        private void OnValidate()
        {
            // 确保参数合理
            _sphereRadius = Mathf.Max(0.1f, _sphereRadius);
            _boxSize = new Vector3(
                Mathf.Max(0.1f, _boxSize.x),
                Mathf.Max(0.1f, _boxSize.y),
                Mathf.Max(0.1f, _boxSize.z)
            );
            
            // 如果碰撞体已存在，更新触发器设置
            if (_collider != null)
            {
                bool shouldBeTrigger = (_collisionType == CollisionType.Trigger);
                if (Application.isPlaying && _collider.isTrigger != shouldBeTrigger)
                {
                    _collider.isTrigger = shouldBeTrigger;
                }
            }
        }
        
        #endregion
    }
}