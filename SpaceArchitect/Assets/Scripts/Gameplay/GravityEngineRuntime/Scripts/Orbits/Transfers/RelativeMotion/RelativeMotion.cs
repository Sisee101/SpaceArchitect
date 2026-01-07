using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Relative Motion for orbital transfers and rendezvous. 
/// The relative motion formalism describes the motion of the ship with
/// repect to the target in a co-rotating coordinate frame in which the
/// target is stationary. It is assumed that the target is in a circular orbit
/// and that the ship is in a "nearby" orbit. The closer the ship and target
/// orbit, the better the approximation to actual motion and computation of
/// rendezvous maneuvers.
///
/// This component can be used to determine the maneuvers for a rendezvous. See
/// e.g. @RelativeXferController.       
/// 
/// The class is developed with reference to the presentation in Curtis
/// "Orbital Mechanics for Engineering Students". Chapter 7. 
/// 
/// There are different conventions for local co-ordinates. This component follows
/// Curtis/Prussing and Conway: 
///   x - vertical 
///   y - downrange
///   z - cross range. 
/// Others (e.g. Woffinden) use x for downrange, z vertical and y cross-range.
///
/// 
/// </summary>
public class RelativeMotion 
{

    private NBody ship;
    private NBody target;
    private NBody planet; 

    // Lower case co-ordinates for the co-rotating frame with taregt at the origin
    private Vector3d x_axis;    // vertical 
    private Vector3d y_axis;    // downrange
    private Vector3d z_axis;    // cross range

    private Vector3d omega_target;
    private double n; 

    // co-ordinate transformation from X to x
    private double[,] Q_Xx;

    private GravityEngine ge;

    private Vector3d burn0;
    private Vector3d burn1;

    private double timeToRdvs = 0; 

    public RelativeMotion(NBody ship, NBody target, NBody planet) {
        this.ship = ship;
        this.target = target;
        this.planet = planet;
        ge = GravityEngine.Instance();

        Vector3d r = (ge.GetPositionDoubleV3(target) - ge.GetPositionDoubleV3(planet));
        Vector3d v = (ge.GetVelocityDoubleV3(target) - ge.GetVelocityDoubleV3(planet));
        // assumes target is in a circular orbit
        omega_target = Vector3d.Cross(r, v) / (r.magnitude * r.magnitude);
        // Need to scale V to compare to book value for omega
        Debug.LogFormat("omega={0} (GE) v/r={1}", omega_target.magnitude, v.magnitude/r.magnitude);
        n = v.magnitude / r.magnitude;
        // transformation matrix (7.11)
        UpdateAxes();       
    }

    /// <summary>
    /// Get the angular velocity of the target in radians/sec.
    /// </summary>
    /// <returns>angular velocity in radians/second</returns>
    public double GetN() {
        return n;
    }

    private void UpdateAxes() {
        Vector3d r = (ge.GetPositionDoubleV3(target) - ge.GetPositionDoubleV3(planet));
        Vector3d v = (ge.GetVelocityDoubleV3(target) - ge.GetVelocityDoubleV3(planet));
        x_axis = r.normalized;
        z_axis = Vector3d.Cross(r, v).normalized;
        y_axis = Vector3d.Cross(z_axis, x_axis);
        Q_Xx = new double[3, 3] { { x_axis.x, x_axis.y, x_axis.z},
                                  { y_axis.x, y_axis.y, y_axis.z},
                                  { z_axis.x, z_axis.y, z_axis.z}};
    }

    /// <summary>
    /// Determine the target angle: the angle from the local horizontal of the ship to the
    /// target based on the current world positions in GE. 
    /// </summary>
    /// <returns>Target angle in degrees</returns>
    public double TargetAngleDegrees() {
        UpdateAxes();
        Vector3d delta_r_XYZ = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target);
        Vector3d delta_r_xyz = TransformToRelative(delta_r_XYZ);
        // assuming ship below target for now
        return Mathd.Atan2(-delta_r_xyz.x, -delta_r_xyz.y) * Mathd.Rad2Deg;
    }

    private Vector3d TransformToRelative(Vector3d v) {
        return Matrix3.MatrixTimesVector(ref Q_Xx, ref v);
    }

    /// <summary>
    /// Compute the two-impulse maneuver to rendezvous with a target in a circular orbit using a
    /// linearized relative motion framework (Clohessy-Wiltshire). See e.g. Curtis 7.5
    /// 
    /// This assumes the altitide distance between the ship and target is small compared to the radius
    /// of the target orbit. The resulting maneuvers are an approximation to the true 3D orbital 
    /// rendezvous problem. 
    /// 
    /// </summary>
    /// <param name="time">The time interval to rendezvous</param>
    /// <param name="targetAngleAlign">trigger angle targeting mode. Ensures initial dV aligns with target angle algorithm. </param>
    public void ComputeRendezvous(double time, bool targetAngleAlign) {
        timeToRdvs = time;
        UpdateAxes();
        Vector3d Rship = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(planet);
        Vector3d delta_r_XYZ = ge.GetPositionDoubleV3(ship) - ge.GetPositionDoubleV3(target);
        Vector3d delta_v_XYZ = ge.GetVelocityDoubleV3(ship) - ge.GetVelocityDoubleV3(target)
                      - Vector3d.Cross(omega_target, delta_r_XYZ);
        // relative versions
        Vector3d delta_r_xyz = TransformToRelative(delta_r_XYZ);
        Vector3d delta_v_xyz = TransformToRelative(delta_v_XYZ);
        Vector3d delta_v_xyz_taylor = new Vector3d(0, -1.5 * omega_target.magnitude * delta_r_xyz.x, 0);
        Debug.LogFormat("deltaV orig={0} taylor={1}", delta_v_xyz, delta_v_xyz_taylor);
        // this ensures dV aligns with target angle since it was the assumption used in calculating the target angle
        if (targetAngleAlign)
            delta_v_xyz = delta_v_xyz_taylor;

        // Compute the CW matrices
        double nt = n * time; // total number of radians to intercept
        double c = Mathd.Cos(nt);
        double s = Mathd.Sin(nt);
        Debug.LogFormat("t={0} n={1} c={2} s={3}", time, n, c, s);

        // Eqns (7.53 a-d)
        double[,] Phi_rr = new double[3, 3] { { 4.0 - 3.0 * c, 0, 0 }, 
                                              { 6.0 * (s - nt), 1.0, 0 }, 
                                              { 0, 0, c } };
        double[,] Phi_rv = new double[3, 3] { { s / n, 2 * (1.0 - c) / n, 0 }, 
                                              { 2.0 * (c - 1) / n, (4.0 * s - 3.0 * nt) / n, 0 }, 
                                              { 0, 0, s / n } };
        double[,] Phi_vr = new double[3, 3] { { 3.0*n*s, 0, 0},
                                             {6.0*n*(c-1.0), 0, 0 },
                                             {0, 0, -n*s }};
        double[,] Phi_vv = new double[3, 3] { { c, 2.0*s, 0},
                                             {-2.0*s, 4.0*c-3.0, 0 },
                                             {0, 0, c }};

        // find the initial vel (7.56): delta_v0plus = -(phi_rv)^-1 Phi_rr delta_r0
        double[,] Phi_rv_inv = new double[3, 3];
        Matrix3.MatrixInverse(ref Phi_rv, ref Phi_rv_inv);
        Vector3d v_int = Matrix3.MatrixTimesVector(ref Phi_rr, ref delta_r_xyz);
        Vector3d delta_v0plus = -1.0* Matrix3.MatrixTimesVector(ref Phi_rv_inv, ref v_int);

        // (7.52b) Arrival vel
        Vector3d delta_vt =  Matrix3.MatrixTimesVector(ref Phi_vr, ref delta_r_xyz) 
                            + Matrix3.MatrixTimesVector(ref Phi_vv, ref delta_v0plus);

        // Map burns back to XYZ
        double[,] Q_xX_inv = new double[3, 3];
        Matrix3.MatrixInverse(ref Q_Xx, ref Q_xX_inv);
        Vector3d deltaV0 = delta_v0plus - delta_v_xyz;
        burn0 = Matrix3.MatrixTimesVector(ref Q_xX_inv, ref deltaV0);

        // DeBUG
        double velAngle = Mathd.Atan2(deltaV0.x, deltaV0.y) * Mathd.Rad2Deg;
        double posAngle = Mathd.Atan2(-delta_r_xyz.x, -delta_r_xyz.y) * Mathd.Rad2Deg;
        Debug.LogFormat("xyz pos angle = {0} vel angle={1} dx={2} dy={3}", posAngle, velAngle, 
            delta_r_xyz.x, delta_r_xyz.y);

        // Cannot map back to XYZ until we are at that point. Use a beforeManeuver callback
        burn1 = delta_vt; 

        // double check the rendezvous delta
        Vector3d delta_rt = Matrix3.MatrixTimesVector(ref Phi_rr, ref delta_r_xyz) +
                            Matrix3.MatrixTimesVector(ref Phi_rv, ref delta_v0plus);
        Debug.LogFormat("At rendezvous r={0}", delta_rt);

        // DEBUG
        double velTarget = ge.GetVelocityDoubleV3(target).magnitude;
        //Debug.LogFormat("target v={0} scaled={1} km/sec vscale={2}", velTarget, (1.0/3600.0)*velTarget / GravityScaler.GetVelocityScale(), 
        //    GravityScaler.GetVelocityScale());


        Debug.LogFormat("xyz coords: deltaR={0} deltaV(scaled)={1} km/sec deltaV={1}", delta_r_xyz, 
            GravityScaler.ScaleVelPhysToScene(delta_v_xyz)/3600.0, delta_v_xyz);
        //Matrix3.LogMatrix(ref Phi_rr, "Phi_rr");
        //Matrix3.LogMatrix(ref Phi_rv, "Phi_rv");
        //Matrix3.LogMatrix(ref Phi_rv_inv, "Phi_rv_inv");
        Debug.LogFormat("Computed delta_v0plus={0} scaled={1} km/sec delta_vt={1} scaled={2} km/sec", 
            delta_v0plus, GravityScaler.ScaleVelPhysToScene(delta_v0plus)/3600.0,  
            delta_vt, GravityScaler.ScaleVelPhysToScene(delta_vt)/3600.0);
        Debug.LogFormat("deltaV0={0} km/sec burn0={1} km/sec angle={2} deg.", GravityScaler.ScaleVelPhysToScene(deltaV0) / 3600.0,
            GravityScaler.ScaleVelPhysToScene(burn0) / 3600.0, 
            Mathd.Atan2(deltaV0.y, deltaV0.x) * Mathd.Rad2Deg);
    }

    /// <summary>
    /// Return the initial burn to initiate a rendezvous computed by a call
    /// to ComputeRendezvous. 
    /// </summary>
    /// <returns>dV required for the burn</returns>
    public Vector3d GetBurn0() {
        return burn0;
    }

    /// <summary>
    /// Provide the maneuvers for the rendezvous determined by a call to
    /// ComputeRendezvous. 
    /// </summary>
    /// <returns>List of maneuvers</returns>
    public List<Maneuver> GetManeuvers() {
        List<Maneuver> maneuvers = new List<Maneuver>();

        Maneuver xfer = new Maneuver();
        xfer.mtype = Maneuver.Mtype.vector;
        xfer.nbody = ship;
        xfer.velChange = burn0.ToVector3();
        xfer.dV = xfer.velChange.magnitude;
        xfer.worldTime = ge.GetPhysicalTime();
        xfer.opaqueData = this;
        xfer.beforeExecuted = MapToXYZ;
        //xfer.relativeTo = planet;
        //xfer.relativeVel = burn0;
        maneuvers.Add(xfer);

        // Need to convert burn1 from xyz to XYZ immediatly before the burn to use position at time of burn
        // As a result setup a "before executed" function to do this. Will be called just before maneuver executes.
        Maneuver arrival = new Maneuver();
        arrival.mtype = Maneuver.Mtype.vector;
        arrival.nbody = ship;
        arrival.velChange = -burn1.ToVector3();
        arrival.dV = -arrival.velChange.magnitude;
        arrival.worldTime = xfer.worldTime + (float) timeToRdvs;
        arrival.opaqueData =  this;
        arrival.beforeExecuted = MapToXYZ;
        // relative
        xfer.relativeTo = planet;
        //xfer.relativeVel = -burn1;

        maneuvers.Add(arrival);

        return maneuvers;
    }

    private void RotateManeuver(Maneuver m) {
        UpdateAxes();
        double[,] Q_xX_inv = new double[3, 3];
        Matrix3.MatrixInverse(ref Q_Xx, ref Q_xX_inv);
        Vector3d vel = new Vector3d(m.velChange);
        m.velChange = Matrix3.MatrixTimesVector(ref Q_xX_inv, ref vel).ToVector3();
    }

    private void MapToXYZ(Maneuver m) {
        RelativeMotion rm = (RelativeMotion) m.opaqueData;
        rm.RotateManeuver(m);
    }

    /// <summary>
    /// Determine the relative position in the LVLH frame of the ship based
    /// on the current world position in GE.
    /// </summary>
    /// <returns></returns>
    public Vector3d ShipToLVLH() {
        Vector3d shipPos = ge.GetPositionDoubleV3(ship);
        Vector3d targetPos = ge.GetPositionDoubleV3(target);
        Vector3d deltaPos = shipPos - targetPos;

        UpdateAxes();
        Vector3d delta_pos_xyz = TransformToRelative(deltaPos);
        return delta_pos_xyz;
    }


    /// <summary>
    /// LVLH in Conway & Prussing/Curtis convention
    /// X - vertical 
    /// Y - down range
    /// Z - cross range
    /// </summary>
    /// <param name="target"></param>
    /// <param name="center"></param>
    /// <param name="x_axis"></param>
    /// <param name="y_axis"></param>
    /// <param name="z_axis"></param>
    private static void GetLVLHAxes(NBody target, NBody center, ref Vector3d x_axis, ref Vector3d y_axis, ref Vector3d z_axis) {
        GravityEngine ge = GravityEngine.Instance();
        Vector3d r = (ge.GetPositionDoubleV3(target) - ge.GetPositionDoubleV3(center)).normalized;
        Vector3d v = (ge.GetVelocityDoubleV3(target) - ge.GetVelocityDoubleV3(center)).normalized;
        x_axis = r;
        z_axis = Vector3d.Cross(r, v).normalized;
        y_axis = Vector3d.Cross(y_axis, z_axis);
    }

    /// <summary>
    /// Get the angle of the target with respect to the ship's local horizon vector. It is assumed the ship and target
    /// are in coplanar orbits. Does not assume orbit is circular. 
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="target"></param>
    /// <param name="planet"></param>
    /// <returns></returns>
    public static double TargetAngle(NBody ship, NBody target, NBody planet) {
        Vector3d x_axis = Vector3d.zero;
        Vector3d y_axis = Vector3d.zero;
        Vector3d z_axis = Vector3d.zero;
        GetLVLHAxes(target, planet, ref x_axis, ref y_axis, ref z_axis);

        Vector3d deltaPos = TargetVector(ship, target, planet);
        return Vector3d.Angle(deltaPos, x_axis);
    }

    /// <summary>
    /// Get the vector to the target with respect to the ship. 
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="target"></param>
    /// <param name="planet"></param>
    /// <returns></returns>
    public static Vector3d TargetVector(NBody ship, NBody target, NBody planet) {
        GravityEngine ge = GravityEngine.Instance();
        Vector3d planetPos = ge.GetPositionDoubleV3(planet);
        Vector3d shipPos = ge.GetPositionDoubleV3(ship) - planetPos;
        Vector3d targetPos = ge.GetPositionDoubleV3(target) - planetPos;
        Vector3d deltaPos = targetPos - shipPos;
        return deltaPos;
    }

}
