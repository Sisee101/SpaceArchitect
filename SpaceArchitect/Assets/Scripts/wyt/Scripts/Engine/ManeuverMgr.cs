using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneuver Manager
/// Handles the GE delegation of maneuver lists for NBody objects in the
/// GE workflow. 
/// 
/// All main methods are typically called from wrappers in GE and then 
/// delegated to here. This allows some separation of concerns. 
/// 
/// </summary>
public class ManeuverMgr  {

	private List<Maneuver> maneuvers; 

	public ManeuverMgr () {
		Maneuver mForCompare = new Maneuver();
        maneuvers = new List<Maneuver>();
	}


    public ManeuverMgr(ManeuverMgr copyFrom) {
        Maneuver mForCompare = new Maneuver();
        maneuvers = new List<Maneuver>();
        foreach (Maneuver m in copyFrom.maneuvers ) {
            maneuvers.Add(m);
        }
    }

    public void Add(Maneuver maneuver) {
        if (maneuvers.IndexOf(maneuver) >= 0) {
            Debug.LogError("Maneuver is already in the list.");
        }
        GravityEngine.FixedBody fb = maneuver.nbody.engineRef.fixedBody;
        // if the body is a KeplerSequence then the manuver is added as segments to the
        // sequence unless it has a before maneuver callback. In that case, have to treat it as a normal maneuver
        // and the time reversability is lost. onExecuted callbacks are handled by the segment change code. 
        if (!(maneuver.beforeExecuted != null) && (fb != null) && (fb.keplerSeq != null)) {
            fb.keplerSeq.AddManeuver(maneuver);
        } else {
            // insert in worldTime order
            bool done = false;
            for (int i=0; i < maneuvers.Count; i++) {
                if (maneuver.worldTime < maneuvers[i].worldTime) {
                    maneuvers.Insert(i, maneuver);
                    done = true;
                    break;
                }
            }
            if (!done) {
                maneuvers.Insert(maneuvers.Count, maneuver);
            }
        }
	}

    public void Add(List<Maneuver> mlist) {
        foreach (Maneuver m in mlist) {
            Add(m);
        }
    }

    /// <summary>
    /// Return a list of all maneuvers executing earlier than <time>.
    /// </summary>
    ///
    /// <param name="time">The time</param>
    ///
    /// <returns>List of all maneuvers that occur before time specified</returns>
    public List<Maneuver> ManeuversUntil(float time) {
		List<Maneuver> list = new List<Maneuver>();
		foreach (Maneuver m in maneuvers) 
		{
			if (m.worldTime < time) {
				list.Add(m);
			} else {
				break;
			}
		}
		return list;
	}

    public void Execute(Maneuver m, GravityState gs, bool isCopy) {
        // maneuvers in a copy of the main state are "what if" projections so do not notify about
        // completed maneuver
        if (!isCopy && m.beforeExecuted != null) {
            m.beforeExecuted(m);
        }
        m.Execute(gs);
        // maneuvers in a copy of the main state are "what if" projections so do not notify about
        // completed maneuver
        if (!isCopy && m.onExecuted != null) {
            m.onExecuted(m);
        }
        Remove(m);
    }

	public void Remove(Maneuver m) {
		bool removed = maneuvers.Remove(m);
		if (!removed) {
			Debug.LogWarning("Could not remove maneuver " + m.LogString());
		}
	}

	public List<Maneuver> GetManeuvers(NBody nbody) {
		List<Maneuver> list = new List<Maneuver>();
		foreach (Maneuver m in maneuvers) {
			if (m.nbody == nbody) {
				list.Add(m);
			}
		}
		return list;
	}

    public void Clear() {
        maneuvers.Clear();
    }

    /// <summary>
    /// Indicate if there are any maneuvers
    /// </summary>
    /// <returns></returns>
    public bool HaveManeuvers() {
        return maneuvers.Count > 0; 
    }

    public string DumpAll()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Maneuvers:\n----------\n");
        if (maneuvers.Count == 0)
            sb.Append("  none");
        foreach (Maneuver m in maneuvers) {
            sb.Append(string.Format("   t={0} {1} {2} type={3} v={4} dv={5}\n",
                m.worldTime,
                m.label,
                m.nbody.gameObject.name,
                m.mtype,
                m.velChange, 
                m.dV));
        }
        return sb.ToString();
    }
}
