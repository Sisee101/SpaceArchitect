# 解决UI遮挡问题指南

## 问题描述

`MainHubPanel` 遮挡了 Sphere 图标，导致点击图标无法触发事件。

## 🔍 问题原因

- **WorldSpaceCanvas**（3D空间图标）和 **MainHubCanvas**（2D UI面板）是两个不同的Canvas
- `MainHubPanel` 在 `MainHubCanvas` 上，其 `Image` 组件的 `Raycast Target` 默认是开启的
- 即使图标在3D空间中可见，如果 `MainHubPanel` 拦截了鼠标射线，点击事件也无法到达图标

## ✅ 解决方案

### 方案 1：禁用 MainHubPanel 的 Raycast Target（推荐）

**优点：**
- 最简单快速
- 不影响按钮等其他UI元素的点击
- 面板仍然显示，只是不拦截点击事件

**操作步骤：**

1. **选中 MainHubPanel**：
   - 在 Hierarchy 中找到 `MainHubCanvas` → `MainHubPanel`
   - 选中它

2. **禁用 Raycast Target**：
   - 在 Inspector 中找到 `Image` 组件
   - **取消勾选** `Raycast Target` ✅
   - 这样面板不会拦截点击事件，但按钮等子元素仍可正常点击

3. **保存场景**：
   - 保存场景（Ctrl+S）

4. **测试**：
   - 运行游戏
   - 按 T 键显示图标
   - 点击图标，应该可以正常触发了

---

### 方案 2：调整 Canvas 的 Sort Order

**适用场景：**
- 如果禁用 Raycast Target 后还有其他问题
- 需要确保 WorldSpaceCanvas 的优先级更高

**操作步骤：**

1. **选中 WorldSpaceCanvas**：
   - 在 Hierarchy 中找到 `WorldSpaceCanvas`
   - 选中它

2. **提高 Sort Order**：
   - 在 Inspector 中找到 `Canvas` 组件
   - **Sort Order**: 设为 `10`（或比 MainHubCanvas 更高的值）

3. **检查 MainHubCanvas**：
   - 选中 `MainHubCanvas`
   - 确认 `Sort Order` 是 `0`（默认值）
   - 如果有值，确保 WorldSpaceCanvas 的值更大

4. **保存场景**：
   - 保存场景

---

### 方案 3：仅禁用面板本身的 Raycast Target（如果方案1不起作用）

如果 `MainHubPanel` 本身没有 Image 组件，可能是其子元素遮挡：

1. **检查 MainHubPanel 的结构**：
   - 展开 `MainHubPanel`
   - 查看有哪些子元素

2. **禁用遮挡元素的 Raycast Target**：
   - 找到可能遮挡图标的子元素（通常是全屏的背景 Image）
   - 取消勾选其 `Image` 组件的 `Raycast Target`

3. **保留需要点击的元素**：
   - 按钮等需要点击的元素保持 `Raycast Target` 勾选

---

## 📋 检查清单

完成修复后，确认：

- [ ] `MainHubPanel` 的 `Image` 组件的 `Raycast Target` 已取消勾选
- [ ] 按钮等需要点击的元素仍可正常点击
- [ ] 运行游戏，按 T 键显示图标
- [ ] 点击图标后，Console 中看到 `"SphereIconManager: 点击了Sphere XXX 的图标"`
- [ ] 点击图标后，订单面板正常显示

---

## 🎯 推荐操作流程

### 快速修复（推荐）

1. **选中 MainHubPanel**
2. **取消勾选 Image 组件的 Raycast Target**
3. **保存场景**
4. **测试**

如果这样还不行，再尝试方案2和方案3。

---

## 💡 原理说明

### Unity UI 事件系统的工作方式

1. **EventSystem** 会从鼠标位置发出射线
2. **GraphicRaycaster**（在Canvas上）会检测这条射线是否命中UI元素
3. 如果命中，事件会被该UI元素处理
4. **第一个命中的UI元素会拦截事件**，后续的元素不会收到事件

### 为什么 MainHubPanel 会遮挡？

- `MainHubCanvas` 是 `Screen Space - Overlay`，覆盖整个屏幕
- `MainHubPanel` 如果有全屏或大范围的 Image，且 `Raycast Target` 开启
- 鼠标点击时，射线首先命中 `MainHubPanel`
- 事件被 `MainHubPanel` 拦截，无法到达 `WorldSpaceCanvas` 的图标

### 为什么禁用 Raycast Target 可以解决？

- 禁用 `Raycast Target` 后，该UI元素不会被 GraphicRaycaster 检测到
- 射线会穿透 `MainHubPanel`，继续检测后续的UI元素
- 最终可以到达 `WorldSpaceCanvas` 的图标
- 但按钮等子元素仍可正常点击（因为它们的 `Raycast Target` 是开启的）

---

## 🆘 如果还是不行

请检查：

1. **Console 中是否有点击日志**？
   - 如果有日志，说明事件已触发，问题在面板显示
   - 如果没有日志，说明事件仍未触发，继续排查遮挡问题

2. **WorldSpaceCanvas 是否有 GraphicRaycaster 组件**？
   - 必须有这个组件，才能接收点击事件

3. **图标的 Image 组件的 Raycast Target 是否开启**？
   - 必须开启，才能被检测到

4. **尝试点击图标的不同位置**：
   - 如果某些位置可以点击，某些不行，可能是遮挡范围的问题
