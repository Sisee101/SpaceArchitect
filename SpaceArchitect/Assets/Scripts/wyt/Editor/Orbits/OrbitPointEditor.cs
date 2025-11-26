using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitPoint), true)]
public class OrbitPointEditor : Editor {

	private const string opTip = "Number of points in orbit path for renderer";
	private const string typeTip = "Type of point on the orbit";
	private const string altTip = "Altitude/radius at which point is to be placed.";
	private const string ftTip = "Time (GE units) ahead of body in orbit";
	private const string timeTip = "NBody to serve as reference for time ahead in orbit.";
	private const string phaseTip = "Phase in orbit (nu in orbit equations)";
	private const string mcTip = "Use mouse input directly. If not checked then control logic in another script must call ";
	private const string camTip = "Camera used to map mouse click into the 3D space to find closest point to click on orbit.";

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		OrbitPoint op = (OrbitPoint) target;

		OrbitPredictor orbitPred = (OrbitPredictor) EditorGUILayout.ObjectField(
				new GUIContent("Orbit Predictor", opTip),
				op.orbitPredictor,
				typeof(OrbitPredictor),
				true);

		OrbitPoint.PointType type = (OrbitPoint.PointType)EditorGUILayout.EnumPopup(new GUIContent("Type", typeTip), op.pointType);


		EditorGUILayout.LabelField("Parameters depend on type selected");

		double pointData = op.pointData;
		NBody timeBody = op.timeRefBody;
		bool mouseControl = op.mouseControl;
		Camera sceneCamera = op.sceneCamera; 

        switch(type)
        {
			case OrbitPoint.PointType.ALTITUDE_1ST:
			case OrbitPoint.PointType.ALTITUDE_2ND:
				pointData = EditorGUILayout.DoubleField(new GUIContent("Altitude", altTip), op.pointData);
				break;

			case OrbitPoint.PointType.APOAPSIS:
			case OrbitPoint.PointType.PERIAPSIS:
			case OrbitPoint.PointType.ASCENDING_NODE:
			case OrbitPoint.PointType.DESCENDING_NODE:
				// none
				EditorGUILayout.LabelField("No params required");
				break;

			case OrbitPoint.PointType.FIXED_TIME:
				timeBody = (NBody)EditorGUILayout.ObjectField(
						new GUIContent("NBody time reference", timeTip),
						op.timeRefBody,
						typeof(NBody),
						true);
				pointData = EditorGUILayout.DoubleField(new GUIContent("Time ahead of body", ftTip), op.pointData);
				break;

			case OrbitPoint.PointType.PHASE:
				pointData = EditorGUILayout.DoubleField(new GUIContent("Phase", phaseTip), op.pointData);
				break;

			case OrbitPoint.PointType.PHASE_FROM_MOUSE:
				sceneCamera = (Camera)EditorGUILayout.ObjectField(
						new GUIContent("Scene Camera", camTip),
						op.sceneCamera,
						typeof(Camera),
						true);
				mouseControl = EditorGUILayout.Toggle(new GUIContent("Direct Mouse Control", mcTip), mouseControl);
				break;

			default:
				Debug.LogWarning("Code stale. Editor does not know about this type");
				break;

		}


		if (GUI.changed) {
			Undo.RecordObject(op, "OrbitPoint Change");
			op.orbitPredictor = orbitPred;
			op.pointType = type;
			op.pointData = pointData;
			op.timeRefBody = timeBody;
			op.mouseControl = mouseControl;
			op.sceneCamera = sceneCamera;
			EditorUtility.SetDirty(op);
		}

	}
}
