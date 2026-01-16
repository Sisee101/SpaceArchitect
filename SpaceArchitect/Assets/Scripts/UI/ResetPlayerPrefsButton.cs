using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重置PlayerPrefs数据按钮
/// 点击按钮后清除所有PlayerPrefs存储的数据
/// </summary>
public class ResetPlayerPrefsButton : MonoBehaviour
{
    [Header("按钮设置")]
    [Tooltip("是否在Start时自动绑定按钮点击事件（如果为false，需要在Inspector中手动绑定OnClick事件到ResetPlayerPrefs方法）")]
    [SerializeField] private bool autoBindOnStart = true;
    
    [Tooltip("重置后是否立即保存（推荐保持为true）")]
    [SerializeField] private bool saveAfterReset = true;
    
    private Button button;
    
    void Start()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 如果启用自动绑定且按钮存在，自动绑定点击事件
        if (autoBindOnStart && button != null)
        {
            button.onClick.AddListener(ResetPlayerPrefs);
        }
        else if (button == null)
        {
            Debug.LogWarning("ResetPlayerPrefsButton: 未找到Button组件，请确保此脚本挂载在带有Button组件的GameObject上。");
        }
    }
    
    /// <summary>
    /// 重置所有PlayerPrefs数据
    /// 此方法可以手动在Inspector的Button OnClick事件中绑定
    /// </summary>
    public void ResetPlayerPrefs()
    {
        // 清除所有PlayerPrefs数据
        PlayerPrefs.DeleteAll();
        
        // 如果启用了保存选项，立即保存
        if (saveAfterReset)
        {
            PlayerPrefs.Save();
        }
        
        Debug.Log("ResetPlayerPrefsButton: 已清除所有PlayerPrefs数据" + (saveAfterReset ? "并已保存" : ""));
    }
    
    void OnDestroy()
    {
        // 清理事件监听，防止内存泄漏
        if (button != null && autoBindOnStart)
        {
            button.onClick.RemoveListener(ResetPlayerPrefs);
        }
    }
}