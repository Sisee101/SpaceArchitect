using UnityEngine;
using System.Collections;

/// <summary>
/// Newton force.
/// This is not generally used - since Netwonian gravity is the more efficient default
/// force built in to the integrators. 
///
/// This code is used to double check the force delegate code
/// </summary>
public class NewtonForce : MonoBehaviour, IForceDelegate {

	// re-use so that there is only one allocation
	private double[] a_ij = new double[] { 0, 0, 0 };

	private const double EPSILON = 1E-4;    // minimum distance for gravitatonal force

	public double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2 + EPSILON;
		a_ij[0] = -rji[0] / r3;
		a_ij[1] = -rji[1] / r3;
		a_ij[2] = -rji[2] / r3;
		return a_ij;
	}

	public double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2 + EPSILON;
		a_ij[0] = rji[0] / r3;
		a_ij[1] = rji[1] / r3;
		a_ij[2] = rji[2] / r3;
		return a_ij;
	}

	public double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r4 = r2 * r2 + EPSILON;
		// re-use aPerM
		a_ij[0] = -2.0 * rji[0] / r4;
		a_ij[1] = -2.0 * rji[1] / r4;
		a_ij[2] = -2.0 * rji[2] / r4;
		return a_ij;
	}
	

}
