using UnityEngine;

public class CometMover : MonoBehaviour
{
    public float speed = 10f;       // 飞行速度
    public float lifeTime = 10f;    // 存活时间（秒），防止飞太远不销毁占内存

    void Start()
    {
        // 自动销毁倒计时
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 沿着自身的“前方”飞行（根据生成时的旋转角度）
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }
}