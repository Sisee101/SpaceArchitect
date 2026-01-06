using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Create a system of particles in an orbit specified by an OrbitUniversal attached to the same object. 
/// 
/// The OrbitUniversal allows more input options (as compared to a DustRing, now deprecated). 
/// 
/// </summary>
[RequireComponent(typeof(GravityParticles))]
[RequireComponent(typeof(OrbitUniversal))]
public class DustOrbit : MonoBehaviour, IGravityParticlesInit
{

    //
    // Create a ring of particles in orbit around a specific GameObject with an attached NBody script
    // Must be called once the position and velocity of the NBody has been initialized

    //! Width of particle ring as a percent of ring radius. 
    [SerializeField]
    private float ringWidthPercent = 10f;

    private OrbitUniversal orbitU;

    // Use this for initialization
    void Start()
    {
        orbitU = GetComponent<OrbitUniversal>();
        // if the ring is too small will break particle system. 
        if (orbitU.p < 1E-3) {
            Debug.LogError("Ring radius too small - setting to 1");
            orbitU.p = 1.0;
        }
        GravityEngine.Instance().AddGEStartCallback(GeStart);
    }

    private void GeStart()
    {
        // since orbitU is used as an orbit utility it's normal init sequence via InitNBody is not triggered. 
        // For our purpose all we need to do here is to set the mass.
        orbitU.SetMu(GravityEngine.Instance().GetMass(orbitU.centerNbody));
        // mostly need conic orientation to be computed
        orbitU.Init();
    }

    public void InitNewParticles(int numLastActive, int numActive, ref double[,] r, ref double[,] v)
    {
        // For each particle, stomp the p in orbitU with the random value, then restore after
        float p_initial = (float) orbitU.p;
        float f = 0;
        float dp = 0.5f * ringWidthPercent / 100f;
        double ecc = orbitU.eccentricity;
        Vector3 pos, vel;
        for (int i = numLastActive; i < numActive; i++) {
            float p = Random.Range(p_initial * (1f - dp), p_initial * (1f + dp));
            orbitU.p = p;
            if (orbitU.eccentricity < 1.0) {
                // distribute uniformly in mean anomoly and then convert so density represents time spent in 
                // each part of orbit. (Without this get clumping of particles at periapsis).
                double M = Random.Range(-Mathf.PI, Mathf.PI);
                double E = OrbitUtils.ConvertMeanAnomolyToE(M, ecc);
                f = (float)OrbitUtils.ConvertEtoTrueAnomoly(E, ecc) * Mathf.Rad2Deg;
            } else {
                f = Random.Range(-89f, 89f);
            }
            pos = orbitU.GetPositionDForThetaRadians(f * Mathf.Deg2Rad, true).ToVector3();
            vel = orbitU.VelocityForPhaseRelative(f);

            r[i, 0] = pos.x;
            r[i, 1] = pos.y;
            r[i, 2] = pos.z;

            v[i, 0] = vel.x;
            v[i, 1] = vel.y;
            v[i, 2] = vel.z;

        }
        orbitU.p = p_initial;
    }

}
