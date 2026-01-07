using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions; 

/// <summary>
/// Utility function to parse JPL Ephemeris data of the form:
/// 2451544.500000000 = A.D. 2000-Jan-01 00:00:00.0000 TDB 
 // EC= 1.704239716781501E-02 QR= 9.833230998788303E-01 IN= 2.669113820737183E-04
 // OM= 1.639752443600624E+02 W = 2.977668064579176E+02 Tp=  2451546.338324738666
 // N = 9.850596796562197E-01 MA= 3.581891404220149E+02 TA= 3.581260865454548E+02
 // A = 1.000371833989169E+00 AD= 1.017420568099508E+00 PR= 3.654600908298652E+02
 //
 // and extract the orbital elements needed to init an OrbitUniversal

/// </summary>
public class JPLEphemerisParser 
{
   
    public static void InitOrbitU(OrbitUniversal orbitU, string jplData)
    {
        string pattern = @"(\w+)\s*=\s+(\S+)";
        Regex r = new Regex(pattern);

        Match m = r.Match(jplData);
        int matchCount = 0;
        while (m.Success)
        {
            Debug.Log("Match" + (++matchCount));
            for (int i = 1; i <= 2; i++)
            {
                Group g = m.Groups[i];
                Debug.Log("Group" + i + "='" + g + "'");
            }
            if (m.Groups[1].Value == "EC")
                orbitU.eccentricity = double.Parse(m.Groups[2].Value);
            else if (m.Groups[1].Value == "IN")
                orbitU.inclination = double.Parse(m.Groups[2].Value);
            else if (m.Groups[1].Value == "OM")
                orbitU.omega_uc = double.Parse(m.Groups[2].Value);
            else if (m.Groups[1].Value == "W")
                orbitU.omega_lc = double.Parse(m.Groups[2].Value);
            else if (m.Groups[1].Value == "TA")
                orbitU.phase = double.Parse(m.Groups[2].Value);
            else if (m.Groups[1].Value == "A")
                orbitU.SetMajorAxisInspector(double.Parse(m.Groups[2].Value));
            m = m.NextMatch();
        }

    }
}
