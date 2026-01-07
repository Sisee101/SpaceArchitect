using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit predictor.
/// An in-scene object that will determine the future orbit based on either:
///     the current position and velocity
///     current position and velocity from script
///     position and velocity from script
///     
/// In the last case, the orbit predictor need not have an NBody specified. 
/// 
/// Depending on the velocity the orbit may be an ellipse or a hyperbola. This class
/// use an OrbitUniversal, since it can handle both cases. 
///
/// Orbit prediction is based on the two-body problem and is with respect to one other
/// body (presumably the dominant source of gravity for the affected object). The OrbitPredictor will
/// create an OrbitUniversal and update the classical orbital elements (COE) based on the current
/// position and velocity on each Update() cycle.
/// 
/// The orbital elements can be retreived from the OrbitUniversal via e.g
///     orbitPredictor.GetOrbitUniversal().eccentricity
///
/// The general N-body orbit prediction problem is significantly harder - it requires simulating the
/// entire scene into the future - re-computing whenever user input is provided. This is provided by
/// the Trajectory prediction sub-system. 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitPredictor : MonoBehaviour {

    //! Number of points to be used in the line renderer for the orbit plot
    public int numPoints = 100;
    //! The body for which an orbit is to be predicted
    public GameObject body;
    //! The object which body is in orbit around. 
    public GameObject centerBody;
    //! Script code will set the velocity explicitly. Do not retreive automatically
    public bool velocityFromScript = false;

    //! Script code will set the velocity explicitly. Do not retreive automatically
    public bool positionFromScript = false;

    //! Use an additional line renderer to show a segement of the orbit to a specified. 
    public bool showSegment = false;
    public bool segmentFoldout = false; 

    public GameObject segmentEnd = null;
    public LineRenderer segementLineR = null;
    public bool retrograde = false;

    //! display radius for hyperbola. If zero will use position of body and show segments from body to +/- periapsis
    public float hyperDisplayRadius = 100;

    public int numPlaneProjections;
    public Vector3 planeNormal = Vector3.forward;

    //! Preserve initial values from editor
    public Vector3 editorPos = Vector3.zero;
    public Vector3 editorVel = Vector3.zero;

    //! velocity of body when set explicitly by script
    public Vector3d velocity = Vector3d.zero;

    //! position of body when set explicitly by script
    public Vector3d position = Vector3d.zero;

    // Both position and velocity are from a script. Do not need an NBody in this case!
    private bool pvFromScript = false;

    public bool dejitterCOE = false;

    private NBody nbody;
    private NBody centerNbody;
    private OrbitUniversal orbitU;
    private BinaryOrbit binaryOrbit; 

    private LineRenderer lineR;

    private GravityEngine ge;

    // initial conditions of OrbitU (used in Kepler mode)
    private Vector3d r0;
    private Vector3d v0;
    private double time0;

    private int orbitPredictorInterval;

    private Vector3d lastCenterPos; 

    // tolerance in change in R0 and V0 to require recompute of an optimized Kepler orbit
    private const double TOL_KEPLER_OPT = 1E-4;  

    void Awake() {
        // If cloned from a live object, may already have an OU
        orbitU = GetComponent<OrbitUniversal>();
        if (orbitU == null) {
            orbitU = transform.gameObject.AddComponent<OrbitUniversal>();
        }
    }

    // Use this for initialization
    // Start() NOT Awake() to ensure that objects created on the fly can have centerBody etc. assigned
    // Awake is called from within Object.Instatiate()
    void Start() {
        ge = GravityEngine.Instance();

        pvFromScript = positionFromScript && velocityFromScript;
        if (!pvFromScript) {
            if (body == null) {
                Debug.LogError("Body is null " + gameObject.name);
            }
            nbody = body.GetComponent<NBody>();
            if (nbody == null) {
                Debug.LogWarning("Cannot show orbit - Body requires NBody component");
                return;
            }
        }
        if (positionFromScript && (editorPos != Vector3.zero))
            position = new Vector3d(editorPos);
        // Awkward start if velocity is set before Awake from a script. Only use editor value if non-zero.
        if (velocityFromScript && (editorVel != Vector3.zero))
            velocity = new Vector3d(editorVel);

        if (centerBody == null) {
            Debug.LogError("Center body is null " + gameObject.name);
        }
        centerNbody = centerBody.GetComponent<NBody>();
        orbitU.SetNBody(nbody);
        orbitU.centerNbody = centerNbody;
        orbitU.SetOrbitPredictor(true);
        orbitU.SetDejitterCOE(dejitterCOE);

        // special case. Binary orbit must have NBody
        binaryOrbit = centerBody.GetComponent<BinaryOrbit>();
        if (binaryOrbit != null) {
            orbitU.SetMu(binaryOrbit.MassForPredictor(nbody));
        } else if (!pvFromScript) {
            orbitU.InitMu();
        }
        // Need full init before start calling with RV
        r0 = Vector3d.zero;
        v0 = Vector3d.zero;
        time0 = 0.0;

        lastCenterPos = Vector3d.zero;

        lineR = GetComponent<LineRenderer>();
    	if (lineR) 
			lineR.positionCount = numPoints + 2 * numPlaneProjections;

        orbitPredictorInterval = Random.Range(0, ge.orbitPredictorInterval);
        // this could run right away (if GE has started) so must be at the bottom
        ge.AddGEStartCallback(GeStart, 0);
    }

    public bool IsConfigured()
    {
        if (!pvFromScript && (nbody == null))
            return false;

        return (centerNbody != null) && (orbitU != null);
    }

	private void GeStart()
	{
        UpdateOrbitU(firstTime: true);
	}

    public void Init()
    {
        Awake();
        Start();
        UpdateOrbitU(firstTime: true);
    }

    // if other scripts enable/disable this OP then turn off line renderer as well
    void OnEnable() {
        if (lineR != null)
            lineR.enabled = true;
    }

    void OnDisable() {
        if (lineR != null)
            lineR.enabled = false;
    }

    public void SetNBody(NBody nbody) {
        // During prefab instantiation this might get called before Start() has run
        if (orbitU == null) {
            Awake();
        }
        orbitU.SetNBody(nbody);
        body = nbody.gameObject;
        // Init();
    }

    public void SetCenterObject(GameObject newCenterBody) {
        centerBody = newCenterBody;
        centerNbody = newCenterBody.GetComponent<NBody>();
        if (orbitU == null) {
            orbitU = transform.gameObject.AddComponent<OrbitUniversal>();
        }
        orbitU.SetNewCenter(centerNbody);

        // optimization, force new RVT value
        time0 = -1f;
    }

    public void SetPosition(Vector3 v)
    {
        position = new Vector3d(v);
    }

    public void SetPosition(Vector3d v)
    {
        position = v;
    }

    public void SetVelocity(Vector3 v) {
        velocity = new Vector3d(v);
    }

    public void SetVelocity(Vector3d v)
    {
        velocity = v;
    }

    public void SetFromScriptFlags(bool posFlag, bool velFlag) {
        positionFromScript = posFlag;
        velocityFromScript = velFlag;
        pvFromScript = positionFromScript && velocityFromScript;
    }


    public Vector3 GetVelocity() {
        return velocity.ToVector3();
    }

    public Vector3d GetVelocityV3()
    {
        return velocity;
    }

    public OrbitUniversal GetOrbitUniversal() {
        return orbitU;
    }

    public double GetCurrentPhase()
    {
        // Due to optimization or load sharing may not be completely up to date
        UpdateOrbitU();
        return orbitU.phase;
    }

    /// <summary>
    /// Test method to allow setup for unit tests
    /// </summary>
    public void TestRunnerSetup() {
        Awake();
        Start();
        orbitU.Init();
        Update();
    }

    /// <summary>
    /// Using the current position and velocity re-init the OrbitU used by the predictor to 
    /// determine the current orbital elements for prediction. 
    /// 
    /// firstTime is definitly icky, but does save a lot of duplication in OrbitU code
    /// </summary>
	public void UpdateOrbitU(bool firstTime = false)
	{
		// nbody may not have been added yet
        if (!pvFromScript && (nbody.engineRef == null))
            return;

        Vector3d pos;
        if (positionFromScript) {
            pos = position;
        } else {
            pos = ge.GetPositionDoubleV3(nbody);
        }
        Vector3d vel;
        if (velocityFromScript) {
            vel = velocity;
        } else {
            vel = ge.GetVelocityDoubleV3(nbody);

        }
        OrbitUniversal nbodyOrbit = null;
        if (nbody != null) {
            nbodyOrbit = nbody.GetComponent<OrbitUniversal>();
        }
        if (!firstTime)
            orbitU.ReInitForOrbitPredictor(pos, vel, ge.GetPhysicalTimeDouble(), nbodyOrbit);
        else {
            orbitU.InitFromRVT(pos, vel, ge.GetPhysicalTimeDouble(), centerNbody, relativePos: false);
            if (dejitterCOE && nbodyOrbit != null) {
                // argp/raan caching
                // user requirement to report a non-zero argp when e close to zero if one was provided in init data (esp. TLEs)
                orbitU.CacheArgpRaan(nbodyOrbit);
            }
        }
    }

    /// <summary>
    /// Check if the NBody orbit we're predictiting is fixed and on rails. 
    /// 
    /// </summary>
    /// <returns></returns>
    private OrbitUniversal KeplerOrbit()
    {
        OrbitUniversal keplerOrbit = null;

        if (velocityFromScript || positionFromScript)
            return null;

        GravityEngine.FixedBody fixedBody = nbody.engineRef.fixedBody;
        if ((fixedBody != null) && (fixedBody.fixedOrbit != null) && (fixedBody.kepler_depth <= 1)) {
            if (nbody.engineRef.fixedBody.keplerSeq != null) {
                keplerOrbit = nbody.engineRef.fixedBody.keplerSeq.GetCurrentOrbit();
            } else if (nbody.engineRef.fixedBody.orbitU != null) {
                keplerOrbit = nbody.engineRef.fixedBody.orbitU;
            }
            // not sure this can happen - paranoid. 
            if ((keplerOrbit != null) && (keplerOrbit.evolveMode != OrbitUniversal.EvolveMode.KEPLERS_EQN))
                keplerOrbit = null;
        }
        return keplerOrbit;
    }

    public void DoUpdate()
    {
        Update();
    }

    // Update is called once per frame
    void Update() {
        // on a deferred add (while running) may get an OP update before actually added to GE. This would be bad.
        if (!pvFromScript) {
            if ((nbody == null) || (nbody.engineRef == null))
                return;

            if (!ge.IsActive(nbody))
                return;

            // body was just added to GE (after Start)
            if (centerNbody == null)
                Init();
        }
        Vector3d centerPos = GravityEngine.Instance().GetPositionDoubleV3(centerNbody);

        // Orbit segment
        if (showSegment) {
            // since the segment code just uses this for the angle, the scale does not matter
            Vector3 destPoint = ge.UnmapFromScene(segmentEnd.transform.position);
            Vector3 pos;
            if (positionFromScript) {
                pos = position.ToVector3();
            } else {
                pos = ge.GetPhysicsPosition(nbody);
            }
            Vector3[] positions;
            if (orbitU.eccentricity < 1.0) {
                // ellipse segement uses absolute positions for pos/dest
                positions = orbitU.EllipseSegmentProRetro(numPoints, centerPos.ToVector3(), pos, destPoint, retrograde);
            } else {
                float radius = (pos - centerPos.ToVector3()).magnitude;
                positions = orbitU.HyperSegmentSymmetric(numPoints, centerPos.ToVector3(), radius, doSceneMapping: true);
            }
            segementLineR.positionCount = positions.Length;
            segementLineR.SetPositions(positions);
        }
        // OPTIMIZATION: Load spreading of OrbitU updates over interval defined in GE advanced
        // load spreading based on using a random interval from Start()
        if (ge.GetOrbitPredictorCounter() != orbitPredictorInterval) {
            return;
        }

        // OPTIMIZATION: Special case 
        // If there is an on-rails parent and no velocity change,
        // then no need to change the points in the orbit. Since parent may go on/off rails, need to check each time. 
        OrbitUniversal orbitForPoints = KeplerOrbit();
        if (ge.orbitPredictorKeplerOpt && (orbitForPoints != null)) {
                // If the center of the Kepler orbit is not moving, then do not need to re-request the points in the orbit
                if ((centerPos-lastCenterPos).magnitude < TOL_KEPLER_OPT) {
                    if (orbitForPoints.RVT_Equal(ref r0, ref v0, ref time0, TOL_KEPLER_OPT)) {
                        // Kepler mode with no change in initial conditions -> same orbit points are ok
                        return;
                    } else {
                        orbitForPoints.GetRVT(ref r0, ref v0, ref time0);
                    }
                }
        } else {
            UpdateOrbitU();
            orbitForPoints = orbitU;
        }

    if( lineR )
    {
        // Can be a slight glitch at end of orbit for ellipses
        lineR.loop = (orbitForPoints.eccentricity < 1.0);

        Vector3[] points = orbitForPoints.OrbitPositions(numPoints, centerPos.ToVector3(), ge.mapToScene, hyperDisplayRadius);
        int totalPoints = points.Length + 2 * numPlaneProjections;
        if (numPlaneProjections > 0) {
            // Add lines to the inclination=0 plane of the orbit
            Vector3[] pointsWithProj = new Vector3[totalPoints];
            int projEvery = points.Length / numPlaneProjections;
            int p = 0;
            int orbitP = 0; 
            while (p < totalPoints) {
                pointsWithProj[p++] = points[orbitP];
                if ((orbitP % projEvery) == 0) {
                    // add a line to plane and back
                    pointsWithProj[p++] = Vector3.ProjectOnPlane(points[orbitP], planeNormal);
                    pointsWithProj[p++] = points[orbitP];
                }
                orbitP++;
            }
            lineR.positionCount = pointsWithProj.Length;
            lineR.SetPositions(pointsWithProj);
        } else {
            // just draw the orbit (no projection lines to the plane)
            lineR.positionCount = points.Length;
            lineR.SetPositions(points);
        }   
        
    }
    }
}
