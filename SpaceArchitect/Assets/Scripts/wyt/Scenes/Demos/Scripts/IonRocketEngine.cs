using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An ion thrust rocket engine for a use-case where the spaceship is in a circular orbit and 
/// wants to raise its orbit via a continuous low-thrust burn in the prograde direction.
/// 
/// The thrustTime is the time the engine will burn prograde. 
/// 
/// </summary>
public class IonRocketEngine : RocketEngine
{
    [SerializeField]
    private double thrust = 0.0;

    [SerializeField]
    private double thrustTime = 30.0f; 

    private NBody nbody;

    public void Start()
    {
        nbody = GetComponent<NBody>();

        if (nbody == null)
            Debug.LogError("IonEngine requires an nbody. Not found on " + gameObject.name);
    }

    public override double[] acceleration(double time, GravityState gravityState, ref double massKg)
    {
        Vector3d a_vector = thrust * gravityState.GetVelocity3d(nbody).normalized;
        if (time > thrustTime) {
            a_vector = Vector3d.zero;
        } 
        double[] a = new double[] { a_vector.x, a_vector.y, a_vector.z };
        return a;
    }

    public override float GetFuel()
    {
        throw new System.NotImplementedException();
    }

    public override float GetThrottlePercent()
    {
        throw new System.NotImplementedException();
    }

    public override void SetEngine(bool state)
    {
        throw new System.NotImplementedException();
    }

    public override void SetThrottlePercent(float throttle)
    {
        throw new System.NotImplementedException();
    }

    public override void SetThrustAxis(Vector3 thrustAxis)
    {
        throw new System.NotImplementedException();
    }


}
