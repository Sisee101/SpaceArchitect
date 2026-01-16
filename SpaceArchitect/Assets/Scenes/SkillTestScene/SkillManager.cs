using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 技能类型枚举
public enum SkillType
{
    Station1,
    Station2,
    Station3,
    Boost1,
    Core1,
    Boost2,
    Core2,
    AntiHeat,
    Predict,
    Boost3,
    Core3,
    AntiCollision
}

public class SkillManager : MonoBehaviour
{
    // Tip Panel 相关引用
    [Header("Tip Panel 设置")]
    public GameObject tipPanel;  // Tip Panel GameObject
    public Text tipText;  // Tip Panel 上的文字提示
    public Button backButton;  // Back 按钮
    
    // Tip1 Panel 相关引用（成功解锁提示）
    [Header("Tip1 Panel 设置")]
    public GameObject tip1Panel;  // Tip1 Panel GameObject
    public TextMeshProUGUI tip1Text;  // Tip1 Panel 上的文字提示
    public Button confirmButton;  // Confirm 按钮
    
    // Information Panel 相关引用
    [Header("Information Panel 设置")]
    public GameObject informationPanel;  // Information Panel GameObject
    public Image informationImage;  // 显示技能介绍图片的Image组件
    public Button confirmUpgradeButton;  // 确认升级按钮
    
    // 技能介绍图片数组（按顺序：Station1, Station2, Station3, Boost1, Core1, Boost2, Core2, AntiHeat, Predict, Boost3, Core3, AntiCollision）
    [Header("技能介绍图片")]
    [Tooltip("技能介绍图片数组，按顺序：[0]Station1, [1]Station2, [2]Station3, [3]Boost1, [4]Core1, [5]Boost2, [6]Core2, [7]AntiHeat, [8]Predict, [9]Boost3, [10]Core3, [11]AntiCollision")]
    public Sprite[] skillInfoImages = new Sprite[12];
    
    // 当前要解锁的技能类型
    private SkillType currentSkillType;
    // Station 解锁状态
    public static bool Station1 = true;
    public static bool Station2 = false;
    public static bool Station3 = false;
    
    // Station1 技能解锁状态
    public static bool Boost1 = false;
    public static bool Core1 = true;

    // Station2 技能解锁状态
    public static bool Boost2 = false;
    public static bool Core2 = false;
    public static bool AntiHeat = false;
    public static bool Predict = false;

    // Station3 技能解锁状态
    public static bool Boost3 = false;
    public static bool Core3 = false;
    public static bool AntiCollision = false;

    // Station 解锁所需的 Money 数额
    public static int Station1Cost = 0;
    public static int Station2Cost = 5000;
    public static int Station3Cost = 10000;

    // Station1 技能解锁所需的 Money 数额
    public static int Boost1Cost = 3000;
    public static int Core1Cost = 0;

    // Station2 技能解锁所需的 Money 数额
    public static int Boost2Cost = 6000;
    public static int Core2Cost = 3000;
    public static int AntiHeatCost = 5000;
    public static int PredictCost = 3000;

    // Station3 技能解锁所需的 Money 数额
    public static int Boost3Cost = 9000;
    public static int Core3Cost = 6000;
    public static int AntiCollisionCost = 5000;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化：默认隐藏 tip panel
        if (tipPanel != null)
        {
            tipPanel.SetActive(false);
        }

        // 绑定 back 按钮的点击事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseTipPanel);
        }

        // 初始化：默认隐藏 tip1 panel
        if (tip1Panel != null)
        {
            tip1Panel.SetActive(false);
        }

        // 绑定 confirm 按钮的点击事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(CloseTip1Panel);
        }

        // 初始化：默认隐藏 information panel
        if (informationPanel != null)
        {
            informationPanel.SetActive(false);
        }

        // 绑定确认升级按钮的点击事件
        if (confirmUpgradeButton != null)
        {
            confirmUpgradeButton.onClick.AddListener(ConfirmUpgrade);
        }
    }

    // 显示 Tip Panel 并设置提示文字
    private void ShowTipPanel(string message)
    {
        if (tipPanel != null)
        {
            if (tipText != null)
            {
                tipText.text = message;
            }
            tipPanel.SetActive(true);
        }
        else
        {
            // 如果没有配置 tip panel，则使用 Console 输出作为备选方案
            Debug.Log(message);
        }
    }

    // 关闭 Tip Panel
    public void CloseTipPanel()
    {
        if (tipPanel != null)
        {
            tipPanel.SetActive(false);
        }
    }

    // 检查解锁条件并显示提示信息
    // 返回true表示满足所有条件，可以解锁；返回false表示不满足条件
    private bool CheckUnlockConditions(bool prerequisiteMet, int cost, string prerequisiteMessage)
    {
        bool hasEnoughMoney = MoneyManager.money >= cost;
        bool allConditionsMet = prerequisiteMet && hasEnoughMoney;

        // 如果满足所有条件，返回true
        if (allConditionsMet)
        {
            return true;
        }

        // 构建提示信息
        string message = "";
        
        if (!prerequisiteMet && !hasEnoughMoney)
        {
            // 两个条件都不满足，组合显示
            message = prerequisiteMessage + "\n金钱不足";
        }
        else if (!prerequisiteMet)
        {
            // 只不满足前置条件
            message = prerequisiteMessage;
        }
        else if (!hasEnoughMoney)
        {
            // 只不满足money条件
            message = "金钱不足";
        }

        // 显示提示
        if (!string.IsNullOrEmpty(message))
        {
            ShowTipPanel(message);
        }

        return false;
    }

    // 显示 Tip1 Panel 并设置提示文字（成功解锁提示）
    private void ShowTip1Panel(string skillName)
    {
        if (tip1Panel != null)
        {
            if (tip1Text != null)
            {
                tip1Text.text = "Successfully unlock " + skillName;
            }
            tip1Panel.SetActive(true);
        }
        else
        {
            // 如果没有配置 tip1 panel，则使用 Console 输出作为备选方案
            Debug.Log("Successfully unlock " + skillName);
        }
    }

    // 关闭 Tip1 Panel
    public void CloseTip1Panel()
    {
        if (tip1Panel != null)
        {
            tip1Panel.SetActive(false);
        }
    }

    // 显示 Information Panel 并设置技能介绍图片
    private void ShowInformationPanel(SkillType skillType)
    {
        if (informationPanel != null)
        {
            // 获取技能对应的图片索引
            int imageIndex = (int)skillType;
            
            // 检查索引是否有效
            if (imageIndex >= 0 && imageIndex < skillInfoImages.Length)
            {
                if (informationImage != null)
                {
                    informationImage.sprite = skillInfoImages[imageIndex];
                }
                else
                {
                    Debug.LogWarning("SkillManager: informationImage 未配置！");
                }
            }
            else
            {
                Debug.LogWarning($"SkillManager: 技能类型 {skillType} 的图片索引 {imageIndex} 无效！");
            }
            
            // 保存当前技能类型
            currentSkillType = skillType;
            
            // 显示面板
            informationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("SkillManager: informationPanel 未配置！");
        }
    }

    // 关闭 Information Panel
    public void CloseInformationPanel()
    {
        if (informationPanel != null)
        {
            informationPanel.SetActive(false);
        }
    }

    // 确认升级按钮点击事件
    private void ConfirmUpgrade()
    {
        // 根据当前技能类型调用对应的解锁方法（不关闭Information Panel）
        switch (currentSkillType)
        {
            case SkillType.Station1:
                ExecuteUnlockStation1();
                break;
            case SkillType.Station2:
                ExecuteUnlockStation2();
                break;
            case SkillType.Station3:
                ExecuteUnlockStation3();
                break;
            case SkillType.Boost1:
                ExecuteUnlockBoost1();
                break;
            case SkillType.Core1:
                ExecuteUnlockCore1();
                break;
            case SkillType.Boost2:
                ExecuteUnlockBoost2();
                break;
            case SkillType.Core2:
                ExecuteUnlockCore2();
                break;
            case SkillType.AntiHeat:
                ExecuteUnlockAntiHeat();
                break;
            case SkillType.Predict:
                ExecuteUnlockPredict();
                break;
            case SkillType.Boost3:
                ExecuteUnlockBoost3();
                break;
            case SkillType.Core3:
                ExecuteUnlockCore3();
                break;
            case SkillType.AntiCollision:
                ExecuteUnlockAntiCollision();
                break;
            default:
                Debug.LogWarning($"SkillManager: 未知的技能类型 {currentSkillType}");
                break;
        }
    }

    /// <summary>
    /// 通知所有技能按钮更新图片（当技能解锁后调用）
    /// </summary>
    /// <param name="skillType">解锁的技能类型（用于优化，只更新对应按钮）</param>
    private void NotifyButtonUpdate(SkillButtonController.SkillType skillType)
    {
        // 查找场景中所有的 SkillButtonController
        SkillButtonController[] allControllers = FindObjectsOfType<SkillButtonController>(true); // true表示包括未激活的对象
        
        foreach (SkillButtonController controller in allControllers)
        {
            // 只更新对应技能类型的按钮（优化性能）
            if (controller != null && controller.CurrentSkillType == skillType)
            {
                controller.UpdateButtonImage();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 可以在这里添加其他更新逻辑
    }

    // Station 解锁方法（显示Information Panel）
    public void UnlockStation1()
    {
        ShowInformationPanel(SkillType.Station1);
    }

    public void UnlockStation2()
    {
        ShowInformationPanel(SkillType.Station2);
    }

    public void UnlockStation3()
    {
        ShowInformationPanel(SkillType.Station3);
    }

    // Station 实际解锁执行方法
    private void ExecuteUnlockStation1()
    {
        if (!Station1)
        {
            // 检查money条件（Station1没有前置条件）
            if (MoneyManager.money < Station1Cost)
            {
                ShowTipPanel("金钱不足");
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station1Cost;
            Station1 = true;
            Debug.Log("Station1 已解锁!");
            ShowTip1Panel("Station1");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Station1);
        }
    }

    private void ExecuteUnlockStation2()
    {
        if (!Station2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Station2Cost, "无法解锁 Station2，请先解锁 Station1！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station2Cost;
            Station2 = true;
            Debug.Log("Station2 已解锁!");
            ShowTip1Panel("Station2");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Station2);
        }
    }

    private void ExecuteUnlockStation3()
    {
        if (!Station3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Station3Cost, "无法解锁 Station3，请先解锁 Station2！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station3Cost;
            Station3 = true;
            Debug.Log("Station3 已解锁!");
            ShowTip1Panel("Station3");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Station3);
        }
    }

    // Station1 技能解锁方法（显示Information Panel）
    public void UnlockBoost1()
    {
        ShowInformationPanel(SkillType.Boost1);
    }

    public void UnlockCore1()
    {
        ShowInformationPanel(SkillType.Core1);
    }

    // Station1 技能实际解锁执行方法
    private void ExecuteUnlockBoost1()
    {
        if (!Boost1)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Boost1Cost, "无法解锁 Boost1，请先解锁 Station1！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost1Cost;
            Boost1 = true;
            Debug.Log("Boost1 已解锁!");
            ShowTip1Panel("Boost1");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Boost1);
        }
    }

    private void ExecuteUnlockCore1()
    {
        if (!Core1)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Core1Cost, "无法解锁 Core1，请先解锁 Station1！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core1Cost;
            Core1 = true;
            Debug.Log("Core1 已解锁!");
            ShowTip1Panel("Core1");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Core1);
        }
    }

    // Station2 技能解锁方法（显示Information Panel）
    public void UnlockBoost2()
    {
        ShowInformationPanel(SkillType.Boost2);
    }

    public void UnlockCore2()
    {
        ShowInformationPanel(SkillType.Core2);
    }

    public void UnlockAntiHeat()
    {
        ShowInformationPanel(SkillType.AntiHeat);
    }

    public void UnlockPredict()
    {
        ShowInformationPanel(SkillType.Predict);
    }

    // Station2 技能实际解锁执行方法
    private void ExecuteUnlockBoost2()
    {
        if (!Boost2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Boost2Cost, "无法解锁 Boost2，请先解锁 Station2！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost2Cost;
            Boost2 = true;
            Debug.Log("Boost2 已解锁!");
            ShowTip1Panel("Boost2");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Boost2);
        }
    }

    private void ExecuteUnlockCore2()
    {
        if (!Core2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Core2Cost, "无法解锁 Core2，请先解锁 Station2！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core2Cost;
            Core2 = true;
            Debug.Log("Core2 已解锁!");
            ShowTip1Panel("Core2");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Core2);
        }
    }

    private void ExecuteUnlockAntiHeat()
    {
        if (!AntiHeat)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, AntiHeatCost, "无法解锁 AntiHeat，请先解锁 Station2！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= AntiHeatCost;
            AntiHeat = true;
            Debug.Log("AntiHeat 已解锁!");
            ShowTip1Panel("AntiHeat");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.AntiHeat);
        }
    }

    private void ExecuteUnlockPredict()
    {
        if (!Predict)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, PredictCost, "无法解锁 Predict，请先解锁 Station2！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= PredictCost;
            Predict = true;
            Debug.Log("Predict 已解锁!");
            ShowTip1Panel("Predict");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Predict);
        }
    }

    // Station3 技能解锁方法（显示Information Panel）
    public void UnlockBoost3()
    {
        ShowInformationPanel(SkillType.Boost3);
    }

    public void UnlockCore3()
    {
        ShowInformationPanel(SkillType.Core3);
    }

    public void UnlockAntiCollision()
    {
        ShowInformationPanel(SkillType.AntiCollision);
    }

    // Station3 技能实际解锁执行方法
    private void ExecuteUnlockBoost3()
    {
        if (!Boost3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, Boost3Cost, "无法解锁 Boost3，请先解锁 Station3！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost3Cost;
            Boost3 = true;
            Debug.Log("Boost3 已解锁!");
            ShowTip1Panel("Boost3");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Boost3);
        }
    }

    private void ExecuteUnlockCore3()
    {
        if (!Core3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, Core3Cost, "无法解锁 Core3，请先解锁 Station3！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core3Cost;
            Core3 = true;
            Debug.Log("Core3 已解锁!");
            ShowTip1Panel("Core3");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.Core3);
        }
    }

    private void ExecuteUnlockAntiCollision()
    {
        if (!AntiCollision)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, AntiCollisionCost, "无法解锁 AntiCollision，请先解锁 Station3！"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= AntiCollisionCost;
            AntiCollision = true;
            Debug.Log("AntiCollision 已解锁!");
            ShowTip1Panel("AntiCollision");
            // 通知按钮更新图片
            NotifyButtonUpdate(SkillButtonController.SkillType.AntiCollision);
        }
    }
}
