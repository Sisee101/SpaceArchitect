# 员工手册多层级弹窗系统 UI 搭建指南

## 概述

本指南将帮助你搭建员工手册的多层级弹窗系统，包括：
- **第一层级**：三个选项卡片（入职指南、业务流程、系统架构）
- **第二层级**：图片浏览界面（支持左右箭头切换，显示页码）

## 一、创建数据配置资源

### 1.1 创建 ScriptableObject 资源

**方法一：通过 Tools 菜单创建（最简单，推荐）**
1. 确保 `EmployeeHandbookDataConfig.cs` 脚本已经编译完成（没有错误）
2. 在 Unity 顶部菜单栏，点击 `Tools`
3. 选择 `Create Employee Handbook Data`
4. 资源文件会自动创建在 `Assets/Data/EmployeeHandbookData.asset`
5. 如果 `Assets/Data` 文件夹不存在，会自动创建

**方法二：通过右键菜单创建**
1. 确保 `EmployeeHandbookDataConfig.cs` 脚本已经编译完成（没有错误）
2. 在 Project 窗口中，**右键点击空白区域**（不是点击文件，而是点击文件夹内的空白处）
3. 选择 `Create > Game Data > Employee Handbook Data`
4. 将创建的资源文件重命名为 `EmployeeHandbookData.asset`（如果需要）

**如果以上方法都不行，请检查：**
- Unity Console 中是否有编译错误
- `EmployeeHandbookDataConfig.cs` 脚本是否在正确的文件夹中（`Assets/Scripts/UI/`）
- `CreateEmployeeHandbookData.cs` Editor 脚本是否在 `Assets/Editor/` 文件夹中
- 尝试重新编译项目（`Assets > Reimport All` 或 `Ctrl+R`）
- 等待 Unity 编译完成（查看右下角的编译进度）

### 1.2 配置选项和图片

1. 选中 `EmployeeHandbookData.asset` 资源文件
2. 在 Inspector 中，展开 `Sections` 列表
3. 点击 `+` 按钮添加三个选项（至少需要3个）：
   - **选项 0**：`Section Name` = "入职指南"，添加对应的图片到 `Images` 列表
   - **选项 1**：`Section Name` = "业务流程"，添加对应的图片到 `Images` 列表
   - **选项 2**：`Section Name` = "系统架构"，添加对应的图片到 `Images` 列表
4. 每个选项的 `Images` 列表中可以添加多张图片（建议10-20张），按顺序排列

**注意**：图片数量可以不同，但至少需要1张图片。

## 二、UI 层级结构搭建

### 2.1 主容器结构

在场景中创建以下层级结构：

```
EmployeeHandbookPanel (GameObject)
├── Canvas (Canvas) [如果还没有Canvas]
│   └── EmployeeHandbookPanel (GameObject)
│       ├── OptionListView (GameObject) - 选项列表视图
│       │   ├── OptionCard1 (Button) - 入职指南卡片
│       │   ├── OptionCard2 (Button) - 业务流程卡片
│       │   └── OptionCard3 (Button) - 系统架构卡片
│       │
│       └── ImageView (GameObject) - 图片浏览视图
│           ├── BackButton (Button) - 返回按钮
│           ├── ImageDisplay (Image) - 图片显示区域
│           ├── LeftArrow (Button) - 左箭头
│           ├── RightArrow (Button) - 右箭头
│           └── PageIndicator (Text) - 页码指示器（可选）
```

### 2.2 创建主容器

1. 在 Hierarchy 中找到 `EmployeeHandbookPanel` GameObject（如果不存在，创建一个新的）
2. 确保该 GameObject 有 `RectTransform` 组件（会自动添加）
3. 设置 `RectTransform`：
   - `Anchors`: 拉伸到全屏（按住 Alt + Shift 点击右下角的锚点）
   - `Left/Right/Top/Bottom`: 0

### 2.3 创建选项列表视图（OptionListView）

1. 在 `EmployeeHandbookPanel` 下创建子 GameObject，命名为 `OptionListView`
2. 添加 `RectTransform` 组件（自动添加）
3. 设置 `RectTransform`：
   - `Anchors`: 拉伸到全屏
   - `Left/Right/Top/Bottom`: 0
4. 添加 `HandbookOptionController` 脚本组件

#### 创建三个选项卡片

**选项卡片 1（入职指南）：**
1. 在 `OptionListView` 下创建子 GameObject，命名为 `OptionCard1`
2. 添加 `Button` 组件
3. 添加 `Image` 组件作为背景（可选）
4. 在 `OptionCard1` 下创建子 GameObject，命名为 `Icon`，添加 `Image` 组件用于显示图标（可选）
5. 在 `OptionCard1` 下创建子 GameObject，命名为 `Title`，添加 `Text` 组件，设置文本为 "-入职指南-"
6. 在 `OptionCard1` 下创建子 GameObject，命名为 `EnterButton`，添加 `Button` 组件和 `Text` 子对象，设置文本为 "立即进入"

**选项卡片 2（业务流程）：**
- 重复上述步骤，命名为 `OptionCard2`，标题为 "-业务流程-"

**选项卡片 3（系统架构）：**
- 重复上述步骤，命名为 `OptionCard3`，标题为 "-系统架构-"

**布局建议：**
- 三个卡片可以水平排列，使用 `Horizontal Layout Group` 组件
- 或者手动设置 `RectTransform` 的 `Anchored Position` 和 `Size Delta`

### 2.4 创建图片浏览视图（ImageView）

1. 在 `EmployeeHandbookPanel` 下创建子 GameObject，命名为 `ImageView`
2. 设置 `RectTransform`：
   - `Anchors`: 拉伸到全屏
   - `Left/Right/Top/Bottom`: 0
3. **初始状态设置为不激活**（取消勾选 Inspector 顶部的复选框）
4. 添加 `HandbookImageViewer` 脚本组件

#### 创建图片显示区域

1. 在 `ImageView` 下创建子 GameObject，命名为 `ImageDisplay`
2. 添加 `Image` 组件
3. 设置 `RectTransform`：
   - `Anchors`: 居中
   - `Width`: 根据需要设置（如 800）
   - `Height`: 根据需要设置（如 600）
   - `Image Type`: `Simple`
   - `Preserve Aspect`: 勾选（保持图片比例）

#### 创建左右箭头按钮

**左箭头：**
1. 在 `ImageView` 下创建子 GameObject，命名为 `LeftArrow`
2. 添加 `Button` 组件
3. 添加 `Image` 组件，设置箭头图标（Sprite）
4. 设置 `RectTransform`：
   - `Anchors`: 左中
   - `Position`: 根据需要调整（如 X: 50, Y: 0）
   - `Width/Height`: 根据需要设置（如 60x60）

**右箭头：**
1. 在 `ImageView` 下创建子 GameObject，命名为 `RightArrow`
2. 添加 `Button` 组件
3. 添加 `Image` 组件，设置箭头图标（Sprite）
4. 设置 `RectTransform`：
   - `Anchors`: 右中
   - `Position`: 根据需要调整（如 X: -50, Y: 0）
   - `Width/Height`: 根据需要设置（如 60x60）

#### 创建返回按钮

1. 在 `ImageView` 下创建子 GameObject，命名为 `BackButton`
2. 添加 `Button` 组件
3. 添加 `Image` 组件作为背景（可选）
4. 在 `BackButton` 下创建子 GameObject，命名为 `Text`，添加 `Text` 组件，设置文本为 "返回"
5. 设置 `RectTransform`：
   - `Anchors`: 根据需要设置（如左上角）
   - `Position`: 根据需要调整

#### 创建页码指示器（可选）

1. 在 `ImageView` 下创建子 GameObject，命名为 `PageIndicator`
2. 添加 `Text` 组件
3. 设置文本为 "1/15"（占位）
4. 设置字体大小和颜色
5. 设置 `RectTransform`：
   - `Anchors`: 根据需要设置（如底部居中）
   - `Position`: 根据需要调整

## 三、配置脚本组件

### 3.1 配置 EmployeeHandbookPanel

1. 选中 `EmployeeHandbookPanel` GameObject
2. 在 Inspector 中找到 `Employee Handbook Panel` 组件
3. 配置以下字段：
   - **Option List View**: 将 `OptionListView` GameObject 拖拽到此字段
   - **Image View**: 将 `ImageView` GameObject 拖拽到此字段
   - **Option Controller**: 将 `OptionListView` GameObject 拖拽到此字段（它会自动获取 `HandbookOptionController` 组件）
   - **Image Viewer**: 将 `ImageView` GameObject 拖拽到此字段（它会自动获取 `HandbookImageViewer` 组件）
   - **Data Config**: 将 `EmployeeHandbookData.asset` 资源文件拖拽到此字段

### 3.2 配置 HandbookOptionController

1. 选中 `OptionListView` GameObject
2. 在 Inspector 中找到 `Handbook Option Controller` 组件
3. 配置以下字段：
   - **Option Buttons**: 
     - `Size`: 3
     - `Element 0`: 将 `OptionCard1` 拖拽到此
     - `Element 1`: 将 `OptionCard2` 拖拽到此
     - `Element 2`: 将 `OptionCard3` 拖拽到此
   - **Option Icons**（可选）: 如果创建了图标，可以配置
   - **Option Titles**（可选）: 如果创建了标题文本，可以配置

**注意**：如果选项卡片的结构是按钮内部有子按钮（如"立即进入"按钮），需要确保：
- 选项卡片本身（`OptionCard1/2/3`）有 `Button` 组件
- 或者将"立即进入"按钮的点击事件绑定到选项卡片

### 3.3 配置 HandbookImageViewer

1. 选中 `ImageView` GameObject
2. 在 Inspector 中找到 `Handbook Image Viewer` 组件
3. 配置以下字段：
   - **Image Display Object**（推荐）: 将 `ImageDisplay` GameObject 拖拽到此字段（代码会自动获取Image组件）
   - **Current Image Display**（备选）: 如果上面的方法不行，可以直接将 `ImageDisplay` GameObject 的 `Image` 组件拖拽到此字段
   - **Left Arrow Button**: 将 `LeftArrow` GameObject 拖拽到此字段
   - **Right Arrow Button**: 将 `RightArrow` GameObject 拖拽到此字段
   - **Back Button**: 将 `BackButton` GameObject 拖拽到此字段
   - **Page Indicator**（可选）: 将 `PageIndicator` GameObject 拖拽到此字段
   - **Image Switch Duration**: 设置图片切换动画时长（如 0.3 秒）

**注意**：
- 优先使用 `Image Display Object` 字段，直接拖入 GameObject 即可
- 如果 `Image Display Object` 无法拖入，可以尝试使用 `Current Image Display` 字段，但需要确保 `ImageDisplay` GameObject 有 `Image` 组件
- 代码会在运行时自动从 GameObject 获取 Image 组件

## 四、测试和调试

### 4.1 运行测试

1. 运行游戏
2. 点击主界面的"员工手册"按钮
3. 应该看到三个选项卡片
4. 点击任意选项卡片，应该切换到图片浏览界面
5. 点击左右箭头，应该能够切换图片
6. 在第一张图片时，左箭头应该隐藏
7. 在最后一张图片时，右箭头应该隐藏
8. 点击返回按钮，应该返回到选项列表
9. 在选项列表时，点击返回按钮（如果有），应该关闭整个弹窗

### 4.2 常见问题排查

**问题1：点击选项卡片没有反应**
- 检查 `HandbookOptionController` 的 `Option Buttons` 是否正确配置
- 检查选项卡片是否有 `Button` 组件
- 检查按钮是否被其他UI元素遮挡

**问题2：图片不显示**
- 检查 `EmployeeHandbookDataConfig` 是否正确配置了图片
- 检查 `ImageDisplay` 的 `Image` 组件是否正确配置
- 检查图片资源的格式是否正确（建议使用 Sprite 格式）

**问题3：箭头不显示/隐藏**
- 检查 `HandbookImageViewer` 的箭头按钮引用是否正确
- 检查箭头按钮的初始状态（应该在 Inspector 中可见）
- 查看 Console 是否有错误信息

**问题4：返回按钮不工作**
- 检查 `HandbookImageViewer` 的 `Back Button` 引用是否正确
- 检查返回按钮是否有 `Button` 组件

## 五、样式和美化建议

### 5.1 选项卡片样式
- 使用半透明背景，添加边框和阴影效果
- 添加悬停效果（高亮、缩放等）
- 图标和标题使用合适的字体和颜色

### 5.2 图片浏览界面样式
- 图片显示区域使用合适的背景色或边框
- 箭头按钮使用清晰的图标，添加悬停效果
- 页码指示器使用合适的字体大小和颜色

### 5.3 动画效果
- 图片切换可以使用淡入淡出动画（已在代码中实现）
- 选项卡片点击可以使用缩放或高亮动画
- 视图切换可以使用滑动或淡入淡出动画

## 六、扩展功能（可选）

### 6.1 添加音效
- 在 `HandbookOptionController` 中添加按钮点击音效
- 在 `HandbookImageViewer` 中添加箭头点击音效和图片切换音效

### 6.2 添加键盘快捷键
- 左右方向键切换图片
- ESC 键返回上一层级

### 6.3 添加图片缩放功能
- 双击图片放大/缩小
- 使用滚轮缩放

## 七、完成检查清单

- [ ] 创建了 `EmployeeHandbookData.asset` 资源文件
- [ ] 配置了三个选项的图片列表
- [ ] 创建了 `OptionListView` 和三个选项卡片
- [ ] 创建了 `ImageView` 和所有子元素
- [ ] 配置了 `EmployeeHandbookPanel` 的所有引用
- [ ] 配置了 `HandbookOptionController` 的所有引用
- [ ] 配置了 `HandbookImageViewer` 的所有引用
- [ ] 测试了选项点击功能
- [ ] 测试了图片切换功能
- [ ] 测试了返回按钮功能
- [ ] 测试了箭头显示/隐藏逻辑

完成以上步骤后，员工手册多层级弹窗系统应该可以正常工作了！
