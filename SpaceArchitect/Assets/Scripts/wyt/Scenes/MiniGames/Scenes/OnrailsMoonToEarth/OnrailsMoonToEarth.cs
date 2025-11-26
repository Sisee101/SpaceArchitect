using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Second generation Free return computation allowing the moon to be in a more general orbit (inclined, eccentric). 
/// Ship is assumed to be in a circular orbit around the planet. 
/// 
/// The controller creates a virtual "ghost" moon and advances it according to the requested transfer time. 
/// (The transfer time is expressed as a fraction of the Hohmann transfer time assuming both orbits are circular)
/// 
/// Use of OrbitUniversal for the ship and moon is assumed. 
/// 
/// General idea:
/// 1) Locate point on current orbit opposite the target point, place a ghost ship here
/// 2) Give the host ship a prograde velocity that causes it escape the local infleuncer (e.g. moon)
/// 3) Find point where escape hyperbola hits SOI and place a ghost ship there
/// 4) Plot path beyond SOI under influence of new influencer (e.g. Earth)
/// 5) Determine point of closest approach to new influencer. 
/// 
/// User-Interface:
/// Keys:   K/L: adjust escape burn magnitude
///         X: Execute transfer to moon
///         SPACE: pause
/// 
/// </summary>
public class OnrailsMoonToEarth : MonoBehaviour
{
    [Header("Transfer Parameters")]
    [Tooltip("Post-TEI velocity as a fraction of escape velocity")]
    [SerializeField]
    // Use a fraction of escape velocity as a reference since this is a dimensionless quantity. 
    private double escapeVelFraction = 0.9;
 
    [Header("Object Handles")]
    [SerializeField]
    private NBody spaceship = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    private NBody moonBody = null;

    //! prefab for the ghost ships created at SOI entry and exit. Need to have an NBody and OrbitUniversal attached. 
    [SerializeField]
    [Tooltip("Prefab for ghost ship with NBody, OrbitUniversal and OrbitPredictor")]
    private GameObject shipSOIPrefab = null;

    [Header("Orbit Path Materials")]
    [SerializeField]
    private Material escapeMaterial = null;
    [SerializeField]
    private Material afterSoiMaterial = null;

    //! Text to show summary of maneuver (optional)
    [Header("UI Components (optional)")]

    //! Text field used to display instructions
    [SerializeField]
    private Text instructions = null;

    [SerializeField]
    private Text periapsisInfo = null;

    [SerializeField]
    private float lineWidth = 0.1f;

    private OrbitUniversal shipOrbit;
    private OrbitUniversal moonOrbit;

    private LambertBattin lambertB;

    private double timeHohmann; 

    private float soiRadius;
    private double shipRadius;
    private double moonRadius;
    private double shipvelocity; 

    // Use a series of ghost ships for varios points of interest in the segements of the free return 
    // trajectory
    private const int NUM_GHOST_SHIPS = 3;
    private const int TEI = 0;
    private const int AT_SOI = 1;
    private const int AFTER_SOI = 2;

    private OrbitPredictor moonOrbitPredictor;
    private OrbitPredictor shipOrbitPredictor;

    private NBody[] ghostShip;
    private OrbitUniversal[] ghostShipOrbit;
    private OrbitPredictor[] ghostShipOrbitPredictor;

    // use two ghost moon, one for SOI entry and one for SOI exit. 
    private const int MOON_TEI = 0;
    private const int MOON_CROSS_SOI = 1;
    private NBody[] ghostMoon;
    private OrbitUniversal[] ghostMoonOrbit;


    //! magnitude of the escape velocity from the moon
    private double escapeVelocity = 0; 

    private GravityEngine ge;

    private bool running;

    private double t_soiExit;


    // Use this for initialization
    void Start() {

        ge = GravityEngine.Instance();

        // mass scaling will cancel in this ratio
        soiRadius = OrbitUtils.SoiRadius(planet, moonBody);

        moonOrbitPredictor = moonBody.gameObject.GetComponentInChildren<OrbitPredictor>();
        if (moonOrbitPredictor == null) {
            Debug.LogError("Moon is required to have an OrbitPredictor");
        }
        moonOrbit = moonOrbitPredictor.GetOrbitUniversal();
        // predictor may not be live yet
        moonRadius = moonBody.GetComponent<OrbitUniversal>().GetMajorAxis();

        shipOrbit = spaceship.GetComponent<OrbitUniversal>();
        if (shipOrbit == null) {
            Debug.LogError("Require that the ship have an OrbitU");
        }
        if (shipOrbit.evolveMode != OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            Debug.LogError("Controller requires ship on-rails but spaceship is off-rails");
        }
               
        // assuming circular orbit for ship
        shipRadius = shipOrbit.GetApogee();

        shipOrbitPredictor = spaceship.GetComponentInChildren<OrbitPredictor>();

        ge.AddGEStartCallback(GEStarted); 
    }

    private void GEStarted() {
        AddGhostBodies();
        escapeVelocity = Mathd.Sqrt(2.0 * ge.GetMass(moonBody) / shipRadius);
        shipvelocity = (ge.GetVelocityDoubleV3(spaceship) - ge.GetVelocityDoubleV3(moonBody)).magnitude;
    }

    private void AddGhostBodies()
    {
        // Create a ghost moon and put soiEnter/Exit ships into orbit around it. Add all to GE
        // (ghost moon does not have an OrbitPredictor)
        ghostMoon = new NBody[2];
        ghostMoonOrbit = new OrbitUniversal[2];
        for (int i = 0; i < 2; i++) {
            GameObject ghostMoonGO = Instantiate(moonBody.gameObject);
            ghostMoon[i] = ghostMoonGO.GetComponent<NBody>();
            ghostMoonOrbit[i] = ghostMoonGO.GetComponent<OrbitUniversal>();
            // might be a camera on the moon, if so turn it off
            Camera camera = ghostMoon[i].GetComponentInChildren<Camera>();
            if (camera != null)
                camera.gameObject.SetActive(false);
        }
        ghostMoon[MOON_TEI].gameObject.name = "GhostMoonTEI";
        ghostMoon[MOON_CROSS_SOI].gameObject.name = "GhostMoonSoiExit";

        ghostMoon[MOON_TEI].GetComponentInChildren<LineRenderer>().material = escapeMaterial;
        ghostMoon[MOON_CROSS_SOI].GetComponentInChildren<LineRenderer>().material = afterSoiMaterial;

        // ghost ships
        ghostShip = new NBody[NUM_GHOST_SHIPS];
        ghostShipOrbit = new OrbitUniversal[NUM_GHOST_SHIPS];
        ghostShipOrbitPredictor = new OrbitPredictor[NUM_GHOST_SHIPS];
        GameObject ghostShipGO;
        for (int i = 0; i < NUM_GHOST_SHIPS; i++) {
            ghostShipGO = Instantiate(shipSOIPrefab);
            ghostShip[i] = ghostShipGO.GetComponent<NBody>();
            ghostShipOrbit[i] = ghostShipGO.GetComponent<OrbitUniversal>();
            ghostShipOrbit[i].p_inspector = soiRadius;
            ghostShipOrbit[i].centerNbody = ghostMoon[MOON_TEI];
            ghostShipOrbitPredictor[i] = ghostShipGO.GetComponentInChildren<OrbitPredictor>();
            ghostShipOrbitPredictor[i].body = ghostShipGO;
            ghostShipOrbitPredictor[i].centerBody = ghostShipOrbit[i].centerNbody.gameObject;
            LineRenderer lineR = ghostShipOrbitPredictor[i].GetComponent<LineRenderer>();
            lineR.startWidth = lineWidth;
            lineR.endWidth = lineWidth;
            ghostShipGO.transform.SetParent(planet.gameObject.transform);
        }

        // check prefab has orbitU in Kepler mode
        if (ghostShipOrbit[0].evolveMode != OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            Debug.LogError("ShipSoi prefab must have an on-rails OrbitU");
            return;
        }

        ghostShip[TEI].gameObject.name = "Ghost TEI";
        ghostShipOrbit[TEI].p_inspector = shipRadius;
        ghostShipOrbitPredictor[TEI].GetComponent<LineRenderer>().material = escapeMaterial;
        ghostShipOrbitPredictor[TEI].hyperDisplayRadius = soiRadius;

        ghostShip[AT_SOI].gameObject.name = "GhostAtSoi";
        ghostShipOrbit[AT_SOI].p_inspector = shipRadius;
        ghostShipOrbit[AT_SOI].centerNbody = ghostMoon[MOON_CROSS_SOI];
        ghostShipOrbitPredictor[AT_SOI].GetComponent<LineRenderer>().material = escapeMaterial;
        ghostShipOrbitPredictor[AT_SOI].hyperDisplayRadius = soiRadius;
        ghostShipOrbitPredictor[AT_SOI].centerBody = ghostMoon[MOON_CROSS_SOI].gameObject;

        // customize ghost ships as necessary
        // CROSS_SOI
        ghostShip[AFTER_SOI].gameObject.name = "GhostAfterSoi";
        ghostShipOrbit[AFTER_SOI].centerNbody = planet;
        ghostShipOrbit[AFTER_SOI].p_inspector = shipRadius;
        ghostShipOrbitPredictor[AFTER_SOI].GetComponent<LineRenderer>().material = afterSoiMaterial;
        ghostShipOrbitPredictor[AFTER_SOI].centerBody = planet.gameObject;

        // Tell GE about everything
        ge.AddBody(ghostMoon[MOON_TEI].gameObject);
        ge.AddBody(ghostMoon[MOON_CROSS_SOI].gameObject);
        foreach (NBody nbody in ghostShip) {
            ge.AddBody(nbody.gameObject);
        }
    }


    private void RemoveGhostBodies() {
        foreach ( NBody nbody in ghostShip) {
            ge.RemoveBody(nbody.gameObject);
            Destroy(nbody.gameObject);
        }
        foreach (NBody nbody in ghostMoon) {
            if (nbody != null) {
                ge.RemoveBody(nbody.gameObject);
                Destroy(nbody.gameObject);
            }
        }
    }

    private void GhostSoiConfig(bool showSoi)
    {
        if (showSoi && ghostShip[AT_SOI].isActiveAndEnabled)
            return;
        if (!showSoi && !ghostShip[AT_SOI].isActiveAndEnabled)
            return;
        ghostShip[AT_SOI].gameObject.SetActive(showSoi);
        ghostShip[AFTER_SOI].gameObject.SetActive(showSoi);
        ghostMoon[MOON_CROSS_SOI].gameObject.SetActive(showSoi);     
    }

    /// <summary>
    /// Find the point where the ship orbit intersects the plane formed by the direction to the Earth and the axis of the ship's orbit.
    /// 
    /// </summary>
    /// <returns></returns>
    /// 

    // info needed by DistanceFromTeiPlane delegate. 
    private OrbitPropagator shipProp;
    private OrbitPropagator moonProp;
    private double lastTeiTime = -1.0;
    private (double, Vector3d, Vector3d) FindShipTEIPoint()
    {
        const double dt = 0.1;
        Vector3 teiPoint = Vector3.zero;

        shipProp = OrbitPropagator.GetPropagator(shipOrbit);
        moonProp = OrbitPropagator.GetPropagator(moonOrbit);
        double t = ge.GetPhysicalTimeDouble();
        double timeNow = t;
        if (t < lastTeiTime) {
            t = lastTeiTime;
        }
        double shipOrbitPeriod = shipOrbit.GetPeriod();

        double timeToTei = SecantRootFind.Secant(DistanceFromTeiPlane, t, t + dt);
        if (double.IsNaN(timeToTei) || (timeToTei < timeNow)) {
            t += shipOrbitPeriod;
            timeToTei = SecantRootFind.Secant(DistanceFromTeiPlane, t, t + dt);
        }
        (Vector3d shipPos, Vector3d shipVel) = shipProp.PropagateToTime(timeToTei);
        (Vector3d moonPos, Vector3d moonVel) = moonProp.PropagateToTime(timeToTei);
        Vector3d shipPosWorld = shipPos + moonPos;
        // Is the tei intercept on the front side?
        if (shipPosWorld.magnitude < moonPos.magnitude) {
            t += 0.5 * shipOrbitPeriod;
            timeToTei = SecantRootFind.Secant(DistanceFromTeiPlane, t, t + dt);
            (shipPos, shipVel) = shipProp.PropagateToTime(timeToTei);
        }
        lastTeiTime = timeToTei;

        return (timeToTei, shipPos, shipVel);
    }

    private double DistanceFromTeiPlane(double time)
    {
        (Vector3d shipPos, Vector3d shipVel) = shipProp.PropagateToTime(time);
        if (double.IsNaN(shipPos.x))
            return double.NaN;
        (Vector3d moonPos, Vector3d moonVel) = moonProp.PropagateToTime(time);
        shipPos += moonPos;
        Vector3 normal = Vector3d.Cross(shipOrbit.GetAxis(), moonPos).ToVector3();
        Plane plane = new Plane(normal, moonPos.ToVector3());
        double d = plane.GetDistanceToPoint(shipPos.ToVector3());
        return d;
    }

    /// <summary>
    /// Computes the transfer and updates all the ghost bodies.
    /// </summary>
    /// <returns></returns>

    // keep some results around for TransferOnRails()
    private double timeToBurn;
    private double timeToSoi; 
   
    private void ComputeTransfer() {

        double timeNow = ge.GetPhysicalTimeDouble();

        // Need a ghost moon for point where ship exits SOI 
        // 1) Position when the ship reaches TEI
        // 2) Position when ship exits Moon SOI

        // Get time until ship reaches opposition to Earth (TEI point)
        (double teiTime, Vector3d teiShipPos, Vector3d teiShipVel) = FindShipTEIPoint();
        if (double.IsNaN(teiTime)) {
            Debug.LogWarning("Could not find ship TEI position (root find failed)");
            return;
        } 
        timeToBurn = teiTime - timeNow;

        // position a moon at the time when TEI will occur
        ghostMoonOrbit[MOON_TEI].LockAtTime(timeNow + timeToBurn);

        // determine a TEI burn based on user provided escape vel fraction
        Vector3d teiBurn = Vector3d.Cross(shipOrbit.GetAxis(), teiShipPos.normalized ).normalized;
        teiBurn = teiBurn * escapeVelocity * escapeVelFraction;
        ghostShipOrbit[TEI].InitFromRVT( teiShipPos, teiBurn, timeNow, ghostMoon[MOON_TEI], relativePos: true);
        double teiBurnDelta = teiBurn.magnitude - shipvelocity;

        // does this TEI burn reach the SOI?
        OrbitUniversal orbitToSoi = ghostShipOrbitPredictor[TEI].GetOrbitUniversal();
        if ((orbitToSoi.eccentricity < 1.0) && (orbitToSoi.GetApogee() < soiRadius)) {
            if (periapsisInfo != null) {
                periapsisInfo.text = "TEI burn does not reach moon/planet SOI";
            }
            // Do not want the remaining ghost ships/moons to be running. For simplicity, leave them in GE, just do not display them
            GhostSoiConfig(false);
            return;
        } else {
            GhostSoiConfig(true);
        }

        // Determine the time to Soi Exit
        double soiPhase = orbitToSoi.GetPhaseDegForRadius(soiRadius) * Mathd.Deg2Rad;
        Vector3d soiPos = orbitToSoi.GetPositionDForThetaRadians(soiPhase, relative: true);
        timeToSoi = ghostShipOrbit[TEI].TimeOfFlight(teiShipPos, soiPos);

        // position a ghost moon at soiExit location
        ghostMoonOrbit[MOON_CROSS_SOI].LockAtTime(timeNow + timeToBurn + timeToSoi);
        ghostShipOrbit[AT_SOI].InitFromRVT(teiShipPos, teiBurn, timeNow, ghostMoon[MOON_CROSS_SOI], relativePos: true);

        // take the relative SOI pos to moon at SOI exit and make absolute (assumes planet fixed at origin)
        Vector3d soiPosAbsolute = soiPos + ge.GetPositionDoubleV3(ghostMoon[MOON_CROSS_SOI]);
        Vector3d soiVelAbsolute = ghostShipOrbit[AT_SOI].GetVelocityDForThetaRadians(soiPhase, relative: false);

        ghostShipOrbit[AFTER_SOI].InitFromRVT(soiPosAbsolute, soiVelAbsolute, timeNow, planet, relativePos:false);

        if (periapsisInfo != null) {
            // get time from soiExit to periapsis
            Vector3d periapsis = ghostShipOrbit[AFTER_SOI].GetPositionDForThetaRadians(0, relative:true);
            double timeToPeri = ghostShipOrbit[AFTER_SOI].TimeOfFlight(soiPosAbsolute, periapsis);
            timeToPeri += timeToBurn + timeToSoi;
            periapsisInfo.text = string.Format("Periapsis: {0:0.00}\ndeltaT={1}\nburn={2} m/s", 
                ghostShipOrbit[AFTER_SOI].GetPerigee(),
                GravityScaler.GetWorldTimeFormatted(timeToPeri, ge.units), 
                GravityScaler.ScaleVelPhysMagnitudeToScene(teiBurnDelta) * GravityScaler.VelocityScaletoSIUnits()
                );

        }
    }

    /// <summary>
    /// Set up a KeplerSequence to do the two phases of the transfer as Kepler mode conics.
    /// 
    /// Add all the ghost orbits with the required times
    /// </summary>
    /// <param name="transferTime"></param>
    private void TransferOnRails() {
        // the ship needs to have a KeplerSequence
        KeplerSequence kseq = spaceship.GetComponent<KeplerSequence>();
        if (kseq == null) {
            Debug.LogError("Could not find a KeplerSequence on " + spaceship.name);
            return;
        }

        // Segement 1: Ellipse or Hyperbola that departs moon SOI
        double t_now = ge.GetPhysicalTime();
        Vector3d r0 = new Vector3d();
        Vector3d v0 = new Vector3d();
        double time0 = 0;
        ghostShipOrbit[TEI].GetRVT(ref r0, ref v0, ref time0);
        kseq.AppendElementRVT(r0, v0, t_now + timeToBurn, true, spaceship, moonBody, TeiBurn);

        // Segment 2: Add with respect to the planet
        ghostShipOrbit[AFTER_SOI].GetRVTAbsolute(ref r0, ref v0, ref time0);
        OrbitUniversal segment2 = kseq.AppendElementRVT(r0, v0, t_now + timeToBurn + timeToSoi, false, spaceship, planet, callback: null);
        segment2.centerNbody = planet;

     }

    /// <summary>
    /// Callback for when the ship leaves the moon SOI. Change the the ship orbit predictor center object
    /// back to the planet. 
    /// </summary>
    /// <param name="orbitU"></param>
    public void TeiBurn(OrbitUniversal orbitU) {
        shipOrbitPredictor.SetCenterObject(moonBody.gameObject);
        shipOrbitPredictor.hyperDisplayRadius = soiRadius;
    }

    private void ExecuteTransfer() {

        TransferOnRails();
        // remove placeholder ships/orbit visualizers
        RemoveGhostBodies();
    }

    // Update is called once per frame
    void Update() {

        if (!running) {
            // Getting user input for FR
            ComputeTransfer();
            if (Input.GetKeyUp(KeyCode.X)) {
                // execute the transfer
                ExecuteTransfer();
                if (instructions != null)
                    instructions.gameObject.SetActive(false);
                running = true;
                ge.SetEvolve(true);
            }
        } 


        if (Input.GetKeyUp(KeyCode.Space)) {
            ge.SetEvolve(!ge.GetEvolve());
        } 

    }

}
