using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple controller for the TransferShip component. 
/// 
/// Pressing X executes the transfer. 
/// </summary>
public class TransferShipController : MonoBehaviour
{

    [SerializeField]
    private NBody ship = null;

    [SerializeField]
    private bool transferAtStart = false;

    [SerializeField]
    private bool transferAt2 = false;

    [SerializeField]
	[Tooltip("Optional in scene objects to mark transfer locations")]
    //! Array of objects already in scene to use as markers (keeps things simple)

    [Header("Press X to initiate transfer")]

    private GameObject[] transferMarkers = null;

    private TransferShip transferShip;

    private bool done = false; 

    // Start is called before the first frame update
    void Start()
    {
        transferShip = ship.GetComponent<TransferShip>();
        if (transferShip == null) {
            Debug.LogError("Controller could not find TransferShip component on " + ship.gameObject.name);
        }
    }

	private void ClearMarkers(Maneuver m)
	{
		if (transferMarkers.Length > 0) {
            foreach (GameObject go in transferMarkers)
                go.SetActive(false);
		}
	}

	private void Transfer()
	{
        transferShip.DoTransfer(ClearMarkers);
        if (transferMarkers.Length > 0) {
            List<Maneuver> maneuvers = transferShip.GetManeuvers();
            GravityEngine ge = GravityEngine.Instance();
            for (int i = 0; i < maneuvers.Count; i++) {
                transferMarkers[i].transform.position =
                    ge.MapToScene(maneuvers[i].physPosition.ToVector3());
            }
        } 
    }

    // Update is called once per frame
    void Update()
    {
        if (!done && transferAtStart && GravityEngine.Instance().IsSetup()) {
            Transfer();
            done = true;
        }

        if (!done && transferAt2 && GravityEngine.Instance().IsSetup() && Time.time > 2.0f) {
            Debug.Log("Compute transfer");
            Transfer();
            done = true;
        }

        if (Input.GetKeyDown(KeyCode.X) && !done) {
            Debug.Log("Transfer maneuvers added to GE");
            Transfer();
            done = true; 
        }
    }
}
