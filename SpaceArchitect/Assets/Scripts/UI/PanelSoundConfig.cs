using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 面板音效配置数据结构
/// 存储单个面板下所有按钮的音效配置
/// </summary>
[System.Serializable]
public class PanelSoundConfig
{
    [Tooltip("面板名称（用于显示）")]
    public string panelName;
    
    [Tooltip("面板GameObject引用（用于自动查找按钮，可选）")]
    public GameObject panelObject;
    
    [Tooltip("该面板下的所有按钮配置")]
    public List<ButtonSoundConfig> buttonConfigs = new List<ButtonSoundConfig>();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public PanelSoundConfig(string name, GameObject panel = null)
    {
        panelName = name;
        panelObject = panel;
        buttonConfigs = new List<ButtonSoundConfig>();
    }
}
