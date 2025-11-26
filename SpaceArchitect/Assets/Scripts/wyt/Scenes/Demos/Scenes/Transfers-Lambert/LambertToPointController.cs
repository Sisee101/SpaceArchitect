using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Demonstrate the operation of the LambertUniversal constructor that allows the target to be shown as 
/// as a point. Compute the path to the target
/// 
/// Keys allow the target point to be moved. 
/// 
/// L - compute a Lambert trajectory to the target point with transfer time for min energy route
/// ,/. - decrease/increase transfer time from the min energy value
/// 
/// </summary>
public class LambertToPointController : MonoBehaviour {

    public NBody spaceship;
    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;
    public GameObject targetPoint;
    public float targetMoveScale = 1.0f;

    public Text dvText;

    // Needed for mouse mapping
    public Camera sceneCamara;
    public GameObject planet; 

    private TransferShip transferShip = null;

    //! use a factor and apply to time of min energy flight
    private float tflightFactor = 1f;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;

    private GravityEngine ge;

    private bool maneuverDone; 

    private class KeyForMove
    {
        public KeyCode code;
        public Vector3 direction;

        public KeyForMove(KeyCode code, Vector3 v) {
            this.code = code;
            direction = v;
        }
    }

    private KeyForMove[] keyCodes;

	// Use this for initialization
	void Start () {
        ge = GravityEngine.Instance();

        keyCodes = new KeyForMove[]{ 
           new KeyForMove(KeyCode.A, new Vector3(-1,0,0)),
           new KeyForMove(KeyCode.D, new Vector3(1, 0, 0)),
           new KeyForMove(KeyCode.W, new Vector3(0, 1, 0)),
           new KeyForMove(KeyCode.S, new Vector3(0, -1, 0)),
           new KeyForMove(KeyCode.Q, new Vector3(0, 0, 1)),
           new KeyForMove(KeyCode.E, new Vector3(0, 0, -1)),
        };

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();

        transferShip = spaceship.GetComponent<TransferShip>();
        if (transferShip == null) {
            Debug.LogError("Did not find TransferShip on " + spaceship.gameObject.name);
        }
        if (transferShip.GetTransferType() != TransferShip.Transfer.LAMBERT_POINT) {
            Debug.LogWarning("Changing xfer to LAMBERT_POINT");
            transferShip.SetTransferType(TransferShip.Transfer.LAMBERT_POINT);
        }
        tflightFactor = transferShip.GetTransferTimeFactor();
        transferShip.SetTargetPoint(ge.UnmapFromScene(targetPoint.transform.position));

        ge.AddGEStartCallback(GEStarted);

    }

	private void GEStarted()
	{
        if (!maneuverOrbitPredictor.gameObject.activeInHierarchy) {
            maneuverOrbitPredictor.gameObject.SetActive(true);
            maneuverSegment.gameObject.SetActive(true);
        }

    }


    private void MoveTarget() {
        foreach( KeyForMove key in keyCodes) {
            if (Input.GetKeyDown(key.code)) {
                targetPoint.transform.position += key.direction * targetMoveScale;
                transferShip.SetTargetPoint(ge.UnmapFromScene(targetPoint.transform.position));
                return;
            }
        }
        if (Input.GetMouseButton(0)) {
            Vector3 mousePos = Input.mousePosition;
            // set Z to be distance from camera to the origin (assumes planet at origin)
            Vector3 mouseOnZ0 = new Vector3(mousePos.x, mousePos.y, sceneCamara.transform.position.magnitude);
            Vector3 targetNew = sceneCamara.ScreenToWorldPoint(mouseOnZ0);
            targetPoint.transform.position = targetNew;
            transferShip.SetTargetPoint(ge.UnmapFromScene(targetPoint.transform.position));
        }
    }

    private void AdjustTimeOfFlight() {
        if (Input.GetKeyDown(KeyCode.Z)) {
            tflightFactor = Mathf.Max(0.1f, tflightFactor - 0.1f);
            transferShip.SetTransferTimeFactor(tflightFactor);
        } else if (Input.GetKeyDown(KeyCode.X)) {
            tflightFactor = Mathf.Min(1.5f, tflightFactor + 0.1f);
            transferShip.SetTransferTimeFactor(tflightFactor);
        }
    }

    private void ComputeTransfer() {
        transferShip.ComputeTransfer();
        List<Maneuver> maneuvers = transferShip.GetManeuvers();
        if (maneuvers.Count != 1) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }

        Vector3 xferVelocity = transferShip.GetTransferVelocity();

        Vector3 dv = xferVelocity - GravityEngine.Instance().GetVelocity(spaceship);
        string hitMsg = "";
        if (transferShip.GetError() == LambertUniversal.IMPACT) {
            hitMsg = "Hit Planet";
        } 
        dvText.text = string.Format("dV = {0:00.00}    Time Factor={1:00.00} Xfer Time={2:00.00}\n{3} ", dv.magnitude,
                                            transferShip.GetTransferTimeFactor(),
                                            transferShip.GetTimeOfFlight(),
                                            hitMsg);
        maneuverOrbitPredictor.SetVelocity(xferVelocity);
        maneuverSegment.SetDestination(targetPoint.transform.position);
        maneuverSegment.SetVelocity(xferVelocity);

    }

    // Update is called once per frame
    void Update () {

        if (!GravityEngine.Instance().IsSetup())
            return;

        if (Input.GetKeyDown(KeyCode.Space)) {
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
        }

        if (maneuverDone)
            return;

        MoveTarget();

        if (Input.GetKeyDown(KeyCode.M)) {
            // perform the maneuver
            // clobber the existing ship velocity and do the adjustment directly
            GravityEngine.Instance().SetVelocity(spaceship, transferShip.GetTransferVelocity());
            maneuverSegment.gameObject.SetActive(false);
            maneuverOrbitPredictor.gameObject.SetActive(false);
            if (maneuverRenderer != null)
                maneuverRenderer.Clear();
            maneuverDone = true;
            return;
        } else if (Input.GetKeyDown(KeyCode.F)) {
            bool reverse = transferShip.GetLambertReversePath();
            transferShip.SetLambertReversePath(!reverse);
            // flip shortPath toggle
            maneuverSegment.retrograde = transferShip.LambertIsReversed();
        } else if (Input.GetKeyDown(KeyCode.A)) {
            double tof = transferShip.GetTimeOfFlight();
            transferShip.SetTransferTime(tof - 1.0);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            double tof = transferShip.GetTimeOfFlight();
            transferShip.SetTransferTime(tof + 1.0);
        }
            AdjustTimeOfFlight();
        // Recompute every frame, since in general the ship is moving
        ComputeTransfer();

        if ((maneuverRenderer != null) ) {
            List<Maneuver> maneuvers = transferShip.GetManeuvers();
            if (maneuvers.Count > 0)
                maneuverRenderer.ShowManeuvers(maneuvers);
        }
    }


}
