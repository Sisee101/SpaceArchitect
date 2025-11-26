
using UnityEngine;
using NUnit.Framework;


public class PatchedConicTLI 
{
    /// <summary>
	/// Test using the worked example in Bate, Meuller and White p341
	/// Units are a bit weird.
	/// 
	/// </summary>
	///
    // Determin DU, TU from solution in the book. 
    // DU distance units (earth radii km)
    static double DU = 6378.145;
    // TU time units (sec)
    static double TU = 806.8;
    double VU = 7.905368;
    // units chosen so Earth has mass 1
    static double muM = 1/81.3; 
    static double muE = 1.0/(1+muM);

    // Check mass scale
    // G = 6.67430E-11 m^3/s^2
    // Convert distance scale: 1 DU = 6,377,965.8 m
    //                         1 TU = 807.2113 sec
    // Gdutu = G (TU)^2/(DU)^3 = 1.6762E-24
    // Gdutu * Emass =
    
    [Test]
    public void ComputeG()
    {
        double DU_m = DU * 1000;
        double G = 6.67430e-11 * TU * TU / (DU_m * DU_m * DU_m);
        Debug.LogFormat("G={0} GM={1}, 1/G={2}", G, G * 5.972e24, 1.0/G);
        Debug.LogFormat("uE=1.0/1+uM = {0}", 1.0 / (1.0 + muM));
    }

    [Test]
    public void BateExample()
    {
        ComputeTLI computeTLI = new ComputeTLI();
        computeTLI.SetEarthInfo(muE, 1.05);
        computeTLI.SetMoonInfo(muM, 60.27);
        computeTLI.SetTransferParams(30.0, 0.0);
        computeTLI.ParamsChanged();
        double v0 = 1.372;
        double rp = computeTLI.ComputePerilune(v0);
        Debug.LogFormat("expected v1 = {0}", 0.1296 );
        Debug.LogFormat("rp={0} DU rp={1} km v1={2} v1={3} (km/sec) v2={4} (km/sec)",
            rp,
            rp*DU,
            computeTLI.GetV1(),
            computeTLI.GetV1() * DU / TU,
            computeTLI.GetV2()*DU/TU);
        Debug.LogFormat("phi1={0} (deg) gamma1={1} (deg)",
            computeTLI.GetPhi1() * GEMath.RAD2DEG,
            computeTLI.GetGamma1() * GEMath.RAD2DEG);
        // weird that the phase from BM&W is 135 and we get 160. Seems like a big delta given other factors.
        Debug.LogFormat("gamma0 = {0} (deg), tE={1}", computeTLI.GetGamma0() * GEMath.RAD2DEG, computeTLI.GetTimeToSOI());
    }

    /// <summary>
    /// Patched conic a la Bates with details from
    /// http://www.vxphysics.com/Space%20Program/Analysis%20&%20Simulation%20of%20the%20Trajectory%20of%20the%20Apollo%2011%20Flight%20to%20Moon.pdf?utm_source=pocket_mylist
    /// </summary>
    [Test]
    public void Apollo11()
    {
        double r0 = 1.0 + 334.0/DU;
        double lambda1Deg = 30.0;
        double phi0Deg = 0.0;
        ComputeTLI computeTLI = new ComputeTLI();
        Debug.Log("mu using reverse engineered value");
        computeTLI.SetEarthInfo(0.9576, r0);
        computeTLI.SetMoonInfo(muM, 60.268);
        computeTLI.SetTransferParams(lambda1Deg, phi0Deg);
        computeTLI.ParamsChanged();
        double v0 = 10.6/VU; // km/sec / VU
        double rp = computeTLI.ComputePerilune(v0);
        Debug.LogFormat("expected v1 = {0}", 0.1296);
        Debug.LogFormat("rp={0} DU rp={1} km v1={2} v1={3} (km/sec) v2={4} (km/sec)",
            rp,
            rp * DU,
            computeTLI.GetV1(),
            computeTLI.GetV1() * DU / TU,
            computeTLI.GetV2() * DU / TU);
        Debug.LogFormat("phi1={0} (deg) gamma1={1} (deg)",
            computeTLI.GetPhi1() * GEMath.RAD2DEG,
            computeTLI.GetGamma1() * GEMath.RAD2DEG);
        // weird that the phase from BM&W is 135 and we get 160. Seems like a big delta given other factors.
        Debug.LogFormat("gamma0 = {0} (deg), tE={1}", computeTLI.GetGamma0() * GEMath.RAD2DEG, computeTLI.GetTimeToSOI());


    }
}
