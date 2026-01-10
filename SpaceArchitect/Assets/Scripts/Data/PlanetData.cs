using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceArchitect.Data
{
    [CreateAssetMenu(menuName = "SpaceArchitect/Data/Planet", fileName = "PlanetData")]
    public class PlanetData : ScriptableObject
    {
        [SerializeField] private string planetName = "New Planet";
        [SerializeField] [TextArea] private string description = "描述";
        [SerializeField] private Sprite icon;
        [SerializeField] private bool isDestination;
        [SerializeField] private Vector3 mapPosition = Vector3.zero;
        [SerializeField] [Min(0.1f)] private float displayRadius = 1.5f;
        [SerializeField] private Color debugColor = Color.cyan;
        [SerializeField] private ResourceRequirement[] resourceRequirements = Array.Empty<ResourceRequirement>();
        [SerializeField] private string gameplaySceneName = "02_Gameplay";

        public string DisplayName => planetName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsDestination => isDestination;
        public Vector3 MapPosition => mapPosition;
        public float DisplayRadius => displayRadius;
        public Color DebugColor => debugColor;
        public string GameplaySceneName => gameplaySceneName;
        public IReadOnlyList<ResourceRequirement> ResourceRequirements => resourceRequirements;

#if UNITY_EDITOR
        private void OnValidate()
        {
            displayRadius = Mathf.Max(0.1f, displayRadius);
        }
#endif
    }

    [Serializable]
    public struct ResourceRequirement
    {
        public string resourceId;
        public int amount;

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return "未命名资源";
            }

            return $"{resourceId} x{Mathf.Max(1, amount)}";
        }
    }
}