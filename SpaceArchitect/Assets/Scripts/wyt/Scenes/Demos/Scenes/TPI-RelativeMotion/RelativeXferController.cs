using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller to demonstrate the use of the RelativeMotion class.  
/// </summary>
public class RelativeXferController : MonoBehaviour
{

    [SerializeField]
    private NBody ship = null;

    [SerializeField]
    private NBody target = null;

    [SerializeField]
    private NBody planet = null;

    public enum RendezvousAfter { TARGET_ANGLE, TIME };
    [SerializeField]
    private RendezvousAfter rendezvousAfter = RendezvousAfter.TARGET_ANGLE;

    [SerializeField]
    private double rendezvousParam = 130.0;

    private OrbitUniversal shipOrbit;

    private OrbitUniversal targetOrbit;

    private GravityEngine ge;

    RelativeMotion relMotion;

    private bool burnDone = false; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        ge.AddGEStartCallback(GECallback);
        shipOrbit = ship.GetComponent<OrbitUniversal>();
        targetOrbit = target.GetComponent<OrbitUniversal>();
    }

    private void GECallback() {
         relMotion = new RelativeMotion(ship, target, planet);
    }

    private void RdvsCompleteCallback(Maneuver m) {
        Debug.LogFormat("Arrival: deltaR={0} deltaV={1}",
            (ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target)).magnitude,
            (ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(target)).magnitude);
    }

    // Check for the target angle and execute the TPI burn when it occurs
    private void FixedUpdate() {
        // Set trigger when trigger angle is acheived
        if (ge.IsSetup() && !burnDone) {
            double timeToRdvs = rendezvousParam; 
            if (rendezvousAfter == RendezvousAfter.TARGET_ANGLE) {
                double n = relMotion.GetN();
                timeToRdvs = rendezvousParam * Mathd.Deg2Rad / n;
            }
            relMotion.ComputeRendezvous(timeToRdvs, false);
            List<Maneuver> maneuvers = relMotion.GetManeuvers();
            maneuvers[1].onExecuted = RdvsCompleteCallback;
            ge.AddManeuvers(maneuvers);
            burnDone = true;
        }

    }

  
    
}
