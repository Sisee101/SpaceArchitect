using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按钮音效配置数据结构
/// 存储单个按钮的音效配置信息
/// </summary>
[System.Serializable]
public class ButtonSoundConfig
{
    [Tooltip("按钮名称（用于显示，自动填充）")]
    public string buttonName;
    
    [Tooltip("按钮引用（自动查找或手动拖拽）")]
    public Button button;
    
    [Tooltip("点击音效（如果为空，使用默认音效）")]
    public AudioClip clickSound;
    
    [Tooltip("悬停音效（如果为空，使用默认音效，可选）")]
    public AudioClip hoverSound;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ButtonSoundConfig(string name, Button btn)
    {
        buttonName = name;
        button = btn;
    }
    
    /// <summary>
    /// 默认构造函数（用于序列化）
    /// </summary>
    public ButtonSoundConfig()
    {
        buttonName = "";
    }
}
