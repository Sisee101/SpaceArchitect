# Sphere气泡图标和订单面板完整搭建指南

## 📋 目录

1. [概述](#概述)
2. [第一步：创建订单数据配置](#第一步创建订单数据配置)
3. [第二步：创建订单信息面板预制体](#第二步创建订单信息面板预制体)
4. [第三步：配置SphereIconManager](#第三步配置sphereiconmanager)
5. [第四步：测试功能](#第四步测试功能)
6. [常见问题排查](#常见问题排查)

---

## 概述

本指南将帮助 you 完成以下功能的搭建：
- ✅ **Sphere图标显示**：按T键显示Sphere右上角的可点击图标
- ✅ **订单信息面板**：点击图标后弹出订单信息面板
- ✅ **场景跳转**：点击"前往配送"按钮跳转到对应场景

**相关文件：**
- `SphereIconManager.cs` - 图标管理器（已创建）
- `SphereOrderDataConfig.cs` - 订单数据配置类（已创建）
- `SphereInfoPanel.cs` - 订单信息面板控制器（已创建）
- `SphereOrderDataConfig.asset` - 数据资源（需要创建）
- `SphereInfoPanel.prefab` - 面板预制体（需要创建）

---

## 第一步：创建订单数据配置

### 步骤 1.1：创建ScriptableObject资源

1. **打开Unity编辑器**
2. **在Project窗口选择位置**：
   - 建议位置：`Assets/Resources/` 或 `Assets/Data/`
   - 如果没有这些文件夹，可以在 `Assets/` 下创建 `Resources` 文件夹
3. **创建资源**：
   - 右键点击目标文件夹 → **Create** → **Game** → **Sphere Order Data**
   - 将资源命名为：`SphereOrderDataConfig.asset`

### 步骤 1.2：配置订单数据

1. **选中** `SphereOrderDataConfig.asset`
2. **在Inspector中找到** `Order Data List`
3. **点击** `+` 按钮添加条目（至少添加3个）

#### 📝 配置内容

**Element 0（Sphere1）：**
```
Sphere Name: Sphere1
Order Image: [拖拽订单图片A到这里]
Target Scene Name: scene02
```

**Element 1（Sphere2）：**
```
Sphere Name: Sphere2
Order Image: [拖拽订单图片B到这里]
Target Scene Name: scene03
```

**Element 2（Sphere4）：**
```
Sphere Name: Sphere4
Order Image: [拖拽订单图片C到这里]
Target Scene Name: scene04
```

### ⚠️ 重要提示

- **Sphere Name必须完全匹配**：场景中Sphere GameObject的名称必须与配置中的名称完全一致（区分大小写）
- **场景名称必须存在**：确保所有场景都已添加到 **File → Build Settings → Scenes In Build** 中
- **图片必须已导入**：确保所有订单图片都已导入Unity

---

## 第二步：创建订单信息面板预制体

### 步骤 2.1：确认Canvas

1. **打开包含Sphere的场景**（如 `scene01.unity` 或主界面场景）
2. **检查Hierarchy中是否有Canvas**：
   - 如果已有Canvas（Screen Space - Overlay），直接使用
   - 如果没有，创建Canvas：
     - 右键 → **UI** → **Canvas**
     - 设置 **Render Mode** 为 **Screen Space - Overlay**

### 步骤 2.2：创建面板根对象

1. **在Canvas下创建Panel**：
   - 右键点击 `Canvas` → **UI** → **Panel**
   - 命名为：`SphereInfoPanel`

2. **设置Panel的背景（可选，设为透明）**：
   - 选中 `SphereInfoPanel`
   - 在Inspector中找到 `Image` 组件
   - **Color**: 设为完全透明（A: 0）或保持默认（如果后续要创建独立的Background子对象）
   - 或者可以删除 `Image` 组件（如果不需要背景）

3. **添加脚本**：
   - 选中 `SphereInfoPanel`
   - 点击 **Add Component**
   - 搜索 `SphereInfoPanel`
   - 添加组件

4. **设置RectTransform**：
   - 点击左上角的锚点预设，选择 `Middle Center`
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Width**: `900`
   - **Height**: `700`

### 步骤 2.3：创建背景（可选）

1. **创建背景Image**：
   - 右键点击 `SphereInfoPanel` → **UI** → **Image**
   - 命名为：`Background`

2. **设置Background**：
   - 锚点预设：`Stretch Stretch`
   - **Left/Right/Top/Bottom**: `0`
   - **Color**: R:0, G:0, B:0, A:200（半透明黑色）

### 步骤 2.4：创建订单图片显示区域

1. **创建图片Image**：
   - 右键点击 `SphereInfoPanel` → **UI** → **Image**
   - 命名为：`OrderImage`

2. **设置OrderImage**：
   - 锚点预设：`Middle Center`
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Width**: `800`（固定大小）
   - **Height**: `600`（固定大小）
   - **Image Type**: `Simple`
   - ✅ **Preserve Aspect**: 勾选（保持图片比例）

### 步骤 2.5：创建按钮容器

1. **创建空GameObject**：
   - 右键点击 `SphereInfoPanel` → **Create Empty**
   - 命名为：`ButtonContainer`

2. **设置ButtonContainer**：
   - 锚点预设：`Bottom Center`
   - **Pos X**: `0`
   - **Pos Y**: `50`（距离底部50像素）
   - **Width**: `800`
   - **Height**: `80`

3. **添加Horizontal Layout Group**：
   - 点击 **Add Component**
   - 搜索 `Horizontal Layout Group`
   - 添加组件
   - **Spacing**: `20`
   - **Child Alignment**: `Middle Center`
   - ✅ **Child Control Width**: 勾选
   - ✅ **Child Control Height**: 勾选

### 步骤 2.6：创建关闭按钮

1. **创建按钮**：
   - 右键点击 `ButtonContainer` → **UI** → **Button - TextMeshPro**（或 **Button**）
   - 命名为：`CloseButton`

2. **设置按钮大小**：
   - **Width**: `150`
   - **Height**: `60`

3. **修改按钮文本**：
   - 展开 `CloseButton`
   - 选中子对象 `Text (TMP)` 或 `Text`
   - 在Inspector中将文本改为：`关闭`

### 步骤 2.7：创建跳转场景按钮

1. **创建按钮**：
   - 右键点击 `ButtonContainer` → **UI** → **Button - TextMeshPro**（或 **Button**）
   - 命名为：`JumpButton`

2. **设置按钮大小**：
   - **Width**: `150`
   - **Height**: `60`

3. **修改按钮文本**：
   - 展开 `JumpButton`
   - 选中子对象 `Text (TMP)` 或 `Text`
   - 在Inspector中将文本改为：`前往配送`

### 步骤 2.8：配置SphereInfoPanel脚本引用

1. **选中** `SphereInfoPanel` 根对象
2. **在Inspector中找到** `SphereInfoPanel` 脚本组件
3. **配置引用**：
   - **Panel Image**: 拖拽 `OrderImage` GameObject到此处
   - **Close Button**: 拖拽 `CloseButton` GameObject到此处
   - **Jump Button**: 拖拽 `JumpButton` GameObject到此处
   - ✅ **Enable Debug Log**: 勾选（用于调试）

### 步骤 2.9：设置面板默认隐藏

1. **确保** `SphereInfoPanel` GameObject是**非激活状态**：
   - 在Inspector左上角的复选框**取消勾选**
   - 这样面板在游戏开始时不会显示

### 步骤 2.10：创建预制体

1. **在Project窗口选择保存位置**：
   - 建议位置：`Assets/Prefeb/` 或 `Assets/Prefabs/`

2. **创建预制体**：
   - 将 `SphereInfoPanel` GameObject从Hierarchy**拖拽**到Project窗口
   - Unity会自动创建预制体

3. **清理场景**：
   - 删除Hierarchy中的原始 `SphereInfoPanel` GameObject（保留预制体即可）

---

## 第三步：配置SphereIconManager

### 步骤 3.1：找到SphereIconManager

1. **打开包含Sphere的场景**（如 `scene01.unity`）
2. **在Hierarchy中找到**挂载了 `SphereIconManager.cs` 脚本的GameObject
   - 如果没有，需要创建一个GameObject并添加脚本

### 步骤 3.2：配置Sphere引用（如果未配置）

1. **选中**挂载了 `SphereIconManager` 的GameObject
2. **在Inspector中找到** `SphereIconManager` 脚本
3. **配置Sphere引用**：
   - **Sphere 1**: 拖拽场景中的 `Sphere1` GameObject到此处
   - **Sphere 2**: 拖拽场景中的 `Sphere2` GameObject到此处
   - **Sphere 4**: 拖拽场景中的 `Sphere4` GameObject到此处

### 步骤 3.3：配置订单数据引用

1. **在Inspector中找到** `订单数据配置` 部分
2. **Order Data Config**: 拖拽 `SphereOrderDataConfig.asset` 到此处

### 步骤 3.4：配置面板引用

1. **在Inspector中找到** `面板引用` 部分
2. **Info Panel**: 
   - 如果面板在场景中：拖拽 `SphereInfoPanel` GameObject到此处
   - 如果使用预制体：从Project窗口拖拽 `SphereInfoPanel.prefab` 到此处

### 步骤 3.5：配置其他设置（可选）

- **Icon Prefab**: 确保已配置图标预制体
- **World Space Canvas**: 如果没有，脚本会自动创建
- 其他设置使用默认值即可

---

## 第四步：测试功能

### 步骤 4.1：运行游戏

1. **点击** **Play** 按钮运行游戏
2. **按** **T** 键显示Sphere图标
   - 应该能看到Sphere1、Sphere2、Sphere4的右上角出现图标

### 步骤 4.2：测试点击图标

1. **点击**任意一个Sphere图标（如Sphere1）
2. **应该看到**：
   - ✅ 订单面板显示在屏幕中央
   - ✅ 显示对应Sphere的订单图片
   - ✅ 显示"关闭"和"前往配送"两个按钮

### 步骤 4.3：测试按钮功能

1. **测试关闭按钮**：
   - 点击"关闭"按钮
   - 面板应该隐藏

2. **测试切换功能**：
   - 点击Sphere1的图标 → 显示订单图片A
   - 点击"关闭"按钮
   - 点击Sphere2的图标 → 显示订单图片B（自动关闭之前的面板）

3. **测试跳转功能**：
   - 点击Sphere1的图标
   - 点击"前往配送"按钮
   - 应该跳转到对应的场景（如scene02）

---

## 常见问题排查

### ❌ 问题1：点击图标后没有反应

**可能原因：**
- Sphere名称不匹配
- `orderDataConfig` 未配置
- `infoPanel` 未配置

**解决方法：**
1. 查看Console日志，找到错误信息
2. 确认Sphere GameObject的名称与数据配置中的名称**完全一致**（区分大小写）
3. 确认 `SphereIconManager` 中的 `Order Data Config` 和 `Info Panel` 都已正确配置

---

### ❌ 问题2：面板显示但图片为空

**可能原因：**
- 订单图片未配置
- `Panel Image` 引用未配置

**解决方法：**
1. 检查 `SphereOrderDataConfig.asset` 中每个条目的 `Order Image` 是否已配置
2. 检查 `SphereInfoPanel` 脚本中的 `Panel Image` 引用是否正确

---

### ❌ 问题3：点击"前往配送"按钮报错

**可能原因：**
- 场景名称不存在
- 场景未添加到Build Settings

**解决方法：**
1. 查看Console日志中的错误信息
2. 确认场景名称拼写正确
3. 打开 **File → Build Settings**，确认所有场景都已添加到 **Scenes In Build** 列表中

---

### ❌ 问题4：图标不显示

**可能原因：**
- Icon Prefab未配置
- World Space Canvas未配置

**解决方法：**
1. 检查 `SphereIconManager` 中的 `Icon Prefab` 是否已配置
2. 如果World Space Canvas为空，脚本会自动创建，但需要确认相机引用正确

---

### ❌ 问题5：面板位置不对

**解决方法：**
1. 选中 `SphereInfoPanel` 根对象
2. 调整RectTransform的 **Anchor** 和 **Pos X/Pos Y**
3. 确保面板在屏幕中央（Anchor设置为 `Middle Center`，Pos X/Y为0）

---

## 📝 配置检查清单

完成搭建后，请确认以下项目：

### 数据配置
- [ ] `SphereOrderDataConfig.asset` 已创建
- [ ] 配置了至少3个Sphere的数据（Sphere1、Sphere2、Sphere4）
- [ ] 每个Sphere的 `Sphere Name` 与场景中GameObject名称完全一致
- [ ] 每个Sphere的 `Order Image` 已配置
- [ ] 每个Sphere的 `Target Scene Name` 已配置且场景存在于Build Settings

### UI预制体
- [ ] `SphereInfoPanel` 预制体已创建
- [ ] 面板结构完整（根对象、Background、OrderImage、ButtonContainer、CloseButton、JumpButton）
- [ ] `SphereInfoPanel` 脚本的引用已正确配置（Panel Image、Close Button、Jump Button）
- [ ] 面板默认是隐藏状态

### 脚本配置
- [ ] `SphereIconManager` 的Sphere引用已配置（Sphere1、Sphere2、Sphere4）
- [ ] `SphereIconManager` 的 `Order Data Config` 已配置
- [ ] `SphereIconManager` 的 `Info Panel` 已配置
- [ ] `SphereIconManager` 的 `Icon Prefab` 已配置（如果有）

### 功能测试
- [ ] 运行游戏，按T键可以显示图标
- [ ] 点击图标可以显示订单面板
- [ ] 订单图片正确显示
- [ ] 点击"关闭"按钮可以关闭面板
- [ ] 点击"前往配送"按钮可以跳转到对应场景

---

## 🎉 完成！

现在你的Sphere气泡图标和订单面板系统已经搭建完成！

### 你可以：
- ✅ 按T键显示Sphere图标
- ✅ 点击图标查看订单信息
- ✅ 点击"关闭"按钮关闭面板
- ✅ 点击"前往配送"按钮跳转到对应场景

### 后续扩展：
如果需要添加更多Sphere或修改配置，只需：
1. 在 `SphereOrderDataConfig.asset` 中添加新条目
2. 配置对应的图片和场景名称
3. 无需修改代码！

---

## 📚 相关文件说明

### 已创建的文件（代码）
- `SphereOrderDataConfig.cs` - 数据配置类（ScriptableObject）
- `SphereInfoPanel.cs` - 面板控制器
- `SphereIconManager.cs` - 图标管理器（已修改）
- `SceneTransitionManager.cs` - 场景切换管理器（已修改）

### 需要创建的资源
- `SphereOrderDataConfig.asset` - 数据资源（ScriptableObject）
- `SphereInfoPanel.prefab` - 面板预制体

### 文档说明
- **本指南** - 完整的搭建指南（✅ 保留）
- **旧文档** - 其他订单相关文档可以删除（见下文）

---

如有问题，请检查Console日志中的错误信息，或参考代码中的注释说明。
