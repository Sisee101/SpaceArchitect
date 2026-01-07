using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Standard Leapfrog algorithm 
// This is vastly better than the standard Euler approach ( x = x_0 + v dt ) because it is energy conserving
// (in the lingo "symplectic"). 
//
// Mark as sealed - may improve performance depending on compiler...

public sealed class LeapfrogIntegrator : INBodyIntegrator {

	private double dt; 
	private int numBodies; 
	private int maxBodies; 
	
	// per body physical parameters. Second index is the dimension. 
	// These are for massive bodies with interactions and NOT particles
	private double[,] a; 
	
	private double initialEnergy; 
	
	private const double EPSILON = 1E-4; 	// minimum distance for gravitatonal force

	// working variable for Evolve - allocate once
	private double[] rji;
	private double[] a_ij;

	private IForceDelegate forceDelegate;

	private GEExternalAcceleration[] externalAccel;

	/// <summary>
	/// Initializes a new instance of the <see cref="LeapfrogIntegrator"/> class.
	/// An optional force delegate can be provided if non-Newtonian gravity is 
	/// desired.
	/// </summary>
	/// <param name="force">Force.</param>
	public LeapfrogIntegrator(IForceDelegate force) {

		forceDelegate = force;
	}

	/// <summary>
	/// Setup the specified maxBodies and timeStep.
	/// </summary>
	/// <param name="maxBodies">Max bodies.</param>
	/// <param name="timeStep">Time step.</param>
	public void Setup(int maxBodies, double timeStep) {
		dt = timeStep; 
		numBodies = 0; 
		this.maxBodies = maxBodies;
		
		a = new double[maxBodies,GravityState.NDIM];

		rji = new double[GravityState.NDIM];

        externalAccel = new GEExternalAcceleration[maxBodies];

    }

    public void Clear() {
        numBodies = 0; 
    }

	// Clone this integrator and copy across internal state
	public INBodyIntegrator DeepClone() {
		LeapfrogIntegrator clone = new LeapfrogIntegrator(forceDelegate);
		clone.Setup(maxBodies, dt);
		for (int i=0; i < maxBodies; i++) {
			for (int j=0; j < GravityState.NDIM; j++) {
				clone.a[i,j] = a[i,j];
			}
			clone.externalAccel[i] = externalAccel[i];
		}
		clone.numBodies = numBodies;
		return clone;
	}

	public void AddNBody( int bodyNum, NBody nbody, GravityState.NbodyState[] nbodyStates) {

		if (numBodies > maxBodies) {
			Debug.LogError("Added more than maximum allocated bodies! max=" + maxBodies);
			return;
		}
		if (bodyNum != numBodies) {
			Debug.LogError("Body numbers are out of sync in integrator=" + numBodies + " GE=" + bodyNum);
			return;
		}
		// r,v,m already in GravityEngine

        // check for engine
        GEExternalAcceleration ext_accel = nbody.GetComponent<GEExternalAcceleration>();
        if (ext_accel != null) {
            externalAccel[numBodies] = ext_accel;
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("Added GEExternalAcceleration engine for " + nbody.gameObject);
#pragma warning restore 162
        }
		PreEvolveForAdd(nbodyStates, bodyNum);
		numBodies++;		
	}
	
	public void RemoveBodyAtIndex(int atIndex) {
	
		// shuffle the rest up + internal info
		for( int j=atIndex; j < (numBodies-1); j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				a[j,k] = a[j+1, k]; 
			}	
			externalAccel[j] = externalAccel[j+1];
		}
		numBodies--; 
	
	}

	public void GrowArrays(int growBy) {
		double[,] a_copy = new double[maxBodies, GravityState.NDIM];  
		GEExternalAcceleration[] externalAccelerations_copy = new GEExternalAcceleration[maxBodies];

		for( int j=0; j < numBodies; j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				a_copy[j,k] = a[j, k]; 
			}
			externalAccelerations_copy[j] = externalAccel[j];
		}
		a = new double[maxBodies+growBy, GravityState.NDIM];
		externalAccel = new GEExternalAcceleration[maxBodies+growBy];

		for( int j=0; j < numBodies; j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				a[j,k] = a_copy[j, k]; 
			}
			externalAccel[j] = externalAccelerations_copy[j];
		}
		maxBodies += growBy;
	}

    public Vector3d GetAccelerationForIndex(int i) {
		return new Vector3d( a[i,0], a[i,1], a[i,2]);
	}

    public string GetExternalAccelForIndex(int i) {
        string s = "none";
        if (externalAccel[i] != null)
            s = externalAccel.ToString();
        return s;
    }

	public void PreEvolveForAdd(GravityState.NbodyState[] nbodyStates, int n)
	{
		// Precalc initial acceleration
		double[] rji = new double[GravityState.NDIM];
		double r2;
		double r3;

		a[n, 0] = 0.0;
		a[n, 1] = 0.0;
		a[n, 2] = 0.0;
		for (int i = 0; i < n; i++) {
			r2 = 0;
			rji[0] = nbodyStates[n].r_x - nbodyStates[i].r_x;
			rji[1] = nbodyStates[n].r_y - nbodyStates[i].r_y;
			rji[2] = nbodyStates[n].r_z - nbodyStates[i].r_z;
			r2 += rji[0] * rji[0];
			r2 += rji[1] * rji[1];
			r2 += rji[2] * rji[2];
			if (forceDelegate == null) {
				r3 = r2 * System.Math.Sqrt(r2) + EPSILON;
				for (int k = 0; k < GravityState.NDIM; k++) {
					a[n, k] -= nbodyStates[i].m * rji[k] / r3;
				}
			} else {
				a_ij = forceDelegate.CalcAccelerationIJPerM(rji, i, n, nbodyStates);
				for (int k = 0; k < GravityState.NDIM; k++) {
					a[n, k] -= nbodyStates[i].m * a_ij[k];
				}
			}
		}
	}

	public void PreEvolve(GravityState gravityState) {

		GravityState.NbodyState[] nbodyStates = gravityState.GetNbodyStates();

		// Precalc initial acceleration
		double[] rji = new double[GravityState.NDIM]; 
		double r2; 
		double r3; 

		for (int i=0; i < numBodies; i++) {
			a[i,0] = 0.0;
			a[i,1] = 0.0;
			a[i,2] = 0.0;
		}
		for (int i=0; i < numBodies; i++) {
			for (int j=i+1; j < numBodies; j++) {
				r2 = 0; 
				rji[0] = nbodyStates[j].r_x- nbodyStates[i].r_x;
				rji[1] = nbodyStates[j].r_y - nbodyStates[i].r_y;
				rji[2] = nbodyStates[j].r_z - nbodyStates[i].r_z;
				r2 += rji[0] * rji[0];
				r2 += rji[1] * rji[1];
				r2 += rji[2] * rji[2];
				if (forceDelegate == null) {
				r3 = r2 * System.Math.Sqrt(r2) + EPSILON; 
					for (int k=0; k < GravityState.NDIM; k++) {
						a[i,k] += nbodyStates[j].m * rji[k]/r3; 
						a[j,k] -= nbodyStates[i].m * rji[k]/r3;
					}
				} else {
					a_ij = forceDelegate.CalcAccelerationIJPerM(rji, i, j, nbodyStates);
					for (int k=0; k < GravityState.NDIM; k++) {
						a[i,k] -= nbodyStates[j].m * a_ij[k];
						a[j,k] += nbodyStates[i].m * a_ij[k];
					}
				}
			}
		}	
		
		initialEnergy = NUtils.GetEnergy(numBodies, nbodyStates);
	}
			
	public float GetEnergy(GravityState gravityState) {
		return (float) NUtils.GetEnergy(numBodies, gravityState.GetNbodyStates() );
	}

	public float GetInitialEnergy(GravityState gravityState) {
		return (float) initialEnergy;
	}


	public double Evolve(double time, GravityState gs, bool exactTime, bool reversing) {

		if (forceDelegate != null) {
			return EvolveForceDelegate(time, gs);
		}
		int numSteps = 0;

        double timeNow = gs.GetPhysicsTime();

		double hackDt = dt; // preserve

		GravityState.NbodyState[] nb = gs.GetNbodyStates();

		// If objects are fixed want to use their mass but not update their position
		// Better to calc their acceleration and ignore than add an if statement to core loop. 
		double t = 0;
		while ((t < time) && (numSteps < 100)) {
			if (exactTime && ((t + dt) > time)) {
				dt = time - t;
			}
			t += dt;
			numSteps++;
			// Update v and r
			for (int i=0; i < numBodies; i++) {
				if (nb[i].isActive && !nb[i].isFixed) {
					nb[i].v_x += a[i,0] * 0.5 * dt;
					nb[i].r_x += nb[i].v_x * dt;
					nb[i].v_y += a[i,1] * 0.5 * dt;
					nb[i].r_y += nb[i].v_y * dt;
					nb[i].v_z += a[i,2] * 0.5 * dt;
					nb[i].r_z += nb[i].v_z * dt;
				}				
			}
			// advance acceleration
			double r2; 
			double r3;
            double dummy = 0;

			// a = 0 or init with eternal value
			double t_accel = reversing ? (timeNow - t) : (timeNow + t);
			for (int i=0; i < numBodies; i++) {
	            if ((externalAccel[i] != null) && (nb[i].isActive && !nb[i].isFixed)) {
	                double[] e_accel = externalAccel[i].acceleration(t_accel, gs, ref dummy);
	                a[i, 0] = e_accel[0];
	                a[i, 1] = e_accel[1];
	                a[i, 2] = e_accel[2];
				} else {
					a[i,0] = 0.0;
					a[i,1] = 0.0;
					a[i,2] = 0.0;
				}
			}
			// calc a
			for (int i=0; i < numBodies; i++) {
			   if (nb[i].isActive) {					
			      for (int j=i+1; j < numBodies; j++) {
					 if (nb[j].isActive && !(nb[i].massless && nb[j].massless)) { 
					 	// O(N^2) in here, unpack loops to optimize				
						r2 = 0; 
						rji[0] = nb[j].r_x - nb[i].r_x;
						r2 += rji[0] * rji[0]; 
						rji[1] = nb[j].r_y - nb[i].r_y;
						r2 += rji[1] * rji[1]; 
						rji[2] = nb[j].r_z - nb[i].r_z;
						r2 += rji[2] * rji[2]; 
						r3 = r2 * System.Math.Sqrt(r2) + EPSILON;
						a[i,0] += nb[j].m * rji[0]/r3; 
						a[j,0] -= nb[i].m * rji[0]/r3;
						a[i,1] += nb[j].m * rji[1]/r3; 
						a[j,1] -= nb[i].m * rji[1]/r3;
						a[i,2] += nb[j].m * rji[2]/r3; 
						a[j,2] -= nb[i].m * rji[2]/r3;
					 }
			      }
			   }
			}
			// update velocity
			for (int i=0; i < numBodies; i++) {
				if (i == 2) {
					double amag = Mathd.Sqrt(a[i, 0] * a[i, 0] + a[i, 1] * a[i, 1] + a[i, 2] * a[i, 2]);
				}
				if (!nb[i].isFixed) {
					//Debug.LogFormat("REMOVE i={0} a={1}", i, System.Math.Sqrt(a[i, 0] * a[i, 0] + a[i, 1] * a[i, 1] + a[i, 2] * a[i, 2]));
					nb[i].v_x += a[i,0] * 0.5 * dt;
					nb[i].v_y += a[i,1] * 0.5 * dt;
					nb[i].v_z += a[i,2] * 0.5 * dt;
				}
			}

		}
		dt = hackDt; // restore
		return t;		
	}
	

	/// <summary>
	/// Evolves using the force delegate. Internals differ slightly and for effeciency do not want
	/// a conditional on forceDelegate in the inner loop. 
	///
	/// </summary>
	/// <returns>The force delegate.</returns>
	/// <param name="time">Time.</param>
	/// <param name="m">M.</param>
	/// <param name="r">The red component.</param>
	/// <param name="info">Info.</param>
	/// 
	public double EvolveForceDelegate(double time, GravityState gs)
	{

		int numSteps = 0;

		double timeNow = gs.GetPhysicsTime();

		GravityState.NbodyState[] nb = gs.GetNbodyStates();

		// If objects are fixed want to use their mass but not update their position
		// Better to calc their acceleration and ignore than add an if statement to core loop. 
		for (double t = 0; t < time; t += dt) {
			numSteps++;
			// Update v and r
			for (int i = 0; i < numBodies; i++) {
				if (nb[i].isActive && !nb[i].isFixed) {
					nb[i].v_x += a[i, 0] * 0.5 * dt;
					nb[i].r_x += nb[i].v_x * dt;
					nb[i].v_y += a[i, 1] * 0.5 * dt;
					nb[i].r_y += nb[i].v_y * dt;
					nb[i].v_z += a[i, 2] * 0.5 * dt;
					nb[i].r_z += nb[i].v_z * dt;
				}
			}
			// advance acceleration
			double dummy = 0;

			// a = 0 or init with eternal value
			for (int i = 0; i < numBodies; i++) {
				if ((externalAccel[i] != null) && (nb[i].isActive && !nb[i].isFixed)) {
					double[] e_accel = externalAccel[i].acceleration(timeNow + t, gs, ref dummy);
					a[i, 0] = e_accel[0];
					a[i, 1] = e_accel[1];
					a[i, 2] = e_accel[2];
				} else {
					a[i, 0] = 0.0;
					a[i, 1] = 0.0;
					a[i, 2] = 0.0;
				}
			}
			// calc a
			for (int i = 0; i < numBodies; i++) {
				if (nb[i].isActive) {
					for (int j = i + 1; j < numBodies; j++) {
						if (nb[j].isActive && !(nb[i].massless && nb[j].massless)) {
							// O(N^2) in here, unpack loops to optimize				
							rji[0] = nb[j].r_x - nb[i].r_x;
							rji[1] = nb[j].r_y - nb[i].r_y;
							rji[2] = nb[j].r_z - nb[i].r_z;
							a_ij = forceDelegate.CalcAccelerationIJPerM(rji, i, j, nb);
							a[i, 0] -= nb[j].m * a_ij[0];
							a[i, 1] -= nb[j].m * a_ij[1];
							a[i, 2] -= nb[j].m * a_ij[2];

							a[j, 0] += nb[i].m * a_ij[0];
							a[j, 1] += nb[i].m * a_ij[1];
							a[j, 2] += nb[i].m * a_ij[2];
						}
					}
				}
			}
			// update velocity
			for (int i = 0; i < numBodies; i++) {
				if (!nb[i].isFixed) {
					nb[i].v_x += a[i, 0] * 0.5 * dt;
					nb[i].v_y += a[i, 1] * 0.5 * dt;
					nb[i].v_z += a[i, 2] * 0.5 * dt;
				}
			}
			// coll_time code
		}
		return (numSteps * dt);


	}

}
