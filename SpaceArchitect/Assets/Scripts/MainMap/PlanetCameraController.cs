using UnityEngine;

namespace SpaceArchitect.MainMap
{
    /// <summary>
    /// 行星视角相机控制：正交俯视，支持滚轮缩放。
    /// 由 MainMapController 在运镜结束后激活/关闭。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlanetCameraController : MonoBehaviour
    {
        [SerializeField] private Camera planetCamera;
        [SerializeField] private float defaultOrthoSize = 25f;
        [SerializeField] private float zoomSpeed = 40f;
        [SerializeField] private Vector2 orthoLimits = new Vector2(10f, 120f);

        private bool isActive;

        public Camera Camera => planetCamera;
        public bool IsActive => isActive;

        private void Awake()
        {
            if (planetCamera == null)
            {
                planetCamera = GetComponent<Camera>();
            }

            if (planetCamera != null)
            {
                planetCamera.orthographic = true;
                planetCamera.orthographicSize = defaultOrthoSize;
                planetCamera.enabled = false;
            }
        }

        /// <summary>
        /// 从主相机的位置/朝向切换到行星相机。
        /// </summary>
        public void ActivateFromCamera(Camera source)
        {
            if (planetCamera == null || source == null)
            {
                return;
            }

            transform.position = source.transform.position;
            transform.rotation = source.transform.rotation;

            planetCamera.orthographic = true;
            planetCamera.orthographicSize = defaultOrthoSize;
            planetCamera.enabled = true;
            isActive = true;
        }

        public void Deactivate()
        {
            if (planetCamera == null)
            {
                return;
            }

            planetCamera.enabled = false;
            isActive = false;
        }

        private void Update()
        {
            if (!isActive || planetCamera == null)
            {
                return;
            }

            HandleZoom();
        }

        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            var size = planetCamera.orthographicSize;
            size = Mathf.Clamp(size - scroll * zoomSpeed * Time.deltaTime, orthoLimits.x, orthoLimits.y);
            planetCamera.orthographicSize = size;
        }
    }
}


