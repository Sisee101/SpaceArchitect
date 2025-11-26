using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;

[CustomEditor(typeof(CelestrakSceneBuilder), true)]

public class CelestrakSceneBuilderEditor : Editor
{
	private const string parentTip = "Make satellites children of this game object";
	private const string prefabTip = "Prefab to instantiate for each satellite. Must have an NBody and OrbitUniversal component";
	private const string centerTip = "Center body around which prefab will orbit.";

	public override void OnInspectorGUI()
    {
        GUI.changed = false;
		CelestrakSceneBuilder csb = (CelestrakSceneBuilder)target;

		GameObject prefab = (GameObject)EditorGUILayout.ObjectField(
				new GUIContent("Satellite Prefab)", prefabTip),
				csb.satellitePrefab,
				typeof(GameObject),
				true);

		NBody centerBody = (NBody)EditorGUILayout.ObjectField(
				new GUIContent("Center Nbody", centerTip),
				csb.centerBody,
				typeof(NBody),
				true);

		string fileName = EditorGUILayout.TextField("Filename", csb.fileName);

		GameObject parent = (GameObject)EditorGUILayout.ObjectField(
		  new GUIContent("Parent (optional)", parentTip),
		  csb.parent,
		  typeof(GameObject),
		  true);

		if (GUILayout.Button("Create Satellites")) {
			string errStr = csb.CreateSatellites();
			if (errStr == null) {
				errStr = "Created";
			}
			csb.lastMessage = errStr;
		}
		EditorGUILayout.LabelField(csb.lastMessage, EditorStyles.boldLabel);

		if (GUI.changed) {
			Undo.RecordObject(csb, "CelestrakBuilder Change");
			csb.fileName = fileName;
			csb.parent = parent;
			csb.satellitePrefab = prefab;
			csb.centerBody = centerBody;
			EditorUtility.SetDirty(csb);
		}
	}
}
