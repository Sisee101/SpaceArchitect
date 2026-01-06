using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("动画设置")]
    [Tooltip("动画触发器名称")]
    public string triggerName = "Boost";

    [Header("输入设置")]
    [Tooltip("触发动画的按键")]
    public KeyCode triggerKey = KeyCode.B;

    [Tooltip("触发冷却时间（秒）")]
    public float cooldown = 0.5f;

    [Header("状态")]
    [Tooltip("是否在冷却中")]
    public bool isOnCooldown = false;

    private float lastTriggerTime = 0f;

    void Start()
    {
        // 获取当前游戏对象上的Animator组件
        animator = GetComponent<Animator>();

        // 如果没有找到Animator组件，可以尝试在子对象中查找
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 如果还是没有找到，输出警告
        if (animator == null)
        {
            Debug.LogWarning("Animator组件未找到！请确保游戏对象上有Animator组件。");
        }
    }

    void Update()
    {
        // 更新冷却状态
        isOnCooldown = Time.time - lastTriggerTime < cooldown;

        // 检测B键按下且不在冷却中
        if (Input.GetKeyDown(triggerKey) && !isOnCooldown)
        {
            TriggerBoostAnimation();
        }
    }

    void TriggerBoostAnimation()
    {
        // 检查animator是否存在
        if (animator != null)
        {
            // 触发名为"Boost"的trigger
            animator.SetTrigger(triggerName);
            lastTriggerTime = Time.time;
            Debug.Log($"B键已触发 {triggerName} 动画");
        }
        else
        {
            Debug.LogWarning("Animator为null，无法触发动画");
        }
    }

    // 可选：添加一个公共方法，方便其他脚本调用
    public void PlayBoostAnimation()
    {
        if (!isOnCooldown)
        {
            TriggerBoostAnimation();
        }
    }

    // 获取剩余冷却时间
    public float GetRemainingCooldown()
    {
        float elapsed = Time.time - lastTriggerTime;
        return Mathf.Max(0, cooldown - elapsed);
    }

    // 检查是否可以触发动画
    public bool CanTriggerAnimation()
    {
        return !isOnCooldown && animator != null;
    }

    // 强制触发动画（忽略冷却）
    public void ForceTriggerBoostAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"强制触发 {triggerName} 动画");
        }
    }
}