using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    // Tip Panel 相关引用
    [Header("Tip Panel 设置")]
    public GameObject tipPanel;  // Tip Panel GameObject
    public TextMeshProUGUI tipText;  // Tip Panel 上的文字提示
    public Button backButton;  // Back 按钮
    
    // Tip1 Panel 相关引用（成功解锁提示）
    [Header("Tip1 Panel 设置")]
    public GameObject tip1Panel;  // Tip1 Panel GameObject
    public TextMeshProUGUI tip1Text;  // Tip1 Panel 上的文字提示
    public Button confirmButton;  // Confirm 按钮
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
    public static int Station2Cost = 10000;
    public static int Station3Cost = 30000;

    // Station1 技能解锁所需的 Money 数额
    public static int Boost1Cost = 2000;
    public static int Core1Cost = 0;

    // Station2 技能解锁所需的 Money 数额
    public static int Boost2Cost = 5000;
    public static int Core2Cost = 3000;
    public static int AntiHeatCost = 6000;
    public static int PredictCost = 7000;

    // Station3 技能解锁所需的 Money 数额
    public static int Boost3Cost = 10000;
    public static int Core3Cost = 8000;
    public static int AntiCollisionCost = 10000;

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
            message = prerequisiteMessage + "\nLack of money";
        }
        else if (!prerequisiteMet)
        {
            // 只不满足前置条件
            message = prerequisiteMessage;
        }
        else if (!hasEnoughMoney)
        {
            // 只不满足money条件
            message = "Lack of money";
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

    // Update is called once per frame
    void Update()
    {
        // 可以在这里添加其他更新逻辑
    }

    // Station 解锁方法
    public void UnlockStation1()
    {
        if (!Station1)
        {
            // 检查money条件（Station1没有前置条件）
            if (MoneyManager.money < Station1Cost)
            {
                ShowTipPanel("Lack of money");
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station1Cost;
            Station1 = true;
            Debug.Log("Station1 已解锁!");
            ShowTip1Panel("Station1");
        }
    }

    public void UnlockStation2()
    {
        if (!Station2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Station2Cost, "Cannot unlock Station2, please unlock Station1 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station2Cost;
            Station2 = true;
            Debug.Log("Station2 已解锁!");
            ShowTip1Panel("Station2");
        }
    }

    public void UnlockStation3()
    {
        if (!Station3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Station3Cost, "Cannot unlock Station3, please unlock Station2 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Station3Cost;
            Station3 = true;
            Debug.Log("Station3 已解锁!");
            ShowTip1Panel("Station3");
        }
    }

    // Station1 技能解锁方法
    public void UnlockBoost1()
    {
        if (!Boost1)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Boost1Cost, "Cannot unlock Boost1, please unlock Station1 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost1Cost;
            Boost1 = true;
            Debug.Log("Boost1 已解锁!");
            ShowTip1Panel("Boost1");
        }
    }

    public void UnlockCore1()
    {
        if (!Core1)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station1, Core1Cost, "Cannot unlock Core1, please unlock Station1 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core1Cost;
            Core1 = true;
            Debug.Log("Core1 已解锁!");
            ShowTip1Panel("Core1");
        }
    }

    // Station2 技能解锁方法
    public void UnlockBoost2()
    {
        if (!Boost2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Boost2Cost, "Cannot unlock Boost2, please unlock Station2 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost2Cost;
            Boost2 = true;
            Debug.Log("Boost2 已解锁!");
            ShowTip1Panel("Boost2");
        }
    }

    public void UnlockCore2()
    {
        if (!Core2)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, Core2Cost, "Cannot unlock Core2, please unlock Station2 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core2Cost;
            Core2 = true;
            Debug.Log("Core2 已解锁!");
            ShowTip1Panel("Core2");
        }
    }

    public void UnlockAntiHeat()
    {
        if (!AntiHeat)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, AntiHeatCost, "Cannot unlock AntiHeat, please unlock Station2 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= AntiHeatCost;
            AntiHeat = true;
            Debug.Log("AntiHeat 已解锁!");
            ShowTip1Panel("AntiHeat");
        }
    }

    public void UnlockPredict()
    {
        if (!Predict)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station2, PredictCost, "Cannot unlock Predict, please unlock Station2 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= PredictCost;
            Predict = true;
            Debug.Log("Predict 已解锁!");
            ShowTip1Panel("Predict");
        }
    }

    // Station3 技能解锁方法
    public void UnlockBoost3()
    {
        if (!Boost3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, Boost3Cost, "Cannot unlock Boost3, please unlock Station3 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Boost3Cost;
            Boost3 = true;
            Debug.Log("Boost3 已解锁!");
            ShowTip1Panel("Boost3");
        }
    }

    public void UnlockCore3()
    {
        if (!Core3)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, Core3Cost, "Cannot unlock Core3, please unlock Station3 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= Core3Cost;
            Core3 = true;
            Debug.Log("Core3 已解锁!");
            ShowTip1Panel("Core3");
        }
    }

    public void UnlockAntiCollision()
    {
        if (!AntiCollision)
        {
            // 检查解锁条件
            if (!CheckUnlockConditions(Station3, AntiCollisionCost, "Cannot unlock AntiCollision, please unlock Station3 first!"))
            {
                return;
            }
            
            // 扣除money并解锁
            MoneyManager.money -= AntiCollisionCost;
            AntiCollision = true;
            Debug.Log("AntiCollision 已解锁!");
            ShowTip1Panel("AntiCollision");
        }
    }
}
