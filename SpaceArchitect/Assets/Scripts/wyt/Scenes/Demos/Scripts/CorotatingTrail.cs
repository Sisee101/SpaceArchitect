using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provide a co-rotating trail using a LineRenderer.
/// 
/// The reference to a planet defines a rotating frame in which the trail is fixed (so a point orbiting
/// in the same orbit as the planet will appear as a single point). 
/// 
/// Each point is recorded with respect to the CM of the system and 
/// 
/// Keep a 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CorotatingTrail : MonoBehaviour
{
    private const float TRAIL_DELTA = 1E-2f; 

    [SerializeField]
	private NBody nbody = null;

    [SerializeField]
    private NBody planet = null; 

    [SerializeField]
	private float trailTime = 10f;

	private LineRenderer lineR;

    private Vector3 rotationAxis;
    private float omega;

    private NBody centerBody; 

    private struct TrackPoint
    {
        public Vector3 pos;     // position wrt to center of mass
        public float time;
    }

    private List<TrackPoint> trailPoints;

    private GravityEngine ge;

    private Vector3 lastPos; 

    // Start is called before the first frame update
    void Start()
    {
        lineR = GetComponent<LineRenderer>();
        trailPoints = new List<TrackPoint>();
        ge = GravityEngine.Instance();
        ge.AddGEStartCallback(GEStart);
    }

    private void GEStart()
    {
        OrbitUniversal planetOrbit = planet.GetComponent<OrbitUniversal>();
        rotationAxis = planetOrbit.GetAxis().ToVector3();
        omega = (float) planetOrbit.GetAngularVelocity();
        centerBody = planetOrbit.centerNbody;
        lastPos = ge.GetPhysicsPosition(nbody);
    }

    void Update()
    {
    	if (!ge.IsSetup())
    		return;

        float time = ge.GetPhysicalTime();
        // the trail lives in Unity world space
        Vector3 centerOfMass = OrbitUtils.CenterOfMass(planet, centerBody).ToVector3();
        Vector3 pos = ge.GetPhysicsPosition(nbody) - centerOfMass;
        if ((pos - lastPos).magnitude > TRAIL_DELTA) {
            TrackPoint tp = new TrackPoint();
            tp.pos = pos;
            tp.time = time;
            trailPoints.Add(tp);
            lastPos = pos;
        }

        if (time > (trailPoints[0].time + trailTime)) {
            trailPoints.RemoveAt(0);
        }

        // Need to adjust points each time through. A bit greedy
        // Need to rotate points about the center of mass of the planet-sun combo
        Vector3[] points = new Vector3[trailPoints.Count];
 
        float angleDeg = 0; 
        for (int i = 0; i < points.Length; i++) {
            angleDeg = (time - trailPoints[i].time) * omega * Mathf.Rad2Deg;
            points[i] = Quaternion.AngleAxis(angleDeg, rotationAxis) * trailPoints[i].pos
                            + centerOfMass;
            points[i] = ge.MapPhyPosToWorld(points[i]);
        }
        lineR.positionCount = trailPoints.Count;
        lineR.SetPositions(points);
    }
}
