using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GECOE), true)]

public class GECOE_editor : Editor
{

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        GECOE gecoe = (GECOE) target;
        GravityEngine ge = GravityEngine.Instance();

        NBody ship = (NBody)EditorGUILayout.ObjectField(
          "Ship",
          gecoe.ship,
          typeof(NBody),
          true);

        NBody center = (NBody)EditorGUILayout.ObjectField(
                   "Center",
                   gecoe.center,
                   typeof(NBody),
                   true);

        if (ge != null) {
            if (Application.isPlaying) {
                Vector3d rShip = ge.GetPositionDoubleV3(ship);
                Vector3d vShip = ge.GetVelocityDoubleV3(ship);
                OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(rShip, vShip, center, relativePos: false);
                EditorGUILayout.LabelField("COE");
                EditorGUILayout.LabelField(string.Format(" type= {0}", coe.typeOrbit));
                EditorGUILayout.LabelField(string.Format(" a= {0}", coe.a));
                EditorGUILayout.LabelField(string.Format(" p= {0}", coe.p));
                EditorGUILayout.LabelField(string.Format(" e= {0}", coe.ecc));
                EditorGUILayout.LabelField(string.Format(" i= {0}", coe.incl));
                EditorGUILayout.LabelField(string.Format(" raan= {0}", coe.raan));
                EditorGUILayout.LabelField(string.Format(" argp= {0}", coe.argp));
                EditorGUILayout.LabelField(string.Format(" nu= {0}", coe.nu));
                EditorGUILayout.LabelField(string.Format(" m= {0}", coe.m));
                EditorGUILayout.LabelField(string.Format(" eccanom= {0}", coe.eccanom));
                EditorGUILayout.LabelField(string.Format(" arglat= {0}", coe.arglat));
                EditorGUILayout.LabelField(string.Format(" truelon= {0}", coe.truelon));
                EditorGUILayout.LabelField(string.Format(" lonper= {0}", coe.lonper));

            } else {
                EditorGUILayout.LabelField("COE (OrbitUtils.OrbitElements");
                EditorGUILayout.LabelField("Will be displayed when running");
            }
        }

        if (GUI.changed) {
            Undo.RecordObject(gecoe, "GEOC Change");
            gecoe.ship = ship;
            gecoe.center = center;
            EditorUtility.SetDirty(gecoe);
        }
    }
}
