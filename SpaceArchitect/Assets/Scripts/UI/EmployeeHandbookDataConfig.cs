using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 员工手册数据配置（ScriptableObject）
/// 用于配置每个选项对应的图片列表
/// </summary>
[CreateAssetMenu(fileName = "EmployeeHandbookData", menuName = "Game Data/EmployeeHandbookData")]
public class EmployeeHandbookDataConfig : ScriptableObject
{
    [System.Serializable]
    public class HandbookSection
    {
        [Tooltip("选项名称（如：入职指南、业务流程、系统架构）")]
        public string sectionName;
        
        [Tooltip("该选项对应的图片列表（按顺序排列）")]
        public List<Sprite> images = new List<Sprite>();
    }
    
    [Header("员工手册选项配置")]
    [Tooltip("每个选项的配置（至少需要3个选项：入职指南、业务流程、系统架构）")]
    public List<HandbookSection> sections = new List<HandbookSection>();
    
    /// <summary>
    /// 获取指定索引的选项配置
    /// </summary>
    public HandbookSection GetSection(int index)
    {
        if (index >= 0 && index < sections.Count)
        {
            return sections[index];
        }
        return null;
    }
    
    /// <summary>
    /// 获取指定选项的图片列表
    /// </summary>
    public List<Sprite> GetSectionImages(int index)
    {
        HandbookSection section = GetSection(index);
        if (section != null)
        {
            return section.images;
        }
        return new List<Sprite>();
    }
}
