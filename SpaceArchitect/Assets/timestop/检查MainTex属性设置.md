# 检查 MainTex 属性设置

## 🔍 如果所有节点连接都正常，但只显示右半边

可能的问题：**MainTex 属性设置或传递有问题**

## 🎯 检查 Shader Graph 中的 MainTex 属性

### 1. 检查 MainTex 属性是否存在

**检查**：
1. **在 Shader Graph 的 Blackboard 中**（左侧属性面板）
2. **查找 `MainTex` 属性**：
   - 类型：`Texture2D`
   - 名称：`_MainTex`（或 `MainTex`）

3. **如果不存在**：
   - 需要创建一个 `Texture2D` 属性
   - 命名为 `_MainTex`

### 2. 检查 MainTex 是否正确连接到 Sample Texture 2D

**检查**：
1. **找到 `Sample Texture 2D` 节点**
2. **检查 `Texture` 输入**：
   - 应该连接到 `MainTex` 属性（从 Blackboard 拖出）
   - 或者连接到 `_MainTex` 属性

3. **检查连接**：
   ```
   MainTex 属性 → Sample Texture 2D (Texture 输入)
   ```

### 3. 检查 MainTex 的默认值

**检查**：
1. **在 Blackboard 中选择 `MainTex` 属性**
2. **检查默认值**：
   - 应该设置为 `None`（白色纹理）
   - 或者不设置（Renderer Feature 会自动传递）

## 🔧 可能的问题和解决方法

### 问题 1：MainTex 属性不存在

**解决方法**：
1. 在 Blackboard 中创建 `Texture2D` 属性
2. 命名为 `_MainTex`
3. 连接到 `Sample Texture 2D` 的 `Texture` 输入

### 问题 2：MainTex 属性名称不对

**解决方法**：
- 确保属性名称是 `_MainTex`（带下划线）
- 或者确保 Renderer Feature 传递的纹理名称匹配

### 问题 3：Renderer Feature 没有正确传递纹理

**检查**：
- Renderer Feature 使用 Blit 时，会自动传递屏幕内容到 `_MainTex`
- 但可能需要显式设置

## 📝 检查清单

- [ ] Shader Graph 中有 `MainTex` 或 `_MainTex` 属性
- [ ] `Sample Texture 2D` 的 `Texture` 输入连接了 `MainTex` 属性
- [ ] `MainTex` 属性的类型是 `Texture2D`
- [ ] `MainTex` 属性的名称是 `_MainTex`（带下划线）

## 🎯 快速检查

1. **打开 Shader Graph**
2. **查看 Blackboard**（左侧属性面板）
3. **查找 `MainTex` 或 `_MainTex` 属性**
4. **检查是否连接到 `Sample Texture 2D` 的 `Texture` 输入**

如果 `MainTex` 属性不存在或未连接，这就是问题所在。

