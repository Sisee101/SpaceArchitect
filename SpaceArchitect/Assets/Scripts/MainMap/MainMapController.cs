using System.Collections;
using UnityEngine;
using SpaceArchitect.Core;
using SpaceArchitect.UI;

namespace SpaceArchitect.MainMap
{
    public class MainMapController : MonoBehaviour
    {
        private enum CameraState
        {
            Free,
            Cinematic
        }

        private enum CameraMode
        {
            Map,        // 大地图模式：透视，相机可旋转/缩放/平移
            Planet      // 行星模式：俯视正交，只围绕单个行星
        }

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float zoomSpeed = 120f;
        [SerializeField] private Vector2 zoomLimits = new Vector2(10f, 120f);
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float panSpeed = 50f;

        [Header("Planet View")]
        [SerializeField] private float planetViewHeight = 40f;
        [SerializeField] private float transitionDuration = 1.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private PlanetCameraController planetCameraController;

        private UIManager uiManager;
        private PlanetSelector currentSelection;
        private CameraState cameraState = CameraState.Free;
        private CameraMode cameraMode = CameraMode.Map;
        private Vector3 focusPoint = new Vector3(15f, 0f, 7.5f);  // 地图中心点（根据行星分布计算）
        private float yaw = 45f;      // 水平旋转：45度斜视角，能看到左右两侧
        private float pitch = 50f;    // 垂直旋转：50度俯视，保持透视感
        private float distance = 160f; // 距离：足够远能看到所有行星
        private Coroutine cinematicRoutine;
        private bool inputLockedByUI;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (cameraPivot == null && mainCamera != null)
            {
                cameraPivot = new GameObject("CameraPivot").transform;
                // 使用预设的 focusPoint 作为 pivot 位置，而不是相机当前位置
                cameraPivot.position = focusPoint;
                cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else if (cameraPivot != null)
            {
                // 如果 pivot 已存在，使用它的位置作为 focusPoint
                focusPoint = cameraPivot.position;
            }

            uiManager = FindAnyObjectByType<UIManager>();

            // 如果 planetCameraController 引用丢失，尝试自动查找
            if (planetCameraController == null)
            {
                var foundPlanetCam = FindAnyObjectByType<PlanetCameraController>();
                if (foundPlanetCam != null)
                {
                    planetCameraController = foundPlanetCam;
                }
            }

            cameraMode = CameraMode.Map;
        }

        private void OnEnable()
        {
            UpdateCameraTransform();
        }

        private void Update()
        {
            if (mainCamera == null || cameraState != CameraState.Free || inputLockedByUI)
            {
                return;
            }
            
            // 仅在大地图模式下响应输入；行星视角由 PlanetCameraController 控制
            if (cameraMode == CameraMode.Map)
            {
                HandleZoom();
                HandleRotation();
                HandlePan();
                UpdateCameraTransform();
            }
        }

        public void HandlePlanetClicked(PlanetSelector selector)
        {
            if (selector == null || cameraState == CameraState.Cinematic)
            {
                return;
            }

            currentSelection = selector;
            inputLockedByUI = true;

            if (uiManager == null)
            {
                BeginTravelToPlanetView(selector);
                return;
            }

            uiManager.ShowPlanetInfo(
                selector.Data,
                () => BeginTravelToPlanetView(selector),
                ClearSelection);
        }

        public void ClearSelection()
        {
            currentSelection = null;
            inputLockedByUI = false;
            cameraState = CameraState.Free;
            cameraMode = CameraMode.Map;
        }

        /// <summary>
        /// 从大地图视角切换到某颗行星的俯视正交视角。
        /// </summary>
        private void BeginTravelToPlanetView(PlanetSelector selector)
        {
            if (selector == null || mainCamera == null)
            {
                return;
            }

            if (cinematicRoutine != null)
            {
                StopCoroutine(cinematicRoutine);
            }

            cinematicRoutine = StartCoroutine(TravelToPlanetRoutine(selector));
        }

        /// <summary>
        /// 协程：插值从大地图视角过渡到行星俯视视角。
        /// </summary>
        private IEnumerator TravelToPlanetRoutine(PlanetSelector selector)
        {
            cameraState = CameraState.Cinematic;
            uiManager?.HideAll();

            var startPosition = mainCamera.transform.position;
            var startRotation = mainCamera.transform.rotation;
            var planetPosition = selector.FocusPoint.position;
            // 最终位置：在行星正上方一定高度
            var finalPosition = planetPosition + Vector3.up * planetViewHeight;
            // 最终朝向：完全俯视
            var finalRotation = Quaternion.Euler(90f, 0f, 0f);

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                var t = transitionCurve.Evaluate(elapsed / transitionDuration);
                mainCamera.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, finalRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.position = finalPosition;
            mainCamera.transform.rotation = finalRotation;

            // 切换到行星相机视角
            if (planetCameraController != null && mainCamera != null)
            {
                planetCameraController.ActivateFromCamera(mainCamera);
                mainCamera.enabled = false;
                cameraMode = CameraMode.Planet;
            }

            cameraState = CameraState.Free;
            inputLockedByUI = false;
            currentSelection = selector;
        }

        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            distance = Mathf.Clamp(distance - scroll * zoomSpeed * Time.deltaTime, zoomLimits.x, zoomLimits.y);
        }

        private void HandleRotation()
        {
            if (!Input.GetMouseButton(1))
            {
                return;
            }

            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");
            yaw += mouseX * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - mouseY * rotationSpeed * Time.deltaTime, 10f, 80f);
        }

        private void HandlePan()
        {
            if (!Input.GetMouseButton(2))
            {
                return;
            }

            var delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            var right = mainCamera.transform.right;
            var forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;

            focusPoint -= right * delta.x * panSpeed * Time.deltaTime;
            focusPoint -= forward * delta.y * panSpeed * Time.deltaTime;
        }

        private void UpdateCameraTransform()
        {
            if (cameraPivot == null || mainCamera == null)
            {
                return;
            }

            cameraPivot.position = focusPoint;
            cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
            mainCamera.transform.position = cameraPivot.position - cameraPivot.forward * distance;
            mainCamera.transform.rotation = cameraPivot.rotation;
        }
    }
}
