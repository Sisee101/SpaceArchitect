using UnityEngine;
using System.Collections.Generic;

public class SpaceshipLauncher1 : MonoBehaviour
{
    [Header("Prefabs and References")]
    public GameObject arrowPrefab;
    public GameObject trajectoryPointPrefab;

    [Header("Launch Settings")]
    public float forceMultiplier = 10f;
    public float maxDragDistance = 10f;

    [Header("Trajectory Prediction")]
    public int trajectoryPoints = 50;
    public float trajectoryTimeStep = 0.02f;
    public bool useHighPrecisionPrediction = true;

    [Header("Debug Settings")]
    public bool showPredictionDebug = false;

    // 私有变量
    private SpaceshipGravity spaceshipGravity;
    private Rigidbody spaceshipRb;
    private GameObject arrowInstance;
    private List<GameObject> trajectoryPointsList;
    private Vector3 dragStartPosition;
    private bool isDragging = false;
    private bool hasLaunched = false;
    private Vector3 startPosition;
    private Planet[] planets;
    private float fixedYPosition;
    private Vector3 dragOffset;
    private bool isPositionLocked = false;
    private float spaceshipMass = 1f;
    private int debugCounter = 0;

    void Start()
    {
        spaceshipGravity = GetComponent<SpaceshipGravity>();
        spaceshipRb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        fixedYPosition = startPosition.y;
        trajectoryPointsList = new List<GameObject>();

        if (spaceshipRb != null)
        {
            spaceshipMass = spaceshipRb.mass;
        }

        planets = FindObjectsOfType<Planet>();

        if (showPredictionDebug)
        {
            Debug.Log($"找到 {planets.Length} 个行星");
            foreach (Planet planet in planets)
            {
                if (planet != null)
                {
                    Debug.Log($"行星: {planet.gameObject.name}, 质量: {planet.mass}, 引力常数: {planet.gravitationalConstant}, 最大引力距离: {planet.maxGravityDistance}");
                }
            }
        }

        CreateTrajectoryPoints();
        LockYPosition();
    }

    void CreateTrajectoryPoints()
    {
        if (trajectoryPointPrefab == null)
        {
            Debug.LogWarning("Trajectory point prefab is not assigned!");
            return;
        }

        // 清理现有的轨迹点
        foreach (GameObject point in trajectoryPointsList)
        {
            if (point != null) Destroy(point);
        }
        trajectoryPointsList.Clear();

        for (int i = 0; i < trajectoryPoints; i++)
        {
            GameObject point = Instantiate(trajectoryPointPrefab, transform.position, Quaternion.identity);
            point.SetActive(false);
            trajectoryPointsList.Add(point);
        }
    }

    void LockYPosition()
    {
        if (spaceshipRb != null)
        {
            spaceshipRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            isPositionLocked = true;
        }
    }

    void UnlockYPosition()
    {
        if (spaceshipRb != null)
        {
            spaceshipRb.constraints = RigidbodyConstraints.FreezeRotation;
            isPositionLocked = false;
        }
    }

    void LockAllMovement()
    {
        if (spaceshipRb != null)
        {
            spaceshipRb.constraints = RigidbodyConstraints.FreezeAll;
            isPositionLocked = true;
        }
    }

    void Update()
    {
        HandleMouseInput();

        if (!hasLaunched && isPositionLocked)
        {
            Vector3 currentPos = transform.position;
            if (Mathf.Abs(currentPos.y - fixedYPosition) > 0.01f)
            {
                transform.position = new Vector3(currentPos.x, fixedYPosition, currentPos.z);
            }
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && !hasLaunched)
        {
            StartDrag();
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            ContinueDrag();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    void StartDrag()
    {
        Vector3 mousePos = GetStableMouseWorldPosition();
        if (mousePos == Vector3.positiveInfinity) return;

        dragOffset = transform.position - mousePos;
        dragStartPosition = mousePos;
        isDragging = true;

        if (arrowPrefab != null)
        {
            arrowInstance = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        }

        ShowTrajectoryPoints(true);
        LockAllMovement();
    }

    void ContinueDrag()
    {
        Vector3 currentMousePos = GetStableMouseWorldPosition();

        if (currentMousePos == Vector3.positiveInfinity)
        {
            if (arrowInstance != null)
            {
                Destroy(arrowInstance);
                arrowInstance = null;
            }
            ShowTrajectoryPoints(false);
            isDragging = false;
            LockYPosition();
            return;
        }

        Vector3 dragVector = (dragStartPosition + dragOffset) - (currentMousePos + dragOffset);
        dragVector.y = 0;

        if (dragVector.magnitude > maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }

        if (arrowInstance != null)
        {
            UpdateArrowVisual(dragVector);
        }

        CalculateAndDisplayTrajectory(dragVector);
    }

    void UpdateArrowVisual(Vector3 dragVector)
    {
        if (arrowInstance == null) return;

        arrowInstance.transform.position = transform.position;

        if (dragVector != Vector3.zero)
        {
            arrowInstance.transform.rotation = Quaternion.LookRotation(dragVector);
        }

        float scale = Mathf.Clamp01(dragVector.magnitude / maxDragDistance);
        arrowInstance.transform.localScale = new Vector3(1, 1, 1 + scale * 2);
    }

    void CalculateAndDisplayTrajectory(Vector3 dragVector)
    {
        Vector3 currentMousePos = GetStableMouseWorldPosition();
        if (currentMousePos == Vector3.positiveInfinity) return;

        float dragDistance = Vector3.Distance(dragStartPosition, currentMousePos);
        dragDistance = Mathf.Min(dragDistance, maxDragDistance);
        float launchForce = (dragDistance / maxDragDistance) * forceMultiplier;
        Vector3 launchVelocity = dragVector.normalized * launchForce;

        Vector3[] trajectory = useHighPrecisionPrediction ?
            PredictTrajectoryWithAdaptiveRK4(transform.position, launchVelocity, trajectoryTimeStep, trajectoryPoints) :
            PredictTrajectoryWithVerlet(transform.position, launchVelocity, trajectoryTimeStep, trajectoryPoints);

        // 修复轨迹点显示问题
        UpdateTrajectoryPointsDisplay(trajectory);
    }

    void UpdateTrajectoryPointsDisplay(Vector3[] trajectory)
    {
        int activePoints = 0;

        for (int i = 0; i < trajectoryPointsList.Count; i++)
        {
            if (i < trajectoryPointsList.Count && trajectoryPointsList[i] != null)
            {
                if (i < trajectory.Length && trajectory[i] != Vector3.zero)
                {
                    trajectoryPointsList[i].transform.position = trajectory[i];
                    trajectoryPointsList[i].SetActive(true);
                    activePoints++;
                }
                else
                {
                    trajectoryPointsList[i].SetActive(false);
                }
            }
        }

        if (showPredictionDebug)
        {
            Debug.Log($"显示 {activePoints} 个轨迹点");
        }
    }

    Vector3[] PredictTrajectoryWithAdaptiveRK4(Vector3 startPosition, Vector3 startVelocity, float timeStep, int pointsCount)
    {
        Vector3[] positions = new Vector3[pointsCount];
        Vector3 currentPos = startPosition;
        Vector3 currentVel = startVelocity;

        for (int i = 0; i < pointsCount; i++)
        {
            Vector3 k1_vel = currentVel;
            Vector3 k1_acc = CalculateTotalAcceleration(currentPos);

            Vector3 k2_vel = currentVel + k1_acc * (timeStep / 2f);
            Vector3 k2_acc = CalculateTotalAcceleration(currentPos + k1_vel * (timeStep / 2f));

            Vector3 k3_vel = currentVel + k2_acc * (timeStep / 2f);
            Vector3 k3_acc = CalculateTotalAcceleration(currentPos + k2_vel * (timeStep / 2f));

            Vector3 k4_vel = currentVel + k3_acc * timeStep;
            Vector3 k4_acc = CalculateTotalAcceleration(currentPos + k3_vel * timeStep);

            currentVel += (k1_acc + 2f * k2_acc + 2f * k3_acc + k4_acc) * (timeStep / 6f);
            currentPos += (k1_vel + 2f * k2_vel + 2f * k3_vel + k4_vel) * (timeStep / 6f);

            positions[i] = currentPos;

            if (IsPositionOutsideAllGravityRanges(currentPos))
            {
                // 填充剩余位置为当前最后位置，确保轨迹连续
                for (int j = i + 1; j < pointsCount; j++)
                {
                    positions[j] = currentPos;
                }
                break;
            }
        }

        return positions;
    }

    Vector3[] PredictTrajectoryWithVerlet(Vector3 startPosition, Vector3 startVelocity, float timeStep, int pointsCount)
    {
        Vector3[] positions = new Vector3[pointsCount];
        Vector3 currentPos = startPosition;
        Vector3 previousPos = currentPos - startVelocity * timeStep;
        Vector3 currentAccel = CalculateTotalAcceleration(currentPos);

        for (int i = 0; i < pointsCount; i++)
        {
            Vector3 newPos = 2f * currentPos - previousPos + currentAccel * timeStep * timeStep;
            Vector3 newAccel = CalculateTotalAcceleration(newPos);

            positions[i] = newPos;

            previousPos = currentPos;
            currentPos = newPos;
            currentAccel = newAccel;

            if (IsPositionOutsideAllGravityRanges(newPos))
            {
                // 填充剩余位置
                for (int j = i + 1; j < pointsCount; j++)
                {
                    positions[j] = newPos;
                }
                break;
            }
        }

        return positions;
    }

    Vector3 CalculateTotalAcceleration(Vector3 position)
    {
        Vector3 totalAcceleration = Vector3.zero;

        foreach (Planet planet in planets)
        {
            if (planet != null && planet.planetRigidbody != null)
            {
                Vector3 direction = planet.transform.position - position;
                float distance = direction.magnitude;

                if (distance > planet.maxGravityDistance || distance < 0.1f)
                    continue;

                float accelerationMagnitude = planet.gravitationalConstant * planet.mass / (distance * distance);

                Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
                totalAcceleration += horizontalDirection * accelerationMagnitude;

                if (showPredictionDebug && debugCounter < 5)
                {
                    Debug.Log($"行星 '{planet.gameObject.name}' 对位置 {position} 的加速度: {accelerationMagnitude}, 方向: {horizontalDirection}");
                    debugCounter++;
                }
            }
        }

        if (showPredictionDebug && totalAcceleration.magnitude > 0.1f && debugCounter < 5)
        {
            Debug.Log($"总加速度: {totalAcceleration.magnitude}, 方向: {totalAcceleration.normalized}");
        }

        return totalAcceleration;
    }

    bool IsPositionOutsideAllGravityRanges(Vector3 position)
    {
        foreach (Planet planet in planets)
        {
            if (planet != null)
            {
                float distance = Vector3.Distance(position, planet.transform.position);
                if (distance <= planet.maxGravityDistance)
                    return false;
            }
        }
        return true;
    }

    void EndDrag()
    {
        ShowTrajectoryPoints(false);

        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
        }

        Vector3 currentMousePos = GetStableMouseWorldPosition();

        if (currentMousePos == Vector3.positiveInfinity)
        {
            isDragging = false;
            LockYPosition();
            return;
        }

        Vector3 launchDirection = (dragStartPosition - currentMousePos).normalized;
        launchDirection.y = 0;

        float dragDistance = Vector3.Distance(dragStartPosition, currentMousePos);
        dragDistance = Mathf.Min(dragDistance, maxDragDistance);

        float launchForce = (dragDistance / maxDragDistance) * forceMultiplier;

        UnlockYPosition();

        spaceshipRb.velocity = launchDirection * launchForce;
        hasLaunched = true;

        if (spaceshipGravity != null)
        {
            spaceshipGravity.SetLaunched(true);
        }

        isDragging = false;
        debugCounter = 0;

        if (showPredictionDebug)
        {
            Debug.Log($"发射参数 - 速度: {launchForce}, 方向: {launchDirection}");
        }
    }

    Vector3 GetStableMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("planet"))
            {
                return Vector3.positiveInfinity;
            }
        }

        Plane heightPlane = new Plane(Vector3.up, new Vector3(0, fixedYPosition, 0));

        float distance;
        if (heightPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    void ShowTrajectoryPoints(bool show)
    {
        for (int i = 0; i < trajectoryPointsList.Count; i++)
        {
            if (trajectoryPointsList[i] != null)
            {
                trajectoryPointsList[i].SetActive(show);
            }
        }
    }

    public void ResetLauncher()
    {
        hasLaunched = false;
        isDragging = false;

        transform.position = startPosition;
        if (spaceshipRb != null)
        {
            spaceshipRb.velocity = Vector3.zero;
            spaceshipRb.angularVelocity = Vector3.zero;
        }

        LockYPosition();

        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
        }

        ShowTrajectoryPoints(false);

        if (spaceshipGravity != null)
        {
            spaceshipGravity.SetLaunched(false);
        }

        debugCounter = 0;
    }

    void OnDestroy()
    {
        foreach (GameObject point in trajectoryPointsList)
        {
            if (point != null)
            {
                Destroy(point);
            }
        }
        trajectoryPointsList.Clear();
    }

    public void ValidatePredictionAccuracy()
    {
        if (!hasLaunched) return;

        Vector3[] predictedTrajectory = PredictTrajectoryWithAdaptiveRK4(
            startPosition, spaceshipRb.velocity, trajectoryTimeStep, trajectoryPoints);

        Vector3 actualPosition = transform.position;
        Vector3 predictedPosition = predictedTrajectory[Mathf.Min(10, trajectoryPoints - 1)];

        float error = Vector3.Distance(actualPosition, predictedPosition);
        Debug.Log($"预测误差: {error}, 实际位置: {actualPosition}, 预测位置: {predictedPosition}");
    }
}