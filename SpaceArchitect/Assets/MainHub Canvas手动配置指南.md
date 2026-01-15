# MainHub Canvas手动配置指南

## 概述

本指南说明如何为5个MainHub场景配置Canvas引用，实现游戏场景加载时自动隐藏MainHub UI，卸载时自动恢复显示的功能。

## 功能特性

- **手动配置**：在Inspector中手动拖拽配置每个MainHub场景的Canvas引用
- **自动隐藏**：加载游戏场景时自动隐藏当前MainHub场景的Canvas
- **自动恢复**：卸载游戏场景时自动恢复MainHub场景的Canvas显示
- **多场景支持**：支持5个MainHub场景（01_MainHub, 02_MainHub, 03_MainHub, 04_MainHub, 05_MainHub）

---

## 一、配置步骤

### 1.1 配置第一个MainHub场景（01_MainHub）

1. **打开场景**
   - 在Unity编辑器中打开 `01_MainHub` 场景

2. **找到OrderDetailPanel组件**
   - 在Hierarchy中找到包含 `OrderDetailPanel` 脚本的GameObject
   - 选中该GameObject，在Inspector中查看 `OrderDetailPanel` 组件

3. **配置Canvas引用**
   - 在Inspector中找到 `Order Detail Panel (Script)` 组件
   - 展开 `MainHub Canvas配置` 部分
   - 找到 `Main Hub Canvas` 字段
   - 在Hierarchy中找到该场景的Canvas GameObject
   - 将Canvas GameObject拖拽到 `Main Hub Canvas` 字段中

4. **验证配置**
   - 确认 `Main Hub Canvas` 字段已显示Canvas的名称（不再是"None"）

### 1.2 配置其他MainHub场景

重复步骤1.1，为以下场景配置Canvas：
- `02_MainHub`
- `03_MainHub`
- `04_MainHub`
- `05_MainHub`

**重要提示**：每个MainHub场景都需要单独配置，因为每个场景都有自己的OrderDetailPanel实例。

---

## 二、配置示例

### 2.1 Inspector配置界面

在Inspector中，`OrderDetailPanel` 组件应该显示如下：

```
Order Detail Panel (Script)
├── 子组件引用
│   ├── Order Image List: [已配置]
│   ├── Text Image Controller: [已配置]
│   ├── Dynamic Text Controller: [已配置]
│   └── Stamp Animation: [已配置]
├── 按钮引用
│   ├── Reset Visit Order Button: [已配置]
│   └── Next Day Button: [已配置]
├── 数据配置
│   └── Order Data Config: [已配置]
├── MainHub Canvas配置  ← 新增部分
│   └── Main Hub Canvas: [Canvas GameObject] ← 需要配置
├── 印章动画延迟
│   └── Stamp Animation Delay: 1.0
└── 调试
    └── Enable Debug Log: ✓
```

### 2.2 配置后的效果

配置完成后，当场景加载时：
- Console中会显示：`OrderDetailPanel: 已注册场景 01_MainHub 的Canvas: [Canvas名称]`
- `SceneTransitionManager` 会记录该Canvas引用

---

## 三、工作原理

### 3.1 注册流程

```
[MainHub场景加载]
    ↓
[OrderDetailPanel.Start()]
    ↓
[获取当前场景名称]
    ↓
[检查Main Hub Canvas字段是否已配置]
    ↓
[SceneTransitionManager.RegisterMainHubCanvas(场景名, Canvas)]
    ↓
[Canvas已注册到字典中]
```

### 3.2 隐藏流程

```
[点击jumpButton加载游戏场景]
    ↓
[SceneTransitionManager.LoadSceneAdditive()]
    ↓
[获取当前激活的MainHub场景名称]
    ↓
[从字典中查找对应的Canvas]
    ↓
[隐藏Canvas]
    ↓
[记录当前MainHub场景名称]
    ↓
[加载游戏场景 (Additive)]
```

### 3.3 恢复流程

```
[点击返回主界面按钮]
    ↓
[SceneTransitionManager.UnloadGameScene()]
    ↓
[卸载游戏场景]
    ↓
[根据记录的MainHub场景名称查找Canvas]
    ↓
[恢复Canvas显示]
```

---

## 四、测试和验证

### 4.1 测试Canvas注册

1. **运行游戏**
   - 进入任意MainHub场景（如 `01_MainHub`）

2. **检查Console日志**
   - 打开Console窗口
   - 应该看到：
     - `OrderDetailPanel: 已注册场景 01_MainHub 的Canvas: [Canvas名称]`
     - `SceneTransitionManager: 已注册场景 01_MainHub 的Canvas: [Canvas名称]`

3. **验证注册**
   - 如果看到上述日志，说明Canvas已成功注册
   - 如果看到警告："MainHub Canvas未配置！"，说明Canvas引用未配置

### 4.2 测试Canvas隐藏

1. **在MainHub场景中**
   - 点击Sphere → 显示SphereInfoPanel
   - 点击jumpButton加载游戏场景

2. **验证隐藏**
   - 观察MainHub的UI是否已隐藏
   - 游戏场景应该正常显示，且看不到MainHub的UI
   - Console中应该显示：
     - `SceneTransitionManager: 已隐藏场景 01_MainHub 的Canvas: [Canvas名称]`

### 4.3 测试Canvas恢复

1. **在游戏场景中**
   - 打开暂停弹窗（按ESC键或点击暂停按钮）
   - 点击返回主界面按钮

2. **验证恢复**
   - 观察游戏场景是否被卸载
   - MainHub的UI应该重新显示
   - Console中应该显示：
     - `SceneTransitionManager: 已恢复场景 01_MainHub 的Canvas显示: [Canvas名称]`

### 4.4 测试多个场景

1. **切换场景**
   - 在 `01_MainHub` 场景中，点击"下一天"按钮
   - 切换到 `02_MainHub` 场景

2. **验证新场景注册**
   - Console中应该显示 `02_MainHub` 的Canvas注册日志

3. **测试新场景的隐藏/恢复**
   - 在 `02_MainHub` 场景中，加载游戏场景
   - 验证 `02_MainHub` 的Canvas是否正确隐藏
   - 卸载游戏场景，验证Canvas是否正确恢复

---

## 五、常见问题排查

### 5.1 Canvas未配置

**症状**：
- Console中显示警告："MainHub Canvas未配置！"
- 加载游戏场景时，MainHub UI未隐藏

**解决方法**：
1. 检查 `OrderDetailPanel` 组件的 `Main Hub Canvas` 字段
2. 确认字段不是"None"
3. 如果为"None"，从Hierarchy中拖拽Canvas GameObject到该字段

### 5.2 Canvas引用丢失

**症状**：
- Console中显示警告："Canvas引用已失效"
- Canvas隐藏/恢复功能失效

**解决方法**：
1. 检查Canvas GameObject是否被删除或重命名
2. 重新配置Canvas引用
3. 如果场景结构发生变化，重新拖拽Canvas到字段

### 5.3 场景名称不匹配

**症状**：
- Console中显示警告："当前场景不是MainHub场景"
- Canvas隐藏功能不工作

**解决方法**：
1. 确认MainHub场景名称格式为：`01_MainHub`, `02_MainHub`, `03_MainHub`, `04_MainHub`, `05_MainHub`
2. 场景名称必须以 `0` 开头，且包含 `_MainHub`
3. 如果场景名称不同，需要修改 `SceneTransitionManager.cs` 中的场景名称检测逻辑

### 5.4 多个Canvas的情况

**如果MainHub场景中有多个Canvas**：

- 系统会隐藏/恢复您在 `Main Hub Canvas` 字段中配置的那个Canvas
- 如果场景中有多个Canvas需要隐藏，建议：
  - 将所有UI放在一个Canvas下
  - 或者创建一个父GameObject包含所有Canvas，将父对象配置为Canvas引用

---

## 六、技术说明

### 6.1 Canvas注册机制

- **注册时机**：在 `OrderDetailPanel.Start()` 中自动注册
- **存储方式**：使用 `Dictionary<string, Canvas>` 存储场景名称到Canvas的映射
- **键值**：场景名称（如"01_MainHub"）
- **值**：Canvas引用

### 6.2 场景名称检测

系统会自动检测MainHub场景，检测规则：
- 场景名称以 `0` 开头
- 场景名称包含 `_MainHub`

支持的场景名称格式：
- `01_MainHub` ✓
- `02_MainHub` ✓
- `03_MainHub` ✓
- `04_MainHub` ✓
- `05_MainHub` ✓

### 6.3 引用管理

- **注册**：场景加载时自动注册
- **注销**：场景卸载时自动注销（在 `OnDestroy()` 中）
- **验证**：每次使用前验证引用是否有效
- **清理**：引用失效时自动从字典中移除

---

## 七、完成检查清单

使用以下清单确保所有MainHub场景都已正确配置：

### 场景配置
- [ ] `01_MainHub` 场景的OrderDetailPanel已配置Canvas引用
- [ ] `02_MainHub` 场景的OrderDetailPanel已配置Canvas引用
- [ ] `03_MainHub` 场景的OrderDetailPanel已配置Canvas引用
- [ ] `04_MainHub` 场景的OrderDetailPanel已配置Canvas引用
- [ ] `05_MainHub` 场景的OrderDetailPanel已配置Canvas引用

### 功能测试
- [ ] 在 `01_MainHub` 中，加载游戏场景，Canvas成功隐藏
- [ ] 在 `01_MainHub` 中，卸载游戏场景，Canvas成功恢复
- [ ] 在 `02_MainHub` 中，加载游戏场景，Canvas成功隐藏
- [ ] 在 `02_MainHub` 中，卸载游戏场景，Canvas成功恢复
- [ ] 测试其他3个MainHub场景的隐藏/恢复功能

### 调试验证
- [ ] Console中显示Canvas注册日志
- [ ] Console中显示Canvas隐藏日志
- [ ] Console中显示Canvas恢复日志
- [ ] 没有错误或警告信息

---

## 八、注意事项

1. **每个场景都需要配置**
   - 每个MainHub场景都有自己的OrderDetailPanel实例
   - 每个OrderDetailPanel都需要单独配置Canvas引用

2. **Canvas引用必须有效**
   - Canvas GameObject必须存在于场景中
   - 如果Canvas被删除或重命名，需要重新配置

3. **场景名称格式**
   - MainHub场景名称必须符合格式：`0X_MainHub`（X为1-5）
   - 场景名称区分大小写

4. **运行时的配置**
   - Canvas引用在运行时无法修改
   - 必须在编辑器中预先配置

5. **多场景切换**
   - 当切换到新的MainHub场景时，新场景的OrderDetailPanel会重新注册Canvas
   - 旧场景的Canvas引用会被新场景覆盖（如果场景已卸载）

---

## 九、故障排除

### 如果Canvas未注册

1. **检查配置**
   - 确认 `Main Hub Canvas` 字段已配置
   - 确认Canvas GameObject存在于场景中

2. **检查Console日志**
   - 查看是否有注册日志
   - 查看是否有警告信息

3. **手动测试**
   - 在代码中手动调用 `SceneTransitionManager.RegisterMainHubCanvas()`
   - 检查是否能成功注册

### 如果Canvas隐藏但未恢复

1. **检查场景名称记录**
   - 确认 `currentMainHubSceneName` 是否正确记录
   - 查看Console中的日志

2. **检查Canvas引用**
   - 确认Canvas引用未丢失
   - 确认Canvas GameObject仍然存在

3. **检查场景状态**
   - 确认MainHub场景仍然存在
   - 确认场景未被意外卸载

---

**配置完成后，系统会自动在加载游戏场景时隐藏MainHub的Canvas，卸载时恢复显示。每个MainHub场景都需要单独配置Canvas引用。**
