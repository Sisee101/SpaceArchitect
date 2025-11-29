using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WarpGridController : MonoBehaviour
{
    public SpriteRenderer targetRenderer; // assign the WarpGrid SpriteRenderer
    public int maxPlanets = 8;
    public float globalMassScale = 1.0f; // ���ڵ���������ֵ���Ӿ��ϵı���
    public float minMass = 0.01f; // �������������

    private Material mat;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            Debug.LogError("WarpGridController needs a SpriteRenderer.");
            enabled = false;
            return;
        }
        // material instance so we can set arrays safely
        mat = targetRenderer.sharedMaterial;
    }

    void Update()
    {
        if (mat == null) return;

        // find NBody objects (planets)
        // depends on project: try find by component NBody if present
        // fallback: find by name prefix "Planet"
        var planets = new List<Transform>();

        // Try to find NBody components first
        var nbList = Object.FindObjectsOfType(typeof(Component), true); // fallback generic
        // more reliable: try to get NBody type via reflection if compiled in different assembly
        // but simplest: find by name "Planet" (case-insensitive)
        foreach (Transform t in transform.root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains("planet"))
            {
                planets.Add(t);
            }
        }

        // If none found by name, attempt to find any GameObjects with "NBody" component via reflection:
        if (planets.Count == 0)
        {
            var all = Object.FindObjectsOfType<GameObject>();
            foreach (var go in all)
            {
                // try get component named "NBody"
                var comp = go.GetComponent("NBody");
                if (comp != null)
                {
                    planets.Add(go.transform);
                }
            }
        }

        // now fill arrays for shader
        int count = Mathf.Min(maxPlanets, planets.Count);
        mat.SetInt("_PlanetCount", count);

        // Prepare vector array (shader expects MAX_PLANETS length, but SetVectorArray can be variable)
        var vecs = new List<Vector4>();
        for (int i = 0; i < count; i++)
        {
            var t = planets[i];
            // estimate mass: try to read "mass" field on NBody if exists, else fallback to scale
            float mass = 1f;
            var nbComp = t.GetComponent("NBody");
            if (nbComp != null)
            {
                // try reflect mass field/property
                var type = nbComp.GetType();
                var f = type.GetField("mass");
                if (f != null)
                {
                    var mv = f.GetValue(nbComp);
                    if (mv is float) mass = (float)mv;
                    else if (mv is double) mass = (float)(double)mv;
                }
                else
                {
                    var p = type.GetProperty("mass");
                    if (p != null)
                    {
                        var mv = p.GetValue(nbComp, null);
                        if (mv is float) mass = (float)mv;
                        else if (mv is double) mass = (float)(double)mv;
                    }
                }
            }
            else
            {
                // fallback: use scale.x as mass proxy
                mass = Mathf.Max(t.localScale.x, 0.01f);
            }

            mass = Mathf.Max(mass * globalMassScale, minMass);

            Vector3 worldPos = t.position;
            vecs.Add(new Vector4(worldPos.x, worldPos.y, mass, 0f));
        }

        // pad with zeros up to maxPlanets to avoid uninitialized behavior
        for (int i = count; i < maxPlanets; i++) vecs.Add(Vector4.zero);

        mat.SetInt("_PlanetCount", count);
        mat.SetVectorArray("_Planets", vecs);
    }

    // --- GravityWellCapture integration helpers ---
    private static readonly int CapturePlanetID = Shader.PropertyToID("_CapturePlanet");
    private static readonly int CaptureProgressID = Shader.PropertyToID("_CaptureProgress");
    private static readonly int CaptureRadiusID = Shader.PropertyToID("_CaptureRadius");

    /// <summary>
    /// 可被 GravityWellCapture 调用，用于向材质传递捕获阶段的可视化数据。
    /// 如果当前材质未定义这些属性，会自动忽略。
    /// </summary>
    public void ApplyCaptureEffect(Transform planet, float progress, float normalizedRadius)
    {
        if (mat == null || planet == null) return;

        if (mat.HasProperty(CapturePlanetID))
        {
            Vector3 pos = planet.position;
            mat.SetVector(CapturePlanetID, new Vector4(pos.x, pos.y, pos.z, 1f));
        }

        if (mat.HasProperty(CaptureProgressID))
        {
            mat.SetFloat(CaptureProgressID, Mathf.Clamp01(progress));
        }

        if (mat.HasProperty(CaptureRadiusID))
        {
            mat.SetFloat(CaptureRadiusID, Mathf.Clamp01(normalizedRadius));
        }
    }

    /// <summary>
    /// 重置捕获可视化。
    /// </summary>
    public void ResetWarp()
    {
        if (mat == null) return;

        if (mat.HasProperty(CaptureProgressID))
        {
            mat.SetFloat(CaptureProgressID, 0f);
        }
        if (mat.HasProperty(CaptureRadiusID))
        {
            mat.SetFloat(CaptureRadiusID, 0f);
        }
    }
}
