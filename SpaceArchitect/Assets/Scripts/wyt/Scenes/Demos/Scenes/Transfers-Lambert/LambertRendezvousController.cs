using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Demonstrate the operation of the LambertUniversal constructor that allows an intercept or rendezvous with 
/// a target NBody in a given orbit. 
/// 
/// Keys allow the target point to be moved. 
/// 
/// T - compute the transfer to the target point with transfer time for min energy route
/// ,/. - decrease/increase transfer time from the min energy value
/// 
/// </summary>
public class LambertRendezvousController : MonoBehaviour {

    [SerializeField]
    private  NBody spaceship = null;

    private TransferShip transferShip = null;

    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;

    public Text dvText;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;

    private bool maneuverDone = false; 

	// Use this for initialization
	void Start () {

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();

        transferShip = spaceship.GetComponent<TransferShip>();
		if (transferShip == null) {
            Debug.LogError("Did not find TransferShip on " + spaceship.gameObject.name);
		}
		if(transferShip.GetTransferType() != TransferShip.Transfer.LAMBERT_RDVS) {
            Debug.LogWarning("Changing xfer to LAMBERT_RDVS");
            transferShip.SetTransferType(TransferShip.Transfer.LAMBERT_RDVS);
		}

        GravityEngine.Instance().AddGEStartCallback(OnGEStart);
    }

    private void OnGEStart() {
        maneuverOrbitPredictor.gameObject.SetActive(true);
        maneuverSegment.gameObject.SetActive(true);
    }

    private void ComputeTransfer() {

        transferShip.ComputeTransfer();
        List<Maneuver> maneuvers = transferShip.GetManeuvers();
        if (maneuvers.Count != 2) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }

        Vector3 xferVelocity = transferShip.GetTransferVelocity();
        maneuverOrbitPredictor.SetVelocity(xferVelocity);
        maneuverSegment.gameObject.SetActive(true);
        maneuverSegment.SetDestination(maneuvers[1].physPosition.ToVector3());
        maneuverSegment.SetVelocity(xferVelocity);
    }

    // Update is called once per frame
    void Update () {

        if (!GravityEngine.Instance().IsSetup())
            return;

        if (Input.GetKeyDown(KeyCode.Space)) {
            // toggle evolution
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
        } else if (Input.GetKeyDown(KeyCode.M)) {
            // perform the maneuver
            ComputeTransfer();
            GravityEngine.Instance().AddManeuvers(transferShip.GetManeuvers());
            maneuverOrbitPredictor.gameObject.SetActive(false);
            maneuverSegment.gameObject.SetActive(false);
            maneuverRenderer.Clear();
            maneuverDone = true;
        } else if (Input.GetKeyDown(KeyCode.T)) {
            transferShip.DoTransfer(null);
            maneuverOrbitPredictor.gameObject.SetActive(false);
            maneuverSegment.gameObject.SetActive(false);
            maneuverRenderer.Clear();
            maneuverDone = true;
        } 

        if (!maneuverDone) {
            // Recompute every frame, since in general the ship is moving
            ComputeTransfer();

            if (maneuverRenderer != null) {
                List<Maneuver> maneuvers = transferShip.GetManeuvers();
				if (maneuvers.Count > 0)
					maneuverRenderer.ShowManeuvers(maneuvers);
            }
        }
    }

}
