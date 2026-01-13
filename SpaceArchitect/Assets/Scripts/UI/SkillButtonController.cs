using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能按钮控制器
/// 管理技能按钮的图片切换，根据技能解锁状态自动更新按钮外观
/// </summary>
public class SkillButtonController : MonoBehaviour
{
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        // Station 解锁
        Station1,
        Station2,
        Station3,
        
        // Station1 技能
        Boost1,
        Core1,
        
        // Station2 技能
        Boost2,
        Core2,
        AntiHeat,
        Predict,
        
        // Station3 技能
        Boost3,
        Core3,
        AntiCollision
    }
    
    [Header("技能类型")]
    [Tooltip("选择此按钮对应的技能类型")]
    [SerializeField] private SkillType skillType = SkillType.Boost1;
    
    /// <summary>
    /// 获取技能类型（供外部访问）
    /// </summary>
    public SkillType CurrentSkillType => skillType;
    
    [Header("图片资源")]
    [Tooltip("未解锁状态的按钮图片（锁定/灰色版本）")]
    [SerializeField] private Sprite lockedSprite;
    
    [Tooltip("已解锁状态的按钮图片（激活/彩色版本）")]
    [SerializeField] private Sprite unlockedSprite;
    
    [Header("组件引用（自动获取）")]
    [Tooltip("按钮的Image组件（如果为空，会自动获取）")]
    [SerializeField] private Image buttonImage;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    [SerializeField] private bool enableDebugLog = false;
    
    private bool lastKnownState = false; // 上次已知的解锁状态
    
    void Awake()
    {
        // 自动获取Image组件
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
            if (buttonImage == null)
            {
                // 尝试从子对象获取
                buttonImage = GetComponentInChildren<Image>();
            }
        }
        
        if (buttonImage == null)
        {
            Debug.LogError($"SkillButtonController: {gameObject.name} 未找到Image组件！请确保按钮有Image组件。");
        }
    }
    
    void Start()
    {
        // 初始化时根据当前解锁状态设置图片
        UpdateButtonImage();
    }
    
    void OnEnable()
    {
        // 每次激活时更新图片（防止状态变化后重新激活时显示错误）
        UpdateButtonImage();
    }
    
    /// <summary>
    /// 更新按钮图片（根据技能解锁状态）
    /// </summary>
    public void UpdateButtonImage()
    {
        if (buttonImage == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"SkillButtonController: {gameObject.name} 的buttonImage为空，无法更新图片。");
            }
            return;
        }
        
        // 获取当前技能解锁状态
        bool isUnlocked = GetSkillUnlockState();
        
        // 如果状态没有变化，不需要更新
        if (isUnlocked == lastKnownState && lastKnownState != false)
        {
            return;
        }
        
        // 根据解锁状态切换图片
        if (isUnlocked)
        {
            if (unlockedSprite != null)
            {
                buttonImage.sprite = unlockedSprite;
                if (enableDebugLog)
                {
                    Debug.Log($"SkillButtonController: {gameObject.name} ({skillType}) 已解锁，切换到已解锁图片。");
                }
            }
            else
            {
                Debug.LogWarning($"SkillButtonController: {gameObject.name} ({skillType}) 的unlockedSprite未配置！");
            }
        }
        else
        {
            if (lockedSprite != null)
            {
                buttonImage.sprite = lockedSprite;
                if (enableDebugLog)
                {
                    Debug.Log($"SkillButtonController: {gameObject.name} ({skillType}) 未解锁，使用锁定图片。");
                }
            }
            else
            {
                Debug.LogWarning($"SkillButtonController: {gameObject.name} ({skillType}) 的lockedSprite未配置！");
            }
        }
        
        // 更新已知状态
        lastKnownState = isUnlocked;
    }
    
    /// <summary>
    /// 根据技能类型获取解锁状态
    /// </summary>
    private bool GetSkillUnlockState()
    {
        switch (skillType)
        {
            case SkillType.Station1:
                return SkillManager.Station1;
            case SkillType.Station2:
                return SkillManager.Station2;
            case SkillType.Station3:
                return SkillManager.Station3;
            case SkillType.Boost1:
                return SkillManager.Boost1;
            case SkillType.Core1:
                return SkillManager.Core1;
            case SkillType.Boost2:
                return SkillManager.Boost2;
            case SkillType.Core2:
                return SkillManager.Core2;
            case SkillType.AntiHeat:
                return SkillManager.AntiHeat;
            case SkillType.Predict:
                return SkillManager.Predict;
            case SkillType.Boost3:
                return SkillManager.Boost3;
            case SkillType.Core3:
                return SkillManager.Core3;
            case SkillType.AntiCollision:
                return SkillManager.AntiCollision;
            default:
                Debug.LogWarning($"SkillButtonController: 未知的技能类型: {skillType}");
                return false;
        }
    }
    
    /// <summary>
    /// 手动刷新按钮图片（供外部调用）
    /// </summary>
    public void Refresh()
    {
        lastKnownState = false; // 重置状态，强制更新
        UpdateButtonImage();
    }
}
