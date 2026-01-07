using System;
using System.Text;
using UnityEngine;
using SpaceArchitect.Data;
using TMPro;
using UnityEngine.UI;

namespace SpaceArchitect.UI
{
    public class LevelSelectPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI requirementText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button travelButton;
        [SerializeField] private Button cancelButton;

        private Action onTravelClicked;
        private Action onCancelClicked;
        private PlanetData currentData;

        private void Awake()
        {
            Hide();
            BindButtons();
        }

        public void Show(PlanetData data, Action travelCallback, Action cancelCallback)
        {
            currentData = data;
            onTravelClicked = travelCallback;
            onCancelClicked = cancelCallback;

            if (root != null)
            {
                root.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = data?.DisplayName ?? "未知目标";
            }

            if (descriptionText != null)
            {
                descriptionText.text = data?.Description ?? string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(data?.Icon != null);
                iconImage.sprite = data?.Icon;
            }

            if (requirementText != null)
            {
                requirementText.text = BuildRequirementText(data);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            onTravelClicked = null;
            onCancelClicked = null;
            currentData = null;
        }

        private void BindButtons()
        {
            if (travelButton != null)
            {
                travelButton.onClick.AddListener(() => onTravelClicked?.Invoke());
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() => onCancelClicked?.Invoke());
            }
        }

        private static string BuildRequirementText(PlanetData data)
        {
            if (data == null || data.ResourceRequirements == null || data.ResourceRequirements.Count == 0)
            {
                return "无额外需求";
            }

            var builder = new StringBuilder();
            foreach (var requirement in data.ResourceRequirements)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }
                builder.Append(requirement.ToString());
            }

            return builder.ToString();
        }
    }
}