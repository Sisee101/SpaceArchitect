using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SGP4;

public class TransferAndLogTLEs : MonoBehaviour
{
    public TransferShip transferShip;
    public NBody centerBody;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            transferShip.ComputeTransfer();
            foreach(Maneuver m in transferShip.GetManeuvers())
            {
                Debug.LogFormat("Manuever {0} r={1} v={2}", m.label, m.relativePos, m.relativeVel);
                // Need to convert maneuver time to JD
                SGP4SatData satData = new SGP4SatData();
                double jdManeuverTime = GravityEngine.Instance().GetTimeAsJulianDate(m.worldTime);
                satData.UpdateOrbitInfo(m.relativePos, m.relativeVel, jdManeuverTime, centerBody);
                (string line1, string line2) = satData.CreateTLELines();
                Debug.LogFormat("Maneuver {0} as TLE:\n{1}\n{2}", m.label, line1, line2);
            }
        }
    }
}
