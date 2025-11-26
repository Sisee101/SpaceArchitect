using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handle any GETrigger requests that have been registered with GE. 
/// 
/// GETrigger allows user code to insert a condition check (e.g. is ship within 100 units of the target) into the
/// main evolution loop of gravity engine. This is better than have user code check the condition itself in FixedUpdate
/// if the time zoom capability is being used (then the time jumps per fixed update can be large). By using a GETrigger
/// the condition is checked every engineDt cycle. 
/// 
/// This does directly impact the time required by the core loop in GE. 
/// 
/// If any triggers are present this also prevents the use of time jumps in all-on-rails cases. 
/// 
/// A trigger consists of a delegate function and an opaque data reference. 
/// 
/// </summary>
public class GETriggerMgr 
{
    /// <summary>
    /// Function to be run at each engineDt cycle. 
    /// - this function is given the attached GravityState
    /// - the function should refer to GE internal positions/velocities (the engine loop has NOT updated transform positions)
    /// - trigger functions can check if the GravityState is for a trajectory prediction by checking gs.isCopy
    /// </summary>
    /// <param name="gs"></param>
    /// <returns></returns>
    public delegate bool TriggerFunction(GravityState gs, System.Object data);

    public class Trigger
    {
        public Trigger(TriggerFunction f, System.Object data)
        {
            trigger = f;
            triggerData = data;
        }

        public TriggerFunction trigger;
        public System.Object triggerData; 
    }

    private List<Trigger> triggers; 

    public GETriggerMgr()
    {
        triggers = new List<Trigger>();
    }

    public GETriggerMgr(GETriggerMgr from)
    {
        triggers = new List<Trigger>(from.triggers);
    }

    public void AddTrigger(Trigger t)
    {
        triggers.Add(t);
    }

    public void RemoveTrigger(Trigger t)
    {
        triggers.Remove(t);
    }

    public void Clear()
    {
        triggers.Clear();
    }

    public bool HaveTriggers()
    {
        return triggers.Count > 0; 
    }

    /// <summary>
    /// Run all the registered triggers. If any ask to be removed, take them out of the list. 
    /// </summary>
    /// <param name="gs"></param>
    public void RunTriggers(GravityState gs)
    {
        List<Trigger> toRemove = new List<Trigger>();
        foreach(Trigger t in triggers) {
            if (t.trigger(gs, t.triggerData)) {
                toRemove.Add(t);
            }
        }
        foreach(Trigger t in toRemove) {
            triggers.Remove(t);
        }
    }
}
