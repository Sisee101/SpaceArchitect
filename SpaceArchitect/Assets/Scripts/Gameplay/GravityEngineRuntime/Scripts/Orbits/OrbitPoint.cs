using UnityEngine;


/// <summary>
/// OrbitPoint places an object at a specified location on the orbit. The location
/// can be specified in various ways according to the PointType enum. 
/// 
/// The game object to which this component is attached will be placed at the 
/// specified location. 
/// 
/// If the script is attached to an NBody element it will set the position and velocity of the NBody
/// element based on the values at the specified point of the orbit. This requires that the NBody 
/// attached has been added to GE. 
/// 
/// </summary>
public class OrbitPoint : MonoBehaviour, IFixedOrbit, INbodyInit
{
    // See OrbitPointEditor for inspector
    public bool mouseControl = true; 

    public OrbitPredictor orbitPredictor = null;

    public enum PointType  {APOAPSIS,
                        PERIAPSIS,
                        ALTITUDE_1ST,
                        ALTITUDE_2ND,
                        ASCENDING_NODE,
                        DESCENDING_NODE,
                        FIXED_TIME,
                        PHASE,
                        PHASE_FROM_MOUSE };

    public PointType pointType = PointType.APOAPSIS;

    //! data field to be used with fixed time, altitude or phase point types
    public double pointData  = 0.0;

    public NBody timeRefBody = null; 

    //! Camera reference is only required if using PHASE_FROM_MOUSE
    public Camera sceneCamera = null;

    //! NBody the OrbitPoint script is attached to
    private NBody nbody = null;

    private OrbitUniversal orbitU;
    private GravityEngine ge;

    private float lastPhase = float.NaN; // ensure update first time through
    private float mousePhase = 0;

    private Vector3 relativeMousePos;

    // Keep position/vel
    private Vector3d lastPos;
    private Vector3d lastVel;

    private NBody centerNbody; 

    private const double CENTER_TOL = 1E-3; 

    // Start is called before the first frame update
    void Awake()
    {
        Init();
    }

    public void Init()
    {
        nbody = GetComponent<NBody>(); // special case if attached to Nbody
        ge = GravityEngine.Instance();
    }


    void OnEnable()
    {
        // ensure position/vel will be re-set when we get re-enabled
        lastPhase = float.NaN;
    }

    // Need to set initialPhyPos in NBody
    public void InitNBody(float physicalScale, float massScale)
    {
        Init();
        DoUpdate(true);
    }

    public void SetOrbitPredictor(OrbitPredictor op)
    {
        orbitPredictor = op;
        // reset the info we get from OP. DoUpdate will fill it in. 
        lastPhase = float.NaN;
        orbitU = null;
        centerNbody = null;
        DoUpdate(false);
    }

    public void SetPointType(PointType ptype) {
        pointType = ptype;
        DoUpdate(false);
    }

    public void SetPointData(double data) {
        pointData = data;
        DoUpdate(false);
    }

    public OrbitUniversal GetOrbit() {
        if (orbitU == null)
            orbitU = orbitPredictor.GetOrbitUniversal();

        return orbitU;
    }

    public float GetPhase() {
        return lastPhase;
    }

    public NBody GetNBody() {
        return nbody;
    }

    public void SetTimeRefBody(NBody refBody)
    {
        timeRefBody = refBody;
    }

    public void SetSceneCamera(Camera c)
    {
        sceneCamera = c;
    }

    /// <summary>
    /// Get the time required to move to the current orbit point position in GE internal time. 
    /// </summary>
    /// <param name="fromNbody"></param>
    /// <returns></returns>
    public double TimeToOrbitPoint(NBody fromNbody) {
        GetOrbit();
        return orbitU.TimeOfFlight(ge.GetPositionDoubleV3(fromNbody), lastPos);
    }

    /// <summary>
    /// Set the orbit point position based on the position of the provided NBody evolved into the
    /// future by the value of time. 
    /// 
    /// Only valid when mode is FIXED_TIME.  
    /// </summary>
    /// <param name="time"></param>
    public void SetTime(NBody refBody, double time) {
        if (pointType != PointType.FIXED_TIME) {
            Debug.LogError("Require FIXED_TIME type have " + pointType + " on " + gameObject.name);
            return;
        }
        timeRefBody = refBody;
        pointData = time;
        DoUpdate(false);
    }

    public float MouseDistanceToShip() { 
        Vector3 mousePos = Input.mousePosition;
        Vector3 shipPos = sceneCamera.WorldToScreenPoint(transform.position);
        Vector3 relativePos = mousePos - shipPos;
        return relativePos.magnitude;
    }

    /// <summary>
    /// Handle mouse input and return true if this resulted in a change of position
    /// </summary>
    /// <returns></returns>
    public bool HandleMouseInput() {
        if (pointType == PointType.PHASE_FROM_MOUSE) {
            // Use the angle from the mouse click to the center of the orbit predictor to determine
            // a phase to place the element. This phase is with respect to the periapsis point, so 
            // need to project that on screen as well.
            if (Input.GetMouseButton(0)) {
                Vector3 mousePos = Input.mousePosition;
                Vector3 originPos = orbitU.centerNbody.transform.position;
                Vector3 origin = sceneCamera.WorldToScreenPoint(originPos);
                relativeMousePos = (mousePos - origin).normalized;
                // peri: Need to map peri position onto the screen via world position. 
                // (peri position includes center body offset)
                Vector3 periPos = ge.MapPhyPosToWorld( orbitU.PositionForPhase(0f));
                Vector3 periLine = (sceneCamera.WorldToScreenPoint(periPos) - origin).normalized;
                float periPhase = Mathf.Atan2(periLine.y, periLine.x) * Mathf.Rad2Deg;
                mousePhase = Mathf.Atan2(relativeMousePos.y, relativeMousePos.x) * Mathf.Rad2Deg - periPhase;
            } else {
                mousePhase = lastPhase;
                // special case after OnEnable(). Awkward.
                if (float.IsNaN(lastPhase))
                    mousePhase = 0; 
            }
        }
        return (mousePhase != lastPhase);
    }

    /// <summary>
    /// Set the flag that enables mouse checking in the Update loop. In some cases this may be delegated to a
    /// controller class e.g. TransferSceneContoller in TransferWithOrbitPoint scene. 
    /// </summary>
    /// <param name="control"></param>
    public void SetMouseControl(bool control) {
        mouseControl = control;
    }

    /// <summary>
    /// Set the initial phase for the PHASE_FROM_MOUSE mode. Typically set to a few degrees ahead
    /// of the ship position so that it is visually distinct from the ship. 
    /// </summary>
    /// <param name="phase"></param>
    public void SetMousePhase(float phase) {
        mousePhase = phase;
    }

    void FixedUpdate() {
        DoUpdate(false);
    }

    // Update is called once per frame.
    // Init flag is a startup bug fix. A bit icky. 
    public void DoUpdate(bool init)
    {
        if (!ge.IsSetup())
            return;

        // Another init edge-case: OrbitPredictor may not be inited yet
        if ((orbitPredictor == null) || (!orbitPredictor.IsConfigured()))
            return;

        // Awkward. OrbitPredictor gets orbitU in start(). Start ordering would be annoying...
        if (orbitU == null) {
            orbitPredictor.Init();
            orbitU = orbitPredictor.GetOrbitUniversal();
        }

        if (centerNbody == null)
            centerNbody = orbitPredictor.centerBody.GetComponent<NBody>();

        if (centerNbody == null)
            return;

        if (mouseControl) {
            HandleMouseInput();
        }

        float phase = 0;
        switch (pointType) {
            case PointType.APOAPSIS:
                // only defined for an ellipse
                if (orbitU.eccentricity < 1.0) {
                    phase = 180f;
                } else {
                    Debug.LogWarning("Cannot determine apoapsis unless orbit is ellipse ecc=" + orbitU.eccentricity);
                    return;
                }
                break;

            case PointType.PERIAPSIS:
                phase = 0f;
                break;

            case PointType.ALTITUDE_1ST:
                phase = orbitU.GetPhaseDegForRadius(pointData);
                break;

            case PointType.ALTITUDE_2ND:
                phase = -orbitU.GetPhaseDegForRadius(pointData);
                break;

            case PointType.FIXED_TIME:
                if (timeRefBody != null) {
                    // Don't assume the NBody has an orbit, build a new OrbitUniversal from internal details 
                    // Greedy implementation - could move some to SetTime
                    // TODO: Check if things have changed before doing all this every frame
                    OrbitUniversal targetOrbit = timeRefBody.gameObject.AddComponent<OrbitUniversal>();
                    targetOrbit.InitFromActiveNBody(timeRefBody, orbitU.centerNbody, OrbitUniversal.EvolveMode.KEPLERS_EQN);
                    double time = GravityEngine.Instance().GetPhysicalTimeDouble() + pointData;
                    double[] ePos = new double[3];
                    double[] eVel = new double[3];
                    targetOrbit.Evolve(time, ge.GetWorldState(), ref ePos, ref eVel);
                    if (nbody == null) {
                        transform.position = ge.MapPhyPosToWorld(
                                    new Vector3((float)ePos[0], (float)ePos[1], (float)ePos[2]));
                    } else {
                        // need to set explicitly, since when paused evolve will not be called.
                        lastPos = new Vector3d(ref ePos);
                        lastVel = new Vector3d(ref eVel);
                        SetRV(init, nbody, lastPos, lastVel);
                    }
                    MonoBehaviour.Destroy(targetOrbit);
                }
                return;

            case PointType.ASCENDING_NODE:
                phase = (float)(0f - orbitU.omega_lc);
                break;

            case PointType.DESCENDING_NODE:
                phase = (float)(180f - orbitU.omega_lc);
                break;

            case PointType.PHASE:
                phase = (float) pointData;
                break;

            case PointType.PHASE_FROM_MOUSE:
                phase = mousePhase;
                break;

            default:
                break;
        }

        Vector3d centerPos = ge.GetPositionDoubleV3(centerNbody);
 
        lastPos = new Vector3d(orbitU.PositionForPhase(phase));
        lastVel = new Vector3d(orbitU.VelocityForPhaseRelative(phase));
        if (nbody == null) {
            // Need to adapt the phyPosition to a scene position based on GE scale etc. 
            transform.position = ge.MapPhyPosToWorld(lastPos.ToVector3());
        } else {
            SetRV(init, nbody, lastPos, lastVel);
        }
        lastPhase = phase;
    }

    private void SetRV(bool init, NBody nbody, Vector3d lastPos, Vector3d lastVel)
    {
        if (init) {
            nbody.initialPhysPosition = lastPos.ToVector3();
            nbody.vel_phys = lastVel.ToVector3();
        } else {
            // need to set explicitly, since when paused evolve will not be called. 
            ge.SetPositionDoubleV3(nbody, lastPos);
            ge.SetVelocityDoubleV3(nbody, lastVel);
        }
    }

    public bool IsOnRails() {
        return true;
    }

    public void PreEvolve(float physicalScale, float massScale) {
        return; 
    }

    public void Evolve(double physicsTime, GravityState gs, ref double[] r, ref double[] v, bool doCallbacks = true) {
        r[0] = lastPos.x;
        r[1] = lastPos.y;
        r[2] = lastPos.z;
        v[0] = lastVel.x;
        v[1] = lastVel.y;
        v[2] = lastVel.z;
    }

    // Orbit Point acts as a FixedBody
    public Vector3 GetVelocity() {
        return lastVel.ToVector3();
    }

    public Vector3 GetPosition() {
        return lastPos.ToVector3();
    }

    public Vector3d GetPositionDouble()
    {
        return lastPos;
    }

    public Vector3d GetPositionDoubleV3() {
        return lastPos;
    }

    public void GEUpdate(GravityEngine ge) {
        throw new System.NotImplementedException();
    }

    public void Move(Vector3 position) {
        throw new System.NotImplementedException();
    }

    public void SetNBody(NBody nbody) {
        throw new System.NotImplementedException();
    }

    public Vector3 ApplyImpulse(Vector3 impulse) {
        throw new System.NotImplementedException();
    }

    public NBody GetCenterNBody() {
        if (orbitU == null)
            orbitU = orbitPredictor.GetOrbitUniversal();
        return orbitU.GetCenterNBody(); 
    }

    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        throw new System.NotImplementedException();
    }

    public string DumpInfo() {
        return string.Format("      pos={0} vel={1}\n", lastPos, lastVel);
    }


}
