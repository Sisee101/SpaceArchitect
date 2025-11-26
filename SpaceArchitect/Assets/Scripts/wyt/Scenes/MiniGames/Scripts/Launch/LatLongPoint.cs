 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine a point on a sphere model corresponding to the latitude and logitude specified.
///
/// Has an editor script to move the transform to the location specified. 
/// </summary>
public class LatLongPoint : MonoBehaviour
{
    public double latitude = 0.0;
    public double longitude = 0.0;

    public bool rayCast = true;

    public GameObject sphereModel;

    private float normalError = 0f;

    private Vector3 normal;

    void Awake()
    {
        ComputePosition();
    }

    public void ComputePosition()
    {
        double theta = (90.0 - latitude) * Mathd.Deg2Rad;
        double phi = longitude * Mathd.Deg2Rad; 
        double r = sphereModel.transform.localScale.x * 0.5;
        // XZ mode
        double x = r * Mathd.Cos(phi) * Mathd.Sin(theta);
        double z = r * Mathd.Sin(phi) * Mathd.Sin(theta);
        double y = r *  Mathd.Cos(theta);
        Vector3 pos = new Vector3((float)x, (float)y, (float)z);
        // want a point ON the sphere. Due to triangulation point on the surface of the sphere
        // will not in general be at the radius of the sphere. Also normal to sphere may not be the
        // normal to the triangle on the sphere.
        // Need to cast from outside in.
        Ray ray = new Ray(1.1f*pos, -pos);
        RaycastHit hit;
        normal = pos.normalized;
        if (rayCast)
        {
            // Debug.LogFormat("Cast from {0} using {1}", pos, ray);
            if (Physics.Raycast(ray, out hit))
            {
                pos = hit.point;
                normal = hit.normal;
            }
            else
            {
                Debug.LogWarning("Did not hit sphere. Please use a MeshCollider");
            }
        }
        transform.position = pos;
        normalError = Vector3.Angle(pos, normal);
        // Nbody needs the absolute position. Assume only one Nbody child (Launch scene)
        NBody nbody = GetComponentInChildren<NBody>();
        if (nbody != null) {
            nbody.initialPos = pos;
            nbody.transform.localPosition = Vector3.zero;
        }
        // set orientation so Y-axis aligns with normal
        Quaternion q = new Quaternion();
        q.SetFromToRotation(Vector3.up, normal);
        transform.rotation = q;
        // Find the Multistage engine and set the thrust axis to the local normal
        MultiStageEngine mse = GetComponentInChildren<MultiStageEngine>();
        if (mse != null)
        {
            mse.SetThrustAxis(-normal);
        }

        
    }

    public Vector3 GetNormal()
    {
        return normal;
    }

    public string Info()
    {
        float r = sphereModel.transform.localScale.x * 0.5f;
        float p = transform.position.magnitude;
        return string.Format("Offset from true radius: {0} Normal Angle Error: {1}", p - r,
            normalError);
    }
}
