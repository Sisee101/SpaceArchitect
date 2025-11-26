using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for the EarthMoonReturn scene. Once a TLI trajectory has been specified in the TLI controller
/// it maps the resulting trajectory into a TLI manuever that can be applied to the ship. This removes the
/// assumption in previous TLI efforts that the ship be "on-rails". Users are free to explore how well the
/// patched-conic approach works in the true N-body case.
///
/// UI:
/// - Key X cause manuver planned in ComputeTLIController to be executed
///
/// Notes:
/// - the maneuver allows for an inclined moon orbit
/// - assumes ship parking orbit is circular
///
/// For the case of an on-rails ship a trigger is added to monitor for SOI entry/exit and move ship into Lunar
/// SOI. At this point the periapsis of the new orbit will be logged and the maneuver for lunar orbit
/// insertion can be calculated. 
///
/// For non-rails ship a trigger is added to detect closest approach to the moon. At this point the orbit
/// can be circularized and the dV/perilune logged. 
///
/// Works in conjunction with the ComputeTLIController. 
/// </summary>
public class PerformTLI : MonoBehaviour
{
    // Refeence to tliController. Will get refs to ship, earth & moon from there. 
    public ComputeTLIController tliController;

    private GravityEngine ge;

    void Start()
    {
        ge = GravityEngine.Instance();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) {
            DoTLI();
            tliController.StopPlot();
        }
    }

    private OrbitPropagator shipProp;
    private OrbitPropagator moonProp;
    private double tliAngleDeg;
    Vector3d shipOrbitAxis;
    Vector3d lineToMoon;

    private void DoTLI()
    {
        // Uggo
        OrbitUniversal moonOrbit = tliController.moon.gameObject.GetComponent<OrbitUniversal>();
        Debug.LogFormat("moon period = {0}", moonOrbit.GetPeriod());

        NBody ship = tliController.ship;
        double moonMu = ge.GetMass(tliController.moon);
        NBody earth = tliController.earth;
        double earthMu = ge.GetMass(tliController.earth);
        NBody moon = tliController.moon;
        lineToMoon = ge.GetPositionDoubleV3(moon) -
                                ge.GetPositionDoubleV3(tliController.earth);
        Vector3d rMoon = ge.GetPositionDoubleV3(moon) - ge.GetPositionDoubleV3(earth);

        // Geometry of the burn.
        // Compute TLI gives us:
        // * gamma0 which is the angle CW from the current Earth moon line
        // * phi0 the flight path angle (angle of burn direction outward wrt to circular velocity)
        // Assume moon motion over the time ship takes to get to burn point is negligible
        (double phi0Deg, double gamma0Deg) = tliController.GetTLIAnglesPhi0Gamma0();
        tliAngleDeg = gamma0Deg;
        gamma0Deg = gamma0Deg - tliController.GetMoonLeadAngle() * GEMath.RAD2DEG;

        Vector3d rShip = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(earth);
        Vector3d vShip = ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(earth);
        shipOrbitAxis = Vector3d.Cross(rShip, vShip).normalized;

        // Greedy, but guard against orbit param tweaks and slightly non-circular orbit. Do a root find
        // to prop to the required angle.
        shipProp = new OrbitPropagator(rShip, vShip, 0.0, earthMu);
 
        Vector3d vMoon = ge.GetVelocityDoubleV3(moon);
        moonProp = new OrbitPropagator(rMoon, vMoon, 0.0, earthMu);
        double timeToTLI = SecantRootFind.Secant(DeltaAngleToTLIPoint, 0.1, 0.2);
        Debug.LogFormat("Time={0} to reach gamma0={1} moonLead={2} lineToMoon={3}",
            timeToTLI, gamma0Deg, tliController.GetMoonLeadAngle() * GEMath.RAD2DEG, lineToMoon);


        // get V so we have a reference for phi0 and use r x v to get axis
        (Vector3d r, Vector3d v) = shipProp.PropagateToTime(timeToTLI);
        (Vector3d r0, Vector3d v0) = tliController.GetR0V0();
        Quaternion rotWrtMoon = Quaternion.FromToRotation(r0.ToVector3(), r.ToVector3());
        Vector3 v0rot = rotWrtMoon * v0.ToVector3();

        // Create maneuver for TLI
        // To get precision on SOI entry, callback will create the SOI entry manuver
        Maneuver tliManeuver = new Maneuver();
        tliManeuver.label = "TLI Burn";
        tliManeuver.mtype = Maneuver.Mtype.setv;
        tliManeuver.velChange = v0rot;
        // tliManeuver.velChange = tliV;
        tliManeuver.worldTime = ge.GetGETime() + timeToTLI;
        tliManeuver.nbody = ship;
        OrbitUniversal shipOrbit = ship.GetComponent<OrbitUniversal>();
        if (shipOrbit.evolveMode == OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            tliManeuver.onExecuted = PrepareSOIManeuver;
        }
        ge.AddManeuver(tliManeuver);


        ge.SetEvolve(true);
    }

    private void PrepareSOIManeuver(Maneuver m)
    {   // what angle do we have between ship and moon ?
        //Vector3d rShip = ge.GetPositionDoubleV3(m.nbody);
        //Vector3d rMoon = ge.GetPositionDoubleV3(tliController.moon);
        //Debug.LogFormat("Angle from ship to moon at TLI={0} gamma0", Vector3d.Angle(rShip, rMoon));

        // Do explicit calc of time to SOI using prop. (Computed time accuracy is not awesome)
        double timeToSOI = ComputeTimetoSOI(tliController.ship, tliController.moon,
            tliController.GetSoiRadius(), ge.GetMass(tliController.earth));
        //Debug.LogFormat("Prop to SOI={0} vs computed={1} delta={2} for SOI={3}",
        //    timeToSOI,
        //    tliController.GetComputeTLI().GetTimeToSOI(),
        //    (timeToSOI - tliController.GetComputeTLI().GetTimeToSOI()),
        //    tliController.GetComputeTLI().GetSoiRadius() );
        Maneuver soiEntryManeuver = new Maneuver();
        soiEntryManeuver.label = "SOI entry";
        soiEntryManeuver.mtype = Maneuver.Mtype.vector;
        soiEntryManeuver.velChange = Vector3.zero;
        soiEntryManeuver.worldTime = ge.GetGETime() + timeToSOI;
        soiEntryManeuver.nbody = m.nbody;
        soiEntryManeuver.onExecuted = SoiEntryCallback;
        soiEntryManeuver.opaqueData = tliController.moon;
        ge.AddManeuver(soiEntryManeuver);

        // Add a maneuver at SOI exit

        Maneuver soiExitManeuver = new Maneuver();
        soiExitManeuver.label = "SOI entry";
        soiExitManeuver.mtype = Maneuver.Mtype.vector;
        soiExitManeuver.velChange = Vector3.zero;
        soiExitManeuver.worldTime = ge.GetGETime() + tliController.GetComputeTLI().GetTimeToSOI()
            + tliController.GetTSoiExit();
        soiExitManeuver.nbody = m.nbody;
        soiExitManeuver.onExecuted = SoiExitCallback;
        soiExitManeuver.opaqueData = tliController.earth;
        ge.AddManeuver(soiExitManeuver);

    }

    /// <summary>
    /// Callback to switch to moon center in on-rails mode
    /// </summary>
    /// <param name="m"></param>
    private void SoiEntryCallback(Maneuver m)
    {
        OrbitUniversal shipOrbit = m.nbody.GetComponent<OrbitUniversal>();
        shipOrbit.SetNewCenter((NBody)m.opaqueData);
        OrbitPredictor op = m.nbody.GetComponentInChildren<OrbitPredictor>();
        op.SetCenterObject(((NBody)m.opaqueData).gameObject);
        // DEBUG
        Vector3d shipPos = ge.GetPositionDoubleV3(m.nbody);
        Vector3d moonPos = ge.GetPositionDoubleV3((NBody)m.opaqueData);
        Vector3d shipVel = ge.GetVelocityDoubleV3(m.nbody);
        Vector3d moonVel = ge.GetVelocityDoubleV3((NBody)m.opaqueData);
        Debug.LogFormat("soiDistance MANEUVER at SOI entry={0} perigee={1} vAngle={2} rAngle={3}",
            (shipPos - moonPos).magnitude,
            shipOrbit.GetPerigee(),
            Vector3d.Angle(shipVel-moonVel, moonPos),
            Vector3d.Angle((shipPos-moonPos), moonPos));
    }

    private void SoiExitCallback(Maneuver m)
    {
        // DEBUG
        Vector3d shipVel = ge.GetVelocityDoubleV3(m.nbody);
        Vector3d shipPos = ge.GetPositionDoubleV3(m.nbody);

        OrbitUniversal shipOrbit = m.nbody.GetComponent<OrbitUniversal>();
        Vector3d moonPos = ge.GetPositionDoubleV3(shipOrbit.centerNbody); // assume Earth at 0
        Vector3d moonVel = ge.GetVelocityDoubleV3(shipOrbit.centerNbody);
        // LOG relative R and V vs moonAngle
        Debug.LogFormat("soiDistance MANEUVER at SOI exit={0} vExit={1} VtoMoon={2} RtoMoon={3}",
            (shipPos-moonPos).magnitude,
            shipVel.magnitude,
            Vector3d.Angle(shipVel-moonVel, moonPos),
            Vector3d.Angle(shipPos-moonPos, moonPos)
            );
        shipOrbit.SetNewCenter((NBody)m.opaqueData);
        OrbitPredictor op = m.nbody.GetComponentInChildren<OrbitPredictor>();
        op.SetCenterObject(((NBody)m.opaqueData).gameObject);
    }

    private double DeltaAngleToTLIPoint(double time)
    {
        (Vector3d r, Vector3d v) = shipProp.PropagateToTime(time);
        (Vector3d rMoon, Vector3d vMoon) = moonProp.PropagateToTime(time);
        double angleDeg = NUtils.AngleFullCircleRadians(r, rMoon, shipOrbitAxis) * GEMath.RAD2DEG;
        Debug.LogFormat("root find> t={0} angle={1} (deg.) target={2} axis={3}", time, angleDeg, tliAngleDeg, shipOrbitAxis);
        return angleDeg - tliAngleDeg;
    }

    // Get exact time to SOI via OrbitProp
    private OrbitPropagator soiShipProp;
    private OrbitPropagator soiMoonProp;
    private double soiDistance;

    // UGH. The moon evolution is based on a mu of m1+m2 in the orbit universal, but all the patched
    // conic calculations use a fixed omega for the moon based on the earth mass.

    private double ComputeTimetoSOI(NBody ship, NBody moon, double d, double earthMu)
    {
        // use same r0, v0 as orbit prop
        OrbitUniversal shipOrbit = ship.GetComponent<OrbitUniversal>();
        Vector3d rShip = Vector3d.zero;
        Vector3d vShip = Vector3d.zero;
        double time = 0.0; 
        shipOrbit.GetRVT(ref rShip, ref vShip, ref time);
        Debug.LogFormat("Actual R0={0} |R0|={1} V0={2} |V0|={3} time={4}",
            rShip, rShip.magnitude, vShip, vShip.magnitude, time);
        Vector3d rMoon = ge.GetPositionDoubleV3(moon);
        Vector3d vMoon = ge.GetVelocityDoubleV3(moon);
        
        soiDistance = d;
        soiShipProp = new OrbitPropagator(rShip, vShip, 0.0, earthMu);
        soiMoonProp = new OrbitPropagator(rMoon, vMoon, 0.0, earthMu);
        double timeSoi = SecantRootFind.Secant(AtSoi, 0.1, 0.2);
        return timeSoi;
    }

    private double AtSoi(double time)
    {
        (Vector3d r, Vector3d v) = soiShipProp.PropagateToTime(time);
        (Vector3d r2, Vector3d v2) = soiMoonProp.PropagateToTime(time);
        return (r2 - r).magnitude - soiDistance;
    }



}
