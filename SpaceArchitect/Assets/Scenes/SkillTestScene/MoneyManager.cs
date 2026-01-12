using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    // 玩家余额，可在整个游戏项目中访问
    public static int money = 20000;

    // TextMeshPro 文本组件引用
    private TextMeshProUGUI moneyText;
    
    // 上一次的money值，用于检测变化
    private int lastMoneyValue;

    // Start is called before the first frame update
    void Start()
    {
        // 获取同一GameObject上的TextMeshProUGUI组件
        moneyText = GetComponent<TextMeshProUGUI>();
        
        // 如果找不到组件，尝试获取子对象上的组件
        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // 初始化显示
        UpdateMoneyDisplay();
        lastMoneyValue = money;
    }

    // Update is called once per frame
    void Update()
    {
        // 检测money值是否发生变化
        if (money != lastMoneyValue)
        {
            UpdateMoneyDisplay();
            lastMoneyValue = money;
        }
    }

    // 更新Money显示
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = money.ToString();
        }
    }
}
