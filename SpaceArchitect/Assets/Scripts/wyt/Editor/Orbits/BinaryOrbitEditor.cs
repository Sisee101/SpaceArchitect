using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BinaryOrbit), true)]
public class BinaryOrbitEditor : OrbitUniversalEditor {

	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		BinaryOrbit bPair = (BinaryOrbit) target;
		Vector3 velocity = Vector3.zero;

		GravityScaler.Units units = GravityEngine.Instance().units;
		string prompt = string.Format("Velocity ({0})", GravityScaler.VelocityUnits(units));
		velocity = EditorGUILayout.Vector3Field(new GUIContent(prompt, "velocity of binary center of mass"), bPair.velocity);

        base.OnInspectorGUI();
        if (GUI.changed) {
			Undo.RecordObject(bPair, "BinaryOrbit Change");
			bPair.velocity = velocity;
			bPair.SetupOrbits();
			EditorUtility.SetDirty(bPair);
		}	

	}
}
