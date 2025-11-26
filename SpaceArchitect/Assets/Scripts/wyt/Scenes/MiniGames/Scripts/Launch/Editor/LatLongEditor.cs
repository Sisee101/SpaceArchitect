using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LatLongPoint), true)]
public class LatLongEditor : Editor
{
    public override void OnInspectorGUI()
    {
		LatLongPoint llPoint = (LatLongPoint)target;
		EditorGUILayout.LabelField("Press <ENTER> to update value");
		double lat = EditorGUILayout.DelayedDoubleField("Latitude", llPoint.latitude);
		EditorGUILayout.LabelField("Positive longitude is east.");
		double longitude = EditorGUILayout.DelayedDoubleField("Longitude", llPoint.longitude);
		bool raycast = EditorGUILayout.Toggle("Raycast to Surface", llPoint.rayCast);
		EditorGUILayout.LabelField("(raycast requires a mesh collider with convex=false)");

		GameObject go = (GameObject) EditorGUILayout.ObjectField("Sphere Model", llPoint.sphereModel, typeof(GameObject), true);
		llPoint.ComputePosition();
		EditorGUILayout.LabelField(llPoint.Info());
		if (GUI.changed)
		{
			Undo.RecordObject(llPoint, "LatLong Change");
			llPoint.latitude = lat;
			llPoint.longitude = longitude;
			llPoint.sphereModel = go;
			llPoint.rayCast = raycast;
			EditorUtility.SetDirty(llPoint);
		}
	}
}
