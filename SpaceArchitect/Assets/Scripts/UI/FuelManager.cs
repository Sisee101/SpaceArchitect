using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 燃料管理器
/// 用于管理和显示燃料数量，挂载到FuelDisplay的Panel上
/// 燃料数据会在场景切换时保持
/// </summary>
public class FuelManager : MonoBehaviour
{
    // PlayerPrefs 保存键名
    private const string FUEL_SAVE_KEY = "PlayerFuelNum";
    private const int DEFAULT_FUEL = 3;

    [Header("燃料设置")]
    [Tooltip("当前燃料数量（初始默认值，实际值会从存档加载）")]
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

    [Header("燃料消耗按钮控制")]
    [Tooltip("所有绑定了FuelCost()的按钮，燃料为0时会自动禁用")]
    [SerializeField] private Button[] fuelCostButtons;

    [Header("事件")]
    [Tooltip("燃料消耗事件，可供其他按钮调用")]
    public UnityEvent onFuelCost;

    [Tooltip("燃料重置事件，当燃料被重置为满时触发")]
    public UnityEvent onFuelReset;

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

        if (onFuelReset == null)
        {
            onFuelReset = new UnityEvent();
        }

        // 从存档加载燃料数量
        LoadFuelData();
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
            SaveFuelData(); // 保存燃料数据
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

        // 更新燃料消耗按钮的可用状态
        UpdateFuelCostButtonsState();
    }

    /// <summary>
    /// 更新燃料消耗按钮的可用状态
    /// </summary>
    private void UpdateFuelCostButtonsState()
    {
        if (fuelCostButtons == null || fuelCostButtons.Length == 0)
        {
            return;
        }

        bool hasEnoughFuel = fuelNum > 0;

        foreach (Button button in fuelCostButtons)
        {
            if (button != null)
            {
                button.interactable = hasEnoughFuel;
            }
        }

        if (!hasEnoughFuel)
        {
            Debug.Log("燃料耗尽！已禁用所有燃料消耗按钮");
        }
    }

    /// <summary>
    /// 添加燃料（可选功能）
    /// </summary>
    /// <param name="amount">添加的数量</param>
    public void AddFuel(int amount)
    {
        FuelNum = Mathf.Min(fuelNum + amount, 3);
        SaveFuelData(); // 保存燃料数据
        Debug.Log($"燃料补充！当前燃料: {fuelNum}");
    }

    /// <summary>
    /// 设置燃料数量（可选功能）
    /// </summary>
    /// <param name="amount">设置的数量</param>
    public void SetFuel(int amount)
    {
        FuelNum = amount;
        SaveFuelData(); // 保存燃料数据
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

    /// <summary>
    /// 保存燃料数据到PlayerPrefs
    /// </summary>
    private void SaveFuelData()
    {
        PlayerPrefs.SetInt(FUEL_SAVE_KEY, fuelNum);
        PlayerPrefs.Save(); // 立即写入磁盘
        Debug.Log($"燃料数据已保存: {fuelNum}");
    }

    /// <summary>
    /// 从PlayerPrefs加载燃料数据
    /// </summary>
    private void LoadFuelData()
    {
        if (PlayerPrefs.HasKey(FUEL_SAVE_KEY))
        {
            fuelNum = PlayerPrefs.GetInt(FUEL_SAVE_KEY, DEFAULT_FUEL);
            Debug.Log($"燃料数据已加载: {fuelNum}");
        }
        else
        {
            // 首次运行，使用默认值
            fuelNum = DEFAULT_FUEL;
            SaveFuelData(); // 保存初始值
            Debug.Log($"首次运行，使用默认燃料值: {fuelNum}");
        }
    }

    /// <summary>
    /// 重置燃料为满（3）
    /// 此方法可以被按钮的OnClick事件调用
    /// </summary>
    public void ResetFuel()
    {
        fuelNum = DEFAULT_FUEL;
        SaveFuelData();
        UpdateFuelDisplay();
        Debug.Log($"燃料已重置为满！当前燃料: {fuelNum}");
        
        // 触发燃料重置事件（通知其他系统燃料已被重置）
        onFuelReset?.Invoke();
    }

    /// <summary>
    /// 重置燃料数据为默认值（与ResetFuel功能相同）
    /// </summary>
    public void ResetFuelData()
    {
        ResetFuel();
    }

    /// <summary>
    /// 清除保存的燃料数据
    /// </summary>
    public void ClearSavedData()
    {
        if (PlayerPrefs.HasKey(FUEL_SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(FUEL_SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("已清除保存的燃料数据");
        }
    }

#if UNITY_EDITOR
    // 编辑器中调试用
    [ContextMenu("测试/消耗燃料")]
    private void TestFuelCost()
    {
        FuelCost();
    }

    [ContextMenu("测试/补充燃料")]
    private void TestAddFuel()
    {
        AddFuel(1);
    }

    [ContextMenu("测试/重置燃料为默认值")]
    private void TestResetFuel()
    {
        ResetFuelData();
    }

    [ContextMenu("测试/清除存档数据")]
    private void TestClearSaveData()
    {
        ClearSavedData();
        Debug.Log("存档已清除，下次运行将使用默认值");
    }

    [ContextMenu("测试/查看当前燃料值")]
    private void TestViewFuelValue()
    {
        Debug.Log($"当前燃料值: {fuelNum}");
        Debug.Log($"存档中的燃料值: {PlayerPrefs.GetInt(FUEL_SAVE_KEY, -1)}");
    }

    [ContextMenu("测试/查看按钮状态")]
    private void TestViewButtonsState()
    {
        if (fuelCostButtons == null || fuelCostButtons.Length == 0)
        {
            Debug.LogWarning("未配置燃料消耗按钮数组");
            return;
        }

        Debug.Log($"燃料消耗按钮数量: {fuelCostButtons.Length}");
        for (int i = 0; i < fuelCostButtons.Length; i++)
        {
            if (fuelCostButtons[i] != null)
            {
                Debug.Log($"按钮 [{i}] {fuelCostButtons[i].name}: {(fuelCostButtons[i].interactable ? "可用" : "禁用")}");
            }
            else
            {
                Debug.LogWarning($"按钮 [{i}] 为空");
            }
        }
    }

    [ContextMenu("测试/强制更新按钮状态")]
    private void TestForceUpdateButtons()
    {
        UpdateFuelCostButtonsState();
        Debug.Log("已强制更新按钮状态");
    }
#endif
}

