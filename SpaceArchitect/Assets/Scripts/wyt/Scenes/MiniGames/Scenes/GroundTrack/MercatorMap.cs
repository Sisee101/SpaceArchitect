using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hold map information for the screen dimensions of a Mercator map of a planet surface.
/// 
/// MercatorTrack instances will reference this to determine the position of points in their line renderers.
/// </summary>
public class MercatorMap : MonoBehaviour
{
    // note that default plane is 10 x 10 units
    [SerializeField]
    private float width = 100.0f;

    [SerializeField]
    private float height = 50.0f;

    [SerializeField]
    private float longitudeOffsetRadians = Mathf.PI;

    [SerializeField]
    private Vector3 mapXDirection = Vector3.right;

    [SerializeField]
    private Vector3 mapYDirection = Vector3.forward;

    [SerializeField]
    private PlanetRotation planetRotation = null;

    private Vector3 origin = Vector3.zero;
    private Vector3 relativeYaxis;
    private Vector3 mapNormal; 
    private float omega; 

    void Start()
    {
        origin = transform.position - 0.5f * width * mapXDirection - 0.5f * height * mapYDirection;
        relativeYaxis = Vector3.Cross(planetRotation.GetAxis(), planetRotation.GetLongitudeReference());
        omega = planetRotation.GetOmega();
        mapNormal = Vector3.Cross(mapXDirection, mapYDirection).normalized;
    }

    private const float LAT_LIMIT = 85.0f * Mathf.Deg2Rad;
    private const float TWO_PI = 2.0f * Mathf.PI;

    public float GetMapWrapDistance()
    {
        return Mathf.Min(0.5f * height, 0.5f * width);
    }

    public Vector3 GetMapNormal()
    {
        return mapNormal;
    }

    /// <summary>
    /// Project the given relative orbit position onto a plane using the Mercator projection.
    /// </summary>
    /// <param name="orbitPosition"></param>
    /// <returns></returns>
    public Vector3 Project(Vector3 orbitPosition, float time)
    {
        // Determine spherical polar co-ordinates of orbit position
        // 1) Determine (x, y, z) with respect to the rotation axis
        float z_component = Vector3.Dot(orbitPosition.normalized, planetRotation.GetAxis());
        Vector3 xy_component = orbitPosition.normalized - z_component * planetRotation.GetAxis();
        // define x axis as aligned with longitude
        float x_component = Vector3.Dot(xy_component, planetRotation.GetLongitudeReference());
        float y_component = Vector3.Dot(xy_component, relativeYaxis);
        // 2) Get theta (from z) and phi (from longitudeRef)
        float theta = Mathf.Acos(z_component); // denom orbitPosition is normalized
        float phi = Mathf.Atan2(y_component, x_component);

        // Mercator projection (see e.g. Gravity, Hartle p26). Use L for width to align with notation
        // Note that a true Mercator projection maps the poles to +/- infinity and that some form of truncation 
        // needs to be applied.
        // Truncation at +/-  85 degress
        float L = width;
        phi = phi + longitudeOffsetRadians;
        phi -= time * omega;
        phi = phi % TWO_PI;
        if (phi < 0) {
            phi += TWO_PI;
        } else if (phi > TWO_PI) {
            phi -= TWO_PI;
        }
        float x = L * phi / TWO_PI;
        float latitude = 0.5f * Mathf.PI - theta;
        latitude = Mathf.Clamp(latitude, -LAT_LIMIT, LAT_LIMIT);
        // HACK - unique to scene or LH issue??
        latitude *= -1.0f;
        float y = height / TWO_PI * Mathf.Log(Mathf.Tan(0.25f * Mathf.PI + 0.5f * latitude));
        //Debug.LogFormat("theta={0} phi={1} x={2} y={3} latitude={4} ",
        //    theta * Mathf.Rad2Deg, phi * Mathf.Rad2Deg, x, y, latitude * Mathf.Rad2Deg);

        return x * mapXDirection + (y + height * 0.5f) * mapYDirection + origin;
    }
}
