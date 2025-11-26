using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Force model for the first order approximation for a non-spherical gravitational source. 
/// 
/// The model requires the value of the radius of the planet. The default units are for the Earth in 
/// orbital units. 
/// 
/// </summary>
/// 

public class J2Gravity : MonoBehaviour, IForceDelegate
{
	private double[] a_ij = new double[] { 0, 0, 0 };

	private const double EPSILON = 1E-4;    // minimum distance for gravitatonal force

	public double[] CalcAccelerationIJPerM(double[] rji, int i, int j, GravityState.NbodyState[] nbodyStates)
	{
		// pertubation force due to J2 
		Vector3d fp = Vector3d.zero;
		Vector3d e;
		Vector3d n = new Vector3d(ref rji).normalized;
		double r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
		double r3 = Mathd.Sqrt(r2) * r2 + EPSILON;
		J2ForceData j2Data = null;

		a_ij[0] = -rji[0] / r3;
		a_ij[1] = -rji[1] / r3;
		a_ij[2] = -rji[2] / r3;
		// Is there a J2 pertubation?
		if (nbodyStates[i].forceData != null) {
			j2Data = (J2ForceData)nbodyStates[i].forceData;
		}
		if (nbodyStates[j].forceData != null) {
			j2Data = (J2ForceData)nbodyStates[j].forceData;
			n *= -1; 
		}
		if (j2Data != null) {
				e = new Vector3d(j2Data.axis);
				double R = j2Data.GetRScaled();
				double fp_mag = 1.5 * j2Data.J2 * R * R / (r2 * r2);
				double eDotn = Vector3d.Dot(e, n);
				fp = fp_mag * ((5 * eDotn * eDotn - 1.0) * n - 2 * eDotn * e);
			if (nbodyStates[i].forceData != null) {
				// i perturbs j
				//Debug.LogFormat("a_ij=({0},{1},{2}) fp={3}", a_ij[0], a_ij[1], a_ij[2], fp);
				a_ij[0] += fp.x;
				a_ij[1] += fp.y;
				a_ij[2] += fp.z;
			} 

		}
		return a_ij;
	}

	public double[] CalcAccelerationIPerParticle(double[] rji, int i, GravityState.NbodyState[] nbodyStates)
	{
		if (nbodyStates[i].forceData != null) {
		}

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
