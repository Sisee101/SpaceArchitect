using UnityEngine;
using UnityEditor;

/// <summary>
/// 订单选择器自定义 PropertyDrawer
/// 在 Inspector 中将标记了 [OrderSelector] 的 int 字段显示为下拉框
/// 下拉框选项从 SphereOrderDataConfig 中动态获取，显示格式为 "Sphere名称 [TaskId: X]"
/// </summary>
[CustomPropertyDrawer(typeof(OrderSelectorAttribute))]
public class OrderSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 确保属性类型是 int
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "OrderSelector 属性只能用于 int 类型字段");
            return;
        }
        
        // 查找支持的组件类型（通过 property 的序列化对象）
        MonoBehaviour targetObject = property.serializedObject.targetObject as MonoBehaviour;
        
        if (targetObject == null)
        {
            EditorGUI.LabelField(position, label.text, "OrderSelector 只能用于 MonoBehaviour 组件");
            return;
        }
        
        // 检查是否是支持的组件类型
        System.Type componentType = null;
        if (targetObject is OrderCompleteButton)
        {
            componentType = typeof(OrderCompleteButton);
        }
        else if (targetObject is OrderCompleteAndLoadSceneButton)
        {
            componentType = typeof(OrderCompleteAndLoadSceneButton);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "OrderSelector 只能用于 OrderCompleteButton 或 OrderCompleteAndLoadSceneButton 组件");
            return;
        }
        
        // 使用反射获取 orderDataConfig 字段（因为它是 private）
        var orderDataConfigField = componentType.GetField("orderDataConfig", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (orderDataConfigField == null)
        {
            EditorGUI.LabelField(position, label.text, "无法访问 orderDataConfig 字段");
            return;
        }
        
        SphereOrderDataConfig orderDataConfig = orderDataConfigField.GetValue(targetObject) as SphereOrderDataConfig;
        
        // 如果未配置 orderDataConfig，显示警告并允许手动输入索引
        if (orderDataConfig == null || orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            // 显示警告信息
            Rect warningRect = position;
            warningRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(warningRect, "请先配置 Order Data Config 才能使用下拉框选择订单", MessageType.Warning);
            
            // 显示手动输入字段
            Rect inputRect = position;
            inputRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            inputRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(inputRect, property, label);
            return;
        }
        
        // 生成下拉框选项数组
        string[] options = new string[orderDataConfig.orderDataList.Count];
        for (int i = 0; i < orderDataConfig.orderDataList.Count; i++)
        {
            var orderInfo = orderDataConfig.orderDataList[i];
            if (orderInfo != null)
            {
                // 格式：Sphere名称 [TaskId: X] 或 Sphere名称 [无任务]
                if (orderInfo.taskId >= 0)
                {
                    options[i] = $"{orderInfo.sphereName} [TaskId: {orderInfo.taskId}]";
                }
                else
                {
                    options[i] = $"{orderInfo.sphereName} [无任务]";
                }
            }
            else
            {
                options[i] = $"索引 {i} (空)";
            }
        }
        
        // 确保当前索引在有效范围内
        int currentIndex = property.intValue;
        if (currentIndex < 0 || currentIndex >= options.Length)
        {
            currentIndex = 0;
            property.intValue = 0;
        }
        
        // 显示下拉框
        EditorGUI.BeginProperty(position, label, property);
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
        
        // 如果选择发生变化，更新属性值
        if (newIndex != currentIndex)
        {
            property.intValue = newIndex;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 查找支持的组件类型
        MonoBehaviour targetObject = property.serializedObject.targetObject as MonoBehaviour;
        
        if (targetObject == null)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        // 检查是否是支持的组件类型
        System.Type componentType = null;
        if (targetObject is OrderCompleteButton)
        {
            componentType = typeof(OrderCompleteButton);
        }
        else if (targetObject is OrderCompleteAndLoadSceneButton)
        {
            componentType = typeof(OrderCompleteAndLoadSceneButton);
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        // 使用反射获取 orderDataConfig 字段
        var orderDataConfigField = componentType.GetField("orderDataConfig", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (orderDataConfigField == null)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        SphereOrderDataConfig orderDataConfig = orderDataConfigField.GetValue(targetObject) as SphereOrderDataConfig;
        
        // 如果未配置，需要额外空间显示警告信息
        if (orderDataConfig == null || orderDataConfig.orderDataList == null || orderDataConfig.orderDataList.Count == 0)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
        
        return EditorGUIUtility.singleLineHeight;
    }
}
