using System;
using UnityEngine;
using SpaceArchitect.Data;
using TMPro;
using UnityEngine.UI;

namespace SpaceArchitect.UI
{
    public class ConfirmTravelPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;
        private Action onCancel;
        private PlanetData currentData;

        private void Awake()
        {
            Hide();
            BindButtons();
        }

        public void Show(PlanetData data, Action confirmCallback, Action cancelCallback)
        {
            currentData = data;
            onConfirm = confirmCallback;
            onCancel = cancelCallback;

            if (root != null)
            {
                root.SetActive(true);
            }

            if (bodyText != null)
            {
                var planetName = data != null ? data.DisplayName : "目标";
                bodyText.text = $"确定前往 {planetName} 吗？";
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            onConfirm = null;
            onCancel = null;
            currentData = null;
        }

        private void BindButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(() => onConfirm?.Invoke());
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() => onCancel?.Invoke());
            }
        }
    }
}

