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
    }
}