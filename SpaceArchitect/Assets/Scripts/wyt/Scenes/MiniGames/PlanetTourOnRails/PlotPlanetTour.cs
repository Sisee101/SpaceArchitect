using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Do a preview plot of the path of a ship that exits planetA's SOI and (may) intersect other planets SOIs.
///
/// The ship trajectory is kept simple - just extend the velocity at the time P is pressed by some percentage and
/// maintain the current velocity direction. This can be iterated by increasing/decreasing the percentage by A/D keys.
///
/// The idea is to make use of OrbitPropagators for the ship and planets and evolve them forward in lockstep and then
/// determine when SOI boundaries are crossed. When this happens the ship propagator is switched to the new primary
/// gravitational source. This is a simpler implementation that e.g. cloning the GravityState and evolving because
/// the ship would need a series of KeplerSequence segments and there would be a lot of "moving parts".
///
/// A line renderer is used to show the potential path. It hold the absolute positions in the solar system space.
///
/// The plot routine also draws an SOI circle at each enter and exit point (since as the ship moves the planets are
/// moving).
/// 
/// </summary>
public class PlotPlanetTour : MonoBehaviour
{
    [Header("Press P to plot course. A/D to alter burn.")]
    public NBody ship;

    [Header("Star")]
    public NBody star;

    [Header("Planets")]
    public NBody[] planets;

    [Header("Duration of path plot (GE time)")]
    public double tFinal = 10.0;

    [Header("Escape burn as a percent of orbital velocity")]
    public double escapeBurnDv = 1.5;
    public double dVStepPercent = 0.05;

    [Header("Line for planet tour")]
    public LineRenderer lineR;
    public double lineDt = 0.1;

    public GameObject circlePrefab;

    /// <summary>
    /// What sphere of influence is the ship currently in?
    /// 0..N-1 Planet 1..N
    /// -1 = star (i.e. in between planet SOIs)
    /// </summary>
    private int activeSphere = 0;   // start in orbit around planet A
    private const int SUN_SOI = -1;
    private double[] soiRadius; 

    private GravityEngine ge;

    // struct to store orbit segments of preview path so they can be added to Kseq on execute
    private struct OrbitSegment
    {
        public Vector3d r;
        public Vector3d v;
        public double t;
        public double mu;
        public NBody center;

        public OrbitSegment(Vector3d r, Vector3d v, double t, double mu, NBody center)
        {
            this.r = r;
            this.v = v;
            this.t = t;
            this.mu = mu;
            this.center = center;
        }
    }
    private List<OrbitSegment> orbitSegments;
    private List<GameObject> circles;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        soiRadius = new double[planets.Length];
        for (int i=0; i < planets.Length; i++) {
            soiRadius[i] = OrbitUtils.SoiRadius(star, planets[i]);
        }
        circles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) {
            ge.SetEvolve(false);
            PlotPath();
        }

        if (Input.GetKeyDown(KeyCode.A)) {
            escapeBurnDv += dVStepPercent;
            PlotPath();
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            escapeBurnDv -= dVStepPercent;
            PlotPath();
        }
        if (Input.GetKeyDown(KeyCode.X)) {
            if (orbitSegments.Count == 0)
                PlotPath();
            KeplerSequence kSeq = ship.GetComponent<KeplerSequence>();
            if (kSeq == null) {
                Debug.LogError("Can't execute. No KeplerSequence on ship");
                return;
            }
            foreach(OrbitSegment os in orbitSegments) {
                kSeq.AppendElementRVT(os.r, os.v, os.t, relativePos: true, ship, os.center, callback:null);
                ge.SetEvolve(true);
            }
        }


    }


    private void PlotPath()
    {
        activeSphere = 0;
        // clear any prevous SOIs
        foreach (GameObject go in circles) {
            Destroy(go);
		}
        // create an Orbit Propator to exit PlanetA. Need relative R, V
        Vector3d v0 = ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(planets[activeSphere]);
        v0 *= escapeBurnDv;
        Vector3d r0 = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(planets[activeSphere]);
        EvolveToTime(r0, v0);
    }

    /// <summary>
    /// Evolve the ship and planets forward in time and at each timeStep check to see if an SOI transition has happened
    /// (modulo some debounce after an SOI change).
    ///
    /// If an SOI change has occured, switch to a new OrbitPropagator for the new regime.
    ///
    /// At the same time, store up orbital segments so they can be added to the ship Kseq if the user decides to
    /// execute. 
    /// 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    private const int SOI_DEBOUNCE = 5; // min number of time steps a ship must stay in an SOI

    private void EvolveToTime(Vector3d r0, Vector3d v0)
    {
        int n = (int) (tFinal / lineDt)+1;
        double tEvolved = 0.0;
        Vector3[] points = new Vector3[n];
        int pIndex = 0;
        int soiDebounce = SOI_DEBOUNCE;
        orbitSegments = new List<OrbitSegment>();
        double mu_star = ge.GetMass(star);
        int nPlanets = planets.Length;
        OrbitPropagator[] planetProp = new OrbitPropagator[nPlanets];
        for (int i=0; i < nPlanets; i++) {
            Vector3d r = ge.GetPositionDoubleV3(planets[i]);
            Vector3d v = ge.GetVelocityDoubleV3(planets[i]);
            planetProp[i] = new OrbitPropagator(r, v, time0: 0.0, mu_star);
        }
        Vector3d[] rPlanets = new Vector3d[nPlanets];
        Vector3d[] vPlanets = new Vector3d[nPlanets];

        OrbitPropagator orbitP = new OrbitPropagator(r0, v0, time0: 0.0, mu: ge.GetMass(planets[activeSphere]));
        while (tEvolved < tFinal) {
            tEvolved += lineDt;
            (Vector3d r, Vector3d v) = orbitP.PropagateToTime(tEvolved);
            points[pIndex] = r.ToVector3();   // 1-1 scale in this simple demo
            for (int i=0; i < nPlanets; i++) {
                (rPlanets[i], vPlanets[i]) = planetProp[i].PropagateToTime(tEvolved);
            }
            if (activeSphere != SUN_SOI)
                points[pIndex] += rPlanets[activeSphere].ToVector3();
            pIndex++;
            // check to see if we cross the SOI
            if (soiDebounce++ > SOI_DEBOUNCE) {
                if (activeSphere == SUN_SOI) {
                    // need to check if we enter ANY of the planet SOIs
                    for (int i = 0; i < planets.Length; i++) {
                        if (Vector3d.Distance(r, rPlanets[i]) < soiRadius[i]) {
                            // Add an object to show SOI at entry
                            DrawCircle(rPlanets[i].ToVector3(), (float) soiRadius[i]);
                            // entered planet SOI, move to relative r, v
                            r = r - rPlanets[i];
                            v = v - vPlanets[i];
                            double mu_seg = ge.GetMass(planets[i]);
                            orbitP = new OrbitPropagator(r, v, tEvolved, mu_seg);
                            orbitSegments.Add(new OrbitSegment(r, v, tEvolved, mu_seg, planets[i]));
                            activeSphere = i;
                            Debug.LogFormat("Enter SOI {0} at {1} ", planets[i].gameObject.name, tEvolved);
                            soiDebounce = 0;

                            break;
                        }
                    }
                }
                else {
                    // r is relative to planet
                    if (r.magnitude > soiRadius[activeSphere]) {
                        // Add an object to show SOI at exit
                        DrawCircle(rPlanets[activeSphere].ToVector3(), (float) soiRadius[activeSphere]);
                        // exited planet SOI
                        v = v + vPlanets[activeSphere];
                        r = r + rPlanets[activeSphere];
                        orbitP = new OrbitPropagator(r, v, tEvolved, mu_star);
                        orbitSegments.Add(new OrbitSegment(r, v, tEvolved, mu_star, star));
                        activeSphere = SUN_SOI;
                        Debug.LogFormat("Enter Sun SOI at {0}", tEvolved);
                        soiDebounce = 0;
                    }
                }
            }
        }
        lineR.positionCount = pIndex;
        lineR.SetPositions(points);
    }

    /// <summary>
	/// Very simple & dumb code to draw a circle
	/// </summary>
	/// <param name="center"></param>
	/// <param name="radius"></param>
    private void DrawCircle(Vector3 center, float radius)
	{
        Vector3[] points = new Vector3[360];
        GameObject go = Instantiate(circlePrefab);
        LineRenderer line = go.GetComponent<LineRenderer>();
        float theta;
        for (int i=0; i < 360; i++) {
            theta = (float)(i) * Mathf.Deg2Rad ;
            points[i] = new Vector3(center.x + radius * Mathf.Cos(theta), center.y + radius * Mathf.Sin(theta), 0);
		}
        line.positionCount = 360;
        line.useWorldSpace = true;
        line.loop = true;
        line.SetPositions(points);
        circles.Add(go);
	}
}
