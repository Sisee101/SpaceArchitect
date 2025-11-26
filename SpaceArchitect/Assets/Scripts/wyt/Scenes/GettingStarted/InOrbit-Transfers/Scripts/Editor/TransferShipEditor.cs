using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TransferShip), true)]
public class TransferShipEditor : Editor

{
    private static string modeTip = "Type of transfer to be performed.";
    private static string targetTip = "Target for rendezvous when using HOHMANN_RDVS, LAMBERT_TARGET or LAMBERT_RDVS modes";
    private static string targetOrbitTip = "Used when HOHMANN or LAMBERT_ORBIT selected to indicate target orbit. (Lambert also needs targetPhase)";
    private static string targetPointTip = "Used when LAMBERT_POINT is selected to designate destination point";
    private static string targetPhaseTip = "Phase in target orbit that specifies the destination of transfer";
    private static string targetTFTip = "Time factor for Lambert (1.0=time for lowest energy path)";
    private static string targetShortTip = "Reverse the path taken (go opposite to the current orbital direction)";

    private const string impactTip = "Check if Lambert transfer will impact the planet";
    private const string radiusTip = "Radius of planet (in GE internal units)";

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        TransferShip transferShip = (TransferShip)target;

        TransferShip.Transfer mode = transferShip.GetTransferType();

        mode = (TransferShip.Transfer)
            EditorGUILayout.EnumPopup(new GUIContent("Parameter Choice", modeTip), mode);

        NBody targetNbody = transferShip.targetNbody;
        OrbitUniversal targetOrbit = transferShip.targetOrbit;
        Vector3 targetPoint = transferShip.GetTargetPoint();
        float targetPhase = transferShip.GetTargetPhase();
        float oldTimeFactor = transferShip.GetTransferTimeFactor();
        float newTimeFactor = oldTimeFactor;
        bool flipPath = transferShip.GetLambertReversePath();

        EditorGUILayout.LabelField("Input fields vary based on transfer type");

        bool checkPlanet = false;

        switch(mode) {
            case TransferShip.Transfer.CIRCULARIZE:
                break;

            case TransferShip.Transfer.HOHMANN:
                targetOrbit = (OrbitUniversal)EditorGUILayout.ObjectField(
                        new GUIContent("Target Orbit", targetOrbitTip),
                        targetOrbit,
                        typeof(OrbitUniversal),
                        true);
                EditorGUILayout.LabelField("OR if Orbit is null use:");
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("Target NBody", targetTip),
                        targetNbody,
                        typeof(NBody),
                        true);
                break;

            case TransferShip.Transfer.LAMBERT_ORBIT:
                targetOrbit = (OrbitUniversal)EditorGUILayout.ObjectField(
                        new GUIContent("Target Orbit", targetOrbitTip),
                        targetOrbit,
                        typeof(OrbitUniversal),
                        true);
                EditorGUILayout.LabelField("OR if Orbit is null use:");
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("Target NBody", targetTip),
                        targetNbody,
                        typeof(NBody),
                        true);
                targetPhase = EditorGUILayout.FloatField(new GUIContent("Target Phase", targetPhaseTip), targetPhase);
                newTimeFactor = EditorGUILayout.FloatField(new GUIContent("Time Factor", targetTFTip), oldTimeFactor);
                flipPath = EditorGUILayout.Toggle(new GUIContent("Reverse Path", targetShortTip), flipPath);
                checkPlanet = true;
                break;

            case TransferShip.Transfer.LAMBERT_MINDVSQ:
                targetOrbit = (OrbitUniversal)EditorGUILayout.ObjectField(
                        new GUIContent("Target Orbit", targetOrbitTip),
                        targetOrbit,
                        typeof(OrbitUniversal),
                        true);
                EditorGUILayout.LabelField("OR if Orbit is null use:");
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("Target NBody", targetTip),
                        targetNbody,
                        typeof(NBody),
                        true);
                targetPhase = EditorGUILayout.FloatField(new GUIContent("Target Phase", targetPhaseTip), targetPhase);
                checkPlanet = true;
                break;

            case TransferShip.Transfer.HOHMANN_RDVS:
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("Target NBody", targetTip),
                        targetNbody,
                        typeof(NBody),
                        true);
                checkPlanet = true;
                break;

            case TransferShip.Transfer.LAMBERT_RDVS:
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                        new GUIContent("Target NBody", targetTip),
                        targetNbody,
                        typeof(NBody),
                        true);
                newTimeFactor = EditorGUILayout.FloatField(new GUIContent("Time Factor", targetTFTip), oldTimeFactor);
                flipPath = EditorGUILayout.Toggle(new GUIContent("Reverse Path", targetShortTip), flipPath);
                checkPlanet = true;
                break;

            case TransferShip.Transfer.LAMBERT_INTERCEPT:
                targetNbody = (NBody)EditorGUILayout.ObjectField(
                         new GUIContent("Target NBody", targetTip),
                         targetNbody,
                         typeof(NBody),
                         true);
                flipPath = EditorGUILayout.Toggle(new GUIContent("Reverse Path", targetShortTip), flipPath);
                newTimeFactor = EditorGUILayout.FloatField(new GUIContent("Time Factor", targetTFTip), oldTimeFactor);
                checkPlanet = true;
                break;

            case TransferShip.Transfer.LAMBERT_POINT:
                targetPoint = EditorGUILayout.Vector3Field(new GUIContent("Target Point", targetPointTip), targetPoint);
                newTimeFactor = EditorGUILayout.FloatField(new GUIContent("Time Factor", targetTFTip), oldTimeFactor);
                flipPath = EditorGUILayout.Toggle(new GUIContent("Reverse Path", targetShortTip), flipPath);
                checkPlanet = true;
                break;
        }

        bool planetCheck = transferShip.checkHit;
        double planetRadius = transferShip.planetRadius;
        if (checkPlanet) {
            planetCheck = EditorGUILayout.Toggle(new GUIContent("Check Planet Impact", impactTip), planetCheck);
            planetRadius = EditorGUILayout.DoubleField(new GUIContent("Planet Radius", radiusTip), planetRadius);
        }

        if (GUI.changed) {
            Undo.RecordObject(transferShip, "TransferShip Change");
            transferShip.SetTransferType(mode);
            transferShip.targetNbody = targetNbody;
            transferShip.targetOrbit = targetOrbit;
            transferShip.SetTargetPoint(targetPoint);
            transferShip.SetTargetPhase(targetPhase);
            transferShip.SetLambertReversePath(flipPath);
            transferShip.checkHit = planetCheck;
            transferShip.planetRadius = planetRadius;
            if (newTimeFactor != oldTimeFactor) {
                transferShip.SetTransferTimeFactor(newTimeFactor);
            }
        }
    }
}
