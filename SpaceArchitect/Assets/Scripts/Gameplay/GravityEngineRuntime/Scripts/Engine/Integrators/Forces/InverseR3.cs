using UnityEngine;
using System.Collections;

public class InverseR3 : IForceDelegate {
	// re-use so that there is only one allocation
	private double[] a_ij = new double[] { 0, 0, 0 };

	private const double EPSILON = 1E-4;    // minimum distance for gravitatonal force

	public double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r4 = r2 * r2 + EPSILON;
		a_ij[0] = -rji[0] / r4;
		a_ij[1] = -rji[1] / r4;
		a_ij[2] = -rji[2] / r4;
		
		return a_ij;
	}

	public double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r4 = r2 * r2 + EPSILON;
		a_ij[0] = rji[0] / r4;
		a_ij[1] = rji[1] / r4;
		a_ij[2] = rji[2] / r4;
		return a_ij;
	}

	public double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r5 = r2 * r2 * Mathd.Sqrt(r2) + EPSILON;
		// re-use aPerM
		a_ij[0] = -3.0 * rji[0] / r5;
		a_ij[1] = -3.0 * rji[1] / r5;
		a_ij[2] = -3.0 * rji[2] / r5;
		return a_ij;
	}
}
