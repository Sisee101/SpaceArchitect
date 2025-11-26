using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Controller for illustrating options for a TLI burn.
///
/// UI:
/// I/K: Increase/decrease the angle of SOI entry (lambda1) measured from the Earth-Moon line
/// W/S: Increase/decrease the flight path angle (angle away from tangent to the circular orbit)
///
/// R: Toggle which root to use (first or second)
/// </summary>
public class ComputeTLIController : MonoBehaviour
{
    [Header("Nbody objects")]
    public NBody ship;
    public NBody earth;
    public NBody moon;

    [Header("Desired Perilune")]
    public double targetPeriluneKm;
    private double targetPerilune;

    // flight angle in radians
    [Header("Starting Input Conditions")]
    public double phi0Degrees = 0.0;

    public double lambda1Deg = 20.0;
    // The solution curve usually has two roots that meet target. Designate which one is selected. Secons is often better
    // for a reasonable free return
    public bool secondRoot = true;

    [Header("Visualization")]
    public OrbitPredictor shipMoonOrbitPredictor;
    public OrbitPredictor shipEarthOutboundPredictor;
    public OrbitPredictor shipEarthReturnPredictor;

    // here so that once a maneuver is programmed we can turn off this camera
    public Camera planningCamera;
    // Component to plot v0 vs perilune
    public LinePlotBasic linePlot;

    private double moonOrbitRadius;

    private double shipOrbitRadius;
    private double shipVEarth; 

    private double soiRadius; 

    private double moonMu;
    private double earthMu;
    private GravityEngine ge;

    private double v0min;
    private double v0Actual;
    private double moonLeadAngle;

    double tliAngle;
    private Vector3d r0_vec;
    private Vector3d v0_vec;

    private double unityToKm;
    private double vToMperS;
    private string velUnits = "(m/s)";
    private string lenUnits = "(km)";
    private string tofUnits = "(dd:hh:mm:ss)";

    private ComputeTLI computeTLI;

    private bool plotted;


    private static bool DEBUG = false;

    private OrbitUniversal moonOrbit;
    private Vector3d moonOrbitAxis; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        ge.AddGEStartCallback(GEStartCallback);
    }

    private void GEStartCallback()
    {
        // IMPORTANT!!!
        // The calculations in ComputeTLI use the Earth mass for the orbital velocity of the moon.
        // By default GE will use (mEarth + mMoon) for the moon and this small difference in mass will have a
        // significant effect on the accuracy of the trajectories.
        // The "fix" is to adjust the mu in the Moon's orbit universal to be just m_Earth
        moonOrbit = moon.GetComponent<OrbitUniversal>();
        moonOrbit.SetMu(ge.GetMass(earth));
        moonOrbit.Init();
        moonOrbitAxis = moonOrbit.GetAxis();
        Debug.LogWarning("Note: Manually adjusting moon center mass to be mEarth and not (mEarth+mMoon)");

        moonOrbitRadius = (ge.GetPositionDoubleV3(earth) - ge.GetPositionDoubleV3(moon)).magnitude;
        moonMu = ge.GetMass(moon);
        earthMu = ge.GetMass(earth);
        soiRadius = OrbitUtils.SoiRadius(earth, moon);

        shipOrbitRadius = (ge.GetPositionDoubleV3(earth) - ge.GetPositionDoubleV3(ship)).magnitude;
        shipVEarth = Math.Sqrt(earthMu / shipOrbitRadius);

        shipMoonOrbitPredictor.hyperDisplayRadius = (float) soiRadius;

        targetPerilune = targetPeriluneKm * ge.lengthScale;
        // determine the minimum orbit required to reach the moon, v0 must be at least this
        // (Bates et al. p330) Orbit with perigee=r0 and apogee = moon radius and 0 flight path angle
        double a = 0.5 * (shipOrbitRadius + moonOrbitRadius);
        v0min = Math.Sqrt(2.0 * earthMu / shipOrbitRadius - earthMu / a);

        unityToKm = 1.0 / ge.lengthScale;
        vToMperS = GravityScaler.KM_HOUR_TO_M_SEC / GravityScaler.GetVelocityScale(); // km/s to m/s

        computeTLI = new ComputeTLI();
        computeTLI.SetEarthInfo(earthMu, shipOrbitRadius);
        computeTLI.SetMoonInfo(moonMu, moonOrbitRadius);
        computeTLI.ParamsChanged();

        // screen the orbits to make sure they fit the asumptions for the patched conic calc
        OrbitUniversal shipOrbit = ship.GetComponent<OrbitUniversal>();
        // ship will be orbiting CW (so i=180)
        if (Math.Abs(shipOrbit.inclination - moonOrbit.inclination) > 0.01) {
            Debug.LogError("Need ship orbit and moon orbit to be same inclination");
        }
        if (moonOrbit.eccentricity > 0.01) {
            Debug.LogError("Need moon orbit to be circular");
        }
        if (shipOrbit.eccentricity > 0.01) {
            Debug.LogError("Need ship orbit to be circular");
        }
        // currently support DL and ORBITAL
        if (ge.units != GravityScaler.Units.ORBITAL) {
            velUnits = "";
            lenUnits = "";
            vToMperS = 1.0;
            tofUnits = "";
        }

 
    }

    public (double, double) GetTLIAnglesPhi0Gamma0()
    {
        return (phi0Degrees, computeTLI.GetGamma0() * GEMath.RAD2DEG);
    }

    public double GetV0Magnitude()
    {
        return v0Actual;
    }

    public double GetSoiRadius(){
        return soiRadius;
    }

    public double GetMoonLeadAngle()
    {
        return moonLeadAngle;
    }

    public ComputeTLI GetComputeTLI()
    {
        return computeTLI;
    }

    // Update is called once per frame
    void Update()
    {
        if (ge.IsSetup() && !plotted)
        {
            ge.SetEvolve(false);
            // compute the required path
            ComputeAndPlot();
            plotted = true;
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            lambda1Deg = Math.Max(0, lambda1Deg + 1.0);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            lambda1Deg = Math.Min(90.0, lambda1Deg - 1.0);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            lambda1Deg = Math.Max(0, lambda1Deg + 0.01);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            lambda1Deg = Math.Min(90.0, lambda1Deg - 0.01);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.W)) {
            phi0Degrees = Math.Max(0, phi0Degrees + 1);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            phi0Degrees = Math.Min(90.0, phi0Degrees - 1);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            secondRoot = !secondRoot;
            ComputeAndPlot();
        }
    }

    /// <summary>
    /// Using ComputeTLI create a plot of perilune vs v0 for a range of v0, then find the points where the
    /// target perilune is acheived (typically there are two and the second one has better return to earth
    /// trajectories).
    ///
    /// With the chosen v0, update the display to show the path taken.
    /// </summary>
    private void ComputeAndPlot()
    {

        computeTLI.SetTransferParams(lambda1Deg, phi0Degrees);
        double r_min = computeTLI.ComputePerilune(v0min);
        DebugLog("Min velocity for lambda1={0} v0={1} => r_perilune={2} (km)", lambda1Deg, v0min, r_min * unityToKm);
        int plotCount = 100;
        double[] v = new double[plotCount];
        double[] rp = new double[plotCount];
        // go to 2.5% more of v0min
        double dv0 = v0min * 0.01 / plotCount;
        double minRp = double.MaxValue;
        for (int i = 0; i < plotCount; i++)
        {
            v[i] = v0min + i * dv0;
            rp[i] = computeTLI.ComputePerilune(v[i]) * unityToKm;
            minRp = Math.Min(rp[i], minRp);
        }
        // create a horizontal line to show target perigee on plot
        double[] x_target = new double[2];
        double[] y_target = new double[2];
        x_target[0] = v[0];
        x_target[1] = v[plotCount - 1];
        y_target[0] = targetPeriluneKm;
        y_target[1] = targetPeriluneKm;


        string report = string.Format("Lambda = {0} (deg.)\nFlight Path Angle = {1} (deg.)\nMin Perilune = {2:0.000} {3}",
            lambda1Deg, phi0Degrees, minRp, lenUnits);
        report += string.Format("\nTarget perilune = {0} {1}\n", targetPeriluneKm, lenUnits);
        // Determine if the target periline is acheivable. If so, find the v0 for this and determine the rest of the trajectory
        if (targetPeriluneKm > minRp) {
            double v0Actual;
            if (secondRoot) {
                // interpolate to find the required v0
                // Greedy: walk the list until we've gone past the target and do simple interpolation
                int i = v.Length-1;
                if (targetPeriluneKm < rp[i]) {
                    while (rp[i] > targetPeriluneKm) i--;
                }
                else {
                    Debug.LogWarning("Unexpected case");
                }
                // linear interpolation y = mx+b
                double dr = rp[i + 1] - rp[i];
                double dv = v[i + 1] - v[i];
                double m = dr / dv;
                double b = 0.5 * ((rp[i] + rp[i + 1]) - m * (v[i + 1] + v[i]));
                double x = (targetPeriluneKm - b) / m;
                v0Actual = x;
            } else {
                // interpolate to find the required v0
                // Greedy: walk the list until we've gone past the target and do simple interpolation
                int i = 0;
                if (targetPeriluneKm < rp[0]) {
                    while (rp[i] > targetPeriluneKm) i++;
                }
                else {
                    while (rp[i] < targetPeriluneKm) i++;
                }
                // linear interpolation y = mx+b
                double dr = rp[i] - rp[i - 1];
                double dv = v[i] - v[i-1];
                double m = dr / dv;
                double b = 0.5 * ((rp[i] + rp[i - 1]) - m * (v[i - 1] + v[i]));
                double x = (targetPeriluneKm - b) / m;
                v0Actual = x;
            }
 

            report += string.Format("\nV0={0:0.00} {1} to reach target perilune", v0Actual * vToMperS, velUnits);
            double rp_actual = computeTLI.ComputePerilune(v0Actual);
            report += string.Format("\nTOF={0} {1}", GravityScaler.GetWorldTimeFormatted(computeTLI.GetTOF(), ge.units), tofUnits);
            DebugLog("Expected rp={0}", rp_actual);
            // Determine dV at Earth and Moon
            double dVEarth = Math.Sqrt(shipVEarth*shipVEarth + v0Actual*v0Actual - 2*v0Actual*shipVEarth*Math.Cos(phi0Degrees * GEMath.DEG2RAD));
            double dVMoon = computeTLI.GetVPerilune() - Math.Sqrt(moonMu / rp_actual);
            report += string.Format("\ndV={0:0.00} {3}\n  (dv_Earth={1:0.00} dv_moon={2:0.00}) {3}",
                (dVEarth + Math.Abs(dVMoon))*vToMperS,
                dVEarth * vToMperS,
                dVMoon * vToMperS,
                velUnits);
            report += string.Format("\nGeometry: gamma0={0:0.00} (deg.) gamma1={1:0.00} (deg",
                computeTLI.GetGamma0() * GEMath.RAD2DEG,
                computeTLI.GetGamma1() * GEMath.RAD2DEG);
            double returnPerigee = UpdateOrbitPredictions(v0Actual);
            report += string.Format("\nFree return Earth perigee={0:0.0} {1}", returnPerigee * unityToKm, lenUnits);

        } else {
            report += "\n\nCannot attain requested perilune";
            shipMoonOrbitPredictor.gameObject.SetActive(false);
            shipEarthOutboundPredictor.gameObject.SetActive(false);
            shipEarthReturnPredictor.gameObject.SetActive(false);
        }

        linePlot.report = report;
        linePlot.PlotData(v, rp, x_target, y_target);
    }

    /// <summary>
    /// Show the orbit that results from the chosen v0 value for TLI.
    ///
    /// Plot is setup to show the existing moon at phase=0 at the time SOI is entered. 
    /// - requires gamma0 (angle from earth-moon axis at time of TLI)
    /// - requires moon position at TLI burn (needs time of travel from burn point to SOI)
    ///
    /// Moon trajectory:
    /// - locate ship at SOI entry point based on the lambda1 value (trig.)
    /// - use magnitude and orientation of v2 to determine moon orbit
    ///
    /// The scene may have an inclination for the moon and ship, but these orbits must be coplanar
    /// (the setup above checks for this).
    ///
    /// Everything done to this point did not care about inclination. Here we need to adapt to the
    /// actual position of the moon and the inlination of the system.
    /// </summary>
    ///

    private OrbitPropagator soiShipProp;
    private double soiDistance;
    private double tSoiExit;

    private double UpdateOrbitPredictions(double v0tli)
    {
        // Earth predictor: Need gamma1 and time to SOI
        moonLeadAngle = computeTLI.GetTimeToSOI() * computeTLI.GetOmegaMoon(); 
        double gamma0 = computeTLI.GetGamma0();
        double r0 = shipOrbitRadius;
        // use flight path angle to set direction of velocity
        double phi0 = phi0Degrees * GEMath.DEG2RAD;

        // Initial version took r0 along x-axis and v0 along y-axis and rotated around Z based on moon
        // at SOI entry being located on x-axis.
        Vector3d moonNowLine = (ge.GetPositionDoubleV3(moon) - ge.GetPositionDoubleV3(earth)).normalized;
        double moonPhaseRad = moonOrbit.GetCurrentPhase() * GEMath.DEG2RAD;

        double rAngle = -1.0 *(moonLeadAngle + gamma0 - moonPhaseRad);
        double vAngle = -1.0 * (moonLeadAngle + gamma0 + phi0 - moonPhaseRad);

        if (ge.xzOrbits) {
            r0_vec = new Vector3d(r0, 0, 0);
            v0_vec = new Vector3d(0, 0, v0tli);
            // apply inclination. 
            r0_vec = GEMath.Rot1(r0_vec, moonOrbit.inclination * GEMath.DEG2RAD);
            v0_vec = GEMath.Rot1(v0_vec, moonOrbit.inclination * GEMath.DEG2RAD);

            r0_vec = Vector3d.RotateAxisAngleRadians(r0_vec, moonOrbitAxis, rAngle);
            v0_vec = Vector3d.RotateAxisAngleRadians(v0_vec, moonOrbitAxis, vAngle);
        }
        else {
            r0_vec = new Vector3d(r0, 0, 0);
            v0_vec = new Vector3d(0, v0tli, 0);
            // apply inclination. 
            r0_vec = GEMath.Rot1(r0_vec, -1.0 * moonOrbit.inclination * GEMath.DEG2RAD);
            v0_vec = GEMath.Rot1(v0_vec, -1.0 * moonOrbit.inclination * GEMath.DEG2RAD);

            r0_vec = Vector3d.RotateAxisAngleRadians(r0_vec, moonOrbitAxis, rAngle);
            v0_vec = Vector3d.RotateAxisAngleRadians(v0_vec, moonOrbitAxis, vAngle);
        }
        Vector3d xAxis = new Vector3d(1, 0, 0);
        Vector3d zAxis = new Vector3d(0, 0, 1);
        DebugLog("r0={0} v0={1} angle r0={2} (rad) angle v0={3} angle(r,v)={4} (deg)", r0_vec, v0_vec,
            Vector3d.AngleRadians(xAxis, r0_vec), Vector3d.AngleRadians(zAxis, v0_vec),
            Vector3d.Angle(r0_vec, v0_vec));
        DebugLog("moonLead+gamma0={0} mL+g+phi0={1} moonAxis={2}",
            moonLeadAngle + gamma0, moonLeadAngle + gamma0 + phi0, moonOrbitAxis);

        shipEarthOutboundPredictor.gameObject.SetActive(true);
        shipEarthOutboundPredictor.SetPosition(r0_vec);
        shipEarthOutboundPredictor.SetVelocity(v0_vec);
        shipEarthOutboundPredictor.UpdateOrbitU();

        // find position of SOI entry: r1_vec
        // want global velocity (not relative) for the OP
        double gamma1 = computeTLI.GetGamma1();
        double r1 = computeTLI.GetR1();

        // A bit cheezy, but take r0, v0 and prop to SOI to get v1_vec
        // Can use r1, v1 directly since OU init is relativePos: false
        OrbitPropagator propToSoi = new OrbitPropagator(r0_vec, v0_vec, 0.0, earthMu);
        (Vector3d r1_vec, Vector3d v1_vec) = propToSoi.PropagateToTime(computeTLI.GetTimeToSOI());

        if (r1_vec.IsNan() || v1_vec.IsNan()) {
            shipMoonOrbitPredictor.gameObject.SetActive(false);
        }
        else {
            shipMoonOrbitPredictor.gameObject.SetActive(true);
            shipMoonOrbitPredictor.SetPosition(r1_vec);
            shipMoonOrbitPredictor.SetVelocity(v1_vec);
            shipMoonOrbitPredictor.hyperDisplayRadius = (float)computeTLI.GetSoiRadius();
            shipMoonOrbitPredictor.UpdateOrbitU();
        }
        DebugLog("Moon SOI entry: r1={0} v1={1} perilune={2}", r1_vec, v1_vec, 
                        shipMoonOrbitPredictor.GetOrbitUniversal().GetPerigee() );
        // <DEBUG>
        Vector3d r_ou = Vector3d.zero;
        Vector3d v_ou = Vector3d.zero;
        double t_ou = 0.0;
        shipMoonOrbitPredictor.GetOrbitUniversal().GetRVT(ref r_ou, ref v_ou, ref t_ou);
        Vector3d vMoon = ge.GetVelocityDoubleV3(moon);
        // Super-awkward, but V from mu hack may not have been updated in GE
        vMoon = vMoon.normalized * computeTLI.GetMoonVel();

        DebugLog("OU: r={0}, v={1} |v|={2} vMoon = {3} |vMoon|={4}", r_ou, v_ou, v_ou.magnitude,
            vMoon, vMoon.magnitude);
        // </DEBUG>

        // Return path if perilune burn fails
        // - get V2 in selenocentric space, rotate by deflection angle (2 nu, where nu is reused and is
        //   not orbital phase)
        // - account for moon motion during lunar transit (rotation of frame)
        // - place on the existing SOI/moon position for simplicity of view
        // - translate to Earth space and init an orbit predictor
        Vector3d v2Moon = v1_vec - vMoon;
        Vector3d moonPos = ge.GetPositionDoubleV3(moon);
        Vector3d r2Moon = r1_vec - moonPos;
        double tMoon = 2.0 * computeTLI.GetTimeSOItoPerilune();
        soiShipProp = new OrbitPropagator(r2Moon, v2Moon, 0.0, moonMu);

        // tMoon is not precise enough. Need to root find to get closer to SOI at exit point
        soiDistance = computeTLI.GetSoiRadius();
        tSoiExit = SecantRootFind.Secant(AtSoi, tMoon, tMoon + 1.0);
        (Vector3d rSOIexit, Vector3d vSOIexit) = soiShipProp.PropagateToTime(tSoiExit);


        DebugLog("soiDistance at SOI Entry={0} vExit={1} V2toMoon={2} R2toMoon={3} tMoon={4}",
            r2Moon.magnitude, v2Moon.magnitude,
            Vector3d.Angle(v2Moon, xAxis),
            Vector3d.Angle(r2Moon, xAxis),
            tMoon);
        DebugLog("soiDistance at SOI exit={0} vExit={1} VtoMoon={2} RtoMoon={3} rootFindDelta={4} soi={5}",
            rSOIexit.magnitude, vSOIexit.magnitude,
            Vector3d.Angle(vSOIexit, xAxis),
            Vector3d.Angle(rSOIexit, xAxis),
            tSoiExit - tMoon,
            soiDistance);


        // As ship transits moon, the moon moves which has the effect of changing exit point
        // with respect to the Earth-Moon line. Adjust for this by rotating CW
        double moonAngle = 1.0*computeTLI.GetOmegaMoon() * tSoiExit;
         if (ge.xzOrbits) {
            rSOIexit = Vector3d.RotateAxisAngleRadians(rSOIexit, -moonOrbitAxis, moonAngle);
            vSOIexit = Vector3d.RotateAxisAngleRadians(vSOIexit, -moonOrbitAxis, moonAngle);
        } else {
            rSOIexit = Vector3d.RotateAxisAngleRadians(rSOIexit, -moonOrbitAxis, moonAngle);
            vSOIexit = Vector3d.RotateAxisAngleRadians(vSOIexit, -moonOrbitAxis, moonAngle);
        }

        DebugLog("soiDistance at SOI exit={0} vExit={1} VtoMoon={2} RtoMoon={3} rootFindDelta={4} soi={5}",
            rSOIexit.magnitude, vSOIexit.magnitude,
            Vector3d.Angle(vSOIexit, xAxis),
            Vector3d.Angle(rSOIexit, xAxis),
            tSoiExit - tMoon,
            soiDistance);


        // Fill in Earth return
        shipEarthReturnPredictor.gameObject.SetActive(true);
        shipEarthReturnPredictor.SetPosition(rSOIexit + moonPos);
        shipEarthReturnPredictor.SetVelocity(vSOIexit + vMoon);
        shipEarthReturnPredictor.UpdateOrbitU();

        // report will want to know earth return perigee
        double earthReturnPerigee = shipEarthReturnPredictor.GetOrbitUniversal().GetPerigee();
        return earthReturnPerigee;
    }

    private double AtSoi(double time)
    {
        (Vector3d r, Vector3d v) = soiShipProp.PropagateToTime(time);
        return r.magnitude - soiDistance;
    }

    public double GetTliAngle()
    {
        return tliAngle;
    }

    public double GetTSoiExit()
    {
        return tSoiExit;
    }

    public (Vector3d, Vector3d) GetR0V0()
    {
        return (r0_vec, v0_vec);
    }

    public void StopPlot()
    {
        shipEarthOutboundPredictor.gameObject.SetActive(false);
        shipMoonOrbitPredictor.gameObject.SetActive(false);
        shipEarthReturnPredictor.gameObject.SetActive(false);
        planningCamera.gameObject.SetActive(false);
    }

    private void DebugLog(string format, params object[] names)
    {
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG)
            Debug.LogFormat(format, names);
#pragma warning restore 162
    }
}
