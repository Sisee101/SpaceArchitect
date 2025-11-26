using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GEOrbitCompare), true)]

public class GEOrbitCompareEditor : Editor
{

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        GravityEngine ge = GravityEngine.Instance();
        GEOrbitCompare geoc = (GEOrbitCompare)target;

        NBody body1 = (NBody)EditorGUILayout.ObjectField(
                  "Body 1",
                  geoc.body1,
                  typeof(NBody),
                  true);

        NBody body2 = (NBody)EditorGUILayout.ObjectField(
                   "Body 2",
                   geoc.body2,
                   typeof(NBody),
                   true);


        if (ge != null) {
            if (Application.isPlaying) {
                EditorGUILayout.LabelField("Orbit Delta:");
                EditorGUILayout.TextArea(geoc.GetOrbitDiff());
                EditorUtility.SetDirty(geoc);

            } else {
                EditorGUILayout.LabelField("Orbit Comparison:");
                EditorGUILayout.LabelField("Will be displayed when running");
            }
        }

        if (GUI.changed) {
            Undo.RecordObject(geoc, "GEOC Change");
            geoc.body1 = body1;
            geoc.body2 = body2;
            EditorUtility.SetDirty(geoc);
        }
    }
}
