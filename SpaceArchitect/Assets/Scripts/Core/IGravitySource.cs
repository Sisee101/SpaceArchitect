using UnityEngine;

namespace SpaceArchitect.Core
{
    /// <summary>
    /// 引力源接口
    /// 所有能够产生引力的对象（行星、引力装置、飞船等）都应实现此接口
    /// 实现低耦合的引力系统，允许ShipController通过接口获取引力，而不直接依赖具体类
    /// </summary>
    public interface IGravitySource
    {
        /// <summary>
        /// 计算给定位置受到的引力向量（世界坐标）
        /// </summary>
        /// <param name="position">计算引力的位置（世界坐标）</param>
        /// <returns>引力向量（世界坐标，单位：m/s²，表示加速度）</returns>
        /// <example>
        /// // 实现示例：
        /// public Vector3 GetGravityForce(Vector3 position)
        /// {
        ///     Vector3 direction = (Position - position).normalized;
        ///     float distance = Vector3.Distance(Position, position);
        ///     float force = (GravityConstant * Mass) / (distance * distance);
        ///     return direction * force;
        /// }
        /// </example>
        Vector3 GetGravityForce(Vector3 position);
        
        /// <summary>
        /// 获取引力源的位置（世界坐标）
        /// </summary>
        Vector3 Position { get; }
    }
}
