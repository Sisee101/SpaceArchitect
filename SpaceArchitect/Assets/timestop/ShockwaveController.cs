using UnityEngine;

public class ShockwaveController : MonoBehaviour
{
    public Material shockwaveMaterial;

    [Header("动画设置")]
    public float contractionDuration = 0.15f;
    public float contractionStrength = -0.15f;
    public float shockwaveDuration = 0.8f;
    public float maxDistance = 2.4f;
    [Range(0, 0.5f)] public float width = 0.05f;
    [Range(-1f, 1f)] public float strength = 0.2f;

    private float currentTimer = 0f;
    private bool isPlaying = false;

    private int distancePropID = Shader.PropertyToID("_RippleDistanceFromCenter");
    private int strengthPropID = Shader.PropertyToID("_RippleStrength");
    private int widthPropID = Shader.PropertyToID("_RippleWidth");
    private int centerPropID = Shader.PropertyToID("_CenterPoint");

    void Start()
    {
        if (shockwaveMaterial != null)
        {
            shockwaveMaterial.SetFloat(strengthPropID, 0);
            // 初始化中心点（屏幕中心）
            shockwaveMaterial.SetVector(centerPropID, new Vector4(0.5f, 0.5f, 0, 0));
        }
        else
        {
            Debug.LogWarning("ShockwaveController: shockwaveMaterial 未赋值！");
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            currentTimer += Time.unscaledDeltaTime;
            float totalTime = contractionDuration + shockwaveDuration;

            if (currentTimer <= contractionDuration)
            {
                // --- 阶段 1：收缩阶段 (蓄力) ---
                float p = currentTimer / contractionDuration;
                // 强度从 0 变为负数，产生向内拉伸感
                float currentDistance = Mathf.Lerp(-width, 0f, p);
                float currentStrength = Mathf.Lerp(0f, contractionStrength, p);
                
                shockwaveMaterial.SetFloat(distancePropID, currentDistance);
                shockwaveMaterial.SetFloat(strengthPropID, currentStrength);
            }
            else if (currentTimer <= totalTime)
            {
                // --- 阶段 2：释放阶段 (冲击波) ---
                float p = (currentTimer - contractionDuration) / shockwaveDuration;
                // 距离从中心向外扩散，强度从正值衰减到 0
                float currentDistance = Mathf.Lerp(0f, maxDistance, p);
                float currentStrength = Mathf.Lerp(strength, 0f, p);

                shockwaveMaterial.SetFloat(distancePropID, currentDistance);
                shockwaveMaterial.SetFloat(strengthPropID, currentStrength);
            }
            else
            {
                // 动画结束，重置状态
                isPlaying = false;
                shockwaveMaterial.SetFloat(strengthPropID, 0);
            }
            
            // 保持宽度同步
            shockwaveMaterial.SetFloat(widthPropID, width);
        }
    }

    public void TriggerShockwave(Vector2 viewportCenter)
    {
        currentTimer = 0f;
        isPlaying = true;
        
        // 确保传入的是 Vector4（shader 需要 Vector4）
        // viewportCenter 应该是屏幕空间坐标 (0-1)
        Vector4 centerPoint = new Vector4(viewportCenter.x, viewportCenter.y, 0, 0);
        shockwaveMaterial.SetVector(centerPropID, centerPoint);
    }
}
