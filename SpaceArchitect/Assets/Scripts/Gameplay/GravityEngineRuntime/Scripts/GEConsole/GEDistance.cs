using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEDistance : MonoBehaviour
{
    [SerializeField]
    public NBody body1 = null;

    [SerializeField]
    public NBody body2 = null; 

    // Start is called before the first frame update
    void Start()
    {
        GravityEngine.Instance().AddGEStartCallback(GECallback);
    }

    private void GECallback()
    {
        LogDistance();
    }

    private double minDistance = double.MaxValue;
    private double t_min = 0; 
    public string LogDistance()
    {
        GravityEngine ge = GravityEngine.Instance();
        double d = (ge.GetPositionDoubleV3(body1) - ge.GetPositionDoubleV3(body2)).magnitude;
        if (d < minDistance) {
            minDistance = d;
            t_min = ge.GetPhysicalTime();
        }
        minDistance = System.Math.Min(d, minDistance);
        return string.Format("distance = {0:0.000} minD={1} t={2}\n GE units  {3}\n World Units p1={4} p2={5}\n In meters={6} ratio (1/2)={7}",
            d, minDistance, t_min,
            (ge.GetScenePosition(body1) - ge.GetScenePosition(body2)).magnitude,
            ge.GetPositionDoubleV3(body1),
            ge.GetPositionDoubleV3(body2),
            d * GravityScaler.PositionScaletoSIUnits() / 100.0, 
            ge.GetPositionDoubleV3(body1).magnitude/ge.GetPositionDoubleV3(body2).magnitude
            );

    }

    public string GetDistance()
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log(LogDistance());
        }
    }
}
