using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(LambertPorkchop), true)]
public class LambertPorkchopEditor : Editor
{

    private const string c3MeshTip = "Porkchop mesh to show C3: excess energy at departure";
    private const string vdMeshTip = "Porkchop mesh to show Vdepart: magnitude of velocity to depart";
    private const string vaMeshTip = "Porkchop mesh to show Varrive: magnitude of velocity to arrive";
    private const string vtMeshTip = "Porkchop mesh to show Vtotal3: magnitude of (Vdepart + Varrive)";

    private const string fopTip = "Orbit predictor to show the transfer selected by clicking on the plot.\n" +
        "Does not require a associated NBody.";

    private const string markTip = "Game object to serve as a marker for the arrival location when an orbit is shown " +
        "by clicking on the plot";

    private const string ftextTip = "UI text field used to display the detailed values of a location clicked on a plot";
                                  

    private const string modeTip = "Input as:\n  RELATIVE: depart/arrive in terms of orbit periods\n  ABSOLUTE: specific times for depart/arrive windows";

    private const string dsTip = "Absolute departure start time for porkchop plot in GE time units";
    private const string dendTip = "Absolute departure end time for porkchop plot in GE time units";
    private const string asTip = "Absolute arrival start time for porkchop plot in GE time units";
    private const string aendTip = "Absolute arrival end time for porkchop plot in GE time units";

    private const string minFlightTip = "Minimum transfer time in GE time units";

    private const string relDepartTip = "Departure time interval expressed as number of departure orbit periods";
    private const string relMinFlight = "Relative minimum flight time expressed as a fraction of the transfer time if a Hohmann transfer was possible (e.g. 0.5)";
    private const string relMaxFlight = "Relative maximum flight time expressed as a fraction of the transfer time if a Hohmann transfer was possible (e.g. 1.5)";

    private const string depIntTip = "Number of intervals to use in calculating the data points to span the departure time range (e.g. 40)";
    private const string arrIntTip = "Number of intervals to use in calculating the data points to span the arrival time range (e.g. 40)";
    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        LambertPorkchop lp = (LambertPorkchop) target;


        PorkchopMesh vDepartMesh = lp.vDepartMesh;

        // visualization
        NBody fromNbody = (NBody)EditorGUILayout.ObjectField(
                                new GUIContent("From NBody", c3MeshTip),
                                lp.fromNbody, typeof(NBody), true);

        NBody toNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("To NBody", c3MeshTip),
                        lp.toNBody, typeof(NBody), true);

        // input mode
        LambertPorkchop.InputMode mode = (LambertPorkchop.InputMode) 
                    EditorGUILayout.EnumPopup(new GUIContent("Input Mode", modeTip), lp.inputMode);
        // abs
        double departStart = lp.departureStart;
        double departEnd = lp.departureEnd;
        double arriveStart = lp.arrivalStart;
        double arriveEnd = lp.arrivalEnd;
        double minFlight = lp.minFlightTime;
        // rel
        double departNumOrbits = lp.departNumOrbits;
        double minFlightTimeHR = lp.minFlightTimeHohRel;
        double maxFlightTimeHR = lp.maxFlightTimeHohRel;

        switch (mode) {
            case LambertPorkchop.InputMode.ABSOLUTE:
                departStart =  EditorGUILayout.DoubleField(new GUIContent("Depart Start Time", dsTip), departStart);
                departEnd = EditorGUILayout.DoubleField(new GUIContent("Depart End Time", dendTip), departEnd);
                arriveStart = EditorGUILayout.DoubleField(new GUIContent("Arrival Start Time", asTip), arriveStart);
                arriveEnd = EditorGUILayout.DoubleField(new GUIContent("Arrival End Time", aendTip), arriveEnd);
                minFlight = EditorGUILayout.DoubleField(new GUIContent("Min Flight Time", minFlightTip), lp.minFlightTime);
                break;

            case LambertPorkchop.InputMode.RELATIVE:
                departNumOrbits = EditorGUILayout.DoubleField(new GUIContent("Depart Num Orbits", relDepartTip), departNumOrbits);
                minFlightTimeHR = EditorGUILayout.DoubleField(new GUIContent("Min Flight (Relative to Hoh xfer)", relMinFlight), minFlightTimeHR);
                maxFlightTimeHR = EditorGUILayout.DoubleField(new GUIContent("Max Flight (Relative to Hoh xfer)", relMaxFlight), maxFlightTimeHR);
                break;

            default:
                Debug.LogError("Unsupported case");
                break;
        }

        // intervals
        EditorGUILayout.LabelField("Plot Intervals", EditorStyles.boldLabel);
        int departIntervals = EditorGUILayout.IntField(new GUIContent("Depart Intervals", depIntTip), lp.departureIntervals);
        int arriveIntervals = EditorGUILayout.IntField(new GUIContent("Arrival Intervals", arrIntTip), lp.arrivalIntervals);


        // meshes
        EditorGUILayout.LabelField("Display Meshes (Porkchop Mesh)", EditorStyles.boldLabel);
        PorkchopMesh c3Mesh = (PorkchopMesh)EditorGUILayout.ObjectField(
                                new GUIContent("C3 Mesh", c3MeshTip),
                                lp.c3Mesh, typeof(PorkchopMesh), true);
        PorkchopMesh vdMesh = (PorkchopMesh)EditorGUILayout.ObjectField(
                                new GUIContent("Vdepart Mesh", vdMeshTip),
                                lp.vDepartMesh, typeof(PorkchopMesh), true);
        PorkchopMesh vaMesh = (PorkchopMesh)EditorGUILayout.ObjectField(
                                new GUIContent("Varrive Mesh", vaMeshTip),
                                lp.vArriveMesh, typeof(PorkchopMesh), true);
        PorkchopMesh vtMesh = (PorkchopMesh)EditorGUILayout.ObjectField(
                                new GUIContent("Vtotal Mesh", vtMeshTip),
                                lp.vTotalMesh, typeof(PorkchopMesh), true);

        // visualization
        EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
        OrbitPredictor op = (OrbitPredictor)EditorGUILayout.ObjectField(
                                new GUIContent("From OrbitPredictor", fopTip),
                                lp.fromPredictor, typeof(OrbitPredictor), true);
        GameObject toMarker = (GameObject)EditorGUILayout.ObjectField(
                                new GUIContent("Destination Marker", markTip),
                                lp.toMarker, typeof(GameObject), true);

        Text flightText = (Text)EditorGUILayout.ObjectField(
                                new GUIContent("Flight Text", ftextTip),
                                lp.flightInfoText, typeof(Text), true);
        if (GUI.changed) {
            Undo.RecordObject(lp, "LambertPorkchop Change");
            lp.inputMode = mode;
            lp.fromNbody = fromNbody;
            lp.toNBody = toNbody;
            // times
            lp.departureStart = departStart;
            lp.departureEnd = departEnd;
            lp.arrivalStart = arriveStart;
            lp.arrivalEnd = arriveEnd;
            // rel
            lp.departNumOrbits = departNumOrbits;
            lp.minFlightTimeHohRel = minFlightTimeHR;
            lp.maxFlightTimeHohRel = maxFlightTimeHR;
            // intervals
            lp.departureIntervals = departIntervals;
            lp.arrivalIntervals = arriveIntervals;
            lp.minFlightTime = minFlight;
            // mesh
            lp.c3Mesh = c3Mesh;
            lp.vDepartMesh = vdMesh;
            lp.vArriveMesh = vaMesh;
            lp.vTotalMesh = vtMesh;
            // visualization
            lp.fromPredictor = op;
            lp.toMarker = toMarker;
            lp.flightInfoText = flightText;
            EditorUtility.SetDirty(lp);
        }
    }
}
