using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Compute the TLI information associated with a burn from Earth orbit in a circular orbit r0 to
/// the moon  using the patched conic algorithm from Bate, Meuller and White.
///
/// This assumes ship and moon are in circular, co-planar orbits.
///
/// Inputs:
/// r0: ship parking orbit radius
/// muMoon: G M for the moon
/// muEarth: GM for the Earth
/// lambda1: insertion point on the SOI measured from the vertical towards the earth
/// rMoon: orbital radius of moon's orbit
/// 
/// </summary>
public class ComputeTLI 
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
    private double gamma0;
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


    public void SetEarthInfo(double mu, double r0)
    {
        this.r0 = r0;
        earthMu = mu;
    }

    public void SetMoonInfo(double mu, double r)
    {
        moonMu = mu;
        rMoon = r;
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

    public double GetGamma0()
    {
        return gamma0;
    }

    public double GetGamma1()
    {
        return gamma1;
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
        return tE;
    }

    public double GetTimeSOItoPerilune()
    {
        return tM;
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
    /// Filho & Fernandes June 2015, Computational and Applied Mathematics
    /// (which closely follows Bate, Meuller & White "Fundamentals of Astrodynamics") 
    /// 
    /// </summary>
    ///
    public double ComputePerilune(double v0)
    {
        // map vars onto those used in the paper for transparency
        double D = rMoon;
        double energy = 0.5 * v0 * v0 - earthMu / r0;
        double h = r0 * v0 * Math.Cos(phi0);
        double r2 = soiRadius;
        // eqn (7.4-4)
        r1 = Mathd.Sqrt(D * D + r2 * r2 - 2 * D * r2 * Math.Cos(lambda1));
        DebugLog("energy={0} r0={1} D={2} r2={3} lambda1={4} h={5}", energy, r0, D, r2, lambda1, h);
        gamma1 = Math.Asin(Math.Sin(lambda1) * r2 / r1);
        v1 = Math.Sqrt(2 * (energy + earthMu / r1));
        phi1 = Math.Acos(h / (r1 * v1));
        DebugLog("r1={0} v1={1} gamma1={2} phi1={3}", r1, v1, gamma1, phi1);
        // eqn (7.4-19)
        v2 = Math.Sqrt(v1 * v1 + vM * vM - 2 * v1 * vM * Math.Cos(phi1 - gamma1));

        // eqn (10) take plus sign for clockwise arrival
        // phi2 is the flight path angle of orbit in moon sphere
        // (derivation here differs from that in BM&W)
        phi2 = Math.Atan2(-v1 * Math.Sin(phi1 - gamma1), (vM - v1 * Math.Cos(phi1 - gamma1))) - lambda1;
        double Q2 = r2 * v2 * v2 / moonMu;
        double af = r2 / (2 - Q2);
        double cosphi2 = Math.Cos(phi2);
        double ef = Math.Sqrt(1 + Q2 * (Q2 - 2) * cosphi2 * cosphi2);
        //// find moon perigee and velocity

        double epsilon2 = Math.Asin(vM / v2 * Math.Cos(lambda1) - v1 / v2 * Math.Cos(lambda1 + gamma1 - phi1));
        DebugLog("Epsilon2={0} (deg)", epsilon2 * GEMath.RAD2DEG);

        // determine the angle by which need to "lead the moon" gamma0 and time of flight
        // follow F&F
        double F2 = GEMath.Acosh((1 / ef) * (1 - r2 / af));
        tM = Math.Sqrt(-af * af * af / moonMu) * (ef * Math.Sinh(F2) - F2);

        // BM&W (added as a sanity check)
        double p = h * h / earthMu;
        double a = -0.5 * earthMu / energy;
        double e = Math.Sqrt(1 - p / a);
        double cos_nu0 = GEMath.Clamp((p - r0) / (r0 * e), -1.0, 1.0);
        double nu0 = Math.Acos(cos_nu0);
        // Need to be careful with nu0 sign! (This cost me a week!)
        if (phi0 < 0)
            nu0 *= -1;

        double cos_nu1 = GEMath.Clamp((p - r1) / (r1 * e), -1.0, 1.0);
        double nu1 = Math.Acos(cos_nu1);
        DebugLog("Transfer Orbit: p={0} a={1} e={2} nu0={3} nu1={4}", p, a, e, nu0, nu1);
        double E0 = Math.Acos((e + cos_nu0) / (1 + e * cos_nu0));
        double E1_ = Math.Acos((e + cos_nu1) / (1 + e * cos_nu1));
        tE = Math.Sqrt(a * a * a / earthMu) * ((E1_ - e * Math.Sin(E1_))) - (E0 - e * Math.Sin(E0));
        gamma0 = nu1 - nu0 - gamma1 - omegaM * tE;
        DebugLog("nu1={0} nu0={1} gamma1={2} omega*tE={3} gamma0={4} tE={5}",
            nu1, nu0, gamma1, omegaM * tE, gamma0, tE);

        // Compute r_peri via BM&W
        double energyM = 0.5 * v2 * v2 - moonMu / r2;
        double hMoon = r2 * v2 * Math.Sin(epsilon2);
        double pMoon = hMoon * hMoon / moonMu;
        double eMoon = Math.Sqrt(1 + 2.0 * energyM * hMoon * hMoon / (moonMu * moonMu));
        double rp_moon = pMoon / (1 + eMoon);
        DebugLog("BMW r_p={0}", rp_moon);

        return rp_moon;
    }

    private void DebugLog(string format, params object[] names)
    {
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG)
            Debug.LogFormat(format, names);
#pragma warning restore 162
    }
}
