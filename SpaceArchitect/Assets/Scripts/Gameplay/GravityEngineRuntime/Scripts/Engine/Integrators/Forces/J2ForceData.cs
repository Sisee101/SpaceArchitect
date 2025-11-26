using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class J2ForceData : ForceData
{
    public Vector3 axis = Vector3.forward; // default is the z-axis

    public double J2 = 0.0010826267; // default is Earth J2 value

    public double R = 6356.751; // default Earth semi-minor axis in km 

    private double rScaled; 

    private void Start()
    {
        forceType = ForceType.J2;
        rScaled = R * GravityEngine.Instance().GetLengthScale();
    }

    public double GetRScaled()
    {
        return rScaled;
    }

}
