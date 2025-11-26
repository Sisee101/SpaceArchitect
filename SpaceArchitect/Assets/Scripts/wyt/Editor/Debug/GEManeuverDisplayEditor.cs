using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GEManeuuverDisplay), true)]

public class GEManeuverDisplayEditor : Editor
{

    public override void OnInspectorGUI()
    {
        GravityEngine ge = GravityEngine.Instance();

        if (ge != null) {
            if (Application.isPlaying) {
                EditorGUILayout.LabelField("Manuevers Pending:");
                EditorGUILayout.TextArea(ge.GetWorldState().maneuverMgr.DumpAll());
                EditorGUILayout.LabelField(string.Format("time={0:00.0}", ge.GetPhysicalTime() ));
                GUI.changed = true;

            } else {
                EditorGUILayout.LabelField("Manuevers Pending:");
                EditorGUILayout.LabelField("Will be displayed when running");
            }
        }
    }
}
