using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理主界面上已解锁技能面板的显示
/// 该面板包含4张图片，每张图片可以显示不同的技能图标
/// </summary>
public class SkillDisplay : MonoBehaviour
{
    [Header("技能显示图片组件")]
    [Tooltip("技能面板上的第1张图片")]
    public Image skillImage1;
    
    [Tooltip("技能面板上的第2张图片")]
    public Image skillImage2;
    
    [Tooltip("技能面板上的第3张图片")]
    public Image skillImage3;
    
    [Tooltip("技能面板上的第4张图片")]
    public Image skillImage4;
    
    [Tooltip("技能面板上的第5张图片")]
    public Image skillImage5;

    [Header("技能图片资源列表")]
    [Tooltip("第1张图片可能显示的所有技能图片")]
    public Sprite[] skillSprites1;
    
    [Tooltip("第2张图片可能显示的所有技能图片")]
    public Sprite[] skillSprites2;
    
    [Tooltip("第3张图片可能显示的所有技能图片")]
    public Sprite[] skillSprites3;
    
    [Tooltip("第4张图片可能显示的所有技能图片")]
    public Sprite[] skillSprites4;
    
    [Tooltip("第5张图片可能显示的所有技能图片")]
    public Sprite[] skillSprites5;

    void Start()
    {
        // 初始化显示
        UpdateSkillDisplay();
    }

    void Update()
    {
        // 每帧更新技能显示状态
        UpdateSkillDisplay();
    }

    /// <summary>
    /// 更新技能面板显示
    /// 根据SkillManager中的技能解锁状态来更新所有图片
    /// </summary>
    public void UpdateSkillDisplay()
    {
        UpdateCoreSkillDisplay();
        UpdateBoostSkillDisplay();
        UpdatePredictSkillDisplay();
        UpdateAntiSkillDisplay();
        UpdateStationSkillDisplay();
    }

    /// <summary>
    /// 更新Skill Sprite 1 - Core技能显示
    /// Core1为假时显示element0
    /// Core1为真时显示element1
    /// Core1为真且Core2为真时显示element2
    /// Core1为真且Core2为真且Core3为真时显示element3
    /// </summary>
    private void UpdateCoreSkillDisplay()
    {
        int index = 0;

        if (!SkillManager.Core1)
        {
            index = 0;
        }
        else if (SkillManager.Core1 && !SkillManager.Core2)
        {
            index = 1;
        }
        else if (SkillManager.Core1 && SkillManager.Core2 && !SkillManager.Core3)
        {
            index = 2;
        }
        else if (SkillManager.Core1 && SkillManager.Core2 && SkillManager.Core3)
        {
            index = 3;
        }

        SetSkillImage1(index);
    }

    /// <summary>
    /// 更新Skill Sprite 2 - Boost技能显示
    /// Boost1为假时显示element0
    /// Boost1为真时显示element1
    /// Boost1为真且Boost2为真时显示element2
    /// Boost1为真且Boost2为真且Boost3为真时显示element3
    /// </summary>
    private void UpdateBoostSkillDisplay()
    {
        int index = 0;

        if (!SkillManager.Boost1)
        {
            index = 0;
        }
        else if (SkillManager.Boost1 && !SkillManager.Boost2)
        {
            index = 1;
        }
        else if (SkillManager.Boost1 && SkillManager.Boost2 && !SkillManager.Boost3)
        {
            index = 2;
        }
        else if (SkillManager.Boost1 && SkillManager.Boost2 && SkillManager.Boost3)
        {
            index = 3;
        }

        SetSkillImage2(index);
    }

    /// <summary>
    /// 更新Skill Sprite 3 - Predict技能显示
    /// Predict为假时显示element0
    /// Predict为真时显示element1
    /// </summary>
    private void UpdatePredictSkillDisplay()
    {
        int index = SkillManager.Predict ? 1 : 0;
        SetSkillImage3(index);
    }

    /// <summary>
    /// 更新Skill Sprite 4 - AntiHeat和AntiCollision技能显示
    /// AntiHeat为假且AntiCollision为假时显示element0
    /// AntiHeat为真且AntiCollision为假时显示element1
    /// AntiHeat为真且AntiCollision为真时显示element2
    /// </summary>
    private void UpdateAntiSkillDisplay()
    {
        int index = 0;

        if (!SkillManager.AntiHeat && !SkillManager.AntiCollision)
        {
            index = 0;
        }
        else if (SkillManager.AntiHeat && !SkillManager.AntiCollision)
        {
            index = 1;
        }
        else if (SkillManager.AntiHeat && SkillManager.AntiCollision)
        {
            index = 2;
        }

        SetSkillImage4(index);
    }

    /// <summary>
    /// 更新Skill Sprite 5 - Station技能显示
    /// Station1为真且Station2为假且Station3为假时显示element0
    /// Station1为真且Station2为真且Station3为假时显示element1
    /// Station1为真且Station2为真且Station3为真时显示element2
    /// </summary>
    private void UpdateStationSkillDisplay()
    {
        int index = 0;

        if (SkillManager.Station1 && !SkillManager.Station2 && !SkillManager.Station3)
        {
            index = 0;
        }
        else if (SkillManager.Station1 && SkillManager.Station2 && !SkillManager.Station3)
        {
            index = 1;
        }
        else if (SkillManager.Station1 && SkillManager.Station2 && SkillManager.Station3)
        {
            index = 2;
        }

        SetSkillImage5(index);
    }

    /// <summary>
    /// 设置第1张图片显示的技能
    /// </summary>
    /// <param name="index">技能图片在列表中的索引</param>
    public void SetSkillImage1(int index)
    {
        if (skillImage1 != null && skillSprites1 != null && index >= 0 && index < skillSprites1.Length)
        {
            skillImage1.sprite = skillSprites1[index];
            skillImage1.enabled = true;
        }
        else
        {
            Debug.LogWarning($"SkillDisplay: 无法设置技能图片1，索引 {index} 无效或组件未配置");
        }
    }

    /// <summary>
    /// 设置第2张图片显示的技能
    /// </summary>
    /// <param name="index">技能图片在列表中的索引</param>
    public void SetSkillImage2(int index)
    {
        if (skillImage2 != null && skillSprites2 != null && index >= 0 && index < skillSprites2.Length)
        {
            skillImage2.sprite = skillSprites2[index];
            skillImage2.enabled = true;
        }
        else
        {
            Debug.LogWarning($"SkillDisplay: 无法设置技能图片2，索引 {index} 无效或组件未配置");
        }
    }

    /// <summary>
    /// 设置第3张图片显示的技能
    /// </summary>
    /// <param name="index">技能图片在列表中的索引</param>
    public void SetSkillImage3(int index)
    {
        if (skillImage3 != null && skillSprites3 != null && index >= 0 && index < skillSprites3.Length)
        {
            skillImage3.sprite = skillSprites3[index];
            skillImage3.enabled = true;
        }
        else
        {
            Debug.LogWarning($"SkillDisplay: 无法设置技能图片3，索引 {index} 无效或组件未配置");
        }
    }

    /// <summary>
    /// 设置第4张图片显示的技能
    /// </summary>
    /// <param name="index">技能图片在列表中的索引</param>
    public void SetSkillImage4(int index)
    {
        if (skillImage4 != null && skillSprites4 != null && index >= 0 && index < skillSprites4.Length)
        {
            skillImage4.sprite = skillSprites4[index];
            skillImage4.enabled = true;
        }
        else
        {
            Debug.LogWarning($"SkillDisplay: 无法设置技能图片4，索引 {index} 无效或组件未配置");
        }
    }

    /// <summary>
    /// 设置第5张图片显示的技能
    /// </summary>
    /// <param name="index">技能图片在列表中的索引</param>
    public void SetSkillImage5(int index)
    {
        if (skillImage5 != null && skillSprites5 != null && index >= 0 && index < skillSprites5.Length)
        {
            skillImage5.sprite = skillSprites5[index];
            skillImage5.enabled = true;
        }
        else
        {
            Debug.LogWarning($"SkillDisplay: 无法设置技能图片5，索引 {index} 无效或组件未配置");
        }
    }

    /// <summary>
    /// 隐藏指定的技能图片
    /// </summary>
    /// <param name="imageNumber">图片编号（1-5）</param>
    public void HideSkillImage(int imageNumber)
    {
        switch (imageNumber)
        {
            case 1:
                if (skillImage1 != null) skillImage1.enabled = false;
                break;
            case 2:
                if (skillImage2 != null) skillImage2.enabled = false;
                break;
            case 3:
                if (skillImage3 != null) skillImage3.enabled = false;
                break;
            case 4:
                if (skillImage4 != null) skillImage4.enabled = false;
                break;
            case 5:
                if (skillImage5 != null) skillImage5.enabled = false;
                break;
            default:
                Debug.LogWarning($"SkillDisplay: 无效的图片编号 {imageNumber}，应为1-5");
                break;
        }
    }

    /// <summary>
    /// 显示指定的技能图片
    /// </summary>
    /// <param name="imageNumber">图片编号（1-5）</param>
    public void ShowSkillImage(int imageNumber)
    {
        switch (imageNumber)
        {
            case 1:
                if (skillImage1 != null) skillImage1.enabled = true;
                break;
            case 2:
                if (skillImage2 != null) skillImage2.enabled = true;
                break;
            case 3:
                if (skillImage3 != null) skillImage3.enabled = true;
                break;
            case 4:
                if (skillImage4 != null) skillImage4.enabled = true;
                break;
            case 5:
                if (skillImage5 != null) skillImage5.enabled = true;
                break;
            default:
                Debug.LogWarning($"SkillDisplay: 无效的图片编号 {imageNumber}，应为1-5");
                break;
        }
    }
}

