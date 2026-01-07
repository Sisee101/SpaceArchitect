using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace SpaceArchitect.UI
{
    public class GameResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button backToMainMapButton;

        private UnityAction onBackToMainMapClick;

        private void Awake()
        {
            if (backToMainMapButton != null)
            {
                backToMainMapButton.onClick.AddListener(OnBackToMainMapClicked);
            }
            Hide();
        }

        public void Show(string title, string desc, UnityAction onBackMainMap = null)
        {
            if (titleText != null)
                titleText.text = title;
            if (descriptionText != null)
                descriptionText.text = desc;
            onBackToMainMapClick = onBackMainMap;
            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            onBackToMainMapClick = null;
        }

        private void OnBackToMainMapClicked()
        {
            onBackToMainMapClick?.Invoke();
        }
    }
}
