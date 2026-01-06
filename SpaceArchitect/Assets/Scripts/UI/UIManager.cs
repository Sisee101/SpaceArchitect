using System;
using UnityEngine;
using SpaceArchitect.Data;

namespace SpaceArchitect.UI
{
    /// <summary>
    /// UI管理器（单例）
    /// 统一管理所有UI面板的显示和隐藏
    /// 包含主菜单UI和游戏内UI（如行星选择面板）
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        
        /// <summary>
        /// 获取UIManager单例
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("主菜单面板")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        
        [Header("行星图鉴面板")]
        [SerializeField] private PlanetEncyclopediaPanel planetEncyclopediaPanel;
        
        [Header("设置面板")]
        [SerializeField] private SettingsPanel settingsPanel;
        
        [Header("游戏内UI面板")]
        [SerializeField] private LevelSelectPanel levelSelectPanel;
        [SerializeField] private ConfirmTravelPanel confirmTravelPanel;
        
        void Awake()
        {
            // 确保单例
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("检测到多个UIManager实例，销毁重复的实例");
                Destroy(gameObject);
                return;
            }
            
            // 初始化：显示主菜单，隐藏其他面板（如果存在）
            InitializePanels();
        }
        
        /// <summary>
        /// 初始化所有面板
        /// </summary>
        private void InitializePanels()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
            }
            
            if (planetEncyclopediaPanel != null)
            {
                planetEncyclopediaPanel.Hide();
            }
            
            if (settingsPanel != null)
            {
                settingsPanel.Hide();
            }
            
            if (levelSelectPanel != null)
            {
                levelSelectPanel.Hide();
            }
            
            if (confirmTravelPanel != null)
            {
                confirmTravelPanel.Hide();
            }
        }
        
        // ========== 主菜单UI功能 ==========
        
        /// <summary>
        /// 显示主菜单
        /// </summary>
        public void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
            }
        }
        
        /// <summary>
        /// 显示行星图鉴
        /// </summary>
        public void ShowPlanetEncyclopedia()
        {
            HideAllPanels();
            if (planetEncyclopediaPanel != null)
            {
                planetEncyclopediaPanel.Show();
            }
        }
        
        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void ShowSettings()
        {
            HideAllPanels();
            if (settingsPanel != null)
            {
                settingsPanel.Show();
            }
        }
        
        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        private void HideAllPanels()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Hide();
            }
            
            if (planetEncyclopediaPanel != null)
            {
                planetEncyclopediaPanel.Hide();
            }
            
            if (settingsPanel != null)
            {
                settingsPanel.Hide();
            }
            
            if (levelSelectPanel != null)
            {
                levelSelectPanel.Hide();
            }
            
            if (confirmTravelPanel != null)
            {
                confirmTravelPanel.Hide();
            }
        }
        
        /// <summary>
        /// 返回主菜单（从其他面板返回）
        /// </summary>
        public void ReturnToMainMenu()
        {
            ShowMainMenu();
        }
        
        // ========== 游戏内UI功能 ==========
        
        /// <summary>
        /// 显示行星信息（游戏内UI）
        /// </summary>
        public void ShowPlanetInfo(PlanetData data, Action onTravelConfirmed, Action onCancelled)
        {
            if (levelSelectPanel == null)
            {
                Debug.LogWarning("LevelSelectPanel 未配置，无法显示行星信息。");
                return;
            }

            levelSelectPanel.Show(
                data,
                () => ShowConfirmPanel(data, onTravelConfirmed),
                () =>
                {
                    confirmTravelPanel?.Hide();
                    levelSelectPanel.Hide();
                    onCancelled?.Invoke();
                });
        }

        /// <summary>
        /// 隐藏所有游戏内UI
        /// </summary>
        public void HideAll()
        {
            levelSelectPanel?.Hide();
            confirmTravelPanel?.Hide();
        }

        private void ShowConfirmPanel(PlanetData data, Action onConfirmed)
        {
            if (confirmTravelPanel == null)
            {
                onConfirmed?.Invoke();
                return;
            }

            confirmTravelPanel.Show(
                data,
                () =>
                {
                    confirmTravelPanel.Hide();
                    levelSelectPanel.Hide();
                    onConfirmed?.Invoke();
                },
                () => confirmTravelPanel.Hide());
        }
    }
}
