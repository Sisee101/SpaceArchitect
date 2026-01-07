using UnityEngine;
using MathNet.Numerics.Interpolation;

/// <summary>
/// Fixed Orbit extension that can be preloaded with a path in GE world space and then used as
/// a propagator. This allows e.g. an ascent trajectory to be precomputed and then reversed as part of
/// a KeplerSequence
/// 
/// If there is a maneuver or change applied to this object, then it is may be necessary to 
/// recompute the future part of the trajectory. This will be done via callbacks (eventually).
/// 
/// </summary>
public class PathPropagator : MonoBehaviour, IFixedOrbit
{
    public NBody centerBody;

    public int numPoints;
    protected double t_interval;

    public enum Interpolate { LINEAR, HERMITE, KEPLER};

    public Interpolate interpolate = Interpolate.HERMITE;

    // GE start time for this segment (allows time in segment to be relative)
    protected double t_start; 

    private int addIndex = 0;

    // states is of the form:
    // row @ time => [time, r_x, r_y, r_z, v_x, v_y, v_z]
    protected const int T = 0;
    protected const int R_X = 1;
    protected const int R_Y = 2;
    protected const int R_Z = 3;
    protected const int V_X = 4;
    protected const int V_Y = 5;
    protected const int V_Z = 6;
    // it is a time ordered sequence of (r,v) state at a regular interval
    protected double[,] states;

    /// <summary>
    /// Init with number of points. 
    /// 
    /// If there is a constant t_interval, providing it will speed up the lookup of the point. If not will do 
    /// a binary search for the appropriate t each time. 
    /// 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="t_interval"></param>
    public void Init(int numPoints, double t_interval = -1.0)
    {
        this.numPoints = numPoints;
        this.t_interval = t_interval;
        states = new double[numPoints, 7];
        addIndex = 0;
    }

    public void SetStartTime(double t)
    {
        t_start = t;
    }

    // for now just assume add in order
    public void AddPoint(double t, Vector3d r, Vector3d v)
    {
        states[addIndex, T] = t;
        states[addIndex, R_X] = r.x;
        states[addIndex, R_Y] = r.y;
        states[addIndex, R_Z] = r.z;
        states[addIndex, V_X] = v.x;
        states[addIndex, V_Y] = v.y;
        states[addIndex, V_Z] = v.z;
        addIndex++;
    }

    public double GetStartTime()
    {
        return t_start + states[0, T]; 
    }

    public double GetEndTime()
    {
        return t_start + states[numPoints - 1, T];
    }

    public void ApplyRotation(Quaternion q)
    {
        // TODO: Really need a double Quaternion, for now live with loss of precision
        for (int i = 0; i < numPoints; i++) {
            Vector3 r = q * new Vector3((float) states[i, R_X], (float) states[i, R_Y], (float) states[i, R_Z]);
            states[i, R_X] = r.x;
            states[i, R_Y] = r.y;
            states[i, R_Z] = r.z;
            Vector3 v = q * new Vector3((float)states[i, V_X], (float)states[i, V_Y], (float)states[i, V_Z]);
            states[i, V_X] = v.x;
            states[i, V_Y] = v.y;
            states[i, V_Z] = v.z;
        }
    }

    public void ScaleStates(double scaleT, double scaleR, double scaleV)
    {
        t_interval *= scaleT;
        t_start *= scaleT;
        for (int i = 0; i < numPoints; i++) {
            states[i, T] *= scaleT;
            states[i, R_X] *= scaleR;
            states[i, R_Y] *= scaleR;
            states[i, R_Z] *= scaleR;
            states[i, V_X] *= scaleV;
            states[i, V_Y] *= scaleV;
            states[i, V_Z] *= scaleV;
        }
    }

    public void FlipYZ()
    {
        double temp; 
        for (int i = 0; i < numPoints; i++) {
            temp = states[i, R_Y] ;
            states[i, R_Y] = states[i, R_Z];
            states[i, R_Z] = temp;
            temp = states[i, V_Y];
            states[i, V_Y] = states[i, V_Z];
            states[i, V_Z] = temp;
        }
    }

    public int NumAdded()
    {
        return addIndex;
    }

    public (double, Vector3d, Vector3d) StateAtIndex(int i)
    {
        return (states[i, T],
                new Vector3d(states[i, R_X], states[i, R_Y], states[i, R_Z]),
                new Vector3d(states[i, V_X], states[i, V_Y], states[i, V_Z]));
    }

    //%%%%%%%%% STANDARD API 

    Vector3 IFixedOrbit.ApplyImpulse(Vector3 impulse)
    {
        throw new System.NotImplementedException();
    }

    string IFixedOrbit.DumpInfo()
    {
        throw new System.NotImplementedException();
    }

    public virtual void PreEvolve(float physicalScale, float massScale)
    {
        //nothing to do
    }

    /// <summary>
    /// Hermite Interpolation
    /// Use a stripped down version of the Math.Net numerics package.
    /// Provide a set of four points for each state variable e.g. (t0, R_X0), (t1, R_X1) etc.
    ///
    /// Goal is to have the requested value of t roughly in the center of the range of four points
    /// In steady state case if i = t/t_interval, want i-1, i, i+1, i+2
    /// In boundary cases just take first 4 points or last four points.
    ///
    /// To avoid re-creating splines, cache them from request to request and onoy re-create when i changes
    /// </summary>

    private int lastBaseIndex = -1;
    private CubicSpline[] splines = new CubicSpline[6];

    private int lastIndex = -1;
    private OrbitPropagator orbitProp;

    public virtual void Evolve(double physicsTime, GravityState gravityState, ref double[] r, ref double[] v, bool isQuery)
    {
        double t = physicsTime - t_start;
        r[0] = 0;
        r[1] = 0;
        r[2] = 0;
        v[0] = 0;
        v[1] = 0;
        v[2] = 0;
        int index;
        if (t_interval < 0) {
            Debug.LogError("Write some code!");
            return;
        } else {
            index = (int)((t - states[0, T]) / t_interval);
        }
        if (index < 0) {
            index = 0;
            // Debug.LogError("Asked for time before path");
        }
        if (index >= numPoints) {
            Debug.LogErrorFormat("time beyond path in {0} t={1} index={2} t_end={3}",
                gameObject.name, physicsTime, index, GetEndTime());
            return;
        }

        switch (interpolate) {
            case Interpolate.HERMITE:
                int baseIndex = index - 1;
                if (baseIndex < 0)
                    baseIndex = 0;
                else if (baseIndex + 4 >= states.Length)
                    baseIndex = states.Length - 5;
                if (baseIndex != lastBaseIndex) {
                    lastBaseIndex = baseIndex;
                    double[] x = new double[] {states[baseIndex,T], states[baseIndex+1, T],
                                       states[baseIndex + 2, T], states[baseIndex + 3, T] };
                    for (int i = 0; i < 3; i++) {
                        double[] y = new double[] {states[baseIndex,i+1], states[baseIndex+1, i+1],
                                       states[baseIndex + 2, i+1], states[baseIndex + 3, i+1] };
                        double[] dy = new double[] {states[baseIndex,i+4], states[baseIndex+1, i+4],
                                       states[baseIndex + 2, i+4], states[baseIndex + 3, i+4] };
                        splines[i] = CubicSpline.InterpolateHermiteSorted(x, y, dy);
                        // don't have derivs for V
                        splines[i + 3] = CubicSpline.InterpolatePchipSorted(x, dy);
                    }

                }
                r[0] = splines[0].Interpolate(t);
                r[1] = splines[1].Interpolate(t);
                r[2] = splines[2].Interpolate(t);
                v[0] = splines[3].Interpolate(t);
                v[1] = splines[4].Interpolate(t);
                v[2] = splines[5].Interpolate(t);
                break;

            case Interpolate.LINEAR:
                if (index + 1 < numPoints) {
                    double tf = (t - states[index, T]) / t_interval;
                    r[0] = tf * states[index + 1, R_X] + (1.0 - tf) * states[index, R_X];
                    r[1] = tf * states[index + 1, R_Y] + (1.0 - tf) * states[index, R_Y];
                    r[2] = tf * states[index + 1, R_Z] + (1.0 - tf) * states[index, R_Z];
                    v[0] = tf * states[index + 1, V_X] + (1.0 - tf) * states[index, V_X];
                    v[1] = tf * states[index + 1, V_Y] + (1.0 - tf) * states[index, V_Y];
                    v[2] = tf * states[index + 1, V_Z] + (1.0 - tf) * states[index, V_Z];


                } else {
                    // just use the last time point, but report an error
                    r[0] = states[index, R_X];
                    r[1] = states[index, R_Y];
                    r[2] = states[index, R_Z];
                    v[0] = states[index, V_X];
                    v[1] = states[index, V_Y];
                    v[2] = states[index, V_Z];
                    Debug.LogError("Evolution beyond last time point");

                }
                break;
            case Interpolate.KEPLER:
                if (index != lastIndex) {
                    Vector3d r0 = new Vector3d(states[index, R_X], states[index, R_Y], states[index, R_Z]);
                    Vector3d v0 = new Vector3d(states[index, V_X], states[index, V_Y], states[index, V_Z]);
                    double mu = GravityEngine.Instance().GetMass(centerBody);
                    orbitProp = new OrbitPropagator(r0, v0, time0: 0.0, mu);
                    lastIndex = index;
                }
                (Vector3d r_v, Vector3d v_v) = orbitProp.PropagateToTime(t - states[index, T]);
                r[0] = r_v.x;
                r[1] = r_v.y;
                r[2] = r_v.z;
                v[0] = v_v.x;
                v[1] = v_v.y;
                v[2] = v_v.z;
                break;
        }
    }

    public NBody GetCenterNBody()
    {
        return centerBody;
    }

    void IFixedOrbit.GEUpdate(GravityEngine ge)
    {
        throw new System.NotImplementedException();
    }

    bool IFixedOrbit.IsOnRails()
    {
        return true;
    }

    void IFixedOrbit.Move(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    void IFixedOrbit.PreEvolve(float physicalScale, float massScale)
    {
        // nothing to do
    }

    void IFixedOrbit.SetNBody(NBody nbody)
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void IFixedOrbit.UpdatePositionAndVelocity(Vector3 pos, Vector3 vel)
    {
        throw new System.NotImplementedException();
    }

    public string DumpInfo()
    {
        return string.Format("  PathP start={0} interval={1} num={2}\n", t_interval, GetEndTime(), numPoints);
    }
}
