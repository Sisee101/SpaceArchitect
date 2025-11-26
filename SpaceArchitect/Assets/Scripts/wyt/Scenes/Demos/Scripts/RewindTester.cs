using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arbitrary controller code to apply certain events in the forward direction
/// to test rewind.
///
/// Press 'R' to trigger rewind.
///
/// TestCases:
///
/// - velocity change (ApplyImpulse to non-Kepler goes this path)
/// - ApplyImpulse to Kepler (changes r0, v0, t0)
/// - Maneuver (from a transfer ship, since most common case)
/// - Add each of NBody, OrbitU, SGP4, FF
/// - Remove each of NBody, OrbitU, SGP4, FF
/// 
/// </summary>
public class RewindTester : MonoBehaviour
{
    [Header("Press R to Rewind")]
    public NBody nbodyImpulse;

    public NBody keplerImpulse;

    public TransferShip transferShip;

    [Header("Inactive object to add")]
    public GameObject nbodyToAdd;
    public GameObject keplerToAdd;

    [Header("Active object to remove")]
    public GameObject nbodyToRemove;

    public GameObject keplerToRemove;

    public Text logText;

    private double lastTime; 
    private GravityEngine ge;


    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        lastTime = ge.GetPhysicalTime();

        // add a callback for rewind events. Will not handle them so return false.
        ge.GetWorldState().GetGERewindMgr().SetRewindCallback(RewindCallback);
    }

    private bool RewindCallback(GERewindMgr.RewindEntry rewindEntry)
    {
        LogAndDisplay("Callback for: " + rewindEntry.ToString());
        return false;
    }

    private bool RunAtTime (double tEvent, double tNow)
    {
        return (tEvent < tNow) && (tEvent > lastTime);
    }

    private void LogAndDisplay(string s)
    {
        string log = string.Format("t={0} {1}", ge.GetTimeWorldSeconds(), s);
        logText.text = log;
        Debug.Log(log);
    }

    // Update is called once per frame
    void Update()
    {
        // very simple time triggered control code for testing
        double t = ge.GetTimeWorldSeconds();

        if (!ge.GetTimeReversed()) {
            if (RunAtTime(2.5, t)) {
                Vector3d v = ge.GetVelocityDoubleV3(nbodyImpulse);
                ge.ApplyImpulse(nbodyImpulse, v.ToVector3() * 0.1f);
                LogAndDisplay("Applied impulse to nbody");
            }
            if (RunAtTime(3.5, t)) {
                Vector3d v = ge.GetVelocityDoubleV3(keplerImpulse);
                ge.ApplyImpulse(keplerImpulse, v.ToVector3() * 0.1f);
                LogAndDisplay("Applied impulse to Kepler");
            }            // Insert maneuvers for a transfer
            if (RunAtTime(5.0, t)) {
                transferShip.DoTransfer(null);
                LogAndDisplay("Transfer ship");
            }
            // Add an object to the scene (use an inactive FF GEO satellite)
            if (RunAtTime(7.0, t)) {
                nbodyToAdd.SetActive(true);
                ge.AddBody(nbodyToAdd);
                LogAndDisplay("Add to GE: " + nbodyToAdd.name);
            }
            // Add an object to the scene (use an inactive FF GEO satellite)
            if (RunAtTime(9.0, t)) {
                ge.RemoveBody(nbodyToRemove);
                nbodyToRemove.SetActive(false);
                LogAndDisplay("Remove from GE: " + nbodyToRemove.name);
            }
            if (RunAtTime(10.0, t)) {
                ge.RemoveBody(keplerToRemove);
                keplerToRemove.SetActive(false);
                LogAndDisplay("Remove from GE: " + keplerToRemove.name);
            }
            // Add an object to the scene (use an inactive FF GEO satellite)
            if (RunAtTime(11.0, t)) {
                keplerToAdd.SetActive(true);
                ge.AddBody(keplerToAdd);
                LogAndDisplay("Add to GE: " + keplerToAdd.name);
            }
        }
        lastTime = t;

        if (Input.GetKeyDown(KeyCode.R)) {
            LogAndDisplay("REWINDING");
            ge.SetTimeReversed(true);
        }
        if (Input.GetKeyDown(KeyCode.F)) {
            LogAndDisplay("FORWARD");
            ge.SetTimeReversed(false);
            ge.SetEvolve(true);
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            Debug.Log(ge.GetWorldState().GetGERewindMgr().DumpAll());
        }
    }
}
