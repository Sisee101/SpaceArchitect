using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 燃料管理器
/// 用于管理和显示燃料数量，挂载到FuelDisplay的Panel上
/// </summary>
public class FuelManager : MonoBehaviour
{
    [Header("燃料设置")]
    [Tooltip("当前燃料数量")]
    [SerializeField] private int fuelNum = 3;

    [Header("燃料图片")]
    [Tooltip("燃料图片数组，索引对应燃料数量(0-3)")]
    [SerializeField] private Sprite[] fuelSprites = new Sprite[4];

    [Header("显示组件")]
    [Tooltip("用于显示燃料图片的Image组件")]
    [SerializeField] private Image fuelDisplayImage;

    [Header("完成按钮")]
    [Tooltip("燃料耗尽时显示的Finish按钮")]
    [SerializeField] private GameObject finishButton;

    [Tooltip("点击Finish按钮后要打开的Panel")]
    [SerializeField] private GameObject targetPanel;

    [Header("事件")]
    [Tooltip("燃料消耗事件，可供其他按钮调用")]
    public UnityEvent onFuelCost;

    /// <summary>
    /// 获取当前燃料数量
    /// </summary>
    public int FuelNum 
    { 
        get { return fuelNum; }
        private set 
        { 
            fuelNum = Mathf.Clamp(value, 0, 3);
            UpdateFuelDisplay();
        }
    }

    private void Awake()
    {
        // 初始化事件
        if (onFuelCost == null)
        {
            onFuelCost = new UnityEvent();
        }
    }

    private void Start()
    {
        // 初始化Finish按钮为隐藏状态
        if (finishButton != null)
        {
            finishButton.SetActive(false);
        }

        // 初始化显示
        UpdateFuelDisplay();
    }

    /// <summary>
    /// 燃料消耗事件
    /// 此方法可以被其他按钮通过OnClick事件调用
    /// 注意：请不要在onFuelCost事件中再次绑定FuelCost方法，否则会造成循环调用
    /// </summary>
    public void FuelCost()
    {
        if (fuelNum > 0)
        {
            fuelNum--;
            UpdateFuelDisplay();
            Debug.Log($"燃料消耗！当前燃料: {fuelNum}");
            
            // 触发燃料消耗事件（通知其他系统燃料已被消耗）
            onFuelCost?.Invoke();
        }
        else
        {
            Debug.LogWarning("燃料已耗尽！");
        }
    }

    /// <summary>
    /// 更新燃料显示
    /// </summary>
    private void UpdateFuelDisplay()
    {
        // 检查Image组件和图片数组是否有效
        if (fuelDisplayImage == null)
        {
            Debug.LogWarning("FuelDisplayImage未分配！请在Inspector中分配Image组件");
            return;
        }

        if (fuelSprites == null || fuelSprites.Length < 4)
        {
            Debug.LogWarning("燃料图片数组未正确配置！需要4张图片(对应燃料数量0-3)");
            return;
        }

        // 确保索引在有效范围内
        int index = Mathf.Clamp(fuelNum, 0, fuelSprites.Length - 1);

        // 检查对应的图片是否存在
        if (fuelSprites[index] != null)
        {
            fuelDisplayImage.sprite = fuelSprites[index];
        }
        else
        {
            Debug.LogWarning($"燃料图片[{index}]未分配！");
        }

        // 检查燃料是否耗尽，如果是则显示Finish按钮
        CheckAndShowFinishButton();
    }

    /// <summary>
    /// 检查并显示Finish按钮
    /// </summary>
    private void CheckAndShowFinishButton()
    {
        if (finishButton != null)
        {
            // 当燃料为0时显示Finish按钮
            if (fuelNum <= 0)
            {
                finishButton.SetActive(true);
                Debug.Log("燃料已耗尽！显示Finish按钮");
            }
            else
            {
                // 燃料不为0时隐藏按钮（例如补充燃料后）
                finishButton.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 添加燃料（可选功能）
    /// </summary>
    /// <param name="amount">添加的数量</param>
    public void AddFuel(int amount)
    {
        FuelNum = Mathf.Min(fuelNum + amount, 3);
        Debug.Log($"燃料补充！当前燃料: {fuelNum}");
    }

    /// <summary>
    /// 设置燃料数量（可选功能）
    /// </summary>
    /// <param name="amount">设置的数量</param>
    public void SetFuel(int amount)
    {
        FuelNum = amount;
        Debug.Log($"燃料设置为: {fuelNum}");
    }

    /// <summary>
    /// 检查是否有足够的燃料（可选功能）
    /// </summary>
    public bool HasFuel()
    {
        return fuelNum > 0;
    }

    /// <summary>
    /// 打开目标Panel
    /// 此方法可以被Finish按钮的OnClick事件调用
    /// </summary>
    public void OpenTargetPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            Debug.Log($"已打开目标Panel: {targetPanel.name}");
        }
        else
        {
            Debug.LogWarning("目标Panel未分配！请在Inspector中分配Target Panel");
        }
    }

    /// <summary>
    /// 关闭目标Panel（可选功能）
    /// </summary>
    public void CloseTargetPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
            Debug.Log($"已关闭目标Panel: {targetPanel.name}");
        }
    }

#if UNITY_EDITOR
    // 编辑器中调试用
    [ContextMenu("测试消耗燃料")]
    private void TestFuelCost()
    {
        FuelCost();
    }

    [ContextMenu("测试补充燃料")]
    private void TestAddFuel()
    {
        AddFuel(1);
    }
#endif
}

