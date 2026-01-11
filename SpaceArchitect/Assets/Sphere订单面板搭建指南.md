# Sphere订单面板搭建指南

本指南将帮助你完成Sphere订单信息面板的完整搭建，包括UI创建、数据配置和脚本设置。

---

## 📋 目录

1. [创建SphereOrderDataConfig数据资源](#1-创建sphereorderdataconfig数据资源)
2. [创建SphereInfoPanel预制体](#2-创建sphereinfopanel预制体)
3. [配置SphereIconManager脚本](#3-配置sphereiconmanager脚本)
4. [测试功能](#4-测试功能)
5. [常见问题排查](#5-常见问题排查)

---

## 1. 创建SphereOrderDataConfig数据资源

### 步骤 1.1：创建ScriptableObject资源

1. 在Project窗口中，找到合适的位置（建议：`Assets/Resources/` 或 `Assets/Data/`）
2. 右键点击 → **Create** → **Game** → **Sphere Order Data**
3. 将资源命名为：`SphereOrderDataConfig.asset`

### 步骤 1.2：配置订单数据

1. 选中 `SphereOrderDataConfig.asset`
2. 在Inspector中，你会看到 **Order Data List** 列表
3. 点击 **+** 按钮添加条目（至少添加3个，对应Sphere1、Sphere2、Sphere4）

#### 配置每个条目：

**Element 0（Sphere1）：**
- **Sphere Name**: `Sphere1`（必须与场景中Sphere GameObject的名称完全一致，区分大小写）
- **Order Image**: 拖拽订单图片A到此处
- **Target Scene Name**: `scene02`（目标场景名称，必须在Build Settings中）

**Element 1（Sphere2）：**
- **Sphere Name**: `Sphere2`
- **Order Image**: 拖拽订单图片B到此处
- **Target Scene Name**: `scene03`

**Element 2（Sphere4）：**
- **Sphere Name**: `Sphere4`
- **Order Image**: 拖拽订单图片C到此处
- **Target Scene Name**: `scene04`

### ⚠️ 重要提示

- **Sphere Name必须完全匹配**：确保场景中Sphere GameObject的名称与配置中的名称完全一致（包括大小写）
- **场景名称必须存在**：确保所有配置的场景名称都已添加到 **File → Build Settings → Scenes In Build** 中
- **图片必须已导入**：确保所有订单图片都已导入Unity并正确设置

---

## 2. 创建SphereInfoPanel预制体

### 步骤 2.1：创建面板根对象

1. 在Hierarchy中，找到你的主Canvas（Screen Space - Overlay）
2. 如果还没有Canvas，创建一个：
   - 右键 → **UI** → **Canvas**
   - 设置 **Render Mode** 为 **Screen Space - Overlay**

3. 在Canvas下创建新GameObject：
   - 右键点击Canvas → **Create Empty**
   - 命名为：`SphereInfoPanel`

### 步骤 2.2：设置面板根对象

1. 选中 `SphereInfoPanel`
2. 添加 `SphereInfoPanel.cs` 脚本：
   - 点击 **Add Component**
   - 搜索 `SphereInfoPanel`
   - 添加组件

3. 设置RectTransform：
   - **Anchor**: `Middle Center`（点击左上角的锚点预设）
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Width**: `900`（可根据需要调整）
   - **Height**: `700`（可根据需要调整）

### 步骤 2.3：创建背景（可选）

1. 在 `SphereInfoPanel` 下创建背景：
   - 右键点击 `SphereInfoPanel` → **UI** → **Image**
   - 命名为：`Background`

2. 设置Background：
   - **Anchor**: `Stretch Stretch`（点击左上角的锚点预设）
   - **Left/Right/Top/Bottom**: `0`
   - **Color**: 半透明黑色（如：R:0, G:0, B:0, A:200）

### 步骤 2.4：创建订单图片显示区域

1. 在 `SphereInfoPanel` 下创建图片容器：
   - 右键点击 `SphereInfoPanel` → **UI** → **Image**
   - 命名为：`OrderImage`

2. 设置OrderImage：
   - **Anchor**: `Middle Center`
   - **Pos X**: `0`
   - **Pos Y**: `0`
   - **Width**: `800`（固定大小，可根据需要调整）
   - **Height**: `600`（固定大小，可根据需要调整）
   - **Image Type**: `Simple`
   - **Preserve Aspect**: ✅ 勾选（保持图片比例）

### 步骤 2.5：创建按钮容器

1. 在 `SphereInfoPanel` 下创建按钮容器：
   - 右键点击 `SphereInfoPanel` → **Create Empty**
   - 命名为：`ButtonContainer`

2. 设置ButtonContainer：
   - **Anchor**: `Bottom Center`
   - **Pos X**: `0`
   - **Pos Y**: `50`（距离底部50像素）
   - **Width**: `800`
   - **Height**: `80`

3. 添加 **Horizontal Layout Group** 组件：
   - **Spacing**: `20`（按钮间距）
   - **Child Alignment**: `Middle Center`
   - **Child Control Width**: ✅ 勾选
   - **Child Control Height**: ✅ 勾选

### 步骤 2.6：创建关闭按钮

1. 在 `ButtonContainer` 下创建关闭按钮：
   - 右键点击 `ButtonContainer` → **UI** → **Button - TextMeshPro**（或 **Button**）
   - 命名为：`CloseButton`

2. 设置CloseButton：
   - **Width**: `150`
   - **Height**: `60`
   - 修改按钮文本为：`关闭`

3. 设置按钮颜色（可选）：
   - **Normal Color**: 灰色
   - **Highlighted Color**: 浅灰色
   - **Pressed Color**: 深灰色

### 步骤 2.7：创建跳转场景按钮

1. 在 `ButtonContainer` 下创建跳转按钮：
   - 右键点击 `ButtonContainer` → **UI** → **Button - TextMeshPro**（或 **Button**）
   - 命名为：`JumpButton`

2. 设置JumpButton：
   - **Width**: `150`
   - **Height**: `60`
   - 修改按钮文本为：`前往配送`

3. 设置按钮颜色（可选）：
   - **Normal Color**: 蓝色
   - **Highlighted Color**: 浅蓝色
   - **Pressed Color**: 深蓝色

### 步骤 2.8：配置SphereInfoPanel脚本引用

1. 选中 `SphereInfoPanel`（根对象）
2. 在Inspector中找到 `SphereInfoPanel` 脚本组件
3. 配置引用：
   - **Panel Image**: 拖拽 `OrderImage` GameObject到此处
   - **Close Button**: 拖拽 `CloseButton` GameObject到此处
   - **Jump Button**: 拖拽 `JumpButton` GameObject到此处
   - **Enable Debug Log**: ✅ 勾选（用于调试）

### 步骤 2.9：设置面板默认隐藏

1. 确保 `SphereInfoPanel` GameObject默认是**非激活状态**（Inspector左上角的复选框未勾选）
2. 这样面板在游戏开始时不会显示

### 步骤 2.10：创建预制体

1. 在Project窗口中，找到合适的位置（建议：`Assets/Prefeb/` 或 `Assets/Prefabs/`）
2. 将 `SphereInfoPanel` GameObject从Hierarchy拖拽到Project窗口
3. 删除Hierarchy中的原始GameObject（保留预制体即可）

---

## 3. 配置SphereIconManager脚本

### 步骤 3.1：找到SphereIconManager

1. 在Hierarchy中找到挂载了 `SphereIconManager.cs` 脚本的GameObject
2. 选中该GameObject

### 步骤 3.2：配置订单数据引用

1. 在Inspector中找到 `SphereIconManager` 脚本组件
2. 找到 **订单数据配置** 部分
3. **Order Data Config**: 拖拽 `SphereOrderDataConfig.asset` 到此处

### 步骤 3.3：配置面板引用

1. 在Inspector中找到 **面板引用** 部分
2. **Info Panel**: 
   - 如果面板在场景中：直接拖拽 `SphereInfoPanel` GameObject到此处
   - 如果使用预制体：从Project窗口拖拽 `SphereInfoPanel.prefab` 到此处

---

## 4. 测试功能

### 步骤 4.1：运行游戏

1. 点击 **Play** 按钮
2. 按 **T** 键显示Sphere图标
3. 点击任意一个Sphere图标（Sphere1、Sphere2或Sphere4）

### 步骤 4.2：验证功能

应该看到：
- ✅ 订单面板显示在屏幕中央
- ✅ 显示对应Sphere的订单图片
- ✅ 显示"关闭"和"前往配送"两个按钮
- ✅ 点击"关闭"按钮，面板隐藏
- ✅ 点击"前往配送"按钮，跳转到对应场景

### 步骤 4.3：测试切换

1. 点击Sphere1的图标 → 显示订单图片A
2. 点击"关闭"按钮 → 面板隐藏
3. 点击Sphere2的图标 → 显示订单图片B（自动关闭之前的面板）
4. 点击"前往配送"按钮 → 跳转到scene03

---

## 5. 常见问题排查

### 问题 1：点击图标后没有反应

**可能原因：**
- Sphere名称不匹配
- `orderDataConfig` 未配置
- `infoPanel` 未配置

**解决方法：**
1. 检查Console日志，查看错误信息
2. 确认Sphere GameObject的名称与数据配置中的名称完全一致
3. 确认 `SphereIconManager` 中的 `Order Data Config` 和 `Info Panel` 都已正确配置

---

### 问题 2：面板显示但图片为空

**可能原因：**
- 订单图片未配置
- `Panel Image` 引用未配置

**解决方法：**
1. 检查 `SphereOrderDataConfig.asset` 中每个条目的 `Order Image` 是否已配置
2. 检查 `SphereInfoPanel` 脚本中的 `Panel Image` 引用是否正确

---

### 问题 3：点击"前往配送"按钮报错

**可能原因：**
- 场景名称不存在
- 场景未添加到Build Settings

**解决方法：**
1. 检查Console日志中的错误信息
2. 确认场景名称拼写正确
3. 打开 **File → Build Settings**，确认所有场景都已添加到 **Scenes In Build** 列表中

---

### 问题 4：面板位置不对

**解决方法：**
1. 选中 `SphereInfoPanel` 根对象
2. 调整RectTransform的 **Anchor** 和 **Pos X/Pos Y**
3. 确保面板在屏幕中央（Anchor设置为 `Middle Center`，Pos X/Y为0）

---

### 问题 5：面板大小不合适

**解决方法：**
1. 选中 `SphereInfoPanel` 根对象
2. 调整RectTransform的 **Width** 和 **Height**
3. 选中 `OrderImage`，调整其 **Width** 和 **Height**（固定大小）

---

## 📝 配置检查清单

在完成搭建后，请确认以下项目：

- [ ] `SphereOrderDataConfig.asset` 已创建并配置了3个Sphere的数据
- [ ] 每个Sphere的 `Sphere Name` 与场景中GameObject名称完全一致
- [ ] 每个Sphere的 `Order Image` 已配置
- [ ] 每个Sphere的 `Target Scene Name` 已配置且场景存在于Build Settings
- [ ] `SphereInfoPanel` 预制体已创建
- [ ] `SphereInfoPanel` 脚本的引用已正确配置（Panel Image、Close Button、Jump Button）
- [ ] `SphereIconManager` 脚本的 `Order Data Config` 已配置
- [ ] `SphereIconManager` 脚本的 `Info Panel` 已配置
- [ ] 面板默认是隐藏状态
- [ ] 运行游戏测试，所有功能正常

---

## 🎉 完成！

现在你的Sphere订单面板系统已经搭建完成。你可以：

- 点击Sphere图标查看订单信息
- 点击"关闭"按钮关闭面板
- 点击"前往配送"按钮跳转到对应场景

如果需要添加更多Sphere或修改配置，只需：
1. 在 `SphereOrderDataConfig.asset` 中添加新条目
2. 配置对应的图片和场景名称
3. 无需修改代码！

---

## 📚 相关文件

- `SphereOrderDataConfig.cs` - 数据配置类
- `SphereInfoPanel.cs` - 面板控制器
- `SphereIconManager.cs` - 图标管理器（已修改）
- `SceneTransitionManager.cs` - 场景切换管理器（已修改）

---

如有问题，请检查Console日志中的错误信息，或参考代码中的注释说明。
