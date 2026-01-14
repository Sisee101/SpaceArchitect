using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 场景加载按钮脚本
/// 可复用的按钮组件，用于点击按钮加载指定场景
/// </summary>
public class SceneLoadButton : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("用于触发场景加载的 Unity UI Button")]
    [SerializeField] private Button loadSceneButton;
    
    [Header("场景配置")]
    [Tooltip("要加载的场景名称（必须在Build Settings中，区分大小写）")]
    [SerializeField] private string targetSceneName;
    
    [Header("音效（可选）")]
    [Tooltip("音频源组件（如果未指定，会自动获取或创建）")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("按钮点击音效（可选，如果未配置则直接加载场景）")]
    [SerializeField] private AudioClip buttonClickSound;
    
    [Tooltip("点击音效播放后的延迟时间（秒），用于确保音效播放完成再执行场景切换")]
    [SerializeField] private float clickSoundDelay = 0.15f;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Start()
    {
        // 如果未手动指定 AudioSource，尝试自动获取
        if (audioSource == null && buttonClickSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            // 如果还是没有，自动添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 绑定按钮点击事件
        if (loadSceneButton != null)
        {
            loadSceneButton.onClick.AddListener(OnButtonClicked);
            
            if (enableDebugLog)
            {
                Debug.Log($"SceneLoadButton: 已绑定按钮点击事件，目标场景: {targetSceneName}");
            }
        }
        else
        {
            Debug.LogWarning("SceneLoadButton: Load Scene Button 未配置！请在 Inspector 中设置按钮引用。");
        }
        
        // 验证配置
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("SceneLoadButton: Target Scene Name 未配置！请在 Inspector 中设置目标场景名称。");
        }
    }
    
    void OnDestroy()
    {
        // 取消绑定按钮点击事件
        if (loadSceneButton != null)
        {
            loadSceneButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClicked()
    {
        // 验证配置
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("SceneLoadButton: 目标场景名称为空，无法加载场景！");
            return;
        }
        
        // 如果配置了音效，播放音效并延迟加载场景
        if (buttonClickSound != null && audioSource != null)
        {
            StartCoroutine(PlayClickSoundAndLoadScene());
        }
        else
        {
            // 直接加载场景
            LoadScene();
        }
    }
    
    /// <summary>
    /// 播放点击音效并加载场景（协程）
    /// </summary>
    private IEnumerator PlayClickSoundAndLoadScene()
    {
        // 播放点击音效
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
            
            if (enableDebugLog)
            {
                Debug.Log($"SceneLoadButton: 播放点击音效，延迟 {clickSoundDelay} 秒后加载场景");
            }
        }
        
        // 等待延迟时间
        yield return new WaitForSeconds(clickSoundDelay);
        
        // 加载场景
        LoadScene();
    }
    
    /// <summary>
    /// 加载场景
    /// </summary>
    private void LoadScene()
    {
        // 使用 SceneTransitionManager 加载场景
        if (SceneTransitionManager.Instance != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"SceneLoadButton: 正在通过 SceneTransitionManager 加载场景 {targetSceneName}");
            }
            
            SceneTransitionManager.Instance.LoadSceneByName(targetSceneName);
        }
        else
        {
            Debug.LogError("SceneLoadButton: SceneTransitionManager 未找到！无法加载场景。");
        }
    }
    
    /// <summary>
    /// 手动设置目标场景名称（可在运行时调用）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void SetTargetSceneName(string sceneName)
    {
        targetSceneName = sceneName;
        
        if (enableDebugLog)
        {
            Debug.Log($"SceneLoadButton: 已设置目标场景名称为 {sceneName}");
        }
    }
    
    /// <summary>
    /// 获取当前目标场景名称
    /// </summary>
    /// <returns>场景名称</returns>
    public string GetTargetSceneName()
    {
        return targetSceneName;
    }
}
