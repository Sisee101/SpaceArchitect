using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Q&D controller to test TOF
/// 
/// AS - change phase of from marger
/// QW - change phase of to marker
/// 
/// Display the time between the two points. 
/// 
/// Run a counter when spaceship passed each marker to report actual time in GE to 
/// compare to TOF algorithm.
/// 
/// </summary>
public class FindTOFController: MonoBehaviour {

    public NBody spaceshipNBody;

    public float fromPhase;
    public float toPhase;

    //! Prefab for symbol to be used for manuevering
    public GameObject fromMarker;
    public GameObject toMarker;

    public Text tofText;

    private OrbitUniversal shipOrbit;

    private const float PHASE_PER_KEY = 0.1f;

    private GravityEngine ge = null;

    // Use this for initialization
    void Start () {

        //shipOrbit = spaceshipNBody.GetComponent<OrbitUniversal>();
        shipOrbit = spaceshipNBody.GetComponentInChildren<OrbitPredictor>().GetOrbitUniversal();
        if (shipOrbit == null) {
            Debug.LogError("spaceship needs an OrbitUniversal");
        }
        ge = GravityEngine.Instance();
     }

    private void SetMarkers() {

 
        // skip scaling since we are in dimensionless units
        Vector3 pos = shipOrbit.PositionForPhase(fromPhase);
        fromMarker.transform.position = pos;

        pos = shipOrbit.PositionForPhase(toPhase);
        toMarker.transform.position = pos;

    }

  
     // Update is called once per frame
    void Update() {
        if (Input.GetKey(KeyCode.A)) {
            fromPhase += PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.S)) {
            fromPhase -= PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.Q)) {
            toPhase += PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.W)) {
            toPhase -= PHASE_PER_KEY;
        }
        fromPhase = NUtils.DegreesMod360(fromPhase);
        toPhase = NUtils.DegreesMod360(toPhase);
        SetMarkers();

        Vector3d shipPos = GravityEngine.instance.GetPositionDoubleV3(spaceshipNBody);
        double tPeri = shipOrbit.TimeOfFlight(shipPos, shipOrbit.GetPositionDForThetaRadians(0f, relative: false));
        double tApo = shipOrbit.TimeOfFlight(shipPos, shipOrbit.GetPositionDForThetaRadians(Mathd.PI, relative: false));
        double tApo2 = shipOrbit.TimeOfFlight(shipPos, shipOrbit.GetPositionDForThetaRadians(Mathd.PI, relative: false));
        double tof = shipOrbit.TimeOfFlight(new Vector3d(fromMarker.transform.position), 
                                            new Vector3d(toMarker.transform.position));
        // Scale to game time
        GravityScaler.Units units = GravityEngine.instance.units;
        tofText.text = string.Format("Time between Markers = {0}\nTime to Apoapsis = {1}\nTime to Periapsis = {2}",
             GravityScaler.GetWorldTimeFormatted(tof, units),
             GravityScaler.GetWorldTimeFormatted(tApo, units),
             GravityScaler.GetWorldTimeFormatted(tPeri, units));
        Debug.LogFormat("tApo={0} tApoEllipse={1}", tApo, tApo2);
    }



}
