using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OrbitPropagator provides a mechanism to initialize an orbit and then determine the 
/// position and velocity at a future time. 
/// 
/// The initialization can be done via:
/// - an OrbitUniversal
/// - a specific set of r, v, t
///
/// OrbitPropagator ALWAYS works in relative R, V values i.e. it assumes that the central mass is at (0,0,0)!
/// 
/// Future position/velocity is returned as a tuple when PropagateToTime(time) is called. This
/// pos/vel is relative to the center object and not a world value!
/// 
/// This class does not require an NBody or game object to be present. 
/// </summary>
public class OrbitPropagator
{

    private Vector3d v0;
    private Vector3d r0;
    private double time0;

    private double mu;

    private Vector3d naNVector;

    private double small;

    public enum PointType
    {
        APOAPSIS,
        PERIAPSIS,
        ALTITUDE_1ST,
        ALTITUDE_2ND,
        ASCENDING_NODE,
        DESCENDING_NODE
    };

    private OrbitUtils.OrbitElements orbitElements;

    // precalc values
    private double magro, magvo, rdotv, sme, alpha, a;
    private Vector3d normal;

    /// <summary>
    /// Init from an OrbitUniversal.
    ///
    /// </summary>
    /// <param name="orbitU"></param>
    /// <returns></returns>
    public static OrbitPropagator GetPropagator(OrbitUniversal orbitU)
    {
        Vector3d o_r0 = new Vector3d();
        Vector3d o_v0 = new Vector3d();
        double o_time0 = 0.0;
        orbitU.GetRVT(ref o_r0, ref o_v0, ref o_time0);
        return new OrbitPropagator(o_r0, o_v0, o_time0, orbitU.GetMu());
    }

    public OrbitPropagator(Vector3d r0, Vector3d v0, double time0, double mu)
    {
        this.r0 = r0;
        this.v0 = v0;
        this.time0 = time0;
        this.mu = mu;

        naNVector = new Vector3d(double.NaN, double.NaN, double.NaN);

        small = 1E-6; // was 1E-8 but some covergence near-misses in SI units with large values

        // (performance) moved from kepler to here to save some recompute
        magro = r0.magnitude;
        magvo = v0.magnitude;
        rdotv = Vector3d.Dot(r0, v0);
        normal = Vector3d.Cross(r0, v0).normalized;

        // -------------  find sme, alpha, and a  ------------------
        sme = ((magvo * magvo) * 0.5) - (mu / magro);
        alpha = -sme * 2.0 / mu;

        if (Mathd.Abs(sme) > small)
            a = -mu / (2.0 * sme);
        else
            a = double.NaN;
    }

    /// <summary>
    /// Determine the position, velocity and time at a specific type of future point.
    ///
    ///
    /// </summary>
    /// <param name="pointType"></param>
    /// <returns>the RELATIVE r, V and time for the specified orbit point</returns>
    public (Vector3d, Vector3d, double) PropToPoint(PointType pointType)
    {
        Vector3d r = Vector3d.zero;
        Vector3d v = Vector3d.zero;
        double tof = 0.0;
        // using the COE determine the R, V, t at the next requested point type
        OrbitUtils.OrbitElements oeCopy = new OrbitUtils.OrbitElements(orbitElements);
        switch (pointType) {
            case PointType.APOAPSIS:
                // phase = 180
                oeCopy.SetPhase(Mathd.PI);
                OrbitUtils.COEtoRVRelative(oeCopy, mu, ref r, ref v);
                tof = OrbitUtils.TimeOfFlight(r0, r, oeCopy.p, mu, normal);
                break;

            case PointType.PERIAPSIS:
                // phase = 0
                oeCopy.SetPhase(0);
                OrbitUtils.COEtoRVRelative(oeCopy, mu, ref r, ref v);
                tof = OrbitUtils.TimeOfFlight(r0, r, oeCopy.p, mu, normal);
                break;

            default:
                throw new System.Exception("Point type not supported");
        }
        return (r, v, tof + time0);
    }


    public double TimeOfFlight(Vector3d r1)
    {
        Vector3d r0_xy = r0;
        Vector3d r1_xy = r1;
        Vector3d v0_xy = v0;
        if (GravityEngine.Instance().xzOrbits) {
            r0_xy = XZPlane.UnityToPhysics(r0);
            r1_xy = XZPlane.UnityToPhysics(r1);
            v0_xy = XZPlane.UnityToPhysics(v0);
        }
        // h_unit is in xy space
        Vector3d h_unit_xy = Vector3d.Cross(r0_xy, v0_xy).normalized;
        if (orbitElements == null) {
            // this is only place OE are used so calc on demand
            // centerBody not used when relativePos: true
            orbitElements = OrbitUtils.RVtoCOE(r0, v0, centerBody: null, mu, relativePos: true);
        }
        double tof = OrbitUtils.TimeOfFlight(r0_xy, r1_xy, orbitElements.p, mu, h_unit_xy);

        if ((orbitElements.ecc < 1.0) && (tof < 0)) {
            tof += orbitElements.period;
        }
        // for hyperbola want to do Abs. Just do it always
        return Mathd.Abs(tof);
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
    /// Propagate the orbit to the specified time. Uses a modified version of the Vallado Kepler routine. 
    /// 
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <returns>Relative (position, velocity)</returns>
    public (Vector3d, Vector3d) PropagateToTime(double physicsTime)
    {

        // If XZ orbits then r0, v0 are in the XZ plane and this will work without any special code. 
        int ktr, numiter;
        double f, g, fdot, gdot, rval, xold, xoldsqrd,
            xnewsqrd, znew, dtnew, dtsec,
            temp,
            magr;
        double c2new = 0.0;
        double c3new = 0.0;
        double xnew = 0.0;

        Vector3d r_new = naNVector;
        Vector3d v_new = naNVector;

        // can have a weird precision issue when same time used in Init and first evolve.
        if (Mathd.Abs(physicsTime - time0) < 1E-5) {
            // very close to init time, avoid prop and just return r0, v0
            return (r0, v0);
        }

        if ((physicsTime - time0) < 0) {
#pragma warning disable 162        // disable unreachable code warning
            if (GravityEngine.DEBUG) {
                Debug.LogWarning(string.Format("evolution time {0} is before time0 reference {1}",
                physicsTime, time0));
            }
#pragma warning restore 162
            return (naNVector, naNVector);
        }
        // evolution time is relative to time0
        double dtseco = physicsTime - time0;

        // Very large times can cause precision issues, normalize if possible
        //if ((eccentricity < 1) && (dtseco > 1E6)) {
        //    dtseco = dtseco % orbit_period;
        //}

        dtsec = dtseco;

        // -------------------------  implementation   -----------------
        // set constants and intermediate printouts
        numiter = 100;

        // --------------------  initialize values   -------------------
        ktr = 0;
        xold = 0.0;
        znew = 0.0;

        if (Mathd.Abs(dtseco) > small) {


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
                // (Note: parabola code removed)
                // ------------------  hyperbola  ------------------
                temp = -2.0 * mu * dtsec /
                    (a * (rdotv + Mathd.Sign(dtsec) * Mathd.Sqrt(-mu * a) * (1.0 - magro * alpha)));
                xold = Mathd.Sign(dtsec) * Mathd.Sqrt(-a) * Mathd.Log(temp);

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
                    "OrbitProp", numiter, dtnew, tmp, dtsec, Mathd.Abs(dtnew * tmp - dtsec)));
                Debug.LogFormat("alpha={0} a={1}", alpha, a);
                // Mitigation: use last known position
                //ge.GetPositionDouble(nbody, ref r_new);
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
                if (Mathd.Abs(temp - 1.0) > 0.00001)
                    Debug.LogWarning(string.Format("consistency check failed {0}", (temp - 1.0)));
                v_new[0] = fdot * r0.x + gdot * v0.x;
                v_new[1] = fdot * r0.y + gdot * v0.y;
                v_new[2] = fdot * r0.z + gdot * v0.z;
            }
        } // if fabs
        else {
            // ----------- set vectors to incoming since 0 time --------
            r_new[0] = r0.x;
            r_new[1] = r0.y;
            r_new[2] = r0.z;
        }
        return (r_new, v_new);
    }   // kepler

    public override string ToString()
    {
        return string.Format("r0={0} v0={1} time0={2}", r0, v0, time0);
    }
}

