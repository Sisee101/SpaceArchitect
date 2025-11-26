using UnityEngine;
using System.Collections;

public class InverseR : IForceDelegate {

	// re-use so that there is only one allocation
	private double[] a_ij = new double[] { 0, 0, 0 };

	public double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		a_ij[0] = -rji[0] / r2;
		a_ij[1] = -rji[1] / r2;
		a_ij[2] = -rji[2] / r2;
		return a_ij;
	}

	public double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		a_ij[0] = rji[0] / r2;
		a_ij[1] = rji[1] / r2;
		a_ij[2] = rji[2] / r2;
		return a_ij;
	}

	public double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2;
		// re-use aPerM
		a_ij[0] = - rji[0] / r3;
		a_ij[1] = - rji[1] / r3;
		a_ij[2] = - rji[2] / r3;
		return a_ij;
	}
}
