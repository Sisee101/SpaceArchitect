using UnityEngine;

public class SpaceFloater : MonoBehaviour
{
    [Header("浮动参数")]
    [Tooltip("浮动的幅度（上下移动的距离）")]
    public float amplitude = 0.5f;

    [Tooltip("浮动的速度（越小越慢，更有宇宙感）")]
    public float frequency = 0.5f;

    [Header("旋转参数")]
    [Tooltip("自转速度")]
    public float rotationSpeed = 10f;

    [Tooltip("旋转轴向（可以让它不规则地转）")]
    public Vector3 rotationAxis = new Vector3(0.5f, 1.0f, 0.2f);

    // 记录初始位置
    private Vector3 startPos;
    // 随机的时间偏移量，防止多个物体同步运动
    private float timeOffset;

    void Start()
    {
        // 记录物体刚开始的位置
        startPos = transform.position;
        // 生成一个随机偏移，让每个物体都在正弦波的不同位置开始
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        // 1. 浮动逻辑 (使用 Sin 函数制造平滑的来回运动)
        // 公式：初始Y + Sin(时间 * 速度 + 随机偏移) * 幅度
        float newY = startPos.y + Mathf.Sin(Time.time * frequency + timeOffset) * amplitude;

        // 更新位置 (只改变Y轴，保持XZ轴在原位)
        // 如果想做全向漂浮，可以将 X 和 Z 也加入类似的 Sin 运算
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 2. 旋转逻辑 (绕着指定轴向缓慢旋转)
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}