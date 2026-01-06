using UnityEngine;

public class SpaceshipThruster : MonoBehaviour
{
    [Header("推进力设置")]
    public float moveForce = 50f;

    [Header("渐变时间设置(秒)")]
    public float forceIncreaseTime = 0.5f;
    public float forceDecreaseTime = 0.3f;

    [Header("相机参考")]
    public Camera mainCamera;

    private Rigidbody spaceshipRb;
    private Vector3 currentForce;
    private Vector3 targetForce;

    void Start()
    {
        spaceshipRb = GetComponent<Rigidbody>();
        currentForce = Vector3.zero;
        targetForce = Vector3.zero;

        // 自动获取主摄像机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("SpaceshipThruster: 未找到主摄像机！请手动指定。");
            }
            else
            {
                Debug.Log("SpaceshipThruster: 已自动获取主摄像机 -> " + mainCamera.name);
            }
        }
    }

    void Update()
    {
        HandleInput();
        UpdateForceTransition();
    }

    void FixedUpdate()
    {
        ApplyForces();
    }

    void HandleInput()
    {
        // 重置目标力
        targetForce = Vector3.zero;

        if (mainCamera == null)
        {
            Debug.LogWarning("SpaceshipThruster: 主摄像机引用为空，无法处理输入。");
            return;
        }

        // 获取基于摄像机的方向向量
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // 投影到水平面 (X-Z平面)，忽略Y轴分量
        cameraForward.y = 0;
        cameraRight.y = 0;

        // 归一化确保是纯方向向量
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 调试输出摄像机方向向量
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log($"摄像机前向 (水平投影): {cameraForward}");
            Debug.Log($"摄像机右向 (水平投影): {cameraRight}");
        }

        // 处理输入
        bool wKey = Input.GetKey(KeyCode.W);
        bool sKey = Input.GetKey(KeyCode.S);
        bool aKey = Input.GetKey(KeyCode.A);
        bool dKey = Input.GetKey(KeyCode.D);

        // 调试输出按键状态
        if (wKey || sKey || aKey || dKey)
        {
            Debug.Log($"按键状态 - W:{wKey}, S:{sKey}, A:{aKey}, D:{dKey}");
        }

        if (wKey)
        {
            targetForce += cameraForward * moveForce;
            Debug.Log("W键按下，添加向前推力。");
        }
        if (sKey)
        {
            targetForce += -cameraForward * moveForce;
            Debug.Log("S键按下，添加向后推力。");
        }
        if (aKey)
        {
            targetForce += -cameraRight * moveForce;
            Debug.Log("A键按下，添加向左推力。");
        }
        if (dKey)
        {
            targetForce += cameraRight * moveForce;
            Debug.Log("D键按下，添加向右推力。");
        }

        // 调试最终计算出的目标力
        if (targetForce != Vector3.zero)
        {
            Debug.Log($"计算出的目标力 (世界坐标): {targetForce}");
        }
    }

    void UpdateForceTransition()
    {
        float increaseSpeed = (forceIncreaseTime > 0) ? 1f / forceIncreaseTime : 10f;
        float decreaseSpeed = (forceDecreaseTime > 0) ? 1f / forceDecreaseTime : 10f;

        currentForce = Vector3.MoveTowards(currentForce, targetForce,
            (targetForce != Vector3.zero ? increaseSpeed : decreaseSpeed) * Time.deltaTime * moveForce);
    }

    void ApplyForces()
    {
        if (spaceshipRb != null)
        {
            spaceshipRb.AddForce(currentForce);
            // 可选：调试当前应用的力
            // Debug.Log($"当前应用的力: {currentForce}");
        }
        else
        {
            Debug.LogError("SpaceshipThruster: 未找到Rigidbody组件！");
        }
    }
}