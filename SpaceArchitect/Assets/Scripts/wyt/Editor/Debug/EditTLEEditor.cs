using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EditTLE), true)]

public class EditTLEEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditTLE editTLE = (EditTLE) target;
        GravityEngine ge = GravityEngine.Instance();

        OrbitUniversal ou = editTLE.GetComponent<OrbitUniversal>();
        if ((ou == null) || (ou.inputMode != OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET)) {
            EditorGUILayout.LabelField("No OrbitUniversal or not TLE input mode");
            return;
        }

        bool enable = EditorGUILayout.Toggle("enable", editTLE.enable);
        editTLE.enable = enable;

        if (enable) {
            // Grab TLE and put info in fields
            string[] tle2Fields = ou.tleLine2.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            double incl = double.Parse(tle2Fields[2]);
            double raan = double.Parse(tle2Fields[3]);
            double ecc = double.Parse("." + tle2Fields[4]);
            double argp = double.Parse(tle2Fields[5]);
            double meanA = double.Parse(tle2Fields[6]);
            EditorGUILayout.LabelField("Press <Enter> to update OrbitU TLE");
            incl = EditorGUILayout.DelayedDoubleField("Inclination ", incl);
            raan = EditorGUILayout.DelayedDoubleField("RAAN ", raan);
            ecc = EditorGUILayout.DelayedDoubleField("Eccentricity ", ecc);
            argp = EditorGUILayout.DelayedDoubleField("Arg. P ", argp);
            meanA = EditorGUILayout.DelayedDoubleField("Mean Anomoly ", meanA);

            if (GUI.changed) {
                Undo.RecordObject(editTLE, "TLE change");
                // Line2
                // Java code ignore checksum
                string line2 = "2 ";
                line2 += tle2Fields[1];
                line2 += string.Format(" {0:000.0000} ", incl);
                line2 += string.Format("{0:000.0000} ", raan);
                line2 += string.Format("{0:.0000000} ", ecc).Replace(".", ""); // decimal point is implied
                line2 += string.Format("{0:000.0000} ", argp);
                line2 += string.Format("{0:000.0000} ", meanA);
                line2 += tle2Fields[7];
                line2 += SGP4toGE.CheckSum(line2);
                ou.tleLine2 = line2;
                Debug.Log("Set TLE to " + line2);
                EditorUtility.SetDirty(editTLE);
            }
        }
    }
}
