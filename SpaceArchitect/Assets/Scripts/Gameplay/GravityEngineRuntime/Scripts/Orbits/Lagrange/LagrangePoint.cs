using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place a game object or initialize an NBody at the specified Lagrange point with the indicated offset from the 
/// Lagrange point. 
/// 
/// Lagrange points are very sensitive to initial conditions:
/// - objects must be placed at the radius with respect the center of mass of the binary system 
/// - L1, L2 and L3 are inherently unstable
/// - L4 and L5 are stable when m2/(m1+m2) less than 0.0385
/// (See Murray and Dermott, Solar System Dynamics Ch3). 
/// 
/// (optional) hostNBody indicates the attached NBody should be initialized at the specified Lagrange point
/// and then left to evolve freely. (If not present then the gameObject hosting this script will be moved
/// every frame to 
/// </summary>
public class LagrangePoint : MonoBehaviour
{
    [SerializeField]
    private NBody planet = null;

    private OrbitUniversal planetOrbit; 

    private NBody centerBody = null;

    //! (optional) hostNBody indicates the attached NBody should be initialized at the specified Lagrange point
    //! and then left to evolve freely. (If not present then the gameObject hosting this script will be moved
    //! every frame to stay at the L point if maintain position is set). 
    private NBody hostNBody;

    public enum Type { L1, L2, L3, L4, L5};

    [SerializeField]
    private Type lagrangeType = Type.L4;

    public enum OffsetUnits { ABSOLUTE, RELATIVE};

    [SerializeField]
    [Tooltip("Use offset relative to normalized radius, or as absolute offset in GE units")]
    private OffsetUnits offsetUnits = OffsetUnits.RELATIVE;

    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;

    [SerializeField]
    private Vector3 velocityOffset = Vector3.zero;

    [SerializeField]
    private bool maintainPosition = false;

    // scaled masses of the system
    private double mu1, mu2;

    private double r_scale;

    private GravityEngine ge;

    private float time0;
    //! angular frequency of rotation 
    private double omega;

    //! the initial Lagrange point in (x,y) space with scale applied. Used when maintaining Lagrange position 
    private Vector3d xyLagrangeStart;
    private Vector3d xyzVelocityStart;
    private Vector3d xyzVelocity;

    //! Lagrange point in GE space
    private Vector3 lagrangePos;
    //! Lagrange point initial co-rotating velocity
    private Vector3 lagrangeVel;

    private Quaternion relativeOrientation; 

    private double m_total; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        planetOrbit = planet.GetComponent<OrbitUniversal>();
        if (planetOrbit == null) {
            Debug.LogError("Require that planet has an OrbitUniversal ");
            return;
        }
        if (planetOrbit.eccentricity > OrbitUtils.small) {
            Debug.LogWarning("Planet must be in a circular orbit for Lagrange points to be well defined. But will proceed.");
        }
        centerBody = planetOrbit.centerNbody;
        hostNBody = GetComponent<NBody>();

        ge.AddGEStartCallback(GEStart);

    }

    private void GEStart() {
        // normalize so mu1 + mu2 = 1 (total mass)
        double p_mass = ge.GetPhysicsMass(planet);
        double c_mass = ge.GetPhysicsMass(centerBody);
        m_total = c_mass + p_mass;
        mu2 = p_mass / (m_total);
        mu1 = 1.0 - mu2;

        r_scale = planetOrbit.GetApogee();

        omega = planetOrbit.GetAngularVelocity();
        time0 = ge.GetPhysicalTime();
        ComputeLagrangeXY();

        // record rotation from (1,0,0) to axis position in space
        Vector3 planetPos = ge.GetPhysicsPosition(planet);
        Vector3 centerPos = ge.GetPhysicsPosition(planetOrbit.centerNbody);
        Vector3 initialPlanetAxis = (planetPos - centerPos).normalized;
        Vector3 orbitAxis = Vector3.right; // Z-axis
        relativeOrientation = Quaternion.FromToRotation(orbitAxis, initialPlanetAxis);
        if (hostNBody != null) {
            // need to compute Lagrangian position and velocity
            Vector3d posNow = ComputeLagrangePosition();
            ge.SetPositionDoubleV3(hostNBody, posNow);
            ge.SetVelocityDoubleV3(hostNBody, xyzVelocity);
        } else {
            transform.position = ge.MapPhyPosToWorld(ComputeLagrangePosition().ToVector3());
            Debug.LogFormat("{0} set position: {1}", gameObject.name, transform.position);
        }
    }

    /// <summary>
    /// Use the Lagrange equations in the canonical co-ordinate reference frame
    /// - the center of mass is at (0, 0, 0)
    /// - the planet is at ( mu2, 0, 0)
    /// - the star is at (-mu1, 0, 0)
    /// 
    /// The xyLagrangeStart and xyzVelocityStart are the scaled up position and velocity of the
    /// Lagrange point in the Lagrange xyz coordinates. They are NOT rotated to align with current
    /// line from star to planet. 
    /// 
    /// </summary>
    private void ComputeLagrangeXY() {
        // locate in x,y plane with unit radius then scale and move into orbital plane
        double x = 0;
        double y = 0;
        double alpha = Mathd.Pow(mu2 / (3.0 * mu1), 1.0 / 3.0);
        double alphaSq = alpha * alpha;
        double r = 0;

        switch (lagrangeType) {
            case Type.L1:
                // Murray & Dermott (3.83)
                r = alpha - (1.0 / 3.0) * alphaSq - (1.0) / (9.0) * alpha * alphaSq 
                    - (23.0 / 81.0) * alphaSq * alphaSq;
                x = mu1 - r;
                break;

            case Type.L2:
                // Murray & Dermott (3.88)
                r = alpha + (1.0 / 3.0) * alphaSq - (1.0) / (9.0) * alpha * alphaSq
                     - (31.0 / 81.0) * alphaSq * alphaSq;
                x = r + mu1;
                break;

            case Type.L3:
                // Murray & Dermott (3.93)
                double mu21 = mu2 / mu1;
                double mu21Sq = mu21 * mu21;
                double beta = -(7.0 / 12.0) * mu21 + (7.0 / 12.0) * mu21Sq - (13223.0 / 20736.0) * mu21 * mu21Sq;
                r = 1 + beta;
                x = -mu2 - r;
                break;

            case Type.L4:
                x = 0.5 - mu2;
                y = Mathd.Sqrt(3) / 2;
                break;

            case Type.L5:
                x = 0.5 - mu2;
                y = -Mathd.Sqrt(3) / 2;
                break;

            default:
                break;
        }
        // x,y are wrt to the center of mass of the system. Correct this before using
        x += mu2;
        double z = 0; 
        if (offsetUnits == OffsetUnits.RELATIVE) {
            x += positionOffset.x;
            y += positionOffset.y;
            z += positionOffset.z;
        }
        // apply scale
        x *= r_scale;
        y *= r_scale;
        z *= r_scale;
        xyLagrangeStart = new Vector3d(x, y, z);
        if (offsetUnits == OffsetUnits.ABSOLUTE) {
            xyLagrangeStart += new Vector3d(positionOffset);
        }

        // velocity. At t=0 position is normal to r vector
        // TODO: Use the pos with no offset ??
        double velMagnitude = Mathd.Sqrt(m_total / xyLagrangeStart.magnitude);
        double velPhase = Mathd.Atan2(xyLagrangeStart.y, xyLagrangeStart.x);
        xyzVelocityStart = new Vector3d(-Mathd.Sin(velPhase), Mathd.Cos(velPhase), 0) * velMagnitude;
    }

    /// <summary>
    /// Map the start position and velocity into alignment with the current planet poisiton. 
    /// - apply the offset rotation based on the difference between (1,0,0) and the initial planet position
    /// - evolve the Lagrange point around the orbit based on the time since init
    /// - set position relative to the current center of mass
    /// 
    /// </summary>
    /// <returns></returns>
    private Vector3d ComputeLagrangePosition()
    {
        // use time to update position/vel in XYZ space
        double phase = (ge.GetPhysicalTime() - time0) * omega;

        xyzVelocity = new Vector3d(xyzVelocityStart.x * Mathd.Cos(phase) - xyzVelocityStart.y * Mathd.Sin(phase),
                            xyzVelocityStart.x * Mathd.Sin(phase) + xyzVelocityStart.y * Mathd.Cos(phase),
                            xyzVelocityStart.z);

        Vector3d xyzPosNow = new Vector3d(xyLagrangeStart.x * Mathd.Cos(phase) - xyLagrangeStart.y * Mathd.Sin(phase),
                            xyLagrangeStart.x * Mathd.Sin(phase) + xyLagrangeStart.y * Mathd.Cos(phase),
                            xyLagrangeStart.z);

        // TODO: double Quaternion
        if (ge.xzOrbits) {
            xyzPosNow = XZPlane.PhysicsToUnity(xyzPosNow);
            xyzVelocity = XZPlane.PhysicsToUnity(xyzVelocity);
        }
        xyzVelocity = new Vector3d(relativeOrientation * xyzVelocity.ToVector3());
        xyzPosNow = new Vector3d(relativeOrientation * xyzPosNow.ToVector3()) + 
                        OrbitUtils.CenterOfMass(planet, planetOrbit.centerNbody);

        return xyzPosNow;
    }

    // Need to align with GE positions, so FixedUpdate
    void FixedUpdate() {
        if (maintainPosition) {
            transform.position = ge.MapPhyPosToWorld(ComputeLagrangePosition().ToVector3());
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {

        // The gizmo may be misleading, since object may have been affected by oither masses. 
        // Orbit predictors are better suited to showing the orbit while playing.
        if (Application.isPlaying) {
            return;
        }

        // If center body has not been configured, cannot draw anything
        if (planet == null)
            return;

        //Vector3d pos = ComputeLagrangePosition();
        //transform.position = pos.ToVector3();
    }

#endif

    }
