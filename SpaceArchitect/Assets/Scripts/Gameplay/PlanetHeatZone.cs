using System.Collections;
using UnityEngine;

/// <summary>
/// 行星加热区域脚本 - 放在有Fire Tag的行星上
/// 基于PlanetGravityCapture，检测飞船进入加热范围
/// 如果飞船没有隔热技能，触发过热失败事件
/// </summary>
public class PlanetHeatZone : MonoBehaviour
{
    [Header("加热范围设置")]
    [Tooltip("飞船进入此距离时开始加热检测")]
    [SerializeField] private float heatRadius = 15f;
    
    [Tooltip("飞船离开此距离时停止加热检测")]
    [SerializeField] private float releaseRadius = 30f;
    
    [Header("过热设置")]
    [Tooltip("进入加热范围后，多少秒后过热（如果没有隔热技能）")]
    [SerializeField] private float overheatTime = 3f;
    
    [Tooltip("是否启用加热检测")]
    [SerializeField] private bool enableHeatDetection = true;
    
    [Header("检测设置")]
    [Tooltip("检测飞船的标签")]
    [SerializeField] private string spaceshipTag = "Spaceship";
    
    [Tooltip("检测间隔（秒），降低性能消耗")]
    [SerializeField] private float detectionInterval = 0.1f;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField] private bool showDebugLog = true;
    
    private float lastDetectionTime;
    private readonly System.Collections.Generic.Dictionary<GameObject, Coroutine> heatingShips = new System.Collections.Generic.Dictionary<GameObject, Coroutine>();
    
    void Start()
    {
        // 确保行星有Fire Tag
        if (!gameObject.CompareTag("Fire"))
        {
            Debug.LogWarning($"PlanetHeatZone: {gameObject.name} 没有Fire Tag，请添加Fire Tag以启用加热功能");
        }
    }
    
    void Update()
    {
        // 如果加热检测被禁用，直接返回
        if (!enableHeatDetection)
        {
            return;
        }
        
        if (Time.time - lastDetectionTime < detectionInterval)
            return;
            
        lastDetectionTime = Time.time;
        
        // 检测附近的飞船
        DetectNearbySpaceships();
    }
    
    /// <summary>
    /// 检测附近的飞船
    /// </summary>
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
            
            // 检查是否在加热范围内
            if (distance <= heatRadius)
            {
                // 检查隔热技能状态
                ShipHeatShieldSkill heatShield = ship.GetComponent<ShipHeatShieldSkill>();
                bool hasHeatShield = heatShield != null && heatShield.IsHeatShieldActive;
                
                if (!heatingShips.ContainsKey(ship))
                {
                    // 如果飞船还没有开始加热，开始加热过程
                    if (!hasHeatShield)
                    {
                        // 飞船没有隔热技能，立即触发开始过热事件（开始变红效果）
                        if (EventManager.Instance != null)
                        {
                            if (showDebugLog)
                            {
                                Debug.Log($"PlanetHeatZone: 正在触发开始过热事件 - 火行星: {gameObject.name}, 飞船: {ship.name}, 飞船GameObject: {ship.GetInstanceID()}");
                            }
                            EventManager.Instance.TriggerShipStartOverheating(gameObject, ship);
                            if (showDebugLog)
                            {
                                Debug.Log($"PlanetHeatZone: 开始过热事件已触发");
                            }
                        }
                        else if (showDebugLog)
                        {
                            Debug.LogWarning($"PlanetHeatZone: EventManager 实例为空，无法触发开始过热事件！");
                        }
                        
                        // 开始过热计时（3秒后爆炸）
                        Coroutine overheatCoroutine = StartCoroutine(OverheatShipCoroutine(ship));
                        heatingShips[ship] = overheatCoroutine;
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 进入加热范围，立即开始变红效果，{overheatTime}秒后爆炸");
                        }
                    }
                    else
                    {
                        // 飞船有隔热技能，可以安全进入
                        if (showDebugLog)
                        {
                            Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 进入加热范围，但有隔热技能保护");
                        }
                    }
                }
                else
                {
                    // 飞船已经在加热列表中，持续检测隔热技能状态
                    // 如果飞船激活了隔热技能，取消过热（停止变红效果）
                    if (hasHeatShield && heatingShips[ship] != null)
                    {
                        StopCoroutine(heatingShips[ship]);
                        heatingShips.Remove(ship);
                        
                        // 触发取消过热事件（让飞船停止变红效果）
                        // 注意：ShipOverheat需要监听这个事件来停止变红
                        // 可以通过检测隔热技能状态来实现，或者添加一个新事件
                        // 暂时通过直接访问ShipOverheat组件来处理
                        ShipOverheat shipOverheat = ship.GetComponent<ShipOverheat>();
                        if (shipOverheat != null)
                        {
                            shipOverheat.CancelOverheating();
                        }
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 在加热范围内激活了隔热技能，取消过热，停止变红效果");
                        }
                    }
                }
            }
            else if (distance > releaseRadius)
            {
                // 飞船离开加热范围，停止过热计时和变红效果
                if (heatingShips.ContainsKey(ship))
                {
                    if (heatingShips[ship] != null)
                    {
                        StopCoroutine(heatingShips[ship]);
                    }
                    heatingShips.Remove(ship);
                    
                    // 停止变红效果（取消过热）
                    ShipOverheat shipOverheat = ship.GetComponent<ShipOverheat>();
                    if (shipOverheat != null)
                    {
                        shipOverheat.CancelOverheating();
                    }
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 离开加热范围，停止过热计时和变红效果");
                    }
                }
            }
            else
            {
                // 在释放半径内但在加热半径外，继续检测隔热技能状态
                // 如果飞船之前没有隔热技能但现在激活了，取消过热
                if (heatingShips.ContainsKey(ship) && heatingShips[ship] != null)
                {
                    ShipHeatShieldSkill heatShield = ship.GetComponent<ShipHeatShieldSkill>();
                    if (heatShield != null && heatShield.IsHeatShieldActive)
                    {
                        // 飞船激活了隔热技能，取消过热
                        StopCoroutine(heatingShips[ship]);
                        heatingShips.Remove(ship);
                        
                        if (showDebugLog)
                        {
                            Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 激活了隔热技能，取消过热");
                        }
                    }
                }
            }
        }
        
        // 清理无效的飞船引用
        var keysToRemove = new System.Collections.Generic.List<GameObject>();
        foreach (var kvp in heatingShips)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            if (heatingShips[key] != null)
            {
                StopCoroutine(heatingShips[key]);
            }
            heatingShips.Remove(key);
        }
    }
    
    /// <summary>
    /// 飞船过热协程
    /// 等待3秒后触发爆炸（变红效果在进入范围时立即触发）
    /// </summary>
    private IEnumerator OverheatShipCoroutine(GameObject ship)
    {
        // 等待过热时间（3秒，这段时间内飞船正在变红）
        yield return new WaitForSecondsRealtime(overheatTime);
        
        // 检查飞船是否仍然存在且在加热范围内
        if (ship == null)
        {
            heatingShips.Remove(ship);
            yield break;
        }
        
        // 再次检查距离
        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(ship.transform.position.x, ship.transform.position.y)
        );
        
        if (distance > releaseRadius)
        {
            // 飞船已经离开，不触发爆炸（但可能还在变红，需要停止）
            ShipOverheat shipOverheat = ship.GetComponent<ShipOverheat>();
            if (shipOverheat != null)
            {
                shipOverheat.CancelOverheating();
            }
            heatingShips.Remove(ship);
            yield break;
        }
        
        // 再次检查隔热技能（可能在这段时间内激活了）
        ShipHeatShieldSkill heatShield = ship.GetComponent<ShipHeatShieldSkill>();
        if (heatShield != null && heatShield.IsHeatShieldActive)
        {
            // 飞船有隔热技能，取消过热（停止变红效果）
            ShipOverheat shipOverheat = ship.GetComponent<ShipOverheat>();
            if (shipOverheat != null)
            {
                shipOverheat.CancelOverheating();
            }
            heatingShips.Remove(ship);
            
            if (showDebugLog)
            {
                Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 在过热前激活了隔热技能，取消过热，停止变红");
            }
            yield break;
        }
        
        // 3秒后，飞船过热完成，触发爆炸事件
        if (showDebugLog)
        {
            Debug.Log($"PlanetHeatZone: 飞船 {ship.name} 过热完成，触发爆炸！");
        }
        
        // 通过EventManager触发过热失败事件（这会触发爆炸特效和模型消失）
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerShipOverheated(gameObject, ship);
        }
        
        // 从加热列表中移除
        heatingShips.Remove(ship);
    }
    
    void OnDrawGizmosSelected()
    {
        // 在编辑器中显示加热范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, heatRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, releaseRadius);
    }
}

