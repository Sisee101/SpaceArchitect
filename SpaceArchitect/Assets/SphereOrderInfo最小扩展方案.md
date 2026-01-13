# SphereOrderInfo最小扩展方案

本文档说明如果只实现"完成订单 → 气泡消失动画 → 播放胜利视频"功能，SphereOrderInfo需要扩展的最小内容。

---

## 🎯 功能需求

1. 键盘输入1完成订单（测试用）
2. 对应气泡播放消失动画
3. 屏幕中间播放对应的胜利结算视频

---

## 📋 需要扩展的字段

### 最小扩展方案（只添加必需字段）

```csharp
[System.Serializable]
public class SphereOrderInfo
{
    [Header("Sphere信息")]
    public string sphereName;
    
    [Header("订单信息")]
    public Sprite orderImage;
    public string targetSceneName;
    
    [Header("任务信息（新增）")]
    [Tooltip("任务ID（唯一标识，用于任务完成检测。如果为-1，表示此订单不参与任务系统）")]
    public int taskId = -1;  // 任务ID（必需）
    
    [Header("胜利视频（新增）")]
    [Tooltip("任务完成时播放的胜利结算视频")]
    public VideoClip victoryVideoClip;  // 胜利视频（必需）
}
```

---

## ✅ 必需字段说明

### 1. taskId（任务ID）

**用途**：
- 唯一标识每个订单/任务
- 用于任务完成检测（键盘输入1时，需要知道完成的是哪个任务）
- 用于关联气泡和任务

**类型**：`int`

**默认值**：`-1`（表示未配置任务，不参与任务系统）

**要求**：
- 必须唯一（不能有重复的taskId）
- 建议从0开始递增：0, 1, 2, 3...

**示例**：
```csharp
// Sphere (1) 的订单
taskId = 0

// Sphere (5) 的订单
taskId = 1

// Sphere (3) 的订单
taskId = 2
```

---

### 2. victoryVideoClip（胜利视频）

**用途**：
- 存储任务完成时播放的胜利结算视频
- 每个订单对应一个独特的胜利视频

**类型**：`VideoClip`（Unity的视频资源类型）

**默认值**：`null`（表示未配置视频）

**要求**：
- 需要导入视频文件（mp4格式推荐）
- 每个订单可以有不同的视频，也可以共享同一个视频

**示例**：
```csharp
// Sphere (1) 的订单
victoryVideoClip = VictoryVideo_Sphere1.mp4

// Sphere (5) 的订单
victoryVideoClip = VictoryVideo_Sphere5.mp4
```

---

## ❌ 不需要的字段

### taskName（任务名称）
- **不需要**：功能不涉及显示任务名称
- **用途**：仅用于UI显示，不是功能必需

### taskDescription（任务描述）
- **不需要**：功能不涉及显示任务描述
- **用途**：仅用于UI显示，不是功能必需

---

## 📐 完整扩展后的SphereOrderInfo

```csharp
[System.Serializable]
public class SphereOrderInfo
{
    [Header("Sphere信息")]
    [Tooltip("Sphere GameObject的名称（必须与场景中的Sphere名称完全一致，区分大小写）")]
    public string sphereName;
    
    [Header("订单信息")]
    [Tooltip("订单面板显示的图片")]
    public Sprite orderImage;
    
    [Tooltip("点击前往配送按钮后跳转的场景名称（必须在Build Settings中）")]
    public string targetSceneName;
    
    [Header("任务信息")]
    [Tooltip("任务ID（唯一标识，用于任务完成检测。如果为-1，表示此订单不参与任务系统）")]
    public int taskId = -1;  // 新增：任务ID
    
    [Header("胜利视频")]
    [Tooltip("任务完成时播放的胜利结算视频")]
    public VideoClip victoryVideoClip;  // 新增：胜利视频
}
```

---

## 🔧 需要添加的查询方法

在`SphereOrderDataConfig`类中添加以下方法：

```csharp
/// <summary>
/// 根据任务ID获取订单信息
/// </summary>
/// <param name="taskId">任务ID</param>
/// <returns>找到的订单信息，如果不存在返回null</returns>
public SphereOrderInfo GetOrderInfoByTaskId(int taskId)
{
    if (orderDataList == null || orderDataList.Count == 0)
    {
        return null;
    }
    
    foreach (var info in orderDataList)
    {
        if (info != null && info.taskId == taskId)
        {
            return info;
        }
    }
    
    return null;
}

/// <summary>
/// 获取所有已配置任务的订单信息（taskId >= 0）
/// </summary>
/// <returns>任务ID大于等于0的订单信息列表</returns>
public List<SphereOrderInfo> GetAllTaskOrders()
{
    List<SphereOrderInfo> tasks = new List<SphereOrderInfo>();
    
    if (orderDataList == null || orderDataList.Count == 0)
    {
        return tasks;
    }
    
    foreach (var info in orderDataList)
    {
        if (info != null && info.taskId >= 0)
        {
            tasks.Add(info);
        }
    }
    
    return tasks;
}
```

---

## 📊 数据配置示例

### 在Unity Editor中配置

假设有3个订单：

**订单1（Sphere (1)）**：
- Sphere Name: `"Sphere (1)"`
- Order Image: `[已配置的图片]`
- Target Scene Name: `"UITRY"`
- **Task ID**: `0` ← 新增
- **Victory Video Clip**: `VictoryVideo_Sphere1.mp4` ← 新增

**订单2（Sphere (5)）**：
- Sphere Name: `"Sphere (5)"`
- Order Image: `[已配置的图片]`
- Target Scene Name: `"UITRY"`
- **Task ID**: `1` ← 新增
- **Victory Video Clip**: `VictoryVideo_Sphere5.mp4` ← 新增

**订单3（Sphere (3)）**：
- Sphere Name: `"Sphere (3)"`
- Order Image: `[已配置的图片]`
- Target Scene Name: `"UITRY"`
- **Task ID**: `2` ← 新增
- **Victory Video Clip**: `VictoryVideo_Sphere3.mp4` ← 新增

---

## 🔄 功能流程

```
键盘输入1
    ↓
TaskManager检测到输入，完成taskId=0的任务
    ↓
通过SphereOrderDataConfig.GetOrderInfoByTaskId(0)获取订单信息
    ↓
获取sphereName = "Sphere (1)"
    ↓
调用SphereIconManager.HideIconForSphere("Sphere (1)")
    ↓
播放气泡消失动画（0.5秒）
    ↓
动画完成
    ↓
通过orderInfo.victoryVideoClip获取胜利视频
    ↓
调用VictoryVideoPlayer.PlayVideo(orderInfo.victoryVideoClip)
    ↓
播放胜利视频
```

---

## ✅ 总结

### 最小扩展内容

**只需添加2个字段**：
1. ✅ `taskId`（int）- 任务ID，用于标识和关联
2. ✅ `victoryVideoClip`（VideoClip）- 胜利视频，用于播放

**不需要的字段**：
- ❌ `taskName` - 不涉及显示任务名称
- ❌ `taskDescription` - 不涉及显示任务描述

### 代码修改

1. **SphereOrderInfo类**：添加`taskId`和`victoryVideoClip`字段
2. **SphereOrderDataConfig类**：添加`GetOrderInfoByTaskId()`和`GetAllTaskOrders()`方法

### 数据配置

1. 为每个订单添加唯一的`taskId`（从0开始）
2. 为每个订单配置对应的`victoryVideoClip`（导入视频资源）

---

**结论**：如果只实现这个功能，SphereOrderInfo只需要扩展2个字段：`taskId`和`victoryVideoClip`。
