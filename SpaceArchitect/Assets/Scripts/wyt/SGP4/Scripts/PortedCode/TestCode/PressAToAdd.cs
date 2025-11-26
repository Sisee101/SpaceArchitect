using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SGP4;

public class PressAToAdd : MonoBehaviour
{
    public GameObject prefab;
    public NBody earth;

    private NBody ship; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            GameObject go = Instantiate(prefab);
            OrbitUniversal orbit = go.GetComponent<OrbitUniversal>();
            NBody center = earth;
            ship = go.GetComponent<NBody>();
            orbit.centerNbody = center;
            //               ".........1.........2.........3.........4.........5.........6.........\n" +
            //               "1234567890123456789012345678901234567890123456789012345678901234567890\n"
            orbit.tleLine1 = "1 26818U 01023A   20001.00000000  .00000000  00000-0  00000-0 0 0000";
            orbit.tleLine2 = "2 26818 051.6458 039.6291  0003151 151.6731 151.6560 16.57337017900000";
            orbit.inputMode = OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET;
            orbit.Init();
            GravityEngine.Instance().AddBody(go);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            SGP4SatData satData = new SGP4SatData();
            double jdManeuverTime = GravityEngine.Instance().GetTimeAsJulianDate(GravityEngine.instance.GetGETime());
            Vector3d position = GravityEngine.instance.GetPositionDoubleV3(ship);
            Vector3d velocity = GravityEngine.instance.GetVelocityDoubleV3(ship);
            satData.UpdateOrbitInfo(position, velocity, jdManeuverTime, earth);
            (string line1, string line2) = satData.CreateTLELines();
            Debug.LogFormat("{0}\n{1}", line1, line2);
        }

    }
}
