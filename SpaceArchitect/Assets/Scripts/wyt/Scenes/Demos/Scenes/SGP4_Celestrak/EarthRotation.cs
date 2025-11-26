using UnityEngine;

/// <summary>
/// Rotate the Earth in ORBITAL unit.
///
/// Initial Earth position is set using the date and time specified in GE start time.
///
/// This assumes that the x-axis is aligned with 0 longitude in the initial rotation of the
/// Earth sphere. 
/// 
/// TODO: Ensure that the rotation is consistent as timeZoom and time reversal occur. 
/// </summary>
public class EarthRotation : MonoBehaviour
{
    private float initPhase = 0f;

    [SerializeField]
    private Vector3 axis = Vector3.forward;

    //! angular frequency in degrees per world second
    private float omegaDeg;

    private Quaternion initialRotation;
    private GravityEngine ge;

    // Start is called before the first frame update
    void Start()
    {
        initialRotation = transform.rotation;
        ge = GravityEngine.Instance();

        omegaDeg = 360.0f / GravityScaler.SECS_PER_SIDEREAL_DAY;
        double jd = GravityEngine.Instance().GetStartTimeAsJD();
        double theta_GMST = OrbitUtils.GsTime(jd);
        initPhase = -1.0f * (float) theta_GMST * Mathf.Rad2Deg;
        Debug.LogFormat("Setting initial phase: {0} for JD={1}", initPhase, jd);
    }

    public Vector3 GetAxis()
    {
        return axis;
    }

    public float GetOmega()
    {
        return omegaDeg;
    }

    // Update is called once per frame
    void Update()
    {
        float angleDegrees = (float)(initPhase - omegaDeg * ge.GetTimeWorldSeconds());
        transform.rotation = Quaternion.AngleAxis(angleDegrees, axis) * initialRotation;
    }

    public Vector3 RotatePoint(Vector3 point, float deltaTime) {
        float angleDegrees = Mathf.Rad2Deg * (omegaDeg * deltaTime);
        return Quaternion.AngleAxis(angleDegrees, axis) * point;
    }
}
