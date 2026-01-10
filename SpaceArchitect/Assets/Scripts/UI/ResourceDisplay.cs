using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 资源显示控制器
/// 在主界面顶部显示金币和燃料信息
/// </summary>
public class ResourceDisplay : MonoBehaviour
{
    [Header("UI文本引用")]
    [SerializeField] private Text goldText;
    [SerializeField] private Text fuelText;
    
    [Header("显示格式")]
    [SerializeField] private string goldFormat = "金币: {0}";
    [SerializeField] private string fuelFormat = "燃料: {0:F1}";
    
    void Start()
    {
        // 订阅资源变化事件
        if (GameResourceManager.Instance != null)
        {
            GameResourceManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            GameResourceManager.Instance.OnFuelChanged += UpdateFuelDisplay;
            
            // 初始化显示
            UpdateGoldDisplay(GameResourceManager.Instance.Gold);
            UpdateFuelDisplay(GameResourceManager.Instance.Fuel);
        }
        else
        {
            Debug.LogWarning("ResourceDisplay: GameResourceManager未找到，资源显示可能无法正常工作");
            // 显示占位文本
            if (goldText != null)
            {
                goldText.text = "金币: --";
            }
            if (fuelText != null)
            {
                fuelText.text = "燃料: --";
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (GameResourceManager.Instance != null)
        {
            GameResourceManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            GameResourceManager.Instance.OnFuelChanged -= UpdateFuelDisplay;
        }
    }
    
    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateGoldDisplay(int gold)
    {
        if (goldText != null)
        {
            goldText.text = string.Format(goldFormat, gold);
        }
    }
    
    /// <summary>
    /// 更新燃料显示
    /// </summary>
    private void UpdateFuelDisplay(float fuel)
    {
        if (fuelText != null)
        {
            fuelText.text = string.Format(fuelFormat, fuel);
        }
    }
}

