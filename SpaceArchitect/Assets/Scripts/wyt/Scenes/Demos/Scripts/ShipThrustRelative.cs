using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Simple controller to demonstrate how to determine a velocity change for a thrust in a direction relative to the orbit.
/// </summary>
public class ShipThrustRelative : MonoBehaviour
{
    [Header("Key controls for orbit relative thrust:")]
    [Header("A/D prograde/retrograde")]
    [Header("W/S cross-track up/dn")]
    [Header("Q/E radial out/in")]
    public NBody ship;
    public NBody centerNbody;

    [Header("Thrust as a percent of relative velocity")]
    public double thrustPercent= 1.0;

    private GravityEngine ge; 

    private class DirectionPerKey
    {
        public KeyCode key;
        //! dir is in format of x=prograde, y=cross-track z=radial
        public Vector3 dir; 


        public DirectionPerKey(KeyCode k, Vector3 d)
        {
            key = k;
            dir = d;
        }
    };

    private List<DirectionPerKey> dirPerKey; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        dirPerKey = new List<DirectionPerKey>();
        dirPerKey.Add(new DirectionPerKey(KeyCode.A, new Vector3(1, 0, 0)));
        dirPerKey.Add(new DirectionPerKey(KeyCode.D, new Vector3(-1, 0, 0)));
        dirPerKey.Add(new DirectionPerKey(KeyCode.W, new Vector3(0, 1, 0)));
        dirPerKey.Add(new DirectionPerKey(KeyCode.S, new Vector3(0, -1, 0)));
        dirPerKey.Add(new DirectionPerKey(KeyCode.Q, new Vector3(0, 0, 1)));
        dirPerKey.Add(new DirectionPerKey(KeyCode.E, new Vector3(0, 0, -1)));
    }

    // Update is called once per frame
    void Update()
    {
        foreach(DirectionPerKey dpk in dirPerKey) {
            if (Input.GetKeyDown(dpk.key)) {
                // get current orbital positions relative to center
                Vector3d rShip = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(centerNbody);
                Vector3d vShip = ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(centerNbody);
                Vector3d radialAxis = rShip.normalized;
                // orbit axis is r x v
                Vector3d crossAxis = Vector3d.Cross(rShip, vShip).normalized;
                // find prograde axis by fact it is ortho to other two
                Vector3d progradeAxis = Vector3d.Cross(crossAxis, radialAxis);
                Vector3d thrustDir = dpk.dir.x * progradeAxis +
                                     dpk.dir.y * crossAxis +
                                     dpk.dir.z * radialAxis;
                float thrust =  (float) (vShip.magnitude * (thrustPercent/100.0));
                ge.ApplyImpulse(ship, thrust * thrustDir.ToVector3());
            }
        }
    }
}
