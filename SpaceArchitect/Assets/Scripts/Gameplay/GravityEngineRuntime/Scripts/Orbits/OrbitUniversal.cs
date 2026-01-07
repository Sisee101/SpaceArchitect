using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A generic double precision orbit class. Can be used in place of OrbitEllipse and OrbitHyper. 
/// 
/// Maintains all orbital elements in double precision in internal GE units. 
/// 
/// Supports two evolve modes:
///     GRAVITY_ENGINE: Set initial velocity and poistion based on orbital elements and then let
///                     GE integrators move the body as time evolves. 
///                     
///     KEPLER: Use Kepler's equation (position, velocity as a function of time) to determine when
///             the body is each GE update. GE will still use the mass of this body to influence
///             other bodies in the scene. 
///             
/// Arbitrary nesting of these modes is expected. (i.e. Kepler motion of a moon, around a GE planet in
/// orbit around a Sun etc.)
/// 
/// Since the semi-major axis (a) can change sign as eccentricy changes, specify the scale of the 
/// orbit with p (semi-latus rectum). 
/// 
/// 
/// 
/// </summary>
public class OrbitUniversal: MonoBehaviour, INbodyInit, IFixedOrbit, IOrbitPositions, IOrbitScalable
{
    //! Mode for evolution; NBody simulation in GE or "on-rails" using Kepler equation. 
    public enum EvolveMode { GRAVITY_ENGINE, KEPLERS_EQN, SGP4_PROPAGATOR, PKEPLER_J2 };

    private NBody nbody;

    public EvolveMode evolveMode;

    //! semi-parameter that defines orbit size in GE internal units. 
    //! For a universal orbit p is needed. See SetMajorAxis() if a is needed.
    public double p = 10.0;

    //! Value of semi-parameter p from inspector (i.e. in GE selected units such as ORBITAL or SOLAR)
    public double p_inspector = 10.0;

    [SerializeField]
    public double eccentricity;

    //! inclinataion in degrees (0..180)
    public double inclination;

    //! Omega in degress (0..360)
    public double omega_uc;

    //! omega in degress (0..360)
    public double omega_lc;

    //! Phase in orbit in degrees. 
    public double phase = 0;

    // Auxillary data for SGP4 init from OrbitUniversal see Vallado p107
    // The values stored in the sgp4Data are converted from the TLE fixed point format into
    // more useful units [see SGP4toGE()]. These vars hold the values in "sgp4 units" and not TLE units.
    // Mean motion (this is a weird way to determine a). 
    public double sgp4_no; 

    // Drag coeffiecient
    public double sgp4_bstar;

    public double pkepler_ndot = 0;
    public double pkepler_nddot = 0; 

    //! Object influencing this bodies motion.
    public NBody centerNbody;

    private double mu = double.NaN;

    protected Quaternion conic_orientation;

    // ellipse
    private double orbit_period;

    // Scaling:
    // In orbitEllipse the imnplementation used a, a_phys and a_scaled and it became confusing. 
    // Here the user physical size is entered in the units chosen in the GE units selector (e.g. ORBITAL, SOLAR, DL)


    // COE in radians
    private double omega_u_rad;
    private double omega_l_rad;
    private double incl_rad;

    // Holdover values of omega_u (raan) and omega_l (argp) when have ecc < SMALL. Prevents a jump in these values
    // For TLE keep the values from initial record. These are cached as part of the dejitter operation.
    private double omega_u0_rad = 0.0;
    private double omega_l0_rad = 0.0;

    // normal to the orbital plane
    private Vector3d h_unit_xy; 

    // anomoly
    private double nu;

    // Initial conditions. Kepler evolution is driven from these values. These are with respect to an 
    // origin (0,0,0) in the centerBody frame of reference.
    private Vector3d r0;
    private Vector3d v0;
    private double time0;
    // The COE for an initFromRVT (used by PKEPLER to avoid repeated RVtoCOE on each evolve)
    private OrbitUtils.OrbitElements coe0;

    private double invLengthScale;

    private const double SMALL = 1E-6;

    protected GravityEngine ge;

    //! Flag to indicate R,V,T set explcitly. No need to refer to COE.
    private bool initFromRVT;

    //! Flag to indicate orbit is being used by orbit predictor. Do not set NBody pos/vel
    private bool isOrbitPredictor; 

    //! Used by LockAtTime()/UnlockTime()
    private bool timeLocked;
    private double[] lockedPos;
    private double[] lockedVel;

    // Editor script allows different ways to specify the size and shape (eccentricity, p or a).
    // Not all modes can work for all cases (e.g if parabola, a is infinite and need to use p)
    public enum InputMode { DOUBLE,
                            DOUBLE_ELLIPSE,
                            ELLIPSE_MAJOR_AXIS_A,
                            ELLIPSE_APOGEE_PERIGEE,
                            ECC_PERIGEE,
							//JPL_EPHEMERIS,
							TWO_LINE_ELEMENT_SET
	};
    public InputMode inputMode = InputMode.DOUBLE;

    // Can init orbit elements using the data from: https://ssd.jpl.nasa.gov/horizons.cgi in OrbitElement
    // output mode. Expected text is of the form:
    //   2451544.500000000 = A.D. 2000-Jan-01 00:00:00.0000 TDB
    // EC= 1.704239716781501E-02 QR= 9.833230998788303E-01 IN= 2.669113820737183E-04
    // OM= 1.639752443600624E+02 W = 2.977668064579176E+02 Tp=  2451546.338324738666
    // N = 9.850596796562197E-01 MA= 3.581891404220149E+02 TA= 3.581260865454548E+02
    // A = 1.000371833989169E+00 AD= 1.017420568099508E+00 PR= 3.654600908298652E+02
    //
    // as a single string. For interpretation, see the JPL web site. 
    //
    public string jplEphemeris = "";

    // NORAD Two Line Element Set Data e.g.
    //    ATLAS CENTAUR 2         
    //1 00694U 63047A   20041.86290382  .00000491  00000-0  50565-4 0  9993
    //2 00694  30.3549 233.3186 0585871 158.9774 203.5990 14.02563639818796
    public string tleName = "";
    public string tleLine1 ="";
    public string tleLine2 ="";

    // SGP4 object for SGP4 prop
    private SGP4toGE sgpForTLE;

    // OrbitPredictor has a "dejitter" mode where it prevents dithering around the threshold used to denote
    // a circular (e=0) or equitorial (i=0) orbit. The default threshold is 1E-3.
    // 1) If an orbit goes from below the
    //    threshold to above, the threshold is changed to 0.5E-4. Once it re-establishes circular/equatorial then the
    //    threshold resets to 1E-3. 
    // 2) If an orbit goes from above to below, the threshold changes to 0.5E-2. If the orbit then exceeds this threshold the
    //    limit is set to 1E-3
    public bool dejitterCOE = false;

    private const double THRESHOLD = 1E-3;
    private const double THRESHOLD_LOW = 0.5 * THRESHOLD;
    private const double THRESHOLD_HI = 2.0*THRESHOLD;
    private OrbitUtils.OrbitElements oe_last;
    private double ecc_threshold = THRESHOLD;
    private double incl_threshold = THRESHOLD;

    // No "real code" in Awake() or Start(). Init is via GE or OrbitPredictor (or via OnDrawGizmos in Editor mode)

    void Awake() {
        ge = GravityEngine.Instance();
        lockedPos = new double[3];
        lockedVel = new double[3];
    }

    // Interface INBodyInit
    public virtual void InitNBody(float physicalScale, float massScale) {
        // GE 8.0. If want to set evolution to some earlier time via InitRVT() do not stomp on the time value
        if (!initFromRVT) {
            time0 = GravityEngine.Instance().GetPhysicalTimeDouble();
        }
        p = p_inspector / physicalScale * GravityEngine.Instance().GetLengthScale();
        Init();
    }

    /// <summary>
    /// Use an active NBody that is live in GE to initialize an orbit. 
    /// 
    /// Can be used in Kepler mode to determine the future position of the object by subsequently calling Evolve(). 
    /// Used by LambertPhasing to propogate the target for the time required for the transfer. 
    /// </summary>
    public void InitFromActiveNBody(NBody activeNbody, NBody center, EvolveMode mode) {
        ge = GravityEngine.Instance();
        evolveMode = mode;
        nbody = activeNbody;
        Vector3d r = ge.GetPositionDoubleV3(activeNbody);
        Vector3d v = ge.GetVelocityDoubleV3(activeNbody);
        double t = ge.GetPhysicalTimeDouble();
        InitFromRVT(r, v, t, center, false);
    }

    public void InitFromJplEphemeris(string data)
    {
        CheckUnits(GravityScaler.Units.SOLAR);
        jplEphemeris = data;
        JPLEphemerisParser.InitOrbitU(this, data);
    }

    /// <summary>
    /// Init the classical orbital element (COE) from Celestrak two-line element (TLE) information.
    /// 
    /// The TLE info describes the orbital parameters and position at a time reference embedded in the TLE. 
    /// 
    /// It is necessary to advance the position and orbital elements to the start time (as a Julian day from GE). 
    /// This is done using the SGP4 propagator. 
    /// 
    /// The time since GE started is then evolved using a standard GE orbit propagator. 
    /// 
    /// </summary>
    public void InitCOEFromTLEData()
    {
        ge = GravityEngine.Instance();
        GravityScaler.Init(); // might be called from inspector

        if (double.IsNaN(mu))
            mu = ge.GetMass(centerNbody) + ge.GetMass(nbody);
        CheckUnits(GravityScaler.Units.ORBITAL);
        sgpForTLE = new SGP4toGE(tleName, tleLine1, tleLine2);
        // fill in sgp4_ to facilitate switch from TLE to OU
        sgpForTLE.UpdateSGP4AuxData(this);
        // Get the initial arpg and raan
        SGP4.SGP4SatData satData = sgpForTLE.GetSatData();
        omega_l0_rad = satData.argpo;
        omega_u0_rad = satData.nodeo;

        // TLE is defined at a specific epoch time and GE start time will be some time after this
        // SGP4 code will propagate from TLE epoch time to GE start time. Need to evolve the initial state
        // from the TLE **using the propagator that has been selected for this orbit**
        // (Prior to 13.0 this was done with SGP4 in all cases. Now fixed)
        time0 = ge.GetPhysicalTime();
        double timeJD = ge.GetTimeAsJulianDate(time0);

        // convert mean anomoly to phase at GE start time (advance mean anomoly up to GE time)
        double minutesSinceEpoch = (timeJD - satData.jdsatepoch) * 24.0 * 60.0;
        double M = (satData.mo + satData.mdot * minutesSinceEpoch) % (2.0 * Math.PI);
        double startPhase = OrbitUtils.ConvertEtoTrueAnomoly(OrbitUtils.ConvertMeanAnomolyToE(M, eccentricity), eccentricity);
        startPhase *= GEMath.RAD2DEG;
        if (startPhase < 0)
            startPhase += 360.0;

        switch (evolveMode) {
            case EvolveMode.GRAVITY_ENGINE:
            case EvolveMode.SGP4_PROPAGATOR:
                (int error, Vector3d r, Vector3d v) = sgpForTLE.SGP4toRVatTime(timeJD);
                if (error != 0) {
                    Debug.LogError(string.Format("Error {0} initing for {1}", SGP4toGE.errString(error), gameObject.name));
                    if (error == 3) {
                        Debug.LogError(string.Format("Sat date: {0} is after GE start time.",
                            sgpForTLE.TLEDateString()));
                    }
                    return;
                } else {

                    r0 = r;
                    v0 = v;
                    if (ge.xzOrbits) {
                        r0 = XZPlane.PhysicsToUnity(r0);
                        v0 = XZPlane.PhysicsToUnity(v0);
                    }
                    SetCOEFromSatData(sgpForTLE.GetSatData());
                    // Circular bias case. If we kept argp even though ecc near zero, need to adjust phase
                    // only impacts inspector, since evolution will be from SGP4 code
                    phase = startPhase;
                } 
                break;

            case EvolveMode.KEPLERS_EQN:
                // preserve the orbit elements from the TLE directly
                SetCOEFromSatData(sgpForTLE.GetSatData());
                phase = startPhase;
                break;

            case EvolveMode.PKEPLER_J2:
                // Need to build a COE and then evolve it up to the start time and then put that into a
                // COE0
                // preserve the orbit elements from the TLE directly
                omega_lc = omega_l0_rad * GEMath.RAD2DEG;
                omega_uc = omega_u0_rad * GEMath.RAD2DEG;
                incl_rad = satData.inclo;
                inclination = incl_rad * GEMath.RAD2DEG;
                eccentricity = satData.ecco;
                // need to scale
                p_inspector = satData.a * (1 - eccentricity * eccentricity);
                p_inspector *= SGP4.SGP4unit.RADIUSEARTHKM;
                p = p_inspector * GravityEngine.Instance().lengthScale;
                phase = OrbitUtils.ConvertEtoTrueAnomoly(
                               OrbitUtils.ConvertMeanAnomolyToE(satData.mo, eccentricity), eccentricity) * Mathd.Rad2Deg;
                OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements(this);
                double dtsec = minutesSinceEpoch * 60.0;
                coe0 = PKepler.PKeplerPropToCOE(oe, dtsec, mu, scaleToKm: invLengthScale);
                // TODO: Get new COE elements from advance to start time
                break;

        }
        

    }

    /// <summary>
    /// If evolve mode is SGP4 and do not have TLE data, need to create an SGP entity from COE
    /// </summary>
    private void InitSGP4FromCOE()
    {
        ge = GravityEngine.Instance();
        if (double.IsNaN(mu))
            mu = ge.GetMass(centerNbody) + ge.GetMass(nbody);
        CheckUnits(GravityScaler.Units.ORBITAL);
        sgpForTLE = new SGP4toGE(gameObject.name, this);
    }

    private void CheckUnits(GravityScaler.Units requiredUnits)
    {
        if (GravityEngine.Instance().units != requiredUnits) {
            Debug.LogWarning(string.Format("Input mode {0} requires units be set to {1}", inputMode, requiredUnits));
        }
    }

    // Used only from OrbitUniversal Editor Script
    public string TLEStartEpoch()
    {
        if (inputMode == OrbitUniversal.InputMode.TWO_LINE_ELEMENT_SET)
        {
            if (sgpForTLE == null)
            {
                InitCOEFromTLEData();
            }
            return SGP4.SGP4utils.EpochDateString(sgpForTLE.GetSatData());
        }
        else
        {
            return "";
        }
    }

    public double TLEStartJD()
    {
        return sgpForTLE.GetSatData().jdsatepoch;
    }

    public void Init() {

        ge = GravityEngine.Instance();
        invLengthScale = 1.0 / ge.lengthScale;

        if (nbody == null) {
            nbody = GetComponent<NBody>();
        }

        if (centerNbody == null)
            centerNbody = OrbitUtils.GetCenterNbody(transform, null);
		if (centerNbody == null) {
            Debug.LogError("Failed to get center for " + gameObject.name);
            return;
		}
        // 5.0: Use M+m (precision is required for Lagrange points)
        // Only retrieve masses if mu not set (e.g. OrbitPredictor use case does this a LOT, so sets once)
        if (double.IsNaN(mu)) {
            mu = ge.GetMass(centerNbody) + ge.GetMass(nbody);
        }


        if (initFromRVT) {
            RVtoCOEWrapper();
            if (!isOrbitPredictor) {
                nbody.SetPosVel3d(r0 + ge.GetPositionDoubleV3(centerNbody), v0 + ge.GetVelocityDoubleV3(centerNbody));
            }
            if (evolveMode == EvolveMode.SGP4_PROPAGATOR) {
                sgpForTLE = new SGP4toGE(gameObject.name, this);
                sgp4BlendTime = sgpForTLE.ReInitFromRV(r0, v0, time0, mu);
            } else if (evolveMode == EvolveMode.PKEPLER_J2) {
                coe0 = OrbitUtils.RVtoCOE(r0, v0, centerNbody, relativePos: true,
                            ecc_threshold: PKepler.LIMIT, incl_threshold: PKepler.LIMIT);
            }
        } else {
            if (inputMode == InputMode.TWO_LINE_ELEMENT_SET) {
                InitCOEFromTLEData();
            }
            omega_u_rad = omega_uc * Mathd.Deg2Rad;
            omega_l_rad = omega_lc * Mathd.Deg2Rad;
            // cache values in case they get used for near zero ecc/incl
            omega_u0_rad = omega_u_rad;
            omega_l0_rad = omega_l_rad;
            incl_rad = inclination * Mathd.Deg2Rad;
            InitFromCOE();
            if ((evolveMode == EvolveMode.SGP4_PROPAGATOR) && (inputMode != InputMode.TWO_LINE_ELEMENT_SET)) {
                InitSGP4FromCOE();
            }
        }

        CalculateRotation();
        GetPeriod();

    }

    /// <summary>
    /// Reset the OrbitPredictor init mode. 
    /// 
    /// Current use case is test code that re-uses OUs. Some maneuvers may have set this. 
    /// </summary>
    public void Reset()
    {
        initFromRVT = false;
    }

    /// <summary>
    /// For ellipses can specify size using major-axis. Typically used by editor script, but may have other uses.
    /// </summary>
    /// <param name="a"></param>
    public void SetMajorAxisInspector(double a) {
        if (eccentricity < 1.0) {
            p_inspector = a * (1 - eccentricity * eccentricity);
        } else {
            Debug.LogWarning("Cannot set size via major axis if not an ellipse.");
        }
    }

    /// <summary>
    /// Get the major axis for the orbit using the inspector value (world units). 
    /// - for a hyperbola this is negative
    /// - for a parabola this is NaN
    /// </summary>
    /// <returns></returns>
    public double GetMajorAxisInspector() {
        if (Mathd.Abs(eccentricity-1.0) < 1E-6) {
            // parabola
            return double.NaN;
        } 
        return p_inspector / (1 - eccentricity * eccentricity);
    }

    /// <summary>
    /// Get the Major Axis is internal physical units. If the orbit is an ellipse the major axis is valid. 
    /// If the orbit is a hyperbola, then the value of p will be negative and there is no major axis value. 
    /// </summary>
    /// <returns></returns>
    public double GetMajorAxis() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            return 0; 
        }
        return p / (1 - eccentricity * eccentricity);
    }

    /// <summary>
    /// Get the apogee (point of greatest distance from focus) for the orbit in Scaled units
    /// (e.g. ORBITAL). This is typically used in the inspector prior to the game starting. 
    /// 
    /// Typically used for an ellipse. For a hyperbola will be a negative number. 
    /// </summary>
    /// <returns></returns>
    public double GetApogeeInspector() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return double.NaN;
        }
        double a = p_inspector / (1 - eccentricity * eccentricity);
        // will be negative for a hyperbola (a < 0)
        return a * (1 + eccentricity);
    }

    /// <summary>
    /// Determine the orbit apogee (apoapsis) is internal physics units. 
    /// 
    /// Typically used for an ellipse. For a hyperbola will be a negative number. 
    /// </summary>
    /// <returns>apogee value in internal physics units</returns>
    public double GetApogee() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return double.NaN;
        }
        double a = p / (1 - eccentricity * eccentricity);
        // will be negative for a hyperbola (a < 0)
        return a * (1 + eccentricity);
    }

    public double GetSemiParam() {
        return p; 
    }

    public SGP4toGE GetSGP4ToGE()
    {
        return sgpForTLE;
    }

    /// <summary>
    /// Compute a real-time value for the orbital energy based on (r,v)
    /// </summary>
    /// <returns></returns>
    public double GetEnergy()
    {
        double v = ge.GetVelocityDoubleV3(nbody).magnitude;
        Vector3d r = ge.GetPositionDoubleV3(nbody) - ge.GetPositionDoubleV3(centerNbody);
        return 0.5 * v * v - mu / r.magnitude;
    }

    /// <summary>
    /// Get the perigee (point of closest approach to the focus) for the orbit in Scaled units
    /// (e.g. ORBITAL). This is typically used in the inspector prior to the game starting. 
    /// 
    /// Valid for all orbit types.
    /// 
    /// </summary>
    /// <returns></returns>
    public double GetPerigeeInspector() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return p_inspector/2.0;
        }
        double a = p_inspector / (1 - eccentricity * eccentricity);
        return a * (1 - eccentricity);
    }

    /// <summary>
    /// Determine the orbit preigee (periapsis) in internal physics units. 
    /// 
    /// </summary>
    /// <returns>perigee value in internal physics units</returns>
    public double GetPerigee() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return p / 2.0;
        }
        double a = p / (1 - eccentricity * eccentricity);
        return a * (1 - eccentricity);
    }

    /// <summary>
    /// Set the size and eccentricity of the ellipse by using values for apogee and perigee. 
    /// (To be precise really should call them apoapsis and periapsis, but more people will 
    /// know apogee/perigee).
    /// 
    /// Typically only used for ellipses (but can work for hyperbolas) by the Editor script for
    /// this component.
    /// 
    /// Values provided are in GE scaled units (e.g. ORBITAL)
    /// 
    /// </summary>
    /// <param name="apogee"></param>
    /// <param name="perigee"></param>
    public void SetSizeWithApogeePerigee(double apogee, double perigee) {
        double a_new = 0.5 * (apogee + perigee);
        eccentricity = apogee / a_new - 1.0;
        SetMajorAxisInspector(a_new);
    }

    public void SetSizeWithEccPerigee(double ecc, double perigee) {
        double a_new = perigee / (1 - ecc); 
        eccentricity = ecc;
        p_inspector = a_new * (1 - ecc * ecc);
    }

    public void SetOrbitPredictor(bool flag)
    {
        isOrbitPredictor = flag;
    }

    /// <summary>
    /// In SGP4 propagation the satellite can decay to the point where it should not be propagated. 
    /// Users may wish to trigger a callback on this event. This allows for a callback to be registered.
    /// </summary>
    /// <param name="ou"></param>
    public delegate void SGP4ErrorCallback(OrbitUniversal ou, int error);

    private SGP4ErrorCallback errorCallback;

    /// <summary>
    /// Callback for when an SGP4 satellite decays. 
    /// 
    /// Warning: This is called from within the GE physics loop. Do not direcly call RemoveBody() while
    /// inside this code!
    /// </summary>
    /// <param name="errorCallback"></param>
    public void AddSGP4ErrorCallback(SGP4ErrorCallback errorCallback)
    {
        this.errorCallback = errorCallback;
    }

    public double GetMu() {
        return mu;
    }

    /// <summary>
    /// Set the mass of the orbit system. This is intended for use when OrbitU is not attached to an NBody 
    /// e.g. in the case of a DustOrbit. 
    /// 
    /// Normally mu is determined correctly by an Init() call without recorse to this function. 
    /// </summary>
    /// <param name="mu"></param>
    public void SetMu(double mu)
    {
        this.mu = mu;
    }

	/// <summary>
	/// Init the value of mu from the nbody elements. Typically used by OrbitPredictor. 
	/// </summary>
	public void InitMu()
	{
        ge = GravityEngine.Instance();
        mu = ge.GetMass(nbody) + ge.GetMass(centerNbody);
	}

    public NBody GetNBody() {
        return nbody;
    }

    /// <summary>
    /// Get the normal vector to the orbital plane (normalized). 
    /// </summary>
    /// <returns></returns>
    public Vector3d GetAxis() {
        return Vector3d.Cross(r0, v0).normalized;
    }

    /// <summary>
    /// Apply COE orientation parameters using XY orbital plane. 
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private Vector3d ApplyRotations(Vector3d v) {
        double[] v_out = new double[] { 0, 0, 0 };

        v_out[0] = (Mathd.Cos(omega_u_rad) * Mathd.Cos(omega_l_rad) -
                    Mathd.Sin(omega_u_rad) * Mathd.Sin(omega_l_rad) * Mathd.Cos(incl_rad)) * v.x -
                   (Mathd.Cos(omega_u_rad) * Mathd.Sin(omega_l_rad) +
                    Mathd.Sin(omega_u_rad) * Mathd.Cos(omega_l_rad) * Mathd.Cos(incl_rad)) * v.y +
                    (Mathd.Sin(omega_u_rad) * Mathd.Sin(incl_rad)) * v.z;

        v_out[1] = (Mathd.Sin(omega_u_rad) * Mathd.Cos(omega_l_rad) +
            Mathd.Cos(omega_u_rad) * Mathd.Sin(omega_l_rad) * Mathd.Cos(incl_rad)) * v.x -
           (Mathd.Sin(omega_u_rad) * Mathd.Sin(omega_l_rad) -
            Mathd.Cos(omega_u_rad) * Mathd.Cos(omega_l_rad) * Mathd.Cos(incl_rad)) * v.y -
            (Mathd.Cos(omega_u_rad) * Mathd.Sin(incl_rad)) * v.z;

        v_out[2] = (Mathd.Sin(omega_l_rad) * Mathd.Sin(incl_rad)) * v.x +
                    (Mathd.Cos(omega_l_rad) * Mathd.Sin(incl_rad)) * v.y +
                    Mathd.Cos(incl_rad) * v.z;

        return new Vector3d(v_out[0], v_out[1], v_out[2]);
    }

    /// <summary>
    /// Determine the initial position and velocity (r0, v0) from the Classical Orbital Elements. 
    /// To be general (all types of orbits) use semi-parameter p instead of a.
    /// 
    /// To preserve double throughout use the explicit form of the rotation matrix, instead of creating 
    /// a double Quaternion. 
    /// 
    /// Use Algorithm 10 from Vallado. p118
    /// </summary>
    private void InitFromCOE() {

        if (double.IsNaN(mu))
            Debug.LogError("Internal error failed to init mu");

        nu = phase * Mathd.Deg2Rad;

        double r_denom = 1 + eccentricity * Mathd.Cos(nu);
        Vector3d r_pqw = new Vector3d(p * Mathd.Cos(nu) / r_denom, p * Mathd.Sin(nu) / r_denom, 0);
        r0 = ApplyRotations(r_pqw);

        if (Mathd.Abs(mu) < 1E-12) {
            Debug.LogWarningFormat("Central mass {0} of {1} is near zero. ", centerNbody.gameObject.name, nbody.name);
        }
        double v_coeef = Mathd.Sqrt(mu / p);
        Vector3d v_pqw = new Vector3d(-v_coeef * Mathd.Sin(nu), v_coeef * (eccentricity + Mathd.Cos(nu)), 0);
        v0 = ApplyRotations(v_pqw);
        Vector3d h = Vector3d.Cross(r0, v0);
        h_unit_xy = h.normalized;
        if (ge.xzOrbits) {
            r0 = XZPlane.PhysicsToUnity(r0);
            v0 = XZPlane.PhysicsToUnity(v0);
        }
        // Seems a bit weird to re-init a COE from r,v but need to ensure this is done consistently using RVtoCOE
        // when using PKepler. Also need high precision for ecc & incl
        double limit = OrbitUtils.small;
        if (evolveMode == EvolveMode.PKEPLER_J2)
            limit = PKepler.LIMIT;
        coe0 = OrbitUtils.RVtoCOE(r0, v0, centerNbody, relativePos: true,   
                    ecc_threshold: limit, incl_threshold: limit);


        Vector3d centerPos;
        Vector3d centerVel;
        // used by widgets - so need to get explcitly
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPositionDoubleV3(centerNbody);
            centerVel = GravityEngine.Instance().GetVelocityDoubleV3(centerNbody);
        }
        else {
            // setup - not yet added to GE
            if ((nbody != null) && (nbody.initWithDouble)) {
                centerPos = centerNbody.initialPhysPositionV3;
                centerVel = centerNbody.vel_physV3;
            }
            else {
                centerPos = new Vector3d(centerNbody.initialPhysPosition);
                centerVel = new Vector3d(centerNbody.vel_phys);
            }
        }
        // Update NBody (particles will have null nbody)
        if (!isOrbitPredictor && (nbody != null)) {
            nbody.SetPosVel3d(r0 + centerPos, v0 + centerVel);
        }
    }



    /// <summary>
    /// Initialize the Orbit from orbital elements contained in an OrbitData object. 
    /// </summary>
    /// <param name="od"></param>
    public void InitFromOrbitData(OrbitData od, double time) {
        eccentricity = od.ecc;
        omega_lc = od.omega_lc;
        omega_l_rad = od.omega_lc * Mathf.Deg2Rad;
        omega_uc = od.omega_uc;
        omega_u_rad = od.omega_uc * Mathf.Deg2Rad;
        inclination = od.inclination;
        incl_rad = od.inclination * Mathf.Deg2Rad;
        phase = od.phase;
        p = od.a * (1 -od.ecc * od.ecc);
        p_inspector = p / GravityEngine.Instance().lengthScale;
        time0 = time;
        centerNbody = od.centralMass;
        Init();
    }

    /// <summary>
    /// Init from another OrbitUniversal 
    /// </summary>
    /// <param name="od"></param>
    public void CopyFromOrbitUniversal(OrbitUniversal fromOrbit) {
        nbody = fromOrbit.nbody;
        evolveMode = fromOrbit.evolveMode;
        initFromRVT = fromOrbit.initFromRVT;
        centerNbody = fromOrbit.centerNbody;
        ge = GravityEngine.Instance();
        mu = fromOrbit.mu;
        // OE
        eccentricity = fromOrbit.eccentricity;
        omega_lc = fromOrbit.omega_lc;
        omega_l_rad = fromOrbit.omega_lc * Mathf.Deg2Rad;
        omega_uc = fromOrbit.omega_uc;
        omega_u_rad = fromOrbit.omega_uc * Mathf.Deg2Rad;
        inclination = fromOrbit.inclination;
        incl_rad = fromOrbit.inclination * Mathf.Deg2Rad;
        phase = fromOrbit.phase;
        p = fromOrbit.p;
        p_inspector = fromOrbit.p_inspector;
        nu = fromOrbit.nu;
        // RVT
        time0 = fromOrbit.time0;
        r0 = fromOrbit.r0;
        v0 = fromOrbit.v0;
        coe0 = new OrbitUtils.OrbitElements(fromOrbit.coe0);
        pkepler_ndot = fromOrbit.pkepler_ndot;
        pkepler_nddot = fromOrbit.pkepler_nddot;

        isOrbitPredictor = fromOrbit.isOrbitPredictor;
        h_unit_xy = fromOrbit.h_unit_xy;
        inputMode = fromOrbit.inputMode;
        invLengthScale = fromOrbit.invLengthScale;

        // skip locked stuff
        // sgp4
        sgp4_bstar = fromOrbit.sgp4_bstar;
        sgp4_no = fromOrbit.sgp4_no;
        sgp4BlendTime = fromOrbit.sgp4BlendTime;
        tleName = fromOrbit.tleName;
        tleLine1 = fromOrbit.tleLine1;
        tleLine2 = fromOrbit.tleLine2;
        if (fromOrbit.sgpForTLE != null) {
            sgpForTLE = new SGP4toGE(fromOrbit.sgpForTLE);
        }
        errorCallback = fromOrbit.errorCallback;
    }

    /// <summary>
    /// Initialize the orbit using position, velocity and time. 
    /// 
    /// Position and velocity are relative to the center object. (This is because
    /// when eg. adding a segement arount a moon in free return calculations cannot
    /// assume we mean the current position of the center).
    /// 
    /// </summary>
    /// <param name="r">relative position wrt center</param>
    /// <param name="v">relative velocity wrt center</param>
    /// <param name="time"></param>
    /// <param name="center"></param>
    public void InitFromRVT(Vector3d r, Vector3d v, double time, NBody center, bool relativePos, bool updateState = false) {
        centerNbody = center;
        if (relativePos) {
            r0 = r;
            v0 = v;
        } else { 
            r0 = r - ge.GetPositionDoubleV3(center);
            v0 = v - ge.GetVelocityDoubleV3(center);
        }
        time0 = time;
        initFromRVT = true;
        Init();
        // Multiplayer notification
        if (!isOrbitPredictor && (ge.geMultiplayerIF != null)) {
            ge.geMultiplayerIF.OrbitChanged(nbody, this);
        }

        // Ensure gravity state cache stays up to date. 
        if (!isOrbitPredictor && updateState) {
            Vector3d vel = v0 + ge.GetVelocityDoubleV3(center);
            Vector3d pos = r0 + ge.GetPositionDoubleV3(center);
            // When adding maneuvers to a KS, if this is a later maneuver then do not want to do this!
            ge.GetWorldState().UpdateInternalDouble(nbody, pos, vel);
        }
    }

    public void SetDejitterCOE(bool value)
    {
        dejitterCOE = value;
    }

    /// <summary>
    /// Preserve initial omega values from the source orbit in case we end up with a near zero
    /// eccentricity/inclination but still want to report the non-zero omegas (argp, raan) values.
    ///
    /// Most commonly used when an OU in an orbit predictor is initialized. 
    /// </summary>
    /// <param name="ou"></param>
    public void CacheArgpRaan(OrbitUniversal ou)
    {
        omega_u0_rad = ou.omega_u0_rad;
        omega_l0_rad = ou.omega_l0_rad;
    }

    /// <summary>
    /// OrbitPredictor does not need to do a full re-init, just use RV to set new COE, or get values from
    /// inside the prop to avoid extra conversion work and loss of accuracy.
    /// 
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// <param name="time"></param>
    public void ReInitForOrbitPredictor(Vector3d r, Vector3d v, double time, OrbitUniversal nbodyOrbit)
    {
        if ((nbodyOrbit != null) && (nbodyOrbit.evolveMode == EvolveMode.SGP4_PROPAGATOR)) {
            // this is useful in GetSpecial even though since this is an orbitPredictor OU evolve
            // will never be called.
            evolveMode = EvolveMode.SGP4_PROPAGATOR;
            SetCOEFromSatData(nbodyOrbit.GetSGP4ToGE().GetSatData());

        } else {
            r0 = r - ge.GetPositionDoubleV3(centerNbody);
            v0 = v - ge.GetVelocityDoubleV3(centerNbody);
            // Need to get new Orbital Elements
            Vector3d r0_xy = r0;
            Vector3d v0_xy = v0;
            if (ge.xzOrbits) {
                // if it was XZ we want to undo that
                r0_xy = XZPlane.UnityToPhysics(r0);
                v0_xy = XZPlane.UnityToPhysics(v0);
            }
            h_unit_xy = Vector3d.Cross(r0_xy, v0_xy).normalized;
            // RVtoCOE will do the XZ orbit converstion to XY if necessary
            OrbitUtils.OrbitElements oe;
            if (dejitterCOE) {
                oe = OrbitUtils.RVtoCOE(r0, v0, centerNbody, mu, relativePos: true, ecc_threshold: ecc_threshold, incl_threshold: incl_threshold);
                // eccentricity
                if (oe.IsCircular() && !oe_last.IsCircular()) {
                    ecc_threshold = THRESHOLD_HI;
                }
                if (!oe.IsCircular() && oe_last.IsCircular()) {
                    ecc_threshold = THRESHOLD_LOW;
                }
                if (oe.IsInclined() && !oe_last.IsInclined()) {
                    incl_threshold = THRESHOLD_LOW;
                }
                if (!oe.IsInclined() && oe_last.IsInclined()) {
                    incl_threshold = THRESHOLD_HI;
                }
            } else {
                oe = OrbitUtils.RVtoCOE(r0, v0, centerNbody, mu, relativePos: true);
            }
            oe_last = oe;
            SetCOEfromOrbitElements(oe);

        } 
    }

    private void SetCOEFromSatData(SGP4.SGP4SatData satData)
    {
        eccentricity = satData._el;
        omega_u_rad = satData._xnode;
        omega_uc = omega_u_rad * Mathd.Rad2Deg;
        omega_l_rad = satData._argpp;
        omega_lc = omega_l_rad * Mathd.Rad2Deg;
        inclination = satData._xinc * Mathd.Rad2Deg;
        double a = satData._am * ge.lengthScale;
        p = a * (1 - eccentricity * eccentricity);
        p_inspector = p / ge.lengthScale;
        // su is omega + nu
        nu = (satData._su - omega_l_rad) % (2.0 * Math.PI);
        if (nu < 0)
            nu += 2.0 * Math.PI;
        phase = nu * Mathd.Rad2Deg;
    }

    /// <summary>
    /// When this OrbitU is being used as an orbit predictor report the last type that was determined by the RVtoCOE. 
    /// </summary>
    /// <returns></returns>
    public OrbitUtils.OrbitElements.TypeOrbit PredictedOrbitType()
    {
        return oe_last.typeOrbit;
    }

    /// <summary>
    /// Inits from solar body. This will always be an ellipse. 
    /// </summary>
    /// <param name="sbody">Sbody.</param>
    public void InitFromSolarBody(SolarBody sbody) {
        eccentricity = sbody.ecc;
        SetMajorAxisInspector(sbody.a);
        omega_lc = sbody.omega_lc;
        omega_uc = sbody.omega_uc;
        inclination = sbody.inclination;
        phase = sbody.longitude;
        Init();
        ApplyScale(GravityEngine.Instance().GetLengthScale());
    }


    // Simliar to code in OrbitData but not quite the same...
    private void RVtoCOEWrapper() {
        Vector3d r0_xy = r0;
        Vector3d v0_xy = v0; 
        if (ge.xzOrbits) {
            // if it was XZ we want to undo that
            r0_xy = XZPlane.UnityToPhysics(r0);
            v0_xy = XZPlane.UnityToPhysics(v0);
        }
        h_unit_xy = Vector3d.Cross(r0_xy, v0_xy).normalized;
        // RVtoCOE will do the XZ orbit converstion to XY if necessary
        double limit = OrbitUtils.small;
        if (evolveMode == EvolveMode.PKEPLER_J2)
            limit = PKepler.LIMIT;
        coe0 = OrbitUtils.RVtoCOE(r0, v0, centerNbody, mu, relativePos: true, 
                                ecc_threshold: limit, incl_threshold: limit);
        SetCOEfromOrbitElements(coe0);
        oe_last = coe0;
        if (isOrbitPredictor && dejitterCOE) {
            if (coe0.IsCircular())
                ecc_threshold = THRESHOLD_HI;
            else
                ecc_threshold = THRESHOLD_LOW;
            if (coe0.IsInclined())
                incl_threshold = THRESHOLD_LOW;
            else
                incl_threshold = THRESHOLD_HI;
        }
    }

    /// <summary>
    /// Special case handling for near circular or near flat orbits.
    ///
    /// Need to get all three as a self-consistent set, since eg if a user is keen to have
    /// an argp when e=0 and want to keep it, then the phase is wrt that argp.
    ///
    /// If the orbit ever becomes e.g. non-flat, then want to zero out the cached oU so we do not impose it
    /// if we return to a near flat orbit.
    ///
    /// Why? Consider a TLE with e=0 and an argp of 100 degrees. RVtoCOE will declare such an orbit
    /// circular and report argp=0 and phase wrt X-axis. User may wish to preserve the initial argp
    /// so it easier to compare the current orbit with the initial one. 
    /// 
    /// </summary>
    /// <returns></returns>
    public (double omegaU, double omegaL, double phase) SpecialOrientationPhase()
    {
        double oU = omega_u_rad;
        double oL = omega_l_rad;
        double ph = phase * GEMath.DEG2RAD;
        // Now that SGP4 COE comes direct from satData, we can use those values "as-is"
        if (evolveMode == EvolveMode.SGP4_PROPAGATOR)
            return (oU, oL, ph);

        bool circular = (eccentricity < OrbitUtils.small);
        bool flat = (inclination < OrbitUtils.small);
        // if we ever drift out of flat or circ, then stop preserving the initial value.
        // Q: Do we want to hold e.g. the argp as an orbit circlarizes to avoid a phase
        // disconinuity??
        if (!circular)
            omega_l0_rad = 0;
        if (!flat)
            omega_u0_rad = 0;
        // three special cases:
        // non-circular equitorial, circular inclined, circular equatorial
        if (circular) {
            if (flat) {
                // circular equitorial
                oU = omega_u0_rad;
                oL = omega_l0_rad;
                ph = (ph - omega_u0_rad - omega_l0_rad) % (2.0 * Math.PI);
            } else {
                // circular inclined
                oL = omega_l0_rad;
                ph -= omega_l0_rad;
            }
        } else {
            if (flat) {
                // non-circular equitorial
                oU = omega_u0_rad;
                oL -= omega_u0_rad;
                if (oL < 0)
                    oL += 2.0 * Math.PI;
                // GetPhase in RVtoCOE has done correction.
            }
        }
        if (ph < 0)
            ph += 2.0 * Math.PI;
        return (oU, oL, ph);
    }

    public OrbitUtils.OrbitElements GetPKeplerCOE()
    {
        return coe0;
    }

    private void SetCOEfromOrbitElements(OrbitUtils.OrbitElements oe)
    {
        p = oe.p;
        p_inspector = p / GravityEngine.Instance().lengthScale;
        eccentricity = oe.ecc;
        inclination = Mathd.Rad2Deg * (float)oe.incl;
        incl_rad = oe.incl;
        omega_uc = 0;
        omega_lc = 0;
        // type was set based on precision when asked for oe, ecc_threshold etc, are ok
        if (oe.IsInclined()) {
            if (!oe.IsCircular()) {
                omega_uc = Mathd.Rad2Deg * (float)oe.raan;
                omega_lc = Mathd.Rad2Deg * (float)oe.argp;
            } else {
                omega_uc = Mathd.Rad2Deg * (float)oe.raan;
            }
        } else {
            // equatorial
            if (!oe.IsCircular()) {
                omega_uc = 0;
                omega_lc = Mathd.Rad2Deg * (float)oe.lonper;
            }
        }
        if (omega_uc >= 360.0)
            omega_uc -= 360.0;
        if (omega_lc >= 360.0)
            omega_lc -= 360.0;
        omega_u_rad = omega_uc * Mathd.Deg2Rad;
        omega_l_rad = omega_lc * Mathd.Deg2Rad;
        if (isOrbitPredictor && dejitterCOE) {
            phase = Mathd.Rad2Deg * OrbitUtils.GetPhaseFromOE(oe, ecc_threshold, incl_threshold);
        } else {
            double limit = OrbitUtils.small;
            if (evolveMode == EvolveMode.PKEPLER_J2)
                limit = PKepler.LIMIT;
            phase = Mathd.Rad2Deg * OrbitUtils.GetPhaseFromOE(oe, limit, limit);
        }
    }

    public virtual void ApplyScale(float scale) {
        nbody = GetComponent<NBody>();
        p = p_inspector / GravityEngine.Instance().physToWorldFactor * scale;

        if (centerNbody == null)
            centerNbody = OrbitUtils.GetCenterNbody(transform, null);
        if (!initFromRVT) {
            Init();
        }
        SetInitialPosition(nbody, centerNbody.gameObject);
    }

    public virtual void ApplyXZChange()
    {
        nbody = GetComponent<NBody>();
        if ((nbody == null) || (centerNbody == null)) {
            Debug.LogWarning("Skipped update on XZ mode flip on " + gameObject.name); 
            return;
        }
        SetInitialPosition(nbody, centerNbody.gameObject);
    }

    public bool FromOrbitPredictor()
    {
        return isOrbitPredictor;
    }

    //-----------------------------------------------------
    // Interface: IFixedOrbit
    //-----------------------------------------------------

    public virtual bool IsOnRails() {
        return evolveMode != EvolveMode.GRAVITY_ENGINE;
    }

    public virtual void PreEvolve(float physicalScale, float massScale) {
        if (!initFromRVT)
            InitFromCOE();
    }

    // Orignal comments from Vallado
    /* -----------------------------------------------------------------------------
	*
	*                           function kepler
	*
	*  this function solves keplers problem for orbit determination and returns a
	*    future geocentric equatorial (ijk) position and velocity vector.  the
	*    solution uses universal variables.
	*
	*  author        : david vallado                  719-573-2600   22 jun 2002
	*
	*  revisions
	*    vallado     - fix some mistakes                             13 apr 2004
	*
	*  inputs          description                    range / units
	*    ro          - ijk position vector - initial  km
	*    vo          - ijk velocity vector - initial  km / s
	*    dtsec       - length of time to propagate    s
	*
	*  outputs       :
	*    r           - ijk position vector            km
	*    v           - ijk velocity vector            km / s
	*    error       - error flag                     'ok', ...
	*
	*  locals        :
	*    f           - f expression
	*    g           - g expression
	*    fdot        - f dot expression
	*    gdot        - g dot expression
	*    xold        - old universal variable x
	*    xoldsqrd    - xold squared
	*    xnew        - new universal variable x
	*    xnewsqrd    - xnew squared
	*    znew        - new value of z
	*    c2new       - c2(psi) function
	*    c3new       - c3(psi) function
	*    dtsec       - change in time                 s
	*    timenew     - new time                       s
	*    rdotv       - result of ro dot vo
	*    a           - semi or axis                   km
	*    alpha       - reciprocol  1/a
	*    sme         - specific mech energy           km2 / s2
	*    period      - time period for satellite      s
	*    s           - variable for parabolic case
	*    w           - variable for parabolic case
	*    h           - angular momentum vector
	*    temp        - temporary real*8 value
	*    i           - index
	*
	*  coupling      :
	*    mag         - magnitude of a vector
	*    findc2c3    - find c2 and c3 functions
	*
	*  references    :
	*    vallado       2013, 93, alg 8, ex 2-4
	---------------------------------------------------------------------------- */
    /// <summary>
    /// Evolve the orbit to the time indicated. The algorithm used requires some internal
    /// iteration but in general converges very quiclky. (All solutions to Kepler's equation
    /// use some iteration, since the equation is not closed form).
    /// 
    /// The evolution sets pos and vel internally.
    /// 
    /// Universal Kepler evolution using KEPLER (algorithm 8) from Vallado, p93
    /// Code taken from book companion site and adapted to C#/Unity.
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="r_new">Position at the specified time (returned by ref)</param>
    /// 
    private const double small = 1E-6;
    private const double halfpi = Math.PI * 0.5;

    private double sgp4BlendTime;
    public virtual void Evolve(double physicsTime, GravityState gravityState, ref double[] r_new, ref double[] v_new, bool isQuery = false)
    {

        if (evolveMode == EvolveMode.SGP4_PROPAGATOR) {
            EvolveSGP4(physicsTime, gravityState, r_new, v_new, isQuery);
            Vector3d v_sgp4_vec = new Vector3d(ref v_new);
            if ((physicsTime - time0) < sgp4BlendTime) {
                /// The SGP4 algorithm will not accurately reproduce the position and velocity for the COE elements in the TLE
                /// at time 0. This is due to the fact that some of the COE secular pertubations modifiy the COE values even when
                /// time has not evolved. 
                /// 
                /// When GE wants to reset an SGP4 evolution e.g. after an impulse/maneuver it is important that the satellite does
                /// not suddenly jump to a new position or velocity. There is no way to preadjust the COE values so that with secular variations
                /// at t=0 the algorithm gives the required r. The solution is to start position/velocity calculations at t=0 using
                /// the TLE COE values and then to "Lerp" over to the secular values. The timescale for this lerping is a matter of choice. 
                /// It can be fixed as an absolute time or can be expressed as a factor of the orbital period of the satellite. 
                /// This "Lerp setting" is selected globally in the GE advanced foldout.
                double[] r_kepler = new double[] { 0, 0, 0 };
                double[] v_kepler = new double[] { 0, 0, 0 };

                double f = (physicsTime - time0) / sgp4BlendTime;
                EvolveKepler(physicsTime, gravityState, ref r_kepler, ref v_kepler);

                for (int i=0; i < 3; i++) {
                    r_new[i] = r_kepler[i] * (1.0 - f) + f * r_new[i];
                    v_new[i] = v_kepler[i] * (1.0 - f) + f * v_new[i];
                }
#pragma warning disable 162     // disable unreachable code warning
                if (GravityEngine.DEBUG) {
                    Vector3d v_new_vec = new Vector3d(ref v_new);
                    Vector3d v_kepler_vec = new Vector3d(ref v_kepler);
                    Debug.LogFormat("Blending {0} from {1} to {2} dV={3} f={4:0.000} v={5} t={6}",
                        nbody.gameObject.name,
                        v_sgp4_vec, v_kepler_vec, Vector3d.Distance(v_new_vec, v_kepler_vec), f, v_new_vec, 
                        physicsTime);
                }
#pragma warning restore 162        // apply an impulse to the indicated NBody            
            }
            return;
        } else if (evolveMode == EvolveMode.KEPLERS_EQN) {
            EvolveKepler(physicsTime, gravityState, ref r_new, ref v_new);
        } else if (evolveMode == EvolveMode.PKEPLER_J2) {
            EvolvePKepler(physicsTime, gravityState, ref r_new, ref v_new);
        }

    }   // kepler

    private void EvolveKepler(double physicsTime, GravityState gravityState, ref double[] r_new, ref double[] v_new)
    {
        // If XZ orbits then r0, v0 are in the XZ plane and this will work without any special code. 
        int ktr, numiter;
        double f, g, fdot, gdot, rval, xold, xoldsqrd, xnewsqrd, znew, pp, dtnew, rdotv, a, dtsec, alpha, sme, s, w, temp, magro, magvo, magr;
        double c2new = 0.0;
        double c3new = 0.0;
        double xnew = 0.0;

        if (timeLocked) {
            r_new[0] = lockedPos[0];
            r_new[1] = lockedPos[1];
            r_new[2] = lockedPos[2];
            v_new[0] = lockedVel[0];
            v_new[1] = lockedVel[1];
            v_new[2] = lockedVel[2];
            return;
        }

        // can have a weird precision issue when same time used in Init and first evolve.
        if (((physicsTime - time0) < 0) && (Mathd.Abs(physicsTime - time0) > 1E-5)) {
            Debug.LogWarning(string.Format("evolution time {0} is before time0 reference {1} for {2}",
                physicsTime, time0, gameObject.name));
            return;
        }
        // evolution time is relative to time0
        double dtseco = physicsTime - time0;

        // Very large times can cause precision issues, normalize if possible
        if ((eccentricity < 1) && (dtseco > 1E6)) {
            dtseco = dtseco % orbit_period;
        }

        dtsec = dtseco;

        Vector3d centerPosLast = gravityState.GetPhysicsPositionDouble(centerNbody);
        Vector3d centerVelLast = gravityState.GetVelocity3d(centerNbody);

        // -------------------------  implementation   -----------------
        // set constants and intermediate printouts
        numiter = 100;

        // --------------------  initialize values   -------------------
        ktr = 0;
        xold = 0.0;
        znew = 0.0;

        if (Mathd.Abs(dtseco) > small) {
            // <TODO>: (performance) put this where r0, v0 are changed and re-use it. 
            magro = r0.magnitude;
            magvo = v0.magnitude;
            rdotv = Vector3d.Dot(r0, v0);
            // </TODO>

            // -------------  find sme, alpha, and a  ------------------
            sme = ((magvo * magvo) * 0.5) - (mu / magro);
            alpha = -sme * 2.0 / mu;

            if (Mathd.Abs(sme) > small)
                a = -mu / (2.0 * sme);
            else
                a = double.NaN;

            bool radialInfall = Vector3d.Cross(r0.normalized, v0.normalized).magnitude < 1E-3;
            if (radialInfall) {
                if (sme <= 0) {
                    // Not debugged, but normal Kepler handles this case ok
                    // EvolveRecilinearBound(dtsec, sme, ref r_new, ref v_new);
                } else {
                    EvolveRecilinearUnbound(dtsec, sme, ref r_new, ref v_new);
                    // Add centerPos to value we ref back
                    r_new[0] += centerPosLast.x;
                    r_new[1] += centerPosLast.y;
                    r_new[2] += centerPosLast.z;
                    // update velocity
                    v_new[0] += centerVelLast.x;
                    v_new[1] += centerVelLast.y;
                    v_new[2] += centerVelLast.z; return;
                }
            }

            // This check breaks the case where SI units are used for the Solar System 
            // (not the recommended choice of units, but more likely than an exactly parabolic Kepler mode)
            // Likewise for check of alpha below. 
            //if (Mathd.Abs(alpha) < small)   // parabola
            //    alpha = 0.0;

            // ------------   setup initial guess for x  ---------------
            // -----------------  circle and ellipse -------------------
            // if (alpha >= small) {
            if (alpha >= 0) {
                if (Mathd.Abs(alpha - 1.0) > small)
                    xold = Mathd.Sqrt(mu) * dtsec * alpha;
                else
                    // - first guess can't be too close. ie a circle, r=a
                    xold = Mathd.Sqrt(mu) * dtsec * alpha * 0.97;
            } else {
                // --------------------  parabola  ---------------------
                if (Mathd.Abs(alpha) < small) {
                    Vector3d h = Vector3d.Cross(r0, v0);
                    pp = h.sqrMagnitude / mu;
                    s = 0.5 * (halfpi - Mathd.Atan(3.0 * Mathd.Sqrt(mu / (pp * pp * pp)) * dtsec));
                    w = Mathd.Atan(Mathd.Pow(Mathd.Tan(s), (1.0 / 3.0)));
                    xold = Mathd.Sqrt(p) * (2.0 * GEMath.Cot(2.0 * w));
                    alpha = 0.0;
                } else {
                    // ------------------  hyperbola  ------------------
                    temp = -2.0 * mu * dtsec /
                        (a * (rdotv + Mathd.Sign(dtsec) * Mathd.Sqrt(-mu * a) * (1.0 - magro * alpha)));
                    xold = Mathd.Sign(dtsec) * Mathd.Sqrt(-a) * Mathd.Log(temp);
                }
            } // if alpha

            ktr = 1;
            dtnew = -10.0;
            // conv for dtsec to x units
            double tmp = 1.0 / Mathd.Sqrt(mu);

            while ((Mathd.Abs(dtnew * tmp - dtsec) >= small) && (ktr < numiter)) {
                xoldsqrd = xold * xold;
                znew = xoldsqrd * alpha;

                // ------------- find c2 and c3 functions --------------
                OrbitUtils.FindC2C3(znew, out c2new, out c3new);

                // ------- use a newton iteration for new values -------
                rval = xoldsqrd * c2new + rdotv * tmp * xold * (1.0 - znew * c3new) +
                    magro * (1.0 - znew * c2new);
                dtnew = xoldsqrd * xold * c3new + rdotv * tmp * xoldsqrd * c2new +
                    magro * xold * (1.0 - znew * c3new);

                // ------------- calculate new value for x -------------
                xnew = xold + (dtsec * Mathd.Sqrt(mu) - dtnew) / rval;

                // ----- check if the univ param goes negative. if so, use bissection
                if (xnew < 0.0)
                    xnew = xold * 0.5;

                ktr = ktr + 1;
                xold = xnew;
            }  // while

            if (ktr >= numiter) {
                Debug.LogWarning(string.Format("{0} not converged in {1} iterations. dtnew={2} tmp={3} dto={4} expr={5}",
                    gameObject.name, numiter, dtnew, tmp, dtsec, Mathd.Abs(dtnew * tmp - dtsec)));
                Debug.LogFormat("ecc={0} p={1}", eccentricity, p);
                // Mitigation: use last known position
                ge.GetPositionDouble(nbody, ref r_new);
            } else {
                // --- find position and velocity vectors at new time --
                xnewsqrd = xnew * xnew;
                f = 1.0 - (xnewsqrd * c2new / magro);
                g = dtsec - xnewsqrd * xnew * c3new / Mathd.Sqrt(mu);
                r_new[0] = f * r0.x + g * v0.x;
                r_new[1] = f * r0.y + g * v0.y;
                r_new[2] = f * r0.z + g * v0.z;
                magr = Mathd.Sqrt(r_new[0] * r_new[0] + r_new[1] * r_new[1] + r_new[2] * r_new[2]);
                gdot = 1.0 - (xnewsqrd * c2new / magr);
                fdot = (Mathd.Sqrt(mu) * xnew / (magro * magr)) * (znew * c3new - 1.0);
                temp = f * gdot - fdot * g;
                //if (Mathd.Abs(temp - 1.0) > 0.00001)
                //    Debug.LogWarning(string.Format("consistency check failed {0}", (temp - 1.0)));
                v_new[0] = fdot * r0.x + gdot * v0.x;
                v_new[1] = fdot * r0.y + gdot * v0.y;
                v_new[2] = fdot * r0.z + gdot * v0.z;
                // Add centerPos to value we ref back
                r_new[0] += centerPosLast.x;
                r_new[1] += centerPosLast.y;
                r_new[2] += centerPosLast.z;
                // update velocity
                v_new[0] += centerVelLast.x;
                v_new[1] += centerVelLast.y;
                v_new[2] += centerVelLast.z;
            }
        } // if fabs
        else {
            // ----------- set vectors to incoming since 0 time --------
            r_new[0] = r0.x;
            r_new[1] = r0.y;
            r_new[2] = r0.z;
            // Add centerPos to value we ref back
            r_new[0] += centerPosLast.x;
            r_new[1] += centerPosLast.y;
            r_new[2] += centerPosLast.z;
            v_new[0] = v0.x + centerVelLast.x;
            v_new[1] = v0.y + centerVelLast.y;
            v_new[2] = v0.z + centerVelLast.z;
        }
    }

    /// <summary>
    /// Evolve using a Kepler model with J2 propagation. 
    /// 
    /// This model optionally has the ability to evolve changes based on ndot and nddot. MORE....
    /// 
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="gravityState"></param>
    /// <param name="r_new"></param>
    /// <param name="v_new"></param>
    private void EvolvePKepler(double physicsTime, GravityState gravityState, ref double[] r_new, ref double[] v_new)
    {
        // The internals (RVtoCOE and COEtoRV are XZ-aware, so do not need to shuffle them here)

        if (timeLocked) {
            r_new[0] = lockedPos[0];
            r_new[1] = lockedPos[1];
            r_new[2] = lockedPos[2];
            v_new[0] = lockedVel[0];
            v_new[1] = lockedVel[1];
            v_new[2] = lockedVel[2];
            return;
        }

        // can have a weird precision issue when same time used in Init and first evolve.
        // evolution time is relative to time0
        double tsec = physicsTime - time0;
        if ((tsec < 0) && (Mathd.Abs(physicsTime - time0) > 1E-5)) {
            Debug.LogWarning(string.Format("evolution time {0} is before time0 reference {1} for {2}",
                physicsTime, time0, gameObject.name));
            return;
        }

        Vector3d centerPosLast = gravityState.GetPhysicsPositionDouble(centerNbody);
        Vector3d centerVelLast = gravityState.GetVelocity3d(centerNbody);

        // Prop is ok at t=0
        (Vector3d r_t, Vector3d v_t) = PKepler.PKeplerProp(coe0, tsec, mu, scaleToKm: invLengthScale, pkepler_ndot, pkepler_nddot);
        r_new[0] = r_t.x + centerPosLast.x;
        r_new[1] = r_t.y + centerPosLast.y;
        r_new[2] = r_t.z + centerPosLast.z;
        v_new[0] = v_t.x + centerVelLast.x;
        v_new[1] = v_t.y + centerVelLast.y;
        v_new[2] = v_t.z + centerVelLast.z;

    }


    private void EvolveRecilinearUnbound(double t, double sme, ref double[] r_new, ref double[] v_new)
    {
        double eta = sme;
        double a = -0.5 * mu / eta;
        double V =  Mathd.Sqrt(mu / (a * a));
        // HACK to slow down things. Might be an error in Roy (vs "About the rectilinear Kepler motion" paper)
        V *= 0.5;
        double a_pos = Mathd.Abs(a);
        // TODO: (opt) Can be done when r0 is set
        // NB: Added a V to scale tau so that recover correct M at t=0. (time0 was already subtracted before t was passed in)
        // Find M0 value at t=0 based on r0, v0
        // r = a[cosh(F)-1]
        double F0 = GEMath.Acosh(r0.magnitude / a_pos + 1);
        double M0 = GEMath.Sinh(F0) - F0;
        // fix sign based on velocity align
        M0 *= Mathd.Sign(Vector3d.Dot(r0, v0));
        double M = V * t + M0;
        // Newton's root finder
        int i = 0;
        double u = M;
        double u_next = 0;
        while (i++ < 1000)
        {
            u_next = u + (M - (GEMath.Sinh(u) - u)) / (GEMath.Cosh(u)-1);
            if (Mathd.Abs(u_next - u) < 1E-6)
                break;
            u = u_next;
        }
        if (i >= 100)
        {
            Debug.LogWarning("Did not converge");
        }
        // E is from center of hyperbola, not focus
        double r = a_pos * (GEMath.Cosh(u)-1);
        Vector3d r_vec = r0.normalized * r;
        r_new[0] = r_vec.x;
        r_new[1] = r_vec.y;
        r_new[2] = r_vec.z;
        // velocity (Roy eqn (4.106): r rdot = a^(1/2) mu^(1/2) sin(E)
        double rdot = Mathd.Sqrt(a * mu) * GEMath.Sinh(u) / r;
        Vector3d v_vec = r0.normalized * rdot;
        v_new[0] = v_vec.x;
        v_new[1] = v_vec.y;
        v_new[2] = v_vec.z;
    }

    /// <summary>
    /// Evolve using the Simplified General Pertubation propagator model V4. 
    /// 
    /// See: https://en.wikipedia.org/wiki/Simplified_perturbations_models
    ///  
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="gravityState"></param>
    /// <param name="r_new"></param>
    /// <param name="v_new"></param>
    public virtual void EvolveSGP4(double physicsTime, GravityState gravityState,  double[] r_new, double[] v_new, bool isQuery = false)
    {
        // TODO: want to record start time and get some factor to convert game time to JD time for SGP4
        double time = ge.GetStartTimeAsJD() + GravityScaler.GetWorldTimeSeconds(physicsTime) / SolarUtils.SEC_PER_DAY;
        (int error, Vector3d r, Vector3d v) = sgpForTLE.SGP4toRVatTime(time);
        if (error != 0) {
#if UNITY_EDITOR
            Debug.LogError(string.Format("Error {0} propagating for {1} at t={2}", sgpForTLE.GetSatData().ErrorString(error), gameObject.name, physicsTime));
#endif
            // if there is a error callback, execute it.
            if ((errorCallback != null) && !isQuery) {
                errorCallback(this, error);
            }
        }
        if (ge.xzOrbits)
        {
            r = XZPlane.PhysicsToUnity(r);
            v = XZPlane.PhysicsToUnity(v);
        }
        //Debug.Log("REMOVE ME " + SGP4.SGP4unit.LogInternalCOE( sgpForTLE.GetSatData()) );
        // SGP4toRVatTime did the unit conversion already
        r_new[0] = r.x;
        r_new[1] = r.y;
        r_new[2] = r.z;
        v_new[0] = v.x;
        v_new[1] = v.y;
        v_new[2] = v.z;
    }

    public void SetSGP4BlendTime(double time)
    {
        sgp4BlendTime = time;
    }


    /// <summary>
    /// Use in On-Rails mode to lock an object at a specific time. Any evolve calls will not re-compute the 
    /// position/velocity but instead leave them unchanged. 
    /// 
    /// This will be applied to the orbit for the world state. If trajectory prediction is being
    /// used this will result in the locked times being returned for the trajectory - this will
    /// be wrong.
    ///
    /// Do not use when trajectory prediction is enabled.
    /// <param name="lockTime"></param>
    public void LockAtTime(double lockTime) {
        if (evolveMode != EvolveMode.KEPLERS_EQN) {
            Debug.LogError("Can only lock time in Kepler mode " + gameObject.name);
        }
        timeLocked = false;
        if (lockTime < time0) {
            Debug.LogWarning(string.Format("lock time {0} earlier than time0 {1}", lockTime, time0));
            time0 = lockTime;
        }
        Evolve(lockTime, ge.GetWorldState(), ref lockedPos, ref lockedVel);
        timeLocked = true;
        ge.GetWorldState().UpdateInternalDouble(nbody, new Vector3d(ref lockedPos), new Vector3d(ref lockedVel));
    }

    public void UnlockTime() {
        timeLocked = false;
    }

    public (Vector3d, Vector3d) GetLockedPosVel()
    {
        return (new Vector3d(ref lockedPos), new Vector3d(ref lockedVel));
    }

    public double GetPeriod() {
        if (centerNbody == null)
            return 0;
        // Use Find to allow Editor to use method. 
        if (ge == null) {
            ge = (GravityEngine)FindObjectOfType(typeof(GravityEngine));
            if (ge == null) {
                Debug.LogError("Need GravityEngine in the scene");
                return 0;
            }
            Init();
        }
        if (eccentricity < 1.0) {
            double a = GetMajorAxis() / ge.GetPhysicalScale();
            orbit_period = 2f * Mathd.PI * Mathd.Sqrt(a * a * a / mu); // G=1
            return orbit_period;
        } else {
            return double.NaN;
        }

    }

    public double GetAngularVelocity() {
        double a = GetMajorAxis() / ge.GetPhysicalScale();
        return Mathd.Sqrt(mu/(a * a * a));
    }

    public double GetStartTime() {
        return time0;
    }

    /// <summary>
    /// Get the intial conditions for the orbit. R0 and V0 are always relative to the center
    /// body. If the center is not at (0,0,0) and/or moving, need to adjust values outside. 
    /// 
    /// These value are always available (even if orbitU was not inited with InitRVT). 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time0"></param>
    public void GetRVT(ref Vector3d r0, ref Vector3d v0, ref double time0) {
        r0 = this.r0;
        v0 = this.v0;
        time0 = this.time0;
    }

    /// <summary>
    /// Utility function (used by e.g. OrbitPredictor) to see if the orbit initial conditions have changed. 
    /// Used to determine when an OP needs to recompute the orbit in KEPLER mode. 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time0"></param>
    /// <param name="tol"></param>
    /// <returns></returns>
    public bool RVT_Equal(ref Vector3d r0, ref Vector3d v0, ref double time0, double tol)
    {
        if (evolveMode == EvolveMode.GRAVITY_ENGINE)
            return false;
        if (Mathd.Abs((this.r0 - r0).magnitude) > tol)
            return false;
        if (Mathd.Abs((this.v0 - v0).magnitude) > tol)
            return false;
        if (Mathd.Abs(this.time0 - time0) > tol)
            return false;
        return true;
    }

    public void GetRVTAbsolute(ref Vector3d r0, ref Vector3d v0, ref double time0)
    {
        GetRVT(ref r0, ref v0, ref time0);
        r0 = this.r0 + ge.GetPositionDoubleV3(centerNbody);
        v0 = this.v0 + ge.GetVelocityDoubleV3(centerNbody);
    }


    public void GEUpdate(GravityEngine ge) {
        Debug.LogError("Values are not cached here anymore");
    }

    /// <summary>
    /// Move in response to a GE.MoveAll() call. (Not called from outside GE).
    ///
    /// Since the next time Evolve() is called it will get a new center, no need
    /// to change the position here. 
    /// </summary>
    /// <param name="position"></param>
    public void Move(Vector3 position) {
        // do nothing
    }

    public void SetNBody(NBody nbody) {
        this.nbody = nbody;
    }

    // Wrapper for IOrbitPositions used only by the OrbitRenderer
    public Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {
        return OrbitPositions(numPoints, centerPos, doSceneMapping, 0);
    }

    public Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping, float hyperRadius) {
        Vector3[] positions;
        if (double.IsNaN(eccentricity)) {
            // an orbit with no angular momtm (free fall) will have NaN for all orbital elements
            // make path a line to the center
            positions = new Vector3[numPoints];
            Vector3 bodypos = ge.GetPhysicsPosition(nbody);
            if (doSceneMapping) {
                bodypos = ge.MapToScene(bodypos);
            }
            positions[0] = bodypos;
            for (int i = 1; i < numPoints; i++)
                positions[i] = centerPos;
        } else if (eccentricity < 1.0) {
            positions = EllipsePositions(numPoints, centerPos, doSceneMapping); 
        } else {
            positions = HyperSegmentSymmetric(numPoints, centerPos, hyperRadius, doSceneMapping);
        }
        return positions;
    }

    /// <summary>
    /// Get the position for the specified phase in physics co-ordinates. 
    /// 
    /// (If map to scene is being used it up to the caller to do the conversion using ge.MapToScene() )
    /// 
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <returns></returns>
    public Vector3 PositionForPhase(float phaseDeg) {
        return GetPositionForThetaRadians(phaseDeg * Mathf.Deg2Rad, ge.GetPhysicsPosition(centerNbody));
    }

    public Vector3 VelocityForPhaseRelative(float phaseDeg) {
        /// Uses Vallado, Algorithm 10 for (x,y) plane and then rotates into place
        double phaseRad = phaseDeg * Mathd.Deg2Rad;
        double vx = -Mathd.Sqrt(mu / p) * Mathd.Sin(phaseRad);
        double vy = Mathd.Sqrt(mu / p) * (eccentricity + Mathd.Cos(phaseRad));
        Vector3 v = ApplyRotations(new Vector3d(vx, vy, 0)).ToVector3();
        if (ge.xzOrbits) {
            v = XZPlane.PhysicsToUnity(v);
        }
        return v;
    }

    /// <summary>
    /// Given a position on the orbit (or a position that is used to establish a phase wrt the center)
    /// determine the velocity vector at that point on the orbit. 
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 VelocityForPosition(Vector3 position) {
        // determine the phase and use that to get the velocity
        Vector3 toPos = position - ge.GetPhysicsPosition(centerNbody);
        Vector3 toPeri = PositionForPhase(0);
        float angle = Vector3.Angle(toPeri, toPos);
        // sign check 
        if ( Vector3.Dot(Vector3.Cross(toPeri, toPos), GetAxis().ToVector3()) < 0) {
            angle = 360f - angle;
        }
        return VelocityForPhaseRelative(angle);
    }

    public bool CanApplyImpulse() {
        return true;
    }

    public NBody GetCenterNBody() {
        return centerNbody;
    }

    /// <summary>
    /// Set a new start position for orbit evolution with respect to the current center object. 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        Vector3 cPos = ge.GetPhysicsPosition(centerNbody);
        Vector3 cVel = ge.GetVelocity(centerNbody);
        r0 = new Vector3d(pos - cPos);
        v0 = new Vector3d(vel - cVel);
        Debug.Log("pos=" + pos + " r0=" + r0);
        time0 = ge.GetPhysicalTimeDouble();
        GEUpdate(ge);
    }

    /*********************************************************************************************
    *   Methods that are unique to OrbitUniversal
    *********************************************************************************************/

    /// <summary>
    /// Set a new center object. Used when doing Kepler ("on-rails") evolution and the object enters
    /// the sphere of influence of a new body. The inital conditions (r0,v0, t0) are updated to be with respect
    /// to the new center object allowing Kepler evolution wrt the new center
    /// </summary>
    /// <param name="newCenter"></param>
    public void SetNewCenter(NBody newCenter) {

        centerNbody = newCenter;
        if ((nbody!= null) && (nbody.engineRef != null)) {
            if (!isOrbitPredictor) {
                GravityEngine.Instance().UpdateKeplerDepth(nbody, this);
            }
            mu = GravityEngine.Instance().GetMass(centerNbody);
            Vector3d r = ge.GetPositionDoubleV3(nbody);
            Vector3d v = ge.GetVelocityDoubleV3(nbody);
            Vector3d r_center = ge.GetPositionDoubleV3(newCenter);
            Vector3d v_center = ge.GetVelocityDoubleV3(newCenter);

            r0 = r - r_center;
            v0 = v - v_center;
            time0 = ge.GetPhysicalTimeDouble();
            InitFromRVT(r0, v0, time0, centerNbody, relativePos: true);
        }
    }

    /// <summary>
    /// Change the evolve mode between KEPLER and GRAVITY_ENGINE. i.e. go from off-rails to on-rails.
    /// </summary>
    /// <param name="newMode"></param>
    public void ChangeEvolveMode(EvolveMode newMode) {
        Debug.LogError("Not Implemented");
    }

    /// <summary>
    /// Apply an impulse to change the on-rails evolution (Kepler or SGP4). OrbitUniversal supports seamless changes from ellipse to 
    /// parabola to hyperbola. This updates  the initial conditions and start time (r0, v0, time0)
    /// 
    /// This method is usually called from GravityEngine ApplyImpulse(). Not commonly called from game code. 
    /// 
    /// </summary>
    /// <param name="impulse">Adjusted Impulse</param>
    public Vector3 ApplyImpulse(Vector3 impulse) {

        GravityState gs = ge.GetWorldState();
        // Cannot get r, v from GE, since this OU may have been cloned and r,v might represent pre-clone state, so Evolve
        double t_now = gs.GetPhysicsTime();
        double[] r_nowa = new double[] { 0, 0, 0 };
        double[] v_nowa = new double[] { 0, 0, 0 };
        Evolve(t_now, gs, ref r_nowa, ref v_nowa, isQuery: true);
        Vector3d r_now = new Vector3d(ref r_nowa);
        Vector3d v_now = new Vector3d(ref v_nowa);

        Vector3d centerPosLast = gs.GetPhysicsPositionDouble(centerNbody);
        Vector3d centerVelLast = gs.GetVelocity3d(centerNbody);
        v0 = v_now + new Vector3d(impulse) - centerVelLast;
        r0 = r_now - centerPosLast;
        time0 = t_now;

        Debug.LogFormat("REMOVE ME r0={0} t0={1}", r0, time0);
        if (evolveMode == EvolveMode.KEPLERS_EQN) {
            // update the classical orbit elements (not essential but might be useful later to avoid an OrbitData?)
            RVtoCOEWrapper();
            initFromRVT = true;
        } else if (evolveMode == EvolveMode.PKEPLER_J2) {
            RVtoCOEWrapper();
            initFromRVT = true;
        } else if (evolveMode == EvolveMode.SGP4_PROPAGATOR) {
            // assume Earth is at the origin, so no offset
            try {
                sgp4BlendTime = sgpForTLE.ReInitFromRV(r0, v0, time0, mu);
            }
            catch (Exception e) {
                // icky but the OrbitUtils will throw an exception for a hyperbola
                if (errorCallback != null) {
                    errorCallback(this, SGP4.SGP4SatData.ECCENTRICITY_ERR_SGP4);
                } else {
                    Debug.LogError("Impulse resulted in unsupported hyperbola for " + gameObject + " " + e.ToString());
                }
                return Vector3.zero;
            }
        } else {
            Debug.LogErrorFormat("Unsuppored on-rails mode GO={0} MODE={1}", gameObject.name, evolveMode);
            return Vector3.zero;
        }
        // update cached velocities (in case we are paused)
        Vector3d v_new = v0 + centerVelLast;
        if (!isOrbitPredictor) {
            ge.GetWorldState().UpdateInternalDouble(nbody, r0 + centerPosLast, v_new);
        }
        // SGP4 re-init will not return an exact match for R, V so init a Kepler and do a blend
        return v_new.ToVector3();
    }

    /// <summary>
    /// Determine the time of flight in physics time units (GE internal time) that it takes for the body
    /// in orbit to go from relative position r0 to position r1. 
    /// 
    /// 
    /// </summary>
    /// <param name="r0">from relative position (assumes center is (0,0,0)</param>
    /// <param name="r1">to relative position</param>
    /// <returns>time to travel from r0 to r1 in GE time</returns>
    public double TimeOfFlight(Vector3d r0, Vector3d r1) {
        Vector3d r0_xy = r0;
        Vector3d r1_xy = r1;
        if (ge.xzOrbits) {
            r0_xy = XZPlane.UnityToPhysics(r0);
            r1_xy = XZPlane.UnityToPhysics(r1);
        }
        // h_unit is in xy space
        double tof = OrbitUtils.TimeOfFlight(r0_xy, r1_xy, p, mu, h_unit_xy);
        if ((eccentricity < 1.0) && (tof < 0)) {
            if (orbit_period == 0)
                GetPeriod();
            tof += orbit_period;
        } 
        // for hyperbola want to do Abs. Just do it always
        return Mathd.Abs(tof);
    }

    /// <summary>
    /// Find the time of flight from position r0 to r1 when the orbit is elliptical.
    /// 
    /// Will issue a warning and return 0 when the orbit in not elliptical.
    /// 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
	[Obsolete("Use TimeOfFlight")]
    public double TimeOfFlightEllipse(Vector3d r0, Vector3d r1)
    {
        if (eccentricity >= 1.0) {
            Debug.LogWarning("Only ellipse is supported");
            return 0; 
        }        
        return TimeOfFlight(r0, r1); 
    }

    /// <summary>
    /// Sets the initial physics position based on the orbit parameters. Used in the init phase to set the NBody in the
    /// correct position in the scene before handing control GE. 
    /// </summary>
    private void SetInitialPosition(NBody nbody, GameObject centerObject) {

        Vector3 new_p = Vector3.zero;
        if (initFromRVT)
            new_p = r0.ToVector3();
        else {
            float phaseRad = (float)(phase * Mathd.Deg2Rad);
            // position object using true anomoly (angle from  focus)
            float r = (float)(p / (1f + eccentricity * Mathf.Cos(phaseRad)));

            Vector3 pos = new Vector3(r * Mathf.Cos(phaseRad), r * Mathf.Sin(phaseRad), 0);
            // move from XY plane to the orbital plane
            new_p = conic_orientation * pos;
            if (GravityEngine.Instance().xzOrbits) {
                new_p = XZPlane.PhysicsToUnity(new_p);
            }
        }
        // orbit position is WRT center. Could be adding dynamically to an object in motion, so need current position. 
        Vector3 centerPos = Vector3.zero;
        // used by widgets - so need to get explcitly
        centerNbody = OrbitUtils.GetCenterNbody(transform, centerObject);
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        } else {
            // setup - not yet added to GE
            centerPos = centerNbody.initialPhysPosition;
        }
        if (nbody != null) {
            nbody.initialPhysPosition = new_p + centerPos;
            if (!Application.isPlaying) {
                nbody.EditorUpdate(GravityEngine.Instance());
            }
        }
    }

    public bool IsCircular() {
        return (eccentricity < OrbitUtils.ecc_circular);
    }


#if UNITY_EDITOR
    /*********************************************************************************************
     *               GIZMO CODE
     *********************************************************************************************/

 
    /// <summary>
    /// Displays the path of the orbit when the object is selected in the editor. 
    /// 
    /// No need for full double precision, so use as convenient.
    /// 
    /// Simpler to use specific code for ellipse vs hyperbola and nudge a true parabola into a hyperbola.
    /// </summary>
    void OnDrawGizmosSelected() {

        // The gizmo may be misleading, since object may have been affected by oither masses. 
        // Orbit predictors are better suited to showing the orbit while playing.
        if (Application.isPlaying) {
            return;
        }

        // If center body has not been configured, cannot draw anything
        if (centerNbody == null)
            return;

        // only display if this object or parent is selected
        bool selected = Selection.Contains(transform.gameObject);
        if (transform.parent != null)
            selected |= Selection.Contains(transform.parent.gameObject);
        if (!selected) {
            return;
        }

        // When part of OrbitPredictor, there will be no NBody to grab, bail out.
        NBody testForNbody = this.GetComponent<NBody>();
        if (testForNbody == null) {
            return;
        } 

        if (nbody == null)
            nbody = testForNbody;

        // check the OEs are legit
        if (double.IsNaN(omega_lc) ||
            double.IsNaN(omega_uc) ||
            double.IsNaN(eccentricity) ||
            double.IsNaN(inclination) ||
            double.IsNaN(p) ||
            double.IsNaN(phase)
            ) {
            return;
        }

        // Init() needs the object active in GE (to get mass), so just update params here
        omega_u_rad = omega_uc * Mathd.Deg2Rad;
        omega_l_rad = omega_lc * Mathd.Deg2Rad;
        incl_rad = inclination * Mathd.Deg2Rad;

        // Center object may need to determine it's position in an orbit
        // and update it's intialPhyPosition
        GravityEngine ge = GravityEngine.Instance();
        centerNbody.InitPosition(ge);
        centerNbody.EditorUpdate(ge);
        Vector3 centerPos = centerNbody.transform.position;
        CalculateRotation();
        SetInitialPosition(nbody, centerNbody.gameObject);
        if (eccentricity < 1.0) {
            DrawEllipseGizmo(centerPos);
        } else {
            DrawHyperGizmo(centerPos);
        }
    }

#endif

    protected void CalculateRotation() {
        // Following Murray and Dermot Ch 2.8 Fig 2.14
        // Quaternions go L to R (matrices are R to L)
        conic_orientation = Quaternion.AngleAxis((float)omega_uc, GEConst.zunit) *
                              Quaternion.AngleAxis((float)inclination, GEConst.xunit) *
                              Quaternion.AngleAxis((float)omega_lc, GEConst.zunit);
    }

    public Quaternion GetConicOrientation() {
        return conic_orientation;
    }

    const int NUM_STEPS = 100;
    const int STEPS_PER_RAY = 10;


    /// <summary>
    /// Calculate an array of points that describe the specified orbit. 
    /// 
    /// Treat the number of points as a suggestion and ensure that no points are more that 
    /// 2*p/numPoints apart.
    ///
    /// </summary>
    /// <returns>The positions.</returns>
    /// <param name="numPoints">Number points.</param>
    /// <summary>
    /// Calculate an array of points that describe the specified orbit
    /// </summary>
    /// <returns>The positions.</returns>
    /// <param name="numPoints">Number points.</param>
    private Vector3[] EllipsePositions(int numPoints, Vector3 centerPos, bool doSceneMapping)
    {

        Vector3[] points = new Vector3[numPoints];

        float dtheta = 2f * Mathf.PI / numPoints;
        float theta = 0;
        double ecc = eccentricity;
        if (Mathd.Abs(ecc - 1f) < 1E-6)
            ecc = (1 + 1E-5);

        double r = 0;
        Vector3d r_pqw = Vector3d.zero;

        // add a fudge factor to ensure we go all the way around the circle
        for (int i = 0; i < numPoints; i++) {
            r = (p / (1.0 + ecc * Mathd.Cos(theta)));
            r_pqw = new Vector3d(r * Mathd.Cos(theta), r * Mathd.Sin(theta), 0);
            points[i] = ApplyRotations(r_pqw).ToVector3();
            theta += dtheta;
        }
        // close the path (credit for fix to R. Vincent)
        points[numPoints - 1] = points[0];
        // post process
        if (GravityEngine.instance.xzOrbits) {
            for (int i = 0; i < numPoints; i++) {
                points[i] = XZPlane.PhysicsToUnity(points[i]);
            }
        }
        // make position relative
        for (int i = 0; i < numPoints; i++) {
            points[i] += centerPos;
        }
        if (doSceneMapping) {
            for (int i = 0; i < numPoints; i++) {
                points[i] = ge.MapToScene(points[i]);
            }
        }
        return points;
    }

    private Vector3 GetPointForTheta(float theta, Vector3 centerPos, bool doSceneMapping) {
        Vector3 point = GetPositionForThetaRadians(theta, centerPos); //centerPos added here
        if (NUtils.VectorNaN(point)) {
            string opDetails = "";
            OrbitPredictor op = GetComponent<OrbitPredictor>();
            if (op != null) {
                string name = (op.body == null) ? "name" : op.body.name;
                opDetails = string.Format(" OrbitPredictor: {0} around {1}", name, op.centerBody.name);
            }
            Debug.LogError("Vector NaN + " + point + " in " + gameObject.name + opDetails);
            point = Vector3.zero;
        } else if (doSceneMapping && ge.mapToScene) {
            point = ge.MapToScene(point);
        }
        return point;
    }

    private Vector3[] HyperOrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {

        // CalculateRotation();

        Vector3[] points = new Vector3[numPoints];
        float theta = -1f * branchDisplayFactor * Mathf.PI;
        float dTheta = 2f * Mathf.Abs(theta) / (float)numPoints;
        GravityEngine ge = GravityEngine.Instance();
        for (int i = 0; i < numPoints; i++) {
            points[i] = GetPositionForThetaRadians(theta, centerPos);
            if (NUtils.VectorNaN(points[i])) {
                points[i] = Vector3.zero;
            } else if (doSceneMapping && ge.mapToScene) {
                points[i] = ge.MapToScene(points[i]);
            }
            theta += dTheta;
        }
        return points;
    }

    /// <summary>
    /// Calculate points suitable for a line renderer that show a segment of the hyperbola for a specified
    /// radius with respect to the center. 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="radius"></param>
    /// <param name="doSceneMapping"></param>
    /// <returns></returns>
    public Vector3[] HyperSegmentSymmetric(int numPoints, Vector3 centerPos, float radiusOrZero, bool doSceneMapping) {
        float radius = radiusOrZero;
        Vector3d pos = ge.GetPositionDoubleV3(nbody);
        if (radiusOrZero < 1E-6) {
            radius = (pos.ToVector3() - centerPos).magnitude;
        }
        CalculateRotation();

        // solve hyperbola equation for theta:
        double cos_theta = Mathd.Clamp((p / radius - 1) / eccentricity, -1.0, 1.0);
        float theta_for_r = (float) Mathd.Acos(cos_theta);

        Vector3[] points = new Vector3[numPoints];
        float dTheta = 2f * Mathf.Abs(theta_for_r) / (float)numPoints;
        float theta = -theta_for_r;
        for (int i = 0; i < numPoints; i++) {
            points[i] = points[i] = GetPointForTheta(theta, centerPos, doSceneMapping);
            theta += dTheta;
        }
        // explicitly include the end point to avoid slight miss
        points[numPoints-1] = GetPointForTheta(theta_for_r, centerPos, doSceneMapping);
        return points;

    }

    // same code as EllipseBase
    /// <summary>
    /// Generate the points for an ellipse segment given the start and end positions. If shortPath then 
    /// the short path between the points will be shown, otherwise the long way around. 
    /// 
    /// The points are used to determine an angle from the main axis of the ellipse and although they 
    /// should be on the ellipse for best results, the code will do it's best if they are not. 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="pos"></param>
    /// <param name="destPoint"></param>
    /// <param name="shortPath"></param>
    /// <returns></returns>
    public Vector3[] EllipseSegment(int numPoints, Vector3 centerPos, Vector3 pos, Vector3 destPoint, bool shortPath) {

        Vector3[] points = new Vector3[numPoints];
        CalculateRotation();
        float dtheta = 2f * Mathf.PI / numPoints;
        float theta;

        // find the vector to theta=0 on the ellipse, with no offset
        Vector3 ellipseAxis = GetPositionForThetaRadians(0f, Vector3.zero);
        Vector3 inczeroNormal = Vector3.forward;
        if (ge.xzOrbits)
            inczeroNormal = Vector3.up;
        Vector3 normal = conic_orientation * inczeroNormal;
        float theta1 = NUtils.AngleFullCircleRadians(ellipseAxis, pos - centerPos, normal);
        float theta2 = NUtils.AngleFullCircleRadians(ellipseAxis, destPoint - centerPos, normal);

        //if (inclination > 90) {
        //    float temp = theta1;
        //    theta1 = theta2;
        //    theta2 = temp;
        //}
        //if (theta1 > theta2) {
        //    float temp = theta1;
        //    theta1 = theta2;
        //    theta2 = temp;
        //}
        //if (!shortPath) {
        //    if ((theta2 - theta1) < Mathf.PI) {
        //        float temp = theta1;
        //        theta1 = theta2;
        //        // ok to go beyond 2 Pi, since will increment to theta2
        //        theta2 = temp + 2f * Mathf.PI;
        //    }
        //} else {
        //    // shortpath=true
        //    if ((theta2 - theta1) > Mathf.PI) {
        //        float temp = theta1;
        //        theta1 = theta2;
        //        // ok to go beyond 2 Pi, since will increment to theta2
        //        theta2 = temp + 2f * Mathf.PI;
        //    }
        //}
        Debug.LogFormat("theta1={0} theta2={1} start={2} end={3} axis={4}", theta1, theta2, pos, destPoint, ellipseAxis);
        if (theta1 > theta2) {
            theta2 += 2f * Mathf.PI;
        }
        int i = 0;
        // TODO: Add to API
        bool doSceneMapping = true; 
        for (theta = theta1; theta < theta2; theta += dtheta) {
            points[i] = points[i] = GetPointForTheta(theta, centerPos, doSceneMapping);
            i++;
            if (i > numPoints - 1)
                break;
        }
        // fill to end with last point
        int last = i - 1;
        points[last] = GetPositionDForThetaRadians(theta2, relative: false).ToVector3();
        if (last < 0)
            last = 0;
        while (i < numPoints) {
            points[i++] = points[last];
        }
        return points;
    }

    /// <summary>
    /// Generate the points for an ellipse segment given the start and end positions. 
    /// 
    /// The points are used to determine an angle from the main axis of the ellipse and although they 
    /// should be on the ellipse for best results, the code will do it's best if they are not. 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="pos"></param>
    /// <param name="destPoint"></param>
    /// <param name="retrograde"></param>
    /// <returns></returns>
    /// 

    public Vector3[] EllipseSegmentProRetro(int numPoints, Vector3 centerPos, Vector3 pos, Vector3 destPoint, bool retrograde)
    {
        OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements(this); 
        Vector3[] points = new Vector3[numPoints];
        CalculateRotation();
        float dtheta = 2f * Mathf.PI / numPoints;

        Vector3 posRelative = pos - centerPos;
        Vector3 destRelative = destPoint - centerPos;
        if (ge.xzOrbits) {
            posRelative = XZPlane.UnityToPhysics(posRelative);
            destRelative = XZPlane.UnityToPhysics(destRelative);
        }

        // Find the angles from the specified points to the axis of the ellipse (defined by theta=0, rotated per orientation)
        float theta1 = (float) OrbitUtils.PhaseAngleRadiansForDirection(new Vector3d(posRelative), oe);
        float theta2 = (float) OrbitUtils.PhaseAngleRadiansForDirection(new Vector3d(destRelative), oe);

        if (retrograde) {
            float tmp = theta2;
            theta2 = theta1;
            theta1 = tmp;
        } 
        if (theta1 > theta2) {
            theta2 += 2 * Mathf.PI;
        }

        int i = 0;
        for (float theta = theta1; theta < theta2; theta += dtheta) {
            points[i] = GetPositionDForThetaRadians(theta, relative: false).ToVector3();
            i++;
            if (i > numPoints - 1)
                break;
        }
        int lasti = i-1;
        if (lasti < 0)
            lasti = 0;
        points[lasti] = GetPositionDForThetaRadians(theta2, relative: false).ToVector3();
        // fill to end with last point
        while (i < numPoints) {
            points[i++] = points[lasti];
        }
        return points;
    }

    public virtual string DumpInfo() {
        string s = string.Format("  OrbitU: p={0:0.00} e={1:0.0000}, i={2:0.000} Om={3:0.00} om={4:0.00}\n" + 
                             "        center={5}\n" +"" +
                             "        r0={6}\n" + "" +
                             "        v0={7}\n" + 
                             "        t0={8}\n" +
                             "        mode={9}\n" +
                             "        tBlend={10}\n",
            p, eccentricity, inclination, omega_uc, omega_lc, centerNbody.name, r0, v0, time0, evolveMode, sgp4BlendTime);
        if (evolveMode == EvolveMode.PKEPLER_J2) {
            s += string.Format("PKEPLER COE0: {0}\n", coe0.ToString());
        }
        return s;
    }

    //-----------------------------------------------
    // Ellipse Gizmo stuff
    //-----------------------------------------------

    private void DrawEllipseGizmo(Vector3 centerPos) {
        int rayCount = 0;
        Gizmos.color = Color.white;
        GravityEngine ge = GravityEngine.Instance();

        GameObject centerObject = centerNbody.gameObject;

        Vector3[] positions = EllipsePositions(NUM_STEPS, centerPos, false); // do not apply mapToScene
        for (int i = 1; i < positions.Length; i++) {
            Gizmos.DrawLine(positions[i], positions[i - 1]);
            rayCount = (rayCount + 1) % STEPS_PER_RAY;
            if (rayCount == 0) {
                Gizmos.DrawLine(centerObject.transform.position, positions[i]);
            }
        }

        // Draw the axes in a different color
        Gizmos.color = Color.red;
        Gizmos.DrawLine(GetPositionForThetaRadians(0.5f * Mathf.PI, centerPos), GetPositionForThetaRadians(-0.5f * Mathf.PI, centerPos));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(GetPositionForThetaRadians(0f, centerPos), GetPositionForThetaRadians(Mathf.PI, centerPos));

        // move body to location specified by parameters but only if GE not running
        if (!Application.isPlaying) {
            NBody nbody = GetComponent<NBody>();
            if (nbody != null) {
                nbody.EditorUpdate(ge);
            }
        }
        // Draw the Hill sphere
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, OrbitUtils.HillRadius(centerObject, transform.gameObject));
    }

    /// <summary>
    /// Determine orbit position in physics space (with rotation and center offset) for the specified
    /// angle in the orbit (in radians)
    /// 
    /// Common code works for ellipse and hyperbola, but not a parabola. Nudge a parabola into hyperbola
    /// </summary>
    /// <param name="thetaRadians"></param>
    /// <param name="centerPos"></param>
    /// <returns></returns>
    public Vector3 GetPositionForThetaRadians(float thetaRadians, Vector3 centerPos) {
        Vector3d pos = GetPositionDForThetaRadians(thetaRadians, relative: true);
        return pos.ToVector3() + centerPos;
    }

    /// <summary>
    /// Determine orbit position in physics space (with rotation and center offset) for the specified
    /// angle in the orbit (in radians)
    /// 
    /// Common code works for ellipse and hyperbola, but not a parabola. Nudge a parabola into hyperbola
    /// </summary>
    /// <param name="thetaRadians"></param>
    /// <param name="relative"></param>
    /// <returns></returns>
    public Vector3d GetPositionDForThetaRadians(double thetaRadians, bool relative)
    {
        double ecc = eccentricity;
        if (Mathd.Abs(ecc - 1f) < 1E-6)
            ecc = (1 + 1E-5);
        double r = (p / (1.0 + ecc * Mathd.Cos(thetaRadians)));
        Vector3d r_pqw = new Vector3d(r * Mathd.Cos(thetaRadians), r * Mathd.Sin(thetaRadians), 0);
        Vector3d r0 = ApplyRotations(r_pqw);
        if (GravityEngine.instance.xzOrbits) {
            r0 = XZPlane.PhysicsToUnity(r0);
        }
        if (!relative) {
            r0 += ge.GetPositionDoubleV3(centerNbody);
        }
        return r0;
    }

    public Vector3d GetVelocityDForThetaRadians(double theta, bool relative)
    {
        double ecc = eccentricity;
        if (Mathd.Abs(eccentricity - 1f) < 1E-6)
            ecc = (1.0 + 1E-5);
        double v_coeef = Mathd.Sqrt(mu / p);
        Vector3d v_pqw = new Vector3d(-v_coeef * Mathd.Sin(theta), v_coeef * (ecc + Mathd.Cos(theta)), 0);
        v0 = ApplyRotations(v_pqw);
        if (GravityEngine.Instance().xzOrbits) {
            v0 = XZPlane.PhysicsToUnity(v0);
        }
        if (!relative) {
            v0 += ge.GetVelocityDoubleV3(centerNbody);
        }
        return v0; 
    }


    /// <summary>
    /// Get the positions where the orbit is at a specified radius. In general there are two. The result
    /// is positions in world space. These can then be used in e.g. TimeOfFlight
    /// 
    /// Use the general equation for hyperbola and ellipse and nudge a parabola into a hyperbola. 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector3[] GetPositionsForRadius(double radius, Vector3 centerPos) {
        float theta = GetPhaseDegForRadius(radius) * Mathf.Deg2Rad;
        return new Vector3[2] { GetPositionForThetaRadians(theta, centerPos), GetPositionForThetaRadians(-theta, centerPos) };

    }

    /// <summary>
    /// Get the phase for the specified radius/altitude. 
    /// 
    /// In the case of an ellipse if the altitute larger/smaller than those allowed just return the max. 
    /// An ellipse will have two values that match the altitude. To get the alternative one use (360 - phase). 
    /// 
    /// A circular orbit will return a phase of zero for any altitude. 
    /// 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public float GetPhaseDegForRadius(double radius) {
        double ecc = eccentricity;
        // Nudge eccentricity if needed to avoid a divide by zero
        if (Mathd.Abs(ecc - 1f) < 1E-6)
            ecc = (float)(1 + 1E-5);
        float theta = 0;
        if (ecc > 1E-8) {
            double thetaEqn = (p / radius - 1) / ecc;
            theta = (float)Mathd.Acos(Mathd.Clamp(thetaEqn, -1.0, 1.0));
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG) {
                if (Mathd.Abs(thetaEqn) > 1.0)
                    Debug.LogFormat("PHASE CLAMP: p={0} r={1} e={2}", p, radius, eccentricity);
            }
#pragma warning restore 162        // apply an impulse to the indicated NBody
        }
        return theta * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Get the current orbital elements using the live position and velocity from GE. 
    /// </summary>
    /// <returns></returns>
    public OrbitUtils.OrbitElements GetCurrentOrbitalElements() {
        return OrbitUtils.RVtoCOE(ge.GetPositionDoubleV3(nbody),
                                  ge.GetVelocityDoubleV3(nbody),
                                  centerNbody,
                                  false /* relative position */);

    }

    /// <summary>
    /// Get the current phase (degrees) of the body based on the position and velocity retreived from GE
    /// </summary>
    /// <returns></returns>
    public double GetCurrentPhase() {
        return OrbitUtils.GetPhaseFromOE(GetCurrentOrbitalElements()) * Mathd.Rad2Deg;
    }


    /// <summary>
    /// Get the positions where the orbit is at a specified radius. In general there are two. The result
    /// is positions in world space. These can then be used in e.g. TimeOfFlight
    /// 
    /// Use the general equation for hyperbola and ellipse and nudge a parabola into a hyperbola. 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector3[] GetPositionsForRadius(double radius) {
        return GetPositionsForRadius(radius, ge.GetPhysicsPosition(centerNbody));
    }

    //-----------------------------------------------
    // Hyperbola Gizmo stuff
    //-----------------------------------------------
    // fraction of the branch of the hyperbola to display in OrbitPositions
    private float branchDisplayFactor = 0.5f;
    private float b; 

    private void DrawHyperGizmo(Vector3 centerPos) {
        GravityEngine ge = GravityEngine.Instance();
        int rayCount = 0;
        Gizmos.color = Color.white;
        Vector3[] points = HyperOrbitPositions(NUM_STEPS, centerPos, false);
        GameObject centerObject = centerNbody.gameObject;

        for (int i = 1; i < NUM_STEPS; i++) {
            Gizmos.DrawLine(points[i - 1], points[i]);
            // draw rays from focus
            rayCount = (rayCount + 1) % STEPS_PER_RAY;
            if (rayCount == 0) {
                Gizmos.DrawLine(centerPos, points[i]);
            }
        }
        Gizmos.color = Color.white;
        // Draw the axes in a different color
        Gizmos.color = Color.red;
        Gizmos.DrawLine(GetPositionForThetaRadians(0.5f * Mathf.PI, centerPos), 
                        GetPositionForThetaRadians(-0.5f * Mathf.PI, centerPos));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(HyperPositionForY(0f, centerPos), centerObject.transform.position);

        // move body to location specified by parameters
        if (!Application.isPlaying) {
            NBody nbody = GetComponent<NBody>();
            if (nbody != null) {
                nbody.EditorUpdate(ge);
            }
        }

    }


 
    /// <summary>
    /// Determine the position in physics space given a Y position wrt the focus.
    /// for the hyperbola. Use Cartesian co-ords since angles are very twitchy for hyperbolas.
    /// </summary>
    /// <param name="y"></param>
    /// <param name="cPos"></param>
    /// <returns></returns>
	private Vector3 HyperPositionForY(float y, Vector3 cPos) {
        float a = (float)(p / (1 - eccentricity * eccentricity));
        float b = (float) p;
        float x = (float)( a * Mathf.Sqrt(1 + y * y / (b * b)));
        // focus is at x = -(a*e), want to translate to origin is at focus
        // -ve x to take the left branch
        Vector3 position = new Vector3((float)(-x + a * eccentricity), y, 0);
        // move from XY plane to the orbital plane
        Vector3 newPosition = conic_orientation * position;
        // orbit position is WRT center
        newPosition += cPos;
        return newPosition;
    }

    public void SetPositionDouble(Vector3d pos) {
        throw new NotImplementedException();
    }
}
