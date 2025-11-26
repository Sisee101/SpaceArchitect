using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEConst : MonoBehaviour {

    public static Vector3 xunit = Vector3.right;
    public static Vector3 yunit = Vector3.up;
    public static Vector3 zunit = Vector3.forward;

    public static double small = 1E-3;

    public static double RADIUS_EARTH_KM = 6371;
    public static double RADIUS_EARTH_M = RADIUS_EARTH_KM * 1000.0;
}
