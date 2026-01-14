# 检查 UV 坐标提取问题

## 🔍 如果所有节点预览都正常，但只显示右半边

可能的问题：**UV 坐标提取有问题**

## 🎯 检查 Screen Position 的 XY 提取

### 用于屏幕内容采样的 Screen Position

**检查**：
1. **找到用于 `Sample Texture 2D` 的 Screen Position 节点**
2. **检查模式**：应该是 `Default`
3. **检查如何提取 XY**：
   - 应该使用 `Split` 节点提取 R (X) 和 G (Y)
   - 然后使用 `Combine` 节点组合成 Vector2

4. **检查连接**：
   ```
   Screen Position (Default) → Split → Combine (R, G) → Sample Texture 2D (UV)
   ```

### 可能的问题

**问题 1：只提取了 X 或只提取了 Y**

**检查**：
- `Split` 节点的 R 和 G 输出是否都连接了
- `Combine` 节点的 R 和 G 输入是否都连接了

**问题 2：Combine 的输出类型不对**

**检查**：
- `Combine` 应该输出 Vector2（RG）
- 不应该输出 Vector3 或 Vector4

**问题 3：UV 坐标范围不对**

**检查**：
- Screen Position (Default) 的坐标范围是 0-1
- 如果使用 Raw 模式，需要转换

## 🔧 解决方法

### 确保正确的连接

```
Screen Position (Default)
  ↓
Split 节点
  - R 输出 = X 坐标
  - G 输出 = Y 坐标
  ↓
Combine 节点
  - R 输入：Split 的 R (X)
  - G 输入：Split 的 G (Y)
  - 输出：Vector2 (XY)
  ↓
Sample Texture 2D (UV 输入)
```

## 📝 检查清单

- [ ] Screen Position 使用 Default 模式
- [ ] Split 节点的 R 和 G 输出都连接了
- [ ] Combine 节点的 R 和 G 输入都连接了
- [ ] Combine 输出 Vector2（不是 Vector3 或 Vector4）
- [ ] Sample Texture 2D 的 UV 输入连接了 Combine 的输出

## 🎯 快速检查

1. **选择用于 UV 的 Combine 节点**
2. **查看预览**：
   - 应该显示一个渐变（从左下到右上）
   - 如果只显示一半，说明 XY 提取有问题

3. **检查输入**：
   - R 输入：应该连接 Split 的 R (X)
   - G 输入：应该连接 Split 的 G (Y)

请检查用于 `Sample Texture 2D` 的 UV 坐标提取部分，确保 R 和 G 都正确连接了。

