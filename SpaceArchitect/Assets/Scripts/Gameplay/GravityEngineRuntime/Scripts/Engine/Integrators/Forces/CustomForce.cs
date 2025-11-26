using UnityEngine;
using System.Collections;

/// <summary>
/// Custom force.
/// Sample code to show how to make a custom force. To use this
/// set the GE force delegate to custom and attach this script
/// to the object holding the GravityEngine
/// </summary>
public class CustomForce : MonoBehaviour, IForceDelegate  {

	public float a = 2.0f;
	public float b = 1.0f;

	/// <summary>
	/// acceleration = a * ln(b * r)
	/// </summary>
	/// <returns>The accel.</returns>
	/// <param name="r_sep">R sep. The distance between the bodies</param>
    /// <param name="i">index of one pair in force (i < j)</param>
    /// <param name="j">index of second pair in force</param>
	public double CalcPseudoForce(double r_sep, int i, int j, GravityState.NbodyState[] nbodyStates) {

		return a*System.Math.Log(b*r_sep);
	}

    public double CalcPseudoForceDot(double r_sep, int i, int j, GravityState.NbodyState[] nbodyStates) {
		return a * b/r_sep;
	}

	// re-use so that there is only one allocation
	private double[] a_ij = new double[] { 0, 0, 0 };

	public double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r = Mathd.Sqrt(r2);
		double f = a * Mathd.Log(b * r);
		a_ij[0] = -f * rji[0] / r;
		a_ij[1] = -f * rji[1] / r;
		a_ij[2] = -f * rji[2] / r;
		return a_ij;
	}

	public double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r = Mathd.Sqrt(r2);
		double f = a * Mathd.Log(b * r);
		a_ij[0] = f * rji[0] / r;
		a_ij[1] = f * rji[1] / r;
		a_ij[2] = f * rji[2] / r;
		return a_ij;
	}

	public double[] CalcJerkIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r = Mathd.Sqrt(r2);
		double f = a * b / r;
		a_ij[0] = f * rji[0] / r;
		a_ij[1] = f * rji[1] / r;
		a_ij[2] = f * rji[2] / r;
		return a_ij;
	}
}
