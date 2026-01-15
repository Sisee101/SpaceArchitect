# 如何将图片制作成 Skybox 环境

本指南将教你如何将一张图片（特别是 360 度全景图）制作成 Unity 的 Skybox 环境。

## 方法一：使用单张全景图（推荐，最简单）

### 适用图片类型
- **Equirectangular（等距圆柱投影）**：最常见的 360 度全景图格式
- 图片比例通常是 **2:1**（宽度是高度的 2 倍）
- 例如：2048x1024、4096x2048 等

### 步骤

#### 1. 准备图片
- 确保图片是 **Equirectangular 格式**的全景图
- 推荐分辨率：2048x1024 或更高（4096x2048）

#### 2. 导入图片到 Unity
1. 将图片拖拽到 Unity 项目的 `Assets` 文件夹中
2. 选中导入的图片，在 Inspector 中设置：
   - **Texture Type**：选择 `Default` 或 `Sprite (2D and UI)`
   - **Texture Shape**：选择 `2D`
   - **sRGB (Color Texture)**：勾选（如果是彩色图片）
   - **Max Size**：根据图片实际大小设置（如 2048 或 4096）
   - **Compression**：根据需求选择（`None` 可获得最佳质量）

#### 3. 创建 Skybox 材质
1. 在 Project 窗口中，右键点击 → `Create` → `Material`
2. 将材质命名为 `MySkybox`（或你喜欢的名字）
3. 选中这个材质，在 Inspector 中：
   - 点击 **Shader** 下拉菜单
   - 选择 `Skybox` → `6 Sided`（用于 6 张图片）或 `Skybox` → `Cubemap`（用于立方体贴图）
   - **但如果是单张全景图，需要选择 `Skybox` → `Procedural` 或使用自定义 Shader**

#### 4. 使用 URP Skybox Shader（推荐）
对于 **URP（Universal Render Pipeline）**，Unity 提供了专门的 Skybox Shader：

1. 选中材质，在 Shader 下拉菜单中选择：
   - `Universal Render Pipeline` → `Skybox` → `6 Sided`（6 张图片）
   - 或使用自定义的 **Equirectangular Skybox Shader**

2. **如果没有现成的 Equirectangular Shader，可以：**
   - 使用 Unity Asset Store 的免费 Skybox 资源包
   - 或使用下面的方法二（转换为 Cubemap）

#### 5. 将全景图转换为 Cubemap（如果 Shader 不支持直接使用全景图）

如果 Unity 的默认 Skybox Shader 不支持直接使用 Equirectangular 图片，需要先转换为 Cubemap：

1. 选中你的全景图
2. 在 Inspector 中：
   - **Texture Type**：改为 `Cubemap`
   - **Mapping**：选择 `Latitude-Longitude (Cylindrical)`（这就是 Equirectangular）
   - 点击 **Apply**

3. 现在创建一个新的材质：
   - Shader 选择：`Skybox` → `Cubemap`
   - 将转换后的 Cubemap 拖拽到 **Cubemap (HDR)** 槽中

#### 6. 应用到场景
1. 打开 `Window` → `Rendering` → `Lighting`（或 `Window` → `Rendering` → `Lighting Settings`）
2. 在 **Environment** 标签页中：
   - 找到 **Skybox Material** 选项
   - 将你创建的 Skybox 材质拖拽到这里
3. 或者通过代码设置：
```csharp
RenderSettings.skybox = mySkyboxMaterial;
```

---

## 方法二：使用 6 张图片（立方体贴图）

### 适用场景
如果你有 6 张分别对应立方体 6 个面的图片（前、后、左、右、上、下）。

### 步骤

#### 1. 准备 6 张图片
- 命名规范（Unity 会自动识别）：
  - `front` / `posz`（前）
  - `back` / `negz`（后）
  - `left` / `negx`（左）
  - `right` / `posx`（右）
  - `up` / `posy`（上）
  - `down` / `negy`（下）

#### 2. 导入图片
1. 将 6 张图片放在同一个文件夹中
2. 选中所有 6 张图片
3. 在 Inspector 中：
   - **Texture Type**：选择 `Cubemap`
   - **Mapping**：选择 `6 Frames Layout (Cubemap)`
   - 点击 **Apply**

#### 3. 创建 Skybox 材质
1. 创建新材质
2. Shader 选择：`Skybox` → `Cubemap`
3. 将生成的 Cubemap 拖拽到 **Cubemap (HDR)** 槽中

---

## 方法三：使用程序化 Skybox（代码生成）

如果需要动态生成或修改 Skybox，可以使用代码：

```csharp
using UnityEngine;

public class SkyboxGenerator : MonoBehaviour
{
    public Texture2D panoramaTexture; // 你的全景图
    
    void Start()
    {
        // 创建 Cubemap
        Cubemap cubemap = new Cubemap(512, TextureFormat.RGB24, false);
        
        // 将 Equirectangular 图片转换为 Cubemap
        ConvertEquirectangularToCubemap(panoramaTexture, cubemap);
        
        // 创建 Skybox 材质
        Material skyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));
        skyboxMaterial.SetTexture("_Tex", cubemap);
        
        // 应用到场景
        RenderSettings.skybox = skyboxMaterial;
    }
    
    void ConvertEquirectangularToCubemap(Texture2D equirectangular, Cubemap cubemap)
    {
        // 这里需要实现 Equirectangular 到 Cubemap 的转换算法
        // 可以使用 Unity 的 Graphics.CopyTexture 或手动采样
        // 或者使用第三方工具/插件
    }
}
```

---

## 快速检查清单

- [ ] 图片是 Equirectangular 格式（2:1 比例）
- [ ] 图片已导入到 Unity
- [ ] 图片的 Texture Type 设置正确
- [ ] 创建了 Skybox 材质
- [ ] 材质使用了正确的 Shader
- [ ] 材质已应用到 Lighting Settings 或通过代码设置

---

## 常见问题

### Q: 我的图片不是 2:1 比例怎么办？
A: 可以使用图像编辑软件（如 Photoshop、GIMP）将图片裁剪或拉伸为 2:1 比例，但可能会造成变形。

### Q: Skybox 显示不正确，有接缝或扭曲？
A: 
- 确保图片是标准的 Equirectangular 格式
- 检查图片的导入设置（Max Size、Compression）
- 尝试调整 Cubemap 的 Filter Mode

### Q: URP 中找不到 Skybox Shader？
A: 
- 确保项目使用的是 URP
- 在 Shader 下拉菜单中查找 `Universal Render Pipeline` 下的选项
- 或使用 Unity 内置的 `Skybox/Cubemap` Shader（在 URP 中也可用）

### Q: 如何让 Skybox 旋转？
A: 在 Skybox 材质的 Inspector 中，找到 **Rotation** 参数（如果有），或通过代码：
```csharp
RenderSettings.skybox.SetFloat("_Rotation", rotationAngle);
```

---

## 推荐资源

- **Unity Asset Store**：搜索 "Skybox" 可以找到很多免费的 Skybox 资源包
- **HDRI Haven**：提供高质量的免费 HDRI 全景图（https://hdrihaven.com/）
- **Poly Haven**：另一个免费的 HDRI 资源网站

---

## 注意事项

1. **性能**：高分辨率的 Skybox 可能会影响性能，建议根据项目需求选择合适的分辨率
2. **HDR**：如果使用 HDR 图片，确保图片格式支持 HDR（如 .exr 格式）
3. **URP 兼容性**：某些旧版本的 Skybox Shader 可能在 URP 中不完全兼容，建议使用 URP 专用的 Shader

