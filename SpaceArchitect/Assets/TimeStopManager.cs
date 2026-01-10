using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; // 必须引用

public class ScreenColorEffect : MonoBehaviour
{
    [Header("配置")]
    public Volume postProcessVolume; // 拖入你的 Volume
    public KeyCode triggerKey = KeyCode.T;

    [Header("动画参数")]
    public float transitionDuration = 0.2f; // 变色的过渡时间 (0 -> 1)
    public float holdDuration = 2.0f;       // 黑白保持多久

    private Coroutine currentRoutine;

    void Start()
    {
        // 游戏开始确保是彩色的
        if (postProcessVolume != null) postProcessVolume.weight = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            // 如果正在播放，先停止旧的，重新开始
            if (currentRoutine != null) StopCoroutine(currentRoutine);
            currentRoutine = StartCoroutine(DoColorPulse());
        }
    }

    // 自动流程：变黑白 -> 等待 -> 变彩色
    IEnumerator DoColorPulse()
    {
        // 1. 渐变到黑白 (Weight 0 -> 1)
        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionDuration;
            if (postProcessVolume != null)
                postProcessVolume.weight = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        // 确保完全变黑白
        if (postProcessVolume != null) postProcessVolume.weight = 1f;

        // 2. 保持黑白状态 (Hold)
        yield return new WaitForSeconds(holdDuration);

        // 3. 渐变回彩色 (Weight 1 -> 0)
        timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime; // 使用普通的 deltaTime
            float progress = timer / transitionDuration;
            if (postProcessVolume != null)
                postProcessVolume.weight = Mathf.Lerp(1f, 0f, progress);
            yield return null;
        }
        // 确保完全变回彩色
        if (postProcessVolume != null) postProcessVolume.weight = 0f;
    }
}