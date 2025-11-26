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

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float zoomSpeed = 120f;
        [SerializeField] private Vector2 zoomLimits = new Vector2(10f, 120f);
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float panSpeed = 50f;

        [Header("Cinematic")]
        [SerializeField] private Transform launchStation;
        [SerializeField] private float cinematicHeight = 40f;
        [SerializeField] private float cinematicDuration = 2.5f;
        [SerializeField] private AnimationCurve cinematicCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private UIManager uiManager;
        private PlanetSelector currentSelection;
        private CameraState cameraState = CameraState.Free;
        private Vector3 focusPoint;
        private float yaw = 45f;
        private float pitch = 45f;
        private float distance = 80f;
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
                cameraPivot.position = mainCamera.transform.position;
                cameraPivot.rotation = mainCamera.transform.rotation;
            }

            uiManager = FindAnyObjectByType<UIManager>();
            focusPoint = cameraPivot != null ? cameraPivot.position : Vector3.zero;
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

            HandleZoom();
            HandleRotation();
            HandlePan();
            UpdateCameraTransform();
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
                BeginTravelCinematic(selector);
                return;
            }

            uiManager.ShowPlanetInfo(
                selector.Data,
                () => BeginTravelCinematic(selector),
                ClearSelection);
        }

        public void ClearSelection()
        {
            currentSelection = null;
            cameraState = CameraState.Free;
            inputLockedByUI = false;
        }

        private void BeginTravelCinematic(PlanetSelector selector)
        {
            if (selector == null || mainCamera == null)
            {
                return;
            }

            if (cinematicRoutine != null)
            {
                StopCoroutine(cinematicRoutine);
            }

            cinematicRoutine = StartCoroutine(CinematicRoutine(selector));
        }

        private IEnumerator CinematicRoutine(PlanetSelector selector)
        {
            cameraState = CameraState.Cinematic;
            uiManager?.HideAll();

            var startPosition = mainCamera.transform.position;
            var startRotation = mainCamera.transform.rotation;
            var travelTarget = selector.FocusPoint.position;
            var launchPosition = launchStation != null ? launchStation.position : Vector3.zero;
            var midpoint = Vector3.Lerp(launchPosition, travelTarget, 0.5f);
            var finalPosition = midpoint + Vector3.up * cinematicHeight;
            var finalRotation = Quaternion.LookRotation((midpoint - finalPosition).normalized, Vector3.up);

            var elapsed = 0f;
            while (elapsed < cinematicDuration)
            {
                var t = cinematicCurve.Evaluate(elapsed / cinematicDuration);
                mainCamera.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, finalRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.position = finalPosition;
            mainCamera.transform.rotation = finalRotation;

            inputLockedByUI = false;
            GameManager.Instance?.EnterGameplay(selector.Data);
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