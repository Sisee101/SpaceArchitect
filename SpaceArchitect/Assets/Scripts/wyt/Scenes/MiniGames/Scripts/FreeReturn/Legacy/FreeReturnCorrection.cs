using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller to compute a correction burn in the free return mini-game.
///
/// Press M to institute the correction burn.
/// Press P to write plot to a file for fromTime to toTime. 
/// 
/// </summary>
public class FreeReturnCorrection : MonoBehaviour
{
    [Header("Press M for correction")]
    public ComputeTLIController tliController;
    private NBody ship;
    private NBody moon;


    private OrbitPredictor shipOP; 

    private double targetPerilune = 5.0; // GE units

    private GravityEngine ge; 
    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        ship = tliController.ship;
        moon = tliController.moon;
        shipOP = ship.GetComponentInChildren<OrbitPredictor>();
        targetPerilune = tliController.targetPeriluneKm * ge.lengthScale;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) {
            // trigger mode in which we determine a correction to attain the target perilune
            if (shipOP.centerBody == moon) {
                Debug.LogWarning("Not implemented inside SOI");
            } else {
                NBody earth = shipOP.centerBody.GetComponent<NBody>();
                earthMu = ge.GetMass(earth);
                moonMu = ge.GetMass(moon);
                soiDistance = OrbitUtils.SoiRadius(earth, moon);
                Debug.LogFormat("Soi distance: {0}", soiDistance);

                rShip = ge.GetPositionDoubleV3(ship);
                vShip = ge.GetVelocityDoubleV3(ship);
                rMoon = ge.GetPositionDoubleV3(moon);
                vMoon = ge.GetVelocityDoubleV3(moon);
                double adjust = SecantRootFind.Secant(ComputePerilune, 1.0, 1.01, tol:1E-4);
                Debug.LogFormat("On axis adjust = {0} or delta={1}", adjust, rShip * (1.0-adjust));
                // just set the required velocity (Instead of doing a maneuver)
                ge.SetVelocityDoubleV3(ship, adjust * vShip);

            }
        } 
        
    }


    private Vector3d rShip;
    private Vector3d vShip;
    private Vector3d rMoon;
    private Vector3d vMoon;
    private double earthMu;
    private double moonMu;
     
    private OrbitPropagator shipProp;
    private OrbitPropagator moonProp;
    private double soiDistance;

    private double ComputePerilune(double adjust)
    {
        // create an orbit propagator for the ship and the moon
        shipProp = new OrbitPropagator(rShip, vShip * adjust, 0.0, earthMu);
        moonProp = new OrbitPropagator(rMoon, vMoon, 0.0, earthMu);

        // Need to forward to point where ship enters SOI (use 100 sec. as arbitrary start point)
        double timeSOI = SecantRootFind.Secant(ShipMoonSeparation, 100, 200, tol: 1E-3);
        //double timeSOI = SecantRootFind.Secant(ShipMoonSeparation, 1.0, 2.0, tol: 1E-3);
        Debug.LogFormat("Found SOI entry at {0}", timeSOI + ge.GetGETime());

        // Jump ship into Moon SOI find perilune
        (Vector3d rShipSoi, Vector3d vShipSoi) = shipProp.PropagateToTime(timeSOI);
        (Vector3d rMoonSoi, Vector3d vMoonSoi) = moonProp.PropagateToTime(timeSOI);
        rShipSoi = rShipSoi - rMoonSoi;
        vShipSoi = vShipSoi - vMoonSoi;
        // determine the perilune for this orbit
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(rShipSoi, vShipSoi, moon, moonMu, relativePos: true);
        Debug.LogFormat("{0}: Predicted perilune {1}", adjust, oe.GetPeriapsis() );
        return oe.GetPeriapsis() - targetPerilune;
    }

    /// <summary>
    /// Function for the secant root find to determine the time at which orbit props to enter to moon SOI
    ///
    /// Makes use of class vars for the orbit prop and SOI distance.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private double ShipMoonSeparation(double time)
    {
        (Vector3d rShip, Vector3d vShip) = shipProp.PropagateToTime(time);
        (Vector3d rMoon, Vector3d vMoon) = moonProp.PropagateToTime(time);
        Debug.LogFormat("At t={0} r={1} rShip={2} rMoon={3}", time, (rShip - rMoon).magnitude - soiDistance, rShip.magnitude, rMoon.magnitude);
        return (rShip - rMoon).magnitude - soiDistance;
    }

}
