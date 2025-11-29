using UnityEngine;
using UnityEditor;
using SpaceArchitect.Core;
using SpaceArchitect.Gameplay;
using SpaceArchitect.CelestialObjects;

/// <summary>
/// 场景搭建验证器
/// 在Unity编辑器中自动检查场景配置是否正确
/// </summary>
public class SceneSetupValidator : EditorWindow
{
    [MenuItem("SpaceArchitect/场景配置检查器")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupValidator>("场景配置检查");
    }

    private Vector2 scrollPosition;
    private bool showErrors = true;
    private bool showWarnings = true;
    private bool showInfo = true;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("场景配置检查器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 过滤选项
        EditorGUILayout.BeginHorizontal();
        showErrors = EditorGUILayout.Toggle("显示错误", showErrors);
        showWarnings = EditorGUILayout.Toggle("显示警告", showWarnings);
        showInfo = EditorGUILayout.Toggle("显示信息", showInfo);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("检查场景配置", GUILayout.Height(30)))
        {
            ValidateScene();
        }

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 显示检查结果
        DrawValidationResults();

        EditorGUILayout.EndScrollView();
    }

    private void ValidateScene()
    {
        // 这里可以添加验证逻辑
    }

    private void DrawValidationResults()
    {
        EditorGUILayout.LabelField("检查结果：", EditorStyles.boldLabel);

        // 检查 EventManager
        CheckEventManager();

        // 检查 LaunchStation
        CheckLaunchStation();

        // 检查 ShipController
        CheckShipController();

        // 检查 Planet
        CheckPlanets();

        // 检查 Obstacle
        CheckObstacles();

        // 检查标签
        CheckTags();

        // 检查摄像机
        CheckCamera();
    }

    private void CheckEventManager()
    {
        EditorGUILayout.LabelField("EventManager:", EditorStyles.boldLabel);
        EventManager eventManager = FindObjectOfType<EventManager>();

        if (eventManager == null && showErrors)
        {
            EditorGUILayout.HelpBox("错误：场景中未找到 EventManager！\n请添加 EventManager GameObject 并挂载 EventManager 脚本。", MessageType.Error);
        }
        else if (eventManager != null && showInfo)
        {
            EditorGUILayout.HelpBox("✓ EventManager 已找到", MessageType.Info);
        }
    }

    private void CheckLaunchStation()
    {
        EditorGUILayout.LabelField("LaunchStation:", EditorStyles.boldLabel);
        LaunchStation launchStation = FindObjectOfType<LaunchStation>();

        if (launchStation == null && showErrors)
        {
            EditorGUILayout.HelpBox("警告：场景中未找到 LaunchStation！\n建议添加发射台用于测试。", MessageType.Warning);
        }
        else if (launchStation != null)
        {
            EditorGUILayout.HelpBox("✓ LaunchStation 已找到", MessageType.Info);

            if (launchStation.Ship == null && showWarnings)
            {
                EditorGUILayout.HelpBox("警告：LaunchStation 未设置飞船引用。\n将自动查找场景中的 ShipController。", MessageType.Warning);
            }
        }
    }

    private void CheckShipController()
    {
        EditorGUILayout.LabelField("ShipController:", EditorStyles.boldLabel);
        ShipController[] ships = FindObjectsOfType<ShipController>();

        if (ships.Length == 0 && showErrors)
        {
            EditorGUILayout.HelpBox("错误：场景中未找到 ShipController！\n请添加飞船 GameObject 并挂载 ShipController 脚本。", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ 找到 {ships.Length} 个飞船", MessageType.Info);

            foreach (var ship in ships)
            {
                Rigidbody rb = ship.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (rb.useGravity && showWarnings)
                    {
                        EditorGUILayout.HelpBox($"警告：{ship.name} 的 Rigidbody 启用了重力。\n建议禁用，因为使用了自定义引力系统。", MessageType.Warning);
                    }
                }
            }
        }
    }

    private void CheckPlanets()
    {
        EditorGUILayout.LabelField("Planets:", EditorStyles.boldLabel);
        Planet[] planets = FindObjectsOfType<Planet>();

        if (planets.Length == 0 && showWarnings)
        {
            EditorGUILayout.HelpBox("警告：场景中未找到 Planet！\n建议添加行星用于测试引力和捕获功能。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ 找到 {planets.Length} 个行星", MessageType.Info);

            foreach (var planet in planets)
            {
                SphereCollider mainCollider = planet.GetComponent<SphereCollider>();
                if (mainCollider == null && showErrors)
                {
                    EditorGUILayout.HelpBox($"错误：{planet.name} 缺少 SphereCollider 组件！", MessageType.Error);
                }
            }
        }
    }

    private void CheckObstacles()
    {
        EditorGUILayout.LabelField("Obstacles:", EditorStyles.boldLabel);
        Obstacle[] obstacles = FindObjectsOfType<Obstacle>();

        if (obstacles.Length == 0 && showInfo)
        {
            EditorGUILayout.HelpBox("信息：场景中未找到 Obstacle。\n可以添加障碍物用于测试碰撞功能。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ 找到 {obstacles.Length} 个障碍物", MessageType.Info);
        }
    }

    private void CheckTags()
    {
        EditorGUILayout.LabelField("标签检查:", EditorStyles.boldLabel);

        string[] requiredTags = { "Obstacle", "PlanetCapture", "Destination", "Ship" };
        bool allTagsExist = true;

        foreach (string tag in requiredTags)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
                if (showInfo)
                {
                    EditorGUILayout.HelpBox($"✓ 标签 '{tag}' 已存在", MessageType.Info);
                }
            }
            catch
            {
                allTagsExist = false;
                if (showWarnings)
                {
                    EditorGUILayout.HelpBox($"警告：标签 '{tag}' 不存在！\n请在 Tags and Layers 中创建此标签。", MessageType.Warning);
                }
            }
        }
    }

    private void CheckCamera()
    {
        EditorGUILayout.LabelField("摄像机:", EditorStyles.boldLabel);
        Camera mainCamera = Camera.main;

        if (mainCamera == null && showWarnings)
        {
            EditorGUILayout.HelpBox("警告：未找到 MainCamera！\nLaunchStation 需要摄像机进行鼠标射线投射。", MessageType.Warning);
        }
        else if (mainCamera != null && showInfo)
        {
            EditorGUILayout.HelpBox($"✓ 主摄像机已找到：{mainCamera.name}", MessageType.Info);
        }
    }
}