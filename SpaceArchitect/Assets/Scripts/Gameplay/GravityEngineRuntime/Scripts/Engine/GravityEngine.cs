using System.Collections.Generic;
using UnityEngine;

/*! \mainpage Gravity Engine Unity Asset
 *
 *  On-line documentation: <a href="http://nbodyphysics.com/blog/gravity-engine-doc-1-3-2-2-2/">Gravity Engine Documentation</a>
 *
 *  To get started: <a href="http://nbodyphysics.com/blog/gravity-engine-doc-1-3-2-2-2/getting-started/">Getting Started</a>
 *  
 *  NOTE: It is generally a good idea to set GE to execute before other scripts in the scene to ensure they all
 *  get a consistent result/world state in their processing. (see https://docs.unity3d.com/Manual/class-MonoManager.html)
 *  
 *  To use gravity engine in a Unity scene there must be an object with a GravityEngine component. GravityEngine will
 *  compute and move objects that have NBody components or particle systems that have GravityParticles components.
 *
 *  GravityEngine is commonly used in a mode that auto-detects all bodies in a scene. This default mode can be
 *  turned off and objects can be added as part of an explicit list or via API calls to AddBody().
 *
 *  The most commonly used components are: NBody, OrbitUniversal, OrbitPredictor and TransferShip.
 *
 *  Tutorial and demo videos can be found on You Tube on the <a href="https://www.youtube.com/channel/UCxH9ldb8ULCO_B7_hZwIPvw">NBodyPhysics channel</a>
 *
 *  Support/Questions: nbodyphysics@gmail.com
 */


/// <summary>
/// GravityEngine
/// Primary controller of Nbody physics evolution with selectable integration algorithms. Singleton. 
/// 
/// Bodies
/// The positions and masses of the N bodes are initialized here and passed by reference to the integrator. 
/// This allows high precision evolution of the bodies and a simpler integration scheme for particle systems
/// moving in the fields of the N bodies. 
///
/// Particles
/// GE creates a ParticleEvolver and evolves particle systems once per fixed update based on new positions
/// of the N bodies. Particles are massless and so do not interact with each other (too computationally expensive).
///
/// </summary>
public class GravityEngine : MonoBehaviour {

	public static string TAG = "GravityEngine";
	//! Singleton instance handle. Initialized during Awake().
	public static GravityEngine instance;

    //! global flag for debug logging789
    public const bool DEBUG = false;

    //! Enable single step mode (code only available from GEConsole)
    private bool singleStep = false;
    private bool stepHasRun = false;

    /// <summary>
    /// Use the transform to reposition the scene via MapToScene and the transform value. 
    /// Setting this will also enable mapToScene. 
    /// </summary>
    public bool useTransform = false;

    /// <summary>
    /// Map To Scene
    /// Commonly, GE is used as a world controller and there is simple mapping from the physics space to the scene. 
    /// In this case leave MapToScene false to avoid extra calculations on each update. 
    ///
    /// In applications where GE is supplying information in a scene element (e.g. floating above the navigation console on the
    /// bridge of a starship) then it is useful to be able to re-locate, rotate and scale the body positions and to do this dynamically. 
    /// In this case enable Map To Scene. (Note model scales will not be adjusted - that's up to game logic outside of GE)
    ///
    /// Flag to apply GE transform to scale, rotate and re-position all objects under it's control
    ///</summary>
    public bool mapToScene = false;

    /// <summary>
    /// Class to be used to delegate the final mapping to the scene. Can be used in cases when a non-linear mapping is desired and the 
    /// simpler useTransformm to map is not able to give the desired result. 
    /// 
    /// Must implement a Map and Unmap function. 
    /// </summary>
    public GameObject mapToSceneGameObject; 
    private GEMapToSceneInterface mapToSceneDelegate;

    /// <summary>
    /// Class that implements the multiplayer abstract class. (Can't be an interface since they are not supported in the inspector).
    /// 
    /// See the base class defintion for details. 
    /// </summary>
    public GameObject multiplayerInterfaceGO;
    public GEMultiplayerInterface geMultiplayerIF; 


    /// <summary>
    /// Set orbits to be in XZ plane. OrbitUniversal will use this in positioning objects and determining the 
    /// orbital elements when using orbit predictors. 
    /// This option is useful for aligning Unity's standard scene view in the editor with the planes of orbits
    /// (which are always in XY by default in the physics literature)
    ///</summary>
    public bool xzOrbits; 

    /// <summary>
    /// GE allows for different modes that control when and how the game object transforms are updated:
    /// FIXED_UPDATE: Run physics and updates on the FixedUpdate call. 
    /// FIXED_INTERPOLATE: Run physics on FixedUpdate and update positions on Update using linear interpolation
    /// UPDATE: Run physics and positions on the Update loop. 
    /// 
    /// Running physics on the fixed update ensures consistent CPU load spreading. In some cases there may be object
    /// jittering when mixing rigidbody objects and GE objects in the same scene at small scales. In this case using
    /// interpolation or moving to UPDATE mode will improve this situation. 
    /// </summary>
    public enum UpdateMode { FIXED_UPDATE, FIXED_INTERPOLATE, UPDATE};

    public UpdateMode updateMode = UpdateMode.FIXED_UPDATE;

	/// <summary>
	/// On startup set the center of mass (CM) to the specified position and velocity. This is commonly used
	/// to null out residial CM velocity rates on scene startup when massive planets are being used and when
	/// anchoring the center with a FixedObject is not desired.
	///
	/// After startup GE will call a routine to reset the center of mass pos/vel when massive bodies are added or
	/// removed.
	/// </summary>
    public bool setCenterOfMass = false;
    public Vector3 cmPosition = Vector3.zero;
    public Vector3 cmVelocity = Vector3.zero;

    /// <summary>
    /// Interval (in Updates) at which NBody (non-Kepler) will run their OrbitPredictors. Orbit predictors in large N
    /// scenarios can use a lot of CPU and dominate the max N when updated every frame. 
    /// 
    /// Cannot be altered after GE starts (OPs generate a random seed to decide when to update to load balance). 
    /// </summary>
    public int orbitPredictorInterval = 1;
    public bool orbitPredictorKeplerOpt = false;
    private int orbitPredictorCounter; 
	                                       
	/// <summary>
	/// Integrator choices:
	/// LEAPFROG - a fixed timestep, with good energy conservation 
	/// HERMITE - an adaptive timestep algorithm with excellent energy conservation
	/// AZTRIPLE - For 3 bodies ONLY. Works in regularized co-ordinates that allow close encounters.
	/// ANY_FORCE_FL - Leapfrog with a force delegate
	/// <summary>
	public enum Algorithm { LEAPFROG, HERMITE8, AZTRIPLE};
	private static string[] algorithmName = new string[]{"Leapfrog", "Hermite (adaptive)", "TRIPLE (Regularized Burlisch-Stoer)"};

	/// <summary>
	/// The force used when one of the ANY_FORCE integrators is selected. 
	/// Any force use requires that the scale be set to DIMENSIONLESS. 
	/// </summary>
	public ForceChooser.Forces force; 

	//! Algorithm for numerical integration of massive bodies
	public Algorithm algorithm; 

	//! Automatically detect all objects with an NBody component and add to the engine.
	public bool detectNbodies = true; 

	//! Enable trajectory prediction - TrajectoryTrails attached to NBody objects will be updated
	public bool trajectoryPrediction = false; 

	//! time to evolve forward for trajectory prediction
	public float trajectoryTime = 15f;

	// Added to reduce impact of "endless trajectory record data" bug
	//! min distance allowed between record data points
	public float trajectoryDataTimeDistance = 0.1f;

 	//! Used by Trajectory when text labels for time are enabled
  	public GameObject trajectoryCanvas; 

	//! Optional parent object to assign trajectory markers to (so they do not clutter up the root object space)
	public GameObject markerParent;

    //! Multiplier for trajectory recompute simulations per frame. Low number spread update over more frames and
    //! have less impact on run-time performance and the cost of longer times to see new trajectory
    public float trajectoryComputeFactor = 4f;

	/// <summary>
	/// physToWorldFactor: factor to allow distance measurements in NBE to be on a different scale than in the Unity world view. 
	/// This is useful when taking initial conditions from literature (e.g. the three body solutions) in which 
	/// the data provided are normalized to [-1..1]. Having all world objects in [-1..1] becomes awkward. Setting
	/// this scale allows the Unity positions to be expanded to a more convenient range.
	///
	/// If this is used for objects that are created in Unity world space it will change the distance scale used
	/// by the physics engine and consequently the time evolution will also change. Moving objects closer (physToWorldFactor > 1)
	/// will result in stronger gravity and faster interactions. 
	/// </summary>
	public float physToWorldFactor = 1.0f;

	public GravityScaler.Units units;

	/// <summary>
	/// The length scale.
	/// Scale from NBody initial pos to Unity position
	/// Expressed in Unity units per scale unit (pos = scale_pos * lengthScale).
	/// Changing this value will result in changes in the positions of all NBody objects in 
	/// the scene. It is intended for use by the Editor scripts during script setup and not
	/// for run-time changes.
	/// </summary>
	[SerializeField]
	private float _lengthScale = 1f; 
	//! Orbital scale in e.g. Unity unity per km
	public float lengthScale {
		get { return _lengthScale; }
		set { UpdateLengthScale(value);}
	}

    /// <summary>
    /// Mass scale applied to all NBody objects handled by the Gravity Engine. Increasing mass scale 
    /// makes everything move faster for the same CPU cost.
    /// 
    /// In dimensionless units the massScale can be set directly in the inspector. In other units, the
    /// mass scale is determined by the choice of length and time scale ans computed by UpdateTimeScale
    /// in the GravityScalar class.
    /// </summary>
    public float massScale = 1.0f;

	/// <summary>
	/// The time scale.
	/// Time scale is used for overall scale at setup and is used via dimension analysis to set an overall
	/// mass scale to produce the required evolution. 
	///
	/// To change evolution speed at run time use SetTimeZoom(). This affectes the amount of physics calculations
	/// performed per frame. 
	///
	/// </summary>
	[SerializeField]
	private float _timeScale = 1.0f; 
	//! Orbital scale in Unity unity per AU
	public float timeScale {
		get { return _timeScale; }
		set { UpdateTimeScale(value);}
	}

	private float timeZoom = 1f; 
	private bool timeZoomChangePending; 
	private float newTimeZoom; 

	//! Array of game objects to be controlled by the engine at start (used when detectNbodies=false). During evolution use AddBody().
	public GameObject[] bodies; 

	//! Begin gravitational evolution when the scene starts.
	public bool evolveAtStart = true;  
	private bool evolve;

	//! State of inspector Advanced foldout
	public bool editorShowAdvanced; 
	//! State of inspector Scale foldout
	public bool editorShowScale;
    //! State of scale details
    public bool editorShowScaleDetails; 
	//! State of inspector Center of Mass foldout
	public bool editorCMfoldout; 
	//! Track state of foldout in editor
	public bool editorShowTrajectory;

    public bool editorShowStartTime;
    public int startTimeYear = 2020;
    public int startTimeMonth = 1;
    public int startTimeDay = 1;
    public double startTimeTimeOfDayUTC = 0.0;

    //! flag to record events for rewind 
    public bool rewindModeEnabled = false;

    private bool rewindActive = false;

	//--Integrator stuff--	

	//! Number of physics steps per frame for massive body evolution. For LEAPFROG will directly map to CPU
	//! use and accuracy. In the case of HERMITE, number of iterations per frame will vary depending on
	//! speeds of bodies. 
	public int stepsPerFrame = 8;
	//! Number of steps per frame for particle evolution. All particle evolution is via LEAPFROG, independent of the
	//! choice of algorithm for massive body evolution.
	public int particleStepsPerFrame = 2;

	// Sub-divide frame time into 8 steps for Leapfrog integration
    // @TODO: readonly
    private const double  PHYSICS_FPS = 50.0;
	public double engineDt = 1.0/(PHYSICS_FPS * 8);

	// run particles at a larger timestep to save CPU
	private double particle_dt;  
		
	// mass/position information for massive bodies
	// For performance reasons (http://jacksondunstan.com/articles/3058) need to manage arrays and
	// grow them dynamically when required. Adding to the fun, the integrator delegates have the same
	// issue and must stay aligned with the arrays here.
	//
	// Typically over-allocate the initial body count
	private int arraySize; 
	private const int GROW_SIZE = 500;

	//! current state of massive bodies
	private GravityState worldState;

    private float previousPhyLoopGameTime;
    private double physTimeError = 0.0;

    //! last world dT time
    private double lastWorldDt;

    //! Bit flag used in integrator code
    public const byte INACTIVE = 1;     // mass should be skipped in integration
                                        //! Bit flag used in integrator code
    public const byte FIXED_MOTION = 1 << 1;    // integrator should not update position/velocity but
                                                // will use mass to affect other object

    public const byte TRAJ_DATA = 1 << 2;       // Track trajectory data for the object                                        

    public const byte INACTIVE_OR_FIXED = GravityEngine.FIXED_MOTION | GravityEngine.INACTIVE;

    //! future state of the world (if trajectory prediction is enabled)
    private GravityState trajectoryState; 
	
	//! Objects to show future trajectories (if trajectory prediction is enabled)
	private Trajectory[] trajectories; 

	private List<GameObject> addedByScript;

    // Gravitational Body tracking
    private NBody[] gameNBodies; 
	
	// less than this consider a body massless
	private const double MASSLESS_LIMIT = 1E-6; 

	private bool isSetup = false; // flag to trigger setup on first evolution

    //! List to hold bodies to go off rails at end of update
    private List<NBody> offRailsDefered; 

	// Force delegate
	private IForceDelegate forceDelegate;

	// ---------inner classes-----------
	public class FixedBody {
        public NBody nbody;
		public IFixedOrbit fixedOrbit;
        // if it's OrbitUniversal/KeplerSequence keep a reference handy
        public OrbitUniversal orbitU;
        public KeplerSequence keplerSeq;
        // must evolve Kepler objects in order parent, child, gradchild etc. so that each update builds
        // on the updated parental positions. Record depth and insert accordingly. 
        public int kepler_depth; 
		
		public FixedBody(NBody nbody, IFixedOrbit fixedOrbit) {
			this.nbody = nbody; 
			this.fixedOrbit = fixedOrbit;
            orbitU = nbody.GetComponent<OrbitUniversal>();
            keplerSeq = nbody.GetComponent<KeplerSequence>();
            kepler_depth = OrbitUtils.CalcKeplerDepth(fixedOrbit);
        }
    }

	//! NBody type - used in integrator code. 
	public enum BodyType { MASSIVE, MASSLESS, FIXED };

	// Held by a NBody object. Hold reference to internal reference details. 
	public class EngineRef {
		public BodyType bodyType;
        public FixedBody fixedBody;
		public int index; 

        public EngineRef() {

        }

        public EngineRef(BodyType bodyType, int index) {
            this.bodyType = bodyType;
            this.index = index;
        }
	}

    public delegate void GEStart(); 

    protected class GEStartListEntry 
    {
        public int priority = 0;
        public GEStart callback;

        public GEStartListEntry(GEStart callback, int priority)
        {
            this.priority = priority;
            this.callback = callback;
        }
    }

    private int GEStartListComparer(GEStartListEntry e1, GEStartListEntry e2)
    {
		// return convention is +1, 0, -1 for ge, eq, lt
        return e1.priority - e2.priority; 
    }

    private List<GEStartListEntry> geStartList;

	// ---------main class-------------

	/// <summary>
	/// Static accessor that finds Instance. Useful for Editor scripts.
	/// </summary>
	public static GravityEngine Instance()
 	{
     	if (instance == null)
         	instance = (GravityEngine)FindObjectOfType(typeof(GravityEngine));
     	return instance;
    }

    void Awake() {
        if (instance == null) {
            instance = this;
        } else if (this != instance) {
            Debug.LogWarning("More than one GravityEngine in Scene");
        }
        DoAwake(); 
    }

    /// <summary>
    /// Unit test call in to wake up GE (since Awake is not directly callable due to protection level)
    /// </summary>
    public void UnitTestAwake() {
        DoAwake();
    }
		
	private void DoAwake () {

		ConfigureDT();
        geStartList = new List<GEStartListEntry>();
        // check for custom scene mapper
        if (mapToSceneGameObject != null) {
            mapToSceneDelegate = mapToSceneGameObject.GetComponent<GEMapToSceneInterface>();
            if (mapToSceneDelegate == null) {
                Debug.LogError("Map to scene game object does not implement GEMapToSceneInterface");
            }
        }
        mapToScene = useTransform || (mapToSceneDelegate != null);

        // check for multiplayer interface
        if (multiplayerInterfaceGO != null) {
            geMultiplayerIF = multiplayerInterfaceGO.GetComponent<GEMultiplayerInterface>();
            if (geMultiplayerIF == null) {
                Debug.LogError("Multiplayer interface object does not implement GEMapToSceneInterface");
            }
        }

        if (massScale == 0) {
			Debug.LogError("Cannot evolve with massScale = 0"); 
			return;
		}
		if (physToWorldFactor == 0) {
			Debug.LogError("Cannot evolve with physToWorldFactor = 0"); 
			return;
		}
		if (timeScale == 0) {
			Debug.LogError("Cannot evolve with timeScale = 0"); 
			return;
		}
		// force computation of massScale
		UpdateTimeScale(_timeScale);

		addedByScript = new List<GameObject>();
        offRailsDefered = new List<NBody>();

        // defensive init for early access to physical time at start (will get replaced)
        worldState = new GravityState(arraySize);
        SetAlgorithm(algorithm);
    }

    void Start() {
        AddConsoleCommands();
        evolve = evolveAtStart;
        // Prior to 7.0 this was done on first evolve frame. Can be commented out if issues found. (Please report to 
        // nbodyphysics@gmail.com)
        Setup();
	}

	/// <summary>
	/// Control evolution of masses and particles in the gravity engine. 
	/// </summary>
	/// <param name="evolve">If set to <c>true</c> evolve.</param>
	public void SetEvolve(bool evolve) {
        if (!isSetup)
            Debug.LogError("GE has not been setup. Cannot change evolve state");

        if (!this.evolve && evolve && isSetup) {
            // any objects added when we were paused need to be added now
            foreach(GameObject go in addedByScript) {
                SetupGameObjectAndChildren(go);
            }
            addedByScript.Clear();
        }
		this.evolve = evolve;       
	}

	public bool GetEvolve() {
		return evolve;
	}
	
	/// <summary>
	/// Sets the integration algorithm used for massive bodies. 
	///
	/// The integration algorithm cannot be changed while the engine is running. 
	/// </summary>
	/// <param name="algorithm">Algorithm.</param>
	public void SetAlgorithm(Algorithm algorithm) {
		if (evolve) {
			Debug.LogError("Cannot change algorithm while evolving");
			return;
		}
        forceDelegate = ForceChooser.InstantiateForce(force, this.gameObject);
        worldState.SetAlgorithmAndForce(algorithm, forceDelegate);

	}

    public IForceDelegate GetForceDelegate() {
        return forceDelegate;
    }

	/// <summary>
	/// Gets the name of the algorithm as a string.
	/// </summary>
	/// <returns>The algorithm name.</returns>
	/// <param name="algorithm">Algorithm.</param>
	public static string GetAlgorithmName(Algorithm algorithm) {
		return algorithmName[(int) algorithm];
	}

	/// <summary>
	/// Gets the particle time step size.
	/// </summary>
	/// <returns>The particle dt.</returns>
	public double GetParticleDt() {
		return particle_dt;
	}

	/// <summary>
	/// Reset the bodies/particle systems known to the Gravity Engine.
	/// </summary>
	public void Clear() {

		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG) {
			Debug.Log("Clearing " + worldState.numBodies + " bodies");
		}
        #pragma warning restore 162
        for (int i=0; i < worldState.numBodies; i++) {
			RemoveBody(gameNBodies[i].gameObject);
            gameNBodies[i] = null;
		}
        // be paranoid - clear all engine refs in scene
        NBody[] nbodies = (NBody[])Object.FindObjectsOfType(typeof(NBody));
        foreach( NBody n in nbodies) {
            n.engineRef = null;
        }
        worldState.Clear();
		isSetup = false;
		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG) {
			Debug.Log("All bodies cleared.");
		}
		#pragma warning restore 162
	}

	public void Setup() {
        // 确保 worldState 已初始化
        if (worldState == null)
        {
            // 初始化 worldState（使用一个临时大小，会在 InitArrays 中重新分配）
            worldState = new GravityState(GROW_SIZE);
            // 设置算法和力
            worldState.SetAlgorithmAndForce(algorithm, null);
        }
        
        worldState.numBodies = 0;
		massScale = (float) GravityScaler.UpdateMassScale(units, _timeScale, _lengthScale);
		GravityScaler.ScaleScene(units,  _lengthScale);

        offRailsDefered = new List<NBody>();

        // do not want to rewind the initial scene setup, turn off during initial object setup
        bool tempRewindEnable = rewindModeEnabled;
        rewindModeEnabled = false;
        if (detectNbodies) {
			SetupAutoDetect();
		} else {
			SetupExplicit();
		}
        rewindModeEnabled = tempRewindEnable;
		worldState.ResetPhysicalTime();
        worldState.PreEvolve(this);

        // update positions on screen
        UpdateGameObjects();

        ClearAllTrails();

        if (trajectoryPrediction) {
            ResetTrajectoryPrediction();
        }

        isSetup = true; // needs to be here so setup calls know we're ready
        // Run any registered startup code
        geStartList.Sort(GEStartListComparer); // ascending order
        foreach(GEStartListEntry geStart in geStartList) {
            geStart.callback();
        }
        geStartList.Clear();

		if (setCenterOfMass) {
            worldState.SetCenterOfMass(this, new Vector3d(cmPosition), new Vector3d(cmVelocity));
		}

#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG) {
			Debug.Log(string.Format("GravityEngine started with {0} nbody,  {1} particle systems. {2} fixed",
                            worldState.numBodies, worldState.gravityParticles.Count, 
                            worldState.fixedBodies.Count));
			LogDump();
		}
		#pragma warning restore 162
	}


    /// <summary>
    /// Scripts may wish to do some setup in the Start() method but GE is not yet running at scene start. 
    /// This is a method to register code to run once GE setup has been completed. 
    /// 
    /// If GE is already running, just go ahead and do the callback now. 
    /// </summary>
    /// <param name="callback"></param>
    public void AddGEStartCallback(GEStart callback) {
        if (isSetup) {
            callback();
        } else {
            AddGEStartCallback(callback, 0);
        }
    }

	/// <summary>
	/// Add code to be run once GE has started with an expicit priority.
	/// Lower priority methods are run first. 
    /// 
    /// If GE is running run the callback immediatly. 
	/// </summary>
	/// <param name="callback"></param>
	/// <param name="priority">lower runs first</param>
    public void AddGEStartCallback(GEStart callback, int priority)
    {
        if (isSetup) {
            callback();
        } else {
            GEStartListEntry ges = new GEStartListEntry(callback, priority);
            geStartList.Add(ges);
        }
    }

    private void ClearAllTrails() {
        NBody[] nbodies = (NBody[])Object.FindObjectsOfType(typeof(NBody));
        foreach (NBody nb in nbodies) {
            TrailRenderer[] trails = nb.gameObject.GetComponentsInChildren<TrailRenderer>();
            foreach (TrailRenderer trail in trails) {
                trail.Clear();
            }
        }
    }

    /// <summary>
    /// Setup only the bodies (and their children) that have been explicitly added to the NBody engine
    /// via the bodies list. There can be bodies already added programatically via AddBody() as well, these
    /// are on the addedByScript list.
    /// </summary>
    private void SetupExplicit() {
		int maxBodies = 0; 
		// two passes - first get a count
		if (bodies != null) {
			foreach (GameObject body in bodies) {
				maxBodies += body.GetComponentsInChildren<NBody>().Length;
			}
		}
		if (addedByScript.Count > 0) {
			foreach (GameObject body in addedByScript) {
				maxBodies += body.GetComponentsInChildren<NBody>().Length;
			}
		}
		InitArrays(maxBodies+GROW_SIZE);
		// Now do setup on each body
		if (bodies != null) {
			foreach (GameObject body in bodies) {
				SetupGameObjectAndChildren(body);
			}
		}
		if (addedByScript.Count > 0) {
			foreach (GameObject body in addedByScript) {
				SetupGameObjectAndChildren(body);
			}
			addedByScript.Clear();
		}
	}

	/// <summary>
	/// Find all active NBody objects in the scene and add them to the engine. 
	/// </summary>
	private void SetupAutoDetect() {

        if (addedByScript.Count > 0)
            Debug.LogWarning("Using auto-detect but some bodies added by script before starting. ");

        // Need to deterimine maxBodies from body lists
        // GetComponentsInChildren also returns components in parent object!
        int maxBodies = 0; 
        // objects added in the inspector
        if (bodies != null) {
               foreach (GameObject body in bodies) {
                       maxBodies += body.GetComponentsInChildren<NBody>().Length;
               }
        }
                       
		NBody[] nbodies = (NBody[]) Object.FindObjectsOfType(typeof(NBody));
		// allocate physics arrays (will over-allocate by number of massless bodies if optimizing massless)
		// add some buffer to allow for dynamic additions
		maxBodies += nbodies.Length;
        InitArrays(maxBodies+GROW_SIZE);

        if (nbodies.Length == 0) {
            Debug.Log("No NBodies in scene at start");
            return;
        }

        // add in order of orbit depth to ensure e.g. moons can get positions and velocities from planets, 
        // planets from stars etc.
        foreach (NBody nbody in nbodies) {
            nbody.CalcOrbitDepth();
        }
        System.Array.Sort(nbodies, 0, nbodies.Length, nbodies[0]);
        foreach (NBody nbody in nbodies) {
			SetupOneGameObject(nbody.gameObject, nbody);
		}
	}

	private void InitArrays(int size) {
		// Typically over-allocate to allow for dynamic additions EXCEPT for the AZT integrator which can only
		// handle three bodies
		arraySize = size;
		worldState.InitArrays(arraySize);
		trajectories = new Trajectory[arraySize];
        gameNBodies = new NBody[arraySize];
		// integrator will allocate internal data and set dt
		worldState.integrator.Setup(arraySize, engineDt);
	}

	// Grow arrays to hold new massive bodies and trigger the same operation in the 
	// integrator to maintain array alignment. 
	//
	// Not ideal - but scientific computing is array based and direct arrays have the best performance. 
	//
	private bool GrowArrays(int growBy) {

        worldState.GrowArrays(growBy);  // Also grows integrator arrays

		Trajectory[] traj_copy = new Trajectory[arraySize];
		NBody[] gameNBodies_copy = new NBody[arraySize];

		for (int i=0; i < arraySize; i++) {
			traj_copy[i] = trajectories[i];
            gameNBodies_copy[i] = gameNBodies[i];
		}

		trajectories = new Trajectory[arraySize+growBy];
        gameNBodies = new NBody[arraySize+growBy];

		for (int i=0; i < arraySize; i++) {
			trajectories[i] = traj_copy[i];
            gameNBodies[i] = gameNBodies_copy[i];
		}
		arraySize += growBy;
		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG)
			Debug.Log("GrowArrays by " + growBy);
		#pragma warning restore 162		
		return true;
	}

	
	
	private int logCounter; 

	public bool IsSetup() {
		return isSetup;
	}

    //******************************************
    // Map position to scene position based on the GE transform
    // This allows the location, orientation and visual scale of the entire
    // system to be adjusted by the GE transform. 
    //
    // Anything that is placed in the GE scene from physics calculations (e.g. orbit paths etc.)
    // needs to go through this method. 
    //******************************************

    public Vector3 MapToScene(Vector3 pos) {
        if (mapToSceneDelegate != null)
            return mapToSceneDelegate.MapToScene(pos);

        if (!mapToScene)
            return pos;

        // someone will think of a weird reason to scale x/y/z differently
        Vector3 scaledPos = new Vector3(transform.localScale.x * pos.x,
                                    transform.localScale.y * pos.y,
                                    transform.localScale.z * pos.z); 
        return transform.rotation * scaledPos + transform.position;
    }

    public Vector3 UnmapFromScene(Vector3 pos) {
        if (mapToSceneDelegate != null)
            return mapToSceneDelegate.UnmapFromScene(pos);

        if (!mapToScene)
            return pos;

        // someone will think of a weird reason to scale x/y/z differently
        Vector3 scaledPos = new Vector3( pos.x/ transform.localScale.x ,
                                    pos.y/transform.localScale.y,
                                    pos.z/transform.localScale.z);
        return Quaternion.Inverse(transform.rotation) * scaledPos - transform.position;

    }

    /// <summary>
    /// Return a clone of the current world state. 
    /// 
    /// This can then be independently evolved as part of e.g. course correction determination
    /// 
    /// </summary>
    /// <returns></returns>
    public GravityState GetGravityStateCopy() {
        return new GravityState(worldState);
    }


    /*******************************************
	* Trajectory Prediction
	* TP prediction is based on maintaining a parallel integrator and 
	* masslessEngine and running them ahead in time. 
	* Trajectory objects attached are given the updated position information 
	* so the future path can be displayed. 
	*
	* If the inputs to the system change (velocity change, body added) then the 
	* system needs to be reset and run forward from the current state again. 
	/*******************************************/

    private bool trajectoryRestart; 

	public void TrajectoryRestart() {
        if (trajectoryPrediction) {
            trajectoryRestart = true;
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("Trajectory restart");
#pragma warning restore 162
        }
    }

	/// <summary>
	/// Sets the trajectory prediction state (enable/disable). 
	/// On enable, will activate the Trajectory elements and re-run the trajectory prediction code.
	/// On disable will de-activate all trajectory elements. 
	/// </summary>
	/// <param name="newState">If set to <c>true</c> new state.</param>
	public void SetTrajectoryPrediction(bool newState) {
		if (newState != trajectoryPrediction) {
			if (newState) {
				// do a restart sync-ed with FixedUpdate
				trajectoryRestart = true;
				// set all trajectories active
				for (int i=0; i < worldState.numBodies; i++) {
					if ((trajectories[i] != null) && worldState.NbodyIndexIsActive(i)) {
						trajectories[i].gameObject.SetActive(true);
					}
				}
                trajectoryPrediction = true;
            } else {
				// hide all trajectories and remove all time/text markers
				for (int i=0; i < worldState.numBodies; i++) {
					if ((trajectories[i] != null) && worldState.NbodyIndexIsActive(i)) {
						trajectories[i].Cleanup();
						trajectories[i].gameObject.SetActive(false);
					}
				}
                trajectoryPrediction = false;
                trajectoryRestart = false;
            }
        }
	}

	private void ResetTrajectoryPrediction() {

		m_prevTrajectoriesUpdateTime = double.MinValue;
		// trajectory state starts as a clone of current world state
		trajectoryState = new GravityState(worldState);
        trajectoryState.hasTrajectories = true;

        // @Awkward: hold trajectories in GS?
        for (int i = 0; i < worldState.numBodies; i++) {
            if ((trajectories[i] != null) && worldState.NbodyIndexIsActive(i)) {
                trajectories[i].Init((float) worldState.time);
            }
        }

		trajectoryRestart = false;
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Reset trajectory prediction");
#pragma warning restore 162
    }

    /// <summary>
    /// If any NBodies have Trajectory components then update them with new projected position/times
    /// 
    /// Internal use only (called from GravityState during evolution when trajectories are present)
    /// </summary>
    public void UpdateTrajectories()
    {
      bool canRecord = false;
      if( trajectoryState.time - m_prevTrajectoriesUpdateTime >= trajectoryDataTimeDistance )
      {
        canRecord = true;
        m_prevTrajectoriesUpdateTime = trajectoryState.time;
      }
        GravityState.NbodyState[] trajBodies = trajectoryState.GetNbodyStates();
      for( int i = 0; i < worldState.numBodies; i++ )
      {
        if( ( trajectories[i] != null ) && worldState.NbodyIndexIsActive( i ) )
        {
				Vector3 position = new Vector3((float)trajBodies[i].r_x, (float)trajBodies[i].r_y, (float)trajBodies[i].r_z); 
				position = physToWorldFactor * position;
				trajectories[i].AddPoint(position, (float)trajectoryState.time, (float) worldState.time);
				// update trajectory data if enabled (used for intercept detection)
          if( trajectories[i].recordData && canRecord )
          {
                    // Want scaled velocity
                    Vector3 velocity = new Vector3((float)trajBodies[i].v_x, (float)trajBodies[i].v_y, (float)trajBodies[i].v_z);
                    trajectories[i].AddData(position, velocity, (float)trajectoryState.time);
				}
			}
		}

	}

    private double m_prevTrajectoriesUpdateTime = float.MinValue;
    private bool trajectoryUpToDate = false;

    /// <summary>
    /// Evolves the trajectory to the specified time or advances the trajectory by the fraction constrained
    /// by the trajectoryComputeFactor. This limits the number of trajectory integrations in a given fixed update
    /// to reduce the frame rate impact of frequent trajectory updates. 
    /// </summary>
    private void EvolveTrajectory(double gameDt) {

        // determine delta time to evolve and invoke common Evolve routine
        double timeInterval = (worldState.time + trajectoryTime) - trajectoryState.time;
        // if we're catching up, then reduce amount of work we do on this step
        trajectoryUpToDate = true;
        if (timeInterval > gameDt) {
            timeInterval = Mathd.Min(trajectoryComputeFactor * gameDt, trajectoryTime);
            trajectoryUpToDate = false;
        }
        trajectoryState.Evolve(this, timeInterval);
    }

    public bool TrajectoryUpToDate() {
        return trajectoryUpToDate;
    }

    /// <summary>
    /// Utility function used by GEConsole to advance the scene by one GE FixedUpdate. 
    /// If single step mode is not active, this call will activate it.
    /// </summary>
    public void EvolveOneFixedUpdate() {
        singleStep = true;
        stepHasRun = false; 
    }

    /// <summary>
    /// Main physics evolution entry point. 
    /// </summary>
    /// 
    void FixedUpdate()
    {
        if (updateMode != UpdateMode.UPDATE) {
            PhysicsLoop(); 
        }
    }

    void PhysicsLoop () {

        if (rewindActive)
        {
            PhysicsLoopReverse();
            return;
        }

        float startTime;
        double startWorldTime;
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG) {
            startTime = Time.realtimeSinceStartup;
            startWorldTime = worldState.time;
        }
#pragma warning restore 162       // disable unreachable code warning

        // MUST do this before we move stuff around, since the (r,v) has been
        // set. Otherwise get an offset.
        if (offRailsDefered.Count > 0)
        {
            foreach (NBody nbody in offRailsDefered)
            {
                // a massless fixed body ended up on the massive list. Leave it there. 
                worldState.RemoveFixedBody(nbody);
                nbody.engineRef.fixedBody = null;
                nbody.engineRef.bodyType = BodyType.MASSIVE;
            }
            offRailsDefered.Clear();
            worldState.UpdateOnRails();
        }
        // Time alignment. There is a desired physics DT based on game time elapsed (scaled by timeZoom). 
        // The actual amount evolved may overshoot slightly since there are internal DTs in the physics code. 
        // track this error and adjust call to call. 
        //
        // physTimeError records the amount the engine overshoots the requested physicsDeltaTime
        float deltaTime = Time.timeSinceLevelLoad - previousPhyLoopGameTime;
        double physicsDeltaTime = deltaTime * timeZoom - physTimeError; 
        
        // 检查worldState是否已初始化，如果未初始化则先调用Setup
        if (worldState == null || !isSetup) {
            Setup();
            isSetup = true;
        }
        
        // 再次检查worldState（防止Setup失败）
        if (worldState == null) {
            return; // worldState仍未初始化，跳过本次更新
        }
        
        double currentPhysTime = worldState.GetPhysicsTime();
        if (evolve) {

            // support for GEconsole single step mode
            if (singleStep && stepHasRun) {
                return;
            }
            stepHasRun = true;

            if (trajectoryPrediction && trajectoryRestart) {
                ResetTrajectoryPrediction();
            }

            EvolveByTimestep(physicsDeltaTime);
            physTimeError = (worldState.GetPhysicsTime() - currentPhysTime) - physicsDeltaTime;

            lastWorldDt = physicsDeltaTime;
            if (updateMode != UpdateMode.FIXED_INTERPOLATE) {
                // update positions on screen
                UpdateGameObjects();
            }

        } else if (isSetup && trajectoryPrediction) {
            if (trajectoryRestart) {
                ResetTrajectoryPrediction();
            }
            // run trajectory evolution always (if setup). May be paused and looking at vel. changes
            EvolveTrajectory(physicsDeltaTime);
        } else if (isSetup) {
            // changes when paused need to get copied into worldState r, v arrays
            worldState.MoveFixedBodies(currentPhysTime);
            // always update positions when paused. Allows e.g. OrbitPoints to change when paused.
            UpdateGameObjects();
        }
        previousPhyLoopGameTime = Time.timeSinceLevelLoad;
		// Fixes bug where pending timescale change doesn't get applied when not evolving
        // if there is a timescale change pending, apply it
        if( timeZoomChangePending )
        {
          timeZoom = newTimeZoom;
          timeZoomChangePending = false;
        }

#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG) {
            if (Mathd.Abs(physTimeError) > 10.0) {
                Debug.LogError("physTimeError issue!" + 
                    string.Format(" phyTimeError={0} deltaTime={1} phyDelta={2}", physTimeError, deltaTime, physicsDeltaTime ));
            }
            float runtime = Time.realtimeSinceStartup - startTime;
            if (runtime > Time.fixedDeltaTime) {
                Debug.LogWarningFormat("GE physics overrun. Used {0} (delta time={1}) from worldTime={2} => {3}", 
                    runtime, Time.fixedDeltaTime, startWorldTime, worldState.time);
            }
        }
#pragma warning restore 162       // re-enable unreachable code warning

    }

    /// <summary>
	/// Run the physics loop with time direction reversed to do a rewind.
	///
	/// This is intended as a "physics backwards" feature and NOT a full game rewind. It will
	/// - make the physical time run backwards at the timeScale indicated up to 0 time
	/// - unapply any maneuvers that were performed
	/// - unapply and impulses that were applied
	///
	/// It will NOT account for:
	/// - removal of objects added earlier
	/// - trajectory prediction
	/// - changes of objects from on-rails to off-rails
	/// - position and velocity sets by scripts
	/// </summary>
    void PhysicsLoopReverse()
    {

        float startTime;
        double startWorldTime;
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG)
        {
            startTime = Time.realtimeSinceStartup;
            startWorldTime = worldState.time;
        }
#pragma warning restore 162       // disable unreachable code warning

        // Time alignment. There is a desired physics DT based on game time elapsed (scaled by timeZoom). 
        // The actual amount evolved may overshoot slightly since there are internal DTs in the physics code. 
        // track this error and adjust call to call. 
        //
        // physTimeError records the amount the engine overshoots the requested physicsDeltaTime
        float deltaTime = Time.timeSinceLevelLoad - previousPhyLoopGameTime;
        double physicsDeltaTime = deltaTime * timeZoom + physTimeError;
        double currentPhysTime = worldState.GetPhysicsTime();
        if (evolve)
        {
            if ((currentPhysTime - physicsDeltaTime) < engineDt)
            {
                evolve = false;
#pragma warning disable 162       // disable unreachable code warning
                if (DEBUG)
                    Debug.LogWarning("Reached start. Time reversed evolution paused. evolve=false");
#pragma warning restore 162       // re-enable unreachable code warning
                return;
            }

            // support for GEconsole single step mode
            if (singleStep && stepHasRun)
            {
                return;
            }
            stepHasRun = true;

            worldState.EvolveReversed(this, physicsDeltaTime);
            physTimeError = physicsDeltaTime - (currentPhysTime - worldState.GetPhysicsTime()) ;

            lastWorldDt = physicsDeltaTime;
            if (updateMode != UpdateMode.FIXED_INTERPOLATE)
            {
                // update positions on screen
                UpdateGameObjects();
            }

        }

        else if (isSetup)
        {   // Fixed body changes when paused need chance to update r, v cached values
            worldState.MoveFixedBodies(currentPhysTime);
            // always update positions when paused. Allows e.g. OrbitPoints to change when paused.
            UpdateGameObjects();
        }
        previousPhyLoopGameTime = Time.timeSinceLevelLoad;

#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG)
        {
            if (Mathd.Abs(physTimeError) > 10.0)
            {
                Debug.LogError("physTimeError issue!" +
                    string.Format(" phyTimeError={0} deltaTime={1} phyDelta={2}", physTimeError, deltaTime, physicsDeltaTime));
            }
            float runtime = Time.realtimeSinceStartup - startTime;
            if (runtime > Time.fixedDeltaTime)
            {
                Debug.LogWarningFormat("GE physics overrun. Used {0} (delta time={1}) from worldTime={2} => {3}",
                    runtime, Time.fixedDeltaTime, startWorldTime, worldState.time);
            }
        }
#pragma warning restore 162       // re-enable unreachable code warning

    }

    /// <summary>
    /// Evolve the physics to the specified time. 
    /// </summary>
    /// <param name="timestep"></param>
    private void EvolveByTimestep(double timestep) {

        // if evolution triggers a maneuver, then will need to restart trajectory prediction with new velocities etc.
        trajectoryRestart = worldState.Evolve(this, timestep);

        // no point in evolving if we're restarting trajectories
        if (trajectoryPrediction && !trajectoryRestart) {
            EvolveTrajectory(timestep);
        }

        // if there is a timescale change pending, apply it
        if (timeZoomChangePending) {
            timeZoom = newTimeZoom;
            timeZoomChangePending = false;
        }
    }

    private void Update()
    {
        orbitPredictorCounter++;
        if (orbitPredictorCounter >= orbitPredictorInterval)
            orbitPredictorCounter = 0; 

        if (updateMode == UpdateMode.UPDATE) {
            PhysicsLoop();
        } else if (updateMode == UpdateMode.FIXED_INTERPOLATE) {
            // do interpolating game object update
            UpdateGameObjects();
        }
    }

    public int GetOrbitPredictorCounter()
    {
        return orbitPredictorCounter;
    }

    /// <summary>
    /// Gets the physical scale.
    /// </summary>
    /// <returns>The physical scale.</returns>
    public float GetPhysicalScale() {
		return physToWorldFactor;
	}

	/// <summary>
	/// Return the length scale that maps lengths in the specified unit system to 
	/// Unity game length units (e.g. Unity units per meter, Unity units per AU)
	/// </summary>
	/// <returns>The length scale.</returns>
	public float GetLengthScale() {
		return lengthScale;
	}

    /// <summary>
    /// If all objects in the control of GE are "on-rails" (FixedObject, OrbitUniversal in Kepler mode)
    /// then the system is decalred "on-rails".
    /// </summary>
    /// <returns></returns>
    public bool IsOnRails()
    {
        return worldState.IsOnRails();
    }

	/// <summary>
	/// Changes the timescale during runtime
	/// </summary>
	/// <param name="value">Value.</param>
	public void SetTimeZoom(float value) {
		newTimeZoom = value;
		timeZoomChangePending = true;
	}

    public float TimeZoom()
    {
        return timeZoom;
    }

    /// <summary>
    /// Fast forward by the specified time. 
    /// 
    /// This can involve *significant* computation and is very 
    /// likely to cause a frame rate stall! 
    /// 
    /// </summary>
    /// <param name="time"></param>
    public void FastForward(float time) {
        EvolveByTimestep( time);
        if( timeZoomChangePending )
        {
          timeZoom = newTimeZoom;
          timeZoomChangePending = false;
        }
    }

    /// <summary>
    /// Set the current physical time and update all objects to be at this time. 
    /// 
    /// ONLY VALID if all objects are "On-Rails" i.e. in Kepler mode. (GravityState will issue
    /// a warning and leave time unchanged if this is not true).
    /// 
    /// Depending on the history of events it can be possible to set to an earlier time. This
    /// depends on whether any Kepler objects have done ApplyImpulse or SOI changes that have 
    /// updated their time0 reference. If this *has* happened, then unspecified Kepler evolution will
    /// result. User code must limit earliest time based on knowledge of when impules were applied.
    /// 
    /// </summary>
    public void SetPhysicalTime(double newTime, bool force = false) {
        worldState.SetTime(newTime, force);
        if (trajectoryState != null) {
            TrajectoryRestart();
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("Trajectory restart");
#pragma warning restore 162        
        }
        UpdateGameObjects();
    }

    /// <summary>
    /// Evolve all bodies to the new time (past or future). This will trigger a 
    /// integration sequence when there is at least one body off rails and this
    /// may cause a burst of CPU activity if the time delta is large.
    /// 
    /// Generally called through the GE wrapper for game logic. 
    /// </summary>
    /// <param name="newTime"></param>
    public void EvolveToTime(double newTime)
    {
        Debug.Log("Evolve to time");
        worldState.EvolveToTime(newTime);
    }

    /// <summary>
    /// Get the current time zoom factor (run-time scaling of physics time execution). 
    /// 
    /// Note that the baseline timescale is set by timeScale (based on units selected) when 
    /// the Gravity Engine initializes. 
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetTimeZoom() {
        if (timeZoomChangePending)
            return newTimeZoom;
        return timeZoom;
    }

	/// <summary>
	/// Gets the physical time. Physical time may differ from game time by the timescale factor.
	/// </summary>
	/// <returns>The physical time.</returns>
	public float GetPhysicalTime() {
        // DrawGizmos can use this before GE has inited
        if (worldState == null)
            return 0;
        return (float)worldState.GetPhysicsTime();
	}

    /// <summary>
    /// Determine the physics time required for interpolation when in FIXED_INTERPOLATE mode
    /// </summary>
    /// <returns></returns>
    public float GetPhysTimeForInterpolation()
    {
        float iTime = -(float)physTimeError;
        if (updateMode == UpdateMode.FIXED_INTERPOLATE) {
            iTime += (Time.timeSinceLevelLoad - previousPhyLoopGameTime) * timeZoom;
        }
        return iTime;
    }

    /// <summary>
    /// Gets the physical time. Physical time may differ from game time by the timescale factor.
    /// </summary>
    /// <returns>The physical time.</returns>
    public double GetPhysicalTimeDouble() {
        if (worldState == null)
            return 0;
        return worldState.GetPhysicsTime();
    }

    public double GetTimeWorldSeconds() {
        if (worldState == null)
            return 0;
        return GravityScaler.GetWorldTimeSeconds(worldState.GetPhysicsTime());
    }

    /// <summary>
    /// Provide the specified start time in the GE inspector as a Julian day. 
    /// </summary>
    /// <returns></returns>
    public double GetStartTimeAsJD()
    {
        return SolarUtils.JulianDate(startTimeYear, startTimeMonth, startTimeDay, startTimeTimeOfDayUTC);
    }

    public double GetTimeAsJulianDate(double time)
    {
        if (units != GravityScaler.Units.ORBITAL) {
            Debug.LogError("Cannot compute JD");
            return 0.0;
        }
        double solarTime = SolarUtils.JulianDate(startTimeYear, startTimeMonth, startTimeDay, startTimeTimeOfDayUTC);
        return solarTime + GravityScaler.GetWorldTimeSeconds(time) / SolarUtils.SEC_PER_DAY;
    }

    private void SetupGameObjectAndChildren(GameObject gameObject) {

		NBody[] nbodies = gameObject.GetComponentsInChildren<NBody>();
        // Add in order of orbitDepth
        foreach (NBody nbody in nbodies) {
            nbody.CalcOrbitDepth();
        }
        System.Array.Sort(nbodies, 0, nbodies.Length, nbodies[0]);

        foreach (NBody nbody in nbodies) {
			SetupOneGameObject(nbody.transform.gameObject, nbody);
		}
	}

    // Adds one game object
	// Calls:
	//      nbody.InitPosition()
	//      - this checks if there is an INbodyInit impementation attached (e.g. an OrbitUniversal) and delegates to that
    // - massless bodies go to the masslessEngine (if "optimize massless" is turned on)
    // - fixed bodies:
    //     * massive: 
    //          add to the integrator and r[] (so their mass can effect others)
    //          add to the fixedBodies list
    //     * massless: also added to integrator & fixedBodies list [integrator not necessary - need to fix]
    //
	private void SetupOneGameObject(GameObject go, NBody nbody) {

        if (nbody.engineRef != null) {
            Debug.LogError("Duplicte add: already have an engine ref for " + go.name);
            return;
        }

        // this engine ref is only used for massive bodies (MBE will create one of it's own) ICK!
        EngineRef engineRef = new EngineRef();

        // If there is a KeplerSequence - it wins (It wraps the OrbitUniversal that sits beside it)
        IFixedOrbit fixedOrbit = null;
        fixedOrbit = (IFixedOrbit) go.GetComponent<KeplerSequence>();
        if (fixedOrbit == null) {
            fixedOrbit = go.GetComponent<IFixedOrbit>();
        }
        bool fixedObject = (fixedOrbit != null);

        // grow arrays if needed
        if (worldState.numBodies + 1 > arraySize) {
            if (!GrowArrays(GROW_SIZE)) {
                // Grow arrays has logged the error
                return;
            }
        }

        // apply e.g. orbit positioning if required (heirarchically) and update initialPhyPos
        nbody.InitPosition(this);

        // Use the standard (massive) engine and the configured integrator

        // traj will be attached to a child of NBody object - record them all
        // Note: if dynamically enable/disable trajectory prediction with scripting all will be affected. 
        // Could choose to bias this to initially active ones in this loop if that was preferred.
        for (int childNum=0; childNum < nbody.transform.childCount; childNum++) {
			Trajectory trajectory = nbody.transform.GetChild(childNum).GetComponent<Trajectory>(); 
			if (trajectory != null) {
				trajectories[worldState.numBodies] = trajectory;
				break;
			}
		}

        engineRef.index = worldState.numBodies;
        gameNBodies[engineRef.index] = nbody;
        if (!fixedObject) {
            engineRef.bodyType = BodyType.MASSIVE;
        }
        nbody.engineRef = engineRef;

        // FixedObject and Ellipses in Kepler mode are fixed, their mass affects others but they move
        // (or not) under control of the IFixedOrbit component.
        if (fixedOrbit != null && fixedOrbit.IsOnRails()) {
            // Fixed objects are ALSO added to the list of massive objects below so that their gravity can affect others
            FixedBody fixedBody = new FixedBody(nbody, fixedOrbit);
            worldState.AddFixedBody(this, engineRef.index, fixedBody, fixedOrbit);
            engineRef.fixedBody = fixedBody;
            engineRef.bodyType = BodyType.FIXED;
        }
        // AFTER AddFixed for onrails check
        worldState.AddNBody(nbody, massScale, physToWorldFactor, isFixed: (engineRef.bodyType == BodyType.FIXED) );

        // update the NBody transform so e.g. trail renderers can be enabled immediatly
        nbody.GEUpdate(nbody.initialPhysPosition, nbody.vel_phys, this);
		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG) {
			int i = worldState.numBodies - 1;
            if (nbody.initWithDouble) {
                Debug.Log(string.Format("GE add massive: {0} as {1} r=[{2}] v=[{3} {4} {5}] |v|={6} index={7} mass={8}",
                    go.name, i, worldState.GetPhysicsPositionDouble(nbody),
                    nbody.vel_physV3.x, nbody.vel_physV3.y, nbody.vel_physV3.z,
                    nbody.vel_physV3.magnitude,
                    nbody.engineRef.index,
                    worldState.GetMass(nbody)));
            } else {
                Debug.Log(string.Format("GE add massive: {0} as {1} r=[{2}] v=[{3} {4} {5}] |v|={6} index={7} mass={8}",
                    go.name, i, worldState.GetPhysicsPosition(nbody),
                    nbody.vel_phys.x, nbody.vel_phys.y, nbody.vel_phys.z,
                    nbody.vel_phys.magnitude,
                    nbody.engineRef.index,
                    worldState.GetMass(nbody)));
            }
		}
		#pragma warning restore 162		// enable unreachable code warning
        if (geMultiplayerIF != null) {
            geMultiplayerIF.AddedBody(go, nbody);
        }
        
	}

    /// <summary>
    /// Adds the game object and it's children to GravityEngine. The engine will then handle position updates for the 
    /// body based on the gravitational force of all other bodies controlled by the engine. 
    ///
    /// If the GravityEngine is set to auto-detect bodies, all game objects present in the scene with a NBody
    /// component will be added once the GravityEngine is set to evolve. If auto-detect is not enabled bodies are
    /// added by calling this method. 
    ///
    /// A gameObject added to the engine must have an NBody script attached. The NBody script specifies the
    /// mass and initial velocity of the object. 
    ///
    /// The add method will traverse the children of the added gameObject and add any that have NBody components.  
    ///
    /// Optionally, a body may also have a fixed motion script (e.g. FixedEllipticalOrbit) or a script that
    /// set the initial position and velocity based on orbit parameters (e.g. EllipticalStart)
    /// </summary>
    /// <param name="go">Game object.</param>
    public void AddBody(GameObject go) {

		if (isSetup) {
			NBody nbody = go.GetComponent<NBody>(); 
			if (nbody == null) {
				Debug.LogError("No NBody found on " + go.name);
				return;
			}
			GravityScaler.ScaleNBody(nbody, units, lengthScale);
			SetupGameObjectAndChildren(go);
		} else {
			addedByScript.Add(go);
		}
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
		if (setCenterOfMass) {
            worldState.SetCenterOfMass(this, new Vector3d(cmPosition), new Vector3d(cmVelocity));
		}

	}

    /// <summary>
    /// Remove game object from GE.
    /// 
    /// Note: In a large-N simulation the shuffle down may cause a real-time hit. In those cases, 
    /// marking the body inactive with InactivateGameObject will exclude it from physics calculations
    /// without the shuffle overhead.
    /// </summary>
    /// <param name="toRemove">Game object to remove (must have a NBody component)</param>
    /// <param name="defer">Do at end of physics loop. Used when e.g. in a callback from physics code</param>
    /// 
    public void RemoveBody(GameObject toRemove) {

#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG)
        {
            Debug.Log("Remove " + toRemove.name);
        }
#pragma warning restore 162        // enable unreachable code warning
        NBody nbody = toRemove.GetComponent<NBody>();
        if (nbody == null ) {
			Debug.LogWarning("object to remove has no NBody " + toRemove.name);
			return;
		}
        if ( nbody.engineRef == null) {
            Debug.LogWarning("object to remove has not been added: " + toRemove.name);
            return;
        }
        if (nbody.engineRef.fixedBody != null) {
            worldState.RemoveFixedBody(nbody);
		}

        // shuffle down the gameObjects array
        for (int j = nbody.engineRef.index; j < (worldState.numBodies - 1); j++) {
            gameNBodies[j] = gameNBodies[j + 1];
            NBody nextNBody = gameNBodies[j];
            nextNBody.engineRef.index = j;
        }
        gameNBodies[worldState.numBodies - 1] = null;
        worldState.RemoveNBody(nbody);

        worldState.UpdateOnRails();
        if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
        if (setCenterOfMass) {
            worldState.SetCenterOfMass(this, new Vector3d(cmPosition), new Vector3d(cmVelocity));
        }

        nbody.engineRef = null;

        if (geMultiplayerIF != null) {
            geMultiplayerIF.RemovedBody(toRemove);
        }
	}

    public (int numBodies, NBody[] nbodies) GetNBodies()
    {
        return (worldState.numBodies, gameNBodies);
    }

	/// <summary>
	/// Inactivates the body in the GravityEngine. 
	///
	/// Mark the object as inactive. It will not affect other bodies/particles in the simulation. 
	///
	/// This can be a better choice than removing since a removal may impact real-time performance. 
	///
	/// This does not affect the activity state of the GameObject, only it's involvement in the GravityEngine
	/// 
	/// </summary>
	/// <returns>The game object.</returns>
	/// <param name="toInactivate">Game object to inactivate.</param>
	public void InactivateBody(GameObject toInactivate) {
		NBody nbody = toInactivate.GetComponent<NBody>(); 
		if (nbody == null) {
			Debug.LogWarning("Not an NBody - cannot remove"); 
			return;
		}

        worldState.NbodySetIsActive( nbody, false);
        if (trajectoryState != null) {
            trajectoryState.NbodySetIsActive(nbody, false);
        }
#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG)
            Debug.Log("Inactivate body " + toInactivate.name);
#pragma warning restore 162       // enable unreachable code warning
        // if massless, no change on trajectories
        if (trajectoryPrediction && !(nbody.engineRef.bodyType == BodyType.MASSLESS)) {
			trajectoryRestart = true;
		}
	}

    public void SetNoUpdateFlag(GameObject go, bool flag)
    {
        NBody nbody = go.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogWarning("Not an NBody - cannot remove");
            return;
        }
        worldState.NbodySetNoUpdateFlag(nbody, flag);
    }

    public bool GetNoUpdateFlag(GameObject go)
    {
        NBody nbody = go.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogWarning("Not an NBody - cannot remove");
            return false;
        }
        return worldState.NbodyGetNoUpdateFlag(nbody);
    }

	/// <summary>
	/// Re-activates an inactive body.
	/// </summary>
	/// <param name="toInactivate">To inactivate.</param>
	/// Code from John Burns
	public void ActivateBody(GameObject activate) {
		NBody nbody = activate.GetComponent<NBody>(); 
		if (nbody == null) {
			Debug.LogWarning("Not an NBody - cannot remove"); 
			return;
		}

		int i = nbody.engineRef.index;
        worldState.NbodySetIsActive( nbody, true);
        if (trajectoryState != null) {
            trajectoryState.NbodySetIsActive(nbody, true);
        }
#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG)
			Debug.Log("Activate body " + activate.name);
			#pragma warning restore 162		// enable unreachable code warning
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
	}

    public bool IsActive(NBody nbody)
    {
        if (nbody.engineRef == null) {
            return false;
        }
        return worldState.NbodyIsActive(nbody);
    }

    /// <summary>
    /// Recompute and update the Kepler depth of the specified NBody. 
    /// 
    /// Used when a Kepler sequence changes OrbitU segements. 
    /// </summary>
        public void UpdateKeplerDepth(NBody nbody, OrbitUniversal orbitU) {
        worldState.UpdateKeplerDepth(nbody, orbitU);
    }

    /// <summary>
    /// Change a body from On-rails to off. This may be called from a KeplerSeqeunce evolved and thus
    /// within the MoveFixedBodies code in GravityState. As a result it cannot make changes to the
    /// fixed bodies list at this time. 
    /// 
    /// (When a KeplerSequence goes off rails it finds the r,v from the previous segment. Hence the need for r,v.)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    public void BodyOffRails(NBody nbody, Vector3d pos, Vector3d vel ) {
        if (nbody.engineRef.bodyType != BodyType.FIXED)
            return;
        // flip type immediatly to avoid a KS update in UpdateGameObjects() and set position in
        // integrator. 
        nbody.engineRef.bodyType = BodyType.MASSIVE;
        worldState.NbodySetIsFixed(nbody, false);
        worldState.SetPosition3d(nbody, pos);
        worldState.SetVelocity3d(nbody, vel);
        // Null out the fixedBody in case there is an Impulse before the deferred Off rails runs
        nbody.engineRef.fixedBody = null;

        offRailsDefered.Add(nbody);
#pragma warning disable 162     // disable unreachable code warning
        if (DEBUG) {
            Debug.LogFormat("Body off-rails: {0}", nbody.gameObject.name);
        }
#pragma warning restore 162
        if (RecordForRewind()) {
            worldState.GetGERewindMgr().RecordBodyOffRails(nbody, worldState.GetPhysicsTime());
        }
    }

    /// <summary>
    /// Take a body that is doing Nbody evolution and put it on rails around the centerNBody. 
    /// 
    /// If there is no KeplerSequence/OrbitUniversal on the NBody, one will be created. 
    /// 
    /// Any existing KeplerSequence on the body will be reset (since cannot time reverse rails to 
    /// earlier segements since nbody evolution phase was off-rails). 
    /// 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="centerNBody"></param>
    /// <returns></returns>
    public KeplerSequence BodyOnRails(NBody nbody, NBody centerNBody) {
        if (nbody.engineRef.bodyType == BodyType.FIXED)
            return nbody.gameObject.GetComponent<KeplerSequence>(); 
        // Setup on-rails with the r,v and center object we have
        const bool relativePosFalse = false;

        // get the position and velocity and remove from NBody
        Vector3d pos = GetPositionDoubleV3(nbody);
        Vector3d vel = GetVelocityDoubleV3(nbody);
        RemoveBody(nbody.gameObject); // this will update GravityState isOnRails()

        KeplerSequence ks = null;
        ks = nbody.gameObject.GetComponent<KeplerSequence>();
        if (ks == null) {
            OrbitUniversal orbitU = nbody.gameObject.GetComponent<OrbitUniversal>();
            if (orbitU == null) {
                orbitU = nbody.gameObject.AddComponent<OrbitUniversal>();
            }
            orbitU.InitFromRVT(pos, vel, GetPhysicalTimeDouble(), centerNBody, relativePosFalse);
            orbitU.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
        } else {
            // Will not be able to time reverse to a time before this, so clear out any orbitU elements
            // (this will leave the first segement in place)
            ks.Reset();
            ks.AppendElementRVT(pos, vel, GetPhysicalTimeDouble(), relativePosFalse, nbody, centerNBody, null);
        }

        AddBody(nbody.gameObject);
        return ks;
    }

    /// <summary>
    /// Updates the position and velocity of an existing body in the engine to new 
    /// GE values (e.g. teleport of the object)
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="pos">position (internal units)</param>
    /// <param name="vel">velocity (internal units)</param>
    public void UpdatePositionAndVelocity(NBody nbody, Vector3 pos, Vector3 vel)
    {

        if (nbody == null) {
            Debug.LogError("object to update has no NBody: ");
            return;
        }
        SetPositionDoubleV3(nbody, new Vector3d(pos));
        SetVelocity(nbody, vel);
        if (trajectoryPrediction) {
            trajectoryRestart = true;
        }
        // Ugh. Need to deprecate the use of vel_phys!
        nbody.UpdateVelocity();
        // If not evolving force game objects to new position(s)
        // @TODO Ideally would only update the one object
        if (!evolve) {
            UpdateGameObjects();
        }
    }


    public void UpdatePositionAndVelocity(NBody nbody, Vector3d pos, Vector3d vel) {

		if (nbody == null) {
			Debug.LogError("object to update has no NBody: ");
			return;
		}
        worldState.UpdatePositionAndVelocity(nbody, pos, vel);
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
        // Ugh. Need to deprecate the use of vel_phys!
        nbody.UpdateVelocity();
        // If not evolving force game objects to new position(s)
        // @TODO Ideally would only update the one object
        if (!evolve) {
            UpdateGameObjects();
        }
	}

    /// <summary>
    /// Changes the length scale of all NBody objects in the scene due to a change in the inspector.
    /// Find all NBody containing objects.
    /// - independent objects are rescaled
    /// - orbit based objects have their primary dimension adjusted (e.g. for ellipse, a)
    ///   (these objects are scalable and are asked to rescale themselves)
    ///
    /// Length scale is Nbody units/Unity Length e.g. km/Unity Length
    /// Not intended for run-time use.
    /// </summary>
    private void UpdateLengthScale(float newScale) {
        if (newScale != _lengthScale) {
            _lengthScale = newScale;
            massScale = (float) GravityScaler.UpdateMassScale(units, _timeScale, _lengthScale);
            GravityScaler.ScaleScene(units, _lengthScale);
        }
	}

	/// <summary>
	/// Updates the time scale.
	/// Prior to scene starting GE adjusts the time scale by setting DT for the numerical integrators.
	///
	/// During evolution DT cannot be changed on the fly for the Leapfrog integrators without violating
	/// energy conservation - so changes are made in the number of integration performed. This imposes a
	/// practical limit on how much "speed up" can occur - since too much time evolution will lower the
	/// frame rate.
	/// </summary>
	/// <param name="value">Value.</param>

	private void UpdateTimeScale(float value) {

		if (!evolve) {
			_timeScale = value;
			massScale = (float) GravityScaler.UpdateMassScale(units, _timeScale, _lengthScale);
			// need to do something with timeZoom here...
		} 
	}

	// TODO: Need to get conversion from phys to world
	public float GetVelocityScale() {
		return GravityScaler.GetVelocityScale();
	}


	// Intially thought in terms of changing DT based on units - but decided it's better to rescale
	// distances and masses to adapt to a universal timescale. 
	private void ConfigureDT() {
		
		double time_g1 = 1f;
		double stepsPerSec = PHYSICS_FPS * (double) stepsPerFrame;
		double particleStepsPerSec = PHYSICS_FPS * (double) particleStepsPerFrame;
		engineDt = time_g1/stepsPerSec; 
		particle_dt = time_g1/particleStepsPerSec; 

	}

    /// <summary>
    /// Update physics based on collisionType between body1 and body2. 
    ///
    /// In all cases except bounce, the handling is a "hit and stick" and body2 is assumed to be
    /// removed. It's momtm is not updated. body1 velocity is adjusted based on conservation of momtm.
    ///
    /// </summary>
    /// <param name="body1">Body1.</param>
    /// <param name="body2">Body2.</param>
    /// <param name="collisionType">Collision type.</param>
    public void Collision(GameObject body1, GameObject body2, NBodyCollision.CollisionType collisionType, float bounce) {
        NBody nbody1 = body1.GetComponent<NBody>();
        NBody nbody2 = body2.GetComponent<NBody>();
        Collision(nbody1, nbody2, collisionType, bounce);
    }

    public void Collision(NBody nbody1, NBody nbody2, NBodyCollision.CollisionType collisionType, float bounce) {

        int index1 = nbody1.engineRef.index;
		int index2 = nbody2.engineRef.index;
		if (index1 < 0 || index2 < 0)
			return; 

		// if either is massless, no momtm to exchange
		if (nbody1.mass == 0 || nbody2.mass == 0) {
			if (collisionType == NBodyCollision.CollisionType.BOUNCE) {
				// reverse the velocities
				if (nbody1.mass == 0) {
                    worldState.SetVelocity3d(nbody1, -1 * worldState.GetVelocity3d(nbody1));
				}
				if (nbody2.mass == 0) {
                    worldState.SetVelocity3d(nbody2, -1 * worldState.GetVelocity3d(nbody2));
                }
            }
			return;
		}
		// velocity information is in the integrators. 
		// 
		Vector3d vel1 = worldState.GetVelocity3d(nbody1);
        Vector3d vel2 = worldState.GetVelocity3d(nbody2);
        // work in CM frame of B1 and B2. 
        double m1 = worldState.GetMass(nbody1);
        double m2 = worldState.GetMass(nbody2);
		float m_total = (float)(m1 + m2);
		Vector3d cm_vel = ( m1*vel1 + m2*vel2)/m_total;
		// Determine new velocities in CM frame
		Vector3d vel1_cm = vel1 - cm_vel;
		Vector3d vel2_cm = vel2 - cm_vel;
		if (collisionType == NBodyCollision.CollisionType.ABSORB_IMMEDIATE ||
			collisionType == NBodyCollision.CollisionType.EXPLODE) {
			// update mass of body1 to include body2
            worldState.SetMass(nbody1, m_total);
            // Update velocities in integrator
            worldState.SetVelocity3d(nbody1, cm_vel);
		} else if (collisionType == NBodyCollision.CollisionType.BOUNCE) {
            // reverse CM velocities and flip back to world velocities
            worldState.SetVelocity3d(nbody1, cm_vel - bounce * vel1_cm);
            worldState.SetVelocity3d(nbody2, cm_vel - bounce * vel2_cm);
		}
	}

    // Update the game objects positions from the values held by the GravityEngine based on physics evolution
    // These positions are globally scaled by physicalScale to allow the physics to act on a
    // suitable scale where required. 	
    //

  
	private void UpdateGameObjects() {
        float iTime = GetPhysTimeForInterpolation();
        for (int i=0; i < worldState.numBodies; i++) {
            NBody nbody = gameNBodies[i];
            if (worldState.NbodyIndexIsActive(i) && !worldState.NbodyGetNoUpdateFlag(i)) {
                if (nbody.engineRef.bodyType != BodyType.MASSLESS) {
                    Vector3 position = worldState.GetPhysicsPosition(nbody);
                    if (updateMode != UpdateMode.FIXED_UPDATE) {
                        Vector3 v = worldState.GetVelocity3d(nbody).ToVector3();
                        position += iTime * v;
                    }
                    if (NUtils.VectorNaN(position)) {
                        InactivateBody(gameNBodies[i].gameObject);
                        Debug.LogWarning("Position NaN - inactivated " + gameNBodies[i].name);
                    } else {
                        // If locked to XY plane, force Z to 0 after physics calculation
                        // This allows gravity to calculate normally, but constrains movement to XY plane
                        if (nbody.lockToXYPlane) {
                            position.z = 0f;
                            // Get velocity and also force Z to 0
                            Vector3 velocity = GetVelocity(nbody);
                            velocity.z = 0f;
                            // Update physics state to maintain Z=0 constraint
                            Vector3d pos3d = new Vector3d(position);
                            worldState.SetPosition3d(nbody, pos3d);
                            Vector3d vel3d = new Vector3d(velocity);
                            worldState.SetVelocity3d(nbody, vel3d);
                            position = physToWorldFactor * position;
                            nbody.GEUpdate(position, velocity, this);
                        } else {
                            position = physToWorldFactor * position;
                            nbody.GEUpdate(position, GetVelocity(nbody), this);
                        }
                    }
                } 
            }
		}
        // particles (no interpolation yet, better to use UPDATE interpolation mode)
        foreach (GravityParticles nbp in worldState.gravityParticles) {
            nbp.UpdateParticles(physToWorldFactor, this);
        }

    }

    /// <summary>
    /// Convert a GE internal physical position to a transform position in the scene. 
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector3 MapPhyPosToWorld(Vector3 pos) {
        return MapToScene(pos * physToWorldFactor);
    }

    /// <summary>
    /// Move all GE controlled objects position by the Vector3 move.
    /// 
    /// This moves the internal physics positions. The idea here is to allow an object that is important in the
    /// scene (e.g. a spaceship) to always be with a few thousand unity units of the origin, so that it's position
    /// maps to the scene without precision errors that could arise if it were at e.g. millions of units in position. 
    /// 
    /// This call will update massive, massless, fixed and particles under the control of GE. If trajectory prediciton is
    /// enabled trajectory values will be updated and this may be CPU-expensive. 
    /// 
    /// OrbitPredictors will recalculate positions on the next frame.
    /// 
    /// This is typically triggered by an external agent monitoring the physical position. Note that other elements in the
    /// scene (e.g. cameras) may need to be adjusted. Elements with internal position state (e.g. trail renderers) must be 
    /// adjusted by external code. 
    /// </summary>
    /// <param name="move">Amount to move in physics space (internal GE positions, not necessarily scene positions depending on units and scale choice.</param>

    public void MoveAll(NBody nbody) {
        Vector3d moveBy = -1.0 * GetPositionDoubleV3(nbody);
        MoveAll(moveBy);
    }

    public void MoveAll(Vector3 move) {
        MoveAll(new Vector3d(move));
    }

    public void MoveAll(Vector3d move) {
        double[] moveBy = new double[] { move.x, move.y, move.z };

        worldState.MoveAll(move);
        // Particles
        foreach (GravityParticles nbp in worldState.gravityParticles) {
            nbp.MoveAll(ref moveBy);
        }
        // Trajectories
        if (trajectoryState != null) {
            // move massless trajectories
            for (int i = 0; i < worldState.numBodies; i++) {
                // move massive trajectories if present
                if ((trajectories[i] != null) && worldState.NbodyIndexIsActive(i)) {
                    trajectories[i].MoveAll(move);
                }               
            }
            trajectoryState.MoveAll(move);
        }
        // Kepler objects cache their position, tell them about move
        foreach (FixedBody fixedBody in worldState.fixedBodies) {
            // TODO: Pass as a vector3d
            fixedBody.fixedOrbit.Move(move.ToVector3());
        }

        // update positions on screen
        UpdateGameObjects();

    }

    // TODO: Try and clean up this API bloat without breaking backwards compatibility...

    /// <summary>
    /// Gets the velocity of the body in "Physics Space" using a GameObject. 
    /// May be different from Unity co-ordinates if physToWorldFactor is not 1. 
    /// This is the velocity in Unity units. For dimensionful velocity use @GetScaledVelocity
    /// </summary>
    /// <returns>The velocity.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetVelocity(GameObject body) {
        NBody nbody = body.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogError("No NBody found on " + body.name + " cannot get velocity");
            return Vector3.zero;
        }
        return GetVelocity(nbody);
    }

    /// <summary>
    /// Gets the velocity of the body in "Physics Space" using an NBody reference. 
    /// May be different from Unity co-ordinates if physToWorldFactor is not 1 or MapToScene is enabled.
    /// 
    /// This is the velocity in Unity units. For dimensionful velocity use @GetScaledVelocity
    /// </summary>
    /// <returns>The velocity.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetVelocity(NBody nbody) {
        return worldState.GetVelocity3d(nbody).ToVector3();
    }

    /// <summary>
    /// Set the velocity of an Nbody. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity">Physics velocity (GE internal value)</param>
	public void SetVelocity(NBody nbody, Vector3 velocity) {
        worldState.SetVelocity3d(nbody, new Vector3d(velocity));
	}

     /// <summary>
    /// Double precision access to GE internal velocity (in physics units)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetVelocityDouble(NBody nbody, ref double[] vel) {
        worldState.GetVelocityDouble(nbody, ref vel);
    }

    /// <summary>
    /// Get double precision velocity in internal physics units. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetVelocityDoubleV3(NBody nbody) {

        return worldState.GetVelocity3d(nbody);
    }

    /// <summary>
    /// Double precision setter for internal velocity in GE. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    public void SetVelocityDouble(NBody nbody, ref double[] velocity) {
        worldState.SetVelocityDouble(nbody, ref velocity);
    }

    public void SetVelocityDoubleV3(NBody nbody, Vector3d vel) {
        worldState.SetVelocity3d(nbody, vel);
    }

    /// <summary>
	/// Set time to evolve in the reversed direction.
    ///
    /// The following events will be undone:
    /// - ApplyImpulse
    /// - SetVelocity
    /// - Maneuvers
    /// - Add game objects
    /// - Remove game objects
    ///   * this works ONLY if game objects are set inactive (i.e. SetActive(false)). If they are Destroy()'d then
    ///     there is no way for the rewind logic to get them back.
	/// </summary>
	/// <param name="flag"></param>
    public void SetTimeReversed(bool flag)
    {
        if (!rewindModeEnabled)
            Debug.LogError("Need to enable record for rewind in GE inspector");

        if (rewindActive != flag)
        {
            worldState.ReverseVelocities();
            // anything that was scheduled for the future is cleared before we rewind
            worldState.maneuverMgr.Clear();
        }
        rewindActive = flag;
    }

    /// <summary>
    /// Is Time running backwards?
    /// </summary>
    /// <returns></returns>
    public bool TimeReversed()
    {
        return rewindActive; 
    } 

    public bool RecordForRewind()
    {
        return rewindModeEnabled && !rewindActive;
    }

    public bool GetTimeReversed()
    {
        return rewindActive;
    }

    public void ApplyPastChange(NBody nbody, double atTime, Vector3d r, Vector3d v)
    {
        worldState.ApplyChangeAtTime(nbody, atTime, r, v);
    }


    /// <summary>
    /// Double precision access to GE internal position (in physics units)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetPositionDouble(NBody nbody, ref double[] p) {
        Vector3d pos3d = worldState.GetPhysicsPositionDouble(nbody);
        p[0] = pos3d.x;
        p[1] = pos3d.y;
        p[2] = pos3d.z;
    }

    /// <summary>
    /// Double precision internal position in physics units.  
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetPositionDoubleV3(NBody nbody) {
        return worldState.GetPhysicsPositionDouble(nbody);
    }

    public void SetPositionDoubleV3(NBody nbody, Vector3d pos) {
        worldState.SetPosition3d(nbody, pos);
        if (!evolve) {
            UpdateGameObjects();
        }
    }

    /// <summary>
    /// Gets the position and velocity in double precision in scaled units.
    /// (Note that the scale factors used to create these are floats - 
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="p">P.</param>
    /// <param name="v">V.</param>
    public void GetPositionVelocityScaled(NBody nbody, ref double[] p, ref double[] v ) {

        Vector3d pos3d = worldState.GetPhysicsPositionDouble(nbody);
        p[0] = pos3d.x;
		p[1] = pos3d.y;
		p[2] = pos3d.z;
        Vector3d vel = worldState.GetVelocity3d(nbody);
        v[0] = vel.x;
        v[1] = vel.y;
        v[2] = vel.z;

		p[0] = p[0]*lengthScale;
		p[1] = p[1]*lengthScale;
		p[2] = p[2]*lengthScale;
		double vscale = (double) GravityScaler.GetVelocityScale();
		v[0] = v[0]/vscale;
		v[1] = v[1]/vscale;
		v[2] = v[2]/vscale;

	}

    /// <summary>
    /// Return the internal physics engine mass
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public double GetMass(NBody nbody) {
        if (nbody == null)
            // Orbit predictor with no NBody case
            return 0.0;
        if (nbody.engineRef == null) {
            massScale = (float)GravityScaler.UpdateMassScale(units, _timeScale, _lengthScale);
            return nbody.mass * massScale;
        }
        return worldState.GetMass(nbody);
    }


	/// <summary>
	/// Gets the velocity of the body in selected unit system.
	/// e.g. for SOLAR get value in km/sec.
    /// 
    /// NBody objects have their position and velocity updated each frame and getting the info from them 
    /// is the normal approach. This routine is used internally to get an updated value during the Evolve()
    /// process (e.g. when a Kepler object wants a update of it's center body mid-integration)
	/// </summary>
	/// <returns>The velocity.</returns>
	/// <param name="body">Body</param>
	public Vector3 GetScaledVelocity(GameObject body) {
		NBody nbody = body.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("No NBody found on " + body.name + " cannot get velocity"); 
			return Vector3.zero;
		}
        
		return GetScaledVelocity(nbody);
	}

    public Vector3 GetScaledVelocity(NBody nbody) {
        Vector3 velocity = Vector3.zero;
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            double[] v = new double[3];
            worldState.GetVelocityDouble(nbody, ref v);
            return new Vector3((float)v[0], (float)v[1], (float)v[2]) / GravityScaler.GetVelocityScale();
        } else { 
            velocity = worldState.GetVelocity3d(nbody).ToVector3();
        }
        velocity = velocity / GravityScaler.GetVelocityScale();
        return velocity;
    }

    /// <summary>
    /// Gets the position of the body in selected unit system.
    /// e.g. for SOLAR get value in km/sec.
    /// 
    /// </summary>
    /// <returns>The position in world space.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetScenePosition(NBody body) {

        return GetPhysicsPosition(body)/lengthScale;
    }

    /// <summary>
    /// Gets the position of the body in internal physics units. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPosition(NBody nbody) {

        return worldState.GetPhysicsPosition(nbody);
    }

    /// <summary>
    /// Get the mass value used internally in GE. This mass value is scaled by the units and timescale factors
    /// as an Nbody is added to GE. 
    /// </summary>
    /// <param name="nBody"></param>
    /// <returns></returns>
    public float GetPhysicsMass(NBody nBody) {
        float mass = 0.0f;
        if  ((nBody.engineRef.bodyType == BodyType.MASSIVE) || 
             (nBody.engineRef.bodyType == BodyType.FIXED)) {
            mass = (float) worldState.GetMass(nBody);
        }
        return mass;
    }

    /// <summary>
    /// Gets the acceleration of the body in "Physics Space". 
    /// May be different from world co-ordinates if physToWorldFactor is not 1. 
    /// </summary>
    /// <returns>The acceleration.</returns>
    /// <param name="body">Body.</param>
    public Vector3d GetAcceleration(GameObject body) {
		NBody nbody = body.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("No NBody found on " + body.name + " cannot get velocity"); 
			return Vector3d.zero;
		}
        // Fixed body will return the acceleration
		return worldState.integrator.GetAccelerationForIndex(nbody.engineRef.index);
	}

    public Vector3 GetAccelerationScaled(GameObject body)
    {
        Vector3 a = GetAcceleration(body).ToVector3();
        return GravityScaler.ScaleAcceleration( a, lengthScale, timeScale);
    }

    /// <summary>
    /// Applies an impulse to an evolving body. The impulse is a change in momentum. The resulting
    /// velocity change will be impulse/mass. In the case of a massless body the velocity will be
    /// changed by the impulse value directly. 
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="impulse">Impulse.</param>
    public void ApplyImpulse(NBody nbody, Vector3 impulse) {
		ApplyImpulseInternal(nbody, impulse, true);
	}

	/// <summary>
	/// Determine the velocity that will result if the impulse is applied BUT
	/// do not apply the impulse. This is typically used to preview an orbit
    /// change for an nbody that has an OrbitPredictor attached. 
	/// </summary>
	/// <returns>The for impulse.</returns>
	/// <param name="nbody">Nbody.</param>
	/// <param name="impulse">Impulse.</param>
	public Vector3 VelocityForImpulse(NBody nbody, Vector3 impulse) {
		return ApplyImpulseInternal(nbody, impulse, false);
	}

    private Vector3 ApplyImpulseInternal(NBody nbody, Vector3 impulse, bool apply)
    {
#pragma warning disable 162     // disable unreachable code warning
        if (DEBUG) {
            Debug.LogFormat("Impulse: {0} gets i={1} ", nbody.gameObject.name, impulse);
        }
#pragma warning restore 162        
        // apply an impulse to the indicated NBody
        // impulse = step change in the momentum (p) of a body
        // delta v = delta p/m
        // If the spaceship is massless, then treat impulse as a change in velocity
        Vector3 velocity = Vector3.zero;
        if (nbody.engineRef.fixedBody != null) {
            // Can only apply impulse to OrbitU (not OrbitEllipse or OrbitHyper)
            OrbitUniversal orbitU = null;
            KeplerSequence kSeq = nbody.engineRef.fixedBody.keplerSeq;
            if ((kSeq != null) && apply) {
                orbitU = kSeq.GetCurrentOrbit();
                // 11.0 Support generation of KeplerSequence segment for impulses
                // Applying an impulse here will scrub any future segments in the case where we have rewound
                // to this point. 
                OrbitUniversal orbitNew = kSeq.NewOrbitSegment();
                orbitNew.CopyFromOrbitUniversal(orbitU);
                orbitNew.ApplyImpulse(impulse);
                kSeq.RemoveFutureSegments();
                kSeq.AppendElementExistingOrbitU(orbitNew, callback: null);

            } else if (nbody.engineRef.fixedBody.orbitU != null) {
                orbitU = nbody.engineRef.fixedBody.orbitU;
                // OrbitU will do the velocity addition internally. 
                if (nbody.mass > 1E-6) {
                    impulse = impulse / (nbody.mass * massScale);
                }
                if (apply) {
                    if (RecordForRewind()) {
                        worldState.GetGERewindMgr().RecordImpulse(nbody, orbitU, worldState.GetPhysicsTime());
                    }
                    orbitU.ApplyImpulse(impulse);
                }
                velocity = impulse + GetVelocity(nbody);
            }
        } else {
            switch (nbody.engineRef.bodyType) {
                case BodyType.MASSLESS:
                case BodyType.MASSIVE:
                    Vector3d impulse3d = new Vector3d(impulse);
                    Vector3d v = worldState.GetVelocity3d(nbody);
                    Vector3d v_new = v;
                    if (nbody.mass < 1E-6) {
                        v_new += impulse3d;
                    } else {
                        v_new += impulse3d / (nbody.mass * massScale);
                    }
                    if (apply) {
                        worldState.SetVelocity3d(nbody, v_new);
                    }
                    break;

                case BodyType.FIXED:
                    Debug.LogWarning("Not Supported");
                    // not yet supported
                    break;
            }
        }
        // will need to re-calc trajectories
        if (trajectoryPrediction) {
            trajectoryRestart = true;
        }
        return velocity;
    }

    /// <summary>
    /// Update the mass of a NBody in the integration while evolving.
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    public void UpdateMass(NBody nbody) {
        worldState.SetMass(nbody, nbody.mass * massScale);
		if (trajectoryPrediction) {
			trajectoryRestart = true;
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("trajectory restart");
#pragma warning restore 162		
        }
    }

    /// <summary>
    /// Update the particle capture size of a NBody in the integration while evolving.
    /// Use the size from the provided NBody
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    public void UpdateSize(NBody nbody) {
        // Only massive objects use their particle capture size
        if (nbody.engineRef.bodyType == BodyType.MASSIVE) {
            worldState.SetSize2( nbody, nbody.size * nbody.size);
        } 
    }

    /// <summary>
    /// Computes the center of mass in physics co-ordinates.
    /// </summary>
    /// <returns>The world center of mass.</returns>
    public Vector3d ComputeCenterOfMass() {
		return worldState.ComputeCenterOfMass();		
	}

	/// <summary>
	/// Computes the center of mass velocity in physics units.
	/// </summary>
	/// <returns>The world center of mass velocity.</returns>
	public Vector3d ComputeCenterOfMassVelocity() {
        return worldState.ComputeCenterOfMassVelocity();		
	}

    /// <summary>
    /// Return the world time in the selected units as a string
    /// </summary>
    /// <returns></returns>
    public string GetScaledTimeFormatted() {
       
        return GravityScaler.GetWorldTimeFormatted(worldState.time, units); 
    }

    /// <summary>
    /// Return the world time in internal GE units
    /// </summary>
    /// <returns></returns>
    [System.Obsolete("Use the better named GetGETime")]
    public double GetWorldTime() {

        return worldState.time;
    }

    /// <summary>
    /// Return the world time in internal GE units
    /// </summary>
    /// <returns></returns>
    public double GetGETime() {
        return worldState.time;
    }

    /// <summary>
    /// Return the world state
    /// </summary>
    /// <returns></returns>
    public GravityState GetWorldState() {

        return worldState;
    }

    /// <summary>
    /// Get the timestep size from the last frame in scaled time units. 
    /// </summary>
    /// <returns></returns>
    public double GetLastScaledDt() {
        return lastWorldDt * GravityScaler.GetGameSecondPerPhysicsSecond();
    }

    /// <summary>
    /// Gets the initial energy.
    /// </summary>
    /// <returns>The initial energy.</returns>
        public float GetInitialEnergy() {
		return worldState.integrator.GetInitialEnergy(worldState);
	}

	/// <summary>
	/// Gets the current energy.
	/// </summary>
	/// <returns>The energy.</returns>
	public float GetEnergy() {
	    if (isSetup)
		return worldState.integrator.GetEnergy(worldState);
            else
                return 0f;
	}

    
    /// <summary>
    /// Register a particle system (with GravityParticles component) to be evolved via the GravityEngine.
    /// </summary>
    /// <param name="nbp">Nbp.</param>
    public void RegisterParticles(GravityParticles nbp) {
		worldState.gravityParticles.Add(nbp);
    }

    /// <summary>
    /// Remove a particle system from the Gravity Engine. 
    /// </summary>
    /// <param name="particles">Particles.</param>
    public void DeregisterParticles(GravityParticles particles) {
		worldState.gravityParticles.Remove(particles);
    }

    //-------------------------------------------------------------------------------
    // Maneuver wrappers
    // TODO: Support adding to active trajectories

    public void AddManeuvers(List<Maneuver> mlist) {
        worldState.maneuverMgr.Add(mlist);
    }

    public void AddManeuver(Maneuver m) {
		worldState.maneuverMgr.Add(m);
	}

	public void RemoveManeuver(Maneuver m) {
        worldState.maneuverMgr.Remove(m);
	}

	public List<Maneuver> GetManeuvers(NBody nbody) {
		return worldState.maneuverMgr.GetManeuvers(nbody);
	}

    public void ClearManeuvers() {
        worldState.maneuverMgr.Clear();
    }

    // trigger manager wrapper 
    // TODO: Support adding to active trajectories
    public void AddTrigger(GETriggerMgr.Trigger t)
    {
        worldState.AddTrigger(t);
    }

    public void RemoveTrigger(GETriggerMgr.Trigger t)
    {
        worldState.RemoveTrigger(t);
    }

    public void ClearTriggers()
    {
        worldState.Clear();
    }

    //-------------------------------------------------------------------------------

    public void LogDump() {
        DumpAll(worldState);
    }

    public string DumpWorldState() {
        return DumpAll(worldState);
    }

	public string DumpAll(GravityState gs) {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		sb.Append(string.Format("massScale={0} timeScale={1} lengthScale={2}\n", massScale, timeScale, lengthScale));
        sb.Append(string.Format("   time={0} physToWorldFactor={1} onRails={2} gameSecPerPhySec={3} engineDt={4} velScale={5}\n", 
                    gs.time, physToWorldFactor, worldState.IsOnRails(), GravityScaler.game_sec_per_phys_sec, engineDt, 
                    GravityScaler.GetVelocityScale()));
        // Reaching into GravityState directly here is not great. Need to RF but have GetVelocity and gameObjects
        // to deal with
        sb.Append(worldState.DumpAll(gameNBodies, this));
        if (rewindModeEnabled)
        {
            sb.Append("\nRewind Manager:\n");
            sb.Append(worldState.GetGERewindMgr().DumpAll());
        }
		return sb.ToString();
	}

    //============================================================================================
    // Console commands: If there is a GEConsole in the scene, these commands will be availbale
    //============================================================================================

    private void AddConsoleCommands() {
        GEConsole.RegisterCommandIfConsole(new ClearCommand());
        GEConsole.RegisterCommandIfConsole(new DumpCommand());
        GEConsole.RegisterCommandIfConsole(new FastForwardCommand());
        GEConsole.RegisterCommandIfConsole(new GoCommand());
        GEConsole.RegisterCommandIfConsole(new InfoCommand());
        GEConsole.RegisterCommandIfConsole(new PauseCommand());
        GEConsole.RegisterCommandIfConsole(new SetTimeCommand());
        GEConsole.RegisterCommandIfConsole(new SingleStepCommand());
        GEConsole.RegisterCommandIfConsole(new TimeZoomCommand());

    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class DumpCommand : GEConsole.GEConsoleCommand
    {
        public DumpCommand() {
            names = new string[] { "dump", "d" };
            help = "dump all bodies and their (r,v) info";
        }

        override
        public string Run(string[] args) {
            Debug.Log(GravityEngine.Instance().DumpWorldState());
            return GravityEngine.Instance().DumpWorldState();
        }
    }

    /// <summary>
    /// Show all possible position/velocity representations of an Nbody object.
    /// </summary>
    public class InfoCommand : GEConsole.GEConsoleCommand
    {
        public InfoCommand() {
            names = new string[] { "info", "i" };
            help = "dump assorted info about a game object by name ";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "info requires one argument (name of gameobject)";
            }
            GameObject go = GameObject.Find(args[1]);
            if (go == null) {
                return string.Format("Cannot find game object {0} in scene.", args[1]);
            }
            NBody nbody = go.GetComponent<NBody>();
            if (nbody == null) {
                return string.Format("Game object {0} does not have an NBody component.", args[1]);
            }
            GravityEngine ge = GravityEngine.Instance();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            // flags
            sb.Append(string.Format("Info for {0}\n", args[1]));
            // check flags
            sb.Append("    Info: ");
            if (!ge.worldState.NbodyIsActive(nbody) ) {
                sb.Append("INACTIVE ");
            }
            if (ge.worldState.NbodyIsFixed(nbody) ) {
                sb.Append("FIXED_MOTION ");
            }
            sb.Append("\n");

            sb.Append(string.Format("   Scene:\n"));
            sb.Append(string.Format("     transform={0} nbody.mass={1} orbitDepth={2}\n", 
                nbody.transform.position, nbody.mass, nbody.GetOrbitDepth() ));
            // engine
            sb.Append(string.Format("   Engine:\n"));
            Vector3 pos = ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0}  r_mag={1} m\n", pos, pos.magnitude));
            Vector3 vel = ge.GetVelocity(nbody);
            sb.Append(string.Format("     v={0}  v_mag={1}\n", vel, vel.magnitude));
            sb.Append(string.Format("    engine mass={0}\n", ge.GetPhysicsMass(nbody)));
            // scaled
            GravityScaler.Units units = GravityEngine.Instance().units;
            sb.Append(string.Format("   Scaled: units={0}\n", units));
            pos = ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0} {1} r_mag={2} {1}\n",
                           pos, GravityScaler.LengthUnits(units), pos.magnitude));
            vel = ge.GetScaledVelocity(nbody);
            sb.Append(string.Format("     v={0} {1} v_mag={2} {1}\n", 
                            vel, GravityScaler.VelocityUnits(units), vel.magnitude));
            // SI
            sb.Append(string.Format("   SI:\n"));
            float conversion = (float) GravityScaler.PositionScaletoSIUnits();
            pos = conversion * ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0} m r_mag={1} m\n", pos, pos.magnitude ));
            conversion = (float)GravityScaler.VelocityScaletoSIUnits();
            vel = conversion * ge.GetVelocity(nbody);
            sb.Append(string.Format("     v={0} m/s  v_mag={1} m/s\n", vel, vel.magnitude));

            // Maneuvers
            List<Maneuver> maneuvers = ge.GetManeuvers(nbody);
            sb.Append(string.Format("   Maneuvers:\n"));
            if (maneuvers.Count > 0) {
                foreach (Maneuver m in maneuvers) {
                    sb.Append(string.Format("    t={0} type={1} dv={2} v={3}", m.worldTime, m.mtype, m.dV, m.velChange));
                }
            } else {
                sb.Append("    none\n");
            }
            return sb.ToString();
        }
    }
    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class PauseCommand : GEConsole.GEConsoleCommand
    {
        public PauseCommand() {
            names = new string[] { "pause", "p" };
            help = "pause GravityEngine";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().SetEvolve(false);
            return "paused\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class GoCommand : GEConsole.GEConsoleCommand
    {
        public GoCommand() {
            names = new string[] { "go" };
            help = "resume evolution";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().SetEvolve(true);
            return "running...\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class ClearCommand : GEConsole.GEConsoleCommand
    {
        public ClearCommand() {
            names = new string[] { "clear" };
            help = "clear all bodies in the engine";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().Clear();
            return "Cleared\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class TimeZoomCommand : GEConsole.GEConsoleCommand
    {
        public TimeZoomCommand() {
            names = new string[] { "timezoom", "z" };
            help = "set time zoom (run time): timezoom <number>";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "timezoom requires one argument";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().SetTimeZoom(value);
            return string.Format("Timezoom set to: {0}\n", value);
        }
    }

    /// <summary>
    /// Fast forward by a specified amount. Runs a full Nbody sim to get to that time.
    /// </summary>
    public class FastForwardCommand : GEConsole.GEConsoleCommand
    {
        public FastForwardCommand() {
            names = new string[] { "fastfwd", "ff" };
            help = "fast forward by the specified amount: fastfwd <time>";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "fast forward requires one argument";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().FastForward(value);
            return string.Format("Fast forward to: {0}\n", GravityEngine.Instance().GetScaledTimeFormatted());
        }
    }

    public class SetTimeCommand : GEConsole.GEConsoleCommand
    {
        public SetTimeCommand() {
            names = new string[] { "settime", "st" };
            help = "set time to the specified amount: settime <time>. Only available if all objects on rails";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "set time requires one argument";
            }
            if (!GravityEngine.Instance().GetWorldState().IsOnRails()) {
                return "Not all objects are on rails. Cannot set the time.\n";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().SetPhysicalTime(value);
            return string.Format("Set time to: {0}\n", GravityEngine.Instance().GetScaledTimeFormatted());
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class SingleStepCommand : GEConsole.GEConsoleCommand
    {
        public SingleStepCommand() {
            names = new string[] { "step", "s" };
            help = "evolve one fixed update cycle";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().EvolveOneFixedUpdate();
            GravityEngine.Instance().SetEvolve(true);
            return "step\n";
        }
    }

}
