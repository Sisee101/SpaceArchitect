# 从 Git 历史恢复更早的 Shader Graph 版本

## 🔍 发现

根据检查，发现：

1. **当前的 `SG_Shockwave.shadergraph`** 使用的 GUID: `2ebff1db8d793604ea6db68283728bec`
2. **其他 prefab 和场景** 使用的 Material GUID: `e9cef7646f9bee943a7cae7bfd1f616f`（这是 `ShockwaveRing.mat` 的 GUID）
3. **Git 历史** 显示有多个提交，包括：
   - `72157ad background03`
   - `fc15283 background02`
   - `39a8b6f 渲染层级调整`
   - `4fbf566 scene1.2`
   - 等等

## 📋 从 Git 恢复更早版本的步骤

### 方法 1：使用 Unity 编辑器的版本控制

1. **在 Unity 编辑器中：**
   - 右键点击 `Assets/timestop/SG_Shockwave.shadergraph`
   - 选择版本控制相关选项（如果有）
   - 查看历史版本
   - 恢复到更早的版本

### 方法 2：使用 Git 命令行

**在项目根目录执行：**

```bash
# 1. 查看所有历史版本
git log --oneline --all -- "Assets/timestop/SG_Shockwave.shadergraph"

# 2. 查看某个提交的 shader graph 内容
git show <commit-hash>:Assets/timestop/SG_Shockwave.shadergraph

# 3. 恢复到某个提交的版本
git checkout <commit-hash> -- Assets/timestop/SG_Shockwave.shadergraph

# 4. 或者创建一个新文件保存旧版本
git show <commit-hash>:Assets/timestop/SG_Shockwave.shadergraph > Assets/timestop/SG_Shockwave_Old.shadergraph
```

### 方法 3：检查其他提交中的 Material

**Material 文件可能也包含 shader 的引用信息：**

```bash
# 查看 Material 文件的历史
git log --oneline --all -- "Assets/timestop/ShockwaveRing.mat"

# 查看某个提交的 Material 内容
git show <commit-hash>:Assets/timestop/ShockwaveRing.mat
```

**在 Material 文件中查找 `m_Shader` 字段，看看它引用的 shader GUID 是什么。**

---

## 🎯 推荐的恢复步骤

### 步骤 1：找到最早的提交

1. 在项目根目录打开终端/命令行
2. 执行：
   ```bash
   git log --oneline --all --reverse -- "Assets/timestop/SG_Shockwave.shadergraph"
   ```
3. 找到**第一个**（最早的）提交

### 步骤 2：查看最早版本的内容

```bash
git show <最早提交的hash>:Assets/timestop/SG_Shockwave.shadergraph
```

### 步骤 3：恢复或创建新文件

**选项 A：直接恢复（会覆盖当前文件）**
```bash
git checkout <最早提交的hash> -- Assets/timestop/SG_Shockwave.shadergraph
```

**选项 B：创建新文件（保留当前版本）**
```bash
git show <最早提交的hash>:Assets/timestop/SG_Shockwave.shadergraph > Assets/timestop/SG_Shockwave_Original.shadergraph
```

### 步骤 4：在 Unity 中应用

1. 如果创建了新文件，Unity 会自动导入
2. 在 Material 中选择新的 shader graph
3. 测试效果

---

## ⚠️ 注意事项

1. **备份当前版本**：恢复前先备份当前的 `SG_Shockwave.shadergraph`
2. **检查 Material 引用**：恢复后检查 Material 是否还正确引用 shader
3. **测试效果**：恢复后立即测试，确保效果正确

---

## 🔧 如果无法使用 Git

**如果 Git 不可用或路径有问题，可以：**

1. **手动检查**：在 Unity 编辑器中查看 `SG_Shockwave.shadergraph` 的修改历史（如果有）
2. **从备份恢复**：如果有项目备份，从备份中恢复
3. **重新创建**：根据原始逻辑重新创建（我可以帮你）

---

## 💡 建议

**最快的方法：**

1. 在项目根目录打开 Git Bash 或 PowerShell
2. 执行：
   ```bash
   git log --oneline --all --reverse -- "Assets/timestop/SG_Shockwave.shadergraph" | head -1
   ```
3. 获取最早的提交 hash
4. 执行：
   ```bash
   git show <hash>:Assets/timestop/SG_Shockwave.shadergraph > Assets/timestop/SG_Shockwave_Original.shadergraph
   ```
5. 在 Unity 中打开新文件，检查是否是你要的版本

**需要我帮你执行这些命令吗？或者你可以告诉我最早的提交 hash，我帮你恢复！**



