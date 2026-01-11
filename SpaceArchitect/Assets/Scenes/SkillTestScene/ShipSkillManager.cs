using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillData
{
    [Tooltip("技能名称（用于标识）")]
    public string skillName;
    
    [Tooltip("包含技能脚本的GameObject（通常是ship GameObject）")]
    public GameObject skillGameObject;
    
    [Tooltip("技能脚本的类型名称（脚本的类名，例如：Boost1, Core1等）")]
    public string scriptTypeName;
}

public class ShipSkillManager : MonoBehaviour
{
    [Header("技能列表")]
    [Tooltip("配置每个技能对应的GameObject和脚本类型名称")]
    public List<SkillData> skillList = new List<SkillData>
    {
        new SkillData { skillName = "Boost1", scriptTypeName = "Boost1" },
        new SkillData { skillName = "Core1", scriptTypeName = "Core1" },
        new SkillData { skillName = "Boost2", scriptTypeName = "Boost2" },
        new SkillData { skillName = "Core2", scriptTypeName = "Core2" },
        new SkillData { skillName = "AntiHeat", scriptTypeName = "AntiHeat" },
        new SkillData { skillName = "Predict", scriptTypeName = "Predict" },
        new SkillData { skillName = "Boost3", scriptTypeName = "Boost3" },
        new SkillData { skillName = "Core3", scriptTypeName = "Core3" },
        new SkillData { skillName = "AntiCollision", scriptTypeName = "AntiCollision" }
    };

    void Awake()
    {
        // 在Awake中执行，确保在其他脚本Start之前完成
        UpdateSkills();
    }

    void OnEnable()
    {
        // 对象激活时更新技能状态（场景加载时也会调用）
        UpdateSkills();
    }

    void Start()
    {
        // 在Start中再次执行，确保在所有初始化完成后执行
        UpdateSkills();
    }

    /// <summary>
    /// 根据SkillManager中的变量值更新技能脚本的启用状态
    /// </summary>
    private void UpdateSkills()
    {
        Debug.Log("ShipSkillManager: UpdateSkills() 被调用");
        
        foreach (SkillData skill in skillList)
        {
            if (skill.skillGameObject == null)
            {
                Debug.LogWarning($"ShipSkillManager: {skill.skillName} 的GameObject引用为空，请检查配置！");
                continue;
            }

            if (string.IsNullOrEmpty(skill.scriptTypeName))
            {
                Debug.LogWarning($"ShipSkillManager: {skill.skillName} 的脚本类型名称为空，请检查配置！");
                continue;
            }

            // 通过类型名称查找对应的脚本组件
            MonoBehaviour skillScript = GetSkillComponent(skill.skillGameObject, skill.scriptTypeName);
            
            if (skillScript == null)
            {
                Debug.LogWarning($"ShipSkillManager: 在 {skill.skillGameObject.name} 上找不到脚本类型 {skill.scriptTypeName}！");
                continue;
            }

            // 根据技能名称获取对应的静态变量值
            bool shouldEnable = GetSkillStatus(skill.skillName);
            
            // 调试信息
            Debug.Log($"ShipSkillManager: 检查技能 {skill.skillName}，状态 = {shouldEnable}，脚本类型 = {skillScript.GetType().Name}");
            
            // 启用或禁用脚本
            skillScript.enabled = shouldEnable;
            
            if (shouldEnable)
            {
                Debug.Log($"ShipSkillManager: 启用技能 {skill.skillName} (脚本: {skillScript.GetType().Name})");
            }
        }
    }

    /// <summary>
    /// 通过类型名称在GameObject上查找对应的脚本组件
    /// </summary>
    private MonoBehaviour GetSkillComponent(GameObject obj, string typeName)
    {
        // 获取所有MonoBehaviour组件
        MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
        
        Debug.Log($"ShipSkillManager: 在 {obj.name} 上查找脚本类型 {typeName}，找到 {components.Length} 个MonoBehaviour组件");
        
        foreach (MonoBehaviour component in components)
        {
            if (component != null)
            {
                string componentTypeName = component.GetType().Name;
                Debug.Log($"ShipSkillManager: 检查组件类型: {componentTypeName}，是否匹配 {typeName}? {componentTypeName == typeName}");
                
                if (componentTypeName == typeName)
                {
                    Debug.Log($"ShipSkillManager: 找到匹配的脚本组件: {componentTypeName}");
                    return component;
                }
            }
        }
        
        Debug.LogWarning($"ShipSkillManager: 在 {obj.name} 上未找到类型名为 {typeName} 的脚本组件！");
        return null;
    }

    /// <summary>
    /// 根据技能名称获取SkillManager中对应的静态变量值
    /// </summary>
    private bool GetSkillStatus(string skillName)
    {
        switch (skillName)
        {
            case "Boost1":
                return SkillManager.Boost1;
            case "Core1":
                return SkillManager.Core1;
            case "Boost2":
                return SkillManager.Boost2;
            case "Core2":
                return SkillManager.Core2;
            case "AntiHeat":
                return SkillManager.AntiHeat;
            case "Predict":
                return SkillManager.Predict;
            case "Boost3":
                return SkillManager.Boost3;
            case "Core3":
                return SkillManager.Core3;
            case "AntiCollision":
                return SkillManager.AntiCollision;
            default:
                Debug.LogWarning($"ShipSkillManager: 未知的技能名称 {skillName}");
                return false;
        }
    }
}
