using UnityEngine;

public class VisualOrbit : MonoBehaviour
{
    [Header("核心设置")]
    public Transform centerPoint; // 圆心（如果不填，默认以世界原点(0,0,0)为中心）

    [Header("轨道参数 (在Scene里可见)")]
    [Range(0.1f, 50f)]
    public float radius = 5f;     // 半径
    public float speed = 50f;     // 旋转速度
    public Vector3 tilt = new Vector3(0, 0, 0); // 轨道的倾斜角度 (X, Y, Z)

    [Header("调试")]
    public bool showGizmo = true; // 是否显示辅助线
    [Range(0, 360)]
    public float startAngle = 0f; // 初始角度位置

    // 内部变量
    private float currentAngle;

    void Start()
    {
        currentAngle = startAngle;
    }

    void Update()
    {
        // 1. 计算当前角度
        currentAngle += speed * Time.deltaTime;
        currentAngle %= 360f; // 保持角度在 0-360 之间

        // 2. 计算位置
        Vector3 finalPos = CalculatePosition(currentAngle);

        // 3. 应用位置
        transform.position = finalPos;
    }

    // 数学核心：根据角度计算出在倾斜圆环上的坐标
    Vector3 CalculatePosition(float angle)
    {
        // 将角度转为弧度
        float rad = angle * Mathf.Deg2Rad;

        // A. 先在一个平躺的平面(XZ平面)上计算圆周位置
        // 公式：x = r * cos, z = r * sin
        Vector3 flatPos = new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);

        // B. 计算倾斜旋转 (Quaternion.Euler)
        Quaternion rotation = Quaternion.Euler(tilt);

        // C. 将平躺的坐标 乘以 旋转四元数 = 倾斜后的坐标
        Vector3 tiltedPos = rotation * flatPos;

        // D. 加上中心点的偏移量
        Vector3 centerPos = centerPoint != null ? centerPoint.position : Vector3.zero;

        return centerPos + tiltedPos;
    }

    // 这个函数会让Unity编辑器在Scene窗口画出辅助线
    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // 设置线条颜色
        Gizmos.color = Color.cyan;

        // 我们可以画由60个小线段组成的圆
        Vector3 prevPos = CalculatePosition(0);
        int segments = 60;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f;
            Vector3 nextPos = CalculatePosition(angle);

            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }
}