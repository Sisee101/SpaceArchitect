using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Controller for the LambertXferDl and LambertXferSolar scenes. 
/// 
/// Spaceship with several potential destination orbits to transfer to. Destinations (targets)
/// are determined by finding all OrbitEllipses in the scene that are NOT the spaceship. 
/// 
/// The transfer makes use of the LambertUniversal class and creates a sequence of maneuvers to 
/// be executed by GE.
/// 
/// Inputs:
/// N key - select a target orbit for spaceship
/// A/D - control point on target ellipse to arrive at
/// W/S - control time of flight for the transfer 
/// F - flip the direction of transfer around the ellipse
/// X - execute the selected transfer
/// 
/// The controller is stateful and offers state dependent help in the statefulHelp component. 
/// 
/// </summary>
public class LambertDemoController : MonoBehaviour {

    public Material selectedMaterial;

    public NBody spaceship;

    //! Prefab for symbol to be used for manuevering
    public GameObject maneuverSymbolPrefab;

    //! Text to show summary of maneuver (optional)
    public Text maneuverText;

    //! Text field used to display staeful help
    public Text statefulHelp;

    //! Text field used to display instructions
    public Text instructions; 

    public float scrollSpeed = 1f;

    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;

    //! Flag that designated which "way around" to go on the transfer ellipse. Toggled with F key
    private bool flipPath = false; 

    private int selectedEllipse = 0;

    private List<TargetEllipse> targetEllipses;

    private TransferShip transferShip = null;

    private double tflightFactor = 1;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;

    private class TargetEllipse
    {
        public OrbitUniversal ellipse;
        public LineRenderer lineRenderer;
        public Material originalMaterial;
        public GameObject maneuverSymbol;
        public float manueverPhase;
    }

    /// <summary>
    /// Game state:
    /// SELECT_DEST: Use N key to toggle which ellipse is the destination orbit
    /// COMPUTE_MANEUVER: With a selected target use AD to designate position on target 
    ///                   ellipse. Use WS to control transfer time
    /// DOING_XFER: In flight to maneuver point on target ellipse. 
    /// </summary>
    private enum State { SELECT_DEST, COMPUTE_MANEUVER, DOING_XFER, STATE_COUNT};
    private State state = State.SELECT_DEST;

    private string[] stateHelpText;

    private GravityEngine ge;

    private OrbitData shipData;
    private OrbitData targetData;

    // Use this for initialization
    void Start () {

        ge = GravityEngine.Instance();
        transferShip = spaceship.GetComponent<TransferShip>();
        if (transferShip == null) {
            Debug.LogError("Did not find TransferShip on " + spaceship.gameObject.name);
        }
        if (transferShip.GetTransferType() != TransferShip.Transfer.LAMBERT_ORBIT) {
            Debug.LogWarning("Changing xfer to LAMBERT_RDVS");
            transferShip.SetTransferType(TransferShip.Transfer.LAMBERT_ORBIT);
        }
        transferShip.SetLambertReversePath(false);

        // Scan ellipses in scene and gather into data structure
        OrbitUniversal[] ellipses = (OrbitUniversal[])Object.FindObjectsOfType(typeof(OrbitUniversal));
        targetEllipses = new List<TargetEllipse>();
        foreach (OrbitUniversal ellipse in ellipses) {
            // skip any that have OrbitPredictors/OrbiSegments attached
            if (ellipse.gameObject.GetComponent<OrbitPredictor>() != null)
                continue;
            if (ellipse.gameObject.GetComponent<OrbitSegment>() != null)
                continue;
            // skip spaceship ellipse (not a target)
            if (ellipse.gameObject != spaceship.gameObject) {
                TargetEllipse targetEllipse = new TargetEllipse();
                targetEllipse.ellipse = ellipse;
                targetEllipse.lineRenderer = ellipse.GetComponentInChildren<LineRenderer>();
                targetEllipse.originalMaterial = targetEllipse.lineRenderer.material;
                // create a maneuver symbol, set inactive for now. 
                targetEllipse.maneuverSymbol = Instantiate<GameObject>(maneuverSymbolPrefab);
                targetEllipse.maneuverSymbol.SetActive(false);
                // make it a child of controller (keep things tidy)
                targetEllipse.maneuverSymbol.transform.parent = transform;
                targetEllipse.manueverPhase = 0;
                targetEllipses.Add(targetEllipse);
            }
        }
        SelectEllipse(0);
        InitHelpText();
        instructions.gameObject.SetActive(false);

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();
    }

    private void InitHelpText() {
        stateHelpText = new string[(int) State.STATE_COUNT];
        stateHelpText[(int)State.SELECT_DEST] = "N to select target ellipse, M to choose maneuver";
        stateHelpText[(int)State.COMPUTE_MANEUVER] = "A/D to move target point around ellipse\n"
                                                    +"W/S to change transfer time\n" + 
                                                    "F to flip maneuver direction\n" +
                                                    "X to execute transfer";
        stateHelpText[(int)State.DOING_XFER] = "Transfer in Progress. No keys active.";

    }

    private void SelectEllipse(int index) {
        targetEllipses[selectedEllipse].lineRenderer.material = targetEllipses[selectedEllipse].originalMaterial;
        // use material to access first entry (cannot do a per index set - it is ignored)
        targetEllipses[index].lineRenderer.material = selectedMaterial;
        selectedEllipse = index;
        transferShip.SetTargetOrbit(targetEllipses[index].ellipse);
    }

    private void UpdateManeuverUI() {
        if (maneuverText == null)
            return;
        List<Maneuver> maneuvers = transferShip.GetManeuvers();
        if (maneuvers.Count == 0)
            return;
        
        Vector3 xferVelocity = transferShip.GetTransferVelocity();
        Vector3 dv = xferVelocity - GravityEngine.Instance().GetVelocity(spaceship);

        string s = string.Format("Burn1: dV=({0:G3}, {1:G3}, {2:G3}),   |dV|={3:G3}\n",
            dv.x, dv.y, dv.z, dv.magnitude);
        string s2 = string.Format("Burn2: dV=({0:G3}, {1:G3}, {2:G3}),   |dV|={3:G3}\n",
            maneuvers[1].velChange.x, maneuvers[1].velChange.y, maneuvers[1].velChange.z,
            maneuvers[1].velChange.magnitude);
        string s3 = string.Format("Transfer Time={0:G4}", maneuvers[1].worldTime - maneuvers[0].worldTime);
        maneuverText.text = s + s2 + s3;
        if (maneuverRenderer != null) {
            maneuverRenderer.ShowManeuvers(maneuvers);
        }

    }

    private void AdjustTimeOfFlight() 
    {
        if (Input.GetKeyDown(KeyCode.S)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - 0.1);
            transferShip.SetTransferTimeFactor((float)tflightFactor);
            ComputeTransfer();
        } else if (Input.GetKeyDown(KeyCode.W)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + 0.1);
            transferShip.SetTransferTimeFactor((float)tflightFactor);
            ComputeTransfer();
        }
    }


    /// <summary>
    /// Use the A/D to position the maneuver symbol on the selected orbit. 
    /// 
    /// As move to a new maneuver destination the transfer time will reset to the 
    /// minimum energy value. At a given maneuver position, can use W/S to increase/decrease
    /// the transfer time. 
    /// </summary>
    private void UpdateManeuverSymbol(int index) {

        TargetEllipse t = targetEllipses[index];
        if (Input.GetKey(KeyCode.A)) {
            t.manueverPhase += scrollSpeed;
            ComputeTransfer();
        } else if (Input.GetKey(KeyCode.D)) {
            t.manueverPhase -= scrollSpeed;
            ComputeTransfer();
        }
        AdjustTimeOfFlight();

        UpdateManeuverUI();

        if (targetEllipses[index].manueverPhase > 360f) {
            t.manueverPhase -= 360f; 
        } else if (targetEllipses[index].manueverPhase < 0f) {
            t.manueverPhase += 360f;
        }
        OrbitUniversal ellipse = targetEllipses[index].ellipse;
        Vector3 centerPos = ge.GetPhysicsPosition(ellipse.centerNbody);
        Vector3 pos = ellipse.GetPositionForThetaRadians(Mathf.Deg2Rad * targetEllipses[index].manueverPhase,
								centerPos);
        pos = ge.MapToScene(pos);
        transferShip.SetTargetPhase(t.manueverPhase);

        targetEllipses[index].maneuverSymbol.transform.position = pos;
    }


    private void ComputeTransfer() {
        transferShip.ComputeTransfer();
        List<Maneuver> maneuvers = transferShip.GetManeuvers();
        if (maneuvers.Count != 2) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }
        Vector3 vel = transferShip.GetTransferVelocity();
        maneuverOrbitPredictor.SetVelocity(vel);
        maneuverOrbitPredictor.gameObject.SetActive(true);
        maneuverSegment.SetVelocity(vel);
        maneuverSegment.gameObject.SetActive(true);
    }

    private void EnableManeuver(int selected) {
        // enable manuever icon on selected orbit at current position of body 
        TargetEllipse t = targetEllipses[selectedEllipse];
        t.maneuverSymbol.SetActive(true);
        maneuverSegment.destination = targetEllipses[selectedEllipse].maneuverSymbol;
        ComputeTransfer();
    }

    /// <summary>
    /// When second maneuver is done, use this to blank the maneuver text
    /// </summary>
    /// <param name="m"></param>
    private void ManeuverDoneCallback(Maneuver m) {
        maneuverText.text = "";
        targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
        state = State.SELECT_DEST;
    }

    private void SequenceDoneCallback(OrbitUniversal orbitu) {
        targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
        state = State.SELECT_DEST;
    }


    private void ExecuteTransfer() {
        transferShip.DoTransfer(ManeuverDoneCallback);
        // start evolution if paused. 
        if (!ge.GetEvolve()) {
            ge.SetEvolve(true);
        }
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);
        if (maneuverRenderer != null) {
            maneuverRenderer.Clear();
        }
    }

    // Update is called once per frame
    void Update() {

        statefulHelp.text = stateHelpText[(int)state];

        switch(state) {
            case State.SELECT_DEST:
                maneuverOrbitPredictor.gameObject.SetActive(false);
                maneuverSegment.gameObject.SetActive(false);
                if (Input.GetKeyDown(KeyCode.N)) {
                    // if maneuver selected, disable
                    targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
                    // advance to next ellipse
                    int nextEllipse = selectedEllipse + 1;
                    if (nextEllipse >= targetEllipses.Count) {
                        nextEllipse = 0;
                    }
                    SelectEllipse(nextEllipse);
                }  else if (Input.GetKeyDown(KeyCode.M)) {
                    state = State.COMPUTE_MANEUVER;
                    EnableManeuver(selectedEllipse);
                    ge.SetEvolve(false);
                } else if (Input.GetKeyDown(KeyCode.Space)) {
                    // Pause/Resume GE evolution
                    bool evolve = GravityEngine.Instance().GetEvolve();
                    ge.SetEvolve(!evolve);
                }
                break;

            case State.COMPUTE_MANEUVER:
                if (Input.GetKeyDown(KeyCode.X)) {
                    ExecuteTransfer();
                    state = State.DOING_XFER;
                } else if (Input.GetKeyDown(KeyCode.N)) {
                    state = State.SELECT_DEST;
                    targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
                } else if (Input.GetKeyDown(KeyCode.F)) {
                    // flip shortPath toggle
                    flipPath = !flipPath;
                    transferShip.SetLambertReversePath(flipPath);
                    ComputeTransfer();
                } else {
                    UpdateManeuverSymbol(selectedEllipse);
                }
                break;

            case State.DOING_XFER:
                // nothing. Manuever callback will set state back to SELECT_DEST
                if (Input.GetKeyDown(KeyCode.Space)) {
                    // Pause/Resume GE evolution
                    bool evolve = GravityEngine.Instance().GetEvolve();
                    ge.SetEvolve(!evolve);
                }
                break;

            default:
                Debug.LogError("Unsupported state :" + state);
                break;
        }

    }

}
