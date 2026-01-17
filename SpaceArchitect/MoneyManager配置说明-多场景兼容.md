# MoneyManager 多场景配置说明

## 📋 问题背景

你的项目中**每个MainHub场景都配置了MoneyManager**，这是一个常见的设计模式。修复后的MoneyManager添加了`persistAcrossScenes`（DontDestroyOnLoad）选项，但这可能与你的现有架构冲突。

## 🎯 推荐配置

### ✅ 方案A：保持默认配置（推荐）

**适用于**：每个MainHub场景都有自己的MoneyManager

#### 配置步骤：

1. **确保 `Persist Across Scenes` 保持未勾选（false）**
   - 这是默认值，不需要修改

2. **每个MainHub场景独立配置**
   - 每个场景都有自己的MoneyManager GameObject
   - 每个MoneyManager绑定自己场景的UI组件
   - 每个MoneyManager自动查找自己场景的TaskManager

3. **在Inspector中手动指定引用（推荐）**
   - 将场景中的TaskManager拖拽到"Task Manager"字段
   - 将SphereOrderDataConfig拖拽到"Order Data Config"字段

#### 优点：
- ✅ 符合你当前的项目结构
- ✅ 每个场景配置独立，互不影响
- ✅ 不会出现引用冲突
- ✅ 场景切换时自动销毁和重建
- ✅ 简单可靠

#### 工作原理：
```
场景A (MainHub1)
  └─ MoneyManager A
      ├─ TaskManager A
      └─ Text Component A

切换场景 →

场景B (MainHub2)
  └─ MoneyManager B (新实例)
      ├─ TaskManager B
      └─ Text Component B

MoneyManager A 被销毁 ✓
MoneyManager B 独立工作 ✓
```

---

## 🔧 方案B：使用DontDestroyOnLoad（高级，需重构）

**适用于**：只在一个场景中创建MoneyManager，其他场景不需要

⚠️ **警告**：如果选择此方案，需要重构你的场景结构！

### 需要做的修改：

#### 1. 场景结构调整

**第一个场景（如01_MainHub）**：
- ✅ 保留MoneyManager GameObject
- ✅ 勾选"Persist Across Scenes"
- ✅ 配置好所有引用

**其他MainHub场景（02-05等）**：
- ❌ **删除MoneyManager GameObject**
- ✅ 保留TaskManager（每个场景仍需要）
- ✅ 保留Text组件（如果需要显示金钱）

#### 2. UI组件处理

由于DontDestroyOnLoad的MoneyManager无法直接绑定新场景的UI组件，需要：

**选项1**：将Text组件也设为DontDestroyOnLoad
```csharp
// 在MoneyManager的Awake中
if (persistAcrossScenes && moneyText != null)
{
    DontDestroyOnLoad(moneyText.gameObject);
}
```

**选项2**：使用动态查找（已实现）
- 系统会在场景切换时自动查找新场景的Text组件
- Text组件需要在MoneyManager对象上或其子对象上

#### 3. 测试清单

- [ ] 第一个场景启动正常
- [ ] 切换到第二个场景，MoneyManager仍然存在
- [ ] 第二个场景的TaskManager被正确找到
- [ ] 金钱显示正常更新
- [ ] 完成订单后金钱正确增加
- [ ] 没有重复的MoneyManager实例

### 优点：
- ✅ 只有一个MoneyManager实例
- ✅ 金钱值在场景切换时保持
- ✅ 减少重复配置

### 缺点：
- ❌ 需要重构现有场景
- ❌ UI组件绑定复杂
- ❌ 调试困难
- ❌ 可能出现引用丢失

---

## 🔍 两种方案对比

| 特性 | 方案A（每场景独立） | 方案B（DontDestroyOnLoad） |
|------|-------------------|--------------------------|
| **配置复杂度** | ⭐ 简单 | ⭐⭐⭐⭐ 复杂 |
| **场景结构** | 不需要修改 | 需要重构 |
| **引用管理** | 自动，每场景独立 | 手动，需要动态查找 |
| **调试难度** | ⭐ 容易 | ⭐⭐⭐⭐ 困难 |
| **金钱持久化** | 通过static变量 | 通过DontDestroyOnLoad |
| **适用场景** | 多个MainHub场景 | 单一入口场景 |
| **推荐度** | ⭐⭐⭐⭐⭐ | ⭐⭐ |

---

## 💡 关键理解

### Money值的持久化

无论使用哪种方案，`money`值都是持久化的，因为它是**静态变量**：

```csharp
public static int money = 0;
```

- ✅ 场景切换时不会丢失
- ✅ 所有MoneyManager实例共享同一个值
- ✅ 不需要DontDestroyOnLoad也能保持

### MoneyManager的作用

MoneyManager主要负责：
1. **显示金钱**：更新UI Text组件
2. **监听事件**：订阅TaskManager的任务完成事件
3. **更新金钱**：当任务完成时增加金钱

### 为什么每个场景可以有独立的MoneyManager？

因为：
- `money`是静态变量，所有实例共享
- 每个场景的MoneyManager只是显示和监听
- 场景切换时旧实例销毁，新实例接管
- 金钱值不会丢失

---

## 🎯 针对你的项目的建议

### 推荐配置：

```
✅ persistAcrossScenes = false (默认)
✅ autoFindOnStart = true
✅ enableDebugLog = true (测试时)
✅ 在Inspector中手动指定TaskManager引用
✅ 在Inspector中手动指定OrderDataConfig引用
```

### 每个MainHub场景的配置：

1. **保留MoneyManager GameObject**
2. **不要勾选"Persist Across Scenes"**
3. **手动拖拽引用（最可靠）**：
   - Task Manager → 场景中的TaskManager
   - Order Data Config → SphereOrderDataConfig资源
4. **配置Text组件**：
   - 将TextMeshProUGUI组件添加到MoneyManager或其子对象

### 场景切换流程：

```
玩家在MainHub1
  ↓
MoneyManager1 显示金钱: 1000
  ↓
完成订单，金钱增加到 1500
  ↓
切换到MainHub2
  ↓
MoneyManager1 被销毁
  ↓
MoneyManager2 启动
  ↓
MoneyManager2 显示金钱: 1500 (静态变量保持)
  ↓
MoneyManager2 订阅TaskManager2的事件
  ↓
继续正常工作 ✓
```

---

## 🐛 常见问题

### Q1: 场景切换后金钱值会丢失吗？

**A**: 不会！`money`是静态变量，场景切换时不会丢失。

### Q2: 每个场景都需要配置MoneyManager吗？

**A**: 如果使用方案A（推荐），是的。但配置很简单，只需要拖拽引用。

### Q3: 如果忘记配置TaskManager引用会怎样？

**A**: 
- 自动查找功能会尝试找到TaskManager（已修复，打包后也能工作）
- 如果找不到，会输出错误日志
- 金钱显示正常，但完成订单时不会增加金钱

### Q4: 可以在某些场景使用DontDestroyOnLoad，某些场景不使用吗？

**A**: 不推荐！这会导致混乱。建议统一使用一种方案。

### Q5: 如何验证配置正确？

**A**: 
1. 运行游戏，查看Console
2. 确认有"已成功订阅TaskManager的任务完成事件"
3. 完成订单，确认有"任务完成！金钱已增加"
4. 切换场景，确认金钱值保持
5. 在新场景完成订单，确认金钱继续增加

---

## 📝 总结

### 对于你的项目：

1. **保持 `persistAcrossScenes = false`**（默认值）
2. **不需要修改场景结构**
3. **每个场景的MoneyManager独立工作**
4. **金钱值通过静态变量自动保持**
5. **核心修复（FindObjectOfType）已解决打包问题**

### 核心修复的作用：

修复的重点不是DontDestroyOnLoad，而是：
- ✅ 将 `Resources.FindObjectsOfTypeAll` 改为 `FindObjectOfType`
- ✅ 这样打包后也能正确找到TaskManager
- ✅ 每个场景的MoneyManager都能正常工作

### DontDestroyOnLoad只是额外功能：

- 默认是**关闭**的
- 不影响核心修复
- 只在特定场景结构下才需要使用
- **你的项目不需要启用它**

---

**修复日期**: 2026-01-17  
**适用版本**: Unity 2019.4+  
**配置建议**: 保持默认（persistAcrossScenes = false）

