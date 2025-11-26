using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to display information from the associated SGP4 Orbit in the inspector
/// during development. 
/// 
/// Useful for scenarios where the SGP4 data is being updated by impulses
/// or manuevers. It then 
/// </summary>
[RequireComponent(typeof(OrbitUniversal))]
public class GESGP4Log : MonoBehaviour
{
    //#if UNITY_EDITOR

    public string info;

    private OrbitUniversal orbitU;
    private SGP4toGE sgp4toGE;

    // Start is called before the first frame update
    void Start()
    {
        orbitU = GetComponent<OrbitUniversal>();
    }

    // Update is called once per frame
    void Update()
    {
        sgp4toGE = orbitU.GetSGP4ToGE();
        info = sgp4toGE.ToString();
    }
//#endif
}
