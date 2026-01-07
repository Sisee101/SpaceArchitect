using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple class for debug/dev. Get the COE fields from a TLE in an attached OrbitUniversal.
///
/// Split the easy orbital elements (purely as strings) and allow a user to change them, pushing the
/// changes back into line2 of the TLE in the attached OU.
///
/// Useful for making quick changes in the TLE in editor to debug/examine stuff.
///
/// All the real work is in the editor script
/// 
/// </summary>
public class EditTLE : MonoBehaviour
{
    public bool enable;

    public double inclination;

    public double raan;

    public double ecc;

    public double argp;

    public double meanA;

}
