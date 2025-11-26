using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kepler elements maintains a time ordered list of OrbitUniversal segments. This allows a seqence of orbits
/// around different bodies to be specified. For example, a free return trajectory around a moon would have: 
/// ellipse around Earth, hyperbola around moon, ellipse around Earth as three segements. By putting everything
/// "on-rails" the scene can jump in time to any value requested without running NBody calculations for all 
/// the intermediate positions. 
/// 
/// On Init the KeplerSequence will automatically find and add the existing OrbitUniversal at the start of the list.
/// 
/// Note that this is NOT real physics since it is a series of two body evolutions and it is not what would
/// really happen. It may be a good enough model for a game, depending on the importance of accuracy vs utility
/// of jumping in time and getting results that do not vary based on numerical accuracy chosen for the GE.
/// 
/// The individual orbital elements are created as components and attached to a synthesized child object (this
/// ensures the KeplerSequence can be unambigously attached to an NBody. This requires that the active body 
/// and center body for the sequence elements be set explicitly (they cannot be inferred). 
/// 
/// </summary>

// Must have some initial information that says what orbit we are on
// [RequireComponent(typeof(OrbitUniversal))]
public class KeplerSequence : MonoBehaviour, IFixedOrbit, INbodyInit {

    //! Optional callback to indicate when a specific element in the sequence starts
    public delegate void ElementStarted(OrbitUniversal orbitU);

    public delegate void ReactivateCallback(NBody nbody);

    public delegate void InactivateCallback(NBody nbody);


    // inner class holding each of the conic sections and a time at which they start
    public class KeplerElement
    {

        public enum KEType {  ORBIT, PATH_PROP};

        public KEType keType = KEType.ORBIT;

        //! time at which the sequence element starts (internal physics time)
        public double timeStart;
        public OrbitUniversal orbit;
        public PathPropagator pathProp;

        //! flag to indicate if at this time object should go off-rails
        public bool returnToGE;
        //! this segment is inactive (has been marked inactive in the scene but NOT in GE. This way Evolve is still called
        public bool inactive;
        //! Callbacks to be run when time jumps from inactivated to activated
        public ReactivateCallback reactivateCallback;
        public InactivateCallback inactivateCallback;

        //! callback when a new sequence is started. Will be called each time transition to sequence occurs. 
        public ElementStarted callback;
        //! optional field used when element added via a maneuver. Allows maneuver callback on sequence change.
        public Maneuver maneuver;

        public KeplerElement()
        {

        }

        // Copy constructor
        public KeplerElement(KeplerElement copyFrom, 
                            GameObject orbitsGO, 
                            NBody nbody,    
                            bool copyActivateCallbacks,
                            bool copyManeuverCallbacks)
        {
            timeStart = copyFrom.timeStart;
            // deep copy of orbit
            orbit = orbitsGO.AddComponent<OrbitUniversal>();
            orbit.CopyFromOrbitUniversal(copyFrom.orbit);
            keType = copyFrom.keType;
            // Q: Does this need to be a deep clone??
            pathProp = copyFrom.pathProp;
            // need orbit to be for this nbody
            orbit.SetNBody(nbody);
            returnToGE = copyFrom.returnToGE;
            inactive = copyFrom.inactive;
            if (copyActivateCallbacks) {
                reactivateCallback = copyFrom.reactivateCallback;
                inactivateCallback = copyFrom.inactivateCallback;
            }
            callback = null;
            maneuver = new Maneuver(copyFrom.maneuver); // deep clone
            if (!copyManeuverCallbacks) {
                maneuver.onExecuted = null;
                maneuver.beforeExecuted = null;
                maneuver.nbody = nbody; // refer to this NBody
            }
        }

        public void Evolve(double physicsTime, GravityState gravityState, ref double[] r_new, ref double[] v_new, bool isQuery = false)
        {
            if (keType == KEType.ORBIT) {
                orbit.Evolve(physicsTime, gravityState, ref r_new, ref v_new, isQuery);
            } else if (keType == KEType.PATH_PROP) {
                pathProp.Evolve(physicsTime, gravityState, ref r_new, ref v_new, isQuery);
            }
        }

        public void PreEvolve(float physicalScale, float massScale)
        {
            if (keType == KEType.ORBIT) {
                orbit.PreEvolve(physicalScale, massScale);
            }
            else if (keType == KEType.PATH_PROP) {
                pathProp.PreEvolve(physicalScale, massScale);
            }
        }
    }

    private NBody nbody; 

    private List<KeplerElement> keplerElements;

    private GameObject orbitsGO;

    private OrbitUniversal initialOrbit;

    private int activeElement = 0;

    public void ClearElements()
    {
        keplerElements = null;
    }

    public void InitNBody(float physicalScale, float massScale) {
        if (keplerElements == null)
            keplerElements = new List<KeplerElement>();
        else
            // if we already have Kepler elements then they were initialized already
            return;
       
        if (orbitsGO == null) {
            orbitsGO = new GameObject("Orbit Sequence");
            orbitsGO.transform.parent = gameObject.transform;
        }
        this.nbody = GetComponent<NBody>();
        // initial orbit may be an OU or a PathP
        initialOrbit = GetComponent<OrbitUniversal>();
        if (initialOrbit != null) {
            if (initialOrbit.evolveMode == OrbitUniversal.EvolveMode.GRAVITY_ENGINE) {
                Debug.LogError("Initial orbit set to GRAVITY_ENGINE. Cannot use Kepler sequence.");
                return;
            }
            AppendElementExistingOrbitU(initialOrbit, null);
            keplerElements[activeElement].orbit.InitNBody(physicalScale, massScale);
        } else {
            PathPropagator pathP = GetComponent<PathPropagator>();
            if (pathP != null) {
                AppendElementExistingPathProp(pathP, callback: null);
            } else {
                Debug.LogError("Did not find an initial segment for KSeq on " + gameObject.name);
                return;
            }
        }
    }


    /// <summary>
    /// Check the time of the appended element is later than the last element in the sequence. 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private bool BadTime(double time) {
        if (keplerElements.Count == 0)
            return false;
        // UGLY. Since maneuver holds time as float can be off by a tiny bit. Do comparision as floats
        bool bad = (float) time < (float) (keplerElements[keplerElements.Count - 1].timeStart);
        if (bad) {
             Debug.LogError(string.Format("Time is earlier than an existing orbit data entry. last={0} time={1}",
                    keplerElements[keplerElements.Count - 1].timeStart, time));
        }
        return bad;
    }

    /// <summary>
    /// Add an element to the sequence using r0/v0/t0 initial conditions as an OrbitUniversal
    /// 
    /// Position and velocity are with respect to the center body (NOT world/physics space!). 
    /// 
    /// Orbit segements must be added in increasing time order. 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time"></param>
    /// <param name="relativePos"></param>
    /// <param name="body"></param>
    /// <param name="centerBody"></param>
    /// <param name="callback"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public OrbitUniversal AppendElementRVT(Vector3d r0, 
                                            Vector3d v0, 
                                            double time, 
                                            bool relativePos,
                                            NBody body, 
                                            NBody centerBody,
                                            ElementStarted callback,
                                            Maneuver m = null,
                                            OrbitUniversal.EvolveMode evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN) {
        if (BadTime(time)) {
            return null;
        }
        if (m !=null) {
            time = m.worldTime;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = time,
            callback = callback,
            keType = KeplerElement.KEType.ORBIT,
            returnToGE = false
        };
        OrbitUniversal orbit = orbitsGO.AddComponent<OrbitUniversal>();
        orbit.centerNbody = centerBody;
        orbit.SetNBody(body);
        if (initialOrbit != null) {
            // by default keep type consistenvy
            orbit.evolveMode = initialOrbit.evolveMode; 
        } else {
            orbit.evolveMode = evolveMode;
        }
        // Maneuvers have some special cases
        if (m != null) {
            if ((m.relativeTo != null) && m.HasRelativePosVel() ) {
                // may be very slight differences, for RDVS want to be exact
                r0 = m.relativePos;
                v0 = m.relativeVel;
                relativePos = true;
            }
            if (m.railsXferMode == Maneuver.RailsTransferMode.KEPLER) {
                orbit.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
            } else if (m.railsXferMode == Maneuver.RailsTransferMode.SGP4) {
                orbit.evolveMode = OrbitUniversal.EvolveMode.SGP4_PROPAGATOR;
            } else if (m.railsXferMode == Maneuver.RailsTransferMode.PKEPLER) {
                orbit.evolveMode = OrbitUniversal.EvolveMode.PKEPLER_J2;
            }
        }
        orbit.InitFromRVT(r0, v0, time, centerBody, relativePos, updateState: false);
        ke.orbit = orbit;
        keplerElements.Add(ke);
        return orbit;
    }

    public OrbitUniversal NewOrbitSegment()
    {
        OrbitUniversal orbit = orbitsGO.AddComponent<OrbitUniversal>();
        return orbit;
    }

 
    /// <summary>
    /// Force KS to go to the next element. 
    /// 
    /// This is hacky and is used in the case where a KS SetVelocityDouble is used by the auto-tester in the same flow where
    /// an orbit check will be done before a newly appended element at the current time becomes active. 
    /// </summary>
    public void AdvanceToNextSegment()
    {
        if (activeElement < keplerElements.Count-1)
            activeElement++;
    }


    /// <summary>
    /// Add an element using an existing OrbitUniversal instance
    /// 
    /// Orbit elements must be added in increasing time order. 
    /// </summary>
    /// <param name="orbitU"></param>
    /// <param name="callback">(Optional) Method to call when sequence starts</param>
    public void AppendElementExistingOrbitU(OrbitUniversal orbitU, ElementStarted callback) {
        if (BadTime(orbitU.GetStartTime())) {
            // bad time will log an error
            return;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = orbitU.GetStartTime(),
            returnToGE = false, 
            orbit = orbitU,
            keType = KeplerElement.KEType.ORBIT,
            callback = callback
        };
        keplerElements.Add(ke);
    }

    public void AppendElementExistingPathProp(PathPropagator pathProp, ElementStarted callback)
    {
        if (BadTime(pathProp.GetStartTime())) {
            // bad time will log an error
            return;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = pathProp.GetStartTime(),
            returnToGE = false,
            pathProp = pathProp,
            keType = KeplerElement.KEType.PATH_PROP,
            callback = callback
        };
        keplerElements.Add(ke);
    }

    public void AppendReturnToGE(double time, NBody body) {
        KeplerElement ke = new KeplerElement
        {
            timeStart = time,
            returnToGE = true
        };
        keplerElements.Add(ke);
    }


    public void AppendInactive(double time, InactivateCallback inactivateCallback, ReactivateCallback reactivateCallback)
    {
        // for now only allow one inactive point (i.e. satellite decay and not object jumping in/out)
        if (HaveInactive())
            return;

        KeplerElement ke = new KeplerElement
        {
            timeStart = time,
            inactive = true,
            reactivateCallback = reactivateCallback,
            inactivateCallback = inactivateCallback
        };
        keplerElements.Add(ke);
    }

    private void RemoveInactive()
    {
        // can only have one inactive element in a sequence
        List<KeplerElement> elements = new List<KeplerElement>();
        foreach (KeplerElement k in keplerElements) {
            if (k.inactive)
                elements.Add(k);
        }
        foreach (KeplerElement k in elements) 
            keplerElements.Remove(k);
    }

    private bool HaveInactive()
    {
        foreach(KeplerElement ke in keplerElements) {
            if (ke.inactive)
                return true;
        }
        return false;
    }


    public void Evolve(double physicsTime, GravityState gs, ref double[] r, ref double[] v, bool isQuery = false) {
        int prevActiveElement = activeElement;

        if ((activeElement < keplerElements.Count - 1) &&
            (physicsTime > keplerElements[activeElement + 1].timeStart)) {
            // Advance to element that covers this time
            do {
                activeElement++;
            } while ((activeElement != keplerElements.Count - 1) &&
                    (physicsTime > keplerElements[activeElement + 1].timeStart));
            GravityEngine ge = GravityEngine.Instance();
            KeplerElement activeKE = keplerElements[activeElement];
            if (activeKE.returnToGE) {
#pragma warning disable 162     // disable unreachable code warning
                if (GravityEngine.DEBUG)
                    Debug.Log("return to GE:" + gameObject.name);
#pragma warning restore 162
                KeplerElement priorKE = keplerElements[activeElement - 1];
                priorKE.Evolve(physicsTime, gs, ref r, ref v);
                ge.BodyOffRails(nbody, new Vector3d(ref r), new Vector3d(ref v));
                if (isQuery)
                    activeElement = prevActiveElement;
                return;
            } else if (activeKE.inactive) {
                // switching to an inactive element. Support code will have removed the GO and told GE not
                // to do updates. 
                if (!isQuery)
                    activeKE.inactivateCallback(nbody);
#pragma warning disable 162     // disable unreachable code warning
                if (GravityEngine.DEBUG)
                    Debug.LogFormat("Changed to inactive segment {0} tnow={1} tseg={2} ", activeElement, physicsTime,
                        keplerElements[activeElement].timeStart);
#pragma warning restore 162
                if (isQuery)
                    activeElement = prevActiveElement;
                return;
            } else {
                // if the center object of the orbit changes, need to recompute the KeplerDepth and update
                //if (keplerElements[activeElement-1].orbit.centerNbody != activeKE.orbit.centerNbody) {
                //    NewCenter( activeKE.orbit);
                //}
                CheckOrbitCenter(activeElement - 1);
                // move on to the next orbit in the sequence
                activeKE.PreEvolve(ge.physToWorldFactor, ge.massScale);
                if (!isQuery && activeKE.callback != null) {
                    activeKE.callback(activeKE.orbit);
                }
                // need to do evolution to maneuver time and update state before maneuver callback is invoked
                if ((activeKE.maneuver != null) && (!isQuery && (activeKE.maneuver.onExecuted != null))) {
                    keplerElements[activeElement].Evolve(activeKE.maneuver.worldTime, gs, ref r, ref v, isQuery);
                    gs.UpdateInternalDouble(nbody, new Vector3d(r[0], r[1], r[2]), new Vector3d(v[0], v[1], v[2]));
                    activeKE.maneuver.onExecuted(activeKE.maneuver);
                }
            }
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.LogFormat("Changed to later segment {0} tnow={1} tseg={2} ", activeElement, physicsTime,
                    keplerElements[activeElement].timeStart);
#pragma warning restore 162
        } else if (physicsTime < keplerElements[activeElement].timeStart) {
            // Use an earlier element (happens if time set to earlier)
            int lastElement = activeElement;
            while (physicsTime < keplerElements[activeElement].timeStart && (activeElement > 0)) {
                activeElement--;
            }

            if (keplerElements[lastElement].inactive && !keplerElements[activeElement].inactive) {
                // reactivating an object that was removed (e.g. satellite that decayed)
                if (!isQuery)
                    keplerElements[lastElement].reactivateCallback(nbody);
            }
            else {
                CheckOrbitCenter(lastElement);
            }
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.LogFormat("Changed to earlier segment {0} tnow={1} tseg={2} ", activeElement, physicsTime,
                    keplerElements[activeElement].timeStart);
#pragma warning restore 162
        }

        if (keplerElements[activeElement].inactive) {
            // do nothing. This assumes GE has been told to set the noUpdate flag on this object.
        } else if (!keplerElements[activeElement].returnToGE) {
            // time for evolve is absolute - up to OrbitUniversal to make it relative to their time0
            keplerElements[activeElement].Evolve(physicsTime, gs, ref r, ref v, isQuery);
        }

        if (isQuery)
            activeElement = prevActiveElement;

    }

    private void CheckOrbitCenter(int element)
    {
        if ((keplerElements[element].keType == KeplerElement.KEType.ORBIT) &&
                (keplerElements[activeElement].keType == KeplerElement.KEType.ORBIT)) {
            if (keplerElements[element].orbit.centerNbody != keplerElements[activeElement].orbit.centerNbody) {
                NewCenter(keplerElements[activeElement].orbit);
            }
        }
    }

    /// <summary>
    /// New center. Update the Kepler depth and any children holding orbit predictors or segments
    /// </summary>
    /// <param name="orbitU"></param>
    private void NewCenter(OrbitUniversal orbitU) {
        GravityEngine.Instance().UpdateKeplerDepth(nbody, orbitU);
        foreach(OrbitPredictor op in gameObject.GetComponentsInChildren<OrbitPredictor>()) {
            op.SetCenterObject(orbitU.centerNbody.gameObject);
        }
        foreach (OrbitSegment os in gameObject.GetComponentsInChildren<OrbitSegment>()) {
            os.SetCenterObject(orbitU.centerNbody.gameObject);
        }
    }

    /// <summary>
    /// Get the current OrbitUniversal for the orbit
    /// </summary>
    /// <returns></returns>
    public OrbitUniversal GetCurrentOrbit() {
        return keplerElements[activeElement].orbit;
    }

    /// <summary>
    /// Get orbit at the specified index. If -1 use the last entry
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public OrbitUniversal GetOrbitAtIndex(int index)
    {
        if (index < 0)
            index = keplerElements.Count - 1;
        return keplerElements[index].orbit;
    }
    /// <summary>
    /// Return the index of the current orbit sequence. 
    /// (Used in the editor script for in-scene display)
    /// </summary>
    /// <returns></returns>
    public int GetCurrentOrbitIndex() {
        return activeElement;
    }

    public void GEUpdate(GravityEngine ge) {
        keplerElements[activeElement].orbit.GEUpdate(ge);
    }

    public void Move(Vector3 position) {
        // Not sure this works. Apply to all segments ???
        keplerElements[activeElement].orbit.Move(position);
    }

    public void PreEvolve(float physicalScale, float massScale) {
        keplerElements[activeElement].PreEvolve(physicalScale, massScale);
    }

    /// <summary>
    /// Not valid for a Kepler Sequence. 
    /// </summary>
    /// <param name="nbody"></param>
    public void SetNBody(NBody nbody) {
        throw new System.NotImplementedException();
    }

    public void SetTimeoffset(double timeOffset) {
        throw new System.NotImplementedException();
    }

    public NBody GetCenterNBody() {
        if (keplerElements[activeElement].keType == KeplerElement.KEType.ORBIT) {
            return keplerElements[activeElement].orbit.centerNbody;
        } else {
            // path prop
            return keplerElements[activeElement].pathProp.centerBody;
        }
    }

    /// <summary>
    /// Apply the impulse to the current OrbitUniversal element.
    /// 
    /// This will break time-reversal because this change in impulse is not recorded. 
    /// </summary>
    /// <param name="impulse"></param>
    /// <returns></returns>
    public Vector3 ApplyImpulse(Vector3 impulse) {
        Debug.LogWarning("Not supported");
        if (keplerElements[activeElement].keType == KeplerElement.KEType.ORBIT) {
            return keplerElements[activeElement].orbit.ApplyImpulse(impulse);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// If there is an Initial OU not in rails mode it will be checked in init. Can just always
    /// return true;
    /// </summary>
    /// <returns></returns>
    public bool IsOnRails() {
        return true;
    }

    /// <summary>
    /// Set the position and velocity at the current time. 
    /// 
    /// This will break time reversal since the change is not recorded.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        Debug.LogError("Not implemented for KSeq, use AppendElementRVT instead");
    }

	public void AddManeuver(Maneuver m)
	{
        if (m.relativeTo != null) {
            AppendElementRVT(m.relativePos, m.relativeVel, m.worldTime, true, m.nbody, m.relativeTo, null, m: m);
            // add the maneuver to the KE so callback can be used
            KeplerElement ke = keplerElements[keplerElements.Count - 1];
            ke.maneuver = m;
        } else {
            Debug.LogError("Could not add maneuver. Missing relativeTo information. Skipped.");
        }
    }

    /// <summary>
    /// Add orbit segments for each of the maneuvers. The maneuvers must have been created by transfer code
    /// that populated the fields: relativeTo, relativePos, relativeVel and time fields. 
    /// 
    /// </summary>
    /// <param name="list"></param>
    public void AddManeuvers(List<Maneuver> maneuverList) {
        foreach (Maneuver m in maneuverList) {
            AddManeuver(m);
        }
    }

    /// <summary>
    /// Remove maneuvers from the Kepler sequence IF they have not started yet.
    /// 
    /// If they have started or are in the past, leave in place and return an error. 
    /// 
    /// </summary>
    /// <param name="maneuverList"></param>
    public bool RemoveManeuvers(List<Maneuver> maneuverList) {

        int numElements = keplerElements.Count;
        foreach (Maneuver m in maneuverList) { 
            for (int i = activeElement+1; i < keplerElements.Count; i++) {
                if (keplerElements[i].maneuver == m) {
                    keplerElements.RemoveAt(i);
                    break;
                }
            }
        }
        if ((numElements - maneuverList.Count) != keplerElements.Count) {
            Debug.LogWarning("Could not delete all maneuvers. Not present or already applied.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Remove all segments that occur after the current time
    /// </summary>
    public void RemoveFutureSegments() {
        RemoveSegmentsAfterTime(GravityEngine.Instance().GetPhysicalTime());
    }

    /// <summary>
    /// Remove all segments after the specified time.
    /// </summary>
    /// <param name="time"></param>
    public void RemoveSegmentsAfterTime(double time) {
        int removeFrom = -1; 
        for (int i = activeElement + 1; i < keplerElements.Count; i++) {
            if (keplerElements[i].timeStart > time) {
                removeFrom = i;
                break;
            }
        }
        // Cannot remove the first segment
        if (removeFrom > 0) {
            keplerElements.RemoveRange(removeFrom, (keplerElements.Count - removeFrom));
        }

    }

    /// <summary>
    /// Remove all previous and current segments. Used when a body is put back on rails after a period of NBody
    /// evolution. The on-rails code will explicitly add an orbitU.
    /// </summary>
    public void Reset() {
        activeElement = 0;
        if ((keplerElements != null) && (keplerElements.Count > 1))
            keplerElements.RemoveRange(1, keplerElements.Count-1);
    }

    /// <summary>
    /// Make a copy of the KeplerSequence copyFrom. 
    /// </summary>
    /// <param name="copyFrom"></param>
    public void CopyFrom(KeplerSequence copyFrom, 
                            bool retainActivateCallbacks = false, 
                            bool retainManeuverCallbacks = false)
    {
        // the 0th active element is the attached initial orbit
        initialOrbit.CopyFromOrbitUniversal(copyFrom.initialOrbit);
        initialOrbit.SetNBody(nbody);
        initialOrbit.Init();
        // do NOT copy the NBody or orbitsGO, that needs to be distinct
        if (keplerElements != null) {
            for (int i=1; i < keplerElements.Count; i++) {
                Destroy(keplerElements[i].orbit);
            }
            if (keplerElements.Count > 1) {
                keplerElements.RemoveRange(1, keplerElements.Count - 2);
            }
        } else {
            Debug.LogError("KeplerSeq mnust be inited before Copy is called");
        }

        for (int i=1; i < copyFrom.keplerElements.Count; i++) {
            keplerElements.Add(new KeplerElement(copyFrom.keplerElements[i], 
                                                    orbitsGO, 
                                                    nbody, 
                                                    retainActivateCallbacks, 
                                                    retainManeuverCallbacks));
        }
        activeElement = copyFrom.activeElement;
    }

    public string DumpInfo() {
        if (keplerElements == null)
            return "Not initialized";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(string.Format("  Kepler Sequence: numElements= {0} activeSeq={1}\n", 
            keplerElements.Count, activeElement));
        for (int i = 0; i < keplerElements.Count; i++) {
            string details;
            if (keplerElements[i].keType == KeplerElement.KEType.ORBIT) {
                details = keplerElements[i].orbit.DumpInfo();
            } else {
                details = keplerElements[i].pathProp.DumpInfo();
            }
            sb.Append(string.Format("    {0} t={1:0.0} {2} {3}\n", 
                    i, keplerElements[i].timeStart, 
                    (keplerElements[i].maneuver != null) ? keplerElements[i].maneuver.label : "custom",
                    details));
            if (keplerElements[i].maneuver != null)
                sb.Append(string.Format("      from Maneuver {0}\n", keplerElements[i].maneuver.label));
        }
        sb.Append("\n");
        return sb.ToString();
    }

}
