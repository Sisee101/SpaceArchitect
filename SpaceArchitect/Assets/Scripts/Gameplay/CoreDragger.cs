using UnityEngine;

/// <summary>
/// Core物体拖拽脚本
/// 只对tag为Core的物体有效，可以在XY平面自由拖拽
/// 物体不受引力影响，位置仅由拖拽决定
/// </summary>
public class CoreDragger : MonoBehaviour
{
    private bool isDragging = false;
    private Rigidbody coreRb;
    private NBody nBody;
    private GravityEngine gravityEngine;
    private Vector3 offset;
    private float mouseZCoord;

    void Start()
    {
        coreRb = GetComponent<Rigidbody>();
        nBody = GetComponent<NBody>();
        gravityEngine = GravityEngine.instance;

        // 确保物体不受引力影响
        RemoveFromGravityEngine();

        // 确保Rigidbody始终是运动学的，这样位置完全由拖拽控制
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
        }
    }

    void FixedUpdate()
    {
        // 持续确保物体不在引力引擎中（防止被自动添加）
        if (nBody != null && nBody.engineRef != null)
        {
            RemoveFromGravityEngine();
        }

        // 确保Rigidbody始终是运动学的
        if (coreRb != null && !coreRb.isKinematic)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
        }
    }

    void OnMouseDown()
    {
        // 只对tag为Core的物体有效
        if (!gameObject.CompareTag("Core"))
        {
            return;
        }

        // 确保物体不在引力引擎中
        RemoveFromGravityEngine();

        isDragging = true;
        mouseZCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPos();

        // 确保是运动学模式，位置完全由拖拽控制
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }
    }

    void OnMouseDrag()
    {
        // 只对tag为Core的物体有效
        if (!isDragging || !gameObject.CompareTag("Core"))
        {
            return;
        }

        Vector3 newPosition = GetMouseWorldPos() + offset;
        newPosition.z = transform.position.z; // 保持原有Z轴深度，只在XY平面移动
        transform.position = newPosition;
    }

    void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        // 保持运动学模式，物体留在拖拽结束的位置
        // 不恢复物理模拟，确保位置完全由拖拽决定
        if (coreRb != null)
        {
            coreRb.isKinematic = true;
            coreRb.useGravity = false;
            coreRb.velocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
        }

        // 确保物体不在引力引擎中
        RemoveFromGravityEngine();
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mouseZCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    /// <summary>
    /// 从引力引擎中移除物体
    /// </summary>
    private void RemoveFromGravityEngine()
    {
        if (nBody != null && gravityEngine != null)
        {
            if (nBody.engineRef != null)
            {
                gravityEngine.RemoveBody(gameObject);
                nBody.engineRef = null;
            }
        }
    }
}
