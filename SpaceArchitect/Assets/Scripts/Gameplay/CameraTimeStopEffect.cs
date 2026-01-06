using System.Collections;
using UnityEngine;

/// <summary>
/// 相机时停效果控制器
/// 响应时停事件，实现相机zoom in特写效果和视觉效果
/// </summary>
public class CameraTimeStopEffect : MonoBehaviour
{
    [Header("相机引用")]
    [Tooltip("要控制的相机（如果为空，使用主相机）")]
    [SerializeField] private Camera targetCamera;

    [Header("Zoom In 设置")]
    [Tooltip("时停时的缩放倍数（相对于初始大小，小于1表示放大）")]
    [SerializeField] private float zoomInScale = 0.5f;

    [Tooltip("Zoom in 的过渡时间（秒）")]
    [SerializeField] private float zoomInDuration = 0.3f;

    [Tooltip("Zoom out 的过渡时间（秒）")]
    [SerializeField] private float zoomOutDuration = 0.3f;

    [Tooltip("Zoom 过渡的缓动曲线")]
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("视觉效果设置")]
    [Tooltip("时停时的视觉效果（动画/特效，可选）")]
    [SerializeField] private GameObject timeStopVFX;

    [Tooltip("视觉效果播放位置（如果为空，使用相机位置）")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Tooltip("视觉效果持续时间（秒，0表示跟随时停时长）")]
    [SerializeField] private float vfxDuration = 0f;

    [Header("目标飞船")]
    [Tooltip("要特写的飞船（如果为空，从事件中获取）")]
    [SerializeField] private Transform targetShip;

    // 内部状态
    private float initialOrthographicSize; // 初始正交大小
    private float initialFieldOfView; // 初始视野角度
    private bool isOrthographic; // 是否为正交摄像机
    private bool isZooming = false; // 是否正在缩放
    private Coroutine zoomCoroutine; // 缩放协程
    private GameObject currentVFX; // 当前播放的视觉效果

    void Awake()
    {
        // 获取相机组件
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError("CameraTimeStopEffect: 未找到相机！");
            return;
        }

        // 保存初始缩放值
        isOrthographic = targetCamera.orthographic;
        if (isOrthographic)
        {
            initialOrthographicSize = targetCamera.orthographicSize;
        }
        else
        {
            initialFieldOfView = targetCamera.fieldOfView;
        }
    }

    void Start()
    {
        // 订阅时停事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnTimeStopStart += OnTimeStopStart;
            EventManager.Instance.OnTimeStopEnd += OnTimeStopEnd;
        }
        else
        {
            Debug.LogWarning("CameraTimeStopEffect: EventManager 未找到，时停效果将不可用");
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnTimeStopStart -= OnTimeStopStart;
            EventManager.Instance.OnTimeStopEnd -= OnTimeStopEnd;
        }

        // 清理视觉效果
        if (currentVFX != null)
        {
            Destroy(currentVFX);
        }
    }

    /// <summary>
    /// 时停开始事件处理
    /// </summary>
    private void OnTimeStopStart(GameObject ship, float timeScale)
    {
        if (targetCamera == null)
        {
            return;
        }

        // 设置目标飞船
        if (ship != null)
        {
            targetShip = ship.transform;
        }

        // 执行 zoom in
        StartZoomIn();

        // 播放视觉效果
        PlayTimeStopVFX();
    }

    /// <summary>
    /// 时停结束事件处理
    /// </summary>
    private void OnTimeStopEnd(GameObject ship)
    {
        if (targetCamera == null)
        {
            return;
        }

        // 执行 zoom out
        StartZoomOut();

        // 停止视觉效果
        StopTimeStopVFX();
    }

    /// <summary>
    /// 开始 zoom in
    /// </summary>
    private void StartZoomIn()
    {
        if (isZooming)
        {
            // 如果正在缩放，先停止当前协程
            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }
        }

        zoomCoroutine = StartCoroutine(ZoomInCoroutine());
    }

    /// <summary>
    /// 开始 zoom out
    /// </summary>
    private void StartZoomOut()
    {
        if (isZooming)
        {
            // 如果正在缩放，先停止当前协程
            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }
        }

        zoomCoroutine = StartCoroutine(ZoomOutCoroutine());
    }

    /// <summary>
    /// Zoom in 协程
    /// </summary>
    private IEnumerator ZoomInCoroutine()
    {
        isZooming = true;

        float startValue, endValue;
        
        if (isOrthographic)
        {
            startValue = targetCamera.orthographicSize;
            endValue = initialOrthographicSize * zoomInScale;
        }
        else
        {
            startValue = targetCamera.fieldOfView;
            endValue = initialFieldOfView * zoomInScale;
        }

        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用未缩放时间，确保即使在时停中也能平滑过渡
            float t = elapsed / zoomInDuration;
            float curveValue = zoomCurve.Evaluate(t);

            float currentValue = Mathf.Lerp(startValue, endValue, curveValue);

            if (isOrthographic)
            {
                targetCamera.orthographicSize = currentValue;
            }
            else
            {
                targetCamera.fieldOfView = currentValue;
            }

            yield return null;
        }

        // 确保最终值正确
        if (isOrthographic)
        {
            targetCamera.orthographicSize = endValue;
        }
        else
        {
            targetCamera.fieldOfView = endValue;
        }

        isZooming = false;
        zoomCoroutine = null;
    }

    /// <summary>
    /// Zoom out 协程
    /// </summary>
    private IEnumerator ZoomOutCoroutine()
    {
        isZooming = true;

        float startValue, endValue;
        
        if (isOrthographic)
        {
            startValue = targetCamera.orthographicSize;
            endValue = initialOrthographicSize;
        }
        else
        {
            startValue = targetCamera.fieldOfView;
            endValue = initialFieldOfView;
        }

        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用未缩放时间
            float t = elapsed / zoomOutDuration;
            float curveValue = zoomCurve.Evaluate(t);

            float currentValue = Mathf.Lerp(startValue, endValue, curveValue);

            if (isOrthographic)
            {
                targetCamera.orthographicSize = currentValue;
            }
            else
            {
                targetCamera.fieldOfView = currentValue;
            }

            yield return null;
        }

        // 确保最终值正确
        if (isOrthographic)
        {
            targetCamera.orthographicSize = endValue;
        }
        else
        {
            targetCamera.fieldOfView = endValue;
        }

        isZooming = false;
        zoomCoroutine = null;
    }

    /// <summary>
    /// 播放时停视觉效果
    /// </summary>
    private void PlayTimeStopVFX()
    {
        if (timeStopVFX == null)
        {
            return;
        }

        // 如果已有视觉效果在播放，先停止
        StopTimeStopVFX();

        // 确定生成位置
        Vector3 spawnPosition;
        if (vfxSpawnPoint != null)
        {
            spawnPosition = vfxSpawnPoint.position;
        }
        else if (targetShip != null)
        {
            spawnPosition = targetShip.position;
        }
        else if (targetCamera != null)
        {
            spawnPosition = targetCamera.transform.position;
        }
        else
        {
            spawnPosition = Vector3.zero;
        }

        // 实例化视觉效果
        currentVFX = Instantiate(timeStopVFX, spawnPosition, Quaternion.identity);

        // 如果设置了持续时间，自动销毁
        if (vfxDuration > 0f)
        {
            StartCoroutine(DestroyVFXAfterDelay(vfxDuration));
        }
    }

    /// <summary>
    /// 停止时停视觉效果
    /// </summary>
    private void StopTimeStopVFX()
    {
        if (currentVFX != null)
        {
            // 尝试停止粒子系统
            ParticleSystem ps = currentVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
            }

            // 尝试停止动画
            Animator animator = currentVFX.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            // 延迟销毁（给粒子时间消失）
            StartCoroutine(DestroyVFXAfterDelay(1f));
            currentVFX = null;
        }
    }

    /// <summary>
    /// 延迟销毁视觉效果
    /// </summary>
    private IEnumerator DestroyVFXAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (currentVFX != null)
        {
            Destroy(currentVFX);
            currentVFX = null;
        }
    }

    /// <summary>
    /// 手动触发 zoom in（用于测试）
    /// </summary>
    [ContextMenu("测试 Zoom In")]
    public void TestZoomIn()
    {
        StartZoomIn();
    }

    /// <summary>
    /// 手动触发 zoom out（用于测试）
    /// </summary>
    [ContextMenu("测试 Zoom Out")]
    public void TestZoomOut()
    {
        StartZoomOut();
    }
}
