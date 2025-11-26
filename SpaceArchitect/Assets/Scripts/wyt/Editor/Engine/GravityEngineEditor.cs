using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GravityEngine), true)]
public class GravityEngineEditor : Editor {


	private const string mTip = "Scale applied to all masses in the scene. Increasing will result in larger forces and faster evolution.";
	private const string algoTip = "Integration algorithm used to evolve massive bodies.\nLeapfrog is default. AZTTriple is for exactly three massive bodies.";
	private const string forceTip = "Select the force to be used in the scene.";
	private const string timeTip = "Timescale controls the difference between game time and physics time in Gravity Engine. Larger values result in faster evolution BUT more calculations being performed. ";
	private const string mlessTip = "Evolve massless bodies seperately using a built-in Leapfrog algorithm.";
	private const string ppTip = "Particle accuracy (0=best performance/lower accuracy, 1 = highest accuracy/most CPU)";
	private const string autoStartTip = "Begin evolving bodies as soon as the scene starts. If false then starting is via script API.";
	private const string autoAddTip = "Automatically detect Nbodies in the scene and add them to the Gravity Engine.";
	private const string phyWTip = "Scale factor applied to physics position to scale up to world view. Typically 1 unless initializing from known solutions via scripts (e.g. ThreeBodySolution)";

	private const string trajTip = "Enable trajectory prediction on bodies with Trajectory component.";

	private const string trajTimeTip = "Time to predict trajectory forward (in scaled units)";
  // BORG - Added to reduce impact of "endless trajectory record data" bug
  private const string trajectoryDataTimeDistanceTip = "Min distance allowed between record data points (in scaled units). Higher values result in a larger gap between points and improved performance. " +
          "Lower values improve accuracy at the expense of performance.";
	private const string trajCanvasTip = "Trajectories with Text labels enabled will be added as children to this world canvas object." +
					"If text is inactive, this can be left blank.";
	private const string markerTip = "Trajectories with time markers will be added as children to thisobject." +
					"If markers not used, this can be left blank.";
	private const string trajResetTip = "Limit trajectory prediction re-calculations to this number of iterations per frame.\n" + 
                "Low numbers reduce CPU spikes when trajectory recomputations are required (but re-compute takes longer)";

	private const string stepTip = "Number of integration steps gravity engine will target for each 60fps frame. " + 
				"The value depends in how much accuracy is desired and the masses in the scene. Larger values may impact the frame rate.\n" +
				"Default value is 8.";

	private const string pstepTip = "Number of particle integration steps gravity engine will target for each 60fps frame. " + 
				"The value depends in how much accuracy is desired and the masses in the scene." +
				" Larger values can slow the simulation significantly if many particles are used.\n" + 
				"Default value is 2.";

	private const string unitsTip = "Units specifies the interpretation of the distance and mass\n" +
				"in NBody objects.\n\n" + 
				"GravityEngine uses G=1 and choice of units defines an intrinsic timescale.\n\n" +
				"Game time to physics time can then be specified in the inspector.";

    private const string mapTip = "Use transform position, rotation and scale to adjust all objects in the scene. " +
            "\nThis can be useful for e.g. navigation console on a ship showing orbits. ";

    private const string singleStepTip = "Enable single step (on N key) when playing in the Unity Editor";

    private const string xzTip = "Define zero inclination orbit to be in the XZ plane, instead of the XY plane.";

    private const string cmTip = "On startup set the center of mass (CM) to the specified position and velocity. This is commonly used "
    + " to null out residial CM velocity rates on scene startup when massive planets are being used and when "
    + " anchoring the center with a FixedObject is not desired.";

    private const string opITip = "Interval (in frames) between orbit predictor updates for non-Kepler orbits. This can dominate "
        + " performance for massive Nbody simulations unless dialed down.";

    private const string opKTip = "Allow Kepler bodies to skip predictor updates when the center body has not moved and"
        + " there have been no changes in the initial conditions.";

	private const string umTip = "GE allows for different modes that control when and how the game object transforms are updated:\n" +
		   "FIXED_UPDATE: Run physics and updates on the FixedUpdate call.\n" +
		   "FIXED_INTERPOLATE: Run physics on FixedUpdate and update positions on Update using linear interpolation\n" +
		   "UPDATE: Run physics and positions on the Update loop.\n";

	private const string yearTip = "Calendar year for start time of orbits specified using TLE/Celestrak data for orbits";
	private const string monthTip = "Calendar month for start time of orbits specified using TLE/Celestrak data for orbits";
	private const string dayTip = "Calendar day for start time of orbits specified using TLE/Celestrak data for orbits";
	private const string timeUtcTip = "Universal decimal time for start time of orbits specified using TLE/Celestrak data for orbits";

	private const string mapDelTip = "(Optional) An object implementing the GEMapToSceneInterface that provides a custom " +
		" mapping of game object final position from GE into the scene. If a linear mapping is needed, Use Transform to Reposition" +
		" is a better choice.";

	private const string multiTip = "(Optional) An object implementing the GEMultiplayerInterface";

	private const string rewindTip = "Enable recording of add/remove/impulse etc. to allow rewind to reverse events.";

	[MenuItem("GameObject/3D Object/GravityEngine")]
	static void Init()
    {
    	if (FindObjectOfType(typeof(GravityEngine))) {
    		Debug.LogWarning("NBodyEngine already in scene.");
    		return;
    	}
		GameObject nbodyEngine = new GameObject();
		nbodyEngine.name = "GravityEngine";
		nbodyEngine.AddComponent<GravityEngine>();
    }

	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		GravityEngine gravityEngine = (GravityEngine) target;
		float massScale = gravityEngine.massScale; 
		float timeScale = gravityEngine.timeScale; 
		float lengthScale = gravityEngine.lengthScale;

        bool xzOrbitsOld = gravityEngine.xzOrbits;

		float physToWorldFactor = gravityEngine.physToWorldFactor; 

		bool detectNbodies = gravityEngine.detectNbodies;
		bool evolveAtStart = gravityEngine.evolveAtStart;
		bool scaling = gravityEngine.editorShowScale;
		bool startTime = gravityEngine.editorShowStartTime;
        bool scalingDetails = gravityEngine.editorShowScaleDetails;
		bool showAdvanced = gravityEngine.editorShowAdvanced;
		bool showTrajectory = gravityEngine.editorShowTrajectory;
		bool cmFoldout = gravityEngine.editorCMfoldout;
        bool useTransform = gravityEngine.useTransform;
		bool setCenterOfMass = gravityEngine.setCenterOfMass;
		Vector3 cmPos = gravityEngine.cmPosition;
		Vector3 cmVel = gravityEngine.cmVelocity;
        int orbitPredInterval = gravityEngine.orbitPredictorInterval;
        bool orbitPredKeplerOpt = gravityEngine.orbitPredictorKeplerOpt;

		bool rewind = gravityEngine.rewindModeEnabled;


        bool trajectoryPrediction = gravityEngine.trajectoryPrediction;
		float trajectoryTime = gravityEngine.trajectoryTime;
    	// Added to reduce impact of "endless trajectory record data" bug
    	float trajectoryDataTimeDistance = gravityEngine.trajectoryDataTimeDistance;
		GameObject trajCanvas = gravityEngine.trajectoryCanvas;
		GameObject markerParent = gravityEngine.markerParent;
		float  computeFactor = gravityEngine.trajectoryComputeFactor;

		int stepsPerFrame = gravityEngine.stepsPerFrame;
		int particleStepsPerFrame = gravityEngine.particleStepsPerFrame;

		GravityEngine.Algorithm algorithm = GravityEngine.Algorithm.LEAPFROG;
		ForceChooser.Forces force = ForceChooser.Forces.Gravity;
		GravityScaler.Units units = gravityEngine.units;

		GameObject mapToSceneGO = gravityEngine.mapToSceneGameObject;
		GameObject multiplayerInterfaceGO = gravityEngine.multiplayerInterfaceGO;

		EditorGUIUtility.labelWidth = 200;

        useTransform = EditorGUILayout.Toggle(new GUIContent("Use Transform to Reposition", mapTip), useTransform);

		mapToSceneGO = (GameObject) EditorGUILayout.ObjectField(
					new GUIContent("MapToScene delegate (optional)", mapDelTip),
					mapToSceneGO, typeof(GameObject), true );

		bool xzOrbits = EditorGUILayout.Toggle(new GUIContent("XZ orbital plane", xzTip), xzOrbitsOld);
		if (xzOrbits != xzOrbitsOld) {
			// All generated positions that use XZ orbits need to be updated
			NBody[] nbodies = FindObjectsOfType<NBody>();
			foreach(NBody n in nbodies) {
				IOrbitScalable ios = n.gameObject.GetComponent<IOrbitScalable>();
				if (ios != null) {
					ios.ApplyXZChange();
				}
			}
		}

		EditorGUIUtility.labelWidth = 150;

		GravityEngine.UpdateMode updateMode = (GravityEngine.UpdateMode) 
					EditorGUILayout.EnumPopup(new GUIContent("Update Mode", umTip), gravityEngine.updateMode);


        // SCALING
        scaling = EditorGUILayout.Foldout(scaling, "Scaling");
		float oldLengthScale = gravityEngine.lengthScale;
		if (scaling) {
			units =
				(GravityScaler.Units)EditorGUILayout.EnumPopup(new GUIContent("Units", unitsTip), gravityEngine.units);
            EditorGUILayout.LabelField("Scaling values change when <ENTER> is pressed.");

            switch (units) {
			case GravityScaler.Units.DIMENSIONLESS:
					// only have mass scale in DL case
					EditorGUILayout.LabelField("Use mass scale to control scene speed.");
					EditorGUILayout.LabelField("(More massive = faster)");
					massScale = EditorGUILayout.DelayedFloatField(new GUIContent("Mass Scale", mTip), gravityEngine.massScale);
					break;
			case GravityScaler.Units.SI:
					// no mass scale is controlled by time exclusivly
					EditorGUILayout.LabelField("m/kg/sec.");
					// meters per Unity unit in the case of meters
					lengthScale = EditorGUILayout.DelayedFloatField(new GUIContent("Unity unit per m", timeTip), gravityEngine.lengthScale);
					timeScale = EditorGUILayout.DelayedFloatField(new GUIContent("Game sec. per sec.", timeTip), gravityEngine.timeScale);
					break;
			case GravityScaler.Units.ORBITAL:
					EditorGUILayout.LabelField("km/1E24 kg/hr");
					// Express in km per Unity unit km/U
					lengthScale = EditorGUILayout.DelayedFloatField(new GUIContent("Unity unit per km", timeTip), gravityEngine.lengthScale);
					timeScale = EditorGUILayout.DelayedFloatField(new GUIContent("Game sec. per hour", timeTip), gravityEngine.timeScale);
					break;
			case GravityScaler.Units.SOLAR:
					EditorGUILayout.LabelField("AU/1E24 kg/year");
					lengthScale = EditorGUILayout.DelayedFloatField(new GUIContent("Unity unit per AU", timeTip), gravityEngine.lengthScale);
					timeScale = EditorGUILayout.DelayedFloatField(new GUIContent("Game Sec per year", timeTip), gravityEngine.timeScale);
					break;
			}

            scalingDetails = EditorGUILayout.Foldout(scalingDetails, "Scaling Details");
            if (scalingDetails) {
                EditorGUILayout.LabelField(string.Format("time = {0}", gravityEngine.timeScale));
				EditorGUILayout.LabelField(string.Format("length = {0}", gravityEngine.lengthScale));
				EditorGUILayout.LabelField(string.Format("velocity = {0}", gravityEngine.GetVelocityScale()));
                EditorGUILayout.LabelField(string.Format("mass ={0}", gravityEngine.massScale));
				EditorGUILayout.LabelField(string.Format("engineDt ={0}", gravityEngine.engineDt));
			}
			// if sclaing changes the length scale has a setter that will run ApplyScale
		}

		// START time
		startTime = EditorGUILayout.Foldout(startTime, "Start Time");
		int year = gravityEngine.startTimeYear;
		int month = gravityEngine.startTimeMonth;
		int day = gravityEngine.startTimeDay;
		double timeUtc = gravityEngine.startTimeTimeOfDayUTC;
		if (startTime) {
			EditorGUILayout.LabelField("Option in ORBITAL to specify start time for satellites that use TLE data");
			year = EditorGUILayout.IntField(new GUIContent("Year", yearTip), year);
			month = EditorGUILayout.IntField(new GUIContent("Month", monthTip), month);
			day = EditorGUILayout.IntField(new GUIContent("Day", dayTip), day);
			timeUtc = EditorGUILayout.DoubleField(new GUIContent("Time (UTC)", timeUtcTip), timeUtc);
			EditorGUILayout.LabelField("  JDS: " + gravityEngine.GetStartTimeAsJD());
			(int yr00, double sgpDays) = SGP4.SGP4SatData.EpochYearDay(gravityEngine.GetStartTimeAsJD());
			EditorGUILayout.LabelField(string.Format("  SGP4 Epoch:  {0:00}{1:000.00000}", yr00, sgpDays));

			// Utility function for scene setup
			if (GUILayout.Button("Set Start by Scanning TLEs")) {
                (year, month, day, timeUtc) = ScanForLatestTLE();
                // add a fudge to UTC to make sure we're starting ahead
                timeUtc *= 1.01;
            }

        }


        // TRAJECTORY PREDICTION
        showTrajectory = EditorGUILayout.Foldout(showTrajectory, "Trajectory Prediction");
		if (showTrajectory) {
			trajectoryPrediction = EditorGUILayout.Toggle(new GUIContent("Trajectory Prediction", trajTip), trajectoryPrediction);
			trajectoryTime = EditorGUILayout.FloatField(new GUIContent("Trajectory Time", trajTimeTip), trajectoryTime);
			computeFactor = EditorGUILayout.FloatField(new GUIContent("Re-computes per frame", trajResetTip), computeFactor);
 			trajCanvas = (GameObject) EditorGUILayout.ObjectField(new GUIContent("Canvas for Text (optional)", trajCanvasTip), 
					trajCanvas, typeof(GameObject),true);
      		// Added to reduce impact of "endless trajectory record data" bug
      		trajectoryDataTimeDistance = EditorGUILayout.FloatField( new GUIContent( "Trajectory Data Time Distance", trajectoryDataTimeDistanceTip ), trajectoryDataTimeDistance );
			markerParent = (GameObject) EditorGUILayout.ObjectField(new GUIContent("Time Marker Parent (optional)", markerTip), 
					markerParent, typeof(GameObject),true);
		}

        // ADVANCED
		showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
		if (showAdvanced) {
			algorithm =
				(GravityEngine.Algorithm)EditorGUILayout.EnumPopup(new GUIContent("Algorithm", algoTip), gravityEngine.algorithm);

			rewind = EditorGUILayout.Toggle(new GUIContent("Record for Rewind", rewindTip), rewind);

			// Force selection is tangled with choice of integrator and scale - so needs to be here
			force = 
				(ForceChooser.Forces)EditorGUILayout.EnumPopup(new GUIContent("Force", forceTip), gravityEngine.force);
			if (force == ForceChooser.Forces.Custom) {
				IForceDelegate force_delegate = gravityEngine.GetComponent<IForceDelegate>();
				if (force_delegate == null) {
					EditorGUILayout.LabelField("  Attach a Force Delegate to this object.", EditorStyles.boldLabel);
				} else {
					EditorGUILayout.LabelField("  Force delegate: " + force_delegate.GetType());
				}
			}
			if (force != ForceChooser.Forces.Gravity) {
				EditorGUILayout.LabelField("    Note: Orbit predictors assume Newtonian gravity");
				EditorGUILayout.LabelField("    They are not accurate for other forces.");
			}
			
			detectNbodies = EditorGUILayout.Toggle(new GUIContent("Automatically Add NBody objects", autoAddTip), gravityEngine.detectNbodies);
			evolveAtStart = EditorGUILayout.Toggle(new GUIContent("Evolve at Start", autoStartTip), gravityEngine.evolveAtStart);
			physToWorldFactor = EditorGUILayout.FloatField(new GUIContent("Physics to World Scale", phyWTip), gravityEngine.physToWorldFactor);

			EditorGUILayout.LabelField("Steps per frame:");
			stepsPerFrame = EditorGUILayout.IntField(new GUIContent("  Massive Bodies", stepTip), stepsPerFrame);
			particleStepsPerFrame = EditorGUILayout.IntField(new GUIContent("  Particles", pstepTip), particleStepsPerFrame);

            EditorGUILayout.LabelField("Orbit Predictor Optimization:", EditorStyles.boldLabel);
            orbitPredKeplerOpt = EditorGUILayout.Toggle(new GUIContent("Kepler Optimization", opKTip), orbitPredKeplerOpt);
            orbitPredInterval = EditorGUILayout.IntField(new GUIContent("Orbit Pred. Interval", opITip), orbitPredInterval);

			EditorGUILayout.LabelField("GE Multiplayer Interface:", EditorStyles.boldLabel);

			multiplayerInterfaceGO = (GameObject )EditorGUILayout.ObjectField(
				new GUIContent("GE Network interface (optional)", multiTip),
				multiplayerInterfaceGO, typeof(GameObject), true);

		}
		// Switch bodies list on/off based on option 
		if (!gravityEngine.detectNbodies) {
			// use native Inspector look & feel for bodies object
			EditorGUILayout.LabelField("NBodies to be added at start:", EditorStyles.boldLabel);
         	SerializedProperty bodiesProp = serializedObject.FindProperty ("bodies");
         	EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(bodiesProp, true);
         	if(EditorGUI.EndChangeCheck())
             	serializedObject.ApplyModifiedProperties();
		} else {
			EditorGUILayout.LabelField("NBody objects will be detected automatically", EditorStyles.boldLabel);
		}

		// Show the CM and the velocity of the CM
		cmFoldout = EditorGUILayout.Foldout(cmFoldout, "Center of Mass Info");
		if (cmFoldout) {
			if (Application.IsPlaying(gravityEngine)) {
                EditorGUILayout.LabelField("Center of Mass:" + gravityEngine.ComputeCenterOfMass());
                Vector3d v = gravityEngine.ComputeCenterOfMassVelocity();
                EditorGUILayout.LabelField(string.Format("CM Velocity:({0},{1},{2})", v.x, v.y, v.z));
            } else {
                setCenterOfMass = EditorGUILayout.Toggle(new GUIContent("Set Center of Mass", cmTip), setCenterOfMass);
				cmPos = EditorGUILayout.Vector3Field("Position", cmPos);
				cmVel = EditorGUILayout.Vector3Field("Velocity", cmVel);
            }
		}

		if (Application.IsPlaying(gravityEngine)) {
			EditorGUILayout.LabelField(string.Format("GE time={0} timeZoom={1}", 
				gravityEngine.GetPhysicalTime(), 
				gravityEngine.TimeZoom()));
			EditorUtility.SetDirty(gravityEngine);
		}

		// Checking the Event type lets us update after Undo and Redo commands.
		// A redo updates _lengthScale but does not run the setter
		if (Event.current.type == EventType.ExecuteCommand &&
            Event.current.commandName == "UndoRedoPerformed") {
            // explicitly re-set so setter code will run. 
			gravityEngine.lengthScale = lengthScale;
        }

		if (GUI.changed) {
			Undo.RecordObject(gravityEngine, "GE Change");
            gravityEngine.useTransform = useTransform;
            gravityEngine.xzOrbits = xzOrbits;
			gravityEngine.updateMode = updateMode;
			gravityEngine.mapToSceneGameObject = mapToSceneGO;
			gravityEngine.multiplayerInterfaceGO = multiplayerInterfaceGO;

			gravityEngine.timeScale = timeScale; 
			gravityEngine.massScale = massScale; 
            // This runs a setter in GE that will do a rescale of the scene if necessary
			gravityEngine.lengthScale = lengthScale;
			gravityEngine.units = units;
			gravityEngine.physToWorldFactor = physToWorldFactor; 
			gravityEngine.detectNbodies = detectNbodies;

			gravityEngine.editorShowStartTime = startTime;
			gravityEngine.startTimeYear = year;
			gravityEngine.startTimeMonth = month;
			gravityEngine.startTimeDay = day;
			gravityEngine.startTimeTimeOfDayUTC = timeUtc;

			gravityEngine.editorShowTrajectory = showTrajectory;
			gravityEngine.trajectoryPrediction = trajectoryPrediction;
			gravityEngine.trajectoryTime = trajectoryTime;
      		gravityEngine.trajectoryDataTimeDistance = trajectoryDataTimeDistance;
			gravityEngine.trajectoryCanvas = trajCanvas;
			gravityEngine.markerParent = markerParent;
			gravityEngine.trajectoryComputeFactor = computeFactor;

			gravityEngine.algorithm = algorithm;
			gravityEngine.force = force;
			gravityEngine.evolveAtStart = evolveAtStart;
			gravityEngine.editorShowScale = scaling;
            gravityEngine.editorShowScaleDetails = scalingDetails;
			gravityEngine.editorShowAdvanced = showAdvanced;
			gravityEngine.editorCMfoldout = cmFoldout;
			gravityEngine.stepsPerFrame = stepsPerFrame;
			gravityEngine.particleStepsPerFrame = particleStepsPerFrame;

			gravityEngine.rewindModeEnabled = rewind;

			// CM
			gravityEngine.setCenterOfMass = setCenterOfMass;
			gravityEngine.cmVelocity = cmVel;
			gravityEngine.cmPosition = cmPos;

            // OP
            gravityEngine.orbitPredictorInterval = orbitPredInterval;
            gravityEngine.orbitPredictorKeplerOpt = orbitPredKeplerOpt;

            EditorUtility.SetDirty(gravityEngine);
		}

	}

	/// <summary>
    /// Utility function (mostly to setup debug scenes)
    /// </summary>
    /// <returns></returns>
	private (int year, int month, int day, double utc) ScanForLatestTLE()
    {
		double jd = 0;

		OrbitUniversal[] orbits = FindObjectsOfType<OrbitUniversal>();
		SGP4.SGP4SatData latestData = null;
		foreach(OrbitUniversal ou in orbits) {
			if (ou.inputMode == OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET) {
				SGP4toGE sgpForTLE = new SGP4toGE(ou.tleName, ou.tleLine1, ou.tleLine2);
				SGP4.SGP4SatData data = sgpForTLE.GetSatData();
				if (data.jdsatepoch > jd) {
					jd = data.jdsatepoch;
					latestData = data;
				}
            }
        }
		if (latestData != null) {
			Debug.LogFormat("Using JD={0} for {1}", jd, latestData.name);
		    double utc = latestData.hr + (60.0 * latestData.min + latestData.sec)/3600.0;
			return (latestData.year, latestData.month, latestData.day, utc);
		}
		return (0, 0, 0, 0.0);
    }
}
