using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 介绍卡片控制器
/// 管理介绍卡片的显示（动态插入的行星详情卡片）
/// </summary>
public class DetailCard : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image detailImage;      // 介绍图片
    
    /// <summary>
    /// 初始化介绍卡片
    /// </summary>
    /// <param name="sprite">介绍卡片图片</param>
    public void Initialize(Sprite sprite)
    {
        // 设置图片
        if (detailImage != null && sprite != null)
        {
            detailImage.sprite = sprite;
        }
        else if (detailImage == null)
        {
            Debug.LogWarning($"DetailCard: detailImage引用为空，无法设置图片！");
        }
    }
}
