using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gravity state.
/// Hold "most" of the information for gravitational evolution of the system. 
/// 
/// The NBody objects added can be massive or massless. They can independently be in normal gravitational motion
/// or have their motion FIXED in some way (either by a Kepler/on-rail evolution mode or simply being not movable).
/// 
/// Massive bodies are tracked here by the arrays m[] and r[]. These arrays are then updated by passing to the 
/// selected numerical integrator. This allows a central object manager to compute the mutual gravitational 
/// interactions in the most effecient way. Each fixed frame the r[] value are copied back to the transform 
/// positions of the associated game objects (this is done in the GE class). 
/// 
/// A parallel list of FixedBodies is maintained BUT they are also in the r[] list, since their masses may
/// affect non-fixed bodies. 
/// 
/// Massless bodies are evolved seperately using a simple Leapfrog integrator (unless Optimize Massless has been
/// set to false). 
/// 
/// Particles are always evolved seperately using a simpe Leapfrog integrator. 
/// 
/// A scene may have more than one gravity state. Additional copies may be used for trajectory prediction, to 
/// determine future paths objects will take.
///
/// </summary>
public class GravityState
{

    public const int NDIM = 3; // Here to "de-magic" numbers. Some integrators have 3 baked in. Do not change.

    public int numBodies;

    // Since we cannot put a fixed length array in a C# struct, flatten out the r, v elements
    // (Integrators generally flatten this to avoid a loop anyway, so less dumb than it seems)

    public struct NbodyState
    {
        public double m;
        public double r_x;
        public double r_y;
        public double r_z;
        public double v_x;
        public double v_y;
        public double v_z;
        public double size2;
        public double timeCreated; 
        public bool massless; 
        public bool isFixed;
        public bool isActive;
        public bool noUpdate;
        public bool recordTrajData;
        // handle for info about custom forces used by force delegates
        public ForceData forceData;
    }

    protected NbodyState[] nbodyStates;

    public List<GravityEngine.FixedBody> fixedBodies;

    // need to keep size^2 for simple collision detection between particles and 
    // massive bodies. Collisions between massive bodies are left to usual Unity
    // collider intrastructure

    //! size of the arrays (may exceed the number of bodies due to pre-allocation)
    public int arraySize;

    //! time of current state (in the engine physics time)
    public double time;

    //! State has trajectories that require updating
    public bool hasTrajectories;

    //!  physical time per evolver since start OR last timescale change
    public enum Evolvers { MASSIVE, FIXED, PARTICLES };
    public double[] physicalTime;

    //! Flag to indicate running async (on a non-main thread). If true, cannot do debug logging or access scene
    public bool isAsync;

    // Integrators - these are held in GE
    public INBodyIntegrator integrator;

    public List<GravityParticles> gravityParticles;

    // Delegate for handling Maneuvers. Access through GE wrapper methods, but
    // separate implementation in a delegate.
    public ManeuverMgr maneuverMgr;

    // Handle triggers (these are polled on each engine Dt update)
    private GETriggerMgr triggerMgr; 

    //! All bodies are on rails (true until a body is added which is not on rails)
    private bool onRails = true; 

    private const double EPSILON = 1E-4; 	// minimum distance for gravitatonal force

    private IForceDelegate forceDelegate;

    //! A force may be selective. Selective force needs to track integrator internal index structure
    private SelectiveForceBase selectiveForce;

    public bool isCopy;

    // As KeplerSequences change segments, the Kepler depth can change (e.g. xfer to moon SOI)
    // This happens as fixedBodies is being iterated over so need to dump changes onto a list and do
    // after the iteration in MoveFixedBodies
    private List<GravityEngine.FixedBody> keplerDepthChanged;

    // Flag to track velocity reversals. When running in reversed mode still want to report the velocities
    // of NBody objects in the forward directions (but integrator needs their state to be reversed to evolve to
    // earlier positions)
    private bool velocitiesReversed = false;

    private GravityEngine ge;
    private GERewindMgr rewindMgr;


    /// <summary>
    /// New Gravity state. Also need to call SetAlgorithmAndForce to fully configure. 
    /// </summary>
    /// <param name="size"></param>
    public GravityState(int size) {

        InitArrays(size);
        gravityParticles = new List<GravityParticles>();
        fixedBodies = new List<GravityEngine.FixedBody>();
        keplerDepthChanged = new List<GravityEngine.FixedBody>();

        maneuverMgr = new ManeuverMgr();
        triggerMgr = new GETriggerMgr();
        onRails = true;

#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Created new (empty) gravityState");
#pragma warning restore 162
        ge = GravityEngine.Instance();
        if (ge.rewindModeEnabled) {
            rewindMgr = new GERewindMgr();
        }
    }

    /// <summary>
    /// Clone constructor
    /// 
    /// Creates a deep copy suitable for independent evolution as a trajectory or for maneuver iterations. 
    /// Maneuvers will be executed but the callback to motify the owner of the maneuver will be skipped (only
    /// the real evolution will notify).
    /// </summary>
    /// <param name="fromState"></param>
    public GravityState(GravityState fromState) {
        nbodyStates = new NbodyState[fromState.arraySize];

        physicalTime = new double[System.Enum.GetNames(typeof(Evolvers)).Length];
        arraySize = fromState.arraySize;
        numBodies = fromState.numBodies;
        onRails = fromState.onRails;

        ge = fromState.ge;

        // omitting hasTrajectories
        integrator = fromState.integrator.DeepClone();

        // don't copy particles, but need to init list
        gravityParticles = new List<GravityParticles>();

        // DO copy the maneuvers
        maneuverMgr = new ManeuverMgr(fromState.maneuverMgr);
        triggerMgr = new GETriggerMgr(fromState.triggerMgr);

        fixedBodies = new List<GravityEngine.FixedBody>(fromState.fixedBodies);

        for (int i = 0; i < physicalTime.Length; i++) {
            physicalTime[i] = fromState.physicalTime[i];
        }
        time = fromState.time;
        forceDelegate = fromState.forceDelegate;
        selectiveForce = fromState.selectiveForce;
  
        keplerDepthChanged = new List<GravityEngine.FixedBody>(fromState.keplerDepthChanged);

        for (int i = 0; i < arraySize; i++) {
            nbodyStates[i] = fromState.nbodyStates[i];
        }

        // copies do not notify maneuver owners of maneuver completion. They are assumed to be "what if"
        // evolutions
        isCopy = true;

#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Created new (copy) gravityState");
#pragma warning restore 162
    }

    public NbodyState[] GetNbodyStates()
    {
        return nbodyStates;
    }

    /// <summary>
    /// Set the integrator required for the chosen algorithm
    /// </summary>
    /// <param name="algorithm"></param>
    public void SetAlgorithmAndForce(GravityEngine.Algorithm algorithm, IForceDelegate forceDelegate) {
        this.forceDelegate = forceDelegate;
        // cast may be null if no force selection
        if (forceDelegate is SelectiveForceBase) {
            selectiveForce = (SelectiveForceBase)forceDelegate;
        }
        switch (algorithm) {
            case GravityEngine.Algorithm.LEAPFROG:
                integrator = new LeapfrogIntegrator(forceDelegate);
                break;
            case GravityEngine.Algorithm.HERMITE8:
                integrator = new HermiteIntegrator(forceDelegate);
                break;
            case GravityEngine.Algorithm.AZTRIPLE:
                integrator = new AZTripleIntegrator();
                break;
            default:
                Debug.LogError("Unknown algortithm");
                break;
        }
    }

    public void InitArrays(int arraySize) {
        nbodyStates = new NbodyState[arraySize];

        physicalTime = new double[System.Enum.GetNames(typeof(Evolvers)).Length];
        this.arraySize = arraySize;
        if (selectiveForce) {
            selectiveForce.Init(arraySize);
        }
    }

    public bool GrowArrays(int growBy) {

        integrator.GrowArrays(growBy);

        NbodyState[] fromState = nbodyStates;

        int newSize = arraySize + growBy;
        nbodyStates = new NbodyState[newSize];

        for (int i = 0; i < arraySize; i++) {
            nbodyStates[i] = fromState[i];
        }
        arraySize += growBy;

        if (selectiveForce) {
            selectiveForce.IncreaseToSize(arraySize);
        }

        return true;
    }

    public void Clear() {
        numBodies = 0;
        integrator.Clear();
        gravityParticles.Clear();
        fixedBodies.Clear();
    }


    public void ResetPhysicalTime() {
        for (int i = 0; i < physicalTime.Length; i++) {
            physicalTime[i] = 0.0;
        }
    }

    public void AddFixedBody(GravityEngine ge, int index, GravityEngine.FixedBody fixedBody, IFixedOrbit fixedOrbit) {
        // Need to maintain order by kepler depth
        int insertAt = fixedBodies.Count;
        for (int i = 0; i < fixedBodies.Count; i++) {
            if (fixedBody.kepler_depth < fixedBodies[i].kepler_depth) {
                insertAt = i;
                break;
            }
        }
        // Fixed orbit bodies need to preEvolve and evolve so that their positions are updated
        // (needed during heirarchical add of Kepler mode objects)
        if (fixedOrbit != null) {
            fixedOrbit.PreEvolve(ge.physToWorldFactor, ge.massScale);
            double[] r_new = new double[3];
            double[] v_new = new double[3];
            fixedOrbit.Evolve(physicalTime[(int)GravityState.Evolvers.MASSIVE], this, ref r_new, ref v_new);
            nbodyStates[index].r_x = r_new[0];
            nbodyStates[index].r_y = r_new[1];
            nbodyStates[index].r_z = r_new[2];
            nbodyStates[index].v_x = v_new[0];
            nbodyStates[index].v_y = v_new[1];
            nbodyStates[index].v_z = v_new[2];
        }
        nbodyStates[index].isFixed = true;
        nbodyStates[index].isActive = true;
        fixedBodies.Insert(insertAt, fixedBody);
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            Debug.Log(string.Format("GS add fixed body {0}", fixedBody.nbody.gameObject.name));
        }
#pragma warning restore 162        // enable unreachable code warning
    }

    public void RemoveFixedBody(NBody nbody) {
        // find object in FixedBodies list and remove
        GravityEngine.FixedBody fbRemove = null;
        foreach (GravityEngine.FixedBody fb in fixedBodies) {
            if (fb.nbody == nbody) {
                fbRemove = fb;
                break;
            }
        }
        if (fbRemove != null) {
            fixedBodies.Remove(fbRemove);
        }
    }

    /// <summary>
    /// Recompute and update the kepler depth of a fixed body.
    /// </summary>
    /// <param name="nbody"></param>
    public void UpdateKeplerDepth(NBody nbody, OrbitUniversal orbitU) {
        if (nbody.engineRef == null)
            return;
        GravityEngine.FixedBody fixedBody = nbody.engineRef.fixedBody;
        if (fixedBody == null)
            return;
        int depth = fixedBody.kepler_depth;
        int newDepth = OrbitUtils.CalcKeplerDepth(orbitU);
        if (newDepth != depth) {
            fixedBody.kepler_depth = newDepth;
            keplerDepthChanged.Add(fixedBody);
        }
    }

    public void AddNBody(NBody nbody, float massScale, float physToWorldFactor, bool isFixed) {
        Vector3d physicsPosition;
        Vector3d velBody;
        if (nbody.initWithDouble) {
            physicsPosition = nbody.initialPhysPositionV3 / physToWorldFactor;
            velBody = nbody.vel_physV3;
        } else {
            physicsPosition = new Vector3d(nbody.initialPhysPosition / physToWorldFactor);
            velBody = new Vector3d(nbody.vel_phys);
        }
        // fixed bodies will already have computed this as part of AddFixedBody
        if (!isFixed) {
            nbodyStates[numBodies].r_x = physicsPosition.x;
            nbodyStates[numBodies].r_y = physicsPosition.y;
            nbodyStates[numBodies].r_z = physicsPosition.z;
            nbodyStates[numBodies].v_x = velBody.x;
            nbodyStates[numBodies].v_y = velBody.y;
            nbodyStates[numBodies].v_z = velBody.z;
        }
        nbodyStates[numBodies].size2 = nbody.size * nbody.size;
        nbodyStates[numBodies].timeCreated = time;
        // mass scale is applied to internal record BUT leave nbody mass as is
        nbodyStates[numBodies].m = nbody.mass * massScale;
        nbodyStates[numBodies].isActive = true;
        nbodyStates[numBodies].massless = (nbody.mass == 0);
        nbodyStates[numBodies].forceData = nbody.GetComponent<ForceData>();
        nbodyStates[numBodies].isFixed = isFixed;
        integrator.AddNBody(numBodies, nbody, nbodyStates);
        numBodies++;
        UpdateOnRails();
        if (ge.RecordForRewind()) {
            rewindMgr.RecordAddBody(nbody, time);
        }
    }

    /// <summary>
    /// Remove the body at index and shuffle up the rest. Ensure the integrator does the same to stay
    /// in alignment. 
    /// </summary>
    /// <param name="index"></param>
    public void RemoveNBody(NBody nbody) {
        if (ge.RecordForRewind()) {
            rewindMgr.RecordRemoveBody(this, nbody, time);
        }
        integrator.RemoveBodyAtIndex(nbody.engineRef.index);
        // shuffle the rest down, update indices
        for (int j = nbody.engineRef.index; j < (numBodies - 1); j++) {
            nbodyStates[j] = nbodyStates[j + 1];
        }
        numBodies--;
        if (selectiveForce) {
            selectiveForce.RemoveBody(nbody.engineRef.index);
        }
        UpdateOnRails();
    }

    /// <summary>
    /// Check if all bodies are on rails. 
    /// 
    /// Called after a RemoveBody in GE. Need to move to a single state RemoveBody() but wait until do 
    /// masslessEngine refactor into integrator. 
    /// 
    /// internal use only. 
    /// </summary>
    public void UpdateOnRails() {
        onRails = false;
        if (fixedBodies.Count == numBodies) { 
                onRails = true;
        }
    }

    // Integrators need to use known positions to pre-determine accel. etc. to 
    // have valid starting values for evolution
    public void PreEvolve(GravityEngine ge) {
        // particles will pre-evolve when loading complete
        foreach (GravityEngine.FixedBody fixedBody in fixedBodies) {
            if (fixedBody.fixedOrbit != null) {
                fixedBody.fixedOrbit.PreEvolve(ge.physToWorldFactor, ge.massScale);
            }
        }
        integrator.PreEvolve(this);
    }

    /// <summary>
    /// Are all bodies in the world state "on-rails"?
    /// </summary>
    /// <returns></returns>
    public bool IsOnRails() {
        return onRails;
    }

    /*******************************************
    * Main Physics Loop
    ********************************************/

    /// <summary>
    /// Evolve the objects subject to gravity. 
    /// 
    /// Normal evolution is done by passing in worldState with a time interval corresponding to the 
    /// frame advance time multiplied by the time zoom. 
    /// 
    /// For trajectory updates in the case where the trajectory is up to date, this will be for the same interval
    /// but starting at a future time. 
    /// 
    /// In order for trajectory computation to "catch up", there are times when the interval may be longer (but limited
    /// by the re-compute factor to avoid a huge recomputation on a single frame). 
    /// 
    /// Maneuvers are ONLY added to the world state, so code in the trajectory state will not encounter maneuvers. 
    /// </summary>
    ///
    /// <param name="ge">The Gravity engine</param>
    /// <param name="timeStep">The amount of physics DT to be evolved</param>
    /// 
    public bool Evolve(GravityEngine ge, double timeStep) {
        double gameDt = timeStep;
        double timeEnd = time + timeStep;
        bool trajectoryRestart = false;
        if (maneuverMgr.HaveManeuvers()) {
            List<Maneuver> maneuversInDt = maneuverMgr.ManeuversUntil((float)timeEnd);
            if (maneuversInDt.Count > 0) {
                foreach (Maneuver m in maneuversInDt) {
                    // evolve up to the time of the earliest maneuver
                    gameDt = Math.Max(m.worldTime - time, 0.0);
                    if (gameDt > 1E-7)
                        EvolveForTimestep(ge, gameDt, exactTime: true);
                    if (ge.RecordForRewind()) {
                        rewindMgr.RecordManeuver(this, m, time);
                    }
                    maneuverMgr.Execute(m, this, isCopy);
                }
                // recompute remaining time to evolve
                gameDt = Math.Max(timeEnd - time, 0.0);
                // if trajectories have made predictions, these need to be re-done since a manuever has
                // occured
                trajectoryRestart = true;           
            }
        }
        EvolveForTimestep(ge, gameDt);
        return trajectoryRestart;
    }

    public bool EvolveReversed(GravityEngine ge, double timeStep)
    {
        double gameDt = timeStep;
        double timeEnd = time - timeStep;
        if (rewindMgr.HaveEntries()) {
            List<GERewindMgr.RewindEntry> rEntries = rewindMgr.EntriesAfter(timeEnd);
            if (rEntries.Count > 0) {
                foreach (GERewindMgr.RewindEntry re in rEntries) {
                    // evolve up to the time of the earliest maneuver
                    // EvolveForTimestepReversed want a +ve time interval to back up
                    gameDt = Math.Max(time - re.t, 0.0);
                    if (gameDt > 1E-7)
                        EvolveForTimestepReversed(ge, gameDt, exactTime: true);
                    rewindMgr.ApplyEvent(this, re);
                }
                // recompute remaining time to evolve
                gameDt = Math.Max(time - timeEnd, 0.0);
            }
        }
        EvolveForTimestepReversed(ge, gameDt);
        return false;
    }

    public void MoveFixedBodies(double time) {
        //==============================
        // Fixed Bodies
        //==============================
        // Evolution is to a specific time - so use massive object physical time
        double[] r_new = new double[NDIM];
        double[] v_new = new double[NDIM];
        foreach (GravityEngine.FixedBody fixedBody in fixedBodies) {
            if (!nbodyStates[fixedBody.nbody.engineRef.index].isActive)
                continue;
            // "if" needed for case where fixed body in process of going off-rails
            if (fixedBody.nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED) {
                fixedBody.fixedOrbit.Evolve(time, this, ref r_new, ref v_new);
                int i = fixedBody.nbody.engineRef.index;
                if (NUtils.Array1DNaN(r_new))
                    Debug.LogError("Boom r_new");
                nbodyStates[i].r_x = r_new[0];
                nbodyStates[i].r_y = r_new[1];
                nbodyStates[i].r_z = r_new[2];
                nbodyStates[i].v_x = v_new[0];
                nbodyStates[i].v_y = v_new[1];
                nbodyStates[i].v_z = v_new[2];
            }
        }
        foreach(GravityEngine.FixedBody fb in keplerDepthChanged) {
            // fixed bodies is ordered by orbit depth
            fixedBodies.Remove(fb);
            int insertAt = fixedBodies.Count;
            for (int i = 0; i < fixedBodies.Count; i++) {
                if (fb.kepler_depth < fixedBodies[i].kepler_depth) {
                    insertAt = i;
                    break;
                }
            }
            fixedBodies.Insert(insertAt, fb);
        }
        // MUST clear after, since OrbitU.SetNewCenter() may have added some things to update
        keplerDepthChanged.Clear();
    }

    private void EvolveForTimestep(GravityEngine ge, double physicsDt, bool exactTime = false) {
        // if everything is on rails, can just jump to the end time
        if (onRails && !triggerMgr.HaveTriggers()) {
            time += physicsDt;
            MoveFixedBodies(time);
            MoveParticles(ge);
            // Keep these up-to-date so can flip to off-rails if needed
            physicalTime[(int)Evolvers.MASSIVE] = time;
            return;
        }
        // Objective is to keep physical time proportional to game time 
        // Each integrator will run for at least as long as it is told but may overshoot
        // so correct time on next iteration. 
        // 
        // Keep the current physical time each integrator has reached in physicalTime[integrator_type]
        //
        double engineDt = ge.engineDt;
        if (physicsDt < engineDt)
            return;

        double timeThisStep = 0;

        // Need to move the integrators forward concurrently in steps matching the engineDt
        // - Hermite may be using a different timestep than this
        // - particles likely use a much longer timestep

        while (timeThisStep < physicsDt) {
            //==============================
            // Massive bodies
            //==============================
            // evolve all the massive game objects 
            double timeEvolved = 0.0;
            if (numBodies > fixedBodies.Count) {
                // typical path - have massive bodies: use NBody integration
                // Exact time case
                double timeToEvolve = engineDt;
                if (exactTime && ((timeThisStep + engineDt) > physicsDt)) {
                    timeToEvolve = physicsDt - timeThisStep;
                }
                timeEvolved = integrator.Evolve(timeToEvolve, this, exactTime);
                physicalTime[(int)Evolvers.MASSIVE] += timeEvolved;
                timeThisStep += timeEvolved;
            } else {
                // all Kepler mode: skip integration
                physicalTime[(int)Evolvers.MASSIVE] += engineDt;
                timeThisStep += engineDt;
            }

            //==============================
            // Fixed Bodies
            //==============================
            // Update fixed update objects (if any)
            // Evolution is to a specific time - so use massive object physical time
            MoveFixedBodies(physicalTime[(int)Evolvers.MASSIVE]);

            // LF is built in to particles. It has it's own DT built in
            // and runs on a fixed timestep (if it is varied energy conservation is wrecked)
            // Track particle evolution vs wall clock time seperately

            MoveParticles(ge);

            // must update time so trajectory times are up to date
            time = physicalTime[(int)Evolvers.MASSIVE];
            if (hasTrajectories) {
                ge.UpdateTrajectories();
            }

            // if there are triggers registered, run them
            if (triggerMgr.HaveTriggers()) {
                triggerMgr.RunTriggers(this);
            }

        } // while
    }

    private void MoveParticles(GravityEngine ge)
    {
        //==============================
        // Particles (should only be present in worldState)
        //==============================
        if (gravityParticles.Count > 0) {
            double particle_dt = ge.GetParticleDt();
            if (physicalTime[(int)GravityState.Evolvers.PARTICLES] <
                    physicalTime[(int)GravityState.Evolvers.MASSIVE]) {
                double evolvedFor = 0.0;
                if (forceDelegate != null) {
                    foreach (GravityParticles nbp in gravityParticles) {
                        evolvedFor = nbp.EvolveWithForce(particle_dt, numBodies, this, forceDelegate);
                    }
                } else {
                    foreach (GravityParticles nbp in gravityParticles) {
                        evolvedFor = nbp.Evolve(particle_dt, numBodies, this);
                    }
                }
                physicalTime[(int)Evolvers.PARTICLES] += evolvedFor;
            }
        }
    }

    /// <summary>
    /// Evolve backwards
    /// - when there are fixed bodies, these are evolved based on absolute time, so cannot just fake it by reversing velocities and
    ///   running forward
    /// - DO let the integrators just run forrward
    /// - do not evolve particles
    /// - do not run triggers
    /// 
    /// </summary>
    /// <param name="ge"></param>
    /// <param name="physicsDt">Amount of time to be reversed (positive number!)</param>
    public void EvolveForTimestepReversed(GravityEngine ge, double physicsDt, bool exactTime = false)
    {
        // if everything is on rails, can just jump to the end time
        if (onRails && !triggerMgr.HaveTriggers()) {
            time -= physicsDt;    // MINUS the timestep
            MoveFixedBodies(time);
            // Keep these up-to-date so can flip to off-rails if needed
            physicalTime[(int)Evolvers.MASSIVE] = time;
            physicalTime[(int)Evolvers.PARTICLES] = time;
            return;
        }
        // Objective is to keep physical time proportional to game time 
        // Each integrator will run for at least as long as it is told but may overshoot
        // so correct time on next iteration. 
        // 
        // Keep the current physical time each integrator has reached in physicalTime[integrator_type]
        //
        double engineDt = ge.engineDt;
        if (physicsDt < engineDt)
            return;

        double timeEvolved = 0;

        // Need to move the integrators forward concurrently in steps matching the engineDt
        // - Hermite may be using a different timestep than this
        // - particles likely use a much longer timestep

        while (timeEvolved < physicsDt) {
            //==============================
            // Massive bodies
            //==============================
            // evolve all the massive game objects 
            double massiveDt = 0.0;
            if (numBodies > fixedBodies.Count) {
                // typical path - have massive bodies: use NBody integration
                massiveDt = integrator.Evolve(engineDt, this, reversing: true);
                physicalTime[(int)Evolvers.MASSIVE] -= massiveDt;
                timeEvolved += massiveDt;
            } else {
                // all Kepler mode: skip integration
                physicalTime[(int)Evolvers.MASSIVE] -= engineDt;
                timeEvolved += engineDt;
            }

            //==============================
            // Fixed Bodies
            //==============================
            // Update fixed update objects (if any)
            // Evolution is to a specific time - so use massive object physical time
            MoveFixedBodies(physicalTime[(int)Evolvers.MASSIVE]);

        } // while
        time = physicalTime[(int)Evolvers.MASSIVE];
    }

    /// <summary>
    /// Get the internal position used by the physics engine. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPosition(NBody nbody) {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            return Vector3.zero;
        }
        // Fixed bodies updated r[], no need to ask again.
        return new Vector3((float)nbodyStates[nbody.engineRef.index].r_x,
                           (float)nbodyStates[nbody.engineRef.index].r_y,
                           (float)nbodyStates[nbody.engineRef.index].r_z);
    }

    public Vector3d GetPhysicsPositionDoubleV3(NBody nbody)
    {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            return Vector3d.zero;
        }
        // Fixed bodies updated r[], no need to ask again.
        return new Vector3d(nbodyStates[nbody.engineRef.index].r_x,
                           nbodyStates[nbody.engineRef.index].r_y,
                           nbodyStates[nbody.engineRef.index].r_z);
    }

    /// <summary>
    /// Get the internal position used by the physics engine. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetPhysicsPositionDouble(NBody nbody) {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            return Vector3d.zero;
        }
        return new Vector3d(nbodyStates[nbody.engineRef.index].r_x,
                            nbodyStates[nbody.engineRef.index].r_y,
                            nbodyStates[nbody.engineRef.index].r_z);
    }
    /// <summary>
    /// Get the physics velocity for an NBody as a double[]. 
    /// 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetVelocityDouble(NBody nbody, ref double[] vel) {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
        }        
        // If Kepler evolution, evolve will have stored the velocity
        int i = nbody.engineRef.index;
        double vSign = 1.0;
        if (velocitiesReversed && !nbodyStates[i].isFixed)
            vSign = -1.0;
        vel[0] = vSign * nbodyStates[i].v_x;
        vel[1] = vSign * nbodyStates[i].v_y;
        vel[2] = vSign * nbodyStates[i].v_z;
    }

    public Vector3d GetVelocity3d(NBody nbody)
    {
        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
            return Vector3d.zero;
        }
        int i = nbody.engineRef.index;
        double vSign = 1.0;
        if (velocitiesReversed && !nbodyStates[i].isFixed)
            vSign = -1.0;
        Vector3d v =  new Vector3d(nbodyStates[i].v_x, nbodyStates[i].v_y, nbodyStates[i].v_z);
        return vSign * v;
    }

    public void UpdatePositionAndVelocity(NBody nbody, Vector3d pos, Vector3d vel, Maneuver m = null)
    {
        if (nbody.engineRef == null) {
            Debug.LogError("nbody has not been added to engine " + nbody.gameObject.name);
            return;
        }
        if (ge.RecordForRewind())
        {
            // record old velocity
            rewindMgr.RecordPositionVelocityChange(this, nbody,  time);
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED) {
            // Allow a fixed body to set the v state. This will get over-written on the
            // next Evolve() but it is used when e.g. an OrbitPoint changes and this
            // needs to be immediatly available to other code.
            int i = nbody.engineRef.index;
            if ((m != null) && (m.relativeTo != null) && m.HasRelativePosVel() ) {
                Vector3d centerPos = GetPhysicsPositionDoubleV3(m.relativeTo);
                Vector3d centerVel = GetVelocity3d(m.relativeTo);
                nbodyStates[i].r_x = m.relativePos.x + centerPos.x;
                nbodyStates[i].r_y = m.relativePos.y + centerPos.y;
                nbodyStates[i].r_z = m.relativePos.z + centerPos.z;
                nbodyStates[i].v_x = m.relativeVel.x + centerVel.x;
                nbodyStates[i].v_y = m.relativeVel.y + centerVel.y;
                nbodyStates[i].v_z = m.relativeVel.z + centerVel.z;
            } else {
                nbodyStates[i].r_x = pos.x;
                nbodyStates[i].r_y = pos.y;
                nbodyStates[i].r_z = pos.z;
                nbodyStates[i].v_x = vel.x;
                nbodyStates[i].v_y = vel.y;
                nbodyStates[i].v_z = vel.z;
            }
            // Normally do not get this for KS since maneuvers usually add a segement except when there is a beforeExecuted callback.
            // In that case, add a new orbit segement. (This ensures the before executed happens close to the correct time and
            // we don't jump way past it, since core loop will loop until the maneuver time)
            double time = physicalTime[(int)Evolvers.MASSIVE];
            if (nbody.engineRef.fixedBody.keplerSeq != null) {
                NBody center = nbody.engineRef.fixedBody.keplerSeq.GetCenterNBody();
                nbody.engineRef.fixedBody.keplerSeq.AppendElementRVT(pos, vel, time, relativePos: false, nbody, center, null, m);
                // when using tester, the testDone callback will happen and this OU has not had a chance to become active
                nbody.engineRef.fixedBody.keplerSeq.AdvanceToNextSegment();
            // Need something like this...
            //    fo.SetPositionDouble
            } else {
                // if this is an OrbitU need to set new initial conditions with this velocity
                // (this means will not be able to rewind to times earlier than this)
                OrbitUniversal orbitU = nbody.engineRef.fixedBody.orbitU;
                if (orbitU != null) {
                    NBody center = orbitU.GetCenterNBody();
                    if ((m != null) && (m.relativeTo != null) && m.HasRelativePosVel() ) {
                        orbitU.InitFromRVT(m.relativePos, m.relativeVel, m.worldTime, center, relativePos: true);
                    } else {
                        orbitU.InitFromRVT(pos, vel, time, center, relativePos: false);
                    }
                }
            }
        } else {
            double[] velArray = new double[] { vel.x, vel.y, vel.z };
            int i = nbody.engineRef.index;
            nbodyStates[i].r_x = pos.x;
            nbodyStates[i].r_y = pos.y;
            nbodyStates[i].r_z = pos.z;
            nbodyStates[i].v_x = vel.x;
            nbodyStates[i].v_y = vel.y;
            nbodyStates[i].v_z = vel.z;
            if (GravityEngine.instance.geMultiplayerIF != null) {
                GravityEngine.instance.geMultiplayerIF.StateChanged(nbody);
            }
        }
    }

    /// <summary>
    /// Set the physics velocity from a double array. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    /// 
    public void SetVelocityDouble(NBody nbody, ref double[] velocity) {
        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
            return;
        }
        SetVelocity3d(nbody, new Vector3d(ref velocity));
    }

    // ICK: - ditch the double[] version eventually
    public void SetVelocity3d(NBody nbody, Vector3d velocity)
    {
        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
            return;
        }
        if (ge.RecordForRewind())
        {
            // record old velocity
            rewindMgr.RecordVelocityChange(this, nbody, GetVelocity3d(nbody), time);
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED)
        {
            // Allow a fixed body to set the v state. This will get over-written on the
            // next Evolve() but it is used when e.g. an OrbitPoint changes and this
            // needs to be immediatly available to other code.
            int i = nbody.engineRef.index;
            nbodyStates[i].v_x = velocity[0];
            nbodyStates[i].v_y = velocity[1];
            nbodyStates[i].v_z = velocity[2];
            // Normally do not get this for KS since maneuvers usually add a segement except when there is a beforeExecuted callback.
            // In that case, add a new orbit segement. (This ensures the before executed happens close to the correct time and
            // we don't jump way past it, since core loop will loop until the maneuver time)
            Vector3d pos = GetPhysicsPositionDouble(nbody);
            Vector3d vel = new Vector3d(velocity[0], velocity[1], velocity[2]);
            double time = physicalTime[(int)Evolvers.MASSIVE];
            if (nbody.engineRef.fixedBody.keplerSeq != null)
            {
                NBody center = nbody.engineRef.fixedBody.keplerSeq.GetCenterNBody();
                nbody.engineRef.fixedBody.keplerSeq.AppendElementRVT(pos, vel, time, relativePos: false, nbody, center, null);
                // when using tester, the testDone callback will happen and this OU has not had a chance to become active
                nbody.engineRef.fixedBody.keplerSeq.AdvanceToNextSegment();
                // TODO: Network update of Kepler Sequence
            }
            else
            {
                // if this is an OrbitU need to set new initial conditions with this velocity
                // (this means will not be able to rewind to times earlier than this)
                OrbitUniversal orbitU = nbody.engineRef.fixedBody.orbitU;
                if (orbitU != null)
                {
                    NBody center = orbitU.GetCenterNBody();
                    orbitU.InitFromRVT(pos, vel, time, center, relativePos: false);
                }
            }
        }
        else
        {
            int i = nbody.engineRef.index;
            nbodyStates[i].v_x = velocity[0];
            nbodyStates[i].v_y = velocity[1];
            nbodyStates[i].v_z = velocity[2];
            if (GravityEngine.instance.geMultiplayerIF != null)
            {
                GravityEngine.instance.geMultiplayerIF.StateChanged(nbody);
            }
        }
    }

    /// <summary>
    /// Internal use only from OrbitUniversal InitFromRVT
    /// Ensures cached state is current so can use info prior to the next Evolve call. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="pos"></param>
    /// <param name="velocity"></param>
    public void UpdateInternalDouble(NBody nbody, Vector3d pos, Vector3d velocity)
    {
        // may not have been added yet.
        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
            return;
        }

        if (nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED) {
            // Allow a fixed body to set the v state. This will get over-written on the
            // next Evolve() but it is used when e.g. an OrbitPoint changes and this
            // needs to be immediatly available to other code.
            int i = nbody.engineRef.index;
            nbodyStates[i].r_x = pos.x;
            nbodyStates[i].r_y = pos.y;
            nbodyStates[i].r_z = pos.z;
            nbodyStates[i].v_x = velocity.x;
            nbodyStates[i].v_y = velocity.y;
            nbodyStates[i].v_z = velocity.z;
        }
    }

    /// <summary>
    /// Set the physics velocity from a double array. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    public void SetPosition3d(NBody nbody, Vector3d pos) {
        if (nbody.engineRef == null) {
            Debug.LogError("nbody has not been added to engine " + nbody.gameObject.name);
        }
        if (nbody.engineRef.fixedBody != null) {
            if (nbody.engineRef.fixedBody.fixedOrbit.GetType() == typeof(FixedObject)) {
                FixedObject fo = (FixedObject)nbody.engineRef.fixedBody.fixedOrbit;
                fo.SetPositionDouble(pos);
            }
        }
        nbodyStates[nbody.engineRef.index].r_x = pos.x;
        nbodyStates[nbody.engineRef.index].r_y = pos.y;
        nbodyStates[nbody.engineRef.index].r_z = pos.z;
    
        if (GravityEngine.instance.geMultiplayerIF != null) {
            GravityEngine.instance.geMultiplayerIF.StateChanged(nbody);
        }
    }

    /// <summary>
    /// Return the internal physics engine mass. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public double GetMass(NBody nbody) {
        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            Debug.LogError("Either Nbody or Nbody.engineRef is null");
            return double.NaN;
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            return 0;
        }
        return nbodyStates[nbody.engineRef.index].m;
    }

    /// <summary>
    /// Get the internal physics time for massive bodies
    /// </summary>
    /// <returns>physics time for massive body integrator</returns>
    public double GetPhysicsTime() {
        return time;
    }

    /// <summary>
    /// @see GravityEngine#SetPhysicalTime for restrictions.
    /// </summary>
    /// <param name="newTime"></param>
    public void SetTime(double newTime, bool force) {
        if (!onRails && !force) {
            // could try to FF?
            Debug.LogWarning("Not all bodies are fixed. Cannot proceed.");
            return;
        }
        time = newTime;
        physicalTime[(int)Evolvers.MASSIVE] = newTime;
        physicalTime[(int)Evolvers.PARTICLES] = newTime;
        MoveFixedBodies(time);
    }

    /// <summary>
    /// Evolve all bodies to the new time (past or future). This will trigger a 
    /// integration sequence when there is at least one body off rails and this
    /// may cause a burst of CPU activity if the time delta is large.
    /// 
    /// Generally called through the GE wrapper for game logic. 
    /// </summary>
    /// <param name="newTime"></param>
    public void EvolveToTime(double newTime)
    {
        double timeDelta = newTime - time;
        GravityEngine ge = GravityEngine.instance;
        if (timeDelta > 0) {
            // just FF to new time
            EvolveForTimestep(ge, timeDelta);
        } else {
            ReverseVelocities();
            EvolveForTimestepReversed(ge, -timeDelta);
            ReverseVelocities();
        }
    }

    /// <summary>
    /// Apply a change in R,V to an NBody object in the past or future. 
    /// 
    /// The intended use is to allow an update that has experienced network delay to be applied at an earlier point in the 
    /// simulation to facilitate a distributed lockstep mode for multiplayer games. 
    /// </summary>
    /// <param name="rewindTime"></param>
    /// <param name="r"></param>
    /// <param name="v"></param>
    public void ApplyChangeAtTime(NBody nbody, double atTime, Vector3d r, Vector3d v)
    {
        double rewindTime = time - atTime;
        GravityState atTimeState = new GravityState(this);
        // DO want to record for rewind using THIS gs rewindMgr
        atTimeState.rewindMgr = rewindMgr;
        int i = nbody.engineRef.index;

        if (rewindTime < 0) {
            double dtime = -rewindTime;
            // move forward to required time
            atTimeState.EvolveForTimestep(ge, dtime);
            atTimeState.UpdatePositionAndVelocity(nbody, r, v);
            atTimeState.ReverseVelocities();
            atTimeState.EvolveForTimestepReversed(ge, dtime);
            atTimeState.ReverseVelocities();
        } else {
            // DEBUG
            //Vector3d rInitial = new Vector3d(nbodyStates[i].r_x, nbodyStates[i].r_y, nbodyStates[i].r_z);
            atTimeState.ReverseVelocities();
            // evolve integrator forward by the amount we need to rewind
            atTimeState.EvolveForTimestepReversed(ge, rewindTime);
            // move forward again
            atTimeState.ReverseVelocities();
            // apply change in the past
            atTimeState.UpdatePositionAndVelocity(nbody, r, v);
            // move back to present time
            atTimeState.EvolveForTimestep(ge, rewindTime);
        }
        // copy across the updated position of the changed entity
        nbodyStates[i] = atTimeState.nbodyStates[i];
        //Vector3d rFinal = new Vector3d(nbodyStates[i].r_x, nbodyStates[i].r_y, nbodyStates[i].r_z);
        //Debug.LogFormat("delta={0} from {1} to {2}", (rFinal - rInitial).magnitude, rInitial, rFinal);
    }

    public void ReverseVelocities()
    {
        velocitiesReversed = !velocitiesReversed;
        for (int i = 0; i < numBodies; i++) { 
            nbodyStates[i].v_x *= -1.0;
            nbodyStates[i].v_y *= -1.0;
            nbodyStates[i].v_z *= -1.0;
        }
    }

    public GERewindMgr GetGERewindMgr()
    {
        return rewindMgr;
    }

    public bool NbodyIsActive(NBody nbody)
    {
        return nbodyStates[nbody.engineRef.index].isActive;
    }

    public bool NbodyIndexIsActive(int  index)
    {
        return nbodyStates[index].isActive;
    }

    public void NbodySetIsActive(NBody nbody, bool state)
    {
         nbodyStates[nbody.engineRef.index].isActive = state;
    }

    public void NbodySetNoUpdateFlag(NBody nbody, bool state)
    {
        nbodyStates[nbody.engineRef.index].noUpdate = state;
    }

    public bool NbodyGetNoUpdateFlag(NBody nbody)
    {
        return nbodyStates[nbody.engineRef.index].noUpdate;
    }

    public bool NbodyGetNoUpdateFlag(int  index)
    {
        return nbodyStates[index].noUpdate;
    }

    public bool NbodyIsFixed(NBody nbody)
    {
        return nbodyStates[nbody.engineRef.index].isFixed;
    }

    public void NbodySetIsFixed(NBody nbody, bool state)
    {
        nbodyStates[nbody.engineRef.index].isFixed = state;
    }

    public void SetMass(NBody nbody, double mass)
    {
        nbodyStates[nbody.engineRef.index].m = mass;
        nbodyStates[nbody.engineRef.index].massless = (mass == 0);
    }

    public void SetSize2(NBody nbody, double size)
    {
        nbodyStates[nbody.engineRef.index].size2 = size;
    }

    // trigger manager wrapper
    public void AddTrigger(GETriggerMgr.Trigger t)
    {
        triggerMgr.AddTrigger(t);
    }

    public void RemoveTrigger(GETriggerMgr.Trigger t)
    {
        triggerMgr.RemoveTrigger(t);
    }

    public void ClearTriggers()
    {
        triggerMgr.Clear();
    }

    private double VelMagnitude(int index)
    {
        return Mathd.Sqrt(nbodyStates[index].v_x * nbodyStates[index].v_x +
                        nbodyStates[index].v_y * nbodyStates[index].v_y +
                        nbodyStates[index].v_z * nbodyStates[index].v_z);
    }

    // debug/console
    public string DumpAll(NBody[] gameNBodies, GravityEngine ge) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Massive Bodies:\n");
        for (int i = 0; i < numBodies; i++) {
            Vector3 vel = ge.GetVelocity(gameNBodies[i]);
            int engineIndex = gameNBodies[i].engineRef.index;
            sb.Append(string.Format("   n={0} {1} m={2} r={3} {4} {5} v={6} {7} {8} |v|= {14} t0={13:0.00} isActive={9} isFixed={10} engineRef.index={11} extAcc={12}\n", 
                i, gameNBodies[i].name,
                nbodyStates[i].m, nbodyStates[i].r_x, nbodyStates[i].r_y, nbodyStates[i].r_z,
                vel.x, vel.y, vel.z, nbodyStates[i].isActive, nbodyStates[i].isFixed, engineIndex, integrator.GetExternalAccelForIndex(engineIndex),
                nbodyStates[i].timeCreated, 
                vel.magnitude
            ));
        }
        sb.Append("Fixed Bodies:\n");
        for (int i = 0; i < fixedBodies.Count; i++) {
            int fb_index = fixedBodies[i].nbody.engineRef.index;
            sb.Append(string.Format("   n={0} {1} kepler_depth={2} v=({3},{4},{5}) |v|={6}\n",
                i,
                gameNBodies[fb_index].name,
                fixedBodies[i].kepler_depth,
                nbodyStates[fb_index].v_x, nbodyStates[fb_index].v_y, nbodyStates[fb_index].v_z, VelMagnitude(fb_index)
                ));
            // add extra info if OrbitU or KeplerSeq
            sb.Append(fixedBodies[i].nbody.engineRef.fixedBody.fixedOrbit.DumpInfo());
        }
        sb.Append("Particles:\n");
        foreach (GravityParticles nbp in gravityParticles) {
            sb.Append(string.Format("   {0} ", nbp.name));
        }
        sb.Append("\n" + maneuverMgr.DumpAll());
        return sb.ToString();
    }

    /// <summary>
    /// Deterimine the center of mass. 
    /// </summary>
    /// <returns></returns>
    public Vector3d ComputeCenterOfMass()
    {
        double m_total = 0.0;
        double[] r_cm = new double[3] { 0, 0, 0 };
        for (int i=0; i < numBodies; i++) {
            m_total += nbodyStates[i].m;
            r_cm[0] += nbodyStates[i].m * nbodyStates[i].r_x;
            r_cm[1] += nbodyStates[i].m * nbodyStates[i].r_y;
            r_cm[2] += nbodyStates[i].m * nbodyStates[i].r_z;
        }
        if (m_total < 1E-9)
            return Vector3d.zero;
        return new Vector3d(r_cm[0]/m_total, r_cm[1]/m_total, r_cm[2]/m_total);
    }

	/// <summary>
	/// Compute the velocity of the CM.
	/// Consider only massive bodies. If there are any FixedObject elements they will
	/// be weighted with a velocity of zero.
	/// </summary>
	/// <returns></returns>
    public Vector3d ComputeCenterOfMassVelocity()
    {
        double m_total = 0.0;
        double[] v_cm = new double[3] { 0, 0, 0 };
        for (int i = 0; i < numBodies; i++) {
            m_total += nbodyStates[i].m;
            v_cm[0] += nbodyStates[i].m * nbodyStates[i].v_x;
            v_cm[1] += nbodyStates[i].m * nbodyStates[i].v_y;
            v_cm[2] += nbodyStates[i].m * nbodyStates[i].v_z;
        }
        if (m_total < 1E-9)
            return Vector3d.zero;
        return new Vector3d(v_cm[0] / m_total, v_cm[1] / m_total, v_cm[2] / m_total);
    }

	/// <summary>
	/// Set the CM to the specified postion and velocity. 
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="vel"></param>
	public void SetCenterOfMass(GravityEngine ge, Vector3d pos, Vector3d vel)
	{
        Vector3d cmPos = ComputeCenterOfMass();
        Vector3d cmVel = ComputeCenterOfMassVelocity();
        // position
        Vector3d moveBy = pos - cmPos;
        ge.MoveAll(moveBy);

		// velocity
		// - fixed objects cannot be adjusted, so skip them
		// - on-rails/Kepler mode objects will pick up the velocity of their CM, so they can be skipped

		// massive objects
        Vector3d velAdjust = vel - cmVel;
        double[] v_int = new double[] { 0, 0, 0 };
        for (int i=0; i < numBodies; i++) {
            nbodyStates[i].v_x += velAdjust.x;
            nbodyStates[i].v_y += velAdjust.y;
            nbodyStates[i].v_z += velAdjust.z;
		}

        // particles
        foreach (GravityParticles nbp in gravityParticles) {
            nbp.AdjustVelocity(velAdjust);
        }

    }

    public void MoveAll(Vector3d moveBy)
    {
        for (int i = 0; i < numBodies; i++) {
            nbodyStates[i].r_x += moveBy.x;
            nbodyStates[i].r_y += moveBy.y;
            nbodyStates[i].r_z += moveBy.z;
        }
    }
}
