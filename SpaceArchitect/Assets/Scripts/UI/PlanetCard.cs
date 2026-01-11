using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 行星卡片控制器
/// 管理单个行星卡片的显示和点击事件
/// </summary>
public class PlanetCard : MonoBehaviour, IPointerClickHandler
{
    [Header("UI引用")]
    [SerializeField] private Image planetImage;      // 行星图片
    
    [Header("数据")]
    private int cardIndex = -1;                      // 卡片索引（0-12）
    private Sprite cardSprite;                       // 卡片图片
    
    /// <summary>
    /// 卡片点击事件
    /// </summary>
    public event Action<PlanetCard> OnCardClicked;
    
    /// <summary>
    /// 初始化卡片
    /// </summary>
    /// <param name="sprite">行星卡片图片</param>
    /// <param name="index">卡片索引（0-12）</param>
    public void Initialize(Sprite sprite, int index)
    {
        cardSprite = sprite;
        cardIndex = index;
        
        // 设置图片
        if (planetImage != null && sprite != null)
        {
            planetImage.sprite = sprite;
        }
        else if (planetImage == null)
        {
            Debug.LogWarning($"PlanetCard: planetImage引用为空，无法设置图片！");
        }
    }
    
    /// <summary>
    /// 获取卡片索引
    /// </summary>
    public int GetCardIndex()
    {
        return cardIndex;
    }
    
    /// <summary>
    /// 实现IPointerClickHandler接口，处理点击事件
    /// 这样不需要Button组件，卡片本身任何部位都可以点击
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnCardClicked != null)
        {
            OnCardClicked.Invoke(this);
        }
    }
}
