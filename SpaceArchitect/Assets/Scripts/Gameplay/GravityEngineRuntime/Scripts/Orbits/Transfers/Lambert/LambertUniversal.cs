using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Calculate the universal Lambert transfer. This algorithm allows the transfer to be elliptic or hyperbolic 
/// as necessary to meet the designated transfer time. The transfer code can be used to determine the minimum
/// energy transfer between the desired points/orbits - in this case the time of transfer is not specified. 
/// Additional API calls can be used to request a specific transfer time. It is frquently useful to obtain the
/// most efficient transfer time and then use this as a baseline from which to investigate the dV cost of faster
/// transfers. 
/// 
/// Usage of this class differs from the the other orbit transfer classes. The constructor is used to provide initial 
/// details in one of several ways:
///     fromOrbit/toOrbit: use this when the goal is to attain the toOrbit or rendezvous with an object in the toOrbit
///     
///     fromOrbit/R-to: use when want to get to the specific point R_to
///     
/// Both constructors will invoke ComputeMinTime() and determine the minimum transfer time. This is then retrieved by:
/// GetTMin()
/// 
/// Calls to ComputeXfer() are then used to calculate the maneuvers required for the specified transfer time. 
///     
/// This algorithm does not provide an answer when the transfer is 180 degrees 
/// (Battin is a good alternative in this case). 
/// 
/// This algorithm is more general than the Lambert minimum energy solution since it provides the path for
/// a specified time of flight and also the final velocity.
/// 
/// Code adapted from Vallado, Fundamentals of Astrophysics and Applications
/// https://celestrak.com/software/vallado-sw.asp
/// </summary>
public class LambertUniversal: OrbitTransfer
{
    public double[] v1;
    public double[] v2;

    private Vector3d r1;
    private Vector3d r2;

    private OrbitData innerOrbit;
    private OrbitData outerOrbit;

    private double mu;

    private double a_min;
    // private double e_min;
    private double t_min;

    // center position of the orbit. r1 and r2 are with respect to this position
    private Vector3d center3d;

    private double planetRadius;
    private bool checkPlanetRadius;

    //! Velocity of target when rendezvous is used.
    private Vector3d targetVelocity;

    //! Error codes
    public const int G_NOT_CONVERGED = 1;
    public const int Y_NEGATIVE = 2;
    public const int XFER_180 = 3;
    public const int IMPACT = 4;


    /// <summary>
    /// Setup for Lambert Universal construction. Calculation is done via ComputeXfer and may be 
    /// done more than once for different transit times. 
    /// 
    /// Initial conditions are passed in when created. 
    /// 
    /// The transfer assumes there is an active NBody at the fromOrbit to be maneuvered to the 
    /// orbit specified by the toOrbit. If the toOrbit has a NBody, then it is used as the target point, otherwise 
    /// the phase in the targetOrbit is used to designate a target point.  One of the from/to orbit data elements 
    /// must have a centerNBody. 
    /// 
    /// The time for a minimum energy transfer is computed using the provided value of shortPath. 
    /// 
    /// </summary>
    /// <param name="fromOrbit"></param>
    /// <param name="toOrbit"></param>
    /// <param name="shortPath">Take shortest path along the ellipse (if xfer is an ellipse)</param>

    public LambertUniversal(OrbitData _fromOrbit, OrbitData _toOrbit, bool shortPath) : base(_fromOrbit, _toOrbit) {

        // (fromOrbit and toOrbit are assigned in the base constructor)
        name = "LambertUniversal";

        // Fundamentals of Astrodynamics and Applications, Vallado, 4th Ed., Algorithm 56 p475 
        // Take r0, r => a_min, e_min, t_min, v0

        double[] r1_array = new double[] { 0, 0, 0 };
        double[] r2_array = new double[] { 0, 0, 0 };
        double[] center = new double[] { 0, 0, 0 };

        GravityEngine ge = GravityEngine.Instance();

        // If Nbody is available get position directly. If not (target marker) then 
        // use orbit phase to compute position
        ge.GetPositionDouble(centerBody, ref center);
        center3d = new Vector3d(ref center);

        if (fromOrbit.nbody != null) {
            ge.GetPositionDouble(fromOrbit.nbody, ref r1_array);
        } else {
            Vector3 pos = toOrbit.GetPhysicsPositionforEllipse(fromOrbit.phase);
            r1_array = new double[] { pos.x, pos.y, pos.z };
        }

        if (toOrbit.nbody != null) {
            ge.GetPositionDouble(toOrbit.nbody, ref r2_array);
        } else {
            Vector3 pos = toOrbit.GetPhysicsPositionforEllipse(toOrbit.phase);
            r2_array = new double[] { pos.x, pos.y, pos.z };
        }

        r1 = new Vector3d(ref r1_array) - center3d;
        r2 = new Vector3d(ref r2_array) - center3d;

        if (fromOrbit.a < toOrbit.a) {
            innerOrbit = fromOrbit;
            outerOrbit = toOrbit;
        } else {
            innerOrbit = toOrbit;
            outerOrbit = fromOrbit;
        }
        mu = ge.GetPhysicsMass(fromOrbit.centralMass);
        // determine t_min. 
        // If Nbody is available get position directly. If not (target marker) then 
        // use orbit phase to compute position
        double[] r0 = new double[] { 0, 0, 0 };
        double[] r = new double[] { 0, 0, 0 };
        if (innerOrbit.nbody != null) {
            ge.GetPositionDouble(innerOrbit.nbody, ref r0);
            r0[0] -= center3d.x;
            r0[1] -= center3d.y;
            r0[2] -= center3d.z;
        } else {
            Vector3 pos = innerOrbit.GetPhysicsPositionforEllipseRelative(innerOrbit.phase);
            r0 = new double[] { pos.x, pos.y, pos.z };
        }

        if (outerOrbit.nbody != null) {
            ge.GetPositionDouble(outerOrbit.nbody, ref r);
            r[0] -= center3d.x;
            r[1] -= center3d.y;
            r[2] -= center3d.z;
        } else {
            Vector3 pos = outerOrbit.GetPhysicsPositionforEllipseRelative(outerOrbit.phase);
            r = new double[] { pos.x, pos.y, pos.z };
        }

        ComputeMinTime(ref r0, ref r, fromOrbit.centralMass, shortPath);
    }

    /// <summary>
    /// Create a Lambert transfer from a given NBody orbit to a position in physics space. 
    /// 
    /// The constructor will compute the minimum energy transfer to the point taking the shortest
    /// path setting into account. The time for the transfer can then be retreived. 
    /// </summary>
    /// <param name="_fromOrbit"></param>
    /// <param name="r_from"></param>
    /// <param name="r_to"></param>
    /// <param name="shortPath"></param>
    public LambertUniversal(OrbitData _fromOrbit, Vector3d r_from, Vector3d r_to, bool shortPath) : base(_fromOrbit) {
        name = "LambertUniversal";

        GravityEngine ge = GravityEngine.Instance();

        center3d = ge.GetPositionDoubleV3(centerBody);
        r1 = r_from - center3d;
        r2 = r_to - center3d;

        fromOrbit = _fromOrbit;
        innerOrbit = fromOrbit;
        mu = ge.GetPhysicsMass(fromOrbit.centralMass);
        // determine t_min. 
        double[] r0 = new double[] { r1.x, r1.y, r1.z };
        double[] r = new double[] { r2.x, r2.y, r2.z };
        ComputeMinTime(ref r0, ref r, _fromOrbit.centralMass, shortPath);

    }

    /// <summary>
    /// Compute the transfer time for the minimum energy transfer using relative positions for r0 and r.
    /// </summary>
    /// <returns></returns>
    private double ComputeMinTime(ref double[] r0, ref double[] r, NBody centerBody, bool shortPath) {

        // Fundamentals of Astrodynamics and Applications, Vallado, 4th Ed., Algorithm 56 p475 
        // Take r0, r => a_min, e_min, t_min
        // This approach assumes r > r0. 
        // If we have r0 > r then need to flip them and then reverse the velocities when doing the 
        // maneuver determination

        Vector3d r0_vec = new Vector3d(ref r0); 
        Vector3d r_vec = new Vector3d(ref r); 

        double r0_mag = Vector3d.Magnitude(r0_vec);
        double r_mag = Vector3d.Magnitude(r_vec);
        double cos_delta_nu = Vector3d.Dot(r0_vec, r_vec) / (r0_mag * r_mag);

        double c = System.Math.Sqrt(r0_mag * r0_mag + r_mag * r_mag
                    - 2 * r0_mag * r_mag * cos_delta_nu);
        double s = (r0_mag + r_mag + c) / 2;
        a_min = s / 2;
        // double p_min = r0_mag * r_mag * (1 - cos_delta_nu) / c;
        // e_min = System.Math.Sqrt(1 - 2 * p_min / s);

        // alpha for elliptical xfer, min energy
        double alpha_e = System.Math.PI;

        // p471: set beta_e negative if delta_nu > 18
        double beta_e = 2.0 * System.Math.Asin(System.Math.Sqrt((s - c) / s));
        // t_min for a_min
        // +/- in front of (beta_e...). Which sign? Negative for shorter path. 
        double sign = -1;
        if (!shortPath) {
            sign = 1;
        }
        t_min = System.Math.Sqrt(a_min * a_min * a_min / mu) * (alpha_e + sign * (beta_e - System.Math.Sin(beta_e)));
        return t_min;
    }

    // only need value up to 13 in the Lambert universal, so make it a look up
    private double[] factorial = { 1.0, 1.0, 2.0, 6.0, 24.0, 120.0, 720.0, 5040.0, 40320.0,
            362880.0, 3628800.0, 3.991680E7, 4.790016E8, 6227021E9};

    private double Factorial(int x) {
        return factorial[x];
    }

    /// <summary>
    /// Set the radius of the planet around which transfer is to be computed. This is used when calls to 
    /// ComputeTransferWithPhasing(). In the event that the requested trajectory hits the planet 
    /// </summary>
    /// <param name="radius"></param>
    public void SetPlanetRadius(double radius) {
        planetRadius = radius;
        checkPlanetRadius = true;
    }

    public void ClearPlanetRadius()
    {
        checkPlanetRadius = false;
    }

    //! Modes used internally to indicate type of calculation in ComputeXfer. 
    private enum ComputeMode { USE_R2, INTERCEPT, RENDEZVOUS};

    /// <summary>
    /// Determine the Lambert transfer in the specified time to the position that target object will be at in that
    /// time. If the rendezvous flag is set, then a maneuver will be generated to match the target. If not set then 
    /// the ship will intercept the target. 
    /// 
    /// This method requires that the class was created with the constructor that defines toOrbit. 
    /// 
    /// In some cases the transfer trajectory may intersect the central body. This can optionally be checked if the 
    /// API SetPlanetRadius() is called prior to the use of this method. If the requested trajectory hits the planet
    /// an error code 4. 
    /// 
    /// </summary>
    /// <param name="reverse"></param>
    /// <param name="df"></param>
    /// <param name="nrev"></param>
    /// <param name="dtsec">time for the transfer</param>
    /// <param name="rendezvous"></param>
    /// <returns>error ()</returns>
    ///     error = 1;   // g not converged
    ///     error = 2;   // y negative
    ///     error = 3;   // impossible 180 transfer    
    ///     error = 4;   // trajectory impacts planet radius specified with SetPlanetRadius()    
    ///     

    public int ComputeXferWithPhasing(
        bool reverse,
        bool df,
        int nrev,
        double dtsec, 
        bool rendezvous) {

        // confirm there is a target NBody (i.e. correct constructor was used)
        if (toOrbit == null || toOrbit.nbody == null) {
            Debug.LogError("Cannot compute phasing, no target orbit was specified.");
        }

        // determine the destination position by evolving the target by dtsec
        OrbitUniversal targetOrbit = toOrbit.nbody.gameObject.AddComponent<OrbitUniversal>();
        targetOrbit.InitFromActiveNBody(toOrbit.nbody, toOrbit.centralMass, OrbitUniversal.EvolveMode.KEPLERS_EQN);
        double[] r_new = new double[] { 0, 0, 0 };
        double[] v_new = new double[] { 0, 0, 0 };
        GravityEngine ge = GravityEngine.Instance();
        GravityState worldState = ge.GetWorldState();
        // Awkward if body is in orbit, since will use the current center pos and center vel and the position at the
        // time we're evolving to. Since we need relative, can do some fudging...
        targetOrbit.Evolve(ge.GetPhysicalTimeDouble() + dtsec, worldState, ref r_new, ref v_new);
        r2 = new Vector3d(ref r_new) - ge.GetPositionDoubleV3(fromOrbit.centralMass);
        targetVelocity = new Vector3d(ref v_new) - ge.GetVelocityDoubleV3(fromOrbit.centralMass);
        MonoBehaviour.Destroy(targetOrbit);

        ComputeMode mode = (rendezvous) ? ComputeMode.RENDEZVOUS : ComputeMode.INTERCEPT; 
        return ComputeXferInternal(reverse, df, nrev, dtsec, mode);
    }

    /// <summary>
    /// Get the final position of the Lambert trajectory. Typically used when doing a xfer with phasing. 
    /// </summary>
    /// <returns></returns>
    public Vector3d GetR2() {
        return r2; 
    }

    /// <summary>
    /// Compute the transfer for the setup via the constructor for the specified transit time dtsec. 
    /// 
    /// This determines the maneuvers requires and places them in the parent classes maneuver list. They 
    /// can be retrieved by GetManeuvers().
    /// 
    /// There will be 1 or 2 maneuvers depending on the configuration. If a target orbit is specified and the
    /// mode is not intercept then a second maneuver will be used to attain the desired target orbit. 
    /// 
    /// The algorithm used comes from Vallado 4th edition Algorithm 58 (p492).
    /// 
    /// Note: Unlike the min. energy calculation it does NOT require r1 < r2. 
    /// 
    /// </summary>
    /// <param name="reverse">direction of motion, true for reverse path (long way around)</param>
    /// <param name="df">Controls intial guess, undocumented in Vallado. False seems to work.</param>
    /// <param name="nrev">Number of revolutions until transfer (typically 0)</param>
    /// <param name="dtsec">Required time of flight</param>
    /// <returns>error ()</returns>
    ///     error = 1;   // g not converged
    ///     error = 2;   // y negative
    ///     error = 3;   // impossible 180 transfer    
    ///     error = 4;   // trajectory impacts planet radius specified with SetPlanetRadius()    
    public int ComputeXfer(
               bool reverse,
               bool df,
               int nrev,
               double dtsec) {
        return ComputeXferInternal(reverse, df, nrev, dtsec, ComputeMode.USE_R2);
    }

    private int ComputeXferInternal(
        bool reverse,
        bool df,
        int nrev,
        double dtsec, ComputeMode mode) 
    { 
        const double small = 0.0000001;
        const int numiter = 40;
        // const double mu = 398600.4418;  // m3s2
        const double pi = System.Math.PI;

        v1 = new double[] { 0, 0, 0 };
        v2 = new double[] { 0, 0, 0 };

        int loops, ynegktr;
        double vara, y, upper, lower, cosdeltanu, f, g, gdot, xold, xoldcubed, magr1, magr2,
            psiold, psinew, c2new, c3new, dtnew, c2dot, c3dot, dtdpsi, psiold2;

        y = 0.0; 

        /* --------------------  initialize values   -------------------- */
        int error = 0;
        psinew = 0.0;

        magr1 = r1.magnitude;
        magr2 = r2.magnitude;

        cosdeltanu = Vector3d.Dot(r1, r2) / (magr1 * magr2);
        if (reverse)
            vara = -System.Math.Sqrt(magr1 * magr2 * (1.0 + cosdeltanu));
        else
            vara = System.Math.Sqrt(magr1 * magr2 * (1.0 + cosdeltanu));

        /* -------- set up initial bounds for the bissection ------------ */
        if (nrev == 0) {
            upper = 4.0 * pi * pi;  // could be negative infinity for all cases
            lower = -4.0 * pi * pi; // allow hyperbolic and parabolic solutions
        } else {
            lower = 4.0 * nrev * nrev * pi * pi;
            upper = 4.0 * (nrev + 1.0) * (nrev + 1.0) * pi * pi;
        }

        /* ----------------  form initial guesses   --------------------- */
        psinew = 0.0;
        xold = 0.0;
        if (nrev == 0) {
            // use log to get initial guess
            // empirical relation here from 10000 random draws
            // 10000 cases up to 85000 dtsec  0.11604050x + 9.69546575
            psiold = (System.Math.Log(dtsec) - 9.61202327) / 0.10918231;
            if (psiold > upper)
                psiold = upper - pi;
        } else {
            if (df)
                psiold = lower + (upper - lower) * 0.3;
            else
                psiold = lower + (upper - lower) * 0.6;
        }
        OrbitUtils.FindC2C3(psiold, out c2new, out c3new);


        /* --------  determine if the orbit is possible at all ---------- */
        if (System.Math.Abs(vara) > small)  // 0.2??
        {
            loops = 0;
            ynegktr = 1; // y neg ktr
            dtnew = -10.0;
            while ((System.Math.Abs(dtnew - dtsec) >= small) && (loops < numiter) && (ynegktr <= 10)) {
                loops = loops + 1;
                if (System.Math.Abs(c2new) > small)
                    y = magr1 + magr2 - (vara * (1.0 - psiold * c3new) / System.Math.Sqrt(c2new));
                else
                    y = magr1 + magr2;
                /* ------- check for negative values of y ------- */
                if ((vara > 0.0) && (y < 0.0)) {
                    ynegktr = 1;
                    while ((y < 0.0) && (ynegktr < 10)) {
                        psinew = 0.8 * (1.0 / c3new) *
                            (1.0 - (magr1 + magr2) * System.Math.Sqrt(c2new) / vara);

                        /* ------ find c2 and c3 functions ------ */
                        OrbitUtils.FindC2C3(psinew, out c2new, out c3new);
                        psiold = psinew;
                        lower = psiold;
                        if (System.Math.Abs(c2new) > small)
                            y = magr1 + magr2 -
                            (vara * (1.0 - psiold * c3new) / System.Math.Sqrt(c2new));
                        else
                            y = magr1 + magr2;
                        ynegktr++;
                    }
                }

                if (ynegktr < 10) {
                    if (System.Math.Abs(c2new) > small)
                        xold = System.Math.Sqrt(y / c2new);
                    else
                        xold = 0.0;
                    xoldcubed = xold * xold * xold;
                    dtnew = (xoldcubed * c3new + vara * System.Math.Sqrt(y)) / System.Math.Sqrt(mu);

                    // try newton rhapson iteration to update psi
                    if (System.Math.Abs(psiold) > 1e-5) {
                        c2dot = 0.5 / psiold * (1.0 - psiold * c3new - 2.0 * c2new);
                        c3dot = 0.5 / psiold * (c2new - 3.0 * c3new);
                    } else {
                        psiold2 = psiold * psiold;
                        c2dot = -1.0 / Factorial(4) + 2.0 * psiold / Factorial(6) - 3.0 * psiold2 / Factorial(8) +
                            4.0 * psiold2 * psiold / Factorial(10) - 5.0 * psiold2 * psiold2 / Factorial(12);
                        c3dot = -1.0 / Factorial(5) + 2.0 * psiold / Factorial(7) - 3.0 * psiold2 / Factorial(9) +
                            4.0 * psiold2 * psiold / Factorial(11) - 5.0 * psiold2 * psiold2 / Factorial(13);
                    }
                    dtdpsi = (xoldcubed * (c3dot - 3.0 * c3new * c2dot / (2.0 * c2new)) + vara / 8.0 * (3.0 * c3new * System.Math.Sqrt(y) / c2new + vara / xold)) / System.Math.Sqrt(mu);
                    psinew = psiold - (dtnew - dtsec) / dtdpsi;

                    // check if newton guess for psi is outside bounds(too steep a slope)
                    if (System.Math.Abs(psinew) > upper || psinew < lower) {
                        // --------readjust upper and lower bounds------ -
                        if (dtnew < dtsec) {
                            if (psiold > lower)
                                lower = psiold;
                        }
                        if (dtnew > dtsec) {
                            if (psiold < upper)
                                upper = psiold;
                        }
                        psinew = (upper + lower) * 0.5;
                    }
                    /* -------------- find c2 and c3 functions ---------- */
                    OrbitUtils.FindC2C3(psinew, out c2new, out c3new);
                    psiold = psinew;

                    /* ---- make sure the first guess isn't too close --- */
                    if ((System.Math.Abs(dtnew - dtsec) < small) && (loops == 1))
                        dtnew = dtsec - 1.0;
                }
            }

            if ((loops >= numiter) || (ynegktr >= 10)) {
                error = 1; // g not converged

                if (ynegktr >= 10) {
                    error = Y_NEGATIVE;  // y negative
                }
            } else {
                /* ---- use f and g series to find velocity vectors ----- */
                f = 1.0 - y / magr1;
                gdot = 1.0 - y / magr2;
                g = 1.0 / (vara * System.Math.Sqrt(y / mu)); // 1 over g
                //	fdot = sqrt(y) * (-magr2 - magr1 + y) / (magr2 * magr1 * vara);
                //for (int i = 0; i < 3; i++) {
                //    v1[i] = ((r2[i] - f * r1[i]) * g);
                //    v2[i] = ((gdot * r2[i] - r1[i]) * g);
                //}
                v1[0] = ((r2.x - f * r1.x) * g);
                v2[0] = ((gdot * r2.x - r1.x) * g);
                v1[1] = ((r2.y - f * r1.y) * g);
                v2[1] = ((gdot * r2.y - r1.y) * g);
                v1[2] = ((r2.z - f * r1.z) * g);
                v2[2] = ((gdot * r2.z - r1.z) * g);
            }
        } else
            error = XFER_180;   // impossible 180 transfer

        // Determine maneuvers needed for ship (fromOrbit)
        if (error == 0) {
            if (checkPlanetRadius) {
                bool hit = OrbitUtils.CheckHitPlanet(planetRadius, mu, r1, new Vector3d(ref v1),
                                                        r2, new Vector3d(ref v2),
                                                        nrev: 0);
                if (hit) {
                    error = IMPACT;
                    // allow maneuver to proceed, might want to display the orbit that results
                }
            }

            maneuvers.Clear();
            Vector3 v1_vec = new Vector3((float)v1[0], (float)v1[1], (float)v1[2]);
            Vector3 v2_vec = new Vector3((float)v2[0], (float)v2[1], (float)v2[2]);
            Vector3d centerVel = GravityEngine.instance.GetVelocityDoubleV3(fromOrbit.centralMass);
            Vector3d fromVelRelative = GravityEngine.instance.GetVelocityDoubleV3(fromOrbit.nbody) - centerVel;
            // Departure
            Maneuver departure = new Maneuver();
            departure.mtype = Maneuver.Mtype.setv;
            departure.label = "Lam.1";
            departure.nbody = fromOrbit.nbody;
            departure.physPosition = r1 + center3d;
            departure.velChange = v1_vec;          
            departure.worldTime = (float)GravityEngine.Instance().GetGETime();
			// relative info
            departure.relativePos = r1;
            departure.relativeVel = new Vector3d(v1_vec);
            departure.relativeTo = fromOrbit.centralMass;
            departure.dVvector = new Vector3d(v1_vec) - fromVelRelative;
            departure.dV = (float) departure.dVvector.magnitude;

            maneuvers.Add(departure);
            deltaV = departure.velChange.magnitude;

            // Arrival (will not be required if intercept)
            // setv is wrt to centerVel
            if ((toOrbit != null) && (mode != ComputeMode.INTERCEPT)) {
                Maneuver arrival = new Maneuver();
                arrival.label = "Lam.2";
                arrival.nbody = fromOrbit.nbody;
                arrival.physPosition = r2 + center3d; // wrong if in orbit, centerPos will have moved on
                arrival.worldTime = departure.worldTime + (float)dtsec;
                arrival.mtype = Maneuver.Mtype.setv;
                if (mode == ComputeMode.RENDEZVOUS) {
                    arrival.velChange = targetVelocity.ToVector3();
                } else {
                    // this is a relative vel.
                    arrival.velChange = toOrbit.GetPhysicsVelocityForEllipse(toOrbit.phase);
                }
                // relative info
                arrival.relativePos = r2;
                arrival.relativeVel = new Vector3d(arrival.velChange);
                arrival.relativeTo = fromOrbit.centralMass;
                // Determine xfer arrival velocity to determine dV, dVvector
                OrbitPropagator orbitProp1 = new OrbitPropagator(departure.relativePos, departure.relativeVel, 0, toOrbit.mu);
                (Vector3d r1_xfer, Vector3d v3d1_xfer) = orbitProp1.PropagateToTime(dtsec);
                arrival.dVvector = arrival.relativeVel - v3d1_xfer;
                arrival.dV = (float)arrival.dVvector.magnitude;
                maneuvers.Add(arrival);
                deltaV += arrival.dV;
            }

        }
        return error;
    }

    /// <summary>
    /// Compute the path from (r1,v1) to (r2,v2) that minimizes the value of (dV)^2. This uses the 
    /// algorithm developed in "A closed-form solution to the minimum (delta Vtotal)^2 Lambert's Problem"
    /// by M. Avendano and D. Mortari, Celest.Mech.Dyn.Ast(2010) 106:25-37. 
    /// 
    /// Note that this solution is not the minimum dV solution but will never exceed it by more than 17%
    /// (and for non-aligned r1, r2 is typically very close to the dV solution). 
    /// 
    /// Currently need to exclude transfer requests where angle between r1 and r2 is in range
    ///   (165, 195) either get excessive dV or no real roots in the solution. 
    /// 
    /// </summary>
    /// <param name="r1">GE position of the start (not relative to center)</param>
    /// <param name="v1">GE velocity of the start (not relative to center)</param>
    /// <param name="r2">GE position of the start (not relative to center)</param>
    /// <param name="v2">GE velocity of the start (not relative to center)</param>
    public void ComputeMinDvSquared(Vector3d r1_, Vector3d v1_, Vector3d r2_, Vector3d v2_) {

        GravityEngine ge = GravityEngine.Instance();
        // get the relative values
        Vector3d centerPos = ge.GetPositionDoubleV3(fromOrbit.centralMass);
        Vector3d centerVel = ge.GetVelocityDoubleV3(fromOrbit.centralMass);
        Vector3d r1 = r1_ - centerPos;
        Vector3d v1 = v1_ - centerVel;
        Vector3d r2 = r2_ - centerPos;
        Vector3d v2 = v2_ - centerVel;

        Vector3d h = Vector3d.Cross(r1, r2).normalized;
        if (Vector3d.Angle(r1,r2) > 179.0) {
            // co-linear is a special case. See section 3 of the paper "Singularity"
            ComputeMinDvSqSingular(r1, v1, r2, v2);
            return;
        }
        //Debug.LogFormat("h={0} r1={1} r2={2}", h, r1, r2);
        Vector3d axis_r1 = Vector3d.Cross(r1, v1).normalized;
        if (Vector3d.Dot(h, axis_r1) < 0)
        {
            h *= -1.0;
        }

        double R1 = r1.magnitude;
        double R2 = r2.magnitude;

        double sqrtmu = Mathd.Sqrt(mu);
        // eqn (4)
        Vector3d s1 = Vector3d.Cross(h, r1.normalized);
        double sin_dPhi = Vector3d.Dot(r2.normalized, s1);
        double cos_dPhi = Vector3d.Dot(r1.normalized, r2.normalized);

        // eqns (5)
        double V1r = Vector3d.Dot(v1, r1.normalized);
        double V2r = Vector3d.Dot(v2, r2.normalized);

        double V1s = Vector3d.Dot(v1, s1);
        Vector3d s2 = Vector3d.Cross(h, r2.normalized);
        double V2s = Vector3d.Dot(v2, s2);

        // Eqn (16)
        double K1 = sqrtmu / sin_dPhi * (cos_dPhi / R1 - 1 / R2) * V1r + sqrtmu / R1 * V1s;
        double L1 = (1 - cos_dPhi) * sqrtmu * V1r / sin_dPhi;

        // Eqn (19)
        double K2 = sqrtmu / sin_dPhi * (1 / R1 - cos_dPhi / R2) * V2r + sqrtmu / R2 * V2s;
        double L2 = sqrtmu * (cos_dPhi - 1) * V2r / sin_dPhi;
        // Eqns (21)
        double a1 = (cos_dPhi / R1 - 1 / R2);
        double alpha = -1 / (R1 * R1) - 1 / (sin_dPhi * sin_dPhi) * a1*a1;
        double g1 = (1.0 - cos_dPhi);
        double gamma = -1.0*(g1*g1)/(sin_dPhi * sin_dPhi);
        double beta = 2/R1-2*g1/(sin_dPhi*sin_dPhi)*(cos_dPhi/R1-1/R2);

        // eqns (25)
        double c3 = (K1 + K2) / (2.0 * alpha * sqrtmu);
        double c1 = -sqrtmu / (2 * alpha) * (L1 + L2);
        double c0 = -mu * mu * gamma / alpha;
        System.Numerics.Complex[] roots = PolynomialSolver.Quartic(c3, 0, c1, c0);

        double hoptPos = double.NaN;
        double hoptNeg = double.NaN;
        foreach (System.Numerics.Complex c in roots) {
            if (Mathd.Abs(c.Imaginary) < 1E-5) {
                if (c.Real > 0)
                    hoptPos = c.Real;
                else
                    hoptNeg = c.Real;
            }
        }
        double hopt = hoptPos;
        if (double.IsNaN(hopt)) 
            hopt = hoptNeg;
        if (double.IsNaN(hopt)) {
            string rootStr = string.Format(" Quartic c3={0} c1={1} c0={2} ", c3, c1, c0);
            rootStr += " Roots: ";
            foreach (System.Numerics.Complex c in roots)
                rootStr += string.Format(" {0}", c);
            Debug.LogWarning("Could not find real root. Likely that R1 and R2 are too close to linear. Bail out r1 dot r2=" + Vector3d.Dot(r1.normalized, r2.normalized) 
                + rootStr);
            return;
        }

        double p = hopt * hopt / mu;
        double ecc = Mathd.Sqrt(1 - alpha * p * p - beta * p - gamma);

        // compute value for phi using (10), (12)
        double cos_phi1 = (1 / ecc) * (p / R1 - 1);
        // if negative h, then need -dPhi. cos is unchanged
        if (hopt < 0)
            sin_dPhi *= -1; 
        double sin_phi1 = (1/ecc)*(1/sin_dPhi)*((p/R1-1)*cos_dPhi-(p/R2-1));

        // (Not from the paper)
        // At this point have: p, ecc, phi1, hopt
        // velocity magnitude (any ecc != 1)
        double a = p / (1 - ecc * ecc);

        // Need to align the orbit properly in space.
        Vector3d v_depart = Vector3d.Cross(h, r1.normalized);
        v_depart = v_depart * Mathd.Sqrt(2.0 * mu / R1 - mu / a);
        double fpa_radians = OrbitUtils.FlightPathAngle(sin_phi1, cos_phi1, ecc);
        Vector3 v_departv3 = Quaternion.AngleAxis((float)(-fpa_radians * Mathd.Rad2Deg), h.ToVector3())
                                * v_depart.ToVector3();
        

        maneuvers.Clear();
        float timeNow = ge.GetPhysicalTime();
        // Departure
        Maneuver departure = new Maneuver();
        departure.mtype = Maneuver.Mtype.setv;
        departure.label = "Lam.mDv.1";
        departure.nbody = fromOrbit.nbody;
        departure.physPosition = r1_;
        departure.velChange = v_departv3; 
        departure.worldTime = timeNow;
        departure.dV = (v1.ToVector3() - v_departv3).magnitude;
        // relative info
        departure.relativePos = r1;
        departure.relativeVel = new Vector3d(v_departv3);
        departure.relativeTo = fromOrbit.centralMass;
        departure.dVvector = new Vector3d(v_departv3) - v1;
        maneuvers.Add(departure);

        OrbitPropagator orbitProp = new OrbitPropagator(r1, new Vector3d(v_departv3), 0.0, fromOrbit.mu);
        double tof = orbitProp.TimeOfFlight(r2);
        (Vector3d r_arrival, Vector3d v_arrival) = orbitProp.PropagateToTime(tof);

        // Arrival
        Maneuver arrival = new Maneuver();
        arrival.mtype = Maneuver.Mtype.setv;
        arrival.label = "Lam.mDv.2";
        arrival.nbody = fromOrbit.nbody;
        arrival.physPosition = r2_;
        arrival.velChange = v2.ToVector3(); 
        arrival.worldTime = (float)(timeNow + tof);
        arrival.relativePos = r2;
        arrival.relativeVel = v2;
        arrival.relativeTo = fromOrbit.centralMass;
        
        arrival.dVvector = v2 - v_arrival ;
        arrival.dV = (float) arrival.dVvector.magnitude;
        maneuvers.Add(arrival);

    }

    private void ComputeMinDvSqSingular(Vector3d r1, Vector3d v1, Vector3d r2, Vector3d v2)
    {
        GravityEngine ge = GravityEngine.Instance();


        double R1 = r1.magnitude;
        double R2 = r2.magnitude;
        double p = 2 * R1 * R2 / (R1 + R2);
        double h = Mathd.Sqrt(mu * p);

        Vector3d r1n = r1.normalized;
        Vector3d h1 = Vector3d.Cross(r1, v1).normalized;
        Vector3d s1 = Vector3d.Cross(h1, r1n).normalized;

        double xi = 0.5 * (Vector3d.Dot(v1, r1n) + Vector3d.Dot(v2, r1n));
        double W1_n = h / R1;
        double W2_n = h / R2; 

        double theta = (fromOrbit.inclination - toOrbit.inclination) * Mathd.Deg2Rad; 
        double[,] R1_theta = new double[3, 3] { { 1, 0, 0},
                                                {0, Mathd.Cos(theta), Mathd.Sin(theta) },
                                                {0, -Mathd.Sin(theta), Mathd.Cos(theta) }
                                              };
        double[,] R1_theta_minus = new double[3, 3] { { -1, 0, 0},
                                                {0, -Mathd.Cos(theta), -Mathd.Sin(theta) },
                                                {0, Mathd.Sin(theta), -Mathd.Cos(theta) }
                                              };
        Vector3d x1 = new Vector3d(xi, W1_n, 0);
        Vector3d x2 = new Vector3d(xi, W2_n, 0);
        Vector3d W1_1 = Matrix3.MatrixTimesVector(ref R1_theta, ref x1);
        Vector3d W2_1 = Matrix3.MatrixTimesVector(ref R1_theta_minus, ref x2);

        double[,] C = new double[3, 3] { { r1n.x, s1.x, h1.x},
                                         { r1n.y, s1.y, h1.y},
                                         { r1n.z, s1.z, h1.z}};
        Vector3d W1 = Matrix3.MatrixTimesVector(ref C, ref W1_1);
        Vector3d W2 = Matrix3.MatrixTimesVector(ref C, ref W2_1);

        maneuvers.Clear();
        Vector3d centerPos = ge.GetPositionDoubleV3(fromOrbit.centralMass);
        // Departure
        Maneuver departure = new Maneuver();
        departure.mtype = Maneuver.Mtype.setv;
        departure.label = "Lam.mDvS.1";
        departure.nbody = fromOrbit.nbody;
        departure.physPosition = r1 + centerPos;
        departure.velChange = W1.ToVector3();
        departure.worldTime = (float)GravityEngine.Instance().GetGETime();
        departure.dV = (float) (v1-W1).magnitude;
        departure.relativePos = r1;
        departure.relativeVel = W1;
        departure.relativeTo = fromOrbit.centralMass;
        departure.dVvector = departure.relativeVel.normalized * departure.dV;
        maneuvers.Add(departure);

        OrbitPropagator orbitProp = new OrbitPropagator(r1, W1, 0.0, fromOrbit.mu);
        double tof = orbitProp.TimeOfFlight(r2);
        (Vector3d r_arrival, Vector3d v_arrival) = orbitProp.PropagateToTime(tof);

        // Arrival
        Maneuver arrival = new Maneuver();
        arrival.label = "Lam.mDvS.2";
        arrival.mtype = Maneuver.Mtype.setv;
        arrival.nbody = fromOrbit.nbody;// TODO: need absolute value
        arrival.velChange = v2.ToVector3();
        arrival.physPosition = r2 + centerPos; // Wrong if in orbit
        arrival.worldTime = (float)(ge.GetPhysicalTime() + tof);
        // TODO: Check sign
        arrival.dV = (float)(W2 - v2).magnitude;
        arrival.relativePos = r2;
        arrival.relativeVel = v2;
        arrival.relativeTo = fromOrbit.centralMass;
        arrival.dVvector = arrival.relativeVel.normalized * arrival.dV;
        maneuvers.Add(arrival);
    }

    public void ComputeIntercept() {

    }

    public Vector3d GetTransferPositionDouble() {
        return r1;
    }

    public Vector3 GetTransferVelocity() {
         return new Vector3((float)v1[0], (float)v1[1], (float)v1[2]);
    }

    public Vector3d GetTransferVelocityDouble() {
        return new Vector3d(v1[0],v1[1], v1[2]);
    }

    public Vector3 GetFinalVelocity() {
        return new Vector3((float)v2[0], (float)v2[1], (float)v2[2]);
    }

    public Vector3d GetFinalVelocityDouble() {
        return new Vector3d(v2[0], v2[1], v2[2]);
    }

    //! Time of flight for minimum energy trajectory
    public double GetTMin() {
        return t_min;
    }

    public string Log()
    {
        return string.Format("from.go={0} r1={1} r2={2}", fromOrbit.nbody.gameObject, r1, r2);
    }
}

