using UnityEngine;

public class ShockwaveController : MonoBehaviour
{
    public Material shockwaveMaterial;

    [Header("动画设置")]
    public float shockwaveDuration = 1f;
    public float maxDistance = 1.5f;
    [Range(0, 0.5f)] public float width = 0.1f;
    [Range(-1f, 1f)] public float strength = 0.05f;

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
        if (Input.GetKeyDown(KeyCode.B))
        {
            TriggerShockwave(new Vector2(0.5f, 0.5f));
        }

        if (isPlaying)
        {
            currentTimer += Time.unscaledDeltaTime;

            float progress = currentTimer / shockwaveDuration;

            float currentDistance = Mathf.Lerp(-width, maxDistance, progress);
            float currentStrength = Mathf.Lerp(strength, 0f, progress);

            shockwaveMaterial.SetFloat(distancePropID, currentDistance);
            shockwaveMaterial.SetFloat(strengthPropID, currentStrength);
            shockwaveMaterial.SetFloat(widthPropID, width);

            if (progress >= 1f)
            {
                isPlaying = false;
                shockwaveMaterial.SetFloat(strengthPropID, 0);
            }
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
