using UnityEngine;
using System;

public class OrbitUtils  {

    // Used in ecc check to determine if circular
    public const double small = 1E-3;
    //public const double small = 1E-6;

    // Used in sme check
    public const double verySmall = 1E-9; // 1E-13;

    public const double ecc_circular = 1E-3; 

	/// <summary>
	/// Calculates the Hill Radius (radius at which the secondary's gravity becomes dominant, when the 
	/// secondary is in orbit around the primary). 
	/// </summary>
	/// <returns>The radius.</returns>
	/// <param name="primary">Primary.</param>
	/// <param name="secondary">Secondary. In orbit around primary</param>
	static public float HillRadius(GameObject primary, GameObject secondary) {

		NBody primaryBody = primary.GetComponent<NBody>(); 
		NBody secondaryBody = secondary.GetComponent<NBody>(); 
		EllipseBase orbit = secondary.GetComponent<EllipseBase>();
		if ((primaryBody == null) || (secondaryBody == null) || (orbit == null)) {
			return 0;
		}
		float denom = 3f*(secondaryBody.mass + primaryBody.mass);
		if (Mathf.Abs(denom) < 1E-6) {
			return 0;
		}
		return Mathf.Pow(secondaryBody.mass/denom, 1/3f) * orbit.a_scaled * (1-orbit.ecc);

	}

    /// <summary>
    /// Get the center
    /// </summary>
    /// <param name="objectInOrbit"></param>
    /// <returns></returns>
    static public NBody GetCenterNbody(Transform objectInOrbit, GameObject centerObject) {
        // If parent has an Nbody assume it is the center
        NBody centerNbody = null;
        if (centerObject == null) {
            if (objectInOrbit.parent != null) {
                centerNbody = objectInOrbit.parent.gameObject.GetComponent<NBody>();
                if (centerNbody != null) {
                    centerObject = objectInOrbit.parent.gameObject;
                } else {
                    Debug.LogError("Parent object must have NBody attached");
                    return null;
                }
            } else {
                Debug.Log("Warning - Require a parent object (with NBody) for " + objectInOrbit.name);
                // This path when init-ed via Instantiate() script will need to 
                // call Init() explicily once orbit params and center are set
                return null;
            }
        } else {
            centerNbody = centerObject.GetComponent<NBody>();
            if (centerNbody == null) {
                Debug.LogError("CenterObject must have an NBody attached");
            }
        }
        return centerNbody;
    }

    static public Vector3d CenterOfMass(NBody body1, NBody body2)
    {
        GravityEngine ge = GravityEngine.Instance();
        Vector3d body1Pos = ge.GetPositionDoubleV3(body1);
        Vector3d body2Pos = ge.GetPositionDoubleV3(body2);
        double m1 = ge.GetMass(body1);
        double m2 = ge.GetMass(body2);
        return (m1 * body1Pos + m2 * body2Pos) / (m1 + m2);
    }

    /// <summary>
    /// Calculate the semi-major axis for the required period
    /// </summary>
    /// <param name="period"></param>
    /// <param name="centerMass"></param>
    /// <returns></returns>
    public static double CalcAForPeriod(double period, double centerMass) {
        double p_over_2pi = period / (2.0 * Math.PI);
        return Math.Pow( p_over_2pi * p_over_2pi * centerMass, 1.0 / 3.0) ;
    }

    /// <summary>
    /// Determine how many parents/grandparents etc. have Kepler mode. 
    /// GE uses this to ensure evolution starts at the most central body in a heirarchy
    /// and works out to the leaves. 
    /// </summary>
    public static int CalcKeplerDepth(IFixedOrbit fixedBody) {

        if (!fixedBody.IsOnRails()) {
            return 0;
        }

        int depth = 0;
        bool done = false;
        NBody center = fixedBody.GetCenterNBody();
        while (!done && (center != null)) {
            IFixedOrbit parent = center.GetComponent<IFixedOrbit>();
            if ((parent != null) && parent.IsOnRails()) {
                depth++;
                center = parent.GetCenterNBody();
            } else {
                done = true;
            }
        }

        return depth;
    }

    /// <summary>
    /// Determine the SOI radius in internal physics units.
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="moon"></param>
    /// <returns></returns>
    public static float SoiRadius(NBody planet, NBody moon) {
        // to allow to run before GE is up, use Ellipse component to get radius
        OrbitEllipse moonEllipse = moon.gameObject.GetComponent<OrbitEllipse>();
        float a;
        if (moonEllipse != null) {
            a = moonEllipse.a_scaled;
        } else {
            OrbitUniversal orbitU = moon.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                a = (float) orbitU.GetApogee();
            } else {
                Debug.LogWarning("Could not get moon orbit size");
                return float.NaN;
            }
        }
        // mass scaling will cancel in this ratio
        return Mathf.Pow(moon.mass / planet.mass, 0.4f) * a;
    }

    public static bool IsOnRails(NBody nbody) {
        bool isOnRails = false;
        IFixedOrbit ifOrbit = nbody.GetComponent<IFixedOrbit>();
        if (ifOrbit != null)
            isOnRails = ifOrbit.IsOnRails();
        return isOnRails; 
    }

    // Taken from Vallado source code site. (Also used in LambertUniversal)
    public static void FindC2C3(double znew, out double c2new, out double c3new) {
        double small, sqrtz;
        small = 0.00000001;

        // -------------------------  implementation   -----------------
        if (znew > small) {
            sqrtz = System.Math.Sqrt(znew);
            c2new = (1.0 - System.Math.Cos(sqrtz)) / znew;
            c3new = (sqrtz - System.Math.Sin(sqrtz)) / (sqrtz * sqrtz * sqrtz);
        } else {
            if (znew < -small) {
                sqrtz = System.Math.Sqrt(-znew);
                c2new = (1.0 - System.Math.Cosh(sqrtz)) / znew;
                c3new = (System.Math.Sinh(sqrtz) - sqrtz) / (sqrtz * sqrtz * sqrtz);
            } else {
                c2new = 0.5;
                c3new = 1.0 / 6.0;
            }
        }
    }  // findc2c3

    /// <summary>
    /// Determine the time of flight in physics time units (GE internal time) that it takes for the body
    /// in orbit to go from position r0 to position r1 in an orbit with parameter p.
    /// 
    /// The angle between two 3D vectors cannot be greater than 180 degrees unless the orientation of the
    /// plane they define is also specified. The normal parameter is used for this. If an angle less than
    /// 180 is desired then the cross product of r0 and v0 can be used as the normal. 
    /// Calling TOF via the OrbitUniversal wrapper will handle all that automatically. 
    /// 
    /// </summary>
    /// <param name="r0">from point (with respect to center)</param>
    /// <param name="r1">to point (with respect to center)</param>
    /// <param name="p">orbit semi-parameter</param>
    /// <param name="mu">centerbody mass</param>
    /// <param name="normal">normal to orital plane</param>
    /// <returns>time to travel from r0 to r1 in GE time</returns>
    public static double TimeOfFlight(Vector3d r0, Vector3d r1, double p, double mu, Vector3d normal) {
        const double SMALL = 0; // 1E-7;  parabola less likely than very large units. 

        // Vallado, Algorithm 11, p126
        double tof = 0;
        double r0r1 = r0.magnitude * r1.magnitude;
        Vector3d r0n = r0.normalized;
        Vector3d r1n = r1.normalized;
        double cos_dnu = Vector3d.Dot(r0n, r1n);
        cos_dnu = Math.Clamp(cos_dnu, -1.0, 1.0);
        double sin_dnu = Vector3d.Cross(r0n, r1n).magnitude;
        sin_dnu = Math.Clamp(sin_dnu, -1.0, 1.0);
        // use the normal to determine if angle is > 180
        if (Vector3d.Dot(Vector3d.Cross(r0, r1), normal) < 0.0) {
            sin_dnu *= -1.0;
        }
        // GE - precision issue at 180 degrees. Simply return 1/2 the orbit period.
        if (Math.Abs(1.0 + cos_dnu) < 1E-10) {
            double a180 = 0.5f * (r0.magnitude + r1.magnitude);
            return Math.Sqrt(a180 * a180 * a180 / mu) * Math.PI;
        }
        //// sin_nu: Need to use direction of flight to pick sign per Algorithm 53
        double k = r0r1 * (1.0 - cos_dnu);
        double l = r0.magnitude + r1.magnitude;
        double m = r0r1 * (1.0 + cos_dnu);
        double a = (m * k * p) / ((2.0 * m - l * l) * p * p + 2.0 * k * l * p - k * k);
        double f = 1.0 - (r1.magnitude / p) * (1.0 - cos_dnu);
        double g = r0r1 * sin_dnu / (Math.Sqrt(mu * p));

        double alpha = 1 / a;
        if (alpha > SMALL) {
            // ellipse
            double delta_nu = Math.Atan2(sin_dnu, cos_dnu);
            double fdot = Math.Sqrt(mu / p) * Math.Tan(0.5 * delta_nu) *
                ((1 - cos_dnu) / p - (1 / r0.magnitude) - (1.0 / r1.magnitude));
            double cos_deltaE = 1 - r0.magnitude / a * (1.0 - f);
            cos_deltaE = Math.Clamp(cos_deltaE, -1.0, 1.0);
            double sin_deltaE = -r0r1 * fdot / (Math.Sqrt(mu * a));
            sin_deltaE = Math.Clamp(sin_deltaE, -1.0, 1.0);
            double deltaE = Math.Atan2(sin_deltaE, cos_deltaE);
            tof = g + Math.Sqrt(a * a * a / mu) * (deltaE - sin_deltaE);
        } else if (alpha < -SMALL) {
            // hyperbola
            double cosh_deltaH = 1.0 + (f - 1.0) * r0.magnitude / a;
            double deltaH = GEMath.Acosh(cosh_deltaH);
            tof = g + Math.Sqrt(-a * a * a / mu) * (GEMath.Sinh(deltaH) - deltaH);
        } else {
            // parabola
            double c = Math.Sqrt(r0.magnitude * r0.magnitude + r1.magnitude * r1.magnitude - 2.0 * r0r1 * cos_dnu);
            double s = 0.5 * (r0.magnitude + r1.magnitude + c);
            tof = 2/3*Math.Sqrt(s*s*s/(2.0*mu))*(1-Math.Pow(((s-c)/s), 1.5));
        }
        return tof;
    }

    /// <summary>
    /// Simpler approach from Curtis for the case of an ellipse. This handles very large values of r (>1E10) 
    /// without some of the precision issues that the OrbitUtils.TimeOfFlight has at values near 0, 180 degrees with
    /// large r.
    /// 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
	[Obsolete("Broken. Use TimeOfFlight")]
    public static double TimeOfFlightEllipse(Vector3d r0, Vector3d r1, Vector3d axis, double eccentricity, double orbit_period)
    {
        if (eccentricity >= 1.0) {
            Debug.LogWarning("Only ellipse is supported");
            return 0;
        }
        // Can get a precision issue with big r values (> 1E10) since the angle changes very slowly near
        // 0 and imprecision in sin and division makes normal algorithm misbehave
        Vector3d r0n = r0.normalized;
        Vector3d r1n = r1.normalized;
        double cos_dnu = Vector3d.Dot(r0n, r1n);
        cos_dnu = Math.Clamp(cos_dnu, -1.0, 1.0); 
        double sin_dnu = Vector3d.Cross(r0n, r1n).magnitude;
        sin_dnu = Math.Clamp(sin_dnu, -1.0, 1.0);
        // use the normal to determine if angle is > 180
        if (Vector3d.Dot(Vector3d.Cross(r0, r1), axis) < 0.0) {
            sin_dnu *= -1.0;
        }
        double nu = Math.Atan2(sin_dnu, cos_dnu);
        double tan_halfE = Math.Sqrt((1 - eccentricity) / (1 + eccentricity)) * Math.Tan(0.5 * nu);
        double E = 2.0 * Math.Atan(tan_halfE);
        double Me = E - eccentricity * Math.Sin(E);

        double t = orbit_period * Me / (2.0 * Math.PI);
        if (t < 0)
            t = orbit_period + t;
        return t;
    }

    // From Vallado source code site. Adpated for GE/C#

    /// <summary>
    /// A "struct-like" class that holds all the orbital elements determined by
    /// RVtoCOE and used by COEtoRV. There are some specific cases that need to be considered:
    ///
    /// General orbit (e != 0, i != 0):
    ///  orientation given by raan, argp
    /// Equitorial, e !=0 i = 0
    ///  longper (longitide of perigee) [OrbitU uses this as omega_lc]
    /// Inclined, circular 
    /// </summary>
    public class OrbitElements {
        public enum TypeOrbit { ELLIPTICAL_INCLINED,
                                CIRCULAR_EQUATORIAL,
                                CIRCULAR_INCLINED,
                                ELLIPTICAL_EQUATORIAL, 
                                FREEFALL
        };
        public double p;
        public double a;
        public double ecc;          // eccentricity
        public Vector3d ecc_vec;
        public double incl;         // inclination
        public double raan;         // right ascension of ascending node
        public double argp;         // argument of perigee
        public double nu;           // phase angle from focus of ellipse
        public double m;
        public double eccanom;
        public double arglat;
        public double truelon;
        public double lonper;       // longitude of perigee
        public TypeOrbit typeOrbit;

        public double period;   // for ecc < 1 only

        public OrbitElements()
        {

        }

        public OrbitElements(OrbitElements oe)
        {
            p = oe.p;
            a = oe.a;
            ecc = oe.ecc;
            ecc_vec = oe.ecc_vec;
            incl = oe.incl;
            raan = oe.raan;
            argp = oe.argp;
            nu = oe.nu;
            m = oe.m;
            eccanom = oe.eccanom;
            arglat = oe.arglat;
            truelon = oe.truelon;
            lonper = oe.lonper;
            typeOrbit = oe.typeOrbit;
        }

        public OrbitElements(OrbitData orbitData)
        {
            a = orbitData.a;
            ecc = orbitData.ecc;
            incl = orbitData.inclination * Mathd.Deg2Rad;
            raan = orbitData.omega_uc * Mathd.Deg2Rad;
            argp = orbitData.omega_lc * Mathd.Deg2Rad;
            nu = orbitData.phase * Mathd.Deg2Rad;
            // be paranoid about special cases (circular orbits). 
            lonper = nu;
            arglat = nu;
            // calc p
            p = Math.Abs(a * (1 - ecc * ecc)); // OrbitData keeps a positive, for hyper it should have been negative
        }

        public OrbitElements(double a, double e, double i, double omegaU, double omegaL, double phase)
        {
            this.a = a;
            ecc = e;
            incl = i * Mathd.Deg2Rad;
            raan = omegaU * Mathd.Deg2Rad;
            argp = omegaL * Mathd.Deg2Rad;
            nu = phase * Mathd.Deg2Rad;
            // be paranoid about special cases (circular orbits). 
            lonper = nu;
            arglat = nu;
            // calc p
            p = Math.Abs(a * (1 - ecc * ecc)); // OrbitData keeps a positive, for hyper it should have been negative
        }

        public OrbitElements(OrbitUniversal orbitU)
        {
            a = orbitU.GetMajorAxis();
            ecc = orbitU.eccentricity;
            incl = orbitU.inclination * Mathd.Deg2Rad;
            raan = orbitU.omega_uc * Mathd.Deg2Rad;
            argp = orbitU.omega_lc * Mathd.Deg2Rad;
            nu = orbitU.phase * Mathd.Deg2Rad;
            p = orbitU.p;
            // arglat, longPer etc.
            lonper = nu;
            arglat = nu;
        }

        public bool IsInclined() {
            return (typeOrbit == TypeOrbit.ELLIPTICAL_INCLINED) || (typeOrbit == TypeOrbit.CIRCULAR_INCLINED);
        }

        public bool IsCircular() {
            return (typeOrbit == TypeOrbit.CIRCULAR_INCLINED) || (typeOrbit == TypeOrbit.CIRCULAR_EQUATORIAL);
        }

        public void ComputeType()
        {
            typeOrbit = TypeOrbit.ELLIPTICAL_INCLINED;
            if (incl < small) {
                if (ecc < small) {
                    typeOrbit = TypeOrbit.CIRCULAR_EQUATORIAL;
                } else {
                    typeOrbit = TypeOrbit.ELLIPTICAL_EQUATORIAL;
                }
            } else if (ecc < small) {
                typeOrbit = TypeOrbit.CIRCULAR_INCLINED;
            }
        }

        public double GetPeriapsis()
        {
            return a * (1 - ecc);
        }

        /// <summary>
        /// Set the phase of the orbit. Requires some care since depending on the orbit type different fields in the
        /// OE class are used. This derives from the algorithms taken from Vallado. 
        /// </summary>
        /// <param name="phaseRadians"></param>
        public void SetPhase(double phaseRadians)
        {

            // Derived from Vallad COEtoRV, but inverse
            if (ecc < small) {
                // ----------------  circular equatorial  ------------------

                if ((incl < small) || (Math.Abs(incl - Math.PI) < small)) {
                    truelon = phaseRadians;
                }
                else {
                    // --------------  circular inclined  ------------------
                    arglat = phaseRadians;
                }
            }
            else {
                nu = phaseRadians;
            }
        }

        /// <summary>
        /// COE has an awkward was of representing phase. Dig it out from the different places it is stored based on the
        /// orbit params.
        /// </summary>
        /// <param name="phaseRadians"></param>
        /// <returns></returns>
        public double GetPhase()
        {

            // Derived from Vallad COEtoRV, but inverse
            if (ecc < small) {
                // ----------------  circular equatorial  ------------------

                if ((incl < small) || (Math.Abs(incl - Math.PI) < small)) {
                    return truelon;
                }
                else {
                    // --------------  circular inclined  ------------------
                    return arglat;
                }
            }
            else {
                return nu;
            }
        }

        public override string ToString() {
            ComputeType();
            return string.Format("type={0} p={1} a={2} ecc={3} incl={4} raan={5} argp={6} nu={7} m={8} phase={9}(deg) lonper={10}(deg) arglat={11}(deg)",
                typeOrbit, p, a, ecc, incl * Mathd.Rad2Deg, raan, argp, nu, m, GetPhase()*Mathd.Rad2Deg, lonper * Mathd.Rad2Deg, arglat * Mathd.Deg2Rad);
        }
    }
    /* -----------------------------------------------------------------------------
    *
    *                           function rv2coe
    *
    *  this function finds the classical orbital elements given the geocentric
    *    equatorial position and velocity vectors.
    *
    *  author        : david vallado                  719-573-2600   21 jun 2002
    *
    *  revisions
    *    vallado     - fix special cases                              5 sep 2002
    *    vallado     - delete extra check in inclination code        16 oct 2002
    *    vallado     - add constant file use                         29 jun 2003
    *
    *  inputs          description                    range / units
    *    r           - ijk position vector            km
    *    v           - ijk velocity vector            km / s
    *
    *  outputs       :
    *    p           - semilatus rectum               km
    *    a           - semimajor axis                 km
    *    ecc         - eccentricity
    *    incl        - inclination                    0.0  to pi rad
    *    raan       - longitude of ascending node    0.0  to 2pi rad
    *    argp        - argument of perigee            0.0  to 2pi rad
    *    nu          - true anomaly                   0.0  to 2pi rad
    *    m           - mean anomaly                   0.0  to 2pi rad
    *    eccanom     - eccentric, parabolic,
    *                  hyperbolic anomaly             rad
    *    arglat      - argument of latitude      (ci) 0.0  to 2pi rad
    *    truelon     - true longitude            (ce) 0.0  to 2pi rad
    *    lonper      - longitude of periapsis    (ee) 0.0  to 2pi rad
    *
    *  locals        :
    *    hbar        - angular momentum h vector      km2 / s
    *    ebar        - eccentricity     e vector
    *    nbar        - line of nodes    n vector
    *    c1          - v**2 - u/r
    *    rdotv       - r dot v
    *    hk          - hk unit vector
    *    sme         - specfic mechanical energy      km2 / s2
    *    i           - index
    *    temp        - temporary variable
    *    typeorbit   - type of orbit                  ee, ei, ce, ci
    *
    *  coupling      :
    *    mag         - magnitude of a vector
    *    cross       - cross product of two vectors
    *    angle       - find the angle between two vectors
    *    newtonnu    - find the mean anomaly
    *
    *  references    :
    *    vallado       2013, 113, alg 9, ex 2-5
    * --------------------------------------------------------------------------- */

    // Vector3 version 4.0
    public static OrbitElements RVtoCOE(Vector3 r_in,
                            Vector3 v_in,
                            NBody centerBody,
                            float mu,
                            bool relativePos,
                            double ecc_threshold = small,
                            double incl_threshold = small) {
        return RVtoCOE(new Vector3d(r_in), new Vector3d(v_in), centerBody, mu, relativePos, ecc_threshold, incl_threshold);
    }

    public static OrbitElements RVtoCOE(Vector3 r_in,
                        Vector3 v_in,
                        NBody centerBody,
                        bool relativePos,
                        double ecc_threshold = small,
                        double incl_threshold = small) {
        double mu = GravityEngine.Instance().GetMass(centerBody);
        return RVtoCOE(new Vector3d(r_in), new Vector3d(v_in), centerBody, mu, relativePos, ecc_threshold, incl_threshold);
    }

    /// <summary>
    /// Pre-4.0 method signature without explicit use of mu
    /// </summary>
    /// <param name="r_in"></param>
    /// <param name="v_in"></param>
    /// <param name="centerBody"></param>
    /// <param name="relativePos"></param>
    /// <returns></returns>
    public static OrbitElements RVtoCOE(Vector3d r_in,
                     Vector3d v_in,
                     NBody centerBody,
                     bool relativePos,
                     double ecc_threshold = small,
                     double incl_threshold = small) {
        double mu = GravityEngine.Instance().GetMass(centerBody);
        return RVtoCOE(r_in, v_in, centerBody, mu, relativePos, ecc_threshold, incl_threshold);
    }

    public static OrbitElements RVtoCOE(Vector3d r_in,
                        Vector3d v_in,
                        NBody centerBody, 
                        double mu,
                        bool relativePos, 
                        double ecc_threshold = small, 
                        double incl_threshold = small) {

        double  magr, magv, magn, sme, rdotv, temp, c1, hk, magh;

        Vector3d r = r_in;
        Vector3d v = v_in;
        if (!relativePos) {
            Vector3d centerPos = GravityEngine.Instance().GetPositionDoubleV3(centerBody);
            Vector3d centerVel = GravityEngine.Instance().GetVelocityDoubleV3(centerBody);
            r = r - centerPos;
            v = v - centerVel;
        }
        if (GravityEngine.Instance().xzOrbits) {
            r = XZPlane.UnityToPhysics(r);
            v = XZPlane.UnityToPhysics(v);
        }
        OrbitElements oe = new OrbitElements();

        oe.eccanom = 0.0;

        // -------------------------  implementation   -----------------
        magr = r.magnitude;
        magv = v.magnitude;

        // ------------------  find h n and e vectors   ----------------
        Vector3d hbar = Vector3d.Cross(r, v);
        magh = hbar.magnitude;
        if (magh > verySmall) {
            Vector3d nbar = new Vector3d(-hbar.y, hbar.x, 0.0);
            magn = nbar.magnitude;
            c1 = magv * magv - mu / magr;
            rdotv = Vector3d.Dot(r, v);
            temp = 1.0 / mu;
            Vector3d ebar = new Vector3d((c1 * r.x - rdotv * v.x) * temp,
                                         (c1 * r.y - rdotv * v.y) * temp,
                                         (c1 * r.z - rdotv * v.z) * temp);
            oe.ecc_vec = ebar;
            oe.ecc = ebar.magnitude;

            // ------------  find a e and semi-latus rectum   ----------
            sme = (magv * magv * 0.5) - (mu / magr);
            // was check vs small, but really care about > 0
            if (Math.Abs(sme) > verySmall)
                oe.a = -mu / (2.0 * sme);
            else
                oe.a = double.NaN;
            oe.p = magh * magh * temp;

            // -----------------  find inclination   -------------------
            hk = hbar.z/ magh;
            oe.incl = Math.Acos(Math.Clamp(hk, -1.0, 1.0));

            oe.typeOrbit = OrbitElements.TypeOrbit.ELLIPTICAL_INCLINED;

            if (oe.ecc < ecc_threshold) {
                // ----------------  circular equatorial ---------------
                if ((oe.incl < incl_threshold) || (Math.Abs(oe.incl - Math.PI) < incl_threshold)) {
                    oe.typeOrbit = OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL;
                } else {
                    oe.typeOrbit = OrbitElements.TypeOrbit.CIRCULAR_INCLINED;
                }
            } else {
                // - elliptical, parabolic, hyperbolic equatorial --
                if ((oe.incl < incl_threshold) || (Math.Abs(oe.incl - Math.PI) < incl_threshold)) {
                     oe.typeOrbit = OrbitElements.TypeOrbit.ELLIPTICAL_EQUATORIAL;
                }
            }

            // ----------  find right ascension of the ascending node ------------
            if (magn > verySmall) {
                temp = nbar.x / magn;
                if (Math.Abs(temp) > 1.0)
                    temp = Math.Sign(temp);
                oe.raan = Math.Acos(Math.Clamp(temp, -1.0, 1.0));
                if (nbar.y < 0.0)
                    oe.raan = 2.0 * Math.PI - oe.raan;
            } else
                oe.raan = double.NaN;

            // ---------------- find argument of perigee ---------------
            if (oe.typeOrbit == OrbitElements.TypeOrbit.ELLIPTICAL_INCLINED) {
                oe.argp = Vector3d.AngleRadians(nbar, ebar);
                if (ebar.z < 0.0)
                    oe.argp = 2.0 * Math.PI - oe.argp;
            } else
                oe.argp = double.NaN;

            // ------------  find true anomaly at epoch    -------------
            if (!oe.IsCircular()) {
                oe.nu = Vector3d.AngleRadians(ebar, r);
                if (rdotv < 0.0)
                    oe.nu = 2.0 * Math.PI - oe.nu;
            } else
                oe.nu = double.NaN;

            // ----  find argument of latitude - circular inclined -----
            if (oe.typeOrbit == OrbitElements.TypeOrbit.CIRCULAR_INCLINED) {
                oe.arglat = Vector3d.AngleRadians(nbar, r); ;
                if (r.z < 0.0)
                    oe.arglat = 2.0 * Math.PI - oe.arglat;
                oe.m = oe.arglat;
            } else
                oe.arglat = double.NaN;

            // -- find longitude of perigee - elliptical equatorial ----
            if ((oe.ecc > ecc_threshold) && (oe.typeOrbit == OrbitElements.TypeOrbit.ELLIPTICAL_EQUATORIAL)) {
                temp = ebar.x / oe.ecc;
                if (Math.Abs(temp) > 1.0)
                    temp = Math.Sign(temp);
                oe.lonper = Math.Acos(Math.Clamp(temp, -1.0, 1.0));
                if (ebar.y < 0.0)
                    oe.lonper = 2.0 * Math.PI - oe.lonper;
                if (oe.incl > 0.5 * Math.PI)
                    oe.lonper = 2.0 * Math.PI - oe.lonper;
            } else
                oe.lonper = double.NaN;

            // -------- find true longitude - circular equatorial ------
            if ((magr > verySmall) && (oe.typeOrbit == OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL)) {
                temp = r.x / magr;
                if (Math.Abs(temp) > 1.0)
                    temp = Math.Sign(temp);
                oe.truelon = Math.Acos(Math.Clamp(temp, -1.0, 1.0));
                if (r.y < 0.0)
                    oe.truelon = 2.0 * Math.PI - oe.truelon;
                if (oe.incl > 0.5 * Math.PI)
                    oe.truelon = 2.0 * Math.PI - oe.truelon;
                oe.m = oe.truelon;
            } else
                oe.truelon = double.NaN;

            // ------------ find mean anomaly for all orbits -----------
            if (!oe.IsCircular())
                NewtonNu(oe);

            // comnpute period
            oe.period = double.NaN;
            if (oe.ecc < 1.0) {
                oe.period = 2.0 * Math.PI * Math.Sqrt(oe.a * oe.a * oe.a / mu);
            }
        } else {
            oe.p = double.NaN;
            oe.a = double.NaN;
            oe.ecc = double.NaN;
            oe.incl = double.NaN;
            oe.raan = double.NaN;
            oe.argp = double.NaN;
            oe.nu = double.NaN;
            oe.m = double.NaN;
            oe.arglat = double.NaN;
            oe.truelon = double.NaN;
            oe.lonper = double.NaN;
            oe.typeOrbit = OrbitElements.TypeOrbit.FREEFALL;
            //Debug.LogWarning(string.Format("h too small. r0={0} v0={1} center={2}", 
            //    r, v, centerBody.gameObject.name));
        }
        return oe;
    }  // rv2coe


    /* ------------------------------------------------------------------------------
	*
	*                           function coe2rv
	*
	*  this function finds the position and velocity vectors in geocentric
	*    equatorial (ijk) system given the classical orbit elements.
	*
	*  author        : david vallado                  719-573-2600    1 mar 2001
	*
	*  inputs          description                    range / units
	*    p           - semilatus rectum               km
	*    ecc         - eccentricity
	*    incl        - inclination                    0.0 to pi rad
	*    raan       - longitude of ascending node    0.0 to 2pi rad
	*    argp        - argument of perigee            0.0 to 2pi rad
	*    nu          - true anomaly                   0.0 to 2pi rad
	*    arglat      - argument of latitude      (ci) 0.0 to 2pi rad
	*    lamtrue     - true longitude            (ce) 0.0 to 2pi rad
	*    lonper      - longitude of periapsis    (ee) 0.0 to 2pi rad
	*
	*  outputs       :
	*    r           - ijk position vector            km
	*    v           - ijk velocity vector            km / s
	*
	*  locals        :
	*    temp        - temporary real*8 value
	*    rpqw        - pqw position vector            km
	*    vpqw        - pqw velocity vector            km / s
	*    sinnu       - sine of nu
	*    cosnu       - cosine of nu
	*    tempvec     - pqw velocity vector
	*
	*  coupling      :
	*    rot3        - rotation about the 3rd axis
	*    rot1        - rotation about the 1st axis
	*
	*  references    :
	*    vallado       2013, 118, alg 10, ex 2-5
	* --------------------------------------------------------------------------- */

    // Some awkward API glue here. Working to make a version that does not require an NBody ref.
    public static void COEtoRV(OrbitElements oe,
                                NBody centerBody,
                                ref Vector3d r,
                                ref Vector3d v,
                                bool relativePos)
    {
        COEtoRV(oe, centerBody, 0, ref r, ref v, relativePos);
    }

    public static void COEtoRVRelative(OrbitElements oe,
                            double mu,
                            ref Vector3d r,
                            ref Vector3d v)
    {
        COEtoRV(oe, null, mu, ref r, ref v, relativePos:true);
    }

    public static void COEtoRV(OrbitElements oe, 
                                NBody centerBody,
                                double mu,
                                ref Vector3d r, 
                                ref Vector3d v, 
                                bool relativePos,
                                double ecc_threshold = small,
                                double incl_threshold = small) {
        double temp, sinnu, cosnu;
        Vector3d rpqw, vpqw;

        GravityEngine ge = GravityEngine.Instance();
        if (centerBody != null)
            mu = ge.GetMass(centerBody);
 
        // --------------------  implementation   ----------------------
        //       determine what type of orbit is involved and set up the
        //       set up angles for the special cases.
        // -------------------------------------------------------------
        if (oe.ecc < ecc_threshold) {
            // ----------------  circular equatorial  ------------------

            if ((oe.incl < incl_threshold) || (Math.Abs(oe.incl - Math.PI) < incl_threshold)) {
                oe.argp = 0.0;
                oe.raan = 0.0;
                oe.nu = oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
                oe.argp = 0.0;
                oe.nu = oe.arglat;
            }
        } else {
            // ---------------  elliptical equatorial  -----------------
            if ((oe.incl < incl_threshold) || (Math.Abs(oe.incl - Math.PI) < incl_threshold)) {
                oe.argp = oe.lonper;
                oe.raan = 0.0;
            }
        }

        // ----------  form pqw position and velocity vectors ----------
        cosnu = Math.Cos(oe.nu);
        sinnu = Math.Sin(oe.nu);
        temp = oe.p / (1.0 + oe.ecc * cosnu);
        rpqw = new Vector3d(temp * cosnu, temp * sinnu, 0.0);
        if (Math.Abs(oe.p) < 0.00000001)
            oe.p = 0.00000001;
        vpqw = new Vector3d(-sinnu * Math.Sqrt(mu / oe.p),
                            (oe.ecc + cosnu) * Math.Sqrt(mu / oe.p),
                                        0.0);

        // ----------------  perform transformation to ijk  ------------
        r = GEMath.Rot3(rpqw, -oe.argp);
        r = GEMath.Rot1(r, -oe.incl);
        r = GEMath.Rot3(r, -oe.raan);

        v = GEMath.Rot3(vpqw, -oe.argp);
        v = GEMath.Rot1(v, -oe.incl);
        v = GEMath.Rot3(v, -oe.raan);
        if (ge.xzOrbits) {
            r = XZPlane.PhysicsToUnity(r);
            v = XZPlane.PhysicsToUnity(v);
        }
        if (!relativePos) {
            r += ge.GetPositionDoubleV3(centerBody);
            v += ge.GetVelocityDoubleV3(centerBody);
        }
    }  // coe2rv

    /// <summary>
    /// Transform the vector v by the rotation matrix due to the COE.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="oe"></param>
    /// <returns></returns>
    public static Vector3d TransformToOrbitFrame(Vector3d v, OrbitElements oe)
    {
        Vector3d vt = GEMath.Rot3(v, -oe.argp);
        vt = GEMath.Rot1(vt, -oe.incl);
        vt = GEMath.Rot3(vt, -oe.raan);
        return vt;
    }

    public static double PhaseAngleRadiansForDirection(Vector3d pos, OrbitElements oe)
    {
        Vector3d x_unit = new Vector3d(1, 0, 0);
        Vector3d y_unit = new Vector3d(0, 1, 0);
        Vector3d x_axis = OrbitUtils.TransformToOrbitFrame(x_unit, oe);
        Vector3d y_axis = OrbitUtils.TransformToOrbitFrame(y_unit, oe);
        double x_component = Vector3d.Dot(Vector3d.Project(pos, x_axis), x_axis);
        double y_component = Vector3d.Dot(Vector3d.Project(pos, y_axis), y_axis);
        double phaseRad = Math.Atan2(y_component, x_component);
        if (phaseRad < 0) {
            phaseRad += 2.0 * Math.PI;
        }
        return phaseRad;
    }

    public static void COEtoRVMirror(OrbitElements oe,
                                NBody centerBody,
                                ref Vector3d r,
                                ref Vector3d v,
                                bool relativePos) {

        double temp, sinnu, cosnu;
        Vector3d rpqw, vpqw;
        GravityEngine ge = GravityEngine.Instance();
        double mu = ge.GetMass(centerBody);

        // --------------------  implementation   ----------------------
        //       determine what type of orbit is involved and set up the
        //       set up angles for the special cases.
        // -------------------------------------------------------------
        if (oe.ecc < small) {
            // ----------------  circular equatorial  ------------------
            if ((oe.incl < small) | (Math.Abs(oe.incl - Math.PI) < small)) {
                oe.argp = 0.0;
                oe.raan = 0.0;
                oe.nu = oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
                oe.argp = 0.0;
                oe.nu = oe.arglat;
            }
        } else {
            // ---------------  elliptical equatorial  -----------------
            if ((oe.incl < small) | (Math.Abs(oe.incl - Math.PI) < small)) {
                oe.argp = oe.lonper;
                oe.raan = 0.0;
            }
        }

        // ----------  form pqw position and velocity vectors ----------
        cosnu = Math.Cos(oe.nu);
        sinnu = Math.Sin(oe.nu);
        temp = oe.p / (1.0 + oe.ecc * cosnu);
        // flip Y
        rpqw = new Vector3d(temp * cosnu, -temp * sinnu, 0.0);
        if (Math.Abs(oe.p) < 0.00000001)
            oe.p = 0.00000001;

        // flip X (not Y)
        vpqw = new Vector3d(sinnu * Math.Sqrt(mu / oe.p),
                            (oe.ecc + cosnu) * Math.Sqrt(mu / oe.p),
                                        0.0);

        // ----------------  perform transformation to ijk  ------------
        r = GEMath.Rot3(rpqw, -oe.argp);
        r = GEMath.Rot1(r, -oe.incl);
        r = GEMath.Rot3(r, -oe.raan);

        v = GEMath.Rot3(vpqw, -oe.argp);
        v = GEMath.Rot1(v, -oe.incl);
        v = GEMath.Rot3(v, -oe.raan);

        if (ge.xzOrbits) {
            r = XZPlane.PhysicsToUnity(r);
            v = XZPlane.PhysicsToUnity(v);
        }

        if (!relativePos) {
            r += ge.GetPositionDoubleV3(centerBody);
            v += ge.GetVelocityDoubleV3(centerBody);
        }

    }  // coe2rvMirror

    /// <summary>
    /// Returns the current phase in radian
    /// </summary>
    /// <param name="oe"></param>
    /// <returns></returns>
    public static double GetPhaseFromOE(OrbitUtils.OrbitElements oe,
                                        double ecc_threshold = small,
                                        double incl_threshold = small)
    {
        if (oe.ecc < ecc_threshold) {
            // ----------------  circular equatorial  ------------------

            if ((oe.incl < incl_threshold) || (Math.Abs(oe.incl - Math.PI) < incl_threshold)) {
               return oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
               return oe.arglat;
            }
        }
        return oe.nu;
    }

    /* -----------------------------------------------------------------------------
	*
	*                           function newtonnu
	*
	*  this function solves keplers equation when the true anomaly is known.
	*    the mean and eccentric, parabolic, or hyperbolic anomaly is also found.
	*    the parabolic limit at 168ø is arbitrary. the hyperbolic anomaly is also
	*    limited. the hyperbolic sine is used because it's not double valued.
	*
	*  author        : david vallado                  719-573-2600   27 may 2002
	*
	*  revisions
	*    vallado     - fix small                                     24 sep 2002
	*
	*  inputs          description                    range / units
	*    ecc         - eccentricity                   0.0  to
	*    nu          - true anomaly                   -2pi to 2pi rad
	*
	*  outputs       :
	*    e0          - eccentric anomaly              0.0  to 2pi rad       153.02 deg
	*    m           - mean anomaly                   0.0  to 2pi rad       151.7425 deg
	*
	*  locals        :
	*    e1          - eccentric anomaly, next value  rad
	*    sine        - sine of e
	*    cose        - cosine of e
	*    ktr         - index
	*
	*  coupling      :
	*    arcsinh     - arc hyperbolic sine
	*    sinh        - hyperbolic sine
	*
	*  references    :
	*    vallado       2013, 77, alg 5
	* --------------------------------------------------------------------------- */

    private static void NewtonNu(OrbitElements oe) {
        double sine, cose, cosnu, temp;

        double ecc = oe.ecc;
        double nu = oe.nu;
        double e0, m;

        // ---------------------  implementation   ---------------------
        e0 = 999999.9;
        m = 999999.9;
        //small = 0.00000001;

        // --------------------------- circular ------------------------
        if (Math.Abs(ecc) < small) {
            m = nu;
            e0 = nu;
        } else
        // ---------------------- elliptical -----------------------
        if (ecc < 1.0 - small) {
            cosnu = Math.Cos(nu);
            temp = 1.0 / (1.0 + ecc * cosnu);
            sine = (Math.Sqrt(1.0 - ecc * ecc) * Math.Sin(nu)) * temp;
            cose = (ecc + cosnu) * temp;
            e0 = Math.Atan2(sine, cose);
            m = e0 - ecc * Math.Sin(e0);
        } else
        // -------------------- hyperbolic  --------------------
        if (ecc > 1.0 + small) {
            if ((ecc > 1.0) && (Math.Abs(nu) + 0.00001 < Math.PI - Math.Acos(Math.Clamp(1.0 / ecc, -1.0, 1.0)))) {
                sine = (Math.Sqrt(ecc * ecc - 1.0) * Math.Sin(nu)) / (1.0 + ecc * Math.Cos(nu));
                e0 = GEMath.Asinh(sine);
                m = ecc * GEMath.Sinh(e0) - e0;
            }
        } else
        // ----------------- parabolic ---------------------
        if (Math.Acos(Math.Clamp(nu, -1.0, 1.0)) < 168.0 * Math.PI / 180.0) {
            e0 = Math.Tan(nu * 0.5);
            m = e0 + (e0 * e0 * e0) / 3.0;
        }

        if (ecc < 1.0) {
            m = (m % (2.0 * Math.PI));
            if (m < 0.0)
                m = m + 2.0 * Math.PI;
            e0 = (e0 % (2.0 * Math.PI));
        }
        oe.m = m;
        oe.eccanom = e0;
    }  // newtonnu

    /// <summary>
    /// Determine the flight path angle. This is the angle from (h x r) to the velocity vector. 
    /// 
    /// Formulae from the front pages in Vallado. 
    /// 
    /// </summary>
    /// <param name="sin_nu">Sine of the true anaomoly</param>
    /// <param name="cos_nu">Cosine of the true anomoly</param>
    /// <param name="ecc">Eccentricity</param>
    /// <returns>Flight Path Angle in Radians</returns>
    public static double FlightPathAngle(double sin_nu, double cos_nu, double ecc)
    {
        double fpa = 0; 
        if (ecc < 1.0) {
            double denom = 1 + ecc * cos_nu;
            double sin_E = sin_nu * Math.Sqrt(1 - ecc * ecc) / denom;
            double cos_E = (ecc + cos_nu) / denom;
            denom = Math.Sqrt(1 - ecc * ecc * cos_E * cos_E);
            double sin_fpa = ecc * sin_E / denom;
            double cos_fpa = Math.Sqrt(1 - ecc * ecc) / denom;
            fpa = Math.Atan2(sin_fpa, cos_fpa);
        } else {
            double denom = 1 + ecc * cos_nu;
            double sinh_H = sin_nu * Math.Sqrt(ecc * ecc - 1)/denom;
            double cosh_H = (ecc + cos_nu) / denom;
            denom = Math.Sqrt(ecc * ecc * cosh_H * cosh_H - 1.0);
            double sin_fpa = -ecc * sinh_H / denom;
            double cos_fpa = Math.Sqrt(ecc * ecc - 1) / denom;
            fpa = Math.Atan2(sin_fpa, cos_fpa);
        }
        return fpa;
    }

  /* ------------------------------------------------------------------------------
  //
  //                           function checkhitearth
  //
  //  this function checks to see if the trajectory hits the earth during the
  //    transfer.  It may calculate quicker if done in canonical units.
  //
  //  author        : david vallado                  719-573-2600   14 aug 2017
  //
  //  inputs          description                    range / units
  //    altPad      - pad for alt above surface       er  (km if 3 code changes below)
  //    r1c         - initial position vector of int  er   if km, need to be consitent with all inputs
  //    v1tc        - initial velocity vector of trns er/tu
  //    r2c         - final position vector of int    er
  //    v2tc        - final velocity vector of trns   er/tu
  //    nrev        - number of revolutions           0, 1, 2, ...
  //
  //  outputs       :
  //    hitearth    - is earth was impacted           'y' 'n'
  //    hitearthstr - is earth was impacted           "y - radii" "no"
  //
  //  locals        :
  //    sme         - specific mechanical energy
  //    rp          - radius of perigee               er
  //    a           - semimajor axis of transfer      er
  //    ecc         - eccentricity of transfer
  //    p           - semi-paramater of transfer      er
  //    hbar        - angular momentum vector of
  //                  transfer orbit
  //
  //  coupling      :
  //    dot         - dot product of vectors
  //    mag         - magnitude of a vector
  //    cross       - cross product of vectors
  //
  //  references    :
  //    vallado       2013, 503, alg 60
  //
  // ------------------------------------------------------------------------------*/

    public static bool CheckHitPlanet(
           double planetRadius, double planetMass, Vector3d r1c, Vector3d v1tc, Vector3d r2c, Vector3d v2tc, int nrev)
    {
        double rp, magh, magv1c, v1c2, a, ainv, ecc, ecosea1, esinea1, ecosea2;
        
        rp = 0.0;
        ecc = 0.0;
        ainv = 0.0;

        double magr1c = r1c.magnitude;
        double magr2c = r2c.magnitude;

        bool hitPlanet = false;

        // check whether Lambert transfer trajectory hits the Earth
        if (magr1c < planetRadius || magr2c < planetRadius) {
            // hitting earth already at start or stop point
            hitPlanet = true;
        } else {
            // canonical units  
            double rdotv1c = Vector3d.Dot(r1c, v1tc);
            double rdotv2c = Vector3d.Dot(r2c, v2tc);

            // Solve for a 
            magv1c = v1tc.magnitude;
            v1c2 = magv1c * magv1c;
            ainv = 2.0 / magr1c - v1c2/planetMass; // v1c2/3.986004418e5  if non-canonical

            // Find ecos(E) 
            ecosea1 = 1.0 - magr1c * ainv;
            ecosea2 = 1.0 - magr2c * ainv;

            // Determine radius of perigee
            // 4 distinct cases pass thru perigee (ignoring apsidal cases already checked above):
            // heading to perigee and ending after perigee
            // nrev > 0
            // both headed away from perigee, but end is closer to perigee
            // both headed toward perigee, but start is closer to perigee
            if ((rdotv1c < 0.0 && rdotv2c > 0.0) || nrev > 0 || (rdotv1c > 0.0 && rdotv2c > 0.0 && ecosea1 < ecosea2) || (rdotv1c < 0.0 && rdotv2c < 0.0 && ecosea1 > ecosea2)) {
                if (Math.Abs(ainv) <= 1.0e-10) {
                    Vector3d hbar = Vector3d.Cross(r1c, v1tc);
                    magh = hbar.magnitude; // find h magnitude
                    rp = magh * magh * 0.5;    // parabola (from DAV code)
                    Debug.LogWarning("Parabola");
                } else {
                    a = 1.0 / ainv;  // elliptical or hyperbolic orbit
                    if (ainv > 0.0) {
                        esinea1 = rdotv1c / Math.Sqrt(planetMass * Math.Abs(a)); // for elliptical (3.986004418e5 * Math.Abs(a)) if non-cannonical
                        ecc = Math.Sqrt(ecosea1 * ecosea1 + esinea1 * esinea1);
                    } else {
                        esinea1 = rdotv1c / Math.Sqrt(planetMass * Math.Abs(-a)); // for hyperbolic  (3.986004418e5 * Math.Abs(-a)) if non-cannonical
                        ecc = Math.Sqrt(ecosea1 * ecosea1 - esinea1 * esinea1);
                    }
                    rp = a * (1.0 - ecc);
                }

                if (rp < planetRadius) {
                    hitPlanet = true;
                }
            }// end of perigee check
        } // end of "hitting Earth surface?" tests
        //Debug.LogFormat("Hit:{0} :r1={1} v1={2} r2={3} v2={4} ecc={5} rp={6} ainv={7}", 
        //    hitPlanet, r1c, v1tc, r2c, v2tc, ecc, rp, ainv);
        return hitPlanet;

    } // checkhitearth

    /// <summary>
    /// Convert the eccentric anomaly (angle from center of ellipse wrt x-axis) to the true anomoly (angle from focus
    /// wrt periapsis/x-axis if e=0). 
    /// 
    /// Equations from front cover of Vallado. 
    /// </summary>
    /// <param name="eValue"></param>
    /// <param name="ecc"></param>
    /// <returns></returns>
    public static double ConvertEtoTrueAnomoly(double eValue, double ecc)
    {
        double taValue = 0.0;
        if (ecc <= 1.0) {
            double denom = 1 - ecc * Math.Cos(eValue);
            double sinTA = Math.Sin(eValue) * Math.Sqrt(1 - ecc * ecc) / denom;
            double cosTA = (Math.Cos(eValue) - ecc) / denom;
            taValue = Math.Atan2(sinTA, cosTA);
        } else {
            throw new System.Exception("Hyperbola not implemented");
        }
        return taValue;
    }

    public static double ConvertEtoMeanAnomoly(double eValue, double ecc)
    {
         return eValue - ecc * Math.Sin(eValue);
    }

    public static double ConvertTrueAnomolytoE(double nu, double ecc)
    {
        double eValue = 0.0;
        if (ecc <= 1.0) {
            double denom = 1 + ecc * Math.Cos(nu);
            double sinE = Math.Sin(nu) * Math.Sqrt(1 - ecc * ecc) / denom;
            double cosE = (Math.Cos(nu) + ecc) / denom;
            eValue = Math.Atan2(sinE, cosE);
        } else {
            throw new System.Exception("Hyperbola not implemented");
        }
        if (eValue < 0) {
            eValue += 2.0 * Math.PI;
        }
        return eValue;
    }

    private static int LOOP_LIMIT = 200;
    public static double ConvertMeanAnomolyToE(double M, double ecc)
    {
        // Vallado Algorithm 2, p65
        double En = M + ecc;
        if ((M > Math.PI) || ((M > -Math.PI) && (M < 0))) {
            En = M - ecc;
        }
        double Enext = En;
        int loops = 0;
        do {
            En = Enext;
            Enext = Enext + (M - Enext + ecc * Math.Sin(Enext)) / (1 - ecc * Math.Cos(Enext));
        } while ((Math.Abs(Enext - En) > 1E-6) && (loops++ < LOOP_LIMIT));
        if (loops >= LOOP_LIMIT) {
            Debug.LogWarningFormat("Value M={0} did not converge Enext={1}", M, Enext);
        }
        return Enext;
    }


    /* -----------------------------------------------------------------------------
  *
  *                           function gstime
  *
  *  this function finds the greenwich sidereal time (iau-82).
  *
  *  author        : david vallado                  719-573-2600    1 mar 2001
  *
  *  revisions
  *    vallado     - conversion to c#                              16 Nov 2011
  *   
  *  inputs          description                    range / units
  *    jdut1       - julian date in ut1             days from 4713 bc
  *
  *  outputs       :
  *    gstime      - greenwich sidereal time        0 to 2pi rad
  *
  *  locals        :
  *    temp        - temporary variable for doubles   rad
  *    tut1        - julian centuries from the
  *                  jan 1, 2000 12 h epoch (ut1)
  *
  *  coupling      :
  *    none
  *
  *  references    :
  *    vallado       2013, 188, eq 3-47
  * --------------------------------------------------------------------------- */

    public static double GsTime(double jdut1)
    {
        const double twopi = 2.0 * Math.PI;
        const double deg2rad = Math.PI / 180.0;
        double temp, tut1;

        tut1 = (jdut1 - 2451545.0) / 36525.0;
        temp = -6.2e-6 * tut1 * tut1 * tut1 + 0.093104 * tut1 * tut1 +
                (876600.0 * 3600 + 8640184.812866) * tut1 + 67310.54841;  // sec
        temp = (temp * deg2rad / 240.0 % twopi); //360/86400 = 1/240, to deg, to rad

        // ------------------------ check quadrants ---------------------
        if (temp < 0.0)
            temp += twopi;

        return temp;
    }  // gstime

    /* -----------------------------------------------------------------------------
        *                           procedure lstime
        *
        *  this procedure finds the local sidereal time at a given location.
        *
        *  author        : david vallado                  719-573-2600    1 mar 2001
        *
        *  inputs          description                    range / units
        *    lon         - site longitude (west -)        -2pi to 2pi rad
        *    jdut1       - julian date in ut1             days from 4713 bc
        *
        *  outputs       :
        *    lst         - local sidereal time            0.0 to 2pi rad
        *    gst         - greenwich sidereal time        0.0 to 2pi rad
        *
        *  locals        :
        *    none.
        *
        *  coupling      :
        *    gstime        finds the greenwich sidereal time
        *
        *  references    :
    *    vallado       2013, 188, eq 3-47, Alg 15
        * --------------------------------------------------------------------------- */

    public static void LsTime(double lon, double jdut1, out double lst, out double gst)
    {
        const double twopi = 2.0 * Math.PI;

        gst = GsTime(jdut1);
        lst = lon + gst;

        /* ------------------------ check quadrants --------------------- */
        lst = (lst % twopi);
        if (lst < 0.0)
            lst = lst + twopi;
    }  // lstime

    public static (Vector3, Vector3) FindNodes(OrbitPredictor ship, OrbitPredictor target)
    {

        OrbitUtils.OrbitElements oe_initial = new OrbitUtils.OrbitElements(ship.GetOrbitUniversal());
        OrbitUtils.OrbitElements oe_final = new OrbitUtils.OrbitElements(target.GetOrbitUniversal());
        Vector3d z_unit = new Vector3d(0, 0, 1);
        // 1) Find the line of intersection of the orbit planes. 
        // - normal is just the rotation of the Z_unit vector
        Vector3d n_initial = OrbitUtils.TransformToOrbitFrame(z_unit, oe_initial);
        Vector3d n_final = OrbitUtils.TransformToOrbitFrame(z_unit, oe_final);
        Vector3d lofn = Vector3d.Cross(n_initial, n_final);

        // 2) Project the line of nodes onto each orbital plane and determine it's angle to the local X_unit direction
        float u1 = (float) (OrbitUtils.PhaseAngleRadiansForDirection(lofn, oe_initial) * GEMath.RAD2DEG);
        Vector3 node1 = ship.GetOrbitUniversal().PositionForPhase(u1);
        Vector3 node2 = ship.GetOrbitUniversal().PositionForPhase(u1+180.0f);
        return (node1, node2);
    }

}
