using UnityEngine;

/// <summary>
/// 捕获系统切换工具
/// 可以一键启用/禁用场景中所有行星的捕获系统
/// </summary>
public class CaptureSystemToggle : MonoBehaviour
{
    [Header("快捷键设置")]
    [Tooltip("切换捕获系统的快捷键")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;
    
    [Header("状态")]
    [Tooltip("捕获系统是否启用")]
    [SerializeField] private bool captureEnabled = true;
    
    void Update()
    {
        // 按下快捷键切换捕获系统
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleCaptureSystem();
        }
    }
    
    /// <summary>
    /// 切换捕获系统开关
    /// </summary>
    public void ToggleCaptureSystem()
    {
        captureEnabled = !captureEnabled;
        ApplyCaptureState(captureEnabled);
        
        Debug.Log($"<color=yellow>捕获系统已 {(captureEnabled ? "启用" : "禁用")}</color>");
    }
    
    /// <summary>
    /// 启用捕获系统
    /// </summary>
    [ContextMenu("启用捕获系统")]
    public void EnableCaptureSystem()
    {
        captureEnabled = true;
        ApplyCaptureState(true);
        Debug.Log("<color=green>捕获系统已启用</color>");
    }
    
    /// <summary>
    /// 禁用捕获系统
    /// </summary>
    [ContextMenu("禁用捕获系统")]
    public void DisableCaptureSystem()
    {
        captureEnabled = false;
        ApplyCaptureState(false);
        Debug.Log("<color=red>捕获系统已禁用</color>");
    }
    
    /// <summary>
    /// 应用捕获状态到所有行星
    /// </summary>
    private void ApplyCaptureState(bool enabled)
    {
        // 查找场景中所有的 PlanetGravityCapture 组件
        PlanetGravityCapture[] captures = FindObjectsOfType<PlanetGravityCapture>();
        
        int count = 0;
        foreach (var capture in captures)
        {
            capture.enableCapture = enabled;
            count++;
        }
        
        Debug.Log($"已更新 {count} 个行星的捕获状态：{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 游戏开始时应用初始状态
    /// </summary>
    void Start()
    {
        ApplyCaptureState(captureEnabled);
    }
    
    void OnGUI()
    {
        // 显示状态提示
        string status = captureEnabled ? "启用" : "禁用";
        Color color = captureEnabled ? Color.green : Color.red;
        
        GUI.color = color;
        GUI.Label(new Rect(10, 100, 300, 30), $"捕获系统: {status} (按 {toggleKey} 切换)");
        GUI.color = Color.white;
    }
}

