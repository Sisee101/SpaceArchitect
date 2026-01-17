# ShockwaveController 代码详细分析

## 📋 代码结构分析

### 1. 类定义和字段

```csharp
public class ShockwaveController : MonoBehaviour
{
    public Material shockwaveMaterial;  // 冲击波材质引用
    
    [Header("动画设置")]
    public float shockwaveDuration = 1f;      // 冲击波持续时间
    public float maxDistance = 1.5f;         // 最大扩散距离
    [Range(0, 0.5f)] public float width = 0.1f;      // 冲击波宽度
    [Range(-0.2f, 0.2f)] public float strength = 0.05f;  // 冲击波强度
    
    private float currentTimer = 0f;          // 当前计时器
    private bool isPlaying = false;            // 是否正在播放
    
    // Shader 属性 ID（性能优化，避免字符串查找）
    private int distancePropID = Shader.PropertyToID("_RippleDistanceFromCenter");
    private int strengthPropID = Shader.PropertyToID("_RippleStrength");
    private int widthPropID = Shader.PropertyToID("_RippleWidth");
    private int centerPropID = Shader.PropertyToID("_CenterPoint");
}
```

**分析**：
- ✅ 使用 `Shader.PropertyToID` 缓存属性 ID，这是性能最佳实践
- ✅ 参数都有合理的范围和默认值
- ⚠️ **问题**：`centerPropID` 设置为 `_CenterPoint`，但传入的是 `Vector2`，而 shader 可能需要 `Vector4`

### 2. Start() 方法

```csharp
void Start()
{
    if (shockwaveMaterial != null)
    {
        shockwaveMaterial.SetFloat(strengthPropID, 0);
    }
}
```

**分析**：
- ✅ 初始化时将强度设置为 0，确保初始状态不显示效果
- ⚠️ **潜在问题**：没有检查 `shockwaveMaterial` 是否已正确赋值

### 3. Update() 方法

```csharp
void Update()
{
    // 测试：按 E 键触发
    if (Input.GetKeyDown(KeyCode.E))
    {
        TriggerShockwave(new Vector2(0.5f, 0.5f));  // 默认从屏幕中心触发
    }

    if (isPlaying)
    {
        currentTimer += Time.unscaledDeltaTime;  // 使用未缩放时间

        float progress = currentTimer / shockwaveDuration;

        // 距离从 -width 到 maxDistance
        float currentDistance = Mathf.Lerp(-width, maxDistance, progress);
        
        // 强度从 strength 到 0
        float currentStrength = Mathf.Lerp(strength, 0f, progress);

        // 更新 shader 参数
        shockwaveMaterial.SetFloat(distancePropID, currentDistance);
        shockwaveMaterial.SetFloat(strengthPropID, currentStrength);
        shockwaveMaterial.SetFloat(widthPropID, width);

        if (progress >= 1f)
        {
            isPlaying = false;
            shockwaveMaterial.SetFloat(strengthPropID, 0);
        }
    }
}
```

**分析**：
- ✅ 使用 `Time.unscaledDeltaTime`，确保时停时也能播放
- ✅ 动画逻辑清晰：距离从负值到最大值，强度从最大值到 0
- ⚠️ **问题**：`TriggerShockwave` 传入的是 `Vector2(0.5f, 0.5f)`，这是屏幕空间的 UV 坐标（0-1），但可能不是世界空间或视图空间坐标

### 4. TriggerShockwave() 方法

```csharp
public void TriggerShockwave(Vector2 viewportCenter)
{
    currentTimer = 0f;
    isPlaying = true;
    shockwaveMaterial.SetVector(centerPropID, viewportCenter);
}
```

**分析**：
- ✅ 重置计时器并开始播放
- ⚠️ **关键问题**：
  - 传入的是 `Vector2`，但 `SetVector` 需要 `Vector4`
  - `viewportCenter` 应该是屏幕空间坐标（0-1），但如果 GameObject 移动到相机镜头内，这个坐标可能不正确
  - **没有考虑 GameObject 的位置**：冲击波中心点应该基于 GameObject 的位置，而不是固定的屏幕中心

## 🔍 问题根源分析

### 问题 1：中心点坐标不正确

**当前实现**：
- 总是使用 `Vector2(0.5f, 0.5f)`（屏幕中心）
- 不考虑 GameObject 的实际位置

**当 GameObject 移动到相机镜头内时**：
- 如果 GameObject 不在屏幕中心，冲击波效果会从错误的位置开始
- Screen Position 节点计算可能不正确

### 问题 2：Screen Position 节点的问题

从 shader graph 分析：
- 有两个 Screen Position 节点：
  - `m_ScreenSpaceType: 0` (Raw) - 用于 Scene Color 的 UV
  - `m_ScreenSpaceType: 1` (Default) - 用于距离计算
- Scene Color Node 需要读取屏幕内容

**当 GameObject 移动到相机镜头内时**：
- Screen Position 的计算可能受到 GameObject 位置的影响
- 如果 GameObject 是一个很大的 Sphere（scale: 121.94），它可能覆盖整个屏幕
- Scene Color Node 可能读取不到正确的屏幕内容

### 问题 3：Scene Color Node 的限制

**Scene Color Node 的特性**：
- 读取的是不透明纹理（Opaque Texture）
- 如果 GameObject 在相机前面，它可能会遮挡屏幕内容
- 即使 ZTest 是 Always，Scene Color Node 仍然可能读取不到正确的屏幕内容

## ✅ 解决方案建议

### 方案 1：基于 GameObject 位置计算中心点

```csharp
public void TriggerShockwave(Vector2 viewportCenter)
{
    currentTimer = 0f;
    isPlaying = true;
    
    // 如果传入的是世界坐标，需要转换为屏幕坐标
    Camera mainCam = Camera.main;
    if (mainCam != null)
    {
        Vector3 worldPos = transform.position;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
        Vector2 viewportPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );
        shockwaveMaterial.SetVector(centerPropID, viewportPos);
    }
    else
    {
        shockwaveMaterial.SetVector(centerPropID, viewportCenter);
    }
}
```

### 方案 2：确保 GameObject 位置不影响效果

- 将 GameObject 放在相机后面（z < 0）
- 或者使用 Quad 而不是 Sphere
- 或者将 GameObject 的 scale 设置得很小

### 方案 3：使用固定的屏幕空间坐标

如果冲击波应该是全屏效果，应该始终使用屏幕中心：
```csharp
shockwaveMaterial.SetVector(centerPropID, new Vector4(0.5f, 0.5f, 0, 0));
```

## 🎯 建议的修改

1. **修改 TriggerShockwave 方法**：根据 GameObject 位置计算正确的屏幕坐标
2. **添加空值检查**：确保 Material 和 Camera 不为空
3. **考虑使用 Vector4**：如果 shader 需要 Vector4，应该传入 Vector4







