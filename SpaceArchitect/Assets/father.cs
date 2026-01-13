using UnityEngine;

// 挂在父物体上
public class RandomTumbleParent : MonoBehaviour
{
    [Header("变化设置")]
    public float changeInterval = 2.0f;    // 每隔几秒改变一次状态
    public float transitionSpeed = 2.0f;   // 改变状态时的平滑过渡速度

    [Header("速度范围")]
    public float minSpeed = 30f;
    public float maxSpeed = 120f;

    private float _timer;
    // 目标状态
    private Vector3 _targetAxis;
    private float _targetSpeed;
    // 当前状态
    private Vector3 _currentAxis;
    private float _currentSpeed;

    void Start()
    {
        // 初始化一个随机状态
        PickNewState();
        _currentAxis = _targetAxis;
        _currentSpeed = _targetSpeed;
    }

    void Update()
    {
        // 1. 计时器逻辑
        _timer += Time.deltaTime;
        if (_timer >= changeInterval)
        {
            PickNewState();
            _timer = 0; // 重置计时器
        }

        // 2. 平滑插值（让变化不突兀）
        // 使用 Lerp 让当前轴向和速度慢慢向目标值靠拢
        _currentAxis = Vector3.Lerp(_currentAxis, _targetAxis, Time.deltaTime * transitionSpeed);
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * transitionSpeed);

        // 3. 应用旋转 (Space.World 保证是在原地翻滚)
        transform.Rotate(_currentAxis * _currentSpeed * Time.deltaTime, Space.World);
    }

    // 随机生成一个新的旋转轴和速度
    void PickNewState()
    {
        _targetAxis = Random.onUnitSphere; // 随机球面上的一点作为旋转轴
        _targetSpeed = Random.Range(minSpeed, maxSpeed);
    }
}