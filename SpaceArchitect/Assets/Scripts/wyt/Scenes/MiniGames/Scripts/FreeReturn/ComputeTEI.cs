using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Compute the TLI information associated with a burn from Earth orbit in a circular orbit r0 to
/// the moon  using the patched conic algorithm from Bate, Meuller and White.
///
/// Inputs:
/// r0: ship parking orbit radius
/// muMoon: G M for the moon
/// muEarth: GM for the Earth
/// lambda1: insertion point on the SOI measured from the vertical towards the earth
/// rMoon: orbital radius of moon's orbit
/// 
/// </summary>
public class ComputeTEI
{
    private static bool DEBUG = false;

    private double r0;
    private double r1;
    private double v1;
    private double v2;

    // flight angle in radians
    private double phi0;

    private double lambda1;

    // Keep key values around so can be used later
    private double theta;
    private double gamma1 = 0;
    private double phi1 = 0;
    private double phi2 = 0;

    private double rMoon;
    // moon velocity
    private double vM; 

    private double soiRadius;
    private double omegaM; 

    private double moonMu;
    private double earthMu;

    // transit times in Earth and moon influence
    private double tE;
    private double tM;

    private double vPerilune;


    public void SetEarthInfo(double mu)
    {
        earthMu = mu;
    }

    public void SetMoonInfo(double mu, double rMoon, double rShip)
    {
        moonMu = mu;
        this.rMoon = rMoon;
        r0 = rShip;
    }

    // Call after updating input parameters
    public void ParamsChanged()
    {
        soiRadius = Math.Pow(moonMu / earthMu, 0.4f) * rMoon;
        vM = Math.Sqrt(earthMu / rMoon);
        // assume moon is in a circular orbit
        omegaM = Math.Sqrt(earthMu / (rMoon * rMoon * rMoon));
        DebugLog("soi={0} omegaM={1} mu={2} moonR={3} vM={4}", soiRadius, omegaM, moonMu, rMoon, vM);
    }

    public void SetTransferParams(double lambda1Deg, double phi0Deg)
    {
        lambda1 = lambda1Deg * GEMath.DEG2RAD;
        phi0 = phi0Deg * GEMath.DEG2RAD;
    }

    public double GetV1()
    {
        return v1;
    }

    public double GetV2()
    {
        return v2;
    }

    public double GetGamma1()
    {
        return gamma1;
    }

    public double GetTheta()
    {
        return theta;
    }

    public double GetOmegaMoon()
    {
        return omegaM;
    }

    public double GetPhi1()
    {
        return phi1;
    }

    public double GetR1()
    {
        return r1;
    }

    public double GetSoiRadius()
    {
        return soiRadius;
    }

    public double GetTOF()
    {
        return tE + tM;
    }

    public double GetTimeToSOI()
    {
        return tM;
    }

    public double GetTimeSOItoPerigee()
    {
        return tE;
    }

    public double GetVPerilune()
    {
        return vPerilune;
    }

    public double GetMoonVel()
    {
        return vM;
    }


    /// <summary>
    /// Follow the approach in "Optimal round trip lunar mission based on the patched-conic approximation"
    /// Filho & Fernandes June 2015, Computational and Applied Mathematics.
    ///
    /// </summary>
    ///
    public double ComputePerigee(double v0)
    {
        // r0 - radius of ship in orbit around moon
        // r1 - distance from SOI exit to Earth
        // r2 - distance from moon to SOI exit
        double D = rMoon;
        double energy = 0.5 * v0 * v0 - moonMu / r0;
        double h = r0 * v0 * Math.Cos(phi0);
        double r2 = soiRadius;
        // velocity at SOI
        double v2 = Math.Sqrt(2.0 * (energy + moonMu / r2));
        phi2 = Math.Acos(GEMath.Clamp(h / (r2 * v2), -1.0, 1.0));
        // Can phi2 be negative? phi2 is FPA at SOI exit. Sign will matter in calc of eta.
        gamma1 = Math.Asin(GEMath.Clamp(r2 * Math.Sin(lambda1) / r1, -1.0, 1.0));
        r1 = Math.Sqrt(D * D + r2 * r2 - 2 * D * r2 * Math.Cos(lambda1));
        phi1 = Math.Atan2(v2 * Math.Sin(phi2 + lambda1), vM + v2 * Math.Cos(phi2 + lambda1)) + gamma1;
        // CW departure
        double eta = phi2 + lambda1;
        // see notes
        double eta_actual = Math.PI - lambda1 - phi2;
        // Eqn (38) *is* cosine law BUT flips Cos to + sign via -Cos(Pi-x) = Cos(x)
        //
        v1 = Math.Sqrt(v2 * v2 + vM * vM + 2 * v2 * vM * Math.Cos(eta));
        DebugLog("Compute: eta_actual={0} (deg) |v1|={1} |V2|={2} |vM|={3}", eta_actual * GEMath.RAD2DEG, v1, v2, vM);
        // geocentric trajectory
        double Q1 = r1 * v1 * v1 / earthMu;
        double af = r1 / (2 - Q1);
        double ef = Math.Sqrt(1 + Q1 * (Q1 - 2) * Math.Cos(phi1) * Math.Cos(phi1));
        double rp_Earth = af * (1 - ef);

        // Compute time of flight info
        double E1 = Math.Acos(GEMath.Clamp(1 / ef * (1 - r1 / af), -1.0, 1.0));
        double Q0 = r0 * v0 * v0 / moonMu;
        double a0 = r0 / (2 - Q0);
        double e0 = Math.Sqrt(1 + Q0 * (Q0 - 2) * Math.Cos(phi0) * Math.Cos(phi0));
        double F2 = GEMath.Acosh(1 / e0 * (1 - r2 / a0));
        DebugLog("af={0} E1={1} ef={2} phi2={3} lambda1={4} vM={5}", af, E1, ef, phi2, lambda1, vM);
        tE = Math.Sqrt(af * af * af / earthMu) * (E1 - ef * Math.Sin(E1));
        tM = Math.Sqrt(-a0 * a0 * a0 / moonMu) * (e0 * Math.Sinh(F2) - F2);
        double f2 = Math.Acos(GEMath.Clamp(a0 * (1 - e0 * e0) / (r2 * e0) - 1 / e0, -1.0, 1.0));
        theta = lambda1 + f2 - Math.PI;
        DebugLog("v0={0} theta={1} perigee={2} tM={3} tE={4} |v1|={5}", v0, theta, rp_Earth, tM, tE, v1);

        return rp_Earth;
    }

    private void DebugLog(string format, params object[] names)
    {
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG)
            Debug.LogFormat(format, names);
#pragma warning restore 162
    }
}
