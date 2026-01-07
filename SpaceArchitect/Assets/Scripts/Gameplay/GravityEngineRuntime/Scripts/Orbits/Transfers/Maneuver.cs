using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manuever.
/// Holds a future course change for the spaceship. Will be triggered based on world time.
/// 
/// In some cases (orbital transfers) the value of the change will be recorded as a
/// scalar. In trajectory intercept cases, a vector velocity change will be provided.
/// 
/// Manuevers are added to the GE. This allows them to be run at the closest possible time
/// step (in general GE will do multiple time steps per FixedUpdate). Due to time precision
/// the resulting trajectory may not be exactly as desired. More timesteps in the GE will
/// reduce this error. 
/// 
/// Manuevers support sorting based on earliest worldTime. 
/// 
/// Maneuvers are always expressed in internal physics units of distance and velocity. 
/// 
/// </summary>
public class Maneuver : IComparer<Maneuver>  {

	//! time at which the maneuver is to occur (physical time in GE)
	public double worldTime;

	//! velocity change OR the velocity to be set (depending on the mtype: vector or setv). This field is BADLY named!
	public Vector3 velChange;

	//! scalar value of the velocity change in physics units (+ve means in-line with motion). Use GetDvScaled() for scaled value.    		
	public float dV;

	//! NBody to apply the course correction to
	public NBody nbody;

    //! Position of the maneuver in internal physics units (used for error estimates and ManeuverRenderer)
    public Vector3d physPosition;

    //! vector representing the change in velocity for the maneuver
    public Vector3d dVvector;

    /// <summary>
    /// RelativePos is used when a maneuver is relative to a body it is orbiting. This allows transfer code to 
    /// program the relative maneuver without taking into account e.g. the velocity of the center body at time of arrival
    /// (which may have shifted over the duration of the transfer)
    /// </summary>
    public NBody relativeTo;

    /// <summary>
    /// RelativePos/Vel are used to define the r/v for use when a maneuver is mapped into a KeplerSequence or 
    /// used to redefine a Kepler mode orbit.
    /// 
    /// If relativeTo is set and relativePos is zero can assume no relative pos/vel info present.
    /// </summary>
    public Vector3d relativePos;
    public Vector3d relativeVel;

    /// <summary>
    /// Type of maneuver:
    /// vector: apply a dV vector
    /// scalar: apply the scalar dV amount to the vector at time of maneuver
    /// setv: set the absolute velocity at the time of the maneuver
    /// </summary>
    public enum Mtype {vector, scalar, setv, none};

	//! Type of information about manuever provided
	public Mtype mtype = Mtype.vector;

    //! Label used in debug logging
    public string label = ""; 

    //! template for the callback to be run when the maneuver is executed
	public delegate void OnExecuted(Maneuver m); 

	//! Delegate to be called when the maneuver is executed (optional)
	public OnExecuted onExecuted;

    //! Delegate to be called immediatly before the maneuver is executed (optional)
    public OnExecuted beforeExecuted;

    //! generic pointer that can be used to hold data for callbacks
    public System.Object opaqueData;

    //! For SGP4/PKepler evolving ships, the transfer phase of the maneuver needs to be KEPLER to ensure Lambert etc. 
    //! works properly. The ship then switches back to SGP4/Kepler evolution on the basis of the Sgp4TransferMode.
    public enum RailsTransferMode { NONE, KEPLER, PKEPLER, SGP4};

    public RailsTransferMode railsXferMode = RailsTransferMode.NONE;

    /// <summary>
    /// Create a vector maneuver at the intercept point to match trajectory
    /// that was intercepted. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="intercept"></param>
	public Maneuver(NBody nbody, TrajectoryData.Intercept intercept) {
		this.nbody = nbody;
        mtype = Mtype.vector;
		worldTime = intercept.tp1.t;
		velChange = intercept.tp2.v - intercept.tp1.v;
		// r = intercept.tp1.r;
		dV = intercept.dV;
	}

	// Empty constructor when caller wishes to fill in field by field
	public Maneuver() {

	}

    public Maneuver(Maneuver m)
    {
        this.label = m.label;
        this.onExecuted = m.onExecuted;
        this.beforeExecuted = m.beforeExecuted;
        this.opaqueData = m.opaqueData;
        this.mtype = m.mtype;

        this.relativePos = m.relativePos;
        this.relativeTo = m.relativeTo;
        this.relativeVel = m.relativeVel;

        this.dV = m.dV;
        this.velChange = m.velChange;
        this.worldTime = m.worldTime;
        this.physPosition = m.physPosition;
        this.nbody = m.nbody;

    }

    public bool HasRelativePosVel()
    {
        return !((relativePos.x == 0.0) && (relativePos.y == 0) && (relativePos.z == 0.0));
    }

    /// <summary>
    /// Return the dV in scaled units. 
    /// </summary>
    /// <returns></returns>
    public float GetDvScaled() {
        return dV / GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the deltaV for a scalar maneuver in world units (e.g. in ORBITAL
    /// units set velocity in km/hr)
    /// </summary>
    /// <param name="newDv"></param>
    public void SetDvScaled(float newDv) {
        dV = newDv * GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the velocity change vector in world units (e.g. in ORBITAL
    /// units set velocity in km/hr)
    /// </summary>
    /// <param name="newVel"></param>
    public void SetVelScaled(Vector3 newVel) {
        velChange = newVel * GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the maneuver time in world units (e.g. in ORBITAL units in 
    /// hours). 
    /// </summary>
    /// <param name="time"></param>
    public void SetTimeScaled(float time) {
        worldTime = time / GravityScaler.GetGameSecondPerPhysicsSecond();
    }

    /// <summary>
    /// Execute the maneuver. Called automatically by Gravity Engine for maneuvers that
    /// have been added to the GE via AddManeuver(). 
    /// 
    /// Unusual to call this method directly. 
    /// </summary>
    /// <param name="ge"></param>
    public void Execute(GravityState gs)
    {
        Vector3d vel = gs.GetVelocity3d(nbody);
        Vector3d centerVel = Vector3d.zero;
        // If the maneuver is relative to a body, need to subtract it's velocity from the current velocity vector of the
        // object and re-add after. 
        if (relativeTo != null) {
            centerVel = gs.GetVelocity3d(relativeTo);
            vel -= centerVel;
        }
        Vector3d oldVel = vel;

        switch (mtype)
        {
            case Mtype.vector:
                vel += new Vector3d(velChange[0], velChange[1], velChange[2]);
                break;

            case Mtype.scalar:
                // scalar: adjust existing velocity by dV
                Vector3d change = vel.normalized * dV ;
                vel += change;
                break;

            case Mtype.setv:
                // direct assignment
                vel = new Vector3d(velChange[0], velChange[1], velChange[2]);
                break;

            case Mtype.none:
                break;
        }
        // Interpolate the position for the maneuver. 
        Vector3d rAtManeuver = gs.GetPhysicsPositionDoubleV3(nbody);
#pragma warning disable 162        // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            if (!gs.isAsync) {
                Debug.LogFormat("Applied manuever: {0} at t={1} engineRef.index={2}  bodyType={3}  timeError={4}",
                     LogString() , gs.time, nbody.engineRef.index, nbody.engineRef.bodyType ,(worldTime - gs.time));
                double dV = Mathd.Sqrt((vel[0] - oldVel[0]) * (vel[0] - oldVel[0]) +
                                        (vel[1] - oldVel[1]) * (vel[1] - oldVel[1]) +
                                        (vel[2] - oldVel[2]) * (vel[2] - oldVel[2]));
                Debug.LogFormat("r(absolute)={0} oldv=({1},{2},{3}) v=({4},{5},{6}) dv actual=({7},{8},{9}) |dV|={10}", 
                            rAtManeuver.magnitude,
                            oldVel[0], oldVel[1], oldVel[2],
                            vel[0], vel[1],vel[2], vel[0]-oldVel[0], vel[1]-oldVel[1], vel[2]-oldVel[2], dV);
                // check if phyPos is ok
                double deltaPos = (physPosition - rAtManeuver).magnitude;
                double relDelta = float.NaN;
                if (relativeTo != null) {
                    Vector3d rRelActual = rAtManeuver - gs.GetPhysicsPositionDoubleV3(relativeTo);
                    relDelta = (relativePos - rAtManeuver).magnitude;
                    Debug.LogFormat("{0} |RelPos| = {1} Pos delta = {2} relDelta={3} centerVel={4}", label, rRelActual.magnitude, deltaPos, relDelta, centerVel);
                }
            }
        }
#pragma warning restore 162        // enable unreachable code warning
        vel += centerVel;
        // Transfers of non-Kepler rails mode will need to update evolve mode
        // Kseq will handle this for itself
        if (nbody.GetComponent<KeplerSequence>() == null) {
            if (railsXferMode != RailsTransferMode.NONE) {
                OrbitUniversal orbit = nbody.GetComponent<OrbitUniversal>();
                if (orbit != null) {
                    if (railsXferMode == RailsTransferMode.KEPLER)
                        orbit.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
                    else if (railsXferMode == RailsTransferMode.PKEPLER)
                        orbit.evolveMode = OrbitUniversal.EvolveMode.PKEPLER_J2;
                    else if (railsXferMode == RailsTransferMode.SGP4)
                        orbit.evolveMode = OrbitUniversal.EvolveMode.SGP4_PROPAGATOR;
                }
            }
        }
        gs.UpdatePositionAndVelocity(nbody, rAtManeuver, vel, this);
    }

	public int Compare(Maneuver m1, Maneuver m2) {
			if (m1 == m2) {
				return 0;
			}
			if (m1.worldTime < m2.worldTime) {
				return -1;
			}
			return 1;
	}

	public string LogString() {
		return string.Format("Maneuver {0} {1} t={2} type={3} dV={4} velChange=({5},{6},{7})", label, nbody.gameObject.name, worldTime, mtype, dV, 
                        velChange.x, velChange.y, velChange.z);
	}

}
