# CameraBGM 配置指南

## 概述

`CameraBGM` 脚本用于根据相机的激活状态自动播放/停止背景音乐，解决Additive模式加载场景时多个场景BGM同时播放的问题。

## 工作原理

- 脚本检测相机的激活状态（`camera.enabled` 和 `gameObject.activeInHierarchy`）
- 相机激活时：自动播放BGM
- 相机禁用时：自动停止BGM
- 确保同时只有一个场景的BGM在播放

## 配置步骤

### 1. 准备BGM音频文件

1. 将BGM音频文件导入Unity项目（例如：`MenuBGM.mp3`、`GameplayBGM.mp3`）
2. 在Project窗口中选择音频文件
3. 在Inspector中配置音频导入设置：
   - **Load Type**: `Streaming` 或 `Compressed In Memory`（推荐）
   - **Compression Format**: `Vorbis`（较小文件大小）
   - **Quality**: `50-70`（平衡质量和大小）
   - **Force To Mono**: 如果是立体声但不需要，可以勾选以减小文件大小

### 2. 在MainHub场景的相机上配置

1. **打开MainHub场景**（例如：`01_MainHub`）
2. **选中Main Camera**（或场景中的主相机）
3. **Add Component** → 搜索并添加 `CameraBGM`
4. **配置Inspector参数**：
   ```
   BGM设置
   ├── BGM Clip: [拖拽MainHub的BGM文件]
   ├── Audio Source: [留空，脚本会自动创建]
   ├── Auto Play On Enable: ✓ [勾选]
   ├── Loop: ✓ [勾选]
   └── Volume: 1.0 [根据需求调整]

   淡入淡出设置（可选）
   ├── Use Fade: ☐ [可选，根据需要勾选]
   └── Fade Time: 1.0 [如果启用淡入淡出，设置时间]

   调试
   └── Show Debug Log: ☐ [开发时可选勾选，查看日志]
   ```

### 3. 在游戏场景的相机上配置

1. **打开游戏场景**（例如：`level1`、`02_Gameplay`）
2. **选中Main Camera**（或场景中的主相机）
3. **Add Component** → 搜索并添加 `CameraBGM`
4. **配置Inspector参数**：
   ```
   BGM设置
   ├── BGM Clip: [拖拽游戏场景的BGM文件]
   ├── Audio Source: [留空，脚本会自动创建]
   ├── Auto Play On Enable: ✓ [勾选]
   ├── Loop: ✓ [勾选]
   └── Volume: 1.0 [根据需求调整]

   淡入淡出设置（可选）
   ├── Use Fade: ☐ [可选，根据需要勾选]
   └── Fade Time: 1.0 [如果启用淡入淡出，设置时间]

   调试
   └── Show Debug Log: ☐ [开发时可选勾选，查看日志]
   ```

### 4. 在主菜单场景的相机上配置（如果需要）

如果主菜单场景（`00_MainMenu`）也需要BGM，按照相同步骤配置。

## 参数说明

### BGM设置

- **BGM Clip**: 背景音乐AudioClip文件，拖拽音频文件到此槽位
- **Audio Source**: AudioSource组件（可选），如果留空，脚本会自动创建
- **Auto Play On Enable**: 启用时自动播放BGM（推荐勾选）
- **Loop**: 是否循环播放（推荐勾选）
- **Volume**: 音量大小（0-1），默认1.0

### 淡入淡出设置（可选）

- **Use Fade**: 是否启用淡入淡出效果
  - 启用：BGM播放/停止时会有淡入淡出过渡
  - 禁用：BGM立即播放/停止
- **Fade Time**: 淡入淡出时间（秒），默认1.0秒

### 调试

- **Show Debug Log**: 是否显示调试日志
  - 启用：在Console窗口显示BGM播放/停止的日志
  - 推荐在开发时启用，便于调试

## 工作流程示例

### Additive模式加载场景时

1. **MainHub场景已加载**：
   - MainHub相机的 `CameraBGM` 检测到相机激活
   - 自动播放MainHub的BGM

2. **点击"开始游戏"按钮**：
   - `SceneTransitionManager.LoadSceneAdditive()` 被调用
   - MainHub相机被禁用（`cam.enabled = false`）
   - MainHub相机的 `CameraBGM` 检测到相机禁用，自动停止BGM

3. **游戏场景加载完成**：
   - 游戏场景的相机激活
   - 游戏场景相机的 `CameraBGM` 检测到相机激活，自动播放游戏BGM

4. **返回MainHub**：
   - `SceneTransitionManager.UnloadGameScene()` 被调用
   - 游戏场景相机被销毁/禁用
   - 游戏场景相机的 `CameraBGM` 停止BGM
   - MainHub相机恢复激活
   - MainHub相机的 `CameraBGM` 检测到相机激活，自动播放MainHub的BGM

## 注意事项

1. **场景名称一致性**：确保场景名称与Build Settings中的名称一致

2. **BGM文件格式**：推荐使用 `.ogg` 或 `.mp3` 格式，Unity对这两种格式支持较好

3. **AudioSource自动创建**：如果未手动配置AudioSource，脚本会自动创建。也可以手动添加AudioSource组件并拖拽到脚本的Audio Source槽位

4. **音量控制**：如果需要全局音量控制，可以通过SettingsPanel修改AudioManager的BGM音量，但CameraBGM的Volume参数会相对独立

5. **淡入淡出性能**：启用淡入淡出会增加少量性能开销（协程），但通常可以忽略

6. **多个相机**：如果场景中有多个相机，只需要在激活时播放BGM的那个相机上添加 `CameraBGM` 脚本

## 故障排查

### BGM不播放

1. 检查BGM Clip是否已配置
2. 检查相机是否激活（`camera.enabled = true` 且 `GameObject.activeInHierarchy = true`）
3. 检查Auto Play On Enable是否勾选
4. 启用Show Debug Log查看详细日志

### 两个BGM同时播放

1. 确认MainHub相机的 `CameraBGM` 正常工作
2. 检查 `SceneTransitionManager` 是否正确禁用了MainHub相机
3. 启用Show Debug Log查看两个相机的状态

### BGM突然停止

1. 检查相机是否被意外禁用
2. 检查AudioSource组件是否存在且正常
3. 启用Show Debug Log查看停止原因

## 高级用法

### 手动控制BGM

如果需要手动控制BGM（不依赖相机状态），可以调用脚本的公共方法：

```csharp
// 获取CameraBGM组件
CameraBGM cameraBGM = Camera.main.GetComponent<CameraBGM>();

// 手动播放
cameraBGM.PlayBGM();

// 手动停止
cameraBGM.StopBGM();

// 设置音量
cameraBGM.SetVolume(0.5f);
```

### 动态切换BGM

可以在运行时动态切换BGM：

```csharp
// 获取CameraBGM组件
CameraBGM cameraBGM = Camera.main.GetComponent<CameraBGM>();

// 通过反射或添加公共方法修改bgmClip（需要扩展脚本）
// 或者直接替换AudioClip并调用PlayBGM()
```
