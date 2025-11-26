using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// TransferShip computes the transfer to a new orbit around the same central body. The script is attached to 
/// the NBody that is to perform the transfer. The transfer can be executed by the DoTransfer method. Alternatly the
/// computed transfer can be accessed and the maneuvers retrieved from there. 
/// 
/// The attached NBody object is assumed to have an OrbitPredictor child object. This is used to determine the
/// initial orbit for the transfer. 
/// 
/// The target orbit may be designated in several different ways, and this depends on the transfer type selected. 
/// 
///     targetNBody: an NBody that is in the desired final orbit. In the case of a rendezvous type maneuver
///                  the transfer will ensure a transfer that results in rendezvous. This may result in a
///                  delay before the transfer and an intermediate phasing orbit if the circumstances require it. 
///                  
///     targetPoint: the point the NBody should intercept when using the LAMBERT_POINT transfer
///     
///     targetPhase: specifies position in target orbit (only used when LAMBERT_ORBIT is selected)
///     
/// In the case of Hohmann transfers the transfer time is fixed by the orbit geometries. 
/// 
/// Lambert transfers require that the time of flight be specified by transferTimeFactor. 
/// In order to maintain scale independence this time is expressed as a multiplier of the minimum energy 
/// Lambert transfer (1.0 will result in the minimum energy transfer). 
/// 
/// Transfer Types:
///
/// CIRCULARIZE: Circularize the current orbit
/// HOHMANN: Perform and immediate Hohmann xfer. Source and destination orbits must be circular.
/// HOHMANN_RDVS: Perform a Hohmann rendezvou. The transfer will be phased (delayed) to ensure rendezvous.
///               If the orbits differ in orientation a phasing orbit may be required and a three maneuver
///               sequence will be created. Source and destination orbits must be circular.
/// LAMBERT_POINT: Transfer to the point specified by targetPoint.
/// LAMBERT_ORBIT: Transfer to the orbit specified by the target orbit, The point on the target orbit can
///                be specified by the phase of the target orbit object or by specifying a target NBody.
/// LAMBERT_INTERCEPT: perform a Lambert intercept of the target NBody (do not match target velocity on arrival)
/// LAMBERT_RDVS: perform a Lambert transfer to the target NBody
/// LAMBERT_MINDVSQ: perform a Lambert transfer (as per LAMBERT_ORBIT) using the minimize (dv)^2 algorithm. 
/// 
/// This class has a custom editor script. 
/// 
/// </summary>
public class TransferShip : MonoBehaviour
{

    public enum Transfer {  CIRCULARIZE,        // no target info needed
                            HOHMANN,            // targetNBody or targetOrbit (to/from must be circular)
                            HOHMANN_RDVS,       // targetNbody (to/from must be circular)
                            LAMBERT_POINT,      // require targetPoint to specify to  
                            LAMBERT_ORBIT,      // can use targetNBody or specify a targetOrbit with phase
                            LAMBERT_INTERCEPT,  // target NBody
                            LAMBERT_RDVS,       // target NBody
                            LAMBERT_MINDVSQ     // can use targetNBody or specify a targetOrbit with phase
    };

    [SerializeField]
    private Transfer transferType = Transfer.HOHMANN;

    [SerializeField]
    [Tooltip("Used when HOHMANN or LAMBERT_ORBIT selected to indicate target orbit. (Lambert also needs targetPhase)")]
    public OrbitUniversal targetOrbit = null;

    [SerializeField]
    [Tooltip("Phase in target orbit for the Lambert transfer")]
    private float targetPhase = 0.0f; 

    [Tooltip("Target for rendezvous when using HOHMANN_RDVS, LAMBERT_TARGET or LAMBERT_RDVS modes")]
    public NBody targetNbody = null; 

    [SerializeField]
    [Tooltip("Used when LAMBERT_POINT is selected to designate destination point")]
    private Vector3 targetPoint = Vector3.zero;

    private Vector3d targetPoint3d = Vector3d.zero;

    [SerializeField]
    [Tooltip("Used when LAMBERT is selected to designate time factor for transfer wrt time for min. energy xfer")]
    private float transferTimeFactor = 1.0f;

    // For Lambert xfers can retreive/set a specific time of flight 
    private double transferTime = double.NaN;
    private double t_flight;

    [SerializeField]
    [Tooltip("Used when LAMBERT is selected to indicate reverse path (invert to flip direction of transfer)")]
    private bool lambertReversePath = false;

    [Header("Planet Hit Detection")]
    public bool checkHit = false;
    public double planetRadius = 5.0;

    // The algorithm will select the transfer direction that aligns with the current motion of the
    // ship. This sometimes requires the Lambert transfer be reversed. This status boolean will indicate this. 
    // (Used by e.g. a controller to set a Manuver segement correctly. See LambertDemoController)
    private bool lambertIsReversed = false;

    private int error = 0;

    private NBody shipNbody = null;
    private OrbitUniversal shipOrbit = null; 

    private GravityEngine ge = null;

    private OrbitTransfer orbitTransfer;

    //! List of maneuvers. Only valid on DoTransfer() has been triggered. 
    private List<Maneuver> maneuvers;

    // Not Awake(): OrbitPredictor will not be setup until Awake phase has completed
    void Start() {
        Init();
    }

    public void Init() {
       
        shipNbody = GetComponent<NBody>();
        if (shipNbody == null) {
            Debug.LogError("Did not find attached NBody");
        }

        OrbitPredictor[] orbitPredictors = GetComponentsInChildren<OrbitPredictor>();
        if (orbitPredictors.Length == 0) {
            Debug.LogError("Script requires an attached OrbitPredictor component");
        } else {
            // May have additional predictors to show velocity of a maneuver (with setVelocity), ignore those
            foreach (OrbitPredictor p in orbitPredictors) {
                if (!p.velocityFromScript) {
                    shipOrbit = p.GetOrbitUniversal();
                    break;
                }
            }
        }
        ge = GravityEngine.Instance();

        targetPoint3d = new Vector3d(targetPoint);
    }

    public void SetTargetPoint(Vector3d point) {
        targetPoint3d = point;
    }

    public void SetTargetPoint(Vector3 point)
    {
        targetPoint = point;
        targetPoint3d = new Vector3d(targetPoint);
    }

    public Vector3 GetTargetPoint()
    {
        return targetPoint3d.ToVector3();
    }

    public void SetTargetPhase(float phase) {
        targetPhase = phase;
    }

    public float GetTargetPhase()
    {
        return targetPhase;
    }

    public float GetTransferTimeFactor()
    {
        return transferTimeFactor;
    }

    public void SetTransferTimeFactor(float time) {
         transferTimeFactor = time;
         transferTime = double.NaN;
    }

    public void SetTransferType(Transfer transferType) {
        this.transferType = transferType;
    }

    public void SetTargetOrbit(OrbitUniversal ou)
    {
        targetOrbit = ou;
    }
	
	public bool GetLambertReversePath()
	{
        return lambertReversePath;
	}

	public void SetLambertReversePath(bool path)
	{
        lambertReversePath = path;
	}

    public bool LambertIsReversed()
    {
        return lambertIsReversed;
    }

    public int GetError()
    {
        return error;
    }

    public double GetTransferTime()
    {

        return transferTime;
    }

    public void SetTransferTime(double t)
    {
        switch (transferType) {
            case Transfer.HOHMANN_RDVS:
            case Transfer.HOHMANN:
            case Transfer.CIRCULARIZE:
                Debug.LogWarning("transfer type does not use SetTransferTime");
                break;
            default:
                break;
        }
        transferTime = t;
    }

    public double GetTimeOfFlight()
    {
        return t_flight;
    }

    public bool IsRendezvousType()
    {
        bool rdvs = false;
        switch(transferType) {
            case Transfer.HOHMANN_RDVS:
            case Transfer.LAMBERT_RDVS:
            case Transfer.LAMBERT_INTERCEPT:
                rdvs = true;
                break;
            default:
                break;
        }
        return rdvs;
    }

    private OrbitData GetTargetOrbitData() {
        OrbitData orbitData = null; 
        switch(transferType) {
            case Transfer.HOHMANN_RDVS:
            case Transfer.LAMBERT_RDVS:
            case Transfer.LAMBERT_INTERCEPT:
                if (targetNbody == null) {
                    Debug.LogError("No target body to rendezvous with");
                    return null;
                }
                orbitData = new OrbitData();
                orbitData.SetOrbit(targetNbody, shipOrbit.centerNbody);
                break;
            default:
                if (targetOrbit == null) {
                    if (targetNbody != null) {
                        orbitData = new OrbitData();
                        orbitData.SetOrbit(targetNbody, shipOrbit.centerNbody);
                    } else {
                        Debug.LogError("No target orbit or target Nbody provided as destination");
                        return null;
                    }
                } else {
                    orbitData = new OrbitData(targetOrbit);
                }
                break;
        }
        return orbitData;
    }

    public OrbitUniversal GetTargetOrbit() {
        return targetOrbit;
    }

	/// <summary>
	/// Utility to get the velocity after transfer has started at the starting point.
	/// (Useful for controllers that want to plot the maneuver before it is initiated).
	///
	/// Assumes compute transfer has been run already. 
	/// </summary>
	/// <returns></returns>
	public Vector3 GetTransferVelocity()
	{
        List<Maneuver> maneuvers = orbitTransfer.GetManeuvers();
		if (maneuvers.Count == 0) {
            Debug.LogWarning("No maneuvers. Either failed to find transfer or did not call ComputeTransfer()");
            return Vector3.zero;
		}
        Maneuver m = maneuvers[0];
		switch (m.mtype) {
            case Maneuver.Mtype.vector:
                return m.velChange + ge.GetVelocity(shipNbody);
            case Maneuver.Mtype.setv:
				return m.velChange;
            default:
                Debug.LogError("Unsupported type");
                break;
		}
        return Vector3.zero;
	}

    private void ComputeCircularize() {
        OrbitData shipOrbitData = new OrbitData(shipOrbit);
        orbitTransfer = new CircularizeXfer(shipOrbitData);
    }

    /// <summary>
    /// A Hohmann tranfer requires a circular source and destination orbit. 
    /// (Eventually could support co-axial ellipses)
    /// 
    /// </summary>
    /// <param name="rendezvous"></param>
    private void ComputeHohmann(bool rendezvous) {
        OrbitData targetOrbitData = GetTargetOrbitData();
        OrbitData shipOrbitData = new OrbitData(shipOrbit);
        orbitTransfer = new HohmannGeneral(shipOrbitData, targetOrbitData, rendezvous);
        // Hohmann may not compute if orbits not circular (it will issue an error)
        if (orbitTransfer.GetManeuvers().Count == 0)
            return;
    }

    private void ComputeLambert() {
        OrbitData shipOrbitData = new OrbitData(shipOrbit);
        shipOrbitData.SetOrbitForVelocity(shipNbody, shipOrbit.centerNbody);
        // Need both, since MINDVSQ is only imlpemented inside LambertU. LambertU has convergence issues for short xfer times and 
        // issues with 180 degree xfers, so prefer LambertBattin.
        LambertUniversal lambertU = null;
        LambertBattin lambertB = null;
        bool shortPath = true;
        // Need lambertU for a Tmin value
        switch(transferType) {
            case Transfer.LAMBERT_POINT:
                Vector3d r_from = GravityEngine.Instance().GetPositionDoubleV3(shipNbody);
                Vector3d r_to = targetPoint3d;
                // compute the min energy path (this will be in the short path direction)
                lambertU = new LambertUniversal(shipOrbitData, r_from, r_to, shortPath);
                lambertB = new LambertBattin(shipNbody, shipOrbitData.centralMass, r_from, r_to, shipOrbitData.GetAxis());
                break;

            case Transfer.LAMBERT_MINDVSQ:
            case Transfer.LAMBERT_ORBIT:
                // maneuvers will include change to target orbit at the target point
                OrbitData targetOrbitData = GetTargetOrbitData();
                // sneaky: need to remove NBody from OrbitData so it uses phase and not the NBody
                targetOrbitData.nbody = null;
                targetOrbitData.phase = targetPhase;
                lambertU = new LambertUniversal(shipOrbitData, targetOrbitData, shortPath);
                lambertB = new LambertBattin(shipOrbitData, targetOrbitData);
                break;

            case Transfer.LAMBERT_RDVS:
            case Transfer.LAMBERT_INTERCEPT:
                // maneuvers will include change to target orbit at the target point
                OrbitData targetOrbitData2 = GetTargetOrbitData();
                lambertU = new LambertUniversal(shipOrbitData, targetOrbitData2, shortPath);
                lambertB = new LambertBattin(shipOrbitData, targetOrbitData2);
                break;
        }
        if (checkHit) {
            lambertU.SetPlanetRadius(planetRadius);
            lambertB.SetPlanetRadius(planetRadius);
        }
        orbitTransfer = lambertB;

        // apply any time of flight change
         
        t_flight = double.NaN;

        if (!double.IsNaN(transferTime)) {
            t_flight = transferTime;
        } else {
            if (transferTimeFactor < 0) {
                t_flight = 1f;
                transferTimeFactor = 1f;
                Debug.LogWarning("Negative time factor, reset to 1.0");
            }
            t_flight = transferTimeFactor * lambertU.GetTMin(); 
        }

        if (transferType == Transfer.LAMBERT_MINDVSQ) {
            Vector3d targetPos = new Vector3d( targetOrbit.PositionForPhase(targetPhase));
            // Velocity for Phase gives relative vel.
            Vector3d targetVel = new Vector3d(targetOrbit.VelocityForPhaseRelative(targetPhase)) + ge.GetVelocityDoubleV3(targetOrbit.centerNbody);
            lambertU.ComputeMinDvSquared(ge.GetPositionDoubleV3(shipNbody), ge.GetVelocityDoubleV3(shipNbody),
                targetPos, targetVel);
            orbitTransfer = lambertU;
        } else {
            lambertIsReversed = false;
            if (ComputeLambertForTflight(lambertB, t_flight, reverse: false))
            {
                // only accept a transfer if in same direction ship is currently orbiting
                Vector3 shipVel = ge.GetVelocity(shipNbody);
                // Lambert maneuver is additive velocity
                Vector3 newVel = lambertB.GetManeuvers()[0].velChange;
                Vector3 posRel = ge.GetPhysicsPosition(shipNbody) - ge.GetPhysicsPosition(shipOrbit.centerNbody);
                Vector3 axisShip = Vector3.Cross(posRel, shipVel).normalized;
                Vector3 axisXfer = Vector3.Cross(posRel, newVel).normalized;
                // normally want the shipVel and new velocity to have same axis
                // unless we have been told to take the reverse path explicitly
                if ((lambertReversePath && (Vector3.Dot(axisShip, axisXfer) > 0)) ||
                    (!lambertReversePath && (Vector3.Dot(axisShip, axisXfer) < 0))) {
                    lambertIsReversed = true;
                    // should find a solution if other way around did, so skip the check
                    ComputeLambertForTflight(lambertB, t_flight, reverse: true);
                }
            }
        }
    }

    private bool ComputeLambertForTflight(LambertBattin lambertB, double t_flight, bool reverse) {
        const bool df = false;
        const int nrev = 0;
        if ((transferType == Transfer.LAMBERT_INTERCEPT) || (transferType == Transfer.LAMBERT_RDVS)) {
            bool rdvs = (transferType == Transfer.LAMBERT_RDVS);
            error = lambertB.ComputeXferWithPhasing(reverse, df, nrev, t_flight, rdvs);
        } else {
            error = lambertB.ComputeXfer(reverse, df, nrev, t_flight);
        }
        if (error != 0) {
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG) {
                Debug.LogWarning("Lambert failed to find solution. error=" + error + " time=" + t_flight + " " + lambertB.Log());
            }
#pragma warning restore 162       
            return false;
        }
        return true;
    }


    public void ShipMoved() {
        ComputeTransfer();
    }

    public OrbitTransfer GetTransfer() {
        return orbitTransfer;
    }

    public Transfer GetTransferType() {
        return transferType;
    }

    public List<Maneuver> GetManeuvers() {
        if (orbitTransfer == null)
            ComputeTransfer(); 
        return orbitTransfer.GetManeuvers(); 
    }

    public float GetDV()
    {
        maneuvers = orbitTransfer.GetManeuvers();
        float dV = 0; 
        foreach (Maneuver m in maneuvers) {
            dV += Mathf.Abs(m.dV);
        }
        return dV;
    }

    /// <summary>
    /// Compute the transfer and add the maneuvers generated to GE.
    /// 
    /// Add a callback to the final maneuver in the sequence. 
    /// </summary>
    /// <param name="maneuverCallback"></param>
    /// <returns></returns>
    public bool DoTransfer(Maneuver.OnExecuted maneuverCallback) {
        return DoTransferCommonCallback(null, maneuverCallback);
    }

    /// <summary>
    /// Compute the transfer and add to GE. 
    /// 
    /// All maneuvers up to last get the common callback. The final maneuver gets lastCallback.
    /// </summary>
    /// <param name="commonCallback"></param>
    /// <param name="lastCallback"></param>
    /// <returns></returns>
    public bool DoTransferCommonCallback(Maneuver.OnExecuted commonCallback, 
                                         Maneuver.OnExecuted lastCallback, 
                                         Maneuver.OnExecuted beforeCallback = null)
    {
        ComputeTransfer();
        maneuvers = orbitTransfer.GetManeuvers();
        if (maneuvers.Count == 0) {
            Debug.LogError("No manuevers - cannot do transfer");
            return false;
        }
        foreach(Maneuver m in maneuvers) {
            m.onExecuted = commonCallback;
            m.beforeExecuted = beforeCallback;
        }
        maneuvers[maneuvers.Count - 1].onExecuted = lastCallback;
        ge.AddManeuvers(orbitTransfer.GetManeuvers());
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            float dVTotal = 0;
            foreach (Maneuver m in maneuvers) {
                dVTotal += m.dV;
                Debug.Log("Transfer added: " + m.LogString());
            }
        }
#pragma warning restore 162        // enable unreachable code warning
        return true;
    }

    public void ComputeTransfer() {
        
        switch(transferType) {
            case Transfer.CIRCULARIZE:
                ComputeCircularize();
                break;
            case Transfer.HOHMANN:
                ComputeHohmann(false);
                break;

            case Transfer.HOHMANN_RDVS:
                ComputeHohmann(true);
                break;

            case Transfer.LAMBERT_POINT:
            case Transfer.LAMBERT_ORBIT:
            case Transfer.LAMBERT_INTERCEPT:
            case Transfer.LAMBERT_RDVS:
            case Transfer.LAMBERT_MINDVSQ:
                ComputeLambert();
                break;

            default:
                Debug.LogError("Unsupported type: " + transferType);
                break;
       
        }
    }


}
