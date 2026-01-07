#if GE_NAMESPACE
namespace GravityEngine
{
#endif
    using UnityEngine;
    using System.Collections;

    /// <summary>
    /// Handles scaling between:
    ///     GE: internal values used in the physics integrators
    ///     Scene: values in Unity units (raw Unity position values and game time seconds)
    ///     World: The chosen units for values in inspector entries etc. (e.g. AU, km, m)
    ///     SI: Metric system values
    ///     
    /// The GE inspector allows for four choices of units, with different inspector inputs in each case:
    /// -DIMENSIONLESS:
    ///  + mass scale
    /// -SI: (m/kg/sec)
    ///  + Unity units per m  (lengthScale)
    ///  + Game sec. per SI sec. (timeScale)
    /// -ORBITAL (km/1E24 kg/hour)
    ///  + Unity units per km  (lengthScale)
    ///  + game sec. per world hour (timeScale)
    /// -SOLAR (AU/1E24 kg/year)
    ///  + Unity units per AU (lengthScale)
    ///  + game sec. per world year (timeScale)
    ///  
    /// Components that specifiy initial positions (e.g. OrbitEllipse) interpret the values provided as World units
    /// and convert them to internal GE units when they are added to GE. A position specified for a NBody in
    /// the inspector will indicate the active choice of units and set the NBody initialPos field. Scripts can
    /// set this field in world units directly.
    /// 
    /// GravityEngine.lengthScale specifies the Unity units per World unit value. For dimensionless units it is 1.
    /// 
    /// GravityEngine.timescale specifies game seconds per World time unit. For dimensionless units it is 1.
    /// 
    /// The internal units for mass and time do not in general align with any of the world unit choices. 
    /// This is a consequence of choosing to implement the gravity calculations in which the gravitational constant
    /// G=1 (this represents a specific choice of internal units, since we are always free to scale units to make
    /// G=1). This adjustment is made by [UpdateTimeScale](@ref UpdateTimeScale). Length is not changed, so length
    /// in World units match internal lengths (scene position differs by lengthScale). 
    /// 
    /// Scaling When Adding an NBody
    /// ============================
    /// position: 
    ///   (initialPos is specified in the NBody inspector in World units)
    ///   nbody.initialPhyPos = nbody.initialPos * lengthScale;
    ///   r = nbody.initialPhyPos/ phyToWorldFactor; 
    ///   (phyToWorldFactor allows dynamic scaling from a value in the GE inspector)
    ///   
    /// velocity:
    ///   vel_phys = nbody.vel * velocityScale;
    /// 
    /// mass:
    ///    m = nbody.mass * massScale;
    ///    
    ///    mass_scale is set by [UpdateMassScale](@ref UpdateMassScale)
    /// </summary>
    public class GravityScaler : MonoBehaviour
    {

        /// <summary>
        /// Units.
        /// Gravity N-Body simulations use units in which G=1 (G, the gravitational constant). 
        /// 
        /// From unit analysis of F = m_1 a = (G m_1 m_2)/r^2 we get: T = (L^3/G M)^(1/2) with
        ///  T = time, L = length, M = Mass, G=Newton's constant = 6.67E-11.
        ///
        ///  SI (m,kg) => T=1.224E5 sec
        ///  Solar (AU, 1E24 kg) => T = 7.083E9 sec.
        ///
        /// To control game time, mass is rescaled in the physics engine to acheive the desired result. 
        /// Initial velocities are also adjusted to appropriate scale. 
        /// </summary>
        public const float G = 6.67408E-11f;  // m^3 kg^(-1) sec^(-2)

        public enum Units { DIMENSIONLESS, SI, ORBITAL, SOLAR }; //, STELLAR};
        public Units units;
        private static string[] lengthUnits = { "DL", "m", "km", "AU", "light-year" };
        private static string[] massUnits = { "DL", "kg", "1E24 kg", "1E24 kg", "Msolar" };
        private static string[] velocityUnits = { "DL", "m/s", "km/hr", "km/s", "km/s" };

        private static float velocityScale;

        public const float M_PER_KM = 1000f;
        public const float M_PER_AU = 1.49598E+11f;
        public const float SEC_PER_YEAR = 3600f * 24f * 365.25f;
        public const float KM_HOUR_TO_M_SEC = M_PER_KM / SEC_PER_HOUR;
        public const float M_SEC_TO_KM_HOUR = 1 / KM_HOUR_TO_M_SEC;

        public const float SECS_PER_SIDEREAL_DAY = 86164.1f;

        public const float FT_TO_M = 0.3048f;
        public const float FTSEC_TO_KMHR = 1.09728f;

        public const float KM_SEC_TO_AU_YR = 0.210949527f;

        // km/ 1E24kg/ hr
        public const float SEC_PER_HOUR = 3600f;
        public const float SEC_PER_DAY = 3600 * 24f;


        private const double NEWTONS_G_SI = 6.67430E-11;
        private const double NEWTONS_G_KMHR = 864989280000;
        private const double NEWTONS_G_AUYR = 1.98534641318525E-05;

        // Use T = Sqrt(L^3/(G M)) with all units and G in m/s/kg.
        // This gives the time unit that results from the choice of length/mass and the imposition of G=1
        // in the integrators. i.e. 1 internal time unit is equalt to this many SI seconds
        //
        // See the scaling spreadsheet for the calculation
        // m/kg/sec
        private const float G1_TIME_SI = 122406.4481f;

        public static double game_sec_per_phys_sec = 1f;

        // i.e. time is in units of approx. 3 ms
        // If we want Unity time in game sec. per physics hour then we need to 
        // timescale = game sec. per physics hour


        public static void Init()
        {
            GravityEngine ge = GravityEngine.Instance();
            UpdateMassScale(ge.units, ge.timeScale, ge.lengthScale);
        }

        /// <summary>
        /// The integrators in GE do not provide Newton's constant G as part of the force calculation. 
        /// 
        /// This is absorbed into a massScale that is applies to all masses as they are added to the 
        /// internal mass recorded in GravityState. In the case of dimensionful units this can be regarded
        /// as "pre-multiplying by G", where G is adjusted based on the units and the scaling of length and time
        /// indicated in the GE inspector settings.
        /// 
        /// This routine also assigns game_sec_per_phys_sec.
        /// </summary>
        /// <param name="units"></param>
        /// <param name="timeScale"></param>
        /// <param name="lengthScale"></param>
        /// <returns></returns>
        public static double UpdateMassScale(Units units, float timeScale, float lengthScale)
        {
            double mu = 0.0;
            // G scales based on the relative time/length scales (see Scaling worksheet)
            switch (units) {
                case Units.DIMENSIONLESS:
                    // mass scale is controlled via inspector for dimensionless units
                    game_sec_per_phys_sec = 1f;
                    velocityScale = 1f;
                    mu = GravityEngine.Instance().massScale;
                    break;
                case Units.SI:
                    game_sec_per_phys_sec = 1f / timeScale;
                    velocityScale = lengthScale / timeScale;
                    mu = NEWTONS_G_SI;
                    break;
                case Units.ORBITAL:
                    game_sec_per_phys_sec = SEC_PER_HOUR / timeScale;
                    velocityScale = lengthScale / timeScale;
                    mu = NEWTONS_G_KMHR;
                    break;
                case Units.SOLAR:
                    game_sec_per_phys_sec = SEC_PER_YEAR / timeScale;
                    velocityScale = KM_SEC_TO_AU_YR * lengthScale / timeScale;
                    mu = NEWTONS_G_AUYR;
                    break;
                default:
                    Debug.LogError("Unsupported units");
                    break;
            }
            if (units != Units.DIMENSIONLESS) {
                // adjust for timescale
                mu *= 1.0 / (timeScale * timeScale);
                // adjust for length scale
                mu *= lengthScale * lengthScale * lengthScale;
            }

#pragma warning disable 162       // disable unreachable code warning
            if (GravityEngine.DEBUG) {
                Debug.Log("SetMassScale:  " +
                        " timeScale=" + timeScale +
                        " lengthScale=" + lengthScale +
                        " game_sec_per_phys_sec=" + game_sec_per_phys_sec +
                        " massScale=" + mu + " velScale=" + velocityScale);
            }
#pragma warning restore 162
            return mu;
        }
        public static float GetGameSecondPerPhysicsSecond()
        {
            return (float)game_sec_per_phys_sec;
        }

        [System.Obsolete("Use the better named ScaleToGameSeconds")]
        public static float ScalePeriod(float period)
        {
            // do not need to scale length - already happened in determination of "a"
            float massScale = GravityEngine.Instance().massScale;
            return period / Mathf.Sqrt(massScale);
        }

        /// <summary>
        /// Scale a physics time to Unity game seconds
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static float ScaleToGameSeconds(float time)
        {
            float massScale = GravityEngine.Instance().massScale;
            return time / Mathf.Sqrt(massScale);
        }

        public static float GameSecToWorldTime(float gameSec)
        {
            return (float)(gameSec * game_sec_per_phys_sec);
        }

        /// <summary>
        /// Modify the physics engine acceleration to appear in world scale units. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="lengthScale"></param>
        /// <param name="timeScale"></param>
        /// <returns></returns>
        public static Vector3 ScaleAcceleration(Vector3 a, float lengthScale, float timeScale)
        {
            return (timeScale * timeScale) * a / lengthScale;
        }

        public static double AccelerationScaleInternalToGEUnits()
        {
            GravityEngine ge = GravityEngine.Instance();
            float ts = ge.timeScale;
            return ts * ts / ge.lengthScale;
        }

        /// <summary>
        /// Factor to convert GE units (SOLAR, ORBITAL) to GE internal velocity units. 
        /// 
        ///  vel_phys = nbody.vel * velocityScale;
        ///  
        /// </summary>
        /// <returns></returns>
        public static float GetVelocityScale()
        {
            return velocityScale;
        }

        /// <summary>
        /// Changes the length scale of all NBody objects in the scene due to a change in the inspector.
        /// Find all NBody containing objects.
        /// - independent objects are rescaled
        /// - orbit based objects have their primary dimension adjusted (e.g. for ellipse, a)
        ///   (these objects are scalable and are asked to rescale themselves)
        ///
        /// Not intended for run-time use.
        /// </summary>
        public static void ScaleScene(Units units, float lengthScale)
        {

            // ensure velocity scale is determined
            // find everything with an NBody. Rescale will ensure only independent NBodies are rescaled. 
            NBody[] nbodies = FindObjectsOfType<NBody>();
            foreach (NBody nbody in nbodies) {
                ScaleNBody(nbody, units, lengthScale);
            }
        }

        /// <summary>
        /// Convert a distance in GE physics units to scene (Unity) units
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static float ScaleDistancePhyToScene(float distance)
        {
            return distance / GravityEngine.Instance().lengthScale;
        }


        /// <summary>
        /// Convert a distance in GE physics units to scene (Unity) units
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector3d ScaleVector3dPhyToScene(Vector3d v)
        {
            // was backwards prior to 5.0
            return v / GravityEngine.Instance().lengthScale;
        }

        public static Vector3 ScalePositionSceneToPhys(Vector3 pos)
        {
            return pos;
        }

        /// <summary>
        /// Scales the N body using the provided units and length scale.
        /// </summary>
        /// <param name="nbody">Nbody.</param>
        /// <param name="units">Units.</param>
        /// <param name="timeScale">Time scale.</param>
        /// <param name="lengthScale">Length scale.</param>
        public static void ScaleNBody(NBody nbody, Units units, float lengthScale)
        {
            // If there is an IOrbitScaler - use it instead
            IOrbitScalable iorbit = nbody.gameObject.GetComponent<IOrbitScalable>();
            if (iorbit != null) {
                iorbit.ApplyScale(lengthScale);
            } else {
                //if (units == GravityScaler.Units.DIMENSIONLESS && lengthScale == 1f) {
                //	// Backwards compatibility with pre 1.3 GE
                //	nbody.initialPos = nbody.transform.position;
                //}
                nbody.ApplyScale(lengthScale, velocityScale);
            }
        }

        /// <summary>
        /// Scale the velocity in Unity scene units (e.g. suitable for a rigidbody) into GE interal
        /// units. 
        /// 
        /// Mapping of scene units depends on the unit choice in GE and the "Unity unit per km/AU"
        /// value set for GE.
        /// 
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public static Vector3 ScaleVelSceneToPhys(Vector3 vel)
        {
            return vel * velocityScale;
        }

        public static Vector3d ScaleVelSceneToPhys(Vector3d vel)
        {
            return vel * velocityScale;
        }

        public static Vector3 ScaleGEVelocityToSI(Vector3 v)
        {
            return v * (float)(VelocityScaletoSIUnits() / GetVelocityScale());
        }

        public static Vector3d ScaleGEVelocityToSI(Vector3d v)
        {
            return v * VelocityScaletoSIUnits() / GetVelocityScale();
        }

        public static Vector3d ScaleSIVelocityToGE(Vector3d v)
        {
            return v *  GetVelocityScale()/ VelocityScaletoSIUnits();
        }

        public static Vector3 ScaleGEPositionToSI(Vector3 v)
        {
            return v * (float)(PositionScaletoSIUnits() / GravityEngine.Instance().lengthScale);
        }

        public static Vector3d ScaleGEPositionToSI(Vector3d v)
        {
            return v * PositionScaletoSIUnits() / GravityEngine.Instance().lengthScale;
        }
        /// <summary>
        /// Scale the velocity from internal phys units to scene units. 
        /// 
        /// Mapping of scene units depends on the unit choice in GE and the "Unity unit per km/AU"
        /// value set for GE.
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public static Vector3 ScaleVelPhysToScene(Vector3 vel)
        {
            return vel / velocityScale;
        }

        public static double ScaleVelPhysMagnitudeToScene(double vel)
        {
            return vel / velocityScale;
        }

        public static Vector3d ScaleVelPhysToScene(Vector3d vel)
        {
            return vel / velocityScale;
        }

        //-----------CONVERSION HELPERS-------------------

        /// <summary>
        /// Provide conversion factor to convert acceleration in SI units to the
        /// GE units in use by Gravity Engine. 
        /// 
        /// This routine references the currently set units in the GE
        /// </summary>
        /// <returns>Conversion Factor</returns>
        public static double AccelSItoGEUnits()
        {
            double conversion = 1.0;
            switch (GravityEngine.Instance().units) {
                case Units.ORBITAL:
                    conversion = SEC_PER_HOUR * SEC_PER_HOUR / M_PER_KM;
                    break;

                case Units.SOLAR:
                    Debug.LogError("Implement me");
                    break;

                case Units.DIMENSIONLESS:
                case Units.SI:
                    // nothing to do 
                    break;
            }
            return conversion;
        }

        /// <summary>
        /// Determine the factor to convert acceleration to SI units. 
        /// </summary>
        /// <returns></returns>
        public static double AccelGEtoSIUnits()
        {
            double conversion = 1.0;
            switch (GravityEngine.Instance().units) {
                case Units.ORBITAL:
                    conversion *= M_PER_KM / (SEC_PER_HOUR * SEC_PER_HOUR);
                    break;

                case Units.SOLAR:
                    Debug.LogError("Implement me");
                    break;

                case Units.DIMENSIONLESS:
                case Units.SI:
                    // nothing to do 
                    break;
            }
            return conversion;
        }

        /// <summary>
        /// Determine the factor to convert scaled position to SI units. 
        /// </summary>
        /// <returns></returns>
        public static double PositionScaletoSIUnits()
        {
            double conversion = 1.0;
            switch (GravityEngine.Instance().units) {
                case Units.ORBITAL:
                    // KM to M
                    conversion *= M_PER_KM;
                    break;

                case Units.SOLAR:
                    // AU to m
                    conversion *= M_PER_AU;
                    break;

                case Units.DIMENSIONLESS:
                case Units.SI:
                    // nothing to do 
                    break;
            }
            return conversion;
        }

        /// <summary>
        /// Determine the factor to convert scaled velocity to SI units. 
        /// </summary>
        /// <returns></returns>
        public static double VelocityScaletoSIUnits()
        {
            double conversion = 1.0;
            switch (GravityEngine.Instance().units) {
                case Units.ORBITAL:
                    // km/hr to m/s
                    conversion = M_PER_KM / SEC_PER_HOUR;
                    break;

                case Units.SOLAR:
                    // km/sec to m/s
                    conversion = M_PER_KM;
                    break;

                case Units.DIMENSIONLESS:
                case Units.SI:
                    // nothing to do 
                    break;
            }
            return conversion;
        }

        /// <summary>
        /// Provide the conversion factor to convert acceleration from GE units
        /// (e.g. km/hr^2 if ORBITAL) into the internal units. The internal units
        /// are determined based on the timeScale and lengthScale chosen. 
        /// 
        /// This routine references the currently set units in the GE
        /// </summary>
        /// <returns>Conversion factor</returns>
        public static double AccelGEtoInternalUnits()
        {
            GravityEngine ge = GravityEngine.Instance();
            return ge.lengthScale / (ge.timeScale * ge.timeScale);
        }

        //-------------STRING HELPERS----------------------

        /// <summary>
        /// Return the string indicating the length units in use by the gravity engine. 
        /// </summary>
        /// <returns>The units.</returns>
        public static string LengthUnits(Units units)
        {
            return lengthUnits[(int)units];
        }

        /// <summary>
        /// Return the string indicating the velocity units in use by the gravity engine. 
        /// </summary>
        /// <returns>The units.</returns>
        public static string VelocityUnits(Units units)
        {
            return velocityUnits[(int)units];
        }

        /// <summary>
        /// Return the string indicating the mass units in use by the gravity engine.
        /// </summary>
        /// <returns>The units.</returns>
        public static string MassUnits(Units units)
        {
            return massUnits[(int)units];
        }

        /// <summary>
        /// Return the world time in the selected units as a string. 
        /// 
        /// DateTime reports 0 time as year 1, so that is the starting time in SOLAR and ORBITAL units.
        /// </summary>
        /// <returns></returns>
        public static string GetWorldTimeFormatted(double physTime, Units units)
        {
            string s;
            if (double.IsNaN(physTime))
                return "NaN";

            double time = physTime * game_sec_per_phys_sec;
            switch (units) {
                case GravityScaler.Units.DIMENSIONLESS:
                    s = string.Format("{0:0000.0}", time);
                    break;
                case GravityScaler.Units.ORBITAL:
                    // convert to HHh MMm SSs (24 hour clock)
                    // use ticks constructor (100ns intervals is an integer tick)
                    long ticTime = (long)(time * 1E7);
                    System.DateTime dateTime = new System.DateTime(ticTime);
                    // start day count at 0
                    s = string.Format("{0:00}:{1}", dateTime.Day - 1, dateTime.ToString("HH:mm:ss")); // 24h format

                    break;
                case GravityScaler.Units.SI:
                    s = string.Format("{0:0000.0}", time);
                    break;
                case GravityScaler.Units.SOLAR:
                    // convert to HHh MMm SSs (24 hour clock)
                    // use ticks constructor (100ns intervals is an integer tick)
                    // NOTE: "Internally, all DateTime values are represented as the number of 
                    // ticks(the number of 100 - nanosecond intervals) that have elapsed since 
                    // 12:00:00 midnight, January 1, 0001."
                    long ticTimeSolar = (long)(time * 1E7);
                    System.DateTime dateTimeSolar = new System.DateTime(ticTimeSolar);
                    s = dateTimeSolar.Year - 1 + ":" + dateTimeSolar.ToString("MM:dd:HH:mm"); // 24h format
                    break;
                default:
                    s = "unknown units";
                    break;
            }
            return s;
        }

        /// <summary>
        /// Return the time as a C# TimeSpan object. Used in code to show Calendar date for the 
        /// evolution. Typically in SOLAR or ORBITAL units.
        /// </summary>
        /// <param name="physTime"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        public static System.TimeSpan GetTimeSpan(double physTime, Units units)
        {
            double time = physTime * game_sec_per_phys_sec;
            long ticTimeSolar = (long)(time * 1E7);
            // convert to HHh MMm SSs (24 hour clock)
            // use ticks constructor (100ns intervals is an integer tick)
            // NOTE: "Internally, all DateTime values are represented as the number of 
            // ticks(the number of 100 - nanosecond intervals) that have elapsed since 
            // 12:00:00 midnight, January 1, 0001."
            return new System.TimeSpan(ticTimeSolar);
        }

        /// <summary>
        /// Return the world time in the selected units as a double.
        /// </summary>
        /// <returns></returns>
        public static double GetWorldTimeSeconds(double physTime)
        {
            return physTime * game_sec_per_phys_sec;
        }

        public static double WorldSecsToPhysTime(double worldSecs)
        {
            return worldSecs / game_sec_per_phys_sec;
        }

        /// <summary>
        /// Convert the world time in GravityEngine units to GE internal phys time. 
        /// </summary>
        /// <param name="worldTime"></param>
        /// <returns></returns>
        public static double WorldTimeToPhysTime(double worldTime)
        {
            double worldSecs = 0.0;
            switch (GravityEngine.Instance().units) {
                case GravityScaler.Units.DIMENSIONLESS:
                case GravityScaler.Units.SI:
                    worldSecs = worldTime;
                    break;
                case GravityScaler.Units.ORBITAL:
                    worldSecs = worldTime * SEC_PER_HOUR;
                    break;
                case GravityScaler.Units.SOLAR:
                    worldSecs = worldTime * SEC_PER_YEAR;
                    break;
                default:
                    break;
            }
            return worldSecs / game_sec_per_phys_sec;
        }

    }
#if GE_NAMESPACE
}
#endif
