using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug component to compute the difference between the orbital elements of two objects with 
/// OrbitPredictor components and display them in the inspector.
/// </summary>
public class GEOrbitCompare : MonoBehaviour
{
    public NBody body1;

    public NBody body2;

    private OrbitPredictor op1;
    private OrbitPredictor op2; 

    // Start is called before the first frame update
    void Start()
    {
        op1 = body1.GetComponentInChildren<OrbitPredictor>();
        op2 = body2.GetComponentInChildren<OrbitPredictor>();
        if ((op1 == null) || (op2 == null))
            Debug.LogError("Cannot compare orbits, no OPs");
    }

    public string GetOrbitDiff()
    {
        if (!op1.gameObject.activeInHierarchy || !op2.gameObject.activeInHierarchy) {
            return "one or both objects inactive";
        }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(string.Format("Orbit Diff {0} vs {1}\n", body1.gameObject.name, body2.gameObject.name));
        OrbitUniversal ou1 = op1.GetOrbitUniversal();
        OrbitUniversal ou2 = op2.GetOrbitUniversal();
        sb.Append(string.Format("p  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.p, ou2.p, (ou1.p - ou2.p)));
        sb.Append(string.Format("e  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.eccentricity, ou2.eccentricity, 
            (ou1.eccentricity - ou2.eccentricity)));
        sb.Append(string.Format("i  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.inclination, ou2.inclination, 
            (ou1.inclination - ou2.inclination)));
        sb.Append(string.Format("O  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.omega_uc, ou2.omega_uc, 
            (ou1.omega_uc - ou2.omega_uc)));
        sb.Append(string.Format("o  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.omega_lc, ou2.omega_lc, 
            (ou1.omega_lc - ou2.omega_lc)));
        sb.Append(string.Format("n  {0:0.000} vs {1:0.000} delta={2:0.000}\n", ou1.phase, ou2.phase,
             (ou1.phase - ou2.phase)));
        return sb.ToString();
    }
}
