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
/// </summary>
public class ComputeTEIController : MonoBehaviour
{

    [Header("Nbody objects")]
    public NBody ship;
    public NBody earth;
    public NBody moon;

    [Header("Desired Perigee")]
    public double targetPerigeeKM;
    private double targetPerigee;


    // flight angle in radians
    [Header("Starting Input Conditions")]
    public double phi0Degrees = 0.0;

    public double lambda1Deg = 20.0;
    public bool secondRoot;

    [Header("Visualization")]
    public OrbitPredictor shipMoonOrbitPredictor;
    public OrbitPredictor shipEarthReturnPredictor;

    public Camera planningCamera;

    public LinePlotBasic linePlot;

    private double moonOrbitRadius;

    private double shipOrbitRadius;
    private double shipVmoon; 

    private double soiRadius; 

    private double moonMu;
    private double earthMu;
    private GravityEngine ge;

    private double v0min;
    private double v0Actual;

    private Vector3d r0_vec;
    private Vector3d v0_vec;

    private double unityToKm;
    private double vToMperS;
    private string velUnits = "(m/s)";
    private string lenUnits = "(km)";
    private string tofUnits = "(dd:hh:mm:ss)";

    private OrbitUniversal moonOrbit;
    private Vector3d moonOrbitAxis;
    private ComputeTEI computeTEI;

    private bool plotted;

    private static bool DEBUG = false;

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

        shipOrbitRadius = (ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(moon)).magnitude;
        shipVmoon = Math.Sqrt(moonMu / shipOrbitRadius);

        shipMoonOrbitPredictor.hyperDisplayRadius = (float) soiRadius;

        targetPerigee = targetPerigeeKM * ge.lengthScale;
        // determine the minimum orbit required to leave moon SOI
        double a = soiRadius;
        v0min = Math.Sqrt(2.0 * moonMu / shipOrbitRadius - moonMu / a);

        unityToKm = 1.0 / ge.lengthScale;
        vToMperS = GravityScaler.KM_HOUR_TO_M_SEC / GravityScaler.GetVelocityScale() ; // km/s to m/s

        computeTEI = new ComputeTEI();
        computeTEI.SetEarthInfo(earthMu);
        computeTEI.SetMoonInfo(moonMu, moonOrbitRadius, shipOrbitRadius);
        computeTEI.ParamsChanged();

        // screen the orbits to make sure they fit the asumptions for the patched conic calc
        // ship is rotating opposite to moon
        OrbitUniversal shipOrbit = ship.GetComponent<OrbitUniversal>();
        if (Math.Abs(Vector3d.Angle(shipOrbit.GetAxis(), moonOrbit.GetAxis()) - 180.0) > 0.01) {
            Debug.LogErrorFormat("Need ship orbit and moon orbit to be same inclination/opposite dir. angle={0} {1}<->{2}",
                Vector3d.Angle(shipOrbit.GetAxis(), moonOrbit.GetAxis()),
                shipOrbit.GetAxis(),
                moonOrbit.GetAxis());
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
            tofUnits = "";
            vToMperS = 1.0;
        }

    }

    public (double, double) GetTEIAnglesPhi0Theta()
    {
        return (phi0Degrees, computeTEI.GetTheta() );
    }

    public double GetV0Magnitude()
    {
        return v0Actual;
    }

    public double GetSoiRadius(){
        return soiRadius;
    }

    public ComputeTEI GetComputeTEI()
    {
        return computeTEI;
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
            lambda1Deg = Math.Max(0, lambda1Deg + 0.1);
            ComputeAndPlot();
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            lambda1Deg = Math.Min(90.0, lambda1Deg - 0.1);
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

    private void ComputeAndPlot()
    {

        computeTEI.SetTransferParams(lambda1Deg, phi0Degrees);
        double r_min = computeTEI.ComputePerigee(v0min);
        DebugLog("Min velocity for lambda1={0} v0={1} => perigee={2} {3}", lambda1Deg, v0min, r_min * unityToKm, lenUnits);
        int plotCount = 100;
        double[] v = new double[plotCount];
        double[] rp = new double[plotCount];
        // go to some % more of v0min
        double dv0 = v0min * 1.04 / plotCount;
        double minRp = double.MaxValue;
        for (int i = 0; i < plotCount; i++)
        {
            v[i] = v0min + i * dv0;
            rp[i] = computeTEI.ComputePerigee(v[i]) * unityToKm;
            minRp = Math.Min(rp[i], minRp);
        }
        // create a horizontal line to show target perigee on plot
        double[] x_target = new double[2];
        double[] y_target = new double[2];
        x_target[0] = v[0];
        x_target[1] = v[plotCount - 1];
        y_target[0] = targetPerigeeKM;
        y_target[1] = targetPerigeeKM;


        string report = string.Format("Lambda = {0} (deg.)\nFlight Path Angle = {1} (deg.)\nMin Perigee = {2:0.000} {3}",
            lambda1Deg, phi0Degrees, minRp, lenUnits);
        report += string.Format("\nTarget perigee = {0} {1}\n", targetPerigeeKM, lenUnits);
        // Determine if the target periline is acheivable. If so, find the v0 for this and determine the rest of the trajectory
        if (targetPerigeeKM > minRp) {
            double v0Actual;
            if (secondRoot) {
                // interpolate to find the required v0
                // Greedy: walk the list until we've gone past the target and do simple interpolation
                int i = v.Length - 1;
                if (targetPerigeeKM < rp[i]) {
                    while (rp[i] > targetPerigeeKM) i--;
                }
                else {
                    Debug.LogWarning("Unexpected case");
                }
                // linear interpolation y = mx+b
                double dr = rp[i + 1] - rp[i];
                double dv = v[i + 1] - v[i];
                double m = dr / dv;
                double b = 0.5 * ((rp[i] + rp[i + 1]) - m * (v[i + 1] + v[i]));
                double x = (targetPerigeeKM - b) / m;
                v0Actual = x;
            }
            else {
                // interpolate to find the required v0
                // Greedy: walk the list until we've gone past the target and do simple interpolation
                int i = 0;
                if (targetPerigeeKM < rp[0]) {
                    while (rp[i] > targetPerigeeKM) i++;
                }
                else {
                    while (rp[i] < targetPerigeeKM) i++;
                }
                // linear interpolation y = mx+b
                double dr = rp[i] - rp[i - 1];
                double dv = v[i] - v[i - 1];
                double m = dr / dv;
                double b = 0.5 * ((rp[i] + rp[i - 1]) - m * (v[i - 1] + v[i]));
                double x = (targetPerigeeKM - b) / m;
                v0Actual = x;
            }
            report += string.Format("\nV0={0:0.000} {1} to reach target perigee", v0Actual * vToMperS, velUnits);
            double rp_actual = computeTEI.ComputePerigee(v0Actual);
            report += string.Format("\nTOF={0} {1}", GravityScaler.GetWorldTimeFormatted(computeTEI.GetTOF(), ge.units), tofUnits);
            DebugLog("Expected rp={0}", rp_actual);
            // Determine dV at Earth and Moon
            double dVMoon = Math.Sqrt(shipVmoon*shipVmoon +
                v0Actual*v0Actual - 2*v0Actual*shipVmoon*Math.Cos(phi0Degrees * GEMath.DEG2RAD));
            report += string.Format("\ndV={0:0.000} {1}", dVMoon * vToMperS, velUnits);
            report += string.Format("\nGeometry: theta={0:0.00} (deg)", computeTEI.GetTheta() * GEMath.RAD2DEG );
            double returnPerigee = UpdateOrbitPredictions(v0Actual);
            report += string.Format("\nFree return Earth perigee={0:0.0} {1}", returnPerigee * unityToKm, lenUnits);

        } else {
            report += "\n\nCannot attain requested perigee";
            shipMoonOrbitPredictor.gameObject.SetActive(false);
            shipEarthReturnPredictor.gameObject.SetActive(false);
        }

        linePlot.report = report;
        linePlot.PlotData(v, rp, x_target, y_target);
    }

    /// <summary>
    /// Show the orbit that results from the chosen v0 value for TEI.
    ///
    /// Plot is setup to show the existing moon at phase=0 at the time TEI burn is performed.
    /// - requires gamma0 (angle from earth-moon axis at time of TLI)
    /// - requires moon position at TLI burn (needs time of travel from burn point to SOI)
    ///
    /// Moon trajectory:
    /// - locate ship at SOI entry point based on the lambda1 value (trig.)
    /// - use magnitude and orientation of v2 to determine moon orbit
    /// </summary>
    ///

    private double UpdateOrbitPredictions(double v0tli)
    {
        // Setup the outbound hyperbola up to the SOI
        double theta = computeTEI.GetTheta();
        double r0 = shipOrbitRadius;
        // use flight path angle to set direction of velocity
        double phi0 = phi0Degrees * GEMath.DEG2RAD;

        Vector3d moonPos = ge.GetPositionDoubleV3(moon);
        Vector3d moonVel = ge.GetVelocityDoubleV3(moon);
        // Super-awkward, but V from mu hack may not have been updated in GE
        moonVel = moonVel.normalized * computeTEI.GetMoonVel();

        // Initial version took r0 along x-axis and v0 along y-axis and rotated around Z based on moon
        // at SOI exitk being located on x-axis.
        double moonPhaseRad = moonOrbit.GetCurrentPhase() * GEMath.DEG2RAD;
        double tSoiExit = computeTEI.GetTimeToSOI();
        double rAngle = 1.0 * (theta - moonPhaseRad );
        double vAngle = 1.0 * (theta + phi0 - moonPhaseRad );

        if (ge.xzOrbits) {
            r0_vec = new Vector3d(r0, 0, 0);
            v0_vec = new Vector3d(0, 0, -v0tli);
            // apply inclination. 
            r0_vec = GEMath.Rot1(r0_vec, moonOrbit.inclination * GEMath.DEG2RAD);
            v0_vec = GEMath.Rot1(v0_vec, moonOrbit.inclination * GEMath.DEG2RAD);

            r0_vec = Vector3d.RotateAxisAngleRadians(r0_vec, moonOrbitAxis, rAngle);
            v0_vec = Vector3d.RotateAxisAngleRadians(v0_vec, moonOrbitAxis, vAngle);
        }
        else {
            r0_vec = new Vector3d(r0, 0, 0);
            v0_vec = new Vector3d(0, -v0tli, 0);
            // apply inclination. 
            r0_vec = GEMath.Rot1(r0_vec, -1.0 * moonOrbit.inclination * GEMath.DEG2RAD);
            v0_vec = GEMath.Rot1(v0_vec, -1.0 * moonOrbit.inclination * GEMath.DEG2RAD);

            r0_vec = Vector3d.RotateAxisAngleRadians(r0_vec, moonOrbitAxis, rAngle);
            v0_vec = Vector3d.RotateAxisAngleRadians(v0_vec, moonOrbitAxis, vAngle);
        }
        shipMoonOrbitPredictor.gameObject.SetActive(true);
        // OP needs absolute R, V
        shipMoonOrbitPredictor.SetPosition(r0_vec + moonPos);
        shipMoonOrbitPredictor.SetVelocity(v0_vec + moonVel);
        shipMoonOrbitPredictor.UpdateOrbitU();

        // SOI Exit and Earth return
        // A bit cheezy, but take r0, v0 and prop to SOI to get v1_vec
        // Can use r1, v1 directly since OU init is relativePos: false
        OrbitPropagator propToSoi = new OrbitPropagator(r0_vec, v0_vec, 0.0, moonMu);
        (Vector3d r2_vec, Vector3d v2_vec) = propToSoi.PropagateToTime(tSoiExit);
        if (r2_vec.IsNan() || v2_vec.IsNan()) {
            shipMoonOrbitPredictor.gameObject.SetActive(false);
            return double.NaN;
        }

        // DEBUG
        Debug.LogFormat("|R0|={0} |V0|={1}", r0_vec.magnitude, v0_vec.magnitude);
        Debug.LogFormat("R at SOI exit={0}", r2_vec.magnitude);
        Vector3d r1_vec = r2_vec + moonPos;
        Vector3d v1_vec = v2_vec + moonVel;
        Debug.LogFormat("Predict: |V1| = {0} |V2|={1} |VM|={2} Angle(v2,vMoon)={3} Angle(r2, moon)={4}",
            v1_vec.magnitude,
            v2_vec.magnitude,
            moonVel.magnitude,
            Vector3d.Angle(v2_vec, moonVel),
            Vector3d.Angle(r2_vec, moonPos));
        // Fill in Earth return
        shipEarthReturnPredictor.gameObject.SetActive(true);
        shipEarthReturnPredictor.SetPosition(r1_vec);
        shipEarthReturnPredictor.SetVelocity(v1_vec);
        shipEarthReturnPredictor.UpdateOrbitU();

        double earthReturnPerigee = shipEarthReturnPredictor.GetOrbitUniversal().GetPerigee();
        return earthReturnPerigee;
    }


    public (Vector3d, Vector3d) GetR0V0()
    {
        return (r0_vec, v0_vec);
    }

    public double GetMoonShift()
    {
        return computeTEI.GetOmegaMoon() * computeTEI.GetTimeToSOI();
    }

    public void StopPlot()
    {
        // shipEarthOutboundPredictor.gameObject.SetActive(false);
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
