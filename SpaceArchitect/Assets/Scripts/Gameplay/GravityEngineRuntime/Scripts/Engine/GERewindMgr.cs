using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Support for rewind in GE.
///
/// Used when the GE flag rewindModeEnabled is true
/// </summary>
public class GERewindMgr
{

    public enum RewindType {IMPULSE_ORBITU, POS_VEL, VELOCITY, MANEUVER, ADD, REMOVE, OFF_RAILS };

    /// <summary>
    /// Callback for rewind event. 
    /// </summary>
    /// <param name="rewindEntry"></param>
    /// <returns>flag indicating if event was handled by callback. If false GE will take required action.</returns>
    public delegate bool RewindCallback(RewindEntry rewindEntry);

    private RewindCallback rewindCallback;

    public class RewindEntry
    {
        public RewindType type;
        public NBody nbody;
        public double t;
        public Vector3d v;
        public Vector3d r;
        public Maneuver m;
        public double orbit_t0;
        public bool keplerMode; 

        public override string ToString()
        {
            return string.Format("type={0} nbody={1} t={2}", type, nbody.gameObject.name, t);
        }
    }

    //! List of rewindable entries sorted in ascending time (can just be added as GE goes forward)
    private List<RewindEntry> rewindList;

    public GERewindMgr()
    {
        rewindList = new List<RewindEntry>();
    }

    public void SetRewindCallback(RewindCallback rewindCallback)
    {
        this.rewindCallback = rewindCallback;
    }

    public void RecordImpulse(NBody nbody, OrbitUniversal orbitU, double time)
    {
        // Kepler mode is assumed
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.type = RewindType.IMPULSE_ORBITU;
        orbitU.GetRVT(ref rewind.r, ref rewind.v, ref rewind.orbit_t0);
        rewind.t = time;
        rewindList.Add(rewind);
    }

    public void RecordVelocityChange(GravityState gs, NBody nbody, Vector3d vel, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.type = RewindType.VELOCITY;
        SetRV(gs, nbody, rewind);
        rewind.t = time;
        rewindList.Add(rewind);
    }
    public void RecordPositionVelocityChange(GravityState gs, NBody nbody, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.type = RewindType.POS_VEL;
        SetRV(gs, nbody, rewind);
        rewind.t = time;
        rewindList.Add(rewind);
    }
    public void RecordManeuver(GravityState gs, Maneuver m, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = m.nbody;
        rewind.type = RewindType.MANEUVER;
        rewind.m = m;
        rewind.t = time;
        SetRV(gs, m.nbody, rewind);
        rewindList.Add(rewind);
    }

    public void RecordAddBody(NBody nbody, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.type = RewindType.ADD;
        rewind.t = time;
        rewindList.Add(rewind);
    }
    public void RecordBodyOffRails(NBody nbody, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.type = RewindType.OFF_RAILS;
        rewind.t = time;
        rewindList.Add(rewind);
    }

    public void RecordRemoveBody(GravityState gs, NBody nbody, double time)
    {
        RewindEntry rewind = new RewindEntry();
        rewind.nbody = nbody;
        rewind.t = time;
        rewind.type = RewindType.REMOVE;
        // is this a Kepler mode object?
        SetRV(gs, nbody, rewind);
        rewindList.Add(rewind);
    }

    private void SetRV(GravityState gs, NBody nbody, RewindEntry rewind)
    {
        OrbitUniversal ou = nbody.GetComponent<OrbitUniversal>();
        if ((ou != null) && (ou.evolveMode == OrbitUniversal.EvolveMode.KEPLERS_EQN)) {
            rewind.keplerMode = true;
            ou.GetRVT(ref rewind.r, ref rewind.v, ref rewind.orbit_t0);
        }
        else {
            rewind.r = gs.GetPhysicsPositionDoubleV3(nbody);
            rewind.v = gs.GetVelocity3d(nbody);
        }
    }

    public List<RewindEntry> EntriesAfter(double t)
    {
        List<RewindEntry> entries = new List<RewindEntry>();
        for (int i= rewindList.Count-1; i >= 0; i--) {
            if (rewindList[i].t > t)
                entries.Add(rewindList[i]);
            else
                break;
        }
        return entries;
    }

    private void SetVelocityForRewind(GravityState gs, RewindEntry re)
    {
        if (re.keplerMode) {
            OrbitUniversal ou2 = re.nbody.GetComponent<OrbitUniversal>();
            ou2.InitFromRVT(re.r, re.v, re.orbit_t0, ou2.GetCenterNBody(), relativePos: true, updateState: true);
        }
        else {
            // evolving backwards. Nbody needs to have -ve of velocity
            gs.SetVelocity3d(re.nbody, -re.v);
        }
    }

    public void ApplyEvent(GravityState gs, RewindEntry re)
    {
        if (rewindCallback != null) {
            // callback has the option of handling the event. If it does, nothing more to do.
            if (rewindCallback(re)) {
                // remove the event from the list
                if (!rewindList.Remove(re)) {
                    Debug.LogError("Failed to remove entry: " + re);
                }
                return;
            }
        }


        Debug.LogFormat("REWIND of {0} for {1} at t={2}", re.type, re.nbody.gameObject.name, re.t);
        switch (re.type) {
            case RewindType.VELOCITY:
                // evolving backwards. Nbody needs to have -ve of velocity
                SetVelocityForRewind(gs, re);
                break;

            case RewindType.POS_VEL:
                // evolving backwards. Nbody needs to have -ve of velocity
                SetVelocityForRewind(gs, re);
                gs.SetPosition3d(re.nbody, re.r);
                break;

            case RewindType.IMPULSE_ORBITU:
                OrbitUniversal ou = re.nbody.GetComponent<OrbitUniversal>();
                ou.InitFromRVT(re.r, re.v, re.orbit_t0, ou.GetCenterNBody(), relativePos: true, updateState: true);
                Debug.Log("restore t0={0}" + re.orbit_t0);
                break;

            case RewindType.MANEUVER:
                // evolving backwards. Nbody needs to have -ve of velocity
                SetVelocityForRewind(gs, re);
                break;

            case RewindType.ADD:
                // undo of an add is a remove
                GravityEngine.Instance().RemoveBody(re.nbody.gameObject);
                re.nbody.gameObject.SetActive(false);
                break;

            case RewindType.OFF_RAILS:
                // need to re-add as a fixed body and mark as fixed
                IFixedOrbit fOrbit = re.nbody.GetComponent<IFixedOrbit>();
                GravityEngine.FixedBody fBody = new GravityEngine.FixedBody(re.nbody, fOrbit);
                re.nbody.engineRef.fixedBody = fBody;
                re.nbody.engineRef.bodyType = GravityEngine.BodyType.FIXED;
                gs.AddFixedBody(GravityEngine.instance, re.nbody.engineRef.index, fBody, fOrbit);
                break;

            case RewindType.REMOVE:
                // undo of remove is add. Need to re-active the game object
                try {
                    re.nbody.gameObject.SetActive(true);
                    GravityEngine.Instance().AddBody(re.nbody.gameObject);
                    gs.SetPosition3d(re.nbody, re.r);
                    SetVelocityForRewind(gs, re);

                } catch (System.Exception e) {
                    Debug.LogError("Cannot re-add object. Was it destroyed? " + e.ToString());
                }
                break;

            default:
                throw new System.NotImplementedException("Case missing");
        }
        // remove the event from the list
        if (!rewindList.Remove(re)) {
            Debug.LogError("Failed to remove entry: " + re);
        }
        Debug.Log("rewind list count="+ rewindList.Count + "\n" + DumpAll());
    }

    public bool HaveEntries()
    {
        return rewindList.Count > 0;
    }

    public string DumpAll()
    {
        string s = "";
        int i = 0;
        foreach(RewindEntry r in rewindList) {
            s += string.Format("  {0}: {1} t={2} type={3}\n", i++, r.nbody.gameObject.name, r.t, r.type);
        }
        return s;
    }


}
