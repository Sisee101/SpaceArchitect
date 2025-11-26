using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for the EarthMoonReturn scene. Once a TEI trajectory has been specified in the TEI controller
/// it maps the resulting trajectory into a TEI manuever that can be applied to the ship. This removes the
/// assumption in previous TEI efforts that the ship be "on-rails". Users are free to explore how well the
/// patched-conic approach works in the true N-body case.
///
/// UI:
/// - Key X cause manuver planned in ComputeTEIController to be executed
///
/// Notes:
/// - the maneuver allows for an inclined moon orbit
/// - assumes ship parking orbit is circular
///
/// For the case of an on-rails ship a trigger is added to monitor for SOI entry/exit and move ship out of lunar
/// SOI. At this point the periapsis of the new orbit around earth will be logged.
///
/// For non-rails ship a trigger is added to detect closest approach to the moon. At this point the orbit
/// can be circularized and the dV/perilune logged. 
///
/// Works in conjunction with the ComputeTEIController. 
/// </summary>
public class PerformTEI : MonoBehaviour
{
    // Reference to TEIController. Will get refs to ship, earth & moon from there. 
    public ComputeTEIController teiController;

    private GravityEngine ge;

    void Start()
    {
        ge = GravityEngine.Instance();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) {
            DoTEI();
            teiController.StopPlot();
        }
    }

    private OrbitPropagator shipProp;
    private double teiAngle;
    Vector3d shipOrbitAxis;
    Vector3d lineToMoon;

    private void DoTEI()
    {
        NBody ship = teiController.ship;
        double moonMu = ge.GetMass(teiController.moon);
        NBody moon = teiController.moon;
        lineToMoon = (ge.GetPositionDoubleV3(moon) -
                                ge.GetPositionDoubleV3(teiController.earth)).normalized;

        // Geometry of the burn.
        // Compute TEI gives us:
        // * theta is the angle CW from the Earth-Moon line at time of SOI exit
        // * phi0 the flight path angle (angle of burn direction outward wrt to circular velocity)
        // Assume moon motion over the time ship takes to get to burn point is negligible
        (double phi0Deg, double theta) = teiController.GetTEIAnglesPhi0Theta();
        // theta is measured CW from moonline, need to make into CCW number
        teiAngle = 2.0*Mathd.PI - theta - teiController.GetMoonShift();
        if (teiAngle < 0)
            teiAngle += 2.0 * Mathd.PI;

        Vector3d rShip = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(moon);
        Vector3d vShip = ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(moon);
        shipOrbitAxis = Vector3d.Cross(rShip, vShip).normalized;


        Vector3d rMoon = ge.GetPositionDoubleV3(moon);
        double earthMu = ge.GetMass(teiController.earth);

        // measuring angle CCW, ship is orbiting CW so it's axis is flipped
        double shipAngleNow = NUtils.AngleFullCircleRadians(rShip, lineToMoon, -shipOrbitAxis);
        Debug.LogFormat("rShip={0} ltoM={1}, axis={2}", rShip, lineToMoon, shipOrbitAxis);
         
        // as ship advances, moon angle rotates CCW at omega_moon
        double rM = rMoon.magnitude;
        double omega_moon = Mathd.Sqrt(earthMu / (rM*rM*rM));
        double rS = rShip.magnitude;
        double omega_ship = Mathd.Sqrt(moonMu / (rS * rS * rS));

        // want theta_ship + t * w_ship = (theta_moon - theta + t * w_moon)
        double angleToGo = teiAngle - shipAngleNow;
        if (angleToGo < 0)
            angleToGo += 2.0 * Mathd.PI;
        double timeToTEI = angleToGo/(omega_ship - omega_moon);
        // double shipOrbitPeriod = 2.0 * Mathd.PI / omega_ship;
        //Debug.LogFormat("timeToTEI={0} shipOrbitPeriod={1} shipAngleNow={2} targetAngle={3} angleToGo={4}",
        //    timeToTEI,
        //    shipOrbitPeriod,
        //    shipAngleNow * GEMath.RAD2DEG,
        //    teiAngleDeg * GEMath.RAD2DEG,
        //    angleToGo * GEMath.RAD2DEG);

        // use rotation from r0 to r to find corresponding rotation for v0
        shipProp = new OrbitPropagator(rShip, vShip, 0.0, moonMu);
        (Vector3d r, Vector3d v) = shipProp.PropagateToTime(timeToTEI);
        (Vector3d r0, Vector3d v0) = teiController.GetR0V0();
        Quaternion rotWrtMoon = Quaternion.FromToRotation(r0.ToVector3(), r.ToVector3());
        Vector3 v0rot = rotWrtMoon * v0.ToVector3();

        // Create maneuver for TEI
        // To get precision on SOI entry, callback will create the SOI entry manuver
        Maneuver teiManeuver = new Maneuver();
        teiManeuver.label = "TEI Burn";
        teiManeuver.mtype = Maneuver.Mtype.setv;
        teiManeuver.velChange = v0rot;
        teiManeuver.worldTime = ge.GetGETime() + timeToTEI;
        teiManeuver.nbody = ship;
        teiManeuver.relativeTo = moon;
        OrbitUniversal shipOrbit = teiController.ship.gameObject.GetComponent<OrbitUniversal>();
        if (shipOrbit.evolveMode == OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            teiManeuver.onExecuted = PrepareSOIManeuver;
        }
        ge.AddManeuver(teiManeuver);


        ge.SetEvolve(true);
    }

    private void PrepareSOIManeuver(Maneuver m)
    {   
        Maneuver soiEntryManeuver = new Maneuver();
        soiEntryManeuver.label = "SOI entry";
        soiEntryManeuver.mtype = Maneuver.Mtype.vector;
        soiEntryManeuver.velChange = Vector3.zero;
        soiEntryManeuver.worldTime = ge.GetGETime() + teiController.GetComputeTEI().GetTimeToSOI();
        soiEntryManeuver.nbody = m.nbody;
        soiEntryManeuver.onExecuted = SOIexitCallback;
        soiEntryManeuver.opaqueData = teiController.earth;
        ge.AddManeuver(soiEntryManeuver);
    }

    /// <summary>
    /// Callback to switch to moon center in on-rails mode
    /// </summary>
    /// <param name="m"></param>
    private void SOIexitCallback(Maneuver m)
    {
        OrbitUniversal shipOrbit = m.nbody.GetComponent<OrbitUniversal>();
        shipOrbit.SetNewCenter((NBody)m.opaqueData);
        OrbitPredictor op = m.nbody.GetComponentInChildren<OrbitPredictor>();
        op.SetCenterObject(((NBody)m.opaqueData).gameObject);
        // DEBUG
        Vector3d shipPos = ge.GetPositionDoubleV3(m.nbody);
        Vector3d moonPos = ge.GetPositionDoubleV3((NBody)m.opaqueData);
        Debug.LogFormat("soiDistance at SOI exit={0} perigee={1}", (shipPos - moonPos).magnitude, shipOrbit.GetPerigee());
    }

}
