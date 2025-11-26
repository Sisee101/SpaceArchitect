using UnityEngine;
using System;

/// <summary>
/// Solar utils.
/// Utility functions for determining planet/comet positions. 
/// </summary>
public class SolarUtils  {

	public static double JulianDate(int year, int month, int day, double utime) {
        // M&D A.3
        double y = 0;
        double m = 0; 
		if (month <= 2) {
			y = year - 1.0;
			m = month + 12.0;
		} else {
			y = year; 
			m = month;
		}
        // Shift to Gregorian Calendar
        double B = -2;
		if ((year > 1582) || 
			((year == 1582) && (month > 10)) ||
			((year == 1582) && (month == 10) && (day >= 15)) ) {
				B = Mathd.Floor( y/400f) - Mathd.Floor(y/100f);
		}
		// Debug.Log("B=" + B + " y=" + y + "m="+m);
        double julianDay = Mathd.Floor( 365.25 * y) + Mathd.Floor(30.6001 * (m+1f))
			+ B + 1720996.5f + day + utime/24f;
		return julianDay;
	}

	// Vallado implementation.
    // Works only for years after the move to the gregorian calendar.
	// Matches above for years > 1582.
	public static double JulianDateVallado(int year, int month, int day, double utime)
	{
		double jd, jdFrac;
		jd = 367.0 * year -
			  Math.Floor((7 * (year + Math.Floor((month + 9) / 12.0))) * 0.25) +
			  Math.Floor(275 * month / 9.0) +
			  day + 1721013.5;  // use - 678987.0 to go to mjd directly
		jdFrac = utime/24.0;

		// check that the day and fractional day are correct
		if (Math.Abs(jdFrac) >= 1.0) {
			double dtt = Math.Floor(jdFrac);
			jd = jd + dtt;
			jdFrac = jdFrac - dtt;
		}
		return jd + jdFrac;
	}

    // Utility used by asteroid/comet to find period of orbit
    private const float AU_METERS = 1.496e11f;
	private const float G = 6.67408e-11f; // m^3 s^(-2) kg^(-1)
	private const float SECS_PER_YEAR = 31557600f;

	public const double SEC_PER_DAY = 24 * 60 * 60;

	/// <summary>
	/// Gets the period of a solar body around the Sun in Years
	/// </summary>
	/// <returns>The period.</returns>
	/// <param name="sbody">Sbody.</param>
	public static float GetPeriodYears(SolarBody sbody) {
		float a = sbody.a * AU_METERS;
		float mu = G * SolarSystem.mass_sun * 1e24f;
		float periodYrs = Mathf.Sqrt((4f*Mathf.PI*Mathf.PI*a*a*a)/mu)/SECS_PER_YEAR;
		return periodYrs;
	}

	// Utility Methods used by Custom Inspector.
	public static System.DateTime DateForEpoch(float epoch) {

		int years = (int) Mathf.Floor(epoch);
		int numDays = 365; 
		if (System.DateTime.IsLeapYear(years))
			numDays = 366;
		float days = Mathf.Round((epoch - years)*numDays); 
		System.DateTime date = new System.DateTime(years, 1, 1); 
		return date.AddDays(days);
	}

	public static float DateTimeToEpoch(System.DateTime datetime) {
		float numDays = 365f; 
		if (System.DateTime.IsLeapYear(datetime.Year))
			numDays = 366f;
		float epoch = datetime.Year + (datetime.DayOfYear-1)/numDays; 
		return epoch;
	}

	// JD are days from -4712 (noon)
	// MJD start from midnight and remove the leading digits of 2400000 (this makes them valid
	// for 300 years from Nov 17, 1858.)
	public static float ConvertMJDtoAD(float mjd) {
		float years = (mjd + 2400000.5f)/365.25f;
		return years - 4712f;
	}

	// Vallado Algorithm 22
	// From the book, since could not find it in the download
	
	public static (int, double) JDtoEpochYearDays(double jd)
    {
		double t1900 = (jd - 2415019.5) / 365.25;
		int year = 1900 + Mathd.FloorToInt(t1900);
		int leapYears = Mathd.FloorToInt((year - 1900 - 1) * 0.25);
		double days = (jd - 2415019.5) - ((year - 1900) * (365.0) + leapYears);
		if (days < 1.0) {
			year = year - 1;
			leapYears = Mathd.FloorToInt((year - 1900 - 1) * (0.25));
			days = (jd - 2415019.5) - ((year - 1900) * (365.0) + leapYears);
        }
		return (year, days);
    }

    /* ------------------------------------------------------------------------------
    *
    *                           function sun
    *
    *  this function calculates the geocentric equatorial position vector
    *    the sun given the julian date. Sergey K (2022) has noted that improved results 
    *    are found assuming the oputput is in a precessing frame (TEME) and converting to ICRF. 
    *    this is the low precision formula and is valid for years from 1950 to 2050.  
    *    accuaracy of apparent coordinates is about 0.01 degrees.  notice many of 
    *    the calculations are performed in degrees, and are not changed until later.  
    *    this is due to the fact that the almanac uses degrees exclusively in their formulations.
    *
    *  author        : david vallado           davallado@gmail.com   27 may 2002
    *
    *  revisions
    *    vallado     - fix mean lon of sun                            7 may 2004
    *  
    *  inputs          description                         range / units
    *    jd          - julian date  (UTC)                       days from 4713 bc
    *
    *  outputs       :
    *    rsun        - inertial position vector of the sun      au
    *    rtasc       - right ascension                          rad
    *    decl        - declination                              rad
    *
    *  locals        :
    *    meanlong    - mean longitude
    *    meananomaly - mean anomaly
    *    eclplong    - ecliptic longitude
    *    obliquity   - mean obliquity of the ecliptic
    *    tut1        - julian centuries of ut1 from
    *                  jan 1, 2000 12h
    *    ttdb        - julian centuries of tdb from
    *                  jan 1, 2000 12h
    *    hr          - hours                                    0 .. 24              10
    *    min         - minutes                                  0 .. 59              15
    *    sec         - seconds                                  0.0  .. 59.99          30.00
    *    temp        - temporary variable
    *    deg         - degrees
    *
    *  coupling      :
    *    none.
    *
    *  references    :
    *    vallado       2013, 279, alg 29, ex 5-1
    * --------------------------------------------------------------------------- */

    // NBP wrapper
    public static (Vector3d rAu, double rtAscRad, double declRad) SunPosition(double jd)
    {
        double[] rsun = new double[3] { 0, 0, 0 };
        double ra;
        double decl;
        sun(jd, out rsun, out ra, out decl);
        return (new Vector3d(ref rsun), ra, decl);
    }

    public static void sun
                (
                double jd,
                out double[] rsun, out double rtasc, out double decl
                )
    {
        double twopi, deg2rad;
        double tut1, meanlong, ttdb, meananomaly, eclplong, obliquity, magr;

        // needed since assignments arn't at root level in procedure
        rsun = new double[] { 0.0, 0.0, 0.0 };

        twopi = 2.0 * Math.PI;
        deg2rad = Math.PI / 180.0;

        // -------------------------  implementation   -----------------
        // -------------------  initialize values   --------------------
        tut1 = (jd - 2451545.0) / 36525.0;

        meanlong = 280.460 + 36000.77 * tut1;
        meanlong = (meanlong % 360.0);  //deg

        ttdb = tut1;
        meananomaly = 357.5277233 + 35999.05034 * ttdb;
        meananomaly = ((meananomaly * deg2rad) % twopi);  //rad
        if (meananomaly < 0.0) {
            meananomaly = twopi + meananomaly;
        }
        eclplong = meanlong + 1.914666471 * Math.Sin(meananomaly)
                    + 0.019994643 * Math.Sin(2.0 * meananomaly); //deg
        obliquity = 23.439291 - 0.0130042 * ttdb;  //deg
        meanlong = meanlong * deg2rad;
        if (meanlong < 0.0) {
            meanlong = twopi + meanlong;
        }
        eclplong = eclplong * deg2rad;
        obliquity = obliquity * deg2rad;

        // --------- find magnitude of sun vector, )   components ------
        magr = 1.000140612 - 0.016708617 * Math.Cos(meananomaly)
                           - 0.000139589 * Math.Cos(2.0 * meananomaly);    // in au's

        rsun[0] = magr * Math.Cos(eclplong);
        rsun[1] = magr * Math.Cos(obliquity) * Math.Sin(eclplong);
        rsun[2] = magr * Math.Sin(obliquity) * Math.Sin(eclplong);

        rtasc = Math.Atan(Math.Cos(obliquity) * Math.Tan(eclplong));

        // --- check that rtasc is in the same quadrant as eclplong ----
        if (eclplong < 0.0) {
            eclplong = eclplong + twopi;    // make sure it's in 0 to 2pi range
        }
        if (Math.Abs(eclplong - rtasc) > Math.PI * 0.5) {
            rtasc = rtasc + 0.5 * Math.PI * Math.Round((eclplong - rtasc) / (0.5 * Math.PI));
        }
        decl = Math.Asin(Math.Sin(obliquity) * Math.Sin(eclplong));
    }  // sun


    /* -----------------------------------------------------------------------------
    *
    *                           function moon
    *
    *  this function calculates the geocentric equatorial (ijk) position vector
    *    for the moon given the julian date.
    *
    *  author        : david vallado           davallado@gmail.com   27 may 2002
    *
    *  revisions
    *                -
    *
    *  inputs          description                             range / units
    *    jd          - julian date                              days from 4713 bc
    *
    *  outputs       :
    *    rmoon       - ijk position vector of moon              km
    *    rtasc       - right ascension                          rad
    *    decl        - declination                              rad
    *
    *  locals        :
    *    eclplong    - ecliptic longitude
    *    eclplat     - eclpitic latitude
    *    hzparal     - horizontal parallax
    *    l           - geocentric direction Math.Cosines
    *    m           -             "     "
    *    n           -             "     "
    *    ttdb        - julian centuries of tdb from
    *                  jan 1, 2000 12h
    *    hr          - hours                                    0 .. 24
    *    min         - minutes                                  0 .. 59
    *    sec         - seconds                                  0.0  .. 59.99
    *    deg         - degrees
    *
    *  coupling      :
    *    none.
    *
    *  references    :
    *    vallado       2013, 288, alg 31, ex 5-3
    * --------------------------------------------------------------------------- */

    public static (Vector3d rKm, double rAscRad, double declRad) MoonPosition(double jd)
    {
        double[] rmoon = new double[] { 0, 0, 0 };
        double ra, decl;
        moon(jd, out rmoon, out ra, out decl);
        return (new Vector3d(ref rmoon), ra, decl);
    }


    public static void moon
        (
        double jd,
        out double[] rmoon, out double rtasc, out double decl
        )
    {
        double twopi, deg2rad, magr;
        double ttdb, l, m, n, eclplong, eclplat, hzparal, obliquity;
        // needed since assignments arn't at root level in procedure
        rmoon = new double[] { 0.0, 0.0, 0.0 };

        twopi = 2.0 * Math.PI;
        deg2rad = Math.PI / 180.0;

        // -------------------------  implementation   -----------------
        ttdb = (jd - 2451545.0) / 36525.0;

        eclplong = 218.32 + 481267.8813 * ttdb
                    + 6.29 * Math.Sin((134.9 + 477198.85 * ttdb) * deg2rad)
                    - 1.27 * Math.Sin((259.2 - 413335.38 * ttdb) * deg2rad)
                    + 0.66 * Math.Sin((235.7 + 890534.23 * ttdb) * deg2rad)
                    + 0.21 * Math.Sin((269.9 + 954397.70 * ttdb) * deg2rad)
                    - 0.19 * Math.Sin((357.5 + 35999.05 * ttdb) * deg2rad)
                    - 0.11 * Math.Sin((186.6 + 966404.05 * ttdb) * deg2rad);      // deg

        eclplat = 5.13 * Math.Sin((93.3 + 483202.03 * ttdb) * deg2rad)
                    + 0.28 * Math.Sin((228.2 + 960400.87 * ttdb) * deg2rad)
                    - 0.28 * Math.Sin((318.3 + 6003.18 * ttdb) * deg2rad)
                    - 0.17 * Math.Sin((217.6 - 407332.20 * ttdb) * deg2rad);      // deg

        hzparal = 0.9508 + 0.0518 * Math.Cos((134.9 + 477198.85 * ttdb)
                   * deg2rad)
                  + 0.0095 * Math.Cos((259.2 - 413335.38 * ttdb) * deg2rad)
                  + 0.0078 * Math.Cos((235.7 + 890534.23 * ttdb) * deg2rad)
                  + 0.0028 * Math.Cos((269.9 + 954397.70 * ttdb) * deg2rad);    // deg

        eclplong = ((eclplong * deg2rad) % twopi);
        eclplat = ((eclplat * deg2rad) % twopi);
        hzparal = ((hzparal * deg2rad) % twopi);

        obliquity = 23.439291 - 0.0130042 * ttdb;  //deg
        obliquity = obliquity * deg2rad;

        // ------------ find the geocentric direction Math.Cosines ----------
        l = Math.Cos(eclplat) * Math.Cos(eclplong);
        m = Math.Cos(obliquity) * Math.Cos(eclplat) * Math.Sin(eclplong) - Math.Sin(obliquity) * Math.Sin(eclplat);
        n = Math.Sin(obliquity) * Math.Cos(eclplat) * Math.Sin(eclplong) + Math.Cos(obliquity) * Math.Sin(eclplat);

        // ------------- calculate moon position vector ----------------
        magr = 1.0 / Math.Sin(hzparal);
        rmoon[0] = magr * l;
        rmoon[1] = magr * m;
        rmoon[2] = magr * n;

        // -------------- find rt ascension and declination ------------
        rtasc = Math.Atan2(m, l);
        decl = Math.Asin(n);
    }  // moon


}
