using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to control the rocket pitch and throttle during launch to orbit via Animation Curves
/// in the inspector. 
/// 
/// The time scale is set manually and the curves are taken to range over this timescale. Time 0
/// indicates when the engines have started. 
/// 
/// The flight path is determined by the desired eccentricity and the position of the launch point.
/// Eccentricity and launch point determine the direction of the normal to the desired orbital plane.
/// The pitch 0 thrust direction is (normal x pos). The pitch 90 position is aligned with pos.
///
/// The normal direction requires the inclination and the AN position via OmegaU.
/// For a launch point at (theta, phi) [polar coords]
/// 
/// The thrust curve indicates percent thrust.
/// </summary>
public class LaunchController : MonoBehaviour {

    //! maximum time for the controller in world seconds (GetTimeWorldSeconds)
    public float timeRange;
    private float timeRangePhysical; 

    public NBody ship;
    public NBody earth;

    public LatLongPoint launchPoint;

    [Header("abs(latitude) <= target inclination <= 180 ")]
    public float targetInclination;

    public bool launchEastwards = true;

    public RocketEngine engine;

    public SpaceshipRV shipModel; 

    //! pitch with respect to the local horizon (at launch 1=90 degrees, 0 = horizontal)
    public AnimationCurve pitchCurve;

    public AnimationCurve thrustCurve;

    private float timeStarted = -1f;

    private GravityEngine ge;

    private Vector3 lastAttitude;

    private Vector3 planeNormal;
    private Vector3 xAxis;
    private Vector3 yAxis;
    private Vector3 zAxis;
        
	// Use this for initialization
	void Start () {
        ge = GravityEngine.Instance();
        xAxis = new Vector3(1, 0, 0);
        yAxis = new Vector3(0, 1, 0);
        zAxis = new Vector3(0, 0, 1);

        // scale to GE time base
        timeRangePhysical = timeRange;

        if (launchPoint != null) {
            float latAbs = (float) Mathd.Abs(launchPoint.latitude);
            if (targetInclination < latAbs) {
                Debug.LogWarning("Cannot achieve desired inclination from launch point");
            } else {
                float phi = (float) launchPoint.longitude;
                if (phi < 0)
                    phi += 360.0f;
                // if we're on equator, omega=long
                float omegaU = phi * Mathf.Deg2Rad;
                float incl = targetInclination * Mathf.Deg2Rad;
                if (latAbs > 1E-3) {
                    // spherical trig. Vallado p338 and Appendix C
                    float sinBeta = Mathf.Cos(incl) / Mathf.Cos(latAbs*Mathf.Deg2Rad);
                    float beta = Mathf.Asin(sinBeta);
                    float tanOffset = Mathf.Tan(beta) * Mathf.Sin(latAbs * Mathf.Deg2Rad);
                    float omegaOffset = Mathf.Atan(tanOffset);
                    omegaU -= omegaOffset;
                    if (launchPoint.latitude < 0) {
                        // offset was to DN, so need to push to AN
                        omegaU += Mathf.PI;
                    } 
                }
                // XZ mode
                // tilt around x axis
                Vector3 planeTilt = -Mathf.Sin(incl) * zAxis + Mathf.Cos(incl) * yAxis;
                // rotate around Y
                planeNormal = new Vector3(Mathf.Cos(omegaU) * planeTilt.x - Mathf.Sin(omegaU) * planeTilt.z,
                                          planeTilt.y,
                                          Mathf.Sin(omegaU) * planeTilt.x + Mathf.Cos(omegaU) * planeTilt.z).normalized;
                Debug.LogFormat("Tilt={0} Plane normal={1} omegaU={2} i={3}",
                    planeTilt, planeNormal, omegaU * Mathf.Rad2Deg, Vector3.Angle(planeNormal, zAxis));
            }
        } else {
            Debug.LogError("Require a non-null Launch Point");
        }
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        // monitor for engine start (not ideal, but avoids linking to LaunchUI script) 
		if ((timeStarted < 0) && engine.engineOn) {
            timeStarted = ge.GetPhysicalTime();
            float pitch0 = pitchCurve.Evaluate(0);
            Vector3 localVertical = Vector3.Normalize(ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(earth));
            lastAttitude = Vector3.Cross(localVertical, Vector3.forward); // forward = (0,0,1)
            lastAttitude = Quaternion.AngleAxis(pitch0, Vector3.forward) * lastAttitude;
        }

        if (timeStarted > 0) {
            float t = (float)(ge.GetTimeWorldSeconds() - timeStarted) / timeRangePhysical;
            t = Mathf.Clamp(t, 0f, 1f);

            // pitch in range 0..90 as curve goes 0..1
            float pitch = pitchCurve.Evaluate(t) * 0.5f * Mathf.PI;
            // find the local vertical
            Vector3 localVertical = Vector3.Normalize(ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(earth));
            Vector3 tangent = Vector3.Cross(localVertical, planeNormal).normalized;
            if ((launchEastwards && (targetInclination > 90)) ||
                (!launchEastwards && (targetInclination < 90))) {
                tangent *= -1f;
            }

            // when pitch=0 aligned with tangent
            Vector3 attitude = Mathf.Sin(pitch) * localVertical + Mathf.Cos(pitch) * tangent;
            engine.SetThrustAxis(-attitude);
            shipModel.RotateToVector(attitude);

            // when attitude changes by 1 degree, re-compute trajectories
            if (Vector3.Angle(attitude, lastAttitude) > 1f) {
                ge.TrajectoryRestart();
                lastAttitude = attitude;
            }

            // thrust
            float thrust = thrustCurve.Evaluate(t);
            engine.SetThrottlePercent(100f * thrust);
        }

	}

    // Debug assist
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector3 pos = ge.GetPhysicsPosition(ship);
            Vector3 tangent = Vector3.Cross(planeNormal, pos.normalized);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pos, tangent);
            Vector3 localVertical = Vector3.Normalize(ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(earth));
            Gizmos.color = Color.black;
            Gizmos.DrawRay(pos, localVertical);
        }
    }
}
