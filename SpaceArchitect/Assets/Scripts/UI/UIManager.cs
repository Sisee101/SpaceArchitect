using System;
using UnityEngine;
using SpaceArchitect.Data;

namespace SpaceArchitect.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private LevelSelectPanel levelSelectPanel;
        [SerializeField] private ConfirmTravelPanel confirmTravelPanel;

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