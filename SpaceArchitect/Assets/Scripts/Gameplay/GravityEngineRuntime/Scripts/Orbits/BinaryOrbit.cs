using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

/// <summary>
/// Binary Orbit
///
/// Configures the initial velocities for two roughly equal masses to establish their
/// elliptical orbits around the center of mass of the pair. 
///
/// Must have two NBody objects with OrbitUniversal as immediate children.
/// 
/// The BinaryPair object can optionally have a zero-mass NBody and OrbitUniversal component attached. This will be used to 
/// determine the initial conditions for the binary pair (position and velocity) so the binary pair can be placed in orbit
/// around another body. This "dummy" NBody is only used during the setup. (It will continue to evolve but there is no model 
/// attached and nothing in the scene will be affected by its zero mass.)
/// 
/// </summary>
[RequireComponent(typeof(NBody))]
public class BinaryOrbit :  OrbitUniversal, IOrbitScalable {

    //! Velocity of center of mass of the binary pair
    public Vector3 initialPosition;

 	public Vector3 velocity;

	private NBody cmNbody; 
	private NBody nbody1; 
	private NBody nbody2;

    private double mu1;
    private double mu2; 

	private OrbitUniversal orbit1;
	private OrbitUniversal orbit2;

    private bool inOrbit; 
    private OrbitUniversal cmOrbit; 

    public void Start()
    {
        ge = GravityEngine.Instance();
    }


    public void SetupOrbits () {

        ge = GravityEngine.Instance();

        cmNbody = GetComponent<NBody>();
		if (cmNbody.mass > 0) {
			cmNbody.mass = 0;
			Debug.LogWarning("Setting CM mass to zero");
		}
        // Is this binary in orbit around something else??
        OrbitUniversal[] cmOrbits = cmNbody.GetComponents<OrbitUniversal>();
        // both orbitU and BinaryOrbit will show up
        inOrbit = false;
        foreach (OrbitUniversal orbitU in cmOrbits) {
            if (orbitU != this) {
                orbitU.InitNBody(ge.physToWorldFactor, ge.massScale);
                inOrbit = true;
                cmOrbit = orbitU;
            }
        }

		nbody1 = transform.GetChild(0).GetComponent<NBody>();
		nbody2 = transform.GetChild(1).GetComponent<NBody>();

		if ((nbody1 == null) || (nbody2 == null)) {
			if (!Application.IsPlaying(this)) {
				// may not be setup in editor yet
				return;
			} else {
				Debug.LogError("BinaryOrbit missing one of the bodies");
			}
		}

		orbit1 = nbody1.GetComponent<OrbitUniversal>();
        if (orbit1 == null)
            orbit1 = nbody1.gameObject.AddComponent<OrbitUniversal>();
		orbit1.centerNbody = cmNbody;
		orbit2 = nbody2.GetComponent<OrbitUniversal>();
        if (orbit2 == null)
            orbit2 = nbody2.gameObject.AddComponent<OrbitUniversal>();
        orbit2.centerNbody = cmNbody;

		if ((orbit1 == null) || (orbit2 == null)) {
			if (!Application.IsPlaying(this)) {
				// may not be setup in editor yet
				return;
			} else {
				Debug.LogError("BinaryOrbit body is missing orbit universal");
			}
		}

		// Need to take the values from this Orbit and scale as required to setup the child objects
		// KeplerSeq code will ensure correct init order on Setup

		// true GE mass is not needed, since a mass ratio is used
		double m_total = (nbody1.mass + nbody2.mass);
		mu1 = nbody1.mass / m_total;
		mu2 = nbody2.mass / m_total;
		orbit1.SetMu(mu2*mu2*ge.GetMass(nbody2));
		orbit2.SetMu(mu1*mu1*ge.GetMass(nbody1));

		// common params
		orbit1.evolveMode = this.evolveMode;
		orbit2.evolveMode = this.evolveMode;
		orbit1.p = this.p * mu2; // yes 2!
		orbit1.p_inspector = this.p_inspector * mu2;
		orbit2.p = this.p * mu1;
		orbit2.p_inspector = this.p_inspector * mu1;
		orbit1.eccentricity = this.eccentricity;
		orbit2.eccentricity = this.eccentricity;

		// orbit1
		orbit1.omega_lc = this.omega_lc;
		orbit1.omega_uc = this.omega_uc;
		orbit1.inclination = this.inclination;
		orbit1.phase = this.phase;

		// orbit2
		orbit2.omega_lc = this.omega_lc;
		orbit2.omega_uc = this.omega_uc + 180f;
		orbit2.inclination = -this.inclination; // flip due to Omega
		orbit2.phase = this.phase;

        if (!Application.isPlaying) {
            // force orbits to update in editor scene
            orbit1.ApplyScale(ge.lengthScale);
            orbit2.ApplyScale(ge.lengthScale);
        }
	}

	public override bool IsOnRails()
	{
		return true;
	}

	public override void PreEvolve(float physicalScale, float massScale)
	{
		// do not let base run
		SetupOrbits();
	}

    /// <summary>
    /// In NBody mode, the center of mass NBody will run as a fixed object and determine it's new position each cycle. This
    /// ensures that it is up to date when used by OrbitPredictors to show the orbits of the binary pair. 
    /// 
    /// If the CM has an OrbitUniversal attached, then it will let that OU move the CM. 
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="gravityState"></param>
    /// <param name="r_new"></param>
    /// <param name="v_new"></param>
	public override void Evolve(double physicsTime, GravityState gravityState, ref double[] r_new, ref double[] v_new, bool doCallbacks = true)
	{
        if (cmOrbit) {
            cmOrbit.Evolve(physicsTime, gravityState, ref r_new, ref v_new);
        } else {
            Vector3d pos;
            Vector3d vel = Vector3d.zero;
            if ((nbody1.engineRef == null) || (nbody2.engineRef == null)) {
                // first evolve during setup, bodies may not be added yet
                pos = new Vector3d(ge.UnmapFromScene(transform.position));
            } else {
                Vector3d pos1 = ge.GetPositionDoubleV3(nbody1);
                Vector3d pos2 = ge.GetPositionDoubleV3(nbody2);
                Vector3d vel1 = ge.GetVelocityDoubleV3(nbody1);
                Vector3d vel2 = ge.GetVelocityDoubleV3(nbody2);
                pos = mu1 * pos1 + mu2 * pos2;
                vel = mu1 * vel1 + mu2 * vel2;
            }
            r_new[0] = pos.x;
            r_new[1] = pos.y;
            r_new[2] = pos.z;
            v_new[0] = vel.x;
            v_new[1] = vel.y;
            v_new[2] = vel.z;
        }
	}

	/// <summary>
	/// Determine the mass to set for an orbit predictor. May be called during OP Start()
	/// before Setup has been run, so needs to get nbody info to be safe. 
	/// </summary>
	/// <param name="nbody"></param>
	/// <returns></returns>
	public double MassForPredictor(NBody nbody)
	{
		nbody1 = transform.GetChild(0).GetComponent<NBody>();
		nbody2 = transform.GetChild(1).GetComponent<NBody>();
		double m_total = (nbody1.mass + nbody2.mass);
		ge = GravityEngine.Instance();
		if (nbody == nbody1) {
			double mu2 = nbody2.mass / m_total;
			return mu2 * mu2 * ge.GetMass(nbody2);
		} else if (nbody == nbody2) {
			double mu1 = nbody1.mass / m_total;
			return mu1 * mu1 * ge.GetMass(nbody1);
		} else {
			Debug.LogError("Nbody is not part of binary");
			return 0;
		}
	}

	public override void ApplyScale(float scale)
	{
        // The orbit universal children will call InitNBody here and that will trigger orbit setup
	}

	public override void ApplyXZChange()
	{
		// The orbit universal children will call InitNBody here and that will trigger orbit setup

	}

	public override void InitNBody(float physicalScale, float massScale)
	{
		SetupOrbits();
        // need to unmap from scene units?
        if (!inOrbit) {
            cmNbody.SetPosVel3d(new Vector3d(transform.position), new Vector3d(velocity));
        }
    }

	public override string DumpInfo()
	{
		return string.Format("  BinaryOrbit: p={0:0.00} e={1:0.00}, i={2:0.00} Om={3:0.00} om={4:0.00}\n",
			p, eccentricity, inclination, omega_uc, omega_lc);
	}


#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
        // can't setup if playing - will revert orbit to initial position
        if (Application.isPlaying)
            return;
        
        //  The children will draw their own orbits
        SetupOrbits();
	}
#endif

}
