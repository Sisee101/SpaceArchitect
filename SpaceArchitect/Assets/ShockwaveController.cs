using UnityEngine;

public class ShockwaveController : MonoBehaviour
{
    public Material shockwaveMaterial;

    [Header("动画设置")]
    public float shockwaveDuration = 1f; // 冲击波持续时间
    public float maxDistance = 1.5f; // 扩散多远 (根据屏幕比例调整)
    [Range(0, 0.5f)] public float width = 0.1f;
    [Range(-0.2f, 0.2f)] public float strength = 0.05f;

    private float currentTimer = 0f;
    private bool isPlaying = false;

    // Shader 属性的 ID，比用字符串名字更快
    private int distancePropID = Shader.PropertyToID("_RippleDistanceFromCenter");
    private int strengthPropID = Shader.PropertyToID("_RippleStrength");
    private int widthPropID = Shader.PropertyToID("_RippleWidth");
    private int centerPropID = Shader.PropertyToID("_CenterPoint");

    void Start()
    {
        // 开始时隐藏效果
        if (shockwaveMaterial != null)
        {
            shockwaveMaterial.SetFloat(strengthPropID, 0);
        }
    }

    void Update()
    {
        // 测试：按下 E 键触发
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerShockwave(new Vector2(0.5f, 0.5f)); // 默认从屏幕中心触发
        }

        if (isPlaying)
        {
            currentTimer += Time.unscaledDeltaTime; // 使用未缩放的时间，确保时停时也能播放

            float progress = currentTimer / shockwaveDuration;

            // 让距离随时间线性增加
            float currentDistance = Mathf.Lerp(-width, maxDistance, progress);

            // 让强度随时间慢慢减弱，最后消失
            float currentStrength = Mathf.Lerp(strength, 0f, progress);

            shockwaveMaterial.SetFloat(distancePropID, currentDistance);
            shockwaveMaterial.SetFloat(strengthPropID, currentStrength);
            shockwaveMaterial.SetFloat(widthPropID, width);

            if (progress >= 1f)
            {
                isPlaying = false;
                shockwaveMaterial.SetFloat(strengthPropID, 0); // 确保完全关闭
            }
        }
    }

    // 公共方法供外部调用
    public void TriggerShockwave(Vector2 viewportCenter)
    {
        currentTimer = 0f;
        isPlaying = true;
        // 设置中心点（如果你想让冲击波从主角脚下爆发，需要把世界坐标转为屏幕视口坐标 0-1）
        shockwaveMaterial.SetVector(centerPropID, viewportCenter);
    }
}