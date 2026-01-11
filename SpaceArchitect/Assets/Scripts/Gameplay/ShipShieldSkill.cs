using UnityEngine;

/// <summary>
/// 飞船防撞技能脚本
/// 点击C键触发防撞技能，飞船可以穿越小行星带
/// </summary>
public class ShipShieldSkill : MonoBehaviour
{
    [Header("技能设置")]
    [Tooltip("触发技能按键（默认C键）")]
    [SerializeField] private KeyCode shieldKey = KeyCode.C;

    [Tooltip("技能持续时间（秒）")]
    [SerializeField] private float shieldDuration = 5f;

    [Tooltip("技能冷却时间（秒）")]
    [SerializeField] private float cooldownDuration = 10f;

    [Header("保护罩特效设置")]
    [Tooltip("保护罩特效GameObject（可选，激活时显示）")]
    [SerializeField] private GameObject shieldVFX;

    [Tooltip("保护罩特效显示位置（相对于飞船，如果为空则使用飞船位置）")]
    [SerializeField] private Transform shieldVFXParent;

    [Header("小行星带设置")]
    [Tooltip("小行星带的Tag（飞船可以穿越的对象）")]
    [SerializeField] private string asteroidBeltTag = "Obstacles";

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugLog = true;

    private bool isShieldActive = false; // 技能是否激活
    private bool isOnCooldown = false; // 是否在冷却中
    private float shieldTimer = 0f; // 技能持续时间计时器
    private float cooldownTimer = 0f; // 冷却时间计时器
    private GameObject shieldVFXInstance; // 保护罩特效实例

    /// <summary>
    /// 获取技能是否激活
    /// </summary>
    public bool IsShieldActive => isShieldActive;

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
        if (isShieldActive)
        {
            shieldTimer -= Time.unscaledDeltaTime;
            if (shieldTimer <= 0f)
            {
                DeactivateShield();
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
                    Debug.Log("ShipShieldSkill: 技能冷却完成，可以再次使用");
                }
            }
        }

        // 检查按键输入
        if (Input.GetKeyDown(shieldKey))
        {
            TryActivateShield();
        }
    }

    /// <summary>
    /// 尝试激活防撞技能
    /// </summary>
    private void TryActivateShield()
    {
        // 如果技能已经激活，不重复激活
        if (isShieldActive)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipShieldSkill: 技能已经在激活状态");
            }
            return;
        }

        // 如果技能在冷却中，不能使用
        if (isOnCooldown)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"ShipShieldSkill: 技能还在冷却中，剩余时间: {cooldownTimer:F2}秒");
            }
            return;
        }

        // 激活技能
        ActivateShield();
    }

    /// <summary>
    /// 激活防撞技能
    /// </summary>
    private void ActivateShield()
    {
        isShieldActive = true;
        shieldTimer = shieldDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipShieldSkill: 防撞技能已激活，持续时间: {shieldDuration}秒");
        }

        // 显示保护罩特效
        ShowShieldVFX();

        // 触发技能激活事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 停用防撞技能
    /// </summary>
    private void DeactivateShield()
    {
        if (!isShieldActive) return;

        isShieldActive = false;
        shieldTimer = 0f;

        // 开始冷却
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;

        if (showDebugLog)
        {
            Debug.Log($"ShipShieldSkill: 防撞技能已停用，开始冷却，冷却时间: {cooldownDuration}秒");
        }

        // 隐藏保护罩特效
        HideShieldVFX();

        // 触发技能停用事件（可选）
        if (EventManager.Instance != null)
        {
            // 如果有相关事件，可以在这里触发
        }
    }

    /// <summary>
    /// 显示保护罩特效
    /// </summary>
    private void ShowShieldVFX()
    {
        if (shieldVFX == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ShipShieldSkill: 未设置保护罩特效（Shield VFX），特效将不显示");
            }
            return;
        }

        // 如果特效实例已存在，先销毁
        if (shieldVFXInstance != null)
        {
            Destroy(shieldVFXInstance);
        }

        // 确定特效父对象位置
        Transform parentTransform = shieldVFXParent != null ? shieldVFXParent : transform;
        
        // 实例化保护罩特效
        shieldVFXInstance = Instantiate(shieldVFX, parentTransform.position, parentTransform.rotation);
        
        // 设置父对象（如果需要跟随飞船）
        if (shieldVFXParent != null)
        {
            shieldVFXInstance.transform.SetParent(shieldVFXParent);
        }
        else
        {
            shieldVFXInstance.transform.SetParent(transform);
        }
        
        shieldVFXInstance.transform.localPosition = Vector3.zero;
        shieldVFXInstance.transform.localRotation = Quaternion.identity;

        // 激活特效
        shieldVFXInstance.SetActive(true);

        if (showDebugLog)
        {
            Debug.Log("ShipShieldSkill: 保护罩特效已显示");
        }
    }

    /// <summary>
    /// 隐藏保护罩特效
    /// </summary>
    private void HideShieldVFX()
    {
        if (shieldVFXInstance != null)
        {
            Destroy(shieldVFXInstance);
            shieldVFXInstance = null;

            if (showDebugLog)
            {
                Debug.Log("ShipShieldSkill: 保护罩特效已隐藏");
            }
        }
    }

    /// <summary>
    /// 检查是否可以忽略碰撞（用于Crash脚本调用）
    /// </summary>
    /// <param name="collisionTag">碰撞对象的Tag</param>
    /// <returns>如果可以忽略碰撞返回true，否则返回false</returns>
    public bool CanIgnoreCollision(string collisionTag)
    {
        // 只有当技能激活且碰撞对象是小行星带时，才能忽略碰撞
        if (isShieldActive && !string.IsNullOrEmpty(collisionTag))
        {
            if (collisionTag == asteroidBeltTag)
            {
                if (showDebugLog)
                {
                    Debug.Log($"ShipShieldSkill: 防撞技能激活，忽略小行星带碰撞（Tag: {collisionTag}）");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 手动激活技能（用于外部调用，如UI按钮）
    /// </summary>
    public void ActivateShieldManually()
    {
        TryActivateShield();
    }

    /// <summary>
    /// 手动停用技能（用于外部调用）
    /// </summary>
    public void DeactivateShieldManually()
    {
        DeactivateShield();
    }

    void OnDisable()
    {
        // 禁用时隐藏特效
        HideShieldVFX();
    }

    void OnDestroy()
    {
        // 销毁时清理特效
        HideShieldVFX();
    }
}

