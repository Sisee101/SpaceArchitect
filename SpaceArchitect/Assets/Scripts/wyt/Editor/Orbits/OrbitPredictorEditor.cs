using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitPredictor), true)]
public class OrbitPredictorEditor : Editor {

	private const string centerTip = "NBody that the body is in orbit around.";
	private const string bodyTip = "NBody for orbit prediction.";
	private const string rTip = "Number of points to use in line renderering of orbit.";
    private const string vTip = "Velocity will be set explicitly by a script (do not ask GE for velocity every frame)";
    private const string pTip = "Position will be set explicitly by a script (do not ask GE for position every frame)";
    private const string vfTip = "Initial setting of velocity from script";
    private const string pfTip = "Initial setting of position from script";

    private const string ppTip = "Number of points to drop from orbit to the specified plane to show inclination";
    private const string pnTip = "Normal to plane to show orbit inclination. Typically (0, 0, 1) XY or (0, 1, 0) XZ mode";

    private const string hyperTip = "When the orbit is a hyperbola it will be shown out to the readius specified by this value";

    private const string djTip = "Apply hysteresis to debounce changes to/from circular and inclined orbit modes";

    public override void OnInspectorGUI()
	{
		GUI.changed = false;
		OrbitPredictor orbit = (OrbitPredictor) target;

		GameObject body; 
		GameObject centerObject;
        bool vFromScript = orbit.velocityFromScript;
        bool pFromScript = orbit.positionFromScript;

        int numPoints;

        centerObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("CenterObject", centerTip),
                orbit.centerBody,
                typeof(GameObject),
                true);
 
        body = (GameObject) EditorGUILayout.ObjectField(
				new GUIContent("Body", bodyTip), 
				orbit.body,
				typeof(GameObject), 
				true);

        Vector3 pos = Vector3.zero;
        Vector3 vel = Vector3.zero;

        vFromScript = EditorGUILayout.Toggle(new GUIContent("Velocity From Script", vTip), vFromScript);
        if (vFromScript) {
            if (!Application.IsPlaying(orbit)) {
                vel = EditorGUILayout.Vector3Field(new GUIContent("Velocity (GE)", vfTip), orbit.velocity.ToVector3());
            } else {
                EditorGUILayout.LabelField("Runtime: v=" + orbit.velocity);
            }
        }


        pFromScript = EditorGUILayout.Toggle(new GUIContent("Position From Script", pTip), pFromScript);
        if (pFromScript) {
            if (!Application.IsPlaying(orbit)) {
                pos = EditorGUILayout.Vector3Field(new GUIContent("Position (GE)", pfTip), orbit.position.ToVector3());
            } else {
                EditorGUILayout.LabelField("Runtime pos=" + orbit.position);
            }
        }

        bool dejitter = EditorGUILayout.Toggle(new GUIContent("Dejitter Orbit Elements", djTip), orbit.dejitterCOE);


        EditorGUILayout.LabelField("Plot Parameters", EditorStyles.boldLabel);

        float hyperR = EditorGUILayout.FloatField(new GUIContent("Hyper Display Radius", hyperTip), orbit.hyperDisplayRadius);

        numPoints = EditorGUILayout.IntField(new GUIContent("Number of Points", rTip), orbit.numPoints);

        int projPoints = EditorGUILayout.IntField(new GUIContent("Number of Projection Points", ppTip), orbit.numPlaneProjections);

        Vector3 planeNormal = EditorGUILayout.Vector3Field(new GUIContent("Plane Normal", pnTip), orbit.planeNormal);

        bool segmentFoldout = EditorGUILayout.Foldout(orbit.segmentFoldout, "Orbit Segment (optional)");
        bool retrograde = orbit.retrograde;
        bool showSegment = orbit.showSegment;
        GameObject segmentEnd = orbit.segmentEnd;
        LineRenderer segmentLR = orbit.segementLineR;
        if (segmentFoldout) {
            showSegment = EditorGUILayout.Toggle(new GUIContent("Show Orbit Segment", pTip), orbit.showSegment);
            segmentEnd = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Segment End", bodyTip),
                    segmentEnd,
                    typeof(GameObject),
                    true);
            segmentLR = (LineRenderer)EditorGUILayout.ObjectField(
                    new GUIContent("Segment Line", bodyTip),
                    segmentLR,
                    typeof(LineRenderer),
                    true);
            retrograde = EditorGUILayout.Toggle(new GUIContent("Retrograde", pTip), retrograde);

        }


        if (GUI.changed) {
			Undo.RecordObject(orbit, "OrbitPredictor Change");
			orbit.centerBody = centerObject;
			orbit.body = body;
			orbit.numPoints = numPoints;
            orbit.velocityFromScript = vFromScript;
            orbit.editorVel = vel;
            orbit.positionFromScript = pFromScript;
            orbit.editorPos = pos;
            orbit.hyperDisplayRadius = hyperR;
            orbit.numPlaneProjections = projPoints;
            orbit.planeNormal = planeNormal;
            // segment
            orbit.segmentFoldout = segmentFoldout;
            orbit.showSegment = showSegment;
            orbit.segmentEnd = segmentEnd;
            orbit.segementLineR = segmentLR;
            orbit.retrograde = retrograde;
            orbit.dejitterCOE = dejitter;
			EditorUtility.SetDirty(orbit);
		}	
	}
}
