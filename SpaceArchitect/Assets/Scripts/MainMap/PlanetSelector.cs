using UnityEngine;
using SpaceArchitect.Data;

namespace SpaceArchitect.MainMap
{
    [RequireComponent(typeof(Collider))]
    public class PlanetSelector : MonoBehaviour
    {
        [SerializeField] private PlanetData planetData;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;

        private MainMapController controller;
        private Material runtimeMaterial;

        public PlanetData Data => planetData;
        public Transform FocusPoint => transform;

        private void Awake()
        {
            controller = FindAnyObjectByType<MainMapController>();
            CacheRenderer();
        }

        private void OnEnable()
        {
            ApplyColor(defaultColor);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void OnMouseEnter()
        {
            ApplyColor(hoverColor);
        }

        private void OnMouseExit()
        {
            ApplyColor(defaultColor);
        }

        private void OnMouseUpAsButton()
        {
            // TODO: 原分支的 Core.GameManager 已不存在
            // 如需在正交游戏模式下禁用点击，请重新实现相关逻辑
            
            controller?.HandlePlanetClicked(this);
        }

        public void Initialize(PlanetData data)
        {
            planetData = data;
            UpdateVisualFromData();
        }

        private void CacheRenderer()
        {
            if (targetRenderer != null)
            {
                return;
            }

            targetRenderer = GetComponentInChildren<Renderer>();
        }

        private void ApplyColor(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = Instantiate(targetRenderer.sharedMaterial);
                targetRenderer.material = runtimeMaterial;
            }

            runtimeMaterial.color = color;
        }

        private void UpdateVisualFromData()
        {
            if (planetData == null)
            {
                return;
            }

            transform.position = planetData.MapPosition;
            transform.localScale = Vector3.one * planetData.DisplayRadius * 2f;

            if (targetRenderer != null)
            {
                defaultColor = planetData.DebugColor;
                ApplyColor(defaultColor);
            }
        }
    }
}