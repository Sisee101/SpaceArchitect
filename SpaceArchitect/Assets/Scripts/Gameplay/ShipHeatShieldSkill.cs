using UnityEngine;

/// <summary>
/// 飞船隔热技能脚本
/// 点击H键触发隔热技能，飞船可以进入火行星的加热范围而不被过热
/// </summary>
public class ShipHeatShieldSkill : MonoBehaviour
{
    [Header("技能设置")]
    [Tooltip("触发技能按键（默认H键）")]
    [SerializeField] private KeyCode heatShieldKey = KeyCode.H;

    [Tooltip("技能持续时间（秒）")]
    [SerializeField] private float shieldDuration = 5f;

    [Tooltip("技能冷却时间（秒）")]
    [SerializeField] private float cooldownDuration = 10f;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private bool isHeatShieldActive = false; // 技能是否激活
    private bool isOnCooldown = false; // 是否在冷却中
    private float shieldTimer = 0f; // 技能持续时间计时器
    private float cooldownTimer = 0f; // 冷却时间计时器

    /// <summary>
    /// 获取技能是否激活
    /// </summary>
    public bool IsHeatShieldActive => isHeatShieldActive;

    /// <summary>
    /// 获取是否在冷却中
    /// </summary>
    public bool IsOnCooldown => isOnCooldown;

    /// <summary>
    /// 获取冷却剩余时间
    /// </summary>
    public float CooldownRemainingTime => cooldownTimer;

    void Update()
    {
        // 更新技能持续时间（使用未缩放时间，确保时停时也能正常计时）
        if (isHeatShieldActive)
        {
            shieldTimer -= Time.unscaledDeltaTime;
            if (shieldTimer <= 0f)
            {
                DeactivateHeatShield();
            }
        }

        // 更新冷却时间（使用未缩放时间）
        if (isOnCooldown)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
                if (showDebugLog)
                {
                    Debug.Log("ShipHeatShieldSkill: 技能冷却完成，可以再次使用");
                }
            }
        }

        // 检查按键输入
        if (Input.GetKeyDown(heatShieldKey))
        {
            TryActivateHeatShield();
        }
    }

    /// <summary>
    /// 尝试激活隔热技能
    /// </summary>
    private void TryActivateHeatShield()
    {
        // 如果技能已经激活，不重复激活
        if (isHeatShieldActive)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipHeatShieldSkill: 技能已经在激活状态");
            }
            return;
        }

        // 如果技能在冷却中，不能使用
        if (isOnCooldown)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipHeatShieldSkill: 技能还在冷却中，剩余时间: {cooldownTimer:F2}秒");
            }
            return;
        }

        // 激活技能
        ActivateHeatShield();
    }

    /// <summary>
    /// 激活隔热技能
    /// </summary>
    private void ActivateHeatShield()
    {
        isHeatShieldActive = true;
        shieldTimer = shieldDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipHeatShieldSkill: 隔热技能已激活，持续时间: {shieldDuration}秒");
        }

        // 触发技能激活事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 停用隔热技能
    /// </summary>
    private void DeactivateHeatShield()
    {
        if (!isHeatShieldActive) return;

        isHeatShieldActive = false;
        shieldTimer = 0f;

        // 开始冷却
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipHeatShieldSkill: 隔热技能已停用，开始冷却，冷却时间: {cooldownDuration}秒");
        }

        // 触发技能停用事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 手动激活技能（用于外部调用，如UI按钮）
    /// </summary>
    public void ActivateHeatShieldManually()
    {
        TryActivateHeatShield();
    }

    /// <summary>
    /// 手动停用技能（用于外部调用）
    /// </summary>
    public void DeactivateHeatShieldManually()
    {
        DeactivateHeatShield();
    }
}











