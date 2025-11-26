using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apollo style rendezvous using trigger angle targeting. 
/// 
/// Input: desired target rendezvous angle (angle through which target moves between burn and rdvs)
/// 
/// This uses equations for relative orbital motion. It is suitable for cases where the separation between the
/// ship and target is small compared to the radius of the orbits. It also assumes the target is in a circular
/// orbit. 
/// 
/// Algorithm:
/// 1) Use the Woffinden paper to determine the trigger angle for the specified target rendezvous angle
/// (this is the angle that aligns the burn with the LOS to the target at the time of the burn)
/// 
/// 2) Create a GE trigger that activates when required target angle is reached. 
/// 
/// 3) [AngleTriggered] Use the Relative Motion (Clohessy-Wiltshire) equations to determine the required maneuver in the
///    direction of line of sight. This is based on a linearized, Taylor expanded rendezvous calculation and will not be very accurate
///    for the Lunar orbit case. 
///    Add the first correction burn time. 
///   
/// 4) CorrectionBurn1: Recompute the rendezvous using RelativeMotion and perform a maneuver to adjust. 
///    Schedule a second correction burn
///    
/// 5) CorrectionBurn2: Recompute the rendezvous. Either configure braking burn triggers or add the final match orbit
///    maneuver based on the useBraking flag. 
/// 
/// </summary>
public class TPIBurn : MonoBehaviour
{

    [SerializeField]
    private NBody ship = null;

    [SerializeField]
    private NBody target = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    [Tooltip("Angle (degrees) after trigger angle maneuver when rendezvous will take place.")]
    private double transferAngle = 130.0;

    [SerializeField]
    [Tooltip("Use the hard coded braking burns/distance in the terminal phase. Otherwise maneuvers to a stop at destination.")]
    private bool useBraking = true;

    [SerializeField]
	[Tooltip("(optional) Sextant to show trigger angle up to first burn")]
    private HorizonSextant sextant = null;

    private OrbitUniversal shipOrbit;

    private OrbitUniversal targetOrbit;

    private GravityEngine ge;

    private double targetAngle = 0;

    private float correctionInterval;
    private float rdvsTime; 
    
    RelativeMotion relMotion;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        shipOrbit = ship.GetComponent<OrbitUniversal>();
        targetOrbit = target.GetComponent<OrbitUniversal>();
        ge.AddGEStartCallback(GECallback);
    }

    private void GECallback() {
        double omega = targetOrbit.GetAngularVelocity();
        double z0 = targetOrbit.GetApogee() - shipOrbit.GetApogee();
        Vector3d targetPos = ge.GetPositionDoubleV3(target) - ge.GetPositionDoubleV3(planet);
        Vector3d targetVel = ge.GetVelocityDoubleV3(target) - ge.GetVelocityDoubleV3(planet);
        Vector3d Omega = Vector3d.Cross(targetPos, targetVel) / (targetPos.magnitude * targetPos.magnitude);
        Vector3d r = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target);
        targetAngle = TriggerAngleTargeting.ComputeAngleRadians(z0, transferAngle * Mathd.Deg2Rad, 0) * Mathd.Rad2Deg; 
        Debug.LogFormat("angle={0} omega={1} z0={2}", targetAngle, omega, z0);
        relMotion = new RelativeMotion(ship, target, planet);

		if (sextant != null) {
            sextant.SetAngle((float) targetAngle);
		}

        double n = relMotion.GetN();
        transferTime = transferAngle * Mathd.Deg2Rad / n;
        bool absoluteCorrectionTime = true;
        if (absoluteCorrectionTime) {
            correctionInterval = (float)GravityScaler.WorldSecsToPhysTime(15 * 60);
        } else {
            correctionInterval = 0.25f * (float)transferTime;
        }

        // set angle trigger. 
        GETriggerMgr.Trigger t = new GETriggerMgr.Trigger(AngleTrigger, this);
        ge.AddTrigger(t);
    }

    private void RdvsCompleteCallback(Maneuver m) {
        state = State.COMPLETE;
        Debug.LogFormat("Arrival: deltaR={0} |deltaR|={1} deltaV={2}",
             ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target),
            (ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target)).magnitude,
            (ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(target)).magnitude);
    }

    private void DummyCallback(Maneuver m)
    {
    }

    private double transferTime = 0;

    /// <summary>
    /// Terminal phase RDVS states: 
    /// WAIT_FOR_TRIGGER: Wait until the required LOS trigger angle to CSM is acheived
    ///           COAST1: Coast until first course correct at +15 min
    ///                   (once nextEventTime is reached, do first correction)
    ///           COAST2: Coast until second course correction at +30 min
    ///           COAST3: Coast to final maneuver (no braking)
    ///          BRAKING: Series of distance triggered braking manuevers
    ///         COMPLETE: RDVS complete
    /// </summary>
    private enum State { WAIT_FOR_TRIGGER, TPI_BURN_DONE, CORRECTION1_DONE, CORRECTION2_DONE, BRAKING, COMPLETE};
    private State state = State.WAIT_FOR_TRIGGER;
    private int brakingIndex;
    private double brakingDv; 

    /// <summary>
    /// Trigger to check line of sight angle from ship to target. 
    /// 
    /// Upon detection of the trigger angle, create the transfer burn along the LOS using 
    /// RelativeMotion with alignment to the angle imposed. Skip the relative motion arrival burn
    /// in favour of a pair of correction burns. 
    /// 
    /// This code runs from the GravityState inner evolve loop. 
    /// </summary>
    /// <param name="gs"></param>
    /// <returns></returns>
    private static bool AngleTrigger(GravityState gs, System.Object triggerData)
    {
        TPIBurn tpiBurn = (TPIBurn)triggerData;
        if (Mathd.Abs(tpiBurn.targetAngle - tpiBurn.relMotion.TargetAngleDegrees()) < 1E-2) {
            // relative or absolute correction times?
            tpiBurn.relMotion.ComputeRendezvous(tpiBurn.transferTime, true);
            List<Maneuver> maneuvers = tpiBurn.relMotion.GetManeuvers();
            // only add TPI burn, not arrival burn
            tpiBurn.ge.AddManeuver(maneuvers[0]);
            tpiBurn.state = State.TPI_BURN_DONE;
            Debug.LogFormat("TPI burn: {0} dV={1} ({2} m/s) ", maneuvers[0].LogString(),
                maneuvers[0].velChange,
                GravityScaler.VelocityScaletoSIUnits() * GravityScaler.ScaleVelPhysToScene(maneuvers[0].velChange).magnitude);
            // register a correction maneuver to happen next
            tpiBurn.rdvsTime = (float) (tpiBurn.ge.GetPhysicalTime() + tpiBurn.transferTime);
            tpiBurn.AddCorrectionBurn(tpiBurn);

			if (tpiBurn.sextant != null) {
                tpiBurn.sextant.gameObject.SetActive(false);
			}
            return true;
        }
        return false;
    }

    private class BrakingData
    {
        public TPIBurn tpiBurn; 
        //! braking distance checkpoints in meters
        // (final entry is trigger to match target velocity, burn value is not used)
        public double[] brakingDistances;
        public double[] brakingBurn;
        public int brakingIndex; 
    }

    private static bool BrakeTrigger(GravityState gs, System.Object triggerData)
    {
        BrakingData bd = (BrakingData)triggerData;
        GravityEngine ge = bd.tpiBurn.ge;
        Vector3d shipPos = ge.GetPositionDoubleV3(bd.tpiBurn.ship);
        Vector3d targetPos = ge.GetPositionDoubleV3(bd.tpiBurn.target);
        Vector3d toTarget = (targetPos - shipPos);
        double ds = toTarget.magnitude;
        if (ds < bd.brakingDistances[bd.brakingIndex]) {
            Debug.LogFormat("Brake {0} triggered at {1}", bd.brakingIndex, ds);
            if (bd.brakingIndex == bd.brakingDistances.Length - 1) {
                // Stop burn - match the target velocity
                Vector3d targetVel = ge.GetVelocityDoubleV3(bd.tpiBurn.target);
                Vector3d shipVel = ge.GetVelocityDoubleV3(bd.tpiBurn.ship);
                Maneuver stopBurn = new Maneuver();
                stopBurn.nbody = bd.tpiBurn.ship;
                stopBurn.mtype = Maneuver.Mtype.vector;
                stopBurn.worldTime = ge.GetPhysicalTime();
                stopBurn.velChange = (targetVel - shipVel).ToVector3();
                stopBurn.dV = stopBurn.velChange.magnitude;
                stopBurn.onExecuted = bd.tpiBurn.RdvsCompleteCallback;
                stopBurn.relativeTo = bd.tpiBurn.planet;
                // force KS to skip by adding a dummy callback. This is a HACK to avoid relative info computation.
                stopBurn.beforeExecuted = bd.tpiBurn.DummyCallback;
                ge.AddManeuver(stopBurn);
                Debug.LogFormat("Stop burn: {0} dV={1} ({2} m/s) ", stopBurn.LogString(),
                    stopBurn.velChange,
                    GravityScaler.VelocityScaletoSIUnits() * GravityScaler.ScaleVelPhysToScene(stopBurn.velChange).magnitude);
            }
            else {
                // brake away from current target position.
                Maneuver brake = new Maneuver();
                brake.nbody = bd.tpiBurn.ship;
                brake.mtype = Maneuver.Mtype.vector; // opposite from direction to target
                brake.velChange = -1.0f * toTarget.ToVector3().normalized * (float)bd.brakingBurn[bd.brakingIndex];
                brake.dV = brake.velChange.magnitude;
                brake.worldTime = ge.GetPhysicalTime();  // now
                // force KS to skip by adding a dummy callback. This is a HACK to avoid relative info computation.
                brake.beforeExecuted = bd.tpiBurn.DummyCallback;
                brake.relativeTo = bd.tpiBurn.planet;
                ge.AddManeuver(brake);
                Debug.LogFormat("Brake {0}: dV={1} ({2} m/s) at ds={3}", bd.brakingIndex, brake.dV,
                    brake.dV / GravityScaler.GetVelocityScale() * 1000 / 3600, ds);
            }
            bd.brakingIndex += 1;
            if (bd.brakingIndex >= bd.brakingDistances.Length) {
                bd.tpiBurn.state = State.COMPLETE;
                return true;
            }
        }
        return false;

    }

    private static void AddBrakeTrigger(TPIBurn tpiBurn)
    {
        BrakingData bd = new BrakingData();
        // scale to m, then km then to Unity units
        double scale = GravityScaler.FT_TO_M * tpiBurn.ge.GetLengthScale() / 1000.0;
        // correspond to Apollo 6000 ft, 3000 ft, 1500 ft, 600 ft in meters
        bd.brakingDistances = new double[5] { 6000.0 * scale, 3000.0 * scale, 1500.0 * scale, 600.0 * scale, 100.0 * scale };

        // Braking burns are ABSOLUTE values based on Apollo 11 Terminal Phase Final values!
        // TODO: find a way to make this relative to final correction burn required
        // see: https://history.nasa.gov/afj/ap11fj/a11fp/a11-fp-1-009.jpg
        double fpsToGEVel = GravityScaler.FTSEC_TO_KMHR * GravityScaler.GetVelocityScale();
        bd.brakingBurn = new double[5] { 0, 12.0 * fpsToGEVel, 9.8 * fpsToGEVel, 4.8 * fpsToGEVel, 4.7 * fpsToGEVel };
        bd.tpiBurn = tpiBurn;
        bd.brakingIndex = 0; 
        GETriggerMgr.Trigger brakeTrigger = new GETriggerMgr.Trigger(BrakeTrigger, bd);
        tpiBurn.ge.AddTrigger(brakeTrigger);
        Debug.Log("Added brake trigger");
    }

    /// <summary>
    /// Callback for a correction burn maneuver. Use relative motion to generate a course correction. 
    /// Add it as an immediate maneuver to GE. 
    /// 
    /// This
    /// </summary>
    /// <param name="m"></param>
    private static void PerformCorrectionBurn(Maneuver m)
    {
        // compute the correction burn using relative motion
        TPIBurn tpiBurn = (TPIBurn)m.opaqueData;
        RelativeMotion relMotion = new RelativeMotion(tpiBurn.ship, tpiBurn.target, tpiBurn.planet);
        double timeToRdvs = tpiBurn.rdvsTime - tpiBurn.ge.GetPhysicalTime();
        relMotion.ComputeRendezvous(timeToRdvs, false);
        List<Maneuver> maneuvers = relMotion.GetManeuvers();
        // use the maneuver result directly
        Vector3 shipvel = tpiBurn.ge.GetVelocityDoubleV3(tpiBurn.ship).ToVector3() + maneuvers[0].velChange;
        tpiBurn.ge.SetVelocity(tpiBurn.ship, shipvel);
        
        if (tpiBurn.state == State.TPI_BURN_DONE) {
            // schedule 2nd correction burn
            tpiBurn.AddCorrectionBurn(tpiBurn);
            tpiBurn.state = State.CORRECTION1_DONE;
            Debug.LogFormat("Correction burn 1");
        } else if (tpiBurn.state == State.CORRECTION1_DONE) {
            // this is final correction. Either schedule braking or terminal phase burn at arrival
            if (tpiBurn.useBraking) {
                // setup the braking trigger
                AddBrakeTrigger(tpiBurn);
            } else {
                maneuvers[1].onExecuted = tpiBurn.RdvsCompleteCallback;
                tpiBurn.ge.AddManeuver(maneuvers[1]);
                Debug.LogFormat("Correction burn 2");
            }
            tpiBurn.state = State.CORRECTION2_DONE;
        } else {
            Debug.LogError("Unexpected state " + tpiBurn.state);
        }
    }

    /// <summary>
    /// Schedule a correction burn. Called as part of a trigger callback (or maneuver callback) so work on the
    /// tpiBurn passed to it. 
    /// 
    /// The burn details will be handled by the before executed callback of the maneuver, so the details are not
    /// known in advance. 
    /// </summary>
    /// <param name=""></param>
    private void AddCorrectionBurn(TPIBurn tpiBurn)
    {
        Maneuver m = new Maneuver();
        m.mtype = Maneuver.Mtype.none;
        m.nbody = tpiBurn.ship;
        m.beforeExecuted = PerformCorrectionBurn;
        m.opaqueData = tpiBurn;
        m.worldTime = tpiBurn.ge.GetPhysicalTime() + tpiBurn.correctionInterval;
        tpiBurn.ge.AddManeuver(m);
    }
    
}
