using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility function to parse a two line element set from https://www.celestrak.com/NORAD/elements/ for Earth
/// satellite data. 
/// 
/// Assumes Orbit units have been chosen (OrbitU verifies this and warns). 
/// 
/// Sample data:
///  ATLAS CENTAUR 2         
///  1 00694U 63047A   20041.86290382  .00000491  00000-0  50565-4 0  9993
///                    YRDDD.DDDDDDDD
///  2 00694  30.3549 233.3186 0585871 158.9774 203.5990 14.02563639818796
///           Incl    RA       Ecc     Arg Peri MA       Mean Motion (rev/day)
///  Note eccentricity has an implied decimal point
/// 
/// For interpretation see Vallado 2.4.2 p105 (4th ed)
/// 
/// </summary>
public class TwoLineElementSetParser 
{
  public static void InitOrbitU(OrbitUniversal orbitU, string data) {

        string[] lines = data.Split('\n');
        // find line with 2 at start
        foreach(string line in lines)
        {
            string line_ = line.Replace("  ", " "); // remove double space if present (inlination < 100)
            string[] s = line_.Split(' ');
            if (s.Length > 3) {
                if (s[0] == "1") {
                    // get the time at which OE are defined
                    int year = int.Parse(s[2].Substring(0, 2));
                    double day = double.Parse(s[2].Substring(2));
                } else if (s[0] == "2") {
                    // found the line of orbital elements
                    orbitU.inclination = double.Parse(s[2]);
                    orbitU.omega_uc = double.Parse(s[3]);
                    orbitU.eccentricity = double.Parse("." + s[4]);
                    orbitU.omega_lc = double.Parse(s[5]);
                    // Q: Need to convert to true anomoly??
                    orbitU.phase = double.Parse(s[6]);
                    double period = double.Parse(s[7]);
                    double sqrtmu = Mathd.Sqrt(orbitU.GetMu());
                    double a = Mathd.Pow( period/(2.0*Mathd.PI)*sqrtmu, 1.0/3.0);
                    orbitU.SetMajorAxisInspector(a);
                }
            }
        }
  }

    // TODO: Need to use NORAD algorithm to predict location of satellites 
    // https://celestrak.com/NORAD/documentation/spacetrk.pdf
    // Use SGP4 for near earth and SDP4 for deep space satellites. 
    // See enhancements: https://celestrak.com/publications/AIAA/2006-6753/AIAA-2006-6753.pdf

}
