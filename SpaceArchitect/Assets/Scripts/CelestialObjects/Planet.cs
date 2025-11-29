using UnityEngine;
using SpaceArchitect.Core;
using SpaceArchitect.Gameplay;

namespace SpaceArchitect.CelestialObjects
{
    /// <summary>
    /// 行星控制器
    /// 管理行星的引力场和资源系统
    /// 实现IGravitySource接口，提供引力计算
    /// 注意：捕获功能已分离到PlanetCaptureSystem组件
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

        #region 碰撞体参数
        
        [Header("碰撞体设置")]
        [Tooltip("碰撞体半径（用于碰撞检测）")]
        [SerializeField] private float _collisionRadius = 10f;
        
        #endregion

        #region 捕获系统引用
        
        [Header("捕获系统（可选）")]
        [Tooltip("捕获系统组件（如果存在，用于处理飞船捕获功能）")]
        private PlanetCaptureSystem _captureSystem;
        
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
        
        #endregion

        #region Unity生命周期
        
        private void Awake()
        {
            // 获取组件引用
            _transform = transform;
            _collider = GetComponent<SphereCollider>();
            
            // 设置碰撞体
            SetupColliders();
            
            // 查找捕获系统组件（可能在子对象上）
            _captureSystem = GetComponentInChildren<PlanetCaptureSystem>();
            if (_captureSystem != null)
            {
                // 设置行星所有者，让捕获系统知道位置提供者
                _captureSystem.SetPlanetOwner(this);
            }
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


        #region 公共属性和方法
        
        /// <summary>
        /// 获取行星质量
        /// </summary>
        public float Mass => _mass;
        
        /// <summary>
        /// 获取碰撞范围半径
        /// </summary>
        public float CollisionRadius => _collisionRadius;
        
        /// <summary>
        /// 获取捕获范围半径（从捕获系统获取）
        /// </summary>
        public float CaptureRadius => _captureSystem != null ? _captureSystem.CaptureRadius : 0f;
        
        /// <summary>
        /// 获取当前是否捕获了飞船（从捕获系统获取）
        /// </summary>
        public bool HasCapturedShip => _captureSystem != null && _captureSystem.HasCapturedShip;
        
        /// <summary>
        /// 获取当前捕获的飞船（从捕获系统获取）
        /// </summary>
        public ShipController CapturedShip => _captureSystem != null ? _captureSystem.CapturedShip : null;
        
        /// <summary>
        /// 获取捕获系统组件
        /// </summary>
        public PlanetCaptureSystem CaptureSystem => _captureSystem;
        
        /// <summary>
        /// 设置捕获参数（运行时修改，转发到捕获系统）
        /// </summary>
        public void SetCaptureParameters(float radius, float minSpeed, float maxSpeed, float angleThreshold)
        {
            if (_captureSystem != null)
            {
                _captureSystem.SetCaptureParameters(radius, minSpeed, maxSpeed, angleThreshold);
            }
            else
            {
                Debug.LogWarning($"行星 {gameObject.name} 没有捕获系统组件，无法设置捕获参数");
            }
        }
        
        /// <summary>
        /// 释放捕获的飞船（转发到捕获系统）
        /// </summary>
        public void ReleaseShip()
        {
            if (_captureSystem != null)
            {
                _captureSystem.ReleaseShip();
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
            
            // 绘制碰撞范围
            Gizmos.color = Color.red;
            float collisionRadius = _collider != null ? _collider.radius : _collisionRadius;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
            
            // 注意：捕获范围由PlanetCaptureSystem组件绘制
        }
        
        /// <summary>
        /// 选中时绘制详细信息
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制更明显的引力影响范围
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _maxGravityDistance);
            
            // 注意：捕获范围由PlanetCaptureSystem组件绘制
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
            
            // 更新碰撞体半径（如果存在）
            if (_collider != null)
            {
                _collider.radius = _collisionRadius;
            }
        }
        
        #endregion
    }
}