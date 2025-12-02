using System;
using SpaceArchitect.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceArchitect.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoLoadGameplayScene = false;
        [SerializeField] private string defaultGameplayScene = "02_Gameplay";

        public event Action<PlanetData> GameplayRequested;

        // 游戏状态定义
        public enum GameState
        {
            MainMap,
            OrthographicGameplay,
            GameOver
        }
        public GameState State { get; private set; } = GameState.MainMap;

        // 摄像机与控制器的引用（在Inspector中拖拽赋值或自动查找）
        [Header("相机与控制器引用")]
        [SerializeField] private Camera mainMapCamera;
        [SerializeField] private Camera orthoCamera;
        [SerializeField] private MainMap.MainMapController mainMapController;
        [SerializeField] private MainMap.PlanetCameraController planetCameraController;

        [Header("UI引用")]
        [SerializeField] private UI.GameResultPanel gameResultPanel;

        // 公开访问器
        public Camera MainMapCamera => mainMapCamera;
        public Camera OrthoCamera => orthoCamera;
        public MainMap.MainMapController MainMapController => mainMapController;
        public MainMap.PlanetCameraController PlanetCameraController => planetCameraController;
        public UI.GameResultPanel GameResultPanel => gameResultPanel;

        // 全局事件: 游戏结束
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // 自动查找并赋值场景中的引用
            AutoAssignReferences();
        }

        /// <summary>
        /// 自动查找场景中的关键引用对象
        /// </summary>
        private void AutoAssignReferences()
        {
            // 查找主地图相机（通常是 Main Camera）
            if (mainMapCamera == null)
            {
                mainMapCamera = Camera.main;
                if (mainMapCamera != null)
                {
                    Debug.Log("[GameManager] 自动找到主地图相机: " + mainMapCamera.name);
                }
            }

            // 查找 MainMapController（可能在同一对象或子对象上）
            if (mainMapController == null)
            {
                mainMapController = FindObjectOfType<MainMap.MainMapController>();
                if (mainMapController != null)
                {
                    Debug.Log("[GameManager] 自动找到 MainMapController: " + mainMapController.name);
                }
            }

            // 查找 PlanetCameraController
            if (planetCameraController == null)
            {
                planetCameraController = FindObjectOfType<MainMap.PlanetCameraController>();
                if (planetCameraController != null)
                {
                    Debug.Log("[GameManager] 自动找到 PlanetCameraController: " + planetCameraController.name);
                    // 正交相机就是 PlanetCameraController 上的相机
                    orthoCamera = planetCameraController.Camera;
                }
            }

            // 查找 GameResultPanel（使用 includeInactive 参数查找包括未激活的对象）
            if (gameResultPanel == null)
            {
                gameResultPanel = FindObjectOfType<UI.GameResultPanel>(true);
                if (gameResultPanel != null)
                {
                    Debug.Log("[GameManager] 自动找到 GameResultPanel: " + gameResultPanel.name);
                }
                else
                {
                    Debug.LogWarning("[GameManager] 未找到 GameResultPanel，请在 Inspector 中手动拖拽赋值！");
                }
            }
        }

        private void OnEnable()
        {
            OnGameOver += ShowGameResultUI;
        }
        private void OnDisable()
        {
            OnGameOver -= ShowGameResultUI;
        }

        private void ShowGameResultUI()
        {
            if (GameResultPanel != null)
            {
                GameResultPanel.Show("Game Over", "You clicked a planet. Game Over!", OnBackToMainMap);
            }
        }

        /// <summary>
        /// 返回主地图（公开方法，可在 Inspector 按钮事件中调用）
        /// </summary>
        public void ReturnToMainMap()
        {
            OnBackToMainMap();
        }

        private void OnBackToMainMap()
        {
            if (GameResultPanel != null)
            {
                GameResultPanel.Hide();
            }
            // 切主地图状态及相机/UI输入等
            EnterMainMapMode();
            UnregisterPlanet2DClickHandler();
            if (MainMapController != null)
            {
                MainMapController.RestoreToMainMapCamera();
            }
            if (MainMapCamera != null) MainMapCamera.enabled = true;
            if (OrthoCamera != null) OrthoCamera.enabled = false;
        }

        public void EnterGameplay(PlanetData destination)
        {
            if (destination == null)
            {
                Debug.LogWarning("GameManager.EnterGameplay 被调用，但 destination 为空。");
                return;
            }

            GameplayRequested?.Invoke(destination);

            if (autoLoadGameplayScene)
            {
                var sceneName = string.IsNullOrWhiteSpace(destination.GameplaySceneName)
                    ? defaultGameplayScene
                    : destination.GameplaySceneName;

                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                }
            }
        }

        public void SetGameState(GameState state)
        {
            State = state;
            // 可加事件通知或日志
            Debug.Log($"[GameManager] 游戏状态切换: {state}");
        }

        // 游戏逻辑入口: 进入正交视角
        public void EnterOrthographicMode()
        {
            SetGameState(GameState.OrthographicGameplay);
            // 视角、输入等切换可以随后补充
        }

        // 游戏逻辑入口: 进入主地图
        public void EnterMainMapMode()
        {
            SetGameState(GameState.MainMap);
        }

        // 触发游戏结束
        public void GameOver()
        {
            SetGameState(GameState.GameOver);
            OnGameOver?.Invoke();
        }

        public void RegisterPlanet2DClickHandler()
        {
            if (PlanetCameraController != null)
            {
                PlanetCameraController.OnPlanetClicked2D += HandlePlanetClickIn2D;
            }
        }
        public void UnregisterPlanet2DClickHandler()
        {
            if (PlanetCameraController != null)
            {
                PlanetCameraController.OnPlanetClicked2D -= HandlePlanetClickIn2D;
            }
        }
        private void HandlePlanetClickIn2D(MainMap.PlanetSelector selector)
        {
            if (State == GameState.OrthographicGameplay)
            {
                GameOver();
            }
        }
    }
}