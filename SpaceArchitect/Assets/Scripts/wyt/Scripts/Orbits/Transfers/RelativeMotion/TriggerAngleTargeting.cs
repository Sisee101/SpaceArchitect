using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine the burn required for a specific trigger angle for rendezvous. 
/// 
/// Apollo used a rendezvous burn that was directed along the line of sight from the
/// ship to the target. The trigger angle was the angle above the ship direction of flight
/// to the target. 
/// 
/// This burn requires that the ship and target be in co-elliptic orbits (i.e. same argument of perigee and 
/// inclination). 
/// 
/// The algorithm is from: Trigger Angle Targeting for Orbital Rendezvous, Woffinden, Rose and Geller (2008)
///                        Journal of the Astronautical Sciences Vol 56. No 4 pp.495-513.
///                        
/// The algorithm uses relative co-ordinates in which the target is at the origin in x, y, z. It is suitable for
/// cases where the differences in altitude are small compared to the radius of the orbits. 
///   
/// There is a window of possible trigger angles from approx. 26.7 degrees to 125.2. In some cases there are
/// two possible burns that result in different transfer phase angles (see Fig 3 in the paper). 
/// 
/// </summary>
public class TriggerAngleTargeting 
{
    public static double triggerAngle; 

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="target"></param>
    /// <param name="planet"></param>
    /// <param name="z0">Difference in Altitide</param>
    /// <param name="theta">Orbital transfer angle</param>
    /// <param name="xf">X offset to target at closest approach (0 to meet target)</param>
    /// <returns></returns>
    public static Vector3d ComputeBurnOld(double z0, double omega, double thetaDeg, double xf, Vector3d Omega) {
        double[] result = new double[] { double.NaN, double.NaN };

        double theta = thetaDeg * Mathd.Deg2Rad;
        double angle = ComputeAngleRadians(z0, theta, xf);
        triggerAngle = angle * Mathd.Rad2Deg;
        result[0] = angle * Mathd.Rad2Deg;
        if (double.IsNaN(angle)) {
            Debug.LogWarning("Could not solve for angle, check range of theta. ");
            return Vector3d.zero; 
        }

        // At point when burn is applied can determine x0 from angle and z0
        // tan(theta) = z0/x0
        // assuming target is below and behind
        double x0 =-z0/Mathd.Tan(angle);
        Debug.LogFormat("x0={0} z0={1}", x0, z0);

        double s_theta = Mathd.Sin(theta);
        double c_theta = Mathd.Cos(theta);
        // Use equation (8) and the angle to solve for the magnitude of deltaV
        // Algorithm assumes there is no out of plane component. Eqn dumped through Maple and solved for the
        // deltaV x-component. 
        double vx = (s_theta * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta - 3 * s_theta * theta 
                        - 8 * c_theta + 4) * (xf - x0 - (6 * s_theta - 6 * theta) * z0)) 
                        + (2 * (c_theta - 1) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta 
                        - 3 * s_theta * theta - 8 * c_theta + 4) * (4 - 3 * c_theta) * z0) 
                        + 0.3e1 / 0.2e1 * omega * z0;

        double vz =  2 * (c_theta - 1) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta 
                        - 3 * s_theta * theta - 8 * c_theta + 4) * (xf - x0 - (6 * s_theta - 6 * theta) * z0) 
                        - (4 * s_theta - 3 * theta) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta 
                        - 3 * s_theta * theta - 8 * c_theta + 4) * (4 - 3 * c_theta) * z0;

        double v0plus_x = s_theta * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta - 3 * s_theta * theta - 8 * c_theta + 4) * (x0 + (6 * s_theta - 6 * theta) * z0) - 2 * (c_theta - 1) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta - 3 * s_theta * theta - 8 * c_theta + 4) * (4 - 3 * c_theta) * z0;
        double v0plus_z = 2 * (c_theta - 1) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta - 3 * s_theta * theta - 8 * c_theta + 4) * (x0 + (6 * s_theta - 6 * theta) * z0) + (4 * s_theta - 3 * theta) * omega / (4 * c_theta * c_theta + 4 * s_theta * s_theta - 3 * s_theta * theta - 8 * c_theta + 4) * (4 - 3 * c_theta) * z0;


        double burnMag = Mathd.Sqrt(vx * vx + vz * vz);
        Debug.LogFormat("TAT: angle={0} v0plus=({1}, 0, {2})  x0={4} z0={5} posAngle={6} dv_x={7} omega={8}", 
            angle*Mathd.Rad2Deg, v0plus_x, v0plus_z, Mathd.Atan2(vz, vx)*Mathd.Rad2Deg, 
            x0, z0, Mathd.Atan2(z0, -x0)*Mathd.Rad2Deg, -1.5*omega*z0, omega);
        return Vector3d.zero;
    }


    public static double ComputeAngleRadians(double z0, double thetaRad, double xf) { 

        double s_theta = Mathd.Sin(thetaRad);
        double c_theta = Mathd.Cos(thetaRad);
        // Eqn (15)
        double alpha = 6.0 * s_theta * (s_theta - thetaRad) + 2.0 * (1.0 - c_theta) * (4.0 - 3.0 * c_theta);
        double beta = s_theta - 12.0 * (c_theta - 1.0) * (s_theta - thetaRad) - 
                            (4.0 - 3.0 * c_theta) * (4.0 * s_theta - 3.0 * thetaRad);
        double gamma = 2.0 * (1 - c_theta);
        double d = 4.0 * s_theta * s_theta - 3.0 * s_theta * thetaRad + 4.0 * (1.0 - c_theta) * (1.0 - c_theta);

        // eqn (19)
        double kappa = xf / z0; 
        double b = beta - kappa * gamma;
        double c = alpha - 1.5 * d - kappa * s_theta; 
        double[] xi = PolynomialSolver.Quadratic(gamma, b, c);
        double e_b = Mathd.Atan(1 / xi[0]);
        double e_b2 = Mathd.Atan(1 / xi[1]);
        Debug.LogFormat("xi[0]={0} xi[1]={1} e_b1={2} deg e_b2={3}", xi[0], xi[1], e_b * Mathd.Rad2Deg, e_b2 * Mathd.Rad2Deg);
        return e_b;
    }

    /// <summary>
    /// Given a ship and a target in coplanar circular orbits and a desired trigger angle, determine the magnitude of
    /// the burn required. The routine does not use the current position of the ship and target.
    /// 
    /// This follows the development in "Practical Astrodynamics Vol1" A. de laco Veris. p659-667
    /// 
    /// The algorithm iterates to find a deltaV with the required precision (delta theta) of the rendezvous. This
    /// is set to 1E-4 radians by default. 
    /// 
    /// NOT DEBUGGED!!!!
    /// 
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="target"></param>
    /// <param name="planet"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static double ComputeBurnForAngle(OrbitUniversal shipOrbit, OrbitUniversal targetOrbit, double angleRad) {

        double mu = shipOrbit.GetMu();
        double R = shipOrbit.GetMajorAxis();
        double RT = targetOrbit.GetMajorAxis();
        double v_ship = Mathd.Sqrt(mu/R);
        // first guess for dV. 1% of v
        double dV = v_ship * 0.01;
        Vector3d v_transfer = new Vector3d(dV * Mathd.Sin(angleRad), v_ship + dV*Mathd.Cos(angleRad), 0);
        Vector3d R1 = new Vector3d(shipOrbit.GetMajorAxis(), 0, 0);
        Vector3d h_ship = Vector3d.Cross(R1, v_transfer);

        // flight path angle of the transfer orbit
        // Quadrant??
        double gamma_transfer = Mathd.Asin(dV * Mathd.Sin(angleRad) / v_transfer.magnitude);
        double h_transfer = R * v_transfer.magnitude * Mathd.Cos(gamma_transfer);

        double p_transfer = h_transfer * h_transfer / mu;
        double a_transfer = mu * R / (2 * mu - v_transfer.magnitude * v_transfer.magnitude*R);
        double e_transfer = Mathd.Sqrt(1 - p_transfer / a_transfer);

        // compute true anomoly of ship in the transfer orbit
        double cos_phi_transfer = (p_transfer - R) / (R * e_transfer);
        double sin_phi_transfer = (p_transfer * Mathd.Tan(gamma_transfer) / (R * e_transfer));
        double phi_t = Mathd.Atan2(cos_phi_transfer, sin_phi_transfer);

        double sinAEtr = Mathd.Sqrt(1 - e_transfer * e_transfer) * Mathd.Sin(phi_t) / (1 + e_transfer * Mathd.Cos(phi_t));
        double cosAEtr = (e_transfer + Mathd.Cos(phi_t)) / (1 + e_transfer * Mathd.Cos(phi_t));
        double AEtr = Mathd.Atan2(cosAEtr, sinAEtr);
        double Mtr = AEtr - e_transfer * Mathd.Sin(AEtr);

        // compute the true anomoly of the ship at it's arrival at the target radius Rt. 
        double v_R = Mathd.Sqrt(mu * (2 / RT - 1 / a_transfer));
        double gamma_R = Mathd.Acos(h_transfer/(RT*v_R));
        double cos_phi_R = (p_transfer - RT) / (RT * e_transfer);
        double sin_phi_R = (p_transfer * Mathd.Tan(gamma_R)) / (RT * e_transfer);
        double phi_R = Mathd.Atan2(cos_phi_R, sin_phi_R);

        double sinAEr = Mathd.Sqrt(1 - e_transfer * e_transfer) * Mathd.Sin(phi_R) / (1 + e_transfer * Mathd.Cos(phi_R));
        double cosAEr = (e_transfer + Mathd.Cos(phi_R)) / (1 + e_transfer * Mathd.Cos(phi_R));
        double AEr = Mathd.Atan2(cosAEr, sinAEr);
        double MR = AEr - e_transfer * Mathd.Sin(AEr);

        double deltat = Mathd.Sqrt(a_transfer * a_transfer * a_transfer / mu) * (MR - Mtr);

        // find the error in the phase at rendezvous
        double alpha = Mathd.Asin(R * Mathd.Cos(angleRad) / RT);
        double beta = 0.5 * Mathd.PI - angleRad - alpha;
        double delta_phiR = phi_R - phi_t;
        double delta_phiT = delta_phiR - beta;

        double nT = Mathd.Sqrt(mu / (RT * RT * RT));
        double delta_phase = delta_phiR - deltat * nT;

        Debug.LogFormat("gamma_xfer={0} e_transfer={1} h_xfer={2} a_Xfer={3}", 
            gamma_transfer, e_transfer, h_transfer, a_transfer);
        Debug.LogFormat("delta_phiT={0} delta_phase={1} diff={2}", 
            delta_phiT, delta_phase, Mathd.Abs(delta_phase - delta_phiT));
        Debug.LogFormat("dV={0}", dV);
        return dV; 
    }
}
