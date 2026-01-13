# 订单详情UI搭建步骤指南（结算界面）

本指南将帮助你搭建**结算界面**的UI结构，包括滑动列表和印章动画。

---

## 📋 功能说明

### 实现的功能

1. ✅ **结算界面触发**：在主界面（01_MainHub）按下空格键显示结算界面
2. ✅ **左侧订单图片滑动列表**：通过箭头按钮切换订单图片
3. ✅ **右上文字图片显示**：结算界面显示时，按A键切换8张文字图片
4. ✅ **右下动态文字显示**：显示订单ID（占位）
5. ✅ **印章动画**：印章逐渐出现的动画效果
6. ✅ **关闭界面**：按ESC键关闭结算界面

### 界面层级

- **结算界面应该放在 `MainHubCanvas` 下**
- **初始状态：隐藏**（Inspector左上角取消勾选）
- **显示时机**：在主界面按下空格键后显示

---

## 🏗️ UI搭建步骤

### 步骤1：找到MainHubCanvas并创建结算界面Panel

1. **找到MainHubCanvas**：
   - 在Hierarchy中找到 `01_MainHub` → `MainHubCanvas`
   - 如果不存在，需要先创建Canvas

2. **创建结算界面Panel**：
   - 右键 `MainHubCanvas` → **UI → Panel**
   - 命名为：`SettlementPanel` 或 `OrderDetailPanel`
   - 设置 **Rect Transform**：
     - **Anchor**: `Stretch Stretch`（全屏）
     - **Left/Right/Top/Bottom**: `0`

3. **配置Panel背景**（可选）：
   - 在 **Image** 组件中设置背景颜色或图片
   - 建议使用半透明背景，覆盖在主界面上方

4. **初始隐藏面板**：
   - 选中 `SettlementPanel`（或 `OrderDetailPanel`）
   - 在Inspector左上角**取消勾选**（设置为非激活状态）
   - ⚠️ **重要**：结算界面初始必须隐藏，通过代码显示

---

### 步骤2：创建左侧订单图片列表

#### 2.1 创建LeftPanel

1. **创建Panel**：
   - 右键 `OrderDetailPanel` → **UI → Panel**
   - 命名为：`LeftPanel`

2. **设置LeftPanel的Rect Transform**：
   - **Anchor**: `Left Stretch`
   - **Left**: `0`
   - **Right**: `60%`（或设置Width为屏幕宽度的40%）
   - **Top**: `0`
   - **Bottom**: `0`

#### 2.2 创建ScrollRect

1. **创建ScrollRect**：
   - 右键 `LeftPanel` → **UI → Scroll View**
   - 自动创建：`Scroll View` GameObject
   - 重命名为：`OrderScrollRect`

2. **配置ScrollRect组件**：
   - 选中 `OrderScrollRect`
   - 在 **Scroll Rect** 组件中：
     - **Movement Type**: `Elastic` 或 `Clamped`
     - **Scroll Sensitivity**: `10`
     - **Horizontal**: ✅ 勾选
     - **Vertical**: ❌ 取消勾选
     - **Viewport**: 拖拽 `Viewport` GameObject
     - **Content**: 拖拽 `Content` GameObject
     - **Horizontal Scrollbar**: 留空（或隐藏Scrollbar）

3. **隐藏Scrollbar**（可选）：
   - 选中 `Scrollbar Horizontal` GameObject
   - 在Inspector左上角取消勾选（隐藏）

#### 2.3 配置Content

1. **选中Content GameObject**：
   - 展开 `OrderScrollRect` → `Viewport` → `Content`

2. **设置Content的Rect Transform**：
   - **Anchor**: `Left Center`
   - **Pivot**: `(0, 0.5)`（左中心）
   - **Width**: `1240`（3张图片 × 400 + 2个间距 × 20）
   - **Height**: 与Viewport高度一致（或设置固定值，如600）

3. **添加Horizontal Layout Group**：
   - 选中 `Content`
   - 点击 **Add Component**
   - 搜索并添加：`Horizontal Layout Group`
   - 配置参数：
     - **Spacing**: `20`（图片间距）
     - **Child Alignment**: `Middle Left`
     - ✅ **Child Control Width**: 勾选
     - ✅ **Child Control Height**: 勾选
     - ✅ **Child Force Expand Width**: 勾选
     - ✅ **Child Force Expand Height**: 勾选

#### 2.4 创建订单图片项

1. **创建第一个订单图片**：
   - 右键 `Content` → **UI → Image**
   - 命名为：`OrderImageItem1`

2. **设置OrderImageItem1的Rect Transform**：
   - **Width**: `400`
   - **Height**: `600`
   - **Pos X/Y**: `0`（Layout Group会自动排列）

3. **配置Image组件**：
   - **Image Type**: `Simple`
   - ✅ **Preserve Aspect**: 勾选（保持图片比例）
   - **Source Image**: 先留空，后续配置占位图片
   - ❌ **Raycast Target**: 取消勾选（如果不需要点击）

4. **重复创建另外两个**：
   - 创建 `OrderImageItem2`
   - 创建 `OrderImageItem3`
   - 配置相同

#### 2.5 创建箭头按钮

1. **创建左箭头按钮**：
   - 右键 `LeftPanel` → **UI → Button**
   - 命名为：`LeftArrowButton`
   - 设置位置：
     - **Anchor**: `Left Center`
     - **Pos X**: `20`（距离左边缘20像素）
     - **Pos Y**: `0`
     - **Width**: `50`
     - **Height**: `50`

2. **创建右箭头按钮**：
   - 右键 `LeftPanel` → **UI → Button**
   - 命名为：`RightArrowButton`
   - 设置位置：
     - **Anchor**: `Right Center`
     - **Pos X**: `-20`（距离右边缘20像素）
     - **Pos Y**: `0`
     - **Width**: `50`
     - **Height**: `50`

3. **配置箭头按钮外观**（可选）：
   - 修改按钮文本或图标
   - 调整按钮样式

---

### 步骤3：创建右上文字图片区域

1. **创建Panel**：
   - 右键 `OrderDetailPanel` → **UI → Panel**
   - 命名为：`TopRightPanel`

2. **设置TopRightPanel的Rect Transform**：
   - **Anchor**: `Top Right`
   - **Pos X**: `-50`（距离右边缘50像素）
   - **Pos Y**: `-50`（距离上边缘50像素）
   - **Width**: `400`
   - **Height**: `300`

3. **创建文字图片显示**：
   - 右键 `TopRightPanel` → **UI → Image**
   - 命名为：`TextImageDisplay`

4. **设置TextImageDisplay的Rect Transform**：
   - **Anchor**: `Stretch Stretch`（填满父对象）
   - **Left/Right/Top/Bottom**: `0`

5. **配置Image组件**：
   - **Image Type**: `Simple`
   - ✅ **Preserve Aspect**: 勾选
   - **Source Image**: 先留空，后续配置占位图片
   - ❌ **Raycast Target**: 取消勾选

---

### 步骤4：创建右下动态文字区域

1. **创建Panel**：
   - 右键 `OrderDetailPanel` → **UI → Panel**
   - 命名为：`BottomRightPanel`

2. **设置BottomRightPanel的Rect Transform**：
   - **Anchor**: `Bottom Right`
   - **Pos X**: `-50`（距离右边缘50像素）
   - **Pos Y**: `50`（距离下边缘50像素）
   - **Width**: `400`
   - **Height**: `500`

3. **创建文字组件**（二选一）：
   
   **选项A：使用TextMeshPro**（推荐）
   - 右键 `BottomRightPanel` → **UI → Text - TextMeshPro**
   - 命名为：`DynamicText`
   
   **选项B：使用传统Text**
   - 右键 `BottomRightPanel` → **UI → Text**
   - 命名为：`DynamicText`

4. **设置DynamicText的Rect Transform**：
   - **Anchor**: `Stretch Stretch`
   - **Left/Right/Top/Bottom**: `20`（留出边距）

5. **配置文字组件**：
   - **Text**: `订单ID: 0`（占位文字）
   - **Font Size**: `24`（根据设计调整）
   - **Alignment**: 根据设计调整（左对齐、居中、右对齐）
   - **Color**: 根据设计调整

---

### 步骤5：创建印章动画面板

1. **创建Panel**：
   - 右键 `OrderDetailPanel` → **UI → Panel**
   - 命名为：`StampPanel`

2. **设置StampPanel的Rect Transform**：
   - **Anchor**: `Middle Center`（居中）
   - **Pos X/Y**: `0`
   - **Width**: `600`（根据印章图片大小调整）
   - **Height**: `200`（根据印章图片大小调整）

3. **初始隐藏面板**：
   - 选中 `StampPanel`
   - 在Inspector左上角**取消勾选**（设置为非激活状态）

4. **创建印章图片**：
   - 右键 `StampPanel` → **UI → Image**
   - 命名为：`StampImage`

5. **设置StampImage的Rect Transform**：
   - **Anchor**: `Stretch Stretch`
   - **Left/Right/Top/Bottom**: `0`

6. **配置Image组件**：
   - **Image Type**: `Simple`
   - ✅ **Preserve Aspect**: 勾选
   - **Source Image**: 拖拽印章图片（"行星图鉴已更新"）
   - ❌ **Raycast Target**: 取消勾选

---

### 步骤6：添加脚本组件

#### 6.1 添加OrderDetailPanel脚本

1. **选中SettlementPanel（或OrderDetailPanel）GameObject**
2. **添加脚本**：
   - 点击 **Add Component**
   - 搜索 `Order Detail Panel`
   - 添加组件

3. **配置引用**：
   - **Order Image List**: 拖拽 `LeftPanel` GameObject（或OrderImageListController组件）
   - **Text Image Controller**: 拖拽 `TopRightPanel` GameObject（或TextImageController组件）
   - **Dynamic Text Controller**: 拖拽 `BottomRightPanel` GameObject（或DynamicTextController组件）
   - **Stamp Animation**: 拖拽 `StampPanel` GameObject（或StampAnimationController组件）

#### 6.6 配置MainHubController（重要）

1. **找到MainHubController GameObject**：
   - 在Hierarchy中查找包含 `Main Hub Controller` 脚本的GameObject

2. **配置结算界面引用**：
   - 选中MainHubController GameObject
   - 在Inspector的 **Main Hub Controller** 组件中：
     - 展开 "结算界面" 部分
     - **Settlement Panel**: 拖拽 `SettlementPanel`（或 `OrderDetailPanel`）GameObject到此处

3. **验证配置**：
   - 确保Settlement Panel引用已配置
   - 确保结算界面初始隐藏（Inspector左上角取消勾选）

#### 6.2 添加OrderImageListController脚本

1. **选中LeftPanel GameObject**（或创建新的GameObject命名为`OrderImageListController`）
2. **添加脚本**：
   - 点击 **Add Component**
   - 搜索 `Order Image List Controller`
   - 添加组件

3. **配置引用**：
   - **Scroll Rect**: 拖拽 `OrderScrollRect` GameObject
   - **Content**: 拖拽 `Content` GameObject
   - **Left Arrow**: 拖拽 `LeftArrowButton` GameObject
   - **Right Arrow**: 拖拽 `RightArrowButton` GameObject

4. **配置订单图片项**：
   - **Order Image Items**: 
     - Size: `3`
     - Element 0: 拖拽 `OrderImageItem1`
     - Element 1: 拖拽 `OrderImageItem2`
     - Element 2: 拖拽 `OrderImageItem3`

5. **配置占位图片**：
   - **Placeholder Image 1**: 拖拽占位图片1
   - **Placeholder Image 2**: 拖拽占位图片2
   - **Placeholder Image 3**: 拖拽占位图片3

6. **配置布局参数**（可选调整）：
   - **Image Width**: `400`
   - **Image Height**: `600`
   - **Image Spacing**: `20`
   - **Scroll Duration**: `0.3`

#### 6.3 添加TextImageController脚本

1. **选中TopRightPanel GameObject**（或创建新的GameObject）
2. **添加脚本**：
   - 点击 **Add Component**
   - 搜索 `Text Image Controller`
   - 添加组件

3. **配置引用**：
   - **Text Image Display**: 拖拽 `TextImageDisplay` GameObject

4. **配置文字图片列表**：
   - **Text Image List**:
     - Size: `8`
     - Element 0-7: 拖拽8张占位图片（或先留空，后续配置）

#### 6.4 添加DynamicTextController脚本

1. **选中BottomRightPanel GameObject**（或创建新的GameObject）
2. **添加脚本**：
   - 点击 **Add Component**
   - 搜索 `Dynamic Text Controller`
   - 添加组件

3. **配置引用**（二选一）：
   - **Dynamic Text**: 如果使用传统Text，拖拽 `DynamicText` GameObject
   - **Dynamic Text TMP**: 如果使用TextMeshPro，拖拽 `DynamicText` GameObject

4. **配置占位文字**：
   - **Placeholder Text**: `订单ID: 0`
   - **Order Id Format**: `订单ID: {0}`

#### 6.5 添加StampAnimationController脚本

1. **选中StampPanel GameObject**
2. **添加脚本**：
   - 点击 **Add Component**
   - 搜索 `Stamp Animation Controller`
   - 添加组件

3. **配置引用**：
   - **Stamp Panel**: 拖拽 `StampPanel` GameObject
   - **Stamp Image**: 拖拽 `StampImage` GameObject

4. **配置动画参数**（可选调整）：
   - **Animation Duration**: `1.0`（动画时长）
   - **Scale Ease**: `OutBack`（缩放缓动）
   - **Fade Ease**: `OutQuad`（淡入缓动）

---

## 📝 配置占位内容

### 占位图片准备

1. **订单图片占位**（3张）：
   - 创建或导入3张占位图片
   - 导入设置：Texture Type = `Sprite (2D and UI)`
   - 命名建议：`PlaceholderOrderImage1`, `PlaceholderOrderImage2`, `PlaceholderOrderImage3`

2. **文字图片占位**（8张）：
   - 创建或导入8张占位图片
   - 导入设置：Texture Type = `Sprite (2D and UI)`
   - 命名建议：`PlaceholderTextImage1` 到 `PlaceholderTextImage8`

3. **印章图片**：
   - 导入印章图片（"行星图鉴已更新"）
   - 导入设置：Texture Type = `Sprite (2D and UI)`

### 配置占位内容

1. **配置订单图片占位**：
   - 在OrderImageListController的Inspector中
   - 拖拽占位图片到 `Placeholder Image 1/2/3` 字段

2. **配置文字图片占位**：
   - 在TextImageController的Inspector中
   - 在 `Text Image List` 中添加8个元素
   - 拖拽占位图片到对应元素

3. **配置印章图片**：
   - 在StampImage的Inspector中
   - 拖拽印章图片到 `Source Image` 字段

---

## 🧪 测试步骤

### 测试1：结算界面显示/隐藏

1. **运行场景**（01_MainHub场景）
2. **按空格键**：
   - ✅ 结算界面应该显示
   - ✅ 覆盖在主界面上方
   - ⚠️ **如果未显示**：检查Console是否有错误，确认MainHubController的Settlement Panel引用已配置
3. **按A键**（在结算界面显示时）：
   - ✅ 右上文字图片应该切换到下一张
4. **按ESC键**：
   - ✅ 结算界面应该隐藏
   - ✅ 返回主界面

### 测试2：订单图片列表

1. **显示结算界面**（按空格键）
2. **点击左箭头**：
   - ✅ 应该向左滚动
   - ✅ 当前索引应该减少
3. **点击右箭头**：
   - ✅ 应该向右滚动
   - ✅ 当前索引应该增加
4. **鼠标滚轮**：
   - ✅ 应该可以滚动列表

### 测试3：文字图片切换

1. **显示结算界面**（按空格键）
2. **按A键**：
   - ✅ 右上文字图片应该切换到下一张
   - ✅ 第8张后应该循环回到第1张
   - ⚠️ **注意**：只有在结算界面显示时，A键才切换文字图片

### 测试4：动态文字

1. **显示结算界面**（按空格键）
2. **切换订单**：
   - ✅ 右下文字应该更新为当前订单ID

### 测试5：印章动画

1. **显示结算界面**（按空格键）
2. **调用动画**（在代码中调用或添加测试按钮）：
   - ✅ 印章面板应该显示
   - ✅ 印章应该逐渐出现（缩放+淡入）
   - ✅ 动画应该平滑

---

## ✅ 配置检查清单

### UI结构
- [ ] MainHubCanvas已找到（或已创建）
- [ ] SettlementPanel（或OrderDetailPanel）已创建（全屏背景）
- [ ] SettlementPanel初始隐藏（Inspector左上角取消勾选）
- [ ] LeftPanel已创建（订单列表区域）
- [ ] OrderScrollRect已创建并配置
- [ ] Content已创建并配置Horizontal Layout Group
- [ ] 3个OrderImageItem已创建（Image组件）
- [ ] 左右箭头按钮已创建
- [ ] TopRightPanel已创建（文字图片区域）
- [ ] TextImageDisplay已创建（Image组件）
- [ ] BottomRightPanel已创建（文字区域）
- [ ] DynamicText已创建（Text/TextMeshPro）
- [ ] StampPanel已创建（初始隐藏）
- [ ] StampImage已创建（Image组件）

### 脚本配置
- [ ] OrderDetailPanel脚本已添加并配置引用
- [ ] MainHubController脚本已配置Settlement Panel引用
- [ ] OrderImageListController脚本已添加并配置引用
- [ ] TextImageController脚本已添加并配置引用
- [ ] DynamicTextController脚本已添加并配置引用
- [ ] StampAnimationController脚本已添加并配置引用

### 占位内容
- [ ] 3张订单占位图片已配置
- [ ] 8张文字占位图片已配置
- [ ] 印章图片已配置
- [ ] 占位文字已配置

---

## 🔌 后续代码接入示例

### 替换订单图片
```csharp
// 获取订单图片列表
List<Sprite> orderImages = GetOrderImagesFromConfig();
orderImageListController.SetOrderImages(orderImages);
```

### 替换文字图片
```csharp
// 获取文字图片列表
List<Sprite> textImages = LoadTextImages();
textImageController.SetTextImages(textImages);
```

### 更新订单ID
```csharp
// 更新订单ID
dynamicTextController.SetOrderId(currentOrderId);
```

### 播放印章动画
```csharp
// 播放印章动画
stampAnimationController.PlayStampAnimation(() => {
    Debug.Log("印章动画完成");
});
```

---

## ⚠️ 注意事项

1. **界面层级**：
   - ⚠️ **重要**：结算界面必须放在 `MainHubCanvas` 下
   - ⚠️ **重要**：结算界面初始必须隐藏（Inspector左上角取消勾选）
   - 结算界面会覆盖在主界面上方

2. **按键处理**：
   - 在主界面：按空格键显示结算界面
   - 在结算界面：按A键切换文字图片
   - 按ESC键：关闭结算界面

3. **DOTween依赖**：
   - 确保项目中已导入DOTween
   - 如果未导入，需要安装DOTween插件

4. **TextMeshPro依赖**：
   - 如果使用TextMeshPro，确保已导入TextMeshPro包
   - 如果使用传统Text，可以忽略

5. **布局调整**：
   - 所有尺寸和位置参数都可以在Inspector中调整
   - 根据实际设计需求调整布局

6. **占位内容**：
   - 当前使用占位图片和文字
   - 后续只需要调用接口方法替换即可

7. **MainHubController配置**：
   - ⚠️ **必须配置**：在MainHubController的Inspector中配置Settlement Panel引用
   - 否则空格键无法显示结算界面

---

完成以上步骤后，UI结构就搭建完成了！后续开发者只需要调用提供的接口方法即可接入真实数据。
