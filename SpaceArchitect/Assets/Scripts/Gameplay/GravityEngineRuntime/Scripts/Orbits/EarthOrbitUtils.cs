using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthOrbitUtils 
{
    public const double rearth_km = 6378.137;  // km
    const double eesqrd = 0.00669437999013;


    /* -----------------------------------------------------------------------------
	*
	*                           function ecef2ll
	*
	*  these subroutines convert a geocentric equatorial position vector into
	*    latitude and longitude.  geodetic and geocentric latitude are found. the
	*    inputs must be ecef.
	*
	*  author        : david vallado                  719-573-2600    6 dec 2005
	*
	*  revisions
	*
	*  inputs          description                         range / units
	*    recef       - ecef position vector                     km
	*
	*  outputs       :
	*    latgc       - geocentric latitude                   -pi to pi rad
	*    latgd       - geodetic latitude                     -pi to pi rad
	*    lon         - longitude (west -)                     -2pi to 2pi rad
	*    hellp       - height above the ellipsoid                  km
	*
	*  locals        :
	*    temp        - diff between geocentric/
	*                  geodetic lat                                rad
	*    sintemp     - sine of temp                                rad
	*    olddelta    - previous value of deltalat                  rad
	*    rtasc       - right ascension                             rad
	*    decl        - declination                                 rad
	*    i           - index
	*
	*  coupling      :
	*    mag         - magnitude of a vector
	*    gcgd        - converts between geocentric and geodetic latitude
	*
	*  references    :
	*    vallado       2013, 173, alg 12 and alg 13, ex 3-3
	* --------------------------------------------------------------------------- */

	public struct LatLonAlt
    {
		// angles in radians
		public double latgc;    // geocentric lat
		public double latgd;    // geodetic lat
		public double lon;
		public double alt_km;	// altitude above earth surface

		public LatLonAlt(double latgc, double lon, double alt_km)
        {
			this.latgc = latgc;
			this.latgd = LatgcToLatgd(latgc);
			this.lon = lon;
			this.alt_km = alt_km;
        }
    }

    public static LatLonAlt Ecef2ll( Vector3d recef)
    {
        const double small = 0.00000001;         // small value for tolerances
        double magr, decl, rtasc, olddelta, temp, sintemp, s, c = 0.0;
        int i;
        const double pi = Mathd.PI;
        const double twopi = 2.0 * pi;
        const double re = 6378.137; // km. This is max Earth radius
		double latgc, latgd, lon, hellp;

        // ---------------------------  implementation   -----------------------
        magr = recef.magnitude;

        // ---------------------- find longitude value  ------------------------
        temp = Mathd.Sqrt(recef[0] * recef[0] + recef[1] * recef[1]);
        if (Mathd.Abs(temp) < small)
            rtasc = Mathd.Sign(recef[2]) * pi * 0.5;
        else
            rtasc = Mathd.Atan2(recef[1], recef[0]);

        lon = rtasc;
        if (Mathd.Abs(lon) >= pi)   // mod it ?
        {
            if (lon < 0.0)
                lon = twopi + lon;
            else
                lon = lon - twopi;

        }
        decl = Mathd.Asin(recef[2] / magr);
        latgd = decl;

        // ----------------- iterate to find geodetic latitude -----------------
        i = 1;
        olddelta = latgd + 10.0;

        while ((Mathd.Abs(olddelta - latgd) >= small) && (i < 10))
        {
            olddelta = latgd;
            sintemp = Mathd.Sin(latgd);
            c = re / (Mathd.Sqrt(1.0 - eesqrd * sintemp * sintemp));
            latgd = Mathd.Atan((recef[2] + c * eesqrd * sintemp) / temp);
            i = i + 1;
        }

        if ((pi * 0.5 - Mathd.Abs(latgd)) > pi / 180.0)  // 1 deg
            hellp = (temp / Mathd.Cos(latgd)) - c;
        else
        {
            s = c * (1.0 - eesqrd);
            hellp = recef[2] / Mathd.Sin(latgd) - s;
        }

        latgc = Mathd.Asin(recef[2] / magr);  // any location
											  //gc_gd(latgc, MathTimeLib::eFrom, latgd);  // surface of the Earth location
											  // NBP: Vallado had this commented out. Seems to be asking for latgc (which was just assigned) ??
		LatLonAlt lla;
		lla.latgc = latgc;
		lla.latgd = latgd;
		lla.lon = lon;
		lla.alt_km = hellp;
		return lla;
	}   // ecef2ll


	/* -----------------------------------------------------------------------------
	*
	*                           function gc_gd
	*
	*  this function converts from geodetic to geocentric latitude for positions
	*    on the surface of the earth.  notice that (1-f) squared = 1-esqrd.
	*
	*  author        : david vallado                  719-573-2600    6 dec 2005
	*
	*  revisions
	*
	*  inputs          description                          range / units
	*    latgd       - geodetic latitude                     -pi to pi rad
	*
	*  outputs       :
	*    latgc       - geocentric latitude                    -pi to pi rad
	*
	*  locals        :
	*    none.
	*
	*  coupling      :
	*    none.
	*
	*  references    :
	*    vallado       2013, 140, eq 3-11
	* --------------------------------------------------------------------------- */

	//public double GCtoGD
	//(
	//	double&    latgc,
	//	MathTimeLib::edirection direct,
	//	double&    latgd
	//)
	//{
	//	const double eesqrd = 0.006694385000;     // eccentricity of earth sqrd

	//	if (direct == MathTimeLib::eTo)
	//		latgd = atan(tan(latgc) / (1.0 - eesqrd));
	//	else
	//		latgc = atan((1.0 - eesqrd) * tan(latgd));
	//}   // gc_gd

	public static double LatgcToLatgd(double latgc)
    {
		return Mathd.Atan(Mathd.Tan(latgc) / (1.0 - eesqrd));
	}

	// TODO: Site function??
	/*---------------------------------------------------------------------------
	*
	*                           procedure site
	*
	*  this function finds the position and velocity vectors for a site.  the
	*    answer is returned in the geocentric equatorial (ecef) coordinate system.
	*    note that the velocity is zero because the coordinate system is fixed to
	*    the earth.
	*
	*  author        : david vallado                  719-573-2600   25 jun 2002
	*
	*  inputs          description                               range / units
	*    latgd       - geodetic latitude                        -pi/2 to pi/2 rad
	*    lon         - longitude of site                        -2pi to 2pi rad
	*    alt         - altitude                                     km
	*
	*  outputs       :
	*    rsecef      - ecef site position vector                     km
	*    vsecef      - ecef site velocity vector                     km/s
	*
	*  locals        :
	*    sinlat      - variable containing  sin(lat)                 rad
	*    temp        - temporary real value
	*    rdel        - rdel component of site vector                 km
	*    rk          - rk component of site vector                   km
	*    cearth      -
	*
	*  coupling      :
	*    none
	*
	*  references    :
	*    vallado       2013, 430, alg 51, ex 7-1
	----------------------------------------------------------------------------*/

	public static Vector3d Site(double latgd, double lon, double alt_km)
    {

        double sinlat, cearth, rdel, rk;

        // ---------------------  initialize values   ------------------- 
        sinlat = Mathd.Sin(latgd);

        // -------  find rdel and rk components of site vector  --------- 
        cearth = rearth_km / Mathd.Sqrt(1.0 - (eesqrd * sinlat * sinlat));
        rdel = (cearth + alt_km) * Mathd.Cos(latgd);
        rk = ((1.0 - eesqrd) * cearth + alt_km) * sinlat;

        // ----------------  find site position vector  ----------------- 
        Vector3d rsecef =new Vector3d( rdel * Mathd.Cos(lon),
                                       rdel * Mathd.Sin(lon),
                                       rk);

        return rsecef;
    }  // site

}
