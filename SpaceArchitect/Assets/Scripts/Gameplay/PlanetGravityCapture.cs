using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行星引力捕获脚本 - 放在行星上
/// 当飞船进入捕获范围时，轻微调整飞船速度使其自然进入轨道
/// </summary>
public class PlanetGravityCapture : MonoBehaviour
{
    [Header("全局设置")]
    [Tooltip("是否启用捕获系统（关闭后可用于测试轨迹预测准确性）")]
    public bool enableCapture = true;
    
    [Header("捕获范围设置")]
    [Tooltip("飞船进入此距离时开始捕获引导")]
    public float captureRadius = 15f;
    [Tooltip("飞船离开此距离时释放捕获")]
    public float releaseRadius = 30f;
    
    [Header("轨道引导强度")]
    [Tooltip("轨道调整的强度，值越小越自然（0.01-0.1推荐）")]
    [Range(0.001f, 0.5f)]
    public float orbitGuidanceStrength = 0.05f;
    [Tooltip("目标轨道半径（相对于行星中心）")]
    public float targetOrbitRadius = 12f;
    [Tooltip("是否强制逆时针旋转（如果为false，会根据飞船当前速度方向自动选择）")]
    public bool forceCounterClockwise = false;
    
    [Header("捕获检测")]
    [Tooltip("检测飞船的标签")]
    public string spaceshipTag = "Spaceship";
    [Tooltip("检测间隔（秒），降低性能消耗")]
    public float detectionInterval = 0.1f;
    
    private NBody planetNBody;
    private GravityEngine ge;
    private float lastDetectionTime;
    private readonly List<SpaceshipCaptureInfo> capturedSpaceships = new List<SpaceshipCaptureInfo>();
    
    private class SpaceshipCaptureInfo
    {
        public Transform spaceship;
        public NBody nbody;
        public float captureTime;
        
        public SpaceshipCaptureInfo(Transform ship, NBody nb)
        {
            spaceship = ship;
            nbody = nb;
            captureTime = Time.time;
        }
    }
    
    void Start()
    {
        planetNBody = GetComponent<NBody>();
        if (planetNBody == null)
        {
            Debug.LogWarning($"行星 {gameObject.name} 没有 NBody 组件，无法进行引力捕获");
        }
        
        ge = GravityEngine.Instance();
    }
    
    void Update()
    {
        // 如果捕获系统被禁用，直接返回
        if (!enableCapture)
        {
            return;
        }
        
        if (Time.time - lastDetectionTime < detectionInterval)
            return;
            
        lastDetectionTime = Time.time;
        
        // 检测附近的飞船
        DetectNearbySpaceships();
        
        // 对已捕获的飞船进行轨道引导
        GuideCapturedSpaceships();
    }
    
    void DetectNearbySpaceships()
    {
        // 查找所有飞船
        GameObject[] spaceships = GameObject.FindGameObjectsWithTag(spaceshipTag);
        
        foreach (GameObject ship in spaceships)
        {
            if (ship == null) continue;
            
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(ship.transform.position.x, ship.transform.position.y)
            );
            
            // 检查是否在捕获范围内
            if (distance <= captureRadius)
            {
                // 检查是否已经在捕获列表中
                bool alreadyCaptured = false;
                foreach (var info in capturedSpaceships)
                {
                    if (info.spaceship == ship.transform)
                    {
                        alreadyCaptured = true;
                        break;
                    }
                }
                
                if (!alreadyCaptured)
                {
                    NBody shipNBody = ship.GetComponent<NBody>();
                    if (shipNBody != null)
                    {
                        capturedSpaceships.Add(new SpaceshipCaptureInfo(ship.transform, shipNBody));
                        Debug.Log($"飞船 {ship.name} 进入 {gameObject.name} 的捕获范围");
                        
                        // 通过EventManager触发捕获事件
                        if (EventManager.Instance != null)
                        {
                            EventManager.Instance.TriggerPlanetCaptureStart(gameObject, ship);
                            EventManager.Instance.TriggerShipCaptured(gameObject, ship);
                        }
                    }
                }
            }
            else if (distance > releaseRadius)
            {
                // 移除捕获
                for (int i = capturedSpaceships.Count - 1; i >= 0; i--)
                {
                    if (capturedSpaceships[i].spaceship == ship.transform)
                    {
                        capturedSpaceships.RemoveAt(i);
                        Debug.Log($"飞船 {ship.name} 离开 {gameObject.name} 的捕获范围");
                        
                        // 通过EventManager触发释放事件
                        if (EventManager.Instance != null)
                        {
                            EventManager.Instance.TriggerPlanetCaptureEnd(gameObject, ship);
                            EventManager.Instance.TriggerShipReleased(gameObject, ship);
                        }
                        break;
                    }
                }
            }
        }
        
        // 清理无效的捕获信息（包括飞船被销毁、NBody失效或engineRef为null的情况）
        for (int i = capturedSpaceships.Count - 1; i >= 0; i--)
        {
            if (capturedSpaceships[i].spaceship == null || 
                capturedSpaceships[i].nbody == null || 
                capturedSpaceships[i].nbody.engineRef == null)
            {
                capturedSpaceships.RemoveAt(i);
            }
        }
    }
    
    void GuideCapturedSpaceships()
    {
        if (planetNBody == null || ge == null) return;
        
        float planetMass = GetPlanetMass();
        
        foreach (var info in capturedSpaceships)
        {
            // 检查飞船、NBody及其engineRef是否有效（飞船坠毁后engineRef会变为null）
            if (info.spaceship == null || info.nbody == null || info.nbody.engineRef == null) continue;
            
            // 如果NBody还未加入GravityEngine（例如飞船重置/坠毁后被移除），跳过
            if (info.nbody.engineRef == null) continue;
            
            Vector3 relativePos = info.spaceship.position - transform.position;
            float currentRadius = Mathf.Max(relativePos.magnitude, 0.1f);
            
            // 如果距离太远，释放捕获
            if (currentRadius > releaseRadius)
            {
                continue;
            }
            
            // 获取当前速度
            Vector3 currentVelocity = ge.GetVelocity(info.nbody);
            
            // 计算理想的轨道速度（圆形轨道）
            Vector3 radialDir = relativePos.normalized;
            Vector3 tangent = GetOrbitTangent(radialDir, currentVelocity);
            
            // 计算圆形轨道速度: v = √(GM/r)
            float idealOrbitSpeed = Mathf.Sqrt(planetMass / Mathf.Max(currentRadius, 0.1f));
            
            // 将当前速度分解为径向和切向分量
            float radialSpeed = Vector3.Dot(currentVelocity, radialDir);
            Vector3 radialVel = radialDir * radialSpeed;
            Vector3 tangentialVel = currentVelocity - radialVel;
            
            // 计算理想切向速度
            Vector3 idealTangentialVel = tangent * idealOrbitSpeed;
            
            // 轻微调整速度，使其逐渐接近理想轨道
            // 只调整切向速度，让径向速度自然衰减
            Vector3 adjustedTangentialVel = Vector3.Lerp(
                tangentialVel.normalized * Mathf.Max(tangentialVel.magnitude, 0.1f),
                idealTangentialVel,
                orbitGuidanceStrength
            );
            
            // 轻微衰减径向速度
            Vector3 adjustedRadialVel = Vector3.Lerp(radialVel, Vector3.zero, orbitGuidanceStrength * 0.5f);
            
            // 组合新速度
            Vector3 newVelocity = adjustedRadialVel + adjustedTangentialVel;
            
            // 应用速度调整（只在速度有效时）
            if (IsVelocityValid(newVelocity))
            {
                ge.SetVelocity(info.nbody, newVelocity);
            }
        }
    }
    
    Vector3 GetOrbitTangent(Vector3 radialDir, Vector3 currentVelocity)
    {
        // 计算两个可能的切向方向（顺时针和逆时针）
        Vector3 counterClockwise = Vector3.Cross(Vector3.forward, radialDir).normalized;
        Vector3 clockwise = -counterClockwise;
        
        // 如果强制逆时针，直接返回
        if (forceCounterClockwise)
        {
            return counterClockwise;
        }
        
        // 根据当前速度方向选择切向方向
        // 如果速度很小或为零，默认使用逆时针
        if (currentVelocity.magnitude < 0.1f)
        {
            return counterClockwise;
        }
        
        // 计算当前速度的切向分量方向
        float radialSpeed = Vector3.Dot(currentVelocity, radialDir);
        Vector3 currentTangential = (currentVelocity - radialDir * radialSpeed).normalized;
        
        // 如果切向速度太小，无法判断方向，使用逆时针
        if (currentTangential.magnitude < 0.1f)
        {
            return counterClockwise;
        }
        
        // 计算当前切向方向与两个可能方向的点积
        float dotCounterClockwise = Vector3.Dot(currentTangential, counterClockwise);
        float dotClockwise = Vector3.Dot(currentTangential, clockwise);
        
        // 选择与当前速度方向更接近的方向
        if (dotCounterClockwise > dotClockwise)
        {
            return counterClockwise;
        }
        else
        {
            return clockwise;
        }
    }
    
    float GetPlanetMass()
    {
        if (planetNBody != null)
        {
            return Mathf.Max(planetNBody.mass, 0.1f);
        }
        return Mathf.Max(transform.localScale.x, 0.1f) * 100f;
    }
    
    bool IsVelocityValid(Vector3 velocity)
    {
        return !float.IsNaN(velocity.x) && !float.IsNaN(velocity.y) &&
               !float.IsInfinity(velocity.x) && !float.IsInfinity(velocity.y) &&
               velocity.magnitude < 1000f;
    }
    
    void OnDrawGizmosSelected()
    {
        // 在编辑器中显示捕获范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, releaseRadius);
        
        if (targetOrbitRadius > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, targetOrbitRadius);
        }
    }
}

