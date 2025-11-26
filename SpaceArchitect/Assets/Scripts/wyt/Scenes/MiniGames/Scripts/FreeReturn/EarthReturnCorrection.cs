
using UnityEngine;

/// <summary>
/// Determine a velocity correction to fine tune the return earth perigee as part of a
/// TEI return to earth scenario.
///
/// Press C once within the earth's sphere of influence. 
/// </summary>
public class EarthReturnCorrection : MonoBehaviour
{
    [Header("Press C to correct return course")]
    public ComputeTEIController teiController;
 
    private double targetPerigee; 
    private GravityEngine ge; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        targetPerigee = teiController.targetPerigeeKM * ge.lengthScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) {
            NBody ship = teiController.ship;
            NBody earth = teiController.earth;
            Vector3d r = ge.GetPositionDoubleV3(ship);
            Vector3d v = ge.GetVelocityDoubleV3(ship);
            // determine e for new orbit
            OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r, v, earth, true);
            double nu = oe.GetPhase();
            // use r=p/(1+e cos(nu)) at current position and at perigee and solve for p, e
            double e = (targetPerigee - r.magnitude) / (r.magnitude * Mathd.Cos(nu) - targetPerigee);
            double p = targetPerigee * (1 + e); 
            Debug.LogFormat("Target orbit e={0} p={1}", e, p);
            // Setup oe for what we want and determine what V we need here
            oe.p = p;
            oe.ecc = e;
            Vector3d r1 = Vector3d.zero;
            Vector3d v1 = Vector3d.zero;
            OrbitUtils.COEtoRV(oe, earth, ref r1, ref v1, relativePos: true);
            Debug.LogFormat("New Velocity = {0} dV={1}", v1, (v - v1).magnitude);
            // could do a maneuver, but just set the velocity for now
            ge.SetVelocityDoubleV3(ship, v1);
        }
    }
}
