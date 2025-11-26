using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple test script to check run-time add of a ship with a TLE init.
/// </summary>
public class CelestrakDynamicAdd : MonoBehaviour
{
    public GameObject shipToAdd;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) {
            GameObject go = Instantiate(shipToAdd);
            GravityEngine.Instance().AddBody(go);
        }
    }
}
