using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectiveNewton : SelectiveForceBase {

	// re-use so that there is only one allocation
	private double[] a_ij = new double[] { 0, 0, 0 };

	private const double EPSILON = 1E-4;    // minimum distance for gravitatonal force

	public override double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2 + EPSILON;
		a_ij[0] = rji[0] / r3;
		a_ij[1] = rji[1] / r3;
		a_ij[2] = rji[2] / r3;
		return a_ij;
	}

	public override double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2 + EPSILON;
		a_ij[0] = rji[0] / r3;
		a_ij[1] = rji[1] / r3;
		a_ij[2] = rji[2] / r3;
		return a_ij;
	}

	public override double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
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
