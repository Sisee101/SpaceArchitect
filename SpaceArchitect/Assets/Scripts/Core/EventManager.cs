using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceArchitect.Core
{
    /// <summary>
    /// 游戏事件类型枚举
    /// 定义游戏中所有可能发生的事件类型
    /// </summary>
    public enum GameEventType
    {
        // 飞船相关事件
        OnShipLaunch,       // 飞船发射事件
        OnShipCaptured,     // 飞船被捕获事件
        OnShipCrashed,      // 飞船坠毁事件
        OnShipEscaped,      // 飞船逃逸事件
        OnShipArrived,      // 飞船抵达目的地事件
        
        // 游戏流程事件
        OnGameSuccess,      // 游戏成功事件
        OnGameFailed,       // 游戏失败事件
        
        // 可以在这里继续添加更多事件类型
    }

    /// <summary>
    /// 事件管理器 - 单例模式
    /// 负责游戏内所有事件的订阅、触发和管理
    /// 实现低耦合的事件通信机制
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        #region 单例模式实现
        
        /// <summary>
        /// 私有静态实例，确保全局唯一
        /// </summary>
        private static EventManager _instance;
        
        /// <summary>
        /// 公共静态属性，提供全局访问点
        /// 使用懒加载模式，首次访问时创建实例
        /// </summary>
        public static EventManager Instance
        {
            get
            {
                // 如果实例不存在，尝试在场景中查找
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventManager>();
                    
                    // 如果场景中也没有，创建一个新的GameObject并挂载EventManager
                    if (_instance == null)
                    {
                        GameObject eventManagerObject = new GameObject("EventManager");
                        _instance = eventManagerObject.AddComponent<EventManager>();
                        
                        // 设置为DontDestroyOnLoad，确保场景切换时不被销毁
                        DontDestroyOnLoad(eventManagerObject);
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Awake中确保单例的唯一性
        /// 如果场景中已经有其他实例，销毁当前实例
        /// </summary>
        private void Awake()
        {
            // 如果实例已存在且不是当前对象，销毁当前对象
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // 设置当前对象为实例
            _instance = this;
            
            // 设置为DontDestroyOnLoad，确保场景切换时不被销毁
            DontDestroyOnLoad(gameObject);
            
            // 初始化事件字典
            InitializeEventDictionaries();
        }
        
        /// <summary>
        /// 对象销毁时清理单例引用
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region 事件存储结构（支持泛型参数）
        
        /// <summary>
        /// 存储无参数事件的字典
        /// Key: 事件类型
        /// Value: 事件回调列表（Action无参数委托）
        /// </summary>
        private Dictionary<GameEventType, Action> _eventActions;
        
        /// <summary>
        /// 存储带参数事件的字典（泛型字典的字典）
        /// 外层Key: 事件类型
        /// 内层: 存储对应类型的泛型Action委托列表
        /// </summary>
        private Dictionary<GameEventType, object> _genericEventActions;
        
        /// <summary>
        /// 初始化事件字典
        /// </summary>
        private void InitializeEventDictionaries()
        {
            _eventActions = new Dictionary<GameEventType, Action>();
            _genericEventActions = new Dictionary<GameEventType, object>();
        }
        
        #endregion

        #region 无参数事件：订阅接口
        
        /// <summary>
        /// 订阅无参数事件
        /// </summary>
        /// <param name="eventType">要订阅的事件类型</param>
        /// <param name="callback">事件触发时的回调函数</param>
        /// <example>
        /// // 使用示例：
        /// EventManager.Instance.Subscribe(GameEventType.OnShipCrashed, OnShipCrashedHandler);
        /// 
        /// private void OnShipCrashedHandler()
        /// {
        ///     Debug.Log("飞船坠毁了！");
        /// }
        /// </example>
        public void Subscribe(GameEventType eventType, Action callback)
        {
            // 如果字典中没有该事件类型，创建一个新的列表
            if (!_eventActions.ContainsKey(eventType))
            {
                _eventActions[eventType] = null;
            }
            
            // 使用 += 运算符添加回调（实际上是合并委托）
            _eventActions[eventType] += callback;
        }
        
        /// <summary>
        /// 取消订阅无参数事件
        /// </summary>
        /// <param name="eventType">要取消订阅的事件类型</param>
        /// <param name="callback">要移除的回调函数</param>
        /// <example>
        /// // 使用示例：
        /// EventManager.Instance.Unsubscribe(GameEventType.OnShipCrashed, OnShipCrashedHandler);
        /// </example>
        public void Unsubscribe(GameEventType eventType, Action callback)
        {
            // 检查事件类型是否存在
            if (_eventActions.ContainsKey(eventType) && _eventActions[eventType] != null)
            {
                // 使用 -= 运算符移除回调
                _eventActions[eventType] -= callback;
            }
        }
        
        #endregion

        #region 泛型参数事件：订阅接口
        
        /// <summary>
        /// 订阅带参数的事件（泛型方法）
        /// </summary>
        /// <typeparam name="T">事件参数的类型</typeparam>
        /// <param name="eventType">要订阅的事件类型</param>
        /// <param name="callback">事件触发时的回调函数，接受T类型参数</param>
        /// <example>
        /// // 使用示例：订阅飞船发射事件，参数为Vector3（初始速度）
        /// EventManager.Instance.Subscribe&lt;Vector3&gt;(GameEventType.OnShipLaunch, OnShipLaunched);
        /// 
        /// private void OnShipLaunched(Vector3 initialVelocity)
        /// {
        ///     Debug.Log($"飞船发射了！初始速度：{initialVelocity}");
        /// }
        /// 
        /// // 使用示例：订阅捕获事件，参数为Planet引用
        /// EventManager.Instance.Subscribe&lt;Planet&gt;(GameEventType.OnShipCaptured, OnShipCaptured);
        /// 
        /// private void OnShipCaptured(Planet capturedBy)
        /// {
        ///     Debug.Log($"飞船被{capturedBy.name}捕获了！");
        /// }
        /// </example>
        public void Subscribe<T>(GameEventType eventType, Action<T> callback)
        {
            // 如果字典中没有该事件类型，创建一个新的泛型字典
            if (!_genericEventActions.ContainsKey(eventType))
            {
                _genericEventActions[eventType] = new Dictionary<Type, object>();
            }
            
            // 获取该事件类型的类型字典
            var typeDict = _genericEventActions[eventType] as Dictionary<Type, object>;
            
            // 如果该类型还没有Action列表，创建一个
            if (!typeDict.ContainsKey(typeof(T)))
            {
                typeDict[typeof(T)] = new List<Action<T>>();
            }
            
            // 将回调添加到列表中
            var callbacks = typeDict[typeof(T)] as List<Action<T>>;
            callbacks.Add(callback);
        }
        
        /// <summary>
        /// 取消订阅带参数的事件（泛型方法）
        /// </summary>
        /// <typeparam name="T">事件参数的类型</typeparam>
        /// <param name="eventType">要取消订阅的事件类型</param>
        /// <param name="callback">要移除的回调函数</param>
        /// <example>
        /// // 使用示例：
        /// EventManager.Instance.Unsubscribe&lt;Vector3&gt;(GameEventType.OnShipLaunch, OnShipLaunched);
        /// </example>
        public void Unsubscribe<T>(GameEventType eventType, Action<T> callback)
        {
            // 检查事件类型是否存在
            if (_genericEventActions.ContainsKey(eventType))
            {
                var typeDict = _genericEventActions[eventType] as Dictionary<Type, object>;
                
                // 检查该类型的事件列表是否存在
                if (typeDict != null && typeDict.ContainsKey(typeof(T)))
                {
                    var callbacks = typeDict[typeof(T)] as List<Action<T>>;
                    if (callbacks != null)
                    {
                        callbacks.Remove(callback);
                    }
                }
            }
        }
        
        #endregion

        #region 无参数事件：触发接口
        
        /// <summary>
        /// 触发无参数事件
        /// </summary>
        /// <param name="eventType">要触发的事件类型</param>
        /// <example>
        /// // 使用示例：触发飞船坠毁事件
        /// EventManager.Instance.TriggerEvent(GameEventType.OnShipCrashed);
        /// </example>
        public void TriggerEvent(GameEventType eventType)
        {
            // 检查事件类型是否存在且有订阅者
            if (_eventActions.ContainsKey(eventType) && _eventActions[eventType] != null)
            {
                // 调用所有订阅的回调函数
                _eventActions[eventType].Invoke();
            }
        }
        
        #endregion

        #region 泛型参数事件：触发接口
        
        /// <summary>
        /// 触发带参数的事件（泛型方法）
        /// </summary>
        /// <typeparam name="T">事件参数的类型</typeparam>
        /// <param name="eventType">要触发的事件类型</param>
        /// <param name="eventData">事件携带的数据</param>
        /// <example>
        /// // 使用示例：触发飞船发射事件，传递初始速度
        /// Vector3 initialVelocity = new Vector3(10f, 0f, 5f);
        /// EventManager.Instance.TriggerEvent(GameEventType.OnShipLaunch, initialVelocity);
        /// 
        /// // 使用示例：触发捕获事件，传递行星引用
        /// EventManager.Instance.TriggerEvent(GameEventType.OnShipCaptured, capturedPlanet);
        /// </example>
        public void TriggerEvent<T>(GameEventType eventType, T eventData)
        {
            // 检查事件类型是否存在
            if (_genericEventActions.ContainsKey(eventType))
            {
                var typeDict = _genericEventActions[eventType] as Dictionary<Type, object>;
                
                // 检查该类型的事件列表是否存在
                if (typeDict != null && typeDict.ContainsKey(typeof(T)))
                {
                    var callbacks = typeDict[typeof(T)] as List<Action<T>>;
                    if (callbacks != null)
                    {
                        // 遍历所有订阅的回调并调用
                        foreach (var callback in callbacks)
                        {
                            callback.Invoke(eventData);
                        }
                    }
                }
            }
        }
        
        #endregion

        #region 辅助方法：清除事件（用于调试和清理）
        
        /// <summary>
        /// 清除指定事件类型的所有订阅
        /// 主要用于调试或场景切换时的清理
        /// </summary>
        /// <param name="eventType">要清除的事件类型</param>
        public void ClearEvent(GameEventType eventType)
        {
            if (_eventActions.ContainsKey(eventType))
            {
                _eventActions[eventType] = null;
            }
            
            if (_genericEventActions.ContainsKey(eventType))
            {
                _genericEventActions.Remove(eventType);
            }
        }
        
        /// <summary>
        /// 清除所有事件的订阅
        /// 主要用于场景切换时的完全清理
        /// </summary>
        public void ClearAllEvents()
        {
            _eventActions.Clear();
            _genericEventActions.Clear();
        }
        
        /// <summary>
        /// 获取指定事件类型的订阅者数量（用于调试）
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount(GameEventType eventType)
        {
            int count = 0;
            
            // 统计无参数事件订阅者
            if (_eventActions.ContainsKey(eventType) && _eventActions[eventType] != null)
            {
                count += _eventActions[eventType].GetInvocationList().Length;
            }
            
            // 统计泛型事件订阅者
            if (_genericEventActions.ContainsKey(eventType))
            {
                var typeDict = _genericEventActions[eventType] as Dictionary<Type, object>;
                if (typeDict != null)
                {
                    foreach (var kvp in typeDict)
                    {
                        if (kvp.Value is System.Collections.ICollection collection)
                        {
                            count += collection.Count;
                        }
                    }
                }
            }
            
            return count;
        }
        
        #endregion
    }
}