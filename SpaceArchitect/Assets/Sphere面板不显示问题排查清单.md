# Sphere面板不显示问题排查清单

## 🔍 快速检查步骤

### ✅ 步骤 1：检查 Console 日志

1. **运行游戏**
2. **按 T 键显示图标**
3. **点击气泡图标**
4. **查看 Console 窗口**（Window → General → Console）

**检查点：**
- [ ] 是否有错误（红色）？
- [ ] 是否有警告（黄色）？
- [ ] 是否看到 "SphereIconManager: 点击了Sphere XXX 的图标" 这条日志？

---

### ✅ 步骤 2：检查 SphereIconManager 配置

**位置：** Hierarchy 中找到挂载 `SphereIconManager.cs` 的 GameObject

#### 检查项：
- [ ] `Order Data Config` 字段是否已配置？
  - **操作**：应该拖入 `SphereOrderDataConfig.asset`
  - **如果为空**：拖入数据资源

- [ ] `Info Panel` 字段是否已配置？
  - **操作**：应该拖入 `SphereInfoPanel` GameObject 或预制体
  - **如果为空**：从 Hierarchy 拖入 `SphereInfoPanel` GameObject

- [ ] `Sphere 1`、`Sphere 2`、`Sphere 4` 是否都已配置？

---

### ✅ 步骤 3：检查数据配置

**位置：** Project 窗口 → 选中 `SphereOrderDataConfig.asset`

#### 检查项：
- [ ] `Order Data List` 是否有至少 3 个条目？

- [ ] **Element 0**：
  - [ ] `Sphere Name` = `Sphere1`（完全一致，区分大小写）
  - [ ] `Order Image` 已配置图片
  - [ ] `Target Scene Name` = `scene02`（或其他场景名）

- [ ] **Element 1**：
  - [ ] `Sphere Name` = `Sphere2`
  - [ ] `Order Image` 已配置图片
  - [ ] `Target Scene Name` = `scene03`

- [ ] **Element 2**：
  - [ ] `Sphere Name` = `Sphere4`
  - [ ] `Order Image` 已配置图片
  - [ ] `Target Scene Name` = `scene04`

---

### ✅ 步骤 4：检查 Sphere 名称匹配

**位置：** Hierarchy 中找到 Sphere GameObject

#### 检查项：
1. **找到场景中的 Sphere GameObject**
   - 在 Hierarchy 中查找：`Sphere1`、`Sphere2`、`Sphere4`

2. **确认名称完全匹配**
   - 场景中的名称：`Sphere1`（首字母大写S）
   - 数据配置中的名称：`Sphere1`（必须完全一致）

3. **常见错误：**
   - ❌ 场景中是 `sphere1`（小写s）
   - ❌ 场景中是 `Sphere 1`（有空格）
   - ❌ 数据配置中是 `Sphere1`（无空格）
   - ✅ 应该都是 `Sphere1`（完全一致）

---

### ✅ 步骤 5：检查 SphereInfoPanel 配置

**位置：** Hierarchy 中找到 `SphereInfoPanel` GameObject

#### 检查项：
- [ ] `SphereInfoPanel` 脚本组件中的引用：
  - [ ] `Panel Image` 已拖入 `OrderImage` GameObject
  - [ ] `Close Button` 已拖入 `CloseButton` GameObject
  - [ ] `Jump Button` 已拖入 `JumpButton` GameObject

- [ ] `SphereInfoPanel` GameObject 的激活状态：
  - [ ] **默认应该隐藏**（Inspector 左上角复选框**未勾选**）
  - [ ] 脚本会在需要时自动显示

---

## 🐛 常见错误及解决方法

### ❌ 错误 1：Console 显示 "orderDataConfig未配置"

**原因：** `SphereIconManager` 的 `Order Data Config` 字段为空

**解决：**
1. 选中挂载 `SphereIconManager` 的 GameObject
2. 在 Inspector 中找到 `Order Data Config` 字段
3. 从 Project 窗口拖拽 `SphereOrderDataConfig.asset` 到此处

---

### ❌ 错误 2：Console 显示 "infoPanel未配置"

**原因：** `SphereIconManager` 的 `Info Panel` 字段为空

**解决：**
1. 选中挂载 `SphereIconManager` 的 GameObject
2. 在 Inspector 中找到 `Info Panel` 字段
3. 从 Hierarchy 拖拽 `SphereInfoPanel` GameObject 到此处

---

### ❌ 错误 3：Console 显示 "未找到名称为 XXX 的Sphere订单数据"

**原因：** Sphere 名称不匹配

**解决：**
1. 在 Hierarchy 中找到 Sphere GameObject
2. 查看其名称（如 `Sphere1`）
3. 打开 `SphereOrderDataConfig.asset`
4. 检查 `Order Data List` 中对应条目的 `Sphere Name`
5. 确保**完全一致**（区分大小写，无空格）

---

### ❌ 错误 4：点击图标有日志，但面板不显示

**可能原因：**
1. **SphereInfoPanel 的引用未配置**
   - 检查 `SphereIconManager` 的 `Info Panel` 字段

2. **数据配置中找不到对应Sphere**
   - 检查 Sphere 名称是否匹配
   - 检查数据配置中是否有对应的条目

3. **订单图片为空**
   - Console 会显示警告，但面板应该还是会显示

---

### ❌ 错误 5：面板显示但图片为空

**原因：** `SphereInfoPanel` 的 `Panel Image` 引用未配置

**解决：**
1. 选中 `SphereInfoPanel` GameObject
2. 在 Inspector 中找到 `SphereInfoPanel` 脚本组件
3. 检查 `Panel Image` 字段
4. 从 Hierarchy 拖拽 `OrderImage` GameObject 到此处

---

## 📝 完整检查清单

完成以下所有检查项：

### 配置检查
- [ ] `SphereIconManager.Order Data Config` 已配置
- [ ] `SphereIconManager.Info Panel` 已配置
- [ ] `SphereIconManager.Sphere 1/2/4` 已配置
- [ ] `SphereOrderDataConfig.asset` 中配置了至少3个条目
- [ ] 每个条目的 `Sphere Name` 与场景中GameObject名称完全一致
- [ ] 每个条目的 `Order Image` 已配置
- [ ] `SphereInfoPanel.Panel Image` 已配置
- [ ] `SphereInfoPanel.Close Button` 已配置
- [ ] `SphereInfoPanel.Jump Button` 已配置

### 测试检查
- [ ] 运行游戏，按 T 键可以显示图标
- [ ] 点击图标后 Console 有日志输出
- [ ] 点击图标后面板显示
- [ ] 面板显示正确的订单图片
- [ ] 点击"关闭"按钮可以关闭面板

---

## 🆘 如果还是不行

请提供以下信息：
1. **Console 中的错误/警告信息**（完整文本）
2. **SphereIconManager 的配置截图**
3. **SphereOrderDataConfig.asset 的配置截图**
4. **Sphere GameObject 的名称**

我会根据这些信息进一步排查。
