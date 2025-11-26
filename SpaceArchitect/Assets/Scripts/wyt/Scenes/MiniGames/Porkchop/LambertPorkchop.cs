using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compute the parameters for a Lambert transfer from one orbit to another over a variety of departure and 
/// arrival times for two bodies in orbit around a common center. 
/// 
/// Creates data sets for the values of dVdepart, dVarrival and their sum over the range of departure and arrival times
/// specified. These can then be used as data sources for the porchchop mesh to allow visualization.
/// </summary>
[System.Serializable]
public class LambertPorkchop : MonoBehaviour
{
    //! body from which porkchop departure orbit is taken. Must have an OrbitUniversal.
    public NBody fromNbody = null;

    //! body to which porkchop arrival orbit is taken. Must have an OrbitUniversal.
    public NBody toNBody = null;

    public bool plotOnStart = true;

    //! Plot bounds can be in absolute times or relative to the orbit period of the fromNBody
    public enum InputMode { RELATIVE, ABSOLUTE };

    public InputMode inputMode = InputMode.ABSOLUTE;

    // absolute info
    public double departureStart = 0;

    public double departureEnd = 0;

    public int departureIntervals = 10;

    public double arrivalStart = 0;

    public double arrivalEnd = 0;

    public int arrivalIntervals = 10;

    public double minFlightTime = 10.0;

    // relative info 
    //! the size of the departure window in number of orbits of the from body
    public double departNumOrbits = 2.0;

    //! minimum flight time as a fraction of the naive Hohmann transfer time (average of to/from orbit periods)
    public double minFlightTimeHohRel = 0.25;

    //! maximum flight time as a fraction of the naive Hohmann transfer time (average of to/from orbit periods)
    public double maxFlightTimeHohRel = 2.0;

    // Meshes
    public PorkchopMesh c3Mesh = null;

    public PorkchopMesh vDepartMesh = null;

    public PorkchopMesh vArriveMesh = null;

    public PorkchopMesh vTotalMesh = null;


    public OrbitPredictor fromPredictor;

    public GameObject toMarker;

    public Text flightInfoText;

    private const double TOO_MANY = 1000.0;

    private OrbitUniversal fromOrbit;
    private OrbitUniversal toOrbit;

    private double departureStep;
    private double arrivalStep;

    private List<Maneuver> maneuvers;

    private OrbitPropagator fromPropagator;
    private OrbitPropagator toPropagator;

    // for SI, ORBITAL and SOLAR these are the right units. Re-assign in start if DL
    private string velUnits = "km/s";
    private string c3Units = "km^2/s^2";

    private GravityEngine ge; 

    void Start()
    {
        fromPredictor.gameObject.SetActive(false);
        toMarker.gameObject.SetActive(false);
        maneuvers = new List<Maneuver>();
        ge = GravityEngine.Instance();
        // modify unit strings if needed
        if (ge.units == GravityScaler.Units.DIMENSIONLESS) {
            velUnits = "L/s";
            c3Units = "L^2/s^2";
        } 
    }

    private bool CheckParams()
    {
        // Check params
        if (fromOrbit.centerNbody != toOrbit.centerNbody) {
            Debug.LogWarning("Orbits of two and from are not around the same center body.");
            return false;
        }
        if (arrivalStart <= departureStart) {
            Debug.LogWarning("Arrival time earlier than departure.");
            return false;
        }
        if (departureIntervals <= 0) {
            Debug.LogWarning("Departure increment is zero or negative");
            return false;
        }
        if (arrivalIntervals <= 0) {
            Debug.LogWarning("Arrival increment is zero or negative");
            return false;
        }
        if (arrivalStart > arrivalEnd) {
            Debug.LogWarning("Arrival end is before start");
            return false;
        }
        if (departureStart > departureEnd) {
            Debug.LogWarning("Departure end is before start");
            return false;
        }
        if (departureIntervals > TOO_MANY) {
            Debug.LogWarning("Too many steps in departure axis. Limit is " + TOO_MANY);
            return false;
        }
        if (arrivalIntervals > TOO_MANY) {
            Debug.LogWarning("Too many steps in arrival axis. Limit is " + TOO_MANY);
            return false;
        }
        return true;
    }

    private void ConvertReltoAbsolute()
    {
        departureStart = GravityEngine.instance.GetPhysicalTimeDouble();
        double departPeriod = fromOrbit.GetPeriod();
        departureEnd = departureStart + departNumOrbits * departPeriod;
        double xFerPeriod = 0.5 * (toOrbit.GetPeriod() + departPeriod);
        minFlightTime = xFerPeriod * minFlightTimeHohRel;
        double maxFlight = xFerPeriod * maxFlightTimeHohRel;
        arrivalStart = departureStart + minFlightTime;
        arrivalEnd = departureEnd + maxFlight;
    }

    private void ComputePorkchop()
    {
        fromOrbit = fromNbody.GetComponent<OrbitUniversal>();
        toOrbit = toNBody.GetComponent<OrbitUniversal>();
        if (fromOrbit == null) {
            Debug.LogWarning("Require fromNbody to have an OrbitUniversal");
            return;
        }
        if (toOrbit == null) {
            Debug.LogWarning("Require toNBody to have an OrbitUniversal");
            return;
        }
        if (inputMode == InputMode.RELATIVE) {
            ConvertReltoAbsolute();
        }

        if (!CheckParams()) {
            return;
        }
        departureStep = (departureEnd - departureStart) / ((double)departureIntervals);
        arrivalStep = (arrivalEnd - arrivalStart) / ((double)arrivalIntervals);
        double departureTime = departureStart;
        double arrivalTime = arrivalStart;
        double mu = fromOrbit.GetMu();

        fromPropagator = OrbitPropagator.GetPropagator(fromOrbit);
        toPropagator = OrbitPropagator.GetPropagator(toOrbit);

        DataGrid c3Depart = new DataGrid(departureStart, departureEnd, departureIntervals,
                                            arrivalStart, arrivalEnd, arrivalIntervals);
        DataGrid vinfDepart = new DataGrid(departureStart, departureEnd, departureIntervals,
                                            arrivalStart, arrivalEnd, arrivalIntervals);
        DataGrid vinfArrival = new DataGrid(departureStart, departureEnd, departureIntervals,
                                            arrivalStart, arrivalEnd, arrivalIntervals);
        DataGrid vinfTotal = new DataGrid(departureStart, departureEnd, departureIntervals,
                                            arrivalStart, arrivalEnd, arrivalIntervals);

        Vector3d departPos = new Vector3d();
        Vector3d departVel = new Vector3d();
        Vector3d arrivalPos = new Vector3d();
        Vector3d arrivalVel = new Vector3d();
        Vector3d v1 = new Vector3d();
        Vector3d v2 = new Vector3d();
        int error = 0;
        double vinfMag1 = 0;
        double vinfMag2 = 0;
        bool dataOk;
        Debug.LogFormat("dStep={0} aStep={1}", departureStep, arrivalStep);

        for (int d = 0; d <= departureIntervals; d++) {
            departureTime = departureStart + (float) d * departureStep;
            (departPos, departVel) = fromPropagator.PropagateToTime(departureTime);
            for (int a = 0; a <= arrivalIntervals; a++) {
                arrivalTime = arrivalStart + (float) a * arrivalStep;
                // points not in range are assigned as NaN in DataGrid
                dataOk = false;
                if (arrivalTime > (departureTime + minFlightTime)) {
                    // Do calculation 
                    (arrivalPos, arrivalVel) = toPropagator.PropagateToTime(arrivalTime);
                    (error, v1, v2) = ComputeLambert(departPos, arrivalPos, mu, reverse: false, dtsec: (arrivalTime - departureTime));
                    // check v1 is headed the right way, if not we want the reverse path
                    if (error == 0 && (Vector3d.Dot(v1, departVel) < 0.0)) {
                        (error, v1, v2) = ComputeLambert(departPos, arrivalPos, mu, reverse: true, dtsec: (arrivalTime - departureTime));
                    }
                    if (error == 0) {
                        // Update data grids
                        vinfMag1 = (v1 - departVel).magnitude;
                        vinfMag2 = (v2 - arrivalVel).magnitude;
                        c3Depart.AddData(d, a, vinfMag1*vinfMag1);
                        vinfDepart.AddData(d, a, vinfMag1);
                        vinfArrival.AddData(d, a, vinfMag2);
                        vinfTotal.AddData(d, a, vinfMag1 + vinfMag2);
                        dataOk = true;
                    } else {
                        Debug.LogWarning(string.Format("Error {0} for d={1} a={2}", error, d, a));
                    }
                }
                if (!dataOk) {
                    c3Depart.AddData(d, a, double.NaN);
                    vinfDepart.AddData(d, a, double.NaN);
                    vinfTotal.AddData(d, a, double.NaN);
                    vinfArrival.AddData(d, a, double.NaN);
                }
            }
        }
        c3Mesh.GenerateFromDataGrid(c3Depart, GridClicked);
        vDepartMesh.GenerateFromDataGrid(vinfDepart, GridClicked);
        vArriveMesh.GenerateFromDataGrid(vinfArrival, GridClicked);
        vTotalMesh.GenerateFromDataGrid(vinfTotal, GridClicked);
    }

    private void GridClicked(int x, int y)
    {
        // determine departure time and arrival time from x, y
        double departureTime = departureStart + x * departureStep;
        double arrivalTime = arrivalStart + y * arrivalStep;

        Vector3d departPos = new Vector3d();
        Vector3d departVel = new Vector3d();
        Vector3d arrivalPos = new Vector3d();
        Vector3d arrivalVel = new Vector3d();
        bool ok = false;
        if (arrivalTime > (departureTime + minFlightTime)) {
            (departPos, departVel) = fromPropagator.PropagateToTime(departureTime);
            (arrivalPos, arrivalVel) = toPropagator.PropagateToTime(arrivalTime);
            Vector3d v1 = new Vector3d();
            Vector3d v2 = new Vector3d();
            int error = 0;
            double mu = fromOrbit.GetMu();
            (error, v1, v2) = ComputeLambert(departPos, arrivalPos, mu, reverse: false, dtsec: (arrivalTime - departureTime));
            if (error == 0 && (Vector3d.Dot(v1, departVel) < 0.0)) {
                (error, v1, v2) = ComputeLambert(departPos, arrivalPos, mu, reverse: true, dtsec: (arrivalTime - departureTime));
            }
            if (error == 0) {
                // position the start and end points and set velocity on the from OrbitPredictor
                fromPredictor.transform.position = GravityEngine.instance.MapPhyPosToWorld(departPos.ToVector3());
                toMarker.transform.position = GravityEngine.instance.MapPhyPosToWorld(arrivalPos.ToVector3());
                if (!fromPredictor.gameObject.activeInHierarchy) {
                    fromPredictor.gameObject.SetActive(true);
                    fromPredictor.Init();
                }
                fromPredictor.SetVelocity(v1.ToVector3());
                fromPredictor.SetPosition(departPos.ToVector3());
                fromPredictor.UpdateOrbitU();
                toMarker.gameObject.SetActive(true);
                ok = true;
                double vdepartInf = (v1 - departVel).magnitude;
                string info = string.Format("c3depart={0:0.000}({14})\n" 
                    +"|dV1|={1:0.000} ({13})\n"
                    + "|dV2|={2:0.000} ({13})\n"
                    + "dT={3:0.00}\n"
                    + "v1=({4:0.00},{5:0.00},{6:0.00})\n"
                    + "v2=({7:0.00},{8:0.00},{9:0.00})\n"
                    + "depart=({10:0.00},{11:0.00},{12:0.00})\n"
                    + "X to execute maneuver",
                    ConvertC3(vdepartInf*vdepartInf),
                    ConvertVelocity(vdepartInf),
                    ConvertVelocity((v2 - arrivalVel).magnitude),
                    (arrivalTime - departureTime),
                    v1.x, v1.y, v1.z,
                    v2.x, v2.y, v2.z,
                    departVel.x, departVel.y, departVel.z, 
                    velUnits, c3Units);
                Debug.LogFormat("departVel (km/sec) = {0}", ConvertVelocity( departVel.magnitude));
                Debug.LogFormat("arrivalVel (km/sec) = {0}", ConvertVelocity(arrivalVel.magnitude));
                Debug.LogFormat("Transfer Time = {0}", GravityScaler.GetWorldTimeFormatted(arrivalTime-departureTime, GravityScaler.Units.SOLAR));

                Debug.LogFormat(info);
                if (flightInfoText != null)
                    flightInfoText.text = info;
                // Create maneuver in case X is pressed
                maneuvers.Clear();
                // Departure
                Maneuver departure = new Maneuver();
                departure.mtype = Maneuver.Mtype.setv;
                departure.label = "LPC.1";
                departure.nbody = fromNbody;
                departure.physPosition = departPos;
                departure.velChange = v1.ToVector3();
                departure.worldTime = (float)departureTime;
                departure.dV = (float)(v1-departVel).magnitude;
                // relative info
                departure.relativePos = departPos;
                departure.relativeVel = v1;
                departure.relativeTo = fromOrbit.centerNbody;

                maneuvers.Add(departure);

                // Arrival (will not be required if intercept)
                // setv is wrt to centerVel
                Maneuver arrival = new Maneuver();
                arrival.label = "LPC.2";
                arrival.nbody = fromNbody;
                arrival.physPosition = arrivalPos; // wrong if in orbit, centerPos will have moved on
                arrival.worldTime = (float)arrivalTime;
                arrival.mtype = Maneuver.Mtype.setv;
                arrival.velChange = arrivalVel.ToVector3();
                arrival.dV = (float)(v2-arrivalVel).magnitude;
                // relative info
                arrival.relativePos = arrivalPos;
                arrival.relativeVel = arrivalVel;
                arrival.relativeTo = fromOrbit.centerNbody;
                maneuvers.Add(arrival);

            }
        }
        if (!ok) {
            fromPredictor.gameObject.SetActive(false);
            toMarker.gameObject.SetActive(false);
            Debug.Log("No transfer at that point");
        }

    }

    /// <summary>
    /// Convert the internal C3 value (from km/hr or AU/year) into km^2/sec^2 (matches units in Nasa
    /// mission design handbook) or m/s
    /// </summary>
    /// <param name="c3"></param>
    /// <returns></returns>
    private double ConvertC3(double c3)
    {
        double lenScale = GravityScaler.PositionScaletoSIUnits()/ge.lengthScale;
        // want km not m ?
        if (ge.units != GravityScaler.Units.DIMENSIONLESS) {
            lenScale = lenScale / 1000.0;
        }
        double gspps = GravityScaler.GetGameSecondPerPhysicsSecond();
        double c3kmsec = c3 * lenScale * lenScale / (gspps * gspps);

        return c3kmsec;
    }

    private double ConvertVelocity(double v)
    {
        double lenScale = GravityScaler.PositionScaletoSIUnits()/ge.lengthScale;
        if (ge.units != GravityScaler.Units.DIMENSIONLESS) {
            // want km not m
            lenScale = lenScale / 1000.0;
        }
        double gspps = GravityScaler.GetGameSecondPerPhysicsSecond();
        double vkmsec = v * lenScale  / gspps;

        return vkmsec;
    }

    //! Error codes
    public const int G_NOT_CONVERGED = 1;
    public const int Y_NEGATIVE = 2;
    public const int XFER_180 = 3;
    public const int IMPACT = 4;

    /// <summary>
    /// Determine the velocities for the Lambert transfer. 
    /// 
    /// This is a modified cut and paste from the LambertUniversal code, streamlined to eliminate the creation of maneuver objects
    /// etc. to just the bare bones required to get the velocities for the LambertPorkchop plot.
    /// 
    /// This code does not require |R1| < |R2|. It is based on Vallado 4th edition Algorithm 58 (p492).
    /// </summary>
    /// <param name="r1"></param>
    /// <param name="r2"></param>
    /// <param name="mu"></param>
    /// <param name="reverse"></param>
    /// <param name="dtsec"></param>
    /// <returns></returns>
    private (int, Vector3d, Vector3d) ComputeLambert(
        Vector3d r1,
        Vector3d r2,
        double mu,
        bool reverse,
        double dtsec)
    {
        // were params
        bool df = false;
        int nrev = 0;

        const double small = 0.0000001;
        const int numiter = 40;
        // const double mu = 398600.4418;  // m3s2
        const double pi = System.Math.PI;

        double[] v1 = new double[] { 0, 0, 0 };
        double[] v2 = new double[] { 0, 0, 0 };

        int loops, ynegktr;
        // NOTE: The c2/c3 in here are not the same as C3 used in the porkchop plot. Just a name collision.
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



        return (error, new Vector3d(ref v1), new Vector3d(ref v2));
    }

    // only need value up to 13 in the Lambert universal, so make it a look up
    private double[] factorial = { 1.0, 1.0, 2.0, 6.0, 24.0, 120.0, 720.0, 5040.0, 40320.0,
            362880.0, 3628800.0, 3.991680E7, 4.790016E8, 6227021E9};

    private double Factorial(int x)
    {
        return factorial[x];
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P) || plotOnStart) {
            Debug.Log("Starting porkchop computations");
            ComputePorkchop();
            GravityEngine.instance.SetEvolve(false);
            Debug.Log("DONE porkchop computations");
            plotOnStart = false;
        }
        if (Input.GetKeyUp(KeyCode.X)) {
            GravityEngine.instance.AddManeuvers(maneuvers);
            GravityEngine.instance.SetEvolve(true);
            fromPredictor.gameObject.SetActive(false);
            toMarker.gameObject.SetActive(false);

        }
    }
}
