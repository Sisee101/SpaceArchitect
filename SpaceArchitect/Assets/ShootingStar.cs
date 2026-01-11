using UnityEngine;

public class ShootingStar : MonoBehaviour
{
    [Header("流星飞行参数")]
    [Tooltip("水平速度：正数向右飞，负数向左飞")]
    public float speedX = 15f;

    [Tooltip("垂直倾斜：0为水平，负数向下坠，正数向上飘")]
    public float speedY = -2f;

    [Tooltip("存活时间（秒）")]
    public float lifeTime = 10f;

    // 内部记录实际速度
    private Vector2 _currentVelocity;

    void Start()
    {
        // 1. 初始化速度
        _currentVelocity = new Vector2(speedX, speedY);

        // 2. 自动销毁倒计时
        Destroy(gameObject, lifeTime);

        // 3. 自动调整角度：让流星头部对齐飞行方向
        UpdateRotation();
    }

    void Update()
    {
        // 4. 持续飞行
        transform.Translate(_currentVelocity * Time.deltaTime, Space.World);
    }

    // 编辑器功能：当你在 Inspector 调整数值时，实时更新角度预览
    void OnValidate()
    {
        _currentVelocity = new Vector2(speedX, speedY);
        UpdateRotation();
    }

    // 封装旋转逻辑，避免代码重复
    void UpdateRotation()
    {
        if (_currentVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(_currentVelocity.y, _currentVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}