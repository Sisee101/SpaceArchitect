using UnityEngine;

// 挂在每一个环（子物体）上
public class ChildSelfRotate : MonoBehaviour
{
    [Header("自转设置")]
    public Vector3 rotateAxis = new Vector3(0, 1, 0); // 默认绕Y轴转
    public float rotateSpeed = 100f;

    void Update()
    {
        // Space.Self 是关键，确保它是相对于父物体独立转动的
        transform.Rotate(rotateAxis.normalized * rotateSpeed * Time.deltaTime, Space.Self);
    }
}