using System.Collections.Generic;
using UnityEngine;
using SpaceArchitect.Data;

namespace SpaceArchitect.MainMap
{
    /// <summary>
    /// 场景启动时根据 PlanetData 列表生成行星占位球体，并挂载 PlanetSelector。
    /// </summary>
    public class MainMapPlanetBootstrapper : MonoBehaviour
    {
        [SerializeField] private PlanetSelector planetTemplate;
        [SerializeField] private List<PlanetData> planets = new();
        [SerializeField] private bool createPrimitiveWhenTemplateMissing = true;
        [SerializeField] private Material primitiveMaterial;

        private void Start()
        {
            SpawnPlanets();
        }

        private void SpawnPlanets()
        {
            foreach (var data in planets)
            {
                if (data == null)
                {
                    continue;
                }

                CreatePlanetInstance(data);
            }
        }

        private void CreatePlanetInstance(PlanetData data)
        {
            PlanetSelector selector = null;

            if (planetTemplate != null)
            {
                selector = Instantiate(planetTemplate, data.MapPosition, Quaternion.identity, transform);
                selector.name = $"Planet_{data.DisplayName}";
                selector.Initialize(data);
                return;
            }

            if (!createPrimitiveWhenTemplateMissing)
            {
                Debug.LogWarning($"缺少 PlanetSelector 模板，且未启用自动创建。Planet: {data.DisplayName}");
                return;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(transform);
            go.transform.position = data.MapPosition;
            go.transform.localScale = Vector3.one * data.DisplayRadius * 2f;
            go.name = $"Planet_{data.DisplayName}";

            if (primitiveMaterial != null)
            {
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = primitiveMaterial;
            }

            selector = go.AddComponent<PlanetSelector>();
            selector.Initialize(data);
        }
    }
}

