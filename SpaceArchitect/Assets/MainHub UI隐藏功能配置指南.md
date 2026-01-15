# MainHub UI隐藏功能配置指南

## 概述

本指南说明MainHub UI自动隐藏功能的工作原理。当使用Additive模式加载游戏场景时，系统会自动隐藏MainHub场景的Canvas，确保游戏场景中看不到MainHub的UI内容。卸载游戏场景后，MainHub的Canvas会自动恢复显示。

## 功能特性

- **自动隐藏**：加载游戏场景时自动隐藏MainHub的Canvas
- **自动恢复**：卸载游戏场景时自动恢复MainHub的Canvas显示
- **智能查找**：自动查找MainHub场景中的Canvas组件
- **引用管理**：自动管理Canvas引用，支持重新查找

---

## 一、工作原理

### 1.1 自动隐藏流程

```
[点击jumpButton加载游戏场景]
    ↓
[SceneTransitionManager.LoadSceneAdditive()]
    ↓
[HideMainHubCanvas()]
    ↓
[查找MainHub场景中的Canvas]
    ↓
[设置Canvas.SetActive(false)]
    ↓
[加载游戏场景 (Additive)]
    ↓
[游戏场景显示，MainHub UI已隐藏]
```

### 1.2 自动恢复流程

```
[点击返回主界面按钮]
    ↓
[SceneTransitionManager.UnloadGameScene()]
    ↓
[卸载游戏场景]
    ↓
[ShowMainHubCanvas()]
    ↓
[查找MainHub场景中的Canvas（如果引用丢失）]
    ↓
[设置Canvas.SetActive(true)]
    ↓
[MainHub UI重新显示]
```

---

## 二、配置说明

### 2.1 无需额外配置

**此功能已自动集成，无需手动配置**：

- SceneTransitionManager会自动查找MainHub场景中的Canvas
- 加载游戏场景时自动隐藏
- 卸载游戏场景时自动恢复

### 2.2 MainHub场景要求

**确保MainHub场景满足以下条件**：

1. **场景名称**：必须是 `01_MainHub`
2. **Canvas组件**：场景中必须包含至少一个Canvas组件
3. **场景已加载**：MainHub场景必须在加载游戏场景前已加载

### 2.3 Canvas查找逻辑

系统会按以下顺序查找Canvas：

1. **根GameObject查找**：在MainHub场景的根GameObject中查找Canvas组件
2. **子对象查找**：如果根GameObject没有Canvas，在子对象中查找
3. **引用保存**：找到后保存引用，避免重复查找
4. **引用验证**：如果引用失效，重新查找

---

## 三、测试和验证

### 3.1 测试UI隐藏

1. **运行游戏**
   - 进入MainHub场景（`01_MainHub`）
   - 确认MainHub的UI正常显示

2. **加载游戏场景**
   - 点击Sphere → 显示SphereInfoPanel
   - 点击jumpButton加载游戏场景
   - 观察MainHub的UI是否已隐藏
   - 游戏场景应该正常显示，且看不到MainHub的UI

3. **验证Console日志**
   - 打开Console，应该看到：
     - "SceneTransitionManager: 找到MainHub场景的Canvas: XXX"
     - "SceneTransitionManager: 已隐藏MainHub场景的Canvas: XXX"
     - "SceneTransitionManager: 使用Additive模式加载场景 XXX，覆盖在MainHub上方，MainHub UI已隐藏"

### 3.2 测试UI恢复

1. **在游戏场景中**
   - 打开暂停弹窗（按ESC键或点击暂停按钮）
   - 点击返回主界面按钮

2. **验证恢复**
   - 观察游戏场景是否被卸载
   - MainHub的UI应该重新显示
   - 打开Console，应该看到：
     - "SceneTransitionManager: 已恢复MainHub场景的Canvas显示: XXX"
     - "SceneTransitionManager: 已卸载游戏场景 XXX，返回MainHub，MainHub UI已恢复显示"

### 3.3 测试多次切换

1. **多次加载/卸载**
   - 加载游戏场景 → MainHub UI隐藏
   - 卸载游戏场景 → MainHub UI恢复
   - 再次加载游戏场景 → MainHub UI再次隐藏
   - 验证每次都能正确隐藏/恢复

---

## 四、常见问题排查

### 4.1 MainHub UI未隐藏

**可能原因**：
- MainHub场景未加载
- MainHub场景中没有Canvas组件
- Canvas查找失败

**解决方法**：
1. 确认MainHub场景已加载（场景名称必须是 `01_MainHub`）
2. 检查MainHub场景中是否有Canvas组件
3. 查看Console中的警告信息
4. 确认Canvas组件在场景的根GameObject或子对象中

### 4.2 MainHub UI未恢复

**可能原因**：
- Canvas引用丢失
- MainHub场景被意外卸载
- Canvas查找失败

**解决方法**：
1. 确认MainHub场景仍然存在（不会被卸载）
2. 检查Console中是否有警告信息
3. 系统会自动重新查找Canvas，如果仍然失败，检查场景结构

### 4.3 多个Canvas的情况

**如果MainHub场景中有多个Canvas**：

- 系统会找到第一个Canvas并隐藏/恢复它
- 如果MainHub有多个Canvas需要隐藏，建议：
  - 将所有UI放在一个Canvas下
  - 或者创建一个父GameObject包含所有Canvas，隐藏父对象

### 4.4 Canvas查找失败

**可能原因**：
- Canvas不在场景的根GameObject中
- Canvas在深层嵌套的子对象中
- Canvas组件未正确添加

**解决方法**：
1. 检查Canvas的位置，确保在场景的根GameObject或直接子对象中
2. 如果Canvas在深层嵌套中，系统会通过 `GetComponentInChildren` 查找
3. 确认Canvas组件已正确添加到GameObject上

---

## 五、技术说明

### 5.1 Canvas查找机制

- **场景查找**：通过 `SceneManager.GetSceneByName("01_MainHub")` 获取MainHub场景
- **根对象遍历**：遍历场景的所有根GameObject
- **组件查找**：使用 `GetComponent<Canvas>()` 和 `GetComponentInChildren<Canvas>()`
- **引用保存**：找到后保存到静态变量 `mainHubCanvas`

### 5.2 引用管理

- **引用保存**：找到Canvas后保存引用，避免重复查找
- **引用验证**：每次使用前检查引用是否有效
- **重新查找**：如果引用失效，自动重新查找

### 5.3 隐藏/恢复机制

- **隐藏方式**：使用 `SetActive(false)` 隐藏Canvas GameObject
- **恢复方式**：使用 `SetActive(true)` 恢复Canvas GameObject
- **状态保持**：Canvas的其他设置（如Sort Order）保持不变

---

## 六、使用示例

### 6.1 手动隐藏MainHub Canvas（代码方式）

如果需要手动控制Canvas的显示/隐藏：

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // 手动隐藏MainHub Canvas
        Scene mainHubScene = SceneManager.GetSceneByName("01_MainHub");
        if (mainHubScene.IsValid() && mainHubScene.isLoaded)
        {
            GameObject[] rootObjects = mainHubScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                Canvas canvas = rootObj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
}
```

### 6.2 检查Canvas状态

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyScript : MonoBehaviour
{
    void Update()
    {
        Scene mainHubScene = SceneManager.GetSceneByName("01_MainHub");
        if (mainHubScene.IsValid() && mainHubScene.isLoaded)
        {
            GameObject[] rootObjects = mainHubScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                Canvas canvas = rootObj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"MainHub Canvas状态: {(canvas.gameObject.activeSelf ? "显示" : "隐藏")}");
                    break;
                }
            }
        }
    }
}
```

---

## 七、完成检查清单

使用以下清单确保功能正常工作：

### 场景配置
- [ ] MainHub场景名称正确（`01_MainHub`）
- [ ] MainHub场景已添加到Build Settings
- [ ] MainHub场景中包含Canvas组件

### 功能测试
- [ ] 点击jumpButton，MainHub UI成功隐藏
- [ ] 游戏场景正常显示，看不到MainHub UI
- [ ] 点击返回主界面按钮，MainHub UI成功恢复
- [ ] 多次切换，每次都能正确隐藏/恢复

### 调试验证
- [ ] Console中显示Canvas查找和隐藏/恢复的日志
- [ ] 没有错误或警告信息
- [ ] Canvas引用管理正常

---

## 八、注意事项

1. **MainHub场景必须存在**
   - MainHub场景必须在加载游戏场景前已加载
   - 场景名称必须是 `01_MainHub`（区分大小写）

2. **Canvas组件要求**
   - MainHub场景中必须包含至少一个Canvas组件
   - Canvas应该在场景的根GameObject或子对象中

3. **多个Canvas**
   - 如果MainHub有多个Canvas，系统会隐藏找到的第一个
   - 建议将所有UI放在一个Canvas下，或创建父对象统一管理

4. **场景结构**
   - Canvas应该在场景的根GameObject中，或作为根GameObject的直接子对象
   - 如果Canvas在深层嵌套中，系统会尝试查找，但可能失败

5. **性能考虑**
   - Canvas查找只在加载/卸载时执行，性能影响很小
   - 引用保存避免了重复查找

---

## 九、故障排除

### 如果Canvas未找到

1. **检查场景名称**
   - 确认MainHub场景名称是 `01_MainHub`（完全匹配，区分大小写）

2. **检查场景是否加载**
   - 在Console中查看是否有 "MainHub场景未加载" 的警告
   - 确认MainHub场景在加载游戏场景前已加载

3. **检查Canvas位置**
   - 打开MainHub场景
   - 确认Canvas在场景的根GameObject中，或作为根GameObject的子对象
   - 如果Canvas在深层嵌套中，考虑调整场景结构

4. **手动测试查找**
   - 在代码中手动执行Canvas查找逻辑
   - 检查是否能找到Canvas

### 如果Canvas隐藏但未恢复

1. **检查引用**
   - 确认Canvas引用未丢失
   - 系统会自动重新查找，如果仍然失败，检查场景结构

2. **检查场景状态**
   - 确认MainHub场景仍然存在
   - 确认场景未被意外卸载

---

**功能已自动集成！现在加载游戏场景时，MainHub的UI会自动隐藏，卸载游戏场景后会自动恢复显示。**
