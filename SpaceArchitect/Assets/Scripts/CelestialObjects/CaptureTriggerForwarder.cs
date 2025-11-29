using UnityEngine;

namespace SpaceArchitect.CelestialObjects
{
    /// <summary>
    /// 捕获触发器转发器
    /// 用于将触发器事件从子对象转发到父对象的PlanetCaptureSystem
    /// 解决OnTriggerEnter需要在有Collider的GameObject上才能被调用的问题
    /// </summary>
    public class CaptureTriggerForwarder : MonoBehaviour
    {
        private PlanetCaptureSystem _captureSystem;
        
        /// <summary>
        /// 设置捕获系统引用
        /// </summary>
        public void SetCaptureSystem(PlanetCaptureSystem captureSystem)
        {
            _captureSystem = captureSystem;
        }
        
        /// <summary>
        /// Unity触发器进入回调
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_captureSystem != null)
            {
                _captureSystem.HandleTriggerEnter(other);
            }
        }
        
        /// <summary>
        /// Unity触发器退出回调
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (_captureSystem != null)
            {
                _captureSystem.HandleTriggerExit(other);
            }
        }
    }
}

