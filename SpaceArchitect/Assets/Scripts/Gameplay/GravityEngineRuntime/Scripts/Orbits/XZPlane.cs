using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to adjust from Unity world with orbit in XZ plane to the physics world where
/// orbits are in XY plane. 
///     z                              y
///     ^                              ^
///     |   /y                         |   /z
///     |  /                 ==>       |  /
///     | /                            | /
///     |/                             |/
///     +----------> x                 +-----------> x
///                                    
///    Physics (RH)                    Unity (LH)
///    
/// Objective is to have orbits with phase=0 on x-axis and inclination 0 in the XZ plane
/// and have the physics positions inside GE match the Unity positions. 
/// 
/// This means that code that goes from classical orbital elements needs to adjust it's output 
/// using the routines in this class when XZ orbit mode is enabled. 
/// </summary>
public class XZPlane 
{
    public static Vector3d UnityToPhysics(Vector3d v)
    {
        Vector3d vunity = new Vector3d(v.x, v.z, v.y);
        return vunity;
    }

    public static Vector3 UnityToPhysics(Vector3 v)
    {
        Vector3 vunity = new Vector3(v.x, v.z, v.y);
        return vunity;
    }

    public static void UnityToPhysics(ref double[] v)
    {
        double temp = v[1];
        v[1] = v[2];
        v[2] = temp;
    }

    public static Vector3d PhysicsToUnity(Vector3d v)
    {
        Vector3d vunity = new Vector3d(v.x, v.z, v.y);
        return vunity;
    }

    public static Vector3 PhysicsToUnity(Vector3 v)
    {
        Vector3 vunity = new Vector3(v.x, v.z, v.y);
        return vunity;
    }

    public static void PhysicsToUnity(ref double[] v)
    {
        double temp = v[1];
        v[1] = v[2];
        v[2] = temp;
    }
}
