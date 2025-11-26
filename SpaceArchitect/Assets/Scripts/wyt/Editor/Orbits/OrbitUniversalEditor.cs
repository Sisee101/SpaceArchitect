#define SOLAR_SYSTEM
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitUniversal), true)]
public class OrbitUniversalEditor : Editor {

	private static string eTip = "Ellipse eccentricity (>= 0). (0=circle, 1=parabola, >1 hyperbola)";
	private static string aTip = "Distance from center to focus of ellipse";
	private static string pTip = "Distance from focus to closest approach";
	private static string paramTip = "Orbit size can be specified by closest approach(p) or ellipse semi-major axis (a)";
	private static string phaseTip = "Initial position specified by angle from focus to closest approach (true anomoly)";
	private static string centerTip = "Object at focus (center) of orbit";
	private static string omega_lcTip = "Rotation of pericenter from ascending node\nWhen inclination=0 will act in the same way as \u03a9";
	private static string omega_ucTip = "Rotation of ellipse from x-axis (degrees)\nWhen inclination=0 will act in the same way as \u03c9";
	
	private static string inclinationTip = "Inclination angle of ellipse to x-y plane (degrees)";
    public const string modeTip = "GRAVITY_ENGINE mode sets the initial velocity to acheive the orbit and then"
                                 + "evolves the body with gravity.\n"
                                 + "KEPLERS_EQN forces the body to move in the indicated orbit. Its mass is still used"
                                 + "by the gravity engine to influence other objects.";

    public static string ndTip = "Rate of change of mean orbital motion due to atmospheric drag in rad/sec. For LEO E-11. For only J2 evolution set to 0 ";
    public static string nddTip = "Derivative of rate of change of mean orbital motion due to atmospheric drag (rad/sec^2).  For only J2 evolution set to 0";


    private void WarnAboutKeplerSeq(OrbitUniversal orbitU) {
        // when playing warn user if OrbitU attached to NBody is being used or not
        if (EditorApplication.isPlaying && orbitU.gameObject.activeInHierarchy) {
            KeplerSequence ks = orbitU.GetComponent<KeplerSequence>();
            if (ks != null) {
                if (ks.GetCurrentOrbit() != orbitU) {
                    EditorGUILayout.LabelField("OrbitUniversal is not current orbit in Kepler sequence!",
                        EditorStyles.boldLabel);
                }
            }
        }
    }

    
    public override void OnInspectorGUI() {
        GUI.changed = false;
        OrbitUniversal orbitU = (OrbitUniversal)target;
        bool displayAndExit = false;

        bool isBinaryOrbit = target.GetType() == typeof(BinaryOrbit);

        // check there is an NBody or particles or BinaryOrbit, if not then assume a synthesized Orbit as part of a predictor
        if ((orbitU.gameObject.transform.parent != null) && 
            (orbitU.gameObject.transform.parent.GetComponent<BinaryOrbit>() != null)) {
            EditorGUILayout.LabelField("Orbit parameters determined from BinaryOrbit");
            EditorGUILayout.LabelField("(parent).");
            displayAndExit = true;

        }
        else if ((orbitU.GetComponent<NBody>() == null) && (orbitU.GetComponent<GravityParticles>() == null)) {
            EditorGUILayout.LabelField("Orbit parameters determined from position/velocity.");
            EditorGUILayout.LabelField("(Orbit Predictor).");
            displayAndExit = true;
        }

        OrbitUniversal.EvolveMode evolveMode = orbitU.evolveMode;

        // If there is a SolarBody, it is the one place data can be changed. The EB holds the
        // orbit scaled per SolarSystem scale. 
        SolarBody sbody = orbitU.GetComponent<SolarBody>();
        if (sbody != null) {
            EditorGUILayout.LabelField("Ellipse parameters controlled by SolarBody settings.");
            displayAndExit = true;
            // allow evolveMode to be changed per solar body
            evolveMode = (OrbitUniversal.EvolveMode)EditorGUILayout.EnumPopup(new GUIContent("Evolve Mode", modeTip), evolveMode);
            if (GUI.changed) {
                Undo.RecordObject(orbitU, "OrbitU Change");
                orbitU.evolveMode = evolveMode;
                EditorUtility.SetDirty(orbitU);
            }
        }


        // fields in class
        NBody centerNBody = null;
        OrbitUniversal.InputMode inputMode = orbitU.inputMode;
        double ecc = orbitU.eccentricity;
        double p_inspector = orbitU.p_inspector;
        double omega_uc = 0;
        double omega_lc = 0;
        double inclination = 0;
        double phase = 0;
        string jplData = orbitU.jplEphemeris;

        bool sizeUpdate = false;

        WarnAboutKeplerSeq(orbitU);

        double pk_ndot = orbitU.pkepler_ndot;
        double pk_nddot = orbitU.pkepler_nddot;

        if (!displayAndExit) {
            if (!isBinaryOrbit) {
                centerNBody = (NBody)EditorGUILayout.ObjectField(
                         new GUIContent("Center NBody", centerTip),
                         orbitU.centerNbody,
                         typeof(NBody),
                         true);
            }

            evolveMode = (OrbitUniversal.EvolveMode)EditorGUILayout.EnumPopup(new GUIContent("Evolve Mode", modeTip), evolveMode);

            if ((evolveMode == OrbitUniversal.EvolveMode.SGP4_PROPAGATOR) &&
                (GravityEngine.Instance().units != GravityScaler.Units.ORBITAL)) {
                EditorGUILayout.LabelField("Warning: SGP4 Prop requires units = ORBITAL", EditorStyles.boldLabel);
            }

            // PKEPLER args
            if (evolveMode == OrbitUniversal.EvolveMode.PKEPLER_J2) {
                EditorGUILayout.LabelField("PKEPLER Drag Coeef.", EditorStyles.boldLabel);
                pk_ndot = EditorGUILayout.DoubleField(new GUIContent("Ndot", ndTip), orbitU.pkepler_ndot);
                pk_nddot = EditorGUILayout.DoubleField(new GUIContent("Nddot", nddTip), orbitU.pkepler_nddot);
            }

            inputMode = (OrbitUniversal.InputMode)
                EditorGUILayout.EnumPopup(new GUIContent("Parameter Choice", paramTip), orbitU.inputMode);

			//if (!(inputMode == OrbitUniversal.InputMode.JPL_EPHEMERIS) ||
			//	(inputMode == OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET)) {
				EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shape Parameters", EditorStyles.boldLabel);
			//}

			sizeUpdate = false;
            GravityScaler.Units units = GravityEngine.Instance().units;
            string promptp = string.Format("Semi-parameter (p) [{0}]", GravityScaler.LengthUnits(units));

            // The values for orbit size and shape can be entered in several ways. OrbitUniversal supports
            // these values as doubles (which is probably overkill in most case). Provide an explicit double mode
            // but also allow sliders (which reduce the value to a float). 

            // The editor script changes p_inspector. (p in OU is scaled for GE internal units)
            switch (inputMode)
            {

                case OrbitUniversal.InputMode.ELLIPSE_MAJOR_AXIS_A:
                    EditorGUILayout.LabelField("Ellipse with float/sliders using semi-major axis.");
                    ecc = EditorGUILayout.Slider(new GUIContent("Eccentricity", eTip), (float)orbitU.eccentricity, 0f, 0.99f);
                    GetMajorAxis(orbitU, ref p_inspector, ref sizeUpdate, units);
                    break;

                case OrbitUniversal.InputMode.ELLIPSE_APOGEE_PERIGEE:
                    EditorGUILayout.LabelField("Ellipse with float/sliders using agogee/perigee.");
                    EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                    double apogee_old = orbitU.GetApogeeInspector();
                    double apogee = EditorGUILayout.DelayedDoubleField(new GUIContent("Apogee", eTip), apogee_old);
                    double perigee_old = orbitU.GetPerigeeInspector();
                    double perigee = EditorGUILayout.DelayedDoubleField(new GUIContent("Perigee", eTip), perigee_old);
                    // enforce apogee > perigee
                    if (apogee < perigee)
                        apogee = perigee;

                    if (!EditorApplication.isPlaying && (apogee != apogee_old) || (perigee != perigee_old))
                    {
                        orbitU.SetSizeWithApogeePerigee(apogee, perigee);
                        sizeUpdate = true;
                        // Need to update ecc and p with new values
                        p_inspector = orbitU.p_inspector;
                        ecc = orbitU.eccentricity;
                    }
                    EditorGUILayout.LabelField(string.Format("Require Apogee > Perigee", ecc, p_inspector));
                    EditorGUILayout.LabelField(string.Format("Apogee/Perigee result in: eccentricty={0:0.00}, p={1:0.00}", ecc, p_inspector));
                    break;

                case OrbitUniversal.InputMode.ECC_PERIGEE:
                    EditorGUILayout.LabelField("Orbit with double using eccentricity/perigee.");
                    EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                    double old_ecc = orbitU.eccentricity;
                    ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                    double hperigee_old = orbitU.GetPerigeeInspector();
                    double hperigee = EditorGUILayout.DelayedDoubleField(new GUIContent("Perigee", eTip), hperigee_old);
                    if (!EditorApplication.isPlaying && ((hperigee != hperigee_old) || (old_ecc != ecc)))
                    {
                        orbitU.SetSizeWithEccPerigee(ecc, hperigee);
                        sizeUpdate = true;
                        // Need to update ecc and p with new values
                        p_inspector = orbitU.p_inspector;
                        ecc = orbitU.eccentricity;
                    }
                    EditorGUILayout.LabelField(string.Format("Apogee/Perigee result in: eccentricty={0:0.00}, p={1:0.00}", ecc, p_inspector));
                    break;


                case OrbitUniversal.InputMode.DOUBLE:
                    EditorGUILayout.LabelField("Specify values with double precision using semi-parameter");
                    EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                    // no sliders (they do float)
                    ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                    double old_p = orbitU.p_inspector;
                    p_inspector = EditorGUILayout.DelayedDoubleField(new GUIContent(promptp, pTip), orbitU.p_inspector);
                    if (old_p != p_inspector)
                    {
                        sizeUpdate = true;
                    }
                    break;

                case OrbitUniversal.InputMode.DOUBLE_ELLIPSE:
                    EditorGUILayout.LabelField("Specify values with double precision using semi-parameter");
                    EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                    // no sliders (they do float)
                    ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                    GetMajorAxis(orbitU, ref p_inspector, ref sizeUpdate, units);
                    break;

                //case OrbitUniversal.InputMode.JPL_EPHEMERIS:
                //	// data from https://ssd.jpl.nasa.gov/horizons.cgi
                //	if (GUILayout.Button("Open JPL Horizons In Browser")) {
                //		Application.OpenURL("https://ssd.jpl.nasa.gov/horizons.cgi");
                //	}
                //	EditorGUILayout.LabelField("Paste Orbital Elements from JPL Horizons");

                //	string newJplData = EditorGUILayout.TextArea(jplData);
                //	if (newJplData != jplData) {
                //		Undo.RecordObject(orbitU, "OrbitU Change");
                //		orbitU.InitFromJplEphemeris(newJplData);
                //	}
                //	if (newJplData != null) {
                //		displayAndExit = true;
                //		orbitU.inputMode = inputMode;
                //	}

                //	break;

                case OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET:
                    if (GravityEngine.Instance().units != GravityScaler.Units.ORBITAL) {
                        EditorGUILayout.LabelField("Warning: TLE input requires units = ORBITAL", EditorStyles.boldLabel);
                    }

                    // data from https://www.celestrak.com/NORAD/elements/
                    if (GUILayout.Button("Open Celestrak In Browser")) {
                        Application.OpenURL("https://www.celestrak.com/NORAD/elements/");
                    }
                    EditorGUILayout.LabelField("Paste Orbital Elements from Celestrak");

                    string newTleName = EditorGUILayout.TextField("Name", orbitU.tleName);
                    string newTleLine1 = EditorGUILayout.TextField("Line 1", orbitU.tleLine1);
                    string newTleLine2 = EditorGUILayout.TextField("Line 2", orbitU.tleLine2);
                    if ((newTleName != orbitU.tleName) || (newTleLine1 != orbitU.tleLine1) || (newTleLine2 != orbitU.tleLine2)) { 
                    }
                    if (GUILayout.Button("Update Orbit from TLE")) {
                        orbitU.InitCOEFromTLEData();
                        if (!EditorApplication.isPlaying && (orbitU.GetNBody() != null)) {
                            orbitU.GetNBody().EditorUpdate(GravityEngine.Instance());
                        }
                        // if PKEPLER mode, the ndot and nddot fields will be updated automatically
                        return;
                    }
                    if (GUI.changed) {
                        Undo.RecordObject(orbitU, "OrbitU Change");
                        orbitU.tleLine2 = newTleLine2;
                        orbitU.tleLine1 = newTleLine1;
                        orbitU.tleName = newTleName;
                        orbitU.inputMode = inputMode;
                        orbitU.evolveMode = evolveMode;
                        orbitU.pkepler_ndot = pk_ndot;
                        orbitU.pkepler_nddot = pk_nddot;
                        EditorUtility.SetDirty(orbitU);
                    }
                    if ((orbitU.GetSGP4ToGE() != null) && (orbitU.GetSGP4ToGE().GetSatData().error != 0)) {
                        EditorGUILayout.LabelField("TLE does not parse. Cannot show COE");
                        return;
                    }
                    displayAndExit = true;
                    EditorGUILayout.LabelField("Orbit at GE start time:", EditorStyles.boldLabel);
                    break;

                default:
                    Debug.LogWarning("Unknown input mode - internal error");
                    break;
            }
        }
        if (displayAndExit) {
            // TODO: Fixed width font
            EditorGUILayout.LabelField("Size:");
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Semi-Major Axis", "a", orbitU.GetMajorAxisInspector()), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} {1:0.00000}",
                "Periapsis", orbitU.GetPerigeeInspector()), EditorStyles.wordWrappedLabel);
            if (orbitU.eccentricity < 1.0) {
                EditorGUILayout.LabelField(string.Format("   {0,-25} {1:0.00000}",
                    "Apoapsis", orbitU.GetApogeeInspector()), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.LabelField("Shape and Tilt:");
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Eccentricity", "e", orbitU.eccentricity), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Incliniation", "i", orbitU.inclination), EditorStyles.wordWrappedLabel);

            string opLabel = "Orientation/Phase: ";
            if (orbitU.FromOrbitPredictor())
                opLabel += " (RVtoCOE Values)";
            EditorGUILayout.LabelField(opLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Arg. of pericenter", "\u03c9", orbitU.omega_lc ), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Longitude of node", "\u03a9", orbitU.omega_uc), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                "Phase", "", orbitU.phase ), EditorStyles.wordWrappedLabel);

            if (orbitU.FromOrbitPredictor()) {
                EditorGUILayout.LabelField("Orientation/Phase (Special Orbit Adjustment) [OP only]");
                (double oU, double oL, double ph) = orbitU.SpecialOrientationPhase();
                EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                    "Arg. of pericenter", "\u03c9", oL * Mathf.Rad2Deg), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                    "Longitude of node", "\u03a9", oU * Mathf.Rad2Deg), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2,15:0.00000}",
                    "Phase", "", ph * Mathf.Rad2Deg), EditorStyles.wordWrappedLabel);
            }

            if (evolveMode == OrbitUniversal.EvolveMode.SGP4_PROPAGATOR) {
                EditorGUILayout.LabelField("SGP4 Aux. Data");
                EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,-5})  {2:0.000E0}",
                    "Bstar", "", orbitU.sgp4_bstar), EditorStyles.wordWrappedLabel);

            } 
            if (inputMode == OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET) {
                EditorGUILayout.LabelField("TLE Epoch: " + orbitU.TLEStartEpoch());
            }
            // When running display the energy in inspector
            NBody nbody = orbitU.GetNBody();
            if ((nbody != null) && Application.IsPlaying(orbitU) && (nbody.engineRef != null)) {
                EditorGUILayout.LabelField(string.Format("   {0,-25} {1:0.00000}",
                     "Energy", orbitU.GetEnergy()), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(string.Format("   {0,-25} {1:0.00000}",  
                     "mu", orbitU.GetMu()), EditorStyles.wordWrappedLabel);
            }
            return;
        }

        if (!EditorApplication.isPlaying && (p_inspector != orbitU.p)) {
            sizeUpdate = true;
        }
        EditorGUILayout.LabelField("Scaled p (Unity units):   " + orbitU.p);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Orientation Parameters", EditorStyles.boldLabel);
        if ((inputMode != OrbitUniversal.InputMode.DOUBLE) && (inputMode != OrbitUniversal.InputMode.DOUBLE_ELLIPSE)) {
            // implementation uses AngleAxis, so degrees are more natural
            omega_uc = EditorGUILayout.Slider(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), (float) orbitU.omega_uc, 0, 360f);
            omega_lc = EditorGUILayout.Slider(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), (float) orbitU.omega_lc, 0, 360f);
            inclination = EditorGUILayout.Slider(new GUIContent("Inclination", inclinationTip), (float) orbitU.inclination, 0f, 180f);
            // physics uses radians - but ask user for degrees to be consistent
            phase = EditorGUILayout.Slider(new GUIContent("Starting Phase", phaseTip), (float) orbitU.phase, 0, 360f);
        } else {
            // DOUBLE, so no sliders
            omega_uc = EditorGUILayout.DoubleField(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), orbitU.omega_uc);
            omega_lc = EditorGUILayout.DoubleField(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), orbitU.omega_lc);
            inclination = EditorGUILayout.DoubleField(new GUIContent("Inclination", inclinationTip), orbitU.inclination);
            phase = EditorGUILayout.DoubleField(new GUIContent("Starting Phase", phaseTip), orbitU.phase);
        }

        // SGP4 aux data. If using SGP4 and not using a TLE to init, need to collect extra info.
        if ((evolveMode == OrbitUniversal.EvolveMode.SGP4_PROPAGATOR) &&
            (inputMode != OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET)) {
            EditorGUILayout.LabelField("SGP4 Prop. Aux Data", EditorStyles.boldLabel);
            double sgp4_bstar = EditorGUILayout.DoubleField(new GUIContent("Bstar", phaseTip), orbitU.sgp4_bstar);

            if (GUI.changed) {
                Undo.RecordObject(orbitU, "OrbitU Change");
                orbitU.sgp4_bstar = sgp4_bstar;
                EditorUtility.SetDirty(orbitU);
            }

        }

        if (EditorApplication.isPlaying && (evolveMode == OrbitUniversal.EvolveMode.KEPLERS_EQN)) {
            Vector3d r = Vector3d.zero;
            Vector3d v = Vector3d.zero;
            double t0 = 0;
            orbitU.GetRVT(ref r, ref v, ref t0);
            EditorGUILayout.LabelField(string.Format("KEPLER: t0={0} r={1} v={2}", t0, r, v));
        }

        if (GUI.changed) {
			Undo.RecordObject(orbitU, "OrbitU Change");
			orbitU.p_inspector = p_inspector; 
			orbitU.eccentricity = ecc; 
			orbitU.centerNbody = centerNBody;
			orbitU.omega_lc = omega_lc;
			orbitU.omega_uc = omega_uc;
			orbitU.inclination = inclination;
			orbitU.phase = phase;
            orbitU.inputMode = inputMode;
            orbitU.evolveMode = evolveMode;
            orbitU.pkepler_ndot = pk_ndot;
            orbitU.pkepler_nddot = pk_nddot;
            EditorUtility.SetDirty(orbitU);
		}

        if (sizeUpdate) {
            orbitU.ApplyScale(GravityEngine.Instance().GetLengthScale());
            if (!EditorApplication.isPlaying && (orbitU.GetNBody() != null)) {
                orbitU.GetNBody().EditorUpdate(GravityEngine.Instance());
            }
        }

    }

    private static void GetMajorAxis(OrbitUniversal orbitU, ref double p_inspector, ref bool sizeUpdate, GravityScaler.Units units) {
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = oldLabelWidth + 50f;
        string prompt = string.Format("Semi-Major Axis (a) [{0}]", GravityScaler.LengthUnits(units));
        double old_a = orbitU.GetMajorAxisInspector();
        double a = EditorGUILayout.DelayedDoubleField(new GUIContent(prompt, aTip), old_a);
        if (!EditorApplication.isPlaying && (a != old_a)) {
            orbitU.SetMajorAxisInspector(a);
            sizeUpdate = true;
            // Need to update ecc and p with new values
            p_inspector = orbitU.p_inspector;
        }
        EditorGUILayout.LabelField(string.Format("Axis result in:  p={0:0.00}", p_inspector));
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}
