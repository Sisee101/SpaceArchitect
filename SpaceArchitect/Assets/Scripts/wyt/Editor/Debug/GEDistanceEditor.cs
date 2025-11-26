using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GEDistance), true)]

public class GEDistanceEditor : Editor
{

    public override void OnInspectorGUI()
    {
        GravityEngine ge = GravityEngine.Instance();
        GEDistance ged = (GEDistance)target;

        NBody body1 = (NBody)EditorGUILayout.ObjectField(
                  "Body 1",
                  ged.body1,
                  typeof(NBody),
                  true);

        NBody body2 = (NBody)EditorGUILayout.ObjectField(
                   "Body 2",
                   ged.body2,
                   typeof(NBody),
                   true);

        if (ge != null) {
            if (Application.isPlaying) {
                EditorGUILayout.LabelField("GE Distance:");
                EditorGUILayout.TextArea(ged.LogDistance());
                GUI.changed = true;

            } else {
                EditorGUILayout.LabelField("GE Distance");
                EditorGUILayout.LabelField("Will be displayed when running");
            }
        }

        if (GUI.changed) {
            Undo.RecordObject(ged, "GED Change");
            ged.body1 = body1;
            ged.body2 = body2;
            EditorUtility.SetDirty(ged);
        }
    }
}
