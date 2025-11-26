using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthMoonLine : MonoBehaviour
{

    public NBody earth;
    public NBody moon;

    private LineRenderer lineR;
    private GravityEngine ge;
    private Vector3[] points;


    // Start is called before the first frame update
    void Start()
    {
        lineR = GetComponent<LineRenderer>();
        ge = GravityEngine.instance;
        points = new Vector3[2];
    }

    // Update is called once per frame
    void Update()
    {
        
        points[0] = ge.MapPhyPosToWorld(ge.GetPhysicsPosition(earth));
        points[1] = ge.MapPhyPosToWorld(ge.GetPhysicsPosition(moon));
        lineR.SetPositions(points);
        lineR.positionCount = 2;
    }
}
