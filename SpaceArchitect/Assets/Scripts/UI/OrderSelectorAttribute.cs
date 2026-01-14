using UnityEngine;

/// <summary>
/// 订单选择器属性标记
/// 用于标记需要显示为下拉框的订单索引字段
/// </summary>
public class OrderSelectorAttribute : PropertyAttribute
{
    // 这个属性类本身不需要任何逻辑，只是作为标记使用
    // PropertyDrawer 会识别这个属性并自定义绘制方式
}
